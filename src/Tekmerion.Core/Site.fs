namespace Tekmerion.Core

open System
open System.IO
open Markdig
open Markdig.Syntax
open Markdig.Syntax.Inlines
open Markdig.Renderers
open Markdig.Renderers.Html
open Tekmerion.Domain
open Tekmerion.Core.Html

type SiteOptions =
    { Title: string
      /// Output-relative path of the Forma stylesheet, when the host supplies
      /// one. Without it pages remain readable semantic HTML (TEK-ECH-003).
      FormaStylesheet: string option
      /// Builds a link to a source location, when a pinned revision is known.
      SourceLink: SourceLocation -> string option
      /// Previously published URL paths (relative to the old base path) and
      /// the repository path each one published (TEK-IDY-006).
      LegacyUrls: (string * string) list }

/// Static, semantic, script-free research experience (GH-18, TEK-EXP-001).
/// Every page is an ordinary HTML document: navigation is links, so browser
/// back/forward, bookmarking and sharing work natively with JavaScript off.
module Site =

    // ------------------------------------------------------------------
    // Addresses
    // ------------------------------------------------------------------

    let pageOf (key: ArtifactKey) = (ArtifactKey.url key).TrimStart('/') + "index.html"
    let dirOf (key: ArtifactKey) = (ArtifactKey.url key).TrimStart('/')
    let projectPage (project: string) = $"p/{project}/index.html"
    let integrityPage = "integrity/index.html"

    type private Context =
        { Corpus: Corpus
          Options: SiteOptions
          ByKey: Map<ArtifactKey, IngestedArtifact>
          ByPath: Map<string, ArtifactKey> }

    let private artifactOf (ctx: Context) key = ctx.ByKey.TryFind key |> Option.map (fun a -> a.Reading.Artifact)

    /// A human label for an artifact. Uses the declared title, else the
    /// declared id, else the file name — each shown as what it is, never
    /// promoted to a title.
    let label (artifact: Artifact) =
        match artifact.Title, artifact.Key with
        | Known title, _ -> title
        | Absent _, Declared id -> ArtifactId.value id
        | Absent _, PathDerived _ -> Path.GetFileName(RepoPath.value artifact.Location.Path)

    let private linkTo (ctx: Context) (fromPage: string) (key: ArtifactKey) =
        match artifactOf ctx key with
        | Some artifact -> a (relative fromPage (dirOf key)) [ text (label artifact) ]
        | None -> text (ArtifactKey.value key)

    let private where (location: SourceLocation) =
        let line = location.Line |> Option.map (sprintf ":%d") |> Option.defaultValue ""
        let key = location.FrontMatterKey |> Option.map (fun k -> $" (front-matter key {k})") |> Option.defaultValue ""
        let heading =
            match location.HeadingPath with
            | [] -> ""
            | path -> $""" (section "{String.Join(" › ", path)}")"""

        $"{RepoPath.value location.Path}{line}{key}{heading}"

    let private sourceRef (ctx: Context) (location: SourceLocation) =
        match ctx.Options.SourceLink location with
        | Some href -> el "a" [ "href", href; "rel", "noopener noreferrer" ] [ code (where location) ]
        | None -> code (where location)

    // ------------------------------------------------------------------
    // Markdown bodies: raw HTML disabled, links rewritten or neutralised
    // ------------------------------------------------------------------

    let private pipeline =
        MarkdownPipelineBuilder().DisableHtml().UsePipeTables().UseEmphasisExtras().Build()

    let private safeExternal (url: string) =
        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)

    /// Render one section body. Links to published artifacts become site
    /// links; other relative links and images become plain text (their
    /// targets are not published); only http(s)/mailto links survive as
    /// external links, with no opener privileges (TEK-SEC-001/003).
    let private renderBody (ctx: Context) (fromPage: string) (declaringFile: RepoPath) (markdown: string) =
        let document = Markdown.Parse(markdown, pipeline)

        for link in document.Descendants<LinkInline>() |> Seq.toList do
            let url = if isNull link.Url then "" else link.Url.Trim()

            let target =
                if link.IsImage || url.StartsWith "#" then None
                elif safeExternal url then Some(Choice1Of2 url)
                else
                    References.candidatePaths declaringFile (References.classify url)
                    |> List.tryPick (fun p -> ctx.ByPath.TryFind(RepoPath.value p))
                    |> Option.map (fun key -> Choice2Of2 key)

            match target with
            | Some(Choice1Of2 external) ->
                link.Url <- external
                link.GetAttributes().AddPropertyIfNotExist("rel", "noopener noreferrer")
            | Some(Choice2Of2 key) -> link.Url <- relative fromPage (dirOf key)
            | None when url.StartsWith "#" -> ()
            | None ->
                // Replace the link by its text so nothing points at an
                // unpublished or unsafe target.
                let replacement = LiteralInline(if link.IsImage then $"[image: {url}]" else "")
                link.ReplaceBy(replacement) |> ignore

                if not link.IsImage then
                    for child in link |> Seq.toList do
                        child.Remove()
                        replacement.InsertBefore(child)

        use writer = new StringWriter()
        let renderer = HtmlRenderer(writer)
        pipeline.Setup(renderer)
        renderer.Render(document) |> ignore
        writer.Flush()
        writer.ToString()

    // ------------------------------------------------------------------
    // Layout
    // ------------------------------------------------------------------

    let private layout (ctx: Context) (page: string) (title: string) (main: Html list) =
        let stylesheet =
            match ctx.Options.FormaStylesheet with
            | Some css -> VoidElement("link", [ "rel", "stylesheet"; "href", relative page css ])
            | None -> empty

        el "html" [ "lang", "en" ] [
            el "head" [] [
                VoidElement("meta", [ "charset", "utf-8" ])
                VoidElement("meta", [ "name", "viewport"; "content", "width=device-width, initial-scale=1" ])
                el "title" [] [ text $"{title} · {ctx.Options.Title}" ]
                stylesheet
                VoidElement("link", [ "rel", "alternate"; "type", "application/json"; "href", relative page "data/v1/manifest.json" ])
            ]
            el "body" [] [
                el "a" [ "class", "ef-visually-hidden"; "href", "#main" ] [ text "Skip to content" ]
                el "header" [] [
                    el "nav" [ "aria-label", "Site"; "class", "ef-cluster" ] [
                        a (relative page "index.html") [ text ctx.Options.Title ]
                        a (relative page integrityPage) [ text "Integrity" ]
                        a (relative page "data/v1/manifest.json") [ text "Machine-readable data" ]
                    ]
                ]
                el "main" [ "id", "main"; "class", "ef-stack" ] main
                el "footer" [] [
                    p [
                        text $"Generated by Tekmerion {Version.Tekmerion} from "
                        code (RepositoryId.value ctx.Corpus.Repository)
                        text ". Every relationship shown is either declared by the research (canonical) or derived from a declared one; derived relationships say how."
                    ]
                ]
            ]
        ]
        |> Html.render

    // ------------------------------------------------------------------
    // Shared fragments
    // ------------------------------------------------------------------

    let private knownOr (fallback: string) (value: Knowable<string>) =
        match value with
        | Known v -> text v
        | Absent Absence.NotDeclared -> el "em" [] [ text fallback ]
        | Absent _ -> el "em" [] [ text "unknown" ]

    let private statusState (reading: StatusReading) =
        match reading.Class with
        | Complete
        | Verified -> "ok"
        | Superseded -> "blocked"
        | Draft
        | Active
        | Open -> "attention"
        | Unclassified -> "unknown"

    let private statusLabel (cls: StatusClass) =
        match cls with
        | Draft -> "draft"
        | Active -> "active"
        | Complete -> "complete"
        | Verified -> "verified"
        | Superseded -> "superseded"
        | Open -> "open"
        | Unclassified -> "unclassified"

    let private statusCell (status: Knowable<StatusReading>) =
        match status with
        | Known reading ->
            Fragment [ lozenge (statusState reading) reading.Text; text " "; el "small" [] [ text $"(read as {statusLabel reading.Class})" ] ]
        | Absent _ -> el "em" [] [ text "no status declared" ]

    let private typeCell (artifact: Artifact) =
        match artifact.TypeSource with
        | DeclaredType(key, raw) -> Fragment [ text (ArtifactType.label artifact.Type); text " "; el "small" [] [ text $"(declared: {key}: {raw})" ] ]
        | InferredFromDirectory segment -> Fragment [ text (ArtifactType.label artifact.Type); text " "; el "small" [] [ text $"(inferred from directory “{segment}”; not declared)" ] ]
        | InferredFromIdPrefix prefix -> Fragment [ text (ArtifactType.label artifact.Type); text " "; el "small" [] [ text $"(inferred from id prefix {prefix})" ] ]
        | UnknownType -> el "em" [] [ text "no type declared or inferable" ]

    let private relationLabel (relation: CanonicalRelation) =
        match relation with
        | SourceRep -> "Produced by research package (source_rep)"
        | RelatedDocument -> "Related document"
        | Supersedes -> "Supersedes"
        | SupersededBy -> "Superseded by"
        | OriginDocument -> "Originated from"
        | Prerequisite -> "Depends on (prerequisite)"
        | TypedLink field -> $"Typed link ({field})"

    let private inverseLabel (relation: CanonicalRelation) =
        match relation with
        | SourceRep -> "Research produced from this package"
        | RelatedDocument -> "Lists this as related"
        | Supersedes -> "Superseded by"
        | SupersededBy -> "Supersedes"
        | OriginDocument -> "Frontier records originating here"
        | Prerequisite -> "Required by"
        | TypedLink field -> $"Refers here ({field})"

    let private referenceText (value: ReferenceValue) =
        match value with
        | RepoRelativePath t
        | FileRelativePath t
        | IdReference t
        | ProseTitle t
        | ExternalUrl t
        | Unparsed t -> t

    let private severityData (severity: Severity) =
        match severity with
        | Blocking -> "blocking"
        | Warning -> "warning"
        | Informational -> "information"

    let private findingItem (ctx: Context) (page: string) (finding: ValidationFinding) =
        let severity = Policy.severity finding.Code

        el "li" [ "class", "ef-obligation"; "data-ef-severity", severityData severity ] [
            el "span" [ "class", "ef-obligation__status" ] [
                text (match severity with Blocking -> "Blocking" | Warning -> "Warning" | Informational -> "Note")
            ]
            el "div" [] [
                el "strong" [] [ text (Policy.code finding.Code) ]
                p [ text finding.Message ]
                (match finding.Remedy with Some remedy -> p [ el "small" [] [ text remedy ] ] | None -> empty)
                (match finding.Location with Some location -> p [ el "small" [] [ text "At "; sourceRef ctx location ] ] | None -> empty)
            ]
            (match finding.Subject with
             | Some key when ctx.ByKey.ContainsKey key -> a (relative page (dirOf key)) [ text "Open artifact" ]
             | _ -> empty)
        ]

    // ------------------------------------------------------------------
    // Artifact page (TEK-EXP-005)
    // ------------------------------------------------------------------

    let private canonicalItem (ctx: Context) (page: string) (edge: CanonicalEdge) =
        let target =
            match edge.Target with
            | Resolves key -> linkTo ctx page key
            | OutOfScope(value, path) ->
                Fragment [ code (referenceText value); text " "; lozenge "unknown" "in repository, not published here"; text " "; code (RepoPath.value path) ]
            | Dangling value -> Fragment [ code (referenceText value); text " "; lozenge "attention" "unresolved" ]
            | NotALink value -> Fragment [ text value; text " "; el "small" [] [ text "(prose, not a link)" ] ]

        li [ el "strong" [] [ text (relationLabel edge.Relation) ]; text ": "; target; text " "; el "small" [] [ text "declared at "; sourceRef ctx edge.Declared ] ]

    /// The answer to "why is Tekmerion showing me this relationship?"
    /// (TEK-REL-003): the rule, and the canonical declaration it came from,
    /// with a link to the declaring artifact and its exact source location.
    let private derivedItem (ctx: Context) (page: string) (edge: DerivedEdge) =
        let basis = NonEmpty.toList edge.Basis

        li [
            linkTo ctx page edge.To
            text " "
            disclosure "Why is this shown?" [
                p [ text "Tekmerion derived this link; it is not declared on this page's artifact. Rule: "; code edge.Rule; text "." ]
                ul (
                    basis
                    |> List.map (fun canonical ->
                        li [
                            linkTo ctx page canonical.From
                            text $" declares {CanonicalRelation.label canonical.Relation} "
                            code (referenceText canonical.Reference)
                            text " at "
                            sourceRef ctx canonical.Declared
                        ])
                )
            ]
        ]

    let private artifactPage (ctx: Context) (ingested: IngestedArtifact) =
        let artifact = ingested.Reading.Artifact
        let page = pageOf artifact.Key
        let outgoing = ctx.Corpus.Canonical |> List.filter (fun e -> e.From = artifact.Key)
        let derived = ctx.Corpus.Derived |> List.filter (fun e -> e.From = artifact.Key)

        let findings =
            ctx.Corpus.Findings
            |> List.filter (fun f -> f.Subject = Some artifact.Key && (match f.Code with UnknownKey _ -> false | _ -> true))

        let identity =
            match artifact.Key with
            | Declared id -> Fragment [ code (ArtifactId.value id); text " "; el "small" [] [ text "(declared id)" ] ]
            | PathDerived key ->
                Fragment [
                    code (PathKey.value key)
                    text " "
                    el "small" [] [
                        text "(no id declared; stable key derived from the source path"
                        (match artifact.DeclaredIdText with Known raw -> text $"; declared id “{raw}” is not usable as an address" | Absent _ -> empty)
                        text ")"
                    ]
                ]

        let project =
            match artifact.Project with
            | Known project -> a (relative page (projectPage project)) [ text project ]
            | Absent _ -> el "em" [] [ text "not declared" ]

        let header =
            el "header" [ "class", "ef-record-header" ] [
                el "div" [ "class", "ef-record-header__main" ] [
                    el "nav" [ "aria-label", "Breadcrumb" ] [
                        a (relative page "index.html") [ text "Home" ]
                        el "span" [ "aria-hidden", "true" ] [ text " / " ]
                        (match artifact.Project with
                         | Known project -> Fragment [ a (relative page (projectPage project)) [ text project ]; el "span" [ "aria-hidden", "true" ] [ text " / " ] ]
                         | Absent _ -> empty)
                        text (match ingested.Population with MachineGenerated -> "Frontier record" | AuthoredResearch -> "Artifact")
                    ]
                    el "div" [ "class", "ef-record-header__title" ] [
                        el "div" [] [
                            el "span" [ "class", "ef-record-header__type" ] [ text (match artifact.TypeSource with UnknownType -> "Untyped document" | _ -> ArtifactType.label artifact.Type) ]
                            h 1 [ text (label artifact) ]
                        ]
                        statusCell artifact.Status
                    ]
                    (match artifact.Title with
                     | Absent _ -> p [ el "em" [] [ text "No title is declared; the heading above is the id or file name." ] ]
                     | Known _ -> empty)
                    (match artifact.Summary with Known summary -> p [ text summary ] | Absent _ -> empty)
                ]
            ]

        let metadata =
            section [ "aria-labelledby", "identity" ] [
                el "h2" [ "id", "identity" ] [ text "Identity and declared metadata" ]
                keyValues [
                    "Identity", identity
                    "Type", typeCell artifact
                    "Status", statusCell artifact.Status
                    "Project", project
                    "Date", knownOr "not declared" artifact.Date
                    "Created", knownOr "not declared" artifact.Created
                    "Updated", knownOr "not declared" artifact.Updated
                    "Purposes", (if artifact.Purposes.IsEmpty then el "em" [] [ text "not declared" ] else text (String.Join(", ", artifact.Purposes)))
                    "Audiences", (if artifact.Audiences.IsEmpty then el "em" [] [ text "not declared" ] else text (String.Join(", ", artifact.Audiences)))
                ]
            ]

        let provenance =
            section [ "class", "ef-provenance-trail"; "aria-labelledby", "provenance" ] [
                el "h2" [ "id", "provenance" ] [ text "Source and provenance" ]
                el "ol" [] [
                    li [ el "span" [ "class", "ef-provenance-trail__kind" ] [ text "Source" ]; el "strong" [] [ sourceRef ctx artifact.Location ]; el "small" [] [ text (RepositoryId.value artifact.Location.Repository + (match artifact.Location.Commit with Some c -> $" @ {c}" | None -> " (revision not recorded)")) ] ]
                    li [ el "span" [ "class", "ef-provenance-trail__kind" ] [ text "Read" ]; el "strong" [] [ text $"Parser profile {Version.ParserProfile}" ]; el "small" [] [ text (match ingested.Reading.FrontMatter with FrontMatterPresent -> "front matter present" | FrontMatterAbsent -> "no front matter" | FrontMatterMalformed reason -> $"front matter malformed: {reason}") ] ]
                    li [ el "span" [ "class", "ef-provenance-trail__kind" ] [ text "Published" ]; el "strong" [] [ text $"Tekmerion {Version.Tekmerion}" ]; el "small" [] [ a (relative page (Contracts.artifactPath artifact.Key)) [ text "artifact data" ]; text " · "; a (relative page (Contracts.neighbourhoodPath artifact.Key)) [ text "relationship data" ] ] ]
                ]
            ]

        let relationships =
            section [ "aria-labelledby", "relationships" ] [
                el "h2" [ "id", "relationships" ] [ text "Relationships" ]
                el "h3" [] [ text "Declared by this artifact (canonical)" ]
                (if outgoing.IsEmpty then p [ el "em" [] [ text "This artifact declares no relationships." ] ]
                 else ul (outgoing |> List.map (canonicalItem ctx page)))
                el "h3" [] [ text "Pointing here (derived for navigation)" ]
                (if derived.IsEmpty then p [ el "em" [] [ text "No other published artifact declares a relationship to this one." ] ]
                 else
                     Fragment(
                         derived
                         |> List.groupBy (fun e -> match e.Relation with Backlink r -> inverseLabel r | SupersessionChain -> "Supersession chain")
                         |> List.map (fun (heading, edges) -> Fragment [ el "h4" [] [ text heading ]; ul (edges |> List.map (derivedItem ctx page)) ])
                     ))
            ]

        let extensions =
            if artifact.Extensions.IsEmpty then empty
            else
                section [ "aria-labelledby", "extensions" ] [
                    el "h2" [ "id", "extensions" ] [ text "Other declared metadata" ]
                    p [ text "Front-matter keys Tekmerion does not interpret, preserved exactly as written." ]
                    disclosure $"{artifact.Extensions.Length} preserved keys" [
                        el "dl" [] (artifact.Extensions |> List.collect (fun ext -> [ el "dt" [] [ code ext.Key; text " "; el "small" [] [ text (where ext.Location) ] ]; el "dd" [] [ verbatim ext.RawValue ] ]))
                    ]
                ]

        let validation =
            if findings.IsEmpty then empty
            else
                el "aside" [ "class", "ef-obligation-panel"; "aria-labelledby", "findings" ] [
                    el "header" [ "class", "ef-obligation-panel__header" ] [ el "div" [] [ el "h2" [ "id", "findings" ] [ text "Validation notes" ]; p [ text "Legitimate research states are notes, not defects. Only blocking findings stop publication." ] ] ]
                    el "ul" [ "class", "ef-obligation-list" ] (findings |> List.map (findingItem ctx page))
                ]

        let content =
            section [ "aria-labelledby", "content" ] [
                el "h2" [ "id", "content" ] [ text "Content" ]
                Fragment(
                    artifact.Sections
                    |> List.map (fun s ->
                        let depth = min 6 (max 3 (s.Depth + 2))

                        Fragment [
                            (if s.Heading = "" then empty else el $"h{depth}" [ "id", "s-" + s.Slug ] [ text s.Heading ])
                            Trusted(renderBody ctx page artifact.Location.Path s.Body)
                        ])
                )
            ]

        { Path = page
          Text = layout ctx page (label artifact) [ header; metadata; provenance; relationships; validation; extensions; content ] }

    // ------------------------------------------------------------------
    // Project page (TEK-EXP-004)
    // ------------------------------------------------------------------

    let private extensionText (artifact: Artifact) (key: string) =
        artifact.Extensions
        |> List.tryFind (fun e -> e.Key = key)
        |> Option.map (fun e -> e.RawValue.Substring(e.RawValue.IndexOf(':') + 1).Trim().Trim('"'))

    let private projectPageFor (ctx: Context) (project: string) (members: IngestedArtifact list) =
        let page = projectPage project
        let authored = members |> List.filter (fun a -> a.Population = AuthoredResearch) |> List.map (fun a -> a.Reading.Artifact)
        let keys = members |> List.map (fun a -> a.Reading.Artifact.Key) |> set

        let frontier =
            ctx.Corpus.Canonical
            |> List.filter (fun e -> e.Relation = OriginDocument)
            |> List.choose (fun e -> match e.Target with Resolves origin when keys.Contains origin -> Some(e.From, origin) | _ -> None)
            |> List.choose (fun (record, origin) -> artifactOf ctx record |> Option.map (fun r -> r, origin))
            |> List.sortBy (fun (r, _) -> ArtifactKey.value r.Key)

        let openFrontier =
            frontier |> List.filter (fun (r, _) -> match r.Status with Known s -> s.Class = Open | Absent _ -> false)

        let entryPoints =
            authored
            |> List.filter (fun a -> extensionText a "entryPoint" = Some "true")
            |> List.sortBy (fun a -> extensionText a "entryPointOrder" |> Option.bind (fun o -> match Int32.TryParse o with true, n -> Some n | _ -> None) |> Option.defaultValue Int32.MaxValue, label a)

        let purpose =
            section [ "aria-labelledby", "purpose" ] [
                el "h2" [ "id", "purpose" ] [ text "Purpose and where to start" ]
                (if entryPoints.IsEmpty then p [ el "em" [] [ text "No entry point is declared for this project (entryPoint: true), so Tekmerion does not choose one." ] ]
                 else
                     ul (
                         entryPoints
                         |> List.map (fun e ->
                             li [
                                 linkTo ctx page e.Key
                                 (match extensionText e "entryPointLabel" with Some l -> Fragment [ text " — "; text l ] | None -> empty)
                                 (match e.Summary with Known s -> p [ text s ] | Absent _ -> empty)
                             ])
                     ))
            ]

        let openSection =
            section [ "aria-labelledby", "frontier" ] [
                el "h2" [ "id", "frontier" ] [ text $"Open frontier ({openFrontier.Length} of {frontier.Length} frontier records open)" ]
                p [ text "Frontier records are machine-generated research opportunities; each names the document it originated from." ]
                (if openFrontier.IsEmpty then p [ el "em" [] [ text "No open frontier records in this project." ] ]
                 else
                     el "div" [ "class", "ef-data-grid"; "role", "region"; "tabindex", "0"; "aria-label", "Open frontier records" ] [
                         el "table" [] [
                             el "caption" [ "class", "ef-visually-hidden" ] [ text "Open frontier records" ]
                             el "thead" [] [ el "tr" [] [ el "th" [ "scope", "col" ] [ text "Record" ]; el "th" [ "scope", "col" ] [ text "Originated from" ]; el "th" [ "scope", "col" ] [ text "Status" ] ] ]
                             el "tbody" [] (
                                 openFrontier
                                 |> List.map (fun (record, origin) ->
                                     el "tr" [] [
                                         el "td" [ "data-label", "Record" ] [ linkTo ctx page record.Key ]
                                         el "td" [ "data-label", "Originated from" ] [ linkTo ctx page origin ]
                                         el "td" [ "data-label", "Status" ] [ statusCell record.Status ]
                                     ])
                             )
                         ]
                     ])
            ]

        let position =
            section [ "aria-labelledby", "position" ] [
                el "h2" [ "id", "position" ] [ text "Current research position" ]
                p [ text "Each artifact's status as written, with Tekmerion's reading of it for grouping. Dates are shown only where declared." ]
                el "div" [ "class", "ef-data-grid"; "role", "region"; "tabindex", "0"; "aria-label", "Authored research" ] [
                    el "table" [] [
                        el "caption" [ "class", "ef-visually-hidden" ] [ text "Authored research in this project" ]
                        el "thead" [] [ el "tr" [] [ el "th" [ "scope", "col" ] [ text "Artifact" ]; el "th" [ "scope", "col" ] [ text "Type" ]; el "th" [ "scope", "col" ] [ text "Status" ]; el "th" [ "scope", "col" ] [ text "Date" ] ] ]
                        el "tbody" [] (
                            authored
                            |> List.sortBy (fun a -> ArtifactType.label a.Type, label a)
                            |> List.map (fun artifact ->
                                el "tr" [] [
                                    el "td" [ "data-label", "Artifact" ] [ linkTo ctx page artifact.Key ]
                                    el "td" [ "data-label", "Type" ] [ typeCell artifact ]
                                    el "td" [ "data-label", "Status" ] [ statusCell artifact.Status ]
                                    el "td" [ "data-label", "Date" ] [ knownOr "not declared" artifact.Date ]
                                ])
                        )
                    ]
                ]
            ]

        let inFlight =
            let pending =
                authored |> List.choose (fun a -> match a.Status with Known s when not s.Outstanding.IsEmpty -> Some(a, s) | _ -> None)

            let warnings =
                ctx.Corpus.Findings
                |> List.filter (fun f -> Policy.severity f.Code <> Informational)
                |> List.filter (fun f -> match f.Subject with Some key -> keys.Contains key | None -> false)

            el "aside" [ "class", "ef-obligation-panel"; "aria-labelledby", "in-flight" ] [
                el "header" [ "class", "ef-obligation-panel__header" ] [
                    el "div" [] [ el "h2" [ "id", "in-flight" ] [ text "In-flight work and obligations" ]; p [ text "Outstanding work named in declared statuses, and validation warnings for this project." ] ]
                    el "span" [ "class", "ef-obligation-panel__count"; "aria-hidden", "true" ] [ text (string (pending.Length + warnings.Length)) ]
                ]
                el "ul" [ "class", "ef-obligation-list" ] (
                    (pending
                     |> List.map (fun (artifact, status) ->
                         el "li" [ "class", "ef-obligation"; "data-ef-severity", "information" ] [
                             el "span" [ "class", "ef-obligation__status" ] [ text "Outstanding" ]
                             el "div" [] [ el "strong" [] [ text (String.Join(", ", status.Outstanding)) ]; p [ text "Declared in the status of "; linkTo ctx page artifact.Key; text $": “{status.Text}”." ] ]
                         ]))
                    @ (warnings |> List.map (findingItem ctx page))
                )
                (if pending.IsEmpty && warnings.IsEmpty then p [ el "em" [] [ text "Nothing outstanding is declared." ] ] else empty)
            ]

        let integrity =
            let count severity = ctx.Corpus.Findings |> List.filter (fun f -> Policy.severity f.Code = severity && (match f.Subject with Some k -> keys.Contains k | None -> false)) |> List.length

            section [ "aria-labelledby", "integrity" ] [
                el "h2" [ "id", "integrity" ] [ text "Integrity" ]
                keyValues [
                    "Artifacts", text $"{authored.Length} authored, {frontier.Length} frontier records (counted separately)"
                    "Declared relationships", text (string (ctx.Corpus.Canonical |> List.filter (fun e -> keys.Contains e.From) |> List.length))
                    "Findings", text $"{count Blocking} blocking, {count Warning} warnings, {count Informational} notes"
                ]
                p [ a (relative page integrityPage) [ text "All validation findings" ] ]
            ]

        { Path = page
          Text = layout ctx page project [ el "header" [] [ h 1 [ text project ]; p [ text "Research project overview built only from declared research." ] ]; purpose; openSection; position; inFlight; integrity ] }

    // ------------------------------------------------------------------
    // Landing, integrity, legacy redirects, 404
    // ------------------------------------------------------------------

    let private landing (ctx: Context) (projects: (string * IngestedArtifact list) list) (unassigned: IngestedArtifact list) =
        let page = "index.html"

        { Path = page
          Text =
            layout ctx page ctx.Options.Title [
                el "header" [] [ h 1 [ text ctx.Options.Title ]; p [ text "Research published from "; code (RepositoryId.value ctx.Corpus.Repository); text " by Tekmerion." ] ]
                section [ "aria-labelledby", "projects" ] [
                    el "h2" [ "id", "projects" ] [ text "Projects" ]
                    ul (projects |> List.map (fun (project, members) -> li [ a (relative page (projectPage project)) [ text project ]; text $" — {members.Length} artifacts" ]))
                ]
                (if unassigned.IsEmpty then empty
                 else
                     section [ "aria-labelledby", "unassigned" ] [
                         el "h2" [ "id", "unassigned" ] [ text "Artifacts with no declared project" ]
                         ul (unassigned |> List.filter (fun a -> a.Population = AuthoredResearch) |> List.map (fun a -> li [ linkTo ctx page a.Reading.Artifact.Key ]))
                     ])
            ] }

    let private integrityPageFor (ctx: Context) =
        let page = integrityPage
        let groups = ctx.Corpus.Findings |> List.groupBy (fun f -> Policy.severity f.Code)

        let block severity heading =
            match groups |> List.tryFind (fun (s, _) -> s = severity) with
            | None -> section [] [ h 2 [ text heading ]; p [ el "em" [] [ text "None." ] ] ]
            | Some(_, findings) when severity = Informational ->
                section [] [
                    h 2 [ text $"{heading} ({findings.Length})" ]
                    disclosure "Show notes" [
                        Fragment(
                            findings
                            |> List.groupBy (fun f -> Policy.code f.Code)
                            |> List.map (fun (code, items) -> disclosure $"{code} ({items.Length})" [ el "ul" [ "class", "ef-obligation-list" ] (items |> List.map (findingItem ctx page)) ])
                        )
                    ]
                ]
            | Some(_, findings) -> section [] [ h 2 [ text $"{heading} ({findings.Length})" ]; el "ul" [ "class", "ef-obligation-list" ] (findings |> List.map (findingItem ctx page)) ]

        { Path = page
          Text =
            layout ctx page "Integrity" [
                el "header" [] [ h 1 [ text "Integrity" ]; p [ text $"Publication state: {Publication.stateName ctx.Corpus.State}. Only blocking findings stop publication; warnings and notes describe legitimate states of live research." ] ]
                block Blocking "Blocking"
                block Warning "Warnings"
                block Informational "Notes"
            ] }

    let private redirect (ctx: Context) (legacyPath: string) (key: ArtifactKey) =
        let page = legacyPath.TrimStart('/').TrimEnd('/') + "/index.html"
        let target = relative page (dirOf key)

        { Path = page
          Text =
            el "html" [ "lang", "en" ] [
                el "head" [] [
                    VoidElement("meta", [ "charset", "utf-8" ])
                    VoidElement("meta", [ "http-equiv", "refresh"; "content", $"0; url={target}" ])
                    VoidElement("link", [ "rel", "canonical"; "href", target ])
                    el "title" [] [ text "Moved" ]
                ]
                el "body" [] [ p [ text "This research has a stable address: "; a target [ text target ] ] ]
            ]
            |> Html.render }

    let private notFound (ctx: Context) =
        { Path = "404.html"
          Text = layout ctx "404.html" "Not found" [ h 1 [ text "Not found" ]; p [ text "No published research has this address. "; a "index.html" [ text "Start from the home page" ]; text "." ] ] }

    /// All static pages and legacy redirect stubs, plus findings for any
    /// legacy URL that would be lost (TEK-IDY-006, blocking).
    let render (corpus: Corpus) (options: SiteOptions) : OutputFile list * ValidationFinding list =
        let ctx =
            { Corpus = corpus
              Options = options
              ByKey = corpus.Artifacts |> List.map (fun a -> a.Reading.Artifact.Key, a) |> Map.ofList
              ByPath = corpus.Artifacts |> List.map (fun a -> RepoPath.value a.Reading.Artifact.Location.Path, a.Reading.Artifact.Key) |> Map.ofList }

        let projectOf (a: IngestedArtifact) =
            match a.Reading.Artifact.Project with
            | Known project -> Some project
            | Absent _ ->
                // Frontier records belong to the project of their origin document.
                corpus.Canonical
                |> List.tryPick (fun e ->
                    match e.Relation, e.Target with
                    | OriginDocument, Resolves origin when e.From = a.Reading.Artifact.Key ->
                        artifactOf ctx origin |> Option.bind (fun o -> Knowable.toOption o.Project)
                    | _ -> None)

        let projects =
            corpus.Artifacts
            |> List.choose (fun a -> projectOf a |> Option.map (fun p -> p, a))
            |> List.groupBy fst
            |> List.map (fun (project, pairs) -> project, pairs |> List.map snd)
            |> List.sortBy fst

        let unassigned = corpus.Artifacts |> List.filter (fun a -> (projectOf a).IsNone)

        let pages =
            [ landing ctx projects unassigned; integrityPageFor ctx; notFound ctx ]
            @ (projects |> List.map (fun (project, members) -> projectPageFor ctx project members))
            @ (corpus.Artifacts |> List.map (artifactPage ctx))

        let emitted = pages |> List.map (fun f -> f.Path) |> set

        let redirects, lost =
            options.LegacyUrls
            |> List.distinct
            |> List.fold
                (fun (stubs, lost) (legacyPath, sourcePath) ->
                    match ctx.ByPath.TryFind sourcePath with
                    | None -> stubs, lost // not in the published scope
                    | Some key ->
                        let stub = redirect ctx legacyPath key

                        if emitted.Contains stub.Path then
                            stubs,
                            { Code = LostPublishedUrl legacyPath
                              Subject = Some key
                              Location = None
                              Message = $"Previously published {legacyPath} collides with a page Tekmerion emits."
                              Remedy = None }
                            :: lost
                        else
                            stub :: stubs, lost)
                ([], [])

        (pages @ List.rev redirects |> List.sortBy (fun f -> f.Path)), List.rev lost
