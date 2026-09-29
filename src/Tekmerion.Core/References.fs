namespace Tekmerion.Core

open System
open System.Text.RegularExpressions
open Tekmerion.Domain

/// Classification of reference values exactly as written (03 C.2). A value is
/// never promoted to a stronger kind than its text supports: prose stays prose.
module References =

    let private idPattern = Regex(@"^[A-Z][A-Z0-9]*(?:-[A-Za-z0-9]+)+$", RegexOptions.CultureInvariant)
    let private markdownLink = Regex(@"^\[[^\]]*\]\(([^)\s]+)\)$", RegexOptions.CultureInvariant)
    let private fileExtension = Regex(@"\.(md|markdown|json|csv|yaml|yml|txt|pdf|png|svg)(#.*)?$", RegexOptions.CultureInvariant ||| RegexOptions.IgnoreCase)

    let rec classify (raw: string) : ReferenceValue =
        let text = raw.Trim()
        let linked = markdownLink.Match text

        if linked.Success then classify linked.Groups.[1].Value
        elif String.IsNullOrWhiteSpace text then Unparsed raw
        elif text.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
             || text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) then ExternalUrl text
        elif text.StartsWith "./" || text.StartsWith "../" then FileRelativePath text
        elif not (text.Contains ' ') && text.Contains '/' then RepoRelativePath text
        elif not (text.Contains ' ') && fileExtension.IsMatch text then FileRelativePath text
        elif idPattern.IsMatch text then IdReference text
        else ProseTitle text

    /// A bare `name.md` with no directory: resolvable only relative to the
    /// declaring file, and worth an authoring hint (G.5 warning).
    let isBareFilename (value: ReferenceValue) =
        match value with
        | FileRelativePath text -> not (text.Contains '/')
        | _ -> false

    let private withoutFragment (text: string) =
        match text.IndexOf '#' with
        | -1 -> text
        | index -> text.Substring(0, index)

    let private combine (directory: string) (relative: string) =
        let segments =
            (if directory = "" then relative else directory + "/" + relative).Split('/')
            |> Array.fold
                (fun (acc: string list) segment ->
                    match segment with
                    | ""
                    | "." -> acc
                    | ".." ->
                        match acc with
                        | _ :: rest -> rest
                        | [] -> [ ".." ]
                    | name -> name :: acc)
                []
            |> List.rev

        String.Join("/", segments)

    /// Candidate repository paths for a path-like reference, most specific
    /// first. Candidates that escape the root are dropped by RepoPath.
    let candidatePaths (declaringFile: RepoPath) (value: ReferenceValue) : RepoPath list =
        let file = RepoPath.value declaringFile

        let directory =
            match file.LastIndexOf '/' with
            | -1 -> ""
            | index -> file.Substring(0, index)

        let raw =
            match value with
            | FileRelativePath text -> [ combine directory (withoutFragment text) ]
            | RepoRelativePath text ->
                let target = withoutFragment text
                [ combine "" target; combine directory target ]
            | _ -> []

        raw |> List.distinct |> List.choose (RepoPath.create >> Result.toOption)
