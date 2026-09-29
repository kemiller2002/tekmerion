namespace Tekmerion.Core

open System
open System.IO
open YamlDotNet.Core
open YamlDotNet.RepresentationModel

/// A YAML value as written. Scalars stay strings: `2026-07-21` is not coerced
/// to a date and `YYYY-MM-DD` is not rejected (TEK-FID-001).
type YamlValue =
    | YNull
    | YScalar of string
    | YSequence of YamlValue list
    | YMapping of (string * YamlValue) list

/// One top-level front-matter key with its parsed value, the verbatim source
/// text of the entry, and its 1-based line in the file (TEK-PRV-001).
type FrontMatterEntry =
    { Key: string
      Value: YamlValue
      Raw: string
      Line: int }

type FrontMatterSplit =
    | NoFrontMatter of body: string
    | WithFrontMatter of yaml: string * yamlStartLine: int * body: string * bodyStartLine: int
    | UnterminatedFrontMatter of body: string

type FrontMatterParse =
    | Entries of FrontMatterEntry list
    | Malformed of reason: string * line: int option

module FrontMatter =

    let normaliseNewlines (text: string) =
        text.Replace("\r\n", "\n").Replace('\r', '\n').TrimStart('﻿')

    let private isFence (line: string) = line.TrimEnd() = "---"

    /// Split `---`-delimited front matter from the body. Line numbers are
    /// 1-based positions in the original file.
    let split (text: string) : FrontMatterSplit =
        let lines = (normaliseNewlines text).Split('\n')

        if lines.Length = 0 || not (isFence lines.[0]) then
            NoFrontMatter(normaliseNewlines text)
        else
            let closing =
                lines
                |> Array.indexed
                |> Array.skip 1
                |> Array.tryFind (fun (_, line) -> isFence line || line.TrimEnd() = "...")

            match closing with
            | None -> UnterminatedFrontMatter(normaliseNewlines text)
            | Some(index, _) ->
                let yaml = String.Join("\n", lines.[1 .. index - 1])
                let body = String.Join("\n", lines.[index + 1 ..])
                WithFrontMatter(yaml, 2, body, index + 2)

    let private isNullScalar (node: YamlScalarNode) =
        node.Style = ScalarStyle.Plain
        && (match node.Value with
            | null
            | ""
            | "~"
            | "null"
            | "Null"
            | "NULL" -> true
            | _ -> false)

    let rec private convert (node: YamlNode) : YamlValue =
        match node with
        | :? YamlScalarNode as scalar when isNullScalar scalar -> YNull
        | :? YamlScalarNode as scalar -> YScalar scalar.Value
        | :? YamlSequenceNode as sequence -> sequence.Children |> Seq.map convert |> List.ofSeq |> YSequence
        | :? YamlMappingNode as mapping ->
            mapping.Children
            |> Seq.map (fun pair ->
                let key =
                    match pair.Key with
                    | :? YamlScalarNode as scalar -> scalar.Value
                    | other -> string other

                key, convert pair.Value)
            |> List.ofSeq
            |> YMapping
        | other -> YScalar(string other)

    /// Parse the front-matter block. A malformed block is a typed outcome
    /// (a warning finding downstream), never an exception.
    let parse (yaml: string) (yamlStartLine: int) : FrontMatterParse =
        let lines = yaml.Split('\n')

        let rawBetween (first: int) (next: int) =
            lines.[first .. next - 1]
            |> Array.rev
            |> Array.skipWhile String.IsNullOrWhiteSpace
            |> Array.rev
            |> fun kept -> String.Join("\n", kept)

        try
            let stream = YamlStream()
            stream.Load(new StringReader(yaml))

            match List.ofSeq stream.Documents with
            | [] -> Entries []
            | document :: _ ->
                match document.RootNode with
                | :? YamlMappingNode as root ->
                    let pairs = root.Children |> List.ofSeq
                    // YamlDotNet lines are 1-based within the block.
                    let starts = pairs |> List.map (fun pair -> int pair.Key.Start.Line - 1)
                    let ends = (starts |> List.tail) @ [ lines.Length ]

                    List.zip3 pairs starts ends
                    |> List.map (fun (pair, first, next) ->
                        { Key =
                            match pair.Key with
                            | :? YamlScalarNode as scalar -> scalar.Value
                            | other -> string other
                          Value = convert pair.Value
                          Raw = rawBetween first next
                          Line = yamlStartLine + first })
                    |> Entries
                | :? YamlScalarNode as scalar when isNullScalar scalar -> Entries []
                | _ -> Malformed("front matter is not a key/value mapping", Some yamlStartLine)
        with
        | :? YamlException as error -> Malformed(error.Message, Some(yamlStartLine + int error.Start.Line - 1))
        // YamlDotNet's scanner reports some malformed input this way; it is
        // still a property of the data, not a defect in Tekmerion.
        | :? InvalidOperationException as error -> Malformed($"unreadable YAML ({error.Message})", Some yamlStartLine)

    /// Scalar text of a value, or the list items' texts; used for fields that
    /// the corpus writes both as a scalar and as a list.
    let rec texts (value: YamlValue) : string list =
        match value with
        | YNull -> []
        | YScalar text -> [ text ]
        | YSequence items -> items |> List.collect texts
        | YMapping _ -> []
