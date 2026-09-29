namespace Tekmerion.Cli

open System
open System.Text.Json

/// `tekmerion.config.json` (TEK-IDN-003, TEK-ING-001). Small, explicit and
/// versioned; nothing in it duplicates what research already declares.
type FrontierConfig = { Records: string; Scope: string }

/// Presentation settings for the static site (GH-18). All optional.
type SiteConfig =
    { Title: string option
      /// Link template for source files: `{path}`, `{line}` and `{revision}`
      /// are substituted. Links are only emitted when a revision is known, so
      /// a link never points at a moving branch.
      SourceUrl: string option
      /// A previously published catalog (legacy `research-catalog.json`)
      /// whose `records[].url`/`sourcePath` pairs are the URLs to preserve.
      LegacyCatalog: string option
      /// Base path the legacy URLs were published under, e.g. `/Visual-Engineering/`.
      LegacyBasePath: string option }

type TekmerionConfig =
    { SchemaVersion: int
      Repository: string
      Include: string list
      Frontier: FrontierConfig option
      /// Optional file listing repository paths (one per line). When absent
      /// the host lists the repository directory itself.
      RepositoryFiles: string option
      /// Source commit of the research repository, when known.
      Revision: string option
      Site: SiteConfig }

module Config =

    let private text (element: JsonElement) (name: string) =
        match element.TryGetProperty name with
        | true, value when value.ValueKind = JsonValueKind.String -> Some(value.GetString())
        | _ -> None

    /// Parse configuration text. Problems are returned, not thrown.
    let parse (json: string) : Result<TekmerionConfig, string list> =
        try
            use document = JsonDocument.Parse json
            let root = document.RootElement

            let schemaVersion =
                match root.TryGetProperty "schemaVersion" with
                | true, value when value.ValueKind = JsonValueKind.Number -> Some(value.GetInt32())
                | _ -> None

            let includes =
                match root.TryGetProperty "include" with
                | true, value when value.ValueKind = JsonValueKind.Array ->
                    value.EnumerateArray()
                    |> Seq.choose (fun item -> if item.ValueKind = JsonValueKind.String then Some(item.GetString()) else None)
                    |> List.ofSeq
                | _ -> []

            let frontier =
                match root.TryGetProperty "frontier" with
                | true, value when value.ValueKind = JsonValueKind.Object ->
                    match text value "records", text value "scope" with
                    | Some records, Some scope -> Some { Records = records; Scope = scope }
                    | Some records, None -> Some { Records = records; Scope = "origin-in-scope" }
                    | _ -> None
                | _ -> None

            let problems =
                [ if schemaVersion <> Some 1 then "schemaVersion must be 1"
                  if (text root "repository").IsNone then "repository is required (owner/name)"
                  if includes.IsEmpty then "include must list at least one glob"
                  match frontier with
                  | Some f when f.Scope <> "origin-in-scope" -> $"frontier.scope '{f.Scope}' is not supported (use origin-in-scope)"
                  | _ -> () ]

            if problems.IsEmpty then
                Ok
                    { SchemaVersion = 1
                      Repository = (text root "repository").Value
                      Include = includes
                      Frontier = frontier
                      RepositoryFiles = text root "repositoryFiles"
                      Revision = text root "revision"
                      Site =
                        match root.TryGetProperty "site" with
                        | true, site when site.ValueKind = JsonValueKind.Object ->
                            { Title = text site "title"
                              SourceUrl = text site "sourceUrl"
                              LegacyCatalog = text site "legacyCatalog"
                              LegacyBasePath = text site "legacyBasePath" }
                        | _ ->
                            { Title = None
                              SourceUrl = None
                              LegacyCatalog = None
                              LegacyBasePath = None } }
            else
                Error problems
        with :? JsonException as error ->
            Error [ $"configuration is not valid JSON: {error.Message}" ]
