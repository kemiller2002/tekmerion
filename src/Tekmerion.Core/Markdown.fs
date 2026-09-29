namespace Tekmerion.Core

open System
open System.Text
open System.Text.RegularExpressions
open Tekmerion.Domain

/// Body structure: ATX headings outside fenced code, each section's body kept
/// verbatim (TEK-ING-003). Nothing is interpreted as a research object.
module Markdown =

    let private heading = Regex(@"^(#{1,6})[ \t]+(.*?)[ \t]*#*[ \t]*$", RegexOptions.CultureInvariant)
    let private fence = Regex(@"^[ ]{0,3}(`{3,}|~{3,})", RegexOptions.CultureInvariant)

    /// GitHub-style heading slug: lower case, punctuation removed, spaces to
    /// hyphens. Best-effort by design (TEK-IDY-007).
    let slug (text: string) =
        let builder = StringBuilder()

        for character in text.Trim().ToLowerInvariant() do
            if Char.IsLetterOrDigit character || character = '-' || character = '_' then
                builder.Append(character) |> ignore
            elif character = ' ' then
                builder.Append('-') |> ignore

        builder.ToString()

    type private Heading = { Depth: int; Text: string; Line: int }

    /// Headings with 1-based file line numbers, skipping fenced code blocks.
    let private headings (lines: string array) (firstLine: int) : Heading list =
        let folder (inFence: string option, found: Heading list) (index: int, line: string) =
            let fenceMatch = fence.Match line

            match inFence with
            | Some marker when fenceMatch.Success && fenceMatch.Groups.[1].Value.StartsWith marker -> None, found
            | Some _ -> inFence, found
            | None when fenceMatch.Success -> Some(fenceMatch.Groups.[1].Value.Substring(0, 3)), found
            | None ->
                let headingMatch = heading.Match line

                if headingMatch.Success then
                    None,
                    { Depth = headingMatch.Groups.[1].Value.Length
                      Text = headingMatch.Groups.[2].Value
                      Line = firstLine + index }
                    :: found
                else
                    None, found

        lines |> Array.indexed |> Array.fold folder (None, []) |> snd |> List.rev

    /// Split a body into sections. Text before the first heading becomes a
    /// depth-0 section with an empty heading, only when it is not blank.
    let sections (location: SourceLocation) (body: string) (bodyStartLine: int) : Section list =
        let lines = body.Split('\n')
        let found = headings lines bodyStartLine

        let text (fromLine: int) (toLine: int) =
            let first = fromLine - bodyStartLine
            let last = min (toLine - bodyStartLine) lines.Length
            if first >= last then "" else String.Join("\n", lines.[first .. last - 1]).Trim('\n')

        let preamble =
            let firstHeading = found |> List.tryHead |> Option.map (fun h -> h.Line) |> Option.defaultValue (bodyStartLine + lines.Length)
            let content = text bodyStartLine firstHeading

            if String.IsNullOrWhiteSpace content then
                []
            else
                [ { Heading = ""
                    Depth = 0
                    Slug = ""
                    Body = content
                    Location = { location with Line = Some bodyStartLine } } ]

        // Each section ends where the next heading starts, the last at the end.
        let ends =
            (found |> List.map (fun h -> h.Line) |> List.skip (min 1 found.Length)) @ [ bodyStartLine + lines.Length ]
            |> List.take found.Length

        let withPaths =
            List.zip found ends
            |> List.fold
                (fun (stack: Heading list, sectionsSoFar: (Heading * int * string list) list) (current, next) ->
                    let ancestors = stack |> List.filter (fun h -> h.Depth < current.Depth)
                    let path = (current :: ancestors) |> List.rev |> List.map (fun h -> h.Text)
                    current :: ancestors, (current, next, path) :: sectionsSoFar)
                ([], [])
            |> snd
            |> List.rev

        let slugs =
            withPaths
            |> List.fold
                (fun (seen: Map<string, int>, acc: string list) (current, _, _) ->
                    let baseSlug = slug current.Text

                    match seen.TryFind baseSlug with
                    | Some count -> seen.Add(baseSlug, count + 1), $"{baseSlug}-{count}" :: acc
                    | None -> seen.Add(baseSlug, 1), baseSlug :: acc)
                (Map.empty, [])
            |> snd
            |> List.rev

        preamble
        @ (List.zip withPaths slugs
           |> List.map (fun ((current, next, path), anchor) ->
               { Heading = current.Text
                 Depth = current.Depth
                 Slug = anchor
                 Body = text (current.Line + 1) next
                 Location =
                   { location with
                       Line = Some current.Line
                       HeadingPath = path } }))
