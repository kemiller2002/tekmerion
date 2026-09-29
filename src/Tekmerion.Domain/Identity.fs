namespace Tekmerion.Domain

open System
open System.Security.Cryptography
open System.Text

/// Identity of the repository that holds canonical research. Qualifies every
/// key in machine contracts so aggregation is not designed out (TEK-IDY-008).
type RepositoryId = private RepositoryId of string

[<RequireQualifiedAccess>]
module RepositoryId =

    let create (raw: string) : Result<RepositoryId, string> =
        if String.IsNullOrWhiteSpace raw then Error "repository id is empty"
        elif raw |> Seq.exists Char.IsWhiteSpace then Error $"repository id '{raw}' contains whitespace"
        else Ok(RepositoryId raw)

    let value (RepositoryId raw) = raw

/// A normalised, repository-relative path that cannot escape the repository
/// root: forward slashes, no leading slash, no `.` or `..` segments.
type RepoPath = private RepoPath of string

[<RequireQualifiedAccess>]
module RepoPath =

    let create (raw: string) : Result<RepoPath, string> =
        if String.IsNullOrWhiteSpace raw then
            Error "path is empty"
        else
            let slashed = raw.Replace('\\', '/')

            if slashed.StartsWith "/" || (slashed.Length > 1 && slashed.[1] = ':') then
                Error $"path '{raw}' is absolute"
            else
                let segments = slashed.Split('/', StringSplitOptions.RemoveEmptyEntries) |> List.ofArray

                if segments |> List.exists (fun segment -> segment = "..") then
                    Error $"path '{raw}' escapes the repository root"
                else
                    match segments |> List.filter (fun segment -> segment <> ".") with
                    | [] -> Error $"path '{raw}' names no file"
                    | kept -> Ok(RepoPath(String.Join("/", kept)))

    let value (RepoPath raw) = raw

/// An identifier the research itself declares (`id:`). Authoritative and
/// permanent; never invented (TEK-IDY-001). The grammar is deliberately
/// permissive because the corpus has no single id grammar, but it must be
/// usable as a URL segment.
type ArtifactId = private ArtifactId of string

[<RequireQualifiedAccess>]
module ArtifactId =

    let private allowed (character: char) =
        Char.IsAsciiLetterOrDigit character || character = '-' || character = '_' || character = '.'

    let create (raw: string) : Result<ArtifactId, string> =
        if String.IsNullOrWhiteSpace raw then Error "declared id is empty"
        elif raw.Length > 128 then Error $"declared id '{raw}' is longer than 128 characters"
        elif raw.StartsWith "." then Error $"declared id '{raw}' starts with '.'"
        elif not (raw |> Seq.forall allowed) then Error $"declared id '{raw}' contains characters outside [A-Za-z0-9._-]"
        else Ok(ArtifactId raw)

    let value (ArtifactId raw) = raw

/// Stable second-class key for an artifact that declares no id. Derived only
/// from the repository-relative source path, never from a title, and never
/// presented or written back as a declared id (TEK-IDY-002, TEK-IDY-003).
type PathKey = private PathKey of string

[<RequireQualifiedAccess>]
module PathKey =

    /// First 16 hex characters of SHA-256 over the UTF-8 normalised path.
    /// Changes only when the file moves, which is a real identity change.
    let ofPath (path: RepoPath) : PathKey =
        let digest = SHA256.HashData(Encoding.UTF8.GetBytes(RepoPath.value path))
        PathKey(Convert.ToHexString(digest).Substring(0, 16).ToLowerInvariant())

    let value (PathKey raw) = raw

/// How an artifact is addressed. The case records whether the identity was
/// declared by research or derived by Tekmerion.
type ArtifactKey =
    | Declared of ArtifactId
    | PathDerived of PathKey

[<RequireQualifiedAccess>]
module ArtifactKey =

    /// A declared id wins; otherwise the path-derived key. There is no third
    /// option, in particular none that consults the title.
    let assign (declared: ArtifactId option) (path: RepoPath) : ArtifactKey =
        match declared with
        | Some id -> Declared id
        | None -> PathDerived(PathKey.ofPath path)

    let value (key: ArtifactKey) =
        match key with
        | Declared id -> ArtifactId.value id
        | PathDerived pathKey -> PathKey.value pathKey

    /// Public address (TEK-IDY-004).
    let url (key: ArtifactKey) =
        match key with
        | Declared id -> $"/a/{ArtifactId.value id}/"
        | PathDerived pathKey -> $"/s/{PathKey.value pathKey}/"

    let isDeclared (key: ArtifactKey) =
        match key with
        | Declared _ -> true
        | PathDerived _ -> false
