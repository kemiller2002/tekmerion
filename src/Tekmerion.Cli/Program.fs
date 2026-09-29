namespace Tekmerion.Cli

open System
open Aegis
open Tekmerion.Domain
open Tekmerion.Core

module Program =

    let private usage =
        """tekmerion — evidence-faithful research ingestion and publication

Usage:
  tekmerion validate --config PATH [--root DIR] [--json]
  tekmerion ingest   --config PATH [--root DIR] --out DIR [--forma CSS] [--json]
  tekmerion --version

  validate  discover, parse, type, validate and resolve; write nothing
  ingest    validate, then write the static research site and versioned
            machine contracts (DIR/data/v1) through staging and promotion.
            --forma names Forma's all.css from a pinned
            @echelon-foundry/design-system release; without it pages are
            unstyled semantic HTML

Exit codes: 0 success, 1 publication blocked, 2 invalid arguments,
6 environment failure. --json writes one JSON document to stdout."""

    let rec private parseOptions (args: string list) (options: IngestOptions) =
        match args with
        | [] -> Ok options
        | "--config" :: value :: rest -> parseOptions rest { options with ConfigPath = value }
        | "--root" :: value :: rest -> parseOptions rest { options with Root = Some value }
        | "--out" :: value :: rest -> parseOptions rest { options with Out = Some value }
        | "--json" :: rest -> parseOptions rest { options with Json = true }
        | "--forma" :: value :: rest -> parseOptions rest { options with Forma = Some value }
        | unknown :: _ -> Error $"unknown argument '{unknown}'"

    let private summary (command: string) (result: CommandResult) =
        let corpusFields =
            match result.Corpus with
            | None -> []
            | Some corpus ->
                let bySeverity severity =
                    corpus.Findings |> List.filter (fun f -> Policy.severity f.Code = severity) |> List.length

                [ "state", Json.str (Publication.stateName corpus.State)
                  "artifacts", Json.int corpus.Artifacts.Length
                  "canonicalEdges", Json.int corpus.Canonical.Length
                  "derivedEdges", Json.int corpus.Derived.Length
                  "findings",
                  Json.obj
                      [ "blocking", Json.int (bySeverity Blocking)
                        "warning", Json.int (bySeverity Warning)
                        "informational", Json.int (bySeverity Informational) ]
                  "blocking",
                  corpus.Findings
                  |> List.filter (fun f -> Policy.severity f.Code = Blocking)
                  |> List.map Contracts.findingJson
                  |> Json.arr ]

        let published =
            match result.Published with
            | None -> []
            | Some outcome ->
                let (ContentDigest digest) = outcome.Digest

                [ "published",
                  Json.obj
                      [ "digest", Json.str digest
                        "files", Json.int outcome.Files
                        "bytes", JInt outcome.Bytes
                        "recovered", Json.strings outcome.Recovered ] ]

        Json.obj (
            [ "command", Json.str command; "exitCode", Json.int result.ExitCode; "version", Json.str Version.Tekmerion ]
            @ corpusFields
            @ published
            @ [ "messages", Json.strings result.Messages ]
        )

    let private human (command: string) (result: CommandResult) =
        result.Corpus
        |> Option.iter (fun corpus ->
            let count severity =
                corpus.Findings |> List.filter (fun f -> Policy.severity f.Code = severity) |> List.length

            printfn "%s: %s" command (Publication.stateName corpus.State)
            printfn "  artifacts        %d" corpus.Artifacts.Length
            printfn "  canonical edges  %d" corpus.Canonical.Length
            printfn "  derived edges    %d" corpus.Derived.Length
            printfn "  findings         %d blocking, %d warning, %d informational" (count Blocking) (count Warning) (count Informational)

            for finding in corpus.Findings |> List.filter (fun f -> Policy.severity f.Code <> Informational) do
                let where =
                    finding.Location
                    |> Option.map (fun l -> $"""{RepoPath.value l.Path}{l.Line |> Option.map (sprintf ":%d") |> Option.defaultValue ""}""")
                    |> Option.defaultValue "-"

                printfn "  [%s] %s %s" (Policy.code finding.Code) where finding.Message)

        result.Published
        |> Option.iter (fun outcome ->
            let (ContentDigest digest) = outcome.Digest
            printfn "  published        %d files, %d bytes, %s" outcome.Files outcome.Bytes digest
            outcome.Recovered |> List.iter (printfn "  recovered        %s"))

        result.Messages |> List.iter (eprintfn "%s")

    [<EntryPoint>]
    let main argv =
        // Blocking persistence: a short-lived CLI must not exit before its
        // fault events are written.
        let aegis = { Aegis.configure "Tekmerion" (Some Version.Tekmerion) [ Sinks.standardError ] with Persistence = PersistenceMode.Blocking }
        let defaults = { ConfigPath = "tekmerion.config.json"; Root = None; Out = None; Forma = None; Json = false }

        match List.ofArray argv with
        | [ "--version" ] ->
            printfn "tekmerion %s" Version.Tekmerion
            ExitCode.Success
        | []
        | [ "--help" ]
        | [ "-h" ] ->
            printfn "%s" usage
            ExitCode.Success
        | command :: rest when command = "validate" || command = "ingest" ->
            match parseOptions rest defaults with
            | Error problem ->
                eprintfn "%s\n\n%s" problem usage
                ExitCode.InvalidArguments
            | Ok options ->
                let result =
                    if command = "validate" then Commands.validate aegis options else Commands.ingest aegis options

                if options.Json then
                    Console.Out.Write(Json.serialize (summary command result))
                    result.Messages |> List.iter (eprintfn "%s")
                else
                    human command result

                result.ExitCode
        | unknown :: _ ->
            eprintfn "unknown command '%s'\n\n%s" unknown usage
            ExitCode.InvalidArguments
