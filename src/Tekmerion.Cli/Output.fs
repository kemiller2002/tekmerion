namespace Tekmerion.Cli

open System
open System.IO
open System.Text
open Aegis
open Tekmerion.Domain
open Tekmerion.Core

type PublishOutcome =
    { Digest: ContentDigest
      Files: int
      Bytes: int64
      Recovered: string list }

/// Staged, verified, atomically promoted output (TEK-PUB-003, TEK-REC-002).
///
/// Files are written to `<out>.tekmerion-staging`, re-read and verified, then
/// promoted by two directory renames: the live output moves aside to
/// `<out>.tekmerion-previous`, the staging directory takes its place, and the
/// previous copy is removed. If the process stops between the two renames,
/// `recover` restores the previous publication on the next run. Until the
/// first rename, the live output is never touched.
module Output =

    let stagingOf (out: string) = out.TrimEnd('/', '\\') + ".tekmerion-staging"
    let previousOf (out: string) = out.TrimEnd('/', '\\') + ".tekmerion-previous"

    let private utf8 = UTF8Encoding(false)

    /// Digest of the whole output tree: SHA-256 over sorted (path, sha256).
    let digest (files: OutputFile list) =
        files
        |> List.sortBy (fun f -> f.Path)
        |> List.map (fun f -> $"{f.Path}\u0000{Hashing.sha256Bytes (utf8.GetBytes f.Text)}")
        |> fun lines -> ContentDigest("sha256:" + Hashing.sha256Text (String.Join("\n", lines)))

    /// Returns the paths that would escape the output root (blocking).
    let escapingPaths (files: OutputFile list) =
        files
        |> List.filter (fun f ->
            match RepoPath.create f.Path with
            | Ok path -> RepoPath.value path <> f.Path
            | Error _ -> true)
        |> List.map (fun f -> f.Path)

    /// Undo an interrupted promotion and discard stale staging.
    let recover (config: AegisConfig) (out: string) : Guarded<string list> =
        let scope = Aegis.scope config "Tekmerion.Output.Recover" (Map [ "out", Internal out ])

        Aegis.capture config scope (Faults.promote config) (fun () ->
            let previous = previousOf out
            let staging = stagingOf out

            [ if not (Directory.Exists out) && Directory.Exists previous then
                  Directory.Move(previous, out)
                  "restored the previous publication after an interrupted promotion"
              elif Directory.Exists previous then
                  Directory.Delete(previous, true)
                  "removed a leftover previous-publication copy"
              if Directory.Exists staging then
                  Directory.Delete(staging, true)
                  "discarded incomplete staging output" ])

    let stage (config: AegisConfig) (out: string) (files: OutputFile list) : Guarded<int64> =
        let scope = Aegis.scope config "Tekmerion.Output.Stage" (Map [ "out", Internal out ])

        Aegis.capture config scope (Faults.writeOutput config) (fun () ->
            let staging = stagingOf out
            Directory.CreateDirectory staging |> ignore

            let written =
                files
                |> List.sumBy (fun file ->
                    let target = Path.Combine(staging, file.Path)
                    Directory.CreateDirectory(Path.GetDirectoryName target) |> ignore
                    let bytes = utf8.GetBytes file.Text
                    File.WriteAllBytes(target, bytes)
                    int64 bytes.Length)

            // Verify what is on disk before it can become the publication.
            for file in files do
                let onDisk = File.ReadAllBytes(Path.Combine(staging, file.Path))

                if Hashing.sha256Bytes onDisk <> Hashing.sha256Bytes (utf8.GetBytes file.Text) then
                    raise (IOException $"staged file {file.Path} does not match what was written")

            written)

    let promote (config: AegisConfig) (out: string) : Guarded<unit> =
        let scope = Aegis.scope config "Tekmerion.Output.Promote" (Map [ "out", Internal out ])

        Aegis.capture config scope (Faults.promote config) (fun () ->
            let previous = previousOf out

            if Directory.Exists out then
                Directory.Move(out, previous)

            Directory.Move(stagingOf out, out)

            if Directory.Exists previous then
                Directory.Delete(previous, true))
