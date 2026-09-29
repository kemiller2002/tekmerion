namespace Tekmerion.Cli

open System
open System.IO
open Aegis
open Tekmerion.Domain
open Tekmerion.Core

/// Exit codes: a documented contract, aligned with the Echelon lifecycle CLIs.
module ExitCode =
    let Success = 0
    let Blocked = 1
    let InvalidArguments = 2
    let EnvironmentFailure = 6

type IngestOptions =
    { ConfigPath: string
      Root: string option
      Out: string option
      /// Path to Forma's `all.css` from a pinned
      /// `@echelon-foundry/design-system` release (doc 18 §3).
      Forma: string option
      Json: bool }

type CommandResult =
    { ExitCode: int
      Corpus: Corpus option
      Published: PublishOutcome option
      Messages: string list }

/// discover → parse → type → validate → resolve → project → stage → promote.
module Commands =

    let private fail code messages =
        { ExitCode = code
          Corpus = None
          Published = None
          Messages = messages }

    /// Load config, select files and ingest. Pure after the reads.
    let loadWithConfig (aegis: AegisConfig) (options: IngestOptions) : Result<TekmerionConfig * string * Corpus, CommandResult> =
        match FileSystem.readConfig aegis options.ConfigPath with
        | Result.Error fault -> Result.Error(fail ExitCode.EnvironmentFailure [ Faults.describe fault ])
        | Ok text ->
            match Config.parse text with
            | Result.Error problems -> Result.Error(fail ExitCode.InvalidArguments problems)
            | Ok config ->
                let root =
                    options.Root
                    |> Option.defaultValue (Path.GetDirectoryName(Path.GetFullPath options.ConfigPath))

                let repositoryFiles =
                    match config.RepositoryFiles with
                    | Some listing ->
                        FileSystem.readBytes aegis root listing
                        |> Result.map (fun bytes -> System.Text.Encoding.UTF8.GetString bytes)
                        |> Result.map (fun text ->
                            text.Split('\n')
                            |> Array.map (fun line -> line.Trim())
                            |> Array.filter (fun line -> line <> "")
                            |> Set.ofArray)
                    | None -> FileSystem.listRepository aegis root

                match repositoryFiles, RepositoryId.create config.Repository with
                | Result.Error fault, _ -> Result.Error(fail ExitCode.EnvironmentFailure [ Faults.describe fault ])
                | _, Result.Error problem -> Result.Error(fail ExitCode.InvalidArguments [ problem ])
                | Ok files, Ok repository ->
                    // Only files that are actually present can be read.
                    match FileSystem.listRepository aegis root with
                    | Result.Error fault -> Result.Error(fail ExitCode.EnvironmentFailure [ Faults.describe fault ])
                    | Ok present ->
                        let select pattern =
                            let matches = Glob.matches pattern
                            present |> Set.filter matches |> Set.toList

                        let included = config.Include |> List.collect select |> List.distinct |> List.sort

                        let frontier =
                            config.Frontier
                            |> Option.map (fun f -> select f.Records |> List.filter (fun p -> not (List.contains p included)))
                            |> Option.defaultValue []

                        let readIncluded, failedIncluded = FileSystem.readSources aegis root included
                        let readFrontier, failedFrontier = FileSystem.readSources aegis root frontier

                        let corpus =
                            Corpus.ingest
                                { Repository = repository
                                  Included = readIncluded
                                  FrontierCandidates = readFrontier
                                  RepositoryFiles = files
                                  Revision = config.Revision }

                        let unreadable =
                            (failedIncluded @ failedFrontier)
                            |> List.map (fun (path, reason) ->
                                { Code = UnreadableSource reason
                                  Subject = None
                                  Location =
                                    RepoPath.create path
                                    |> Result.toOption
                                    |> Option.map (SourceLocation.ofFile repository)
                                  Message = $"{path} could not be read: {reason}."
                                  Remedy = Some "Check the file's permissions and encoding (UTF-8)." })

                        let findings = unreadable @ corpus.Findings

                        Ok(
                            config,
                            root,
                            { corpus with
                                Findings = findings
                                Assessment = Assessment.evaluate findings
                                State =
                                    if unreadable.IsEmpty then corpus.State
                                    else Assessed(Assessment.evaluate findings) }
                        )

    let load (aegis: AegisConfig) (options: IngestOptions) : Result<Corpus, CommandResult> =
        loadWithConfig aegis options |> Result.map (fun (_, _, corpus) -> corpus)

    /// Previously published URLs from a legacy catalog: (path under the old
    /// base path, repository source path). Read errors are faults; a catalog
    /// that is not the expected shape yields no URLs and a message.
    let private legacyUrls (aegis: AegisConfig) (root: string) (site: SiteConfig) : Result<(string * string) list, string> =
        match site.LegacyCatalog with
        | None -> Ok []
        | Some catalog ->
            match FileSystem.readBytes aegis root catalog with
            | Result.Error fault -> Result.Error(Faults.describe fault)
            | Ok bytes ->
                try
                    use document = System.Text.Json.JsonDocument.Parse(bytes: byte array)
                    let basePath = site.LegacyBasePath |> Option.defaultValue "/"

                    document.RootElement.GetProperty("records").EnumerateArray()
                    |> Seq.choose (fun record ->
                        match record.TryGetProperty "url", record.TryGetProperty "sourcePath" with
                        | (true, url), (true, source) when url.ValueKind = System.Text.Json.JsonValueKind.String && source.ValueKind = System.Text.Json.JsonValueKind.String ->
                            let value = url.GetString()
                            let relativeUrl = if value.StartsWith basePath then value.Substring basePath.Length else value.TrimStart('/')
                            Some(relativeUrl, source.GetString())
                        | _ -> None)
                    |> List.ofSeq
                    |> Ok
                with
                | :? System.Text.Json.JsonException
                | :? System.Collections.Generic.KeyNotFoundException
                | :? InvalidOperationException -> Result.Error $"legacy catalog {catalog} is not a research-catalog.json with records[].url/sourcePath"

    let private siteOptions (config: TekmerionConfig) (legacy: (string * string) list) (forma: bool) : SiteOptions =
        { Title = config.Site.Title |> Option.defaultValue config.Repository
          FormaStylesheet = if forma then Some "assets/forma/all.css" else None
          SourceLink =
            fun location ->
                match config.Site.SourceUrl, location.Commit with
                | Some template, Some revision ->
                    Some(
                        template
                            .Replace("{revision}", revision)
                            .Replace("{path}", RepoPath.value location.Path)
                            .Replace("{line}", location.Line |> Option.map string |> Option.defaultValue "1")
                    )
                | _ -> None
          LegacyUrls = legacy }

    let validate (aegis: AegisConfig) (options: IngestOptions) : CommandResult =
        match load aegis options with
        | Result.Error result -> result
        | Ok corpus ->
            { ExitCode =
                match Assessment.permitsPublication corpus.Assessment with
                | Some _ -> ExitCode.Success
                | None -> ExitCode.Blocked
              Corpus = Some corpus
              Published = None
              Messages = [] }

    let private apply state event =
        match Publication.apply state event with
        | Ok next -> next
        | Result.Error refusal -> failwithf "illegal publication transition: %A" refusal

    let ingest (aegis: AegisConfig) (options: IngestOptions) : CommandResult =
        match loadWithConfig aegis options, options.Out with
        | Result.Error result, _ -> result
        | Ok _, None -> fail ExitCode.InvalidArguments [ "--out is required" ]
        | Ok(config, root, corpus), Some out ->
            let blocked (corpus: Corpus) messages =
                { ExitCode = ExitCode.Blocked
                  Corpus = Some corpus
                  Published = None
                  Messages = messages }

            let formaCss =
                match options.Forma with
                | None -> Ok None
                | Some path ->
                    FileSystem.readBytes aegis (Directory.GetCurrentDirectory()) path
                    |> Result.map (fun bytes -> Some(System.Text.Encoding.UTF8.GetString bytes))
                    |> Result.mapError Faults.describe

            match legacyUrls aegis root config.Site, formaCss with
            | Result.Error message, _
            | _, Result.Error message -> { (fail ExitCode.EnvironmentFailure [ message ]) with Corpus = Some corpus }
            | Ok legacy, Ok css ->
                let pages, lost = Site.render corpus (siteOptions config legacy css.IsSome)

                // A lost published URL is a blocking obligation (TEK-IDY-006).
                let corpus =
                    if lost.IsEmpty then corpus
                    else
                        let findings = lost @ corpus.Findings
                        { corpus with Findings = findings; Assessment = Assessment.evaluate findings; State = Assessed(Assessment.evaluate findings) }

                match Assessment.permitsPublication corpus.Assessment with
                | None -> blocked corpus [ "Publication blocked; the previous publication is unchanged." ]
                | Some _ ->
                    let assets =
                        match css with
                        | Some text -> [ ({ Path = "assets/forma/all.css"; Text = text }: OutputFile) ]
                        | None -> []

                    let files = Contracts.project corpus @ pages @ assets

                    match Output.escapingPaths files with
                    | escaping when not escaping.IsEmpty ->
                        blocked corpus (escaping |> List.map (fun p -> $"output path escapes the output root: {p}"))
                    | _ ->
                        let digest = Output.digest files
                        let projected = apply corpus.State (OutputProjected digest)

                        match Output.recover aegis out with
                        | Result.Error fault -> { (fail ExitCode.EnvironmentFailure [ Faults.describe fault ]) with Corpus = Some corpus }
                        | Ok recovered ->
                            match Output.stage aegis out files with
                            | Result.Error fault ->
                                apply projected (StageFailed(DuringStaging, Faults.describe fault)) |> ignore
                                { (fail ExitCode.EnvironmentFailure [ Faults.describe fault; "The previous publication is unchanged." ]) with Corpus = Some corpus }
                            | Ok bytes ->
                                let staged = apply projected (OutputStaged digest)

                                match Output.promote aegis out with
                                | Result.Error fault ->
                                    apply staged (StagingPromoted(Failed(Faults.describe fault))) |> ignore
                                    { (fail ExitCode.EnvironmentFailure [ Faults.describe fault; "Run again to restore the previous publication." ]) with Corpus = Some corpus }
                                | Ok() ->
                                    let promoted = apply staged (StagingPromoted(Succeeded digest))

                                    { ExitCode = ExitCode.Success
                                      Corpus = Some { corpus with State = promoted }
                                      Published =
                                        Some
                                            { Digest = digest
                                              Files = files.Length
                                              Bytes = bytes
                                              Recovered = recovered }
                                      Messages = [] }
