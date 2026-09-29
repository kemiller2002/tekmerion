namespace Tekmerion.Cli

open System
open System.IO
open System.Text
open Aegis
open Tekmerion.Domain
open Tekmerion.Core

/// Filesystem reads, each behind an Aegis boundary. Returns data or a fault;
/// never throws an infrastructure exception across the boundary.
module FileSystem =

    let private strictUtf8 = UTF8Encoding(false, true)

    let private relative (root: string) (full: string) =
        Path.GetRelativePath(root, full).Replace('\\', '/')

    /// Every file under `root` as a repository-relative path, skipping `.git`.
    let listRepository (config: AegisConfig) (root: string) : Guarded<Set<string>> =
        let scope = Aegis.scope config "Tekmerion.Repository.List" (Map [ "root", Internal root ])

        Aegis.capture config scope (Faults.enumerate config) (fun () ->
            Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            |> Seq.map (relative root)
            |> Seq.filter (fun path -> not (path.StartsWith ".git/" || path = ".git"))
            |> Set.ofSeq)

    /// The I/O boundary: bytes or a fault.
    let readBytes (config: AegisConfig) (root: string) (path: string) : Guarded<byte array> =
        let scope = Aegis.scope config "Tekmerion.Source.Read" (Map [ "path", Public path ])
        Aegis.capture config scope (Faults.readSource config) (fun () -> File.ReadAllBytes(Path.Combine(root, path)))

    /// Decoding is not an operational failure: text that is not UTF-8 is a
    /// property of the source, reported as a finding (doc 18 §2.5).
    let decode (bytes: byte array) : Result<string, string> =
        try
            Ok(strictUtf8.GetString bytes)
        with :? DecoderFallbackException ->
            Error "the file is not valid UTF-8"

    let readText (config: AegisConfig) (root: string) (path: string) : Result<string, string> =
        match readBytes config root path with
        | Result.Error fault -> Result.Error(Faults.describe fault)
        | Ok bytes -> decode bytes

    let readConfig (config: AegisConfig) (path: string) : Guarded<string> =
        let scope = Aegis.scope config "Tekmerion.Config.Read" (Map [ "path", Public path ])
        Aegis.capture config scope (Faults.readConfig config) (fun () -> File.ReadAllText path)

    /// Read every selected file. A file that cannot be read or decoded is an
    /// `UnreadableSource` finding (blocking), not a crash.
    let readSources (config: AegisConfig) (root: string) (paths: string list) : SourceFile list * (string * string) list =
        paths
        |> List.fold
            (fun (read: SourceFile list, failed) path ->
                match RepoPath.create path with
                | Error reason -> read, (path, reason) :: failed
                | Ok repoPath ->
                    match readText config root path with
                    | Ok text -> { Path = repoPath; Text = text } :: read, failed
                    | Result.Error reason -> read, (path, reason) :: failed)
            ([], [])
        |> fun (read, failed) -> List.rev read, List.rev failed
