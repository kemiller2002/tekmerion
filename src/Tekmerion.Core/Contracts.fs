namespace Tekmerion.Core

open System
open System.Text
open Tekmerion.Domain

/// One output file: a path relative to the output root and its UTF-8 text.
type OutputFile = { Path: string; Text: string }

/// Versioned machine-readable contracts under `/data/v1/` (TEK-CON-001…004,
/// ADR 0007). Metadata is split from content so one artifact's full context
/// is two small fetches: `artifact/…` and `edges/…` (TEK-CON-003).
/// No file contains rendered HTML (TEK-CON-002) or a wall-clock value
/// (TEK-ARC-004).
module Contracts =

    [<Literal>]
    let Root = "data/v1"

    let private absence (absence: Absence) =
        match absence with
        | Absence.NotDeclared -> Json.JNull
        | Absence.Unknown -> Json.obj [ "absent", Json.str "unknown" ]
        | Absence.Unavailable reason -> Json.obj [ "absent", Json.str "unavailable"; "reason", Json.str reason ]
        | Absence.NotApplicable reason -> Json.obj [ "absent", Json.str "not-applicable"; "reason", Json.str reason ]

    /// `null` means "not declared by the source"; other absences are objects.
    let knowable (mapping: 'T -> Json) (value: Knowable<'T>) =
        match value with
        | Known known -> mapping known
        | Absent reason -> absence reason

    let private keySegment (key: ArtifactKey) =
        match key with
        | Declared id -> $"a/{ArtifactId.value id}"
        | PathDerived pathKey -> $"s/{PathKey.value pathKey}"

    let artifactPath key = $"{Root}/artifact/{keySegment key}.json"
    let contentPath key = $"{Root}/content/{keySegment key}.json"
    let neighbourhoodPath key = $"{Root}/edges/{keySegment key}.json"

    let keyJson (key: ArtifactKey) =
        Json.obj
            [ "key", Json.str (ArtifactKey.value key)
              "kind", Json.str (if ArtifactKey.isDeclared key then "declared" else "path-derived")
              "url", Json.str (ArtifactKey.url key) ]

    let location (location: SourceLocation) =
        Json.obj
            [ "repository", Json.str (RepositoryId.value location.Repository)
              "path", Json.str (RepoPath.value location.Path)
              "line", Json.ofOption Json.int location.Line
              "frontMatterKey", Json.ofOption Json.str location.FrontMatterKey
              "headingPath", Json.strings location.HeadingPath
              "commit", Json.ofOption Json.str location.Commit ]

    let private referenceJson (value: ReferenceValue) =
        let kind, text =
            match value with
            | RepoRelativePath t -> "repo-relative-path", t
            | FileRelativePath t -> "file-relative-path", t
            | IdReference t -> "id", t
            | ProseTitle t -> "prose", t
            | ExternalUrl t -> "external-url", t
            | Unparsed t -> "unparsed", t

        Json.obj [ "kind", Json.str kind; "text", Json.str text ]

    let private targetJson (target: Resolution) =
        match target with
        | Resolves key -> Json.obj [ "status", Json.str "resolves"; "artifact", keyJson key ]
        | OutOfScope(_, path) -> Json.obj [ "status", Json.str "out-of-scope"; "path", Json.str (RepoPath.value path) ]
        | Dangling _ -> Json.obj [ "status", Json.str "dangling" ]
        | NotALink _ -> Json.obj [ "status", Json.str "not-a-link" ]

    let canonicalId (edge: CanonicalEdge) =
        Hashing.shortId
            [ ArtifactKey.url edge.From
              CanonicalRelation.label edge.Relation
              RepoPath.value edge.Declared.Path
              string edge.Declared.Line
              (referenceJson edge.Reference |> Json.serialize) ]

    let private derivedLabel (relation: DerivedRelation) =
        match relation with
        | Backlink canonical -> $"backlink:{CanonicalRelation.label canonical}"
        | SupersessionChain -> "supersession-chain"

    let derivedId (edge: DerivedEdge) =
        Hashing.shortId
            ([ ArtifactKey.url edge.From; ArtifactKey.url edge.To; derivedLabel edge.Relation ]
             @ (edge.Basis |> NonEmpty.toList |> List.map canonicalId))

    let canonicalJson (edge: CanonicalEdge) =
        Json.obj
            [ "id", Json.str (canonicalId edge)
              "derived", Json.JBool false
              "relation", Json.str (CanonicalRelation.label edge.Relation)
              "from", keyJson edge.From
              "reference", referenceJson edge.Reference
              "target", targetJson edge.Target
              "declared", location edge.Declared ]

    /// A derived edge always carries its basis. `inlineBasis` embeds the
    /// canonical edges themselves so "why am I seeing this?" is answerable
    /// without another fetch (TEK-REL-003).
    let derivedJson (inlineBasis: bool) (edge: DerivedEdge) =
        let basis = edge.Basis |> NonEmpty.toList

        Json.obj
            ([ "id", Json.str (derivedId edge)
               "derived", Json.JBool true
               "relation", Json.str (derivedLabel edge.Relation)
               "from", keyJson edge.From
               "to", keyJson edge.To
               "rule", Json.str edge.Rule
               "basis", basis |> List.map (canonicalId >> Json.str) |> Json.arr ]
             @ (if inlineBasis then [ "basisEdges", basis |> List.map canonicalJson |> Json.arr ] else []))

    let private severityLabel (severity: Severity) =
        match severity with
        | Blocking -> "blocking"
        | Warning -> "warning"
        | Informational -> "informational"

    let findingJson (finding: ValidationFinding) =
        Json.obj
            [ "code", Json.str (Policy.code finding.Code)
              "severity", Json.str (severityLabel (Policy.severity finding.Code))
              "subject", Json.ofOption keyJson finding.Subject
              "location", Json.ofOption location finding.Location
              "message", Json.str finding.Message
              "remedy", Json.ofOption Json.str finding.Remedy ]

    let private populationLabel (population: Population) =
        match population with
        | AuthoredResearch -> "authored"
        | MachineGenerated -> "machine-generated"

    let private typeSourceJson (source: TypeSource) =
        match source with
        | DeclaredType(key, raw) -> Json.obj [ "kind", Json.str "declared"; "key", Json.str key; "raw", Json.str raw ]
        | InferredFromDirectory segment -> Json.obj [ "kind", Json.str "inferred-from-directory"; "segment", Json.str segment ]
        | InferredFromIdPrefix prefix -> Json.obj [ "kind", Json.str "inferred-from-id-prefix"; "prefix", Json.str prefix ]
        | UnknownType -> Json.obj [ "kind", Json.str "unknown" ]

    let private statusJson (status: StatusReading) =
        let label =
            match status.Class with
            | Draft -> "draft"
            | Active -> "active"
            | Complete -> "complete"
            | Verified -> "verified"
            | Superseded -> "superseded"
            | Open -> "open"
            | Unclassified -> "unclassified"

        Json.obj
            [ "text", Json.str status.Text
              "derivedClass", Json.str label
              "outstanding", Json.strings status.Outstanding ]

    /// The row in `artifacts.json`: listing and filtering fields only.
    let summaryJson (ingested: IngestedArtifact) =
        let artifact = ingested.Reading.Artifact

        Json.obj
            [ "artifact", keyJson artifact.Key
              "declaredId", knowable Json.str artifact.DeclaredIdText
              "population", Json.str (populationLabel ingested.Population)
              // An artifact with no declared or inferable type has no type,
              // rather than a type named "unknown" (TEK-FID-001).
              "type",
              (match artifact.TypeSource with
               | UnknownType -> Json.JNull
               | _ -> Json.str (ArtifactType.label artifact.Type))
              "typeSource", typeSourceJson artifact.TypeSource
              "title", knowable Json.str artifact.Title
              "project", knowable Json.str artifact.Project
              "status", knowable statusJson artifact.Status
              "date", knowable Json.str artifact.Date
              "created", knowable Json.str artifact.Created
              "updated", knowable Json.str artifact.Updated
              "summary", knowable Json.str artifact.Summary
              "purposes", Json.strings artifact.Purposes
              "audiences", Json.strings artifact.Audiences
              "source", location artifact.Location
              "detail", Json.str (artifactPath artifact.Key)
              "edges", Json.str (neighbourhoodPath artifact.Key) ]

    let private frontMatterLabel (state: FrontMatterState) =
        match state with
        | FrontMatterPresent -> Json.obj [ "state", Json.str "present" ]
        | FrontMatterAbsent -> Json.obj [ "state", Json.str "absent" ]
        | FrontMatterMalformed reason -> Json.obj [ "state", Json.str "malformed"; "reason", Json.str reason ]

    let private sectionOutline (section: Section) =
        Json.obj
            [ "heading", Json.str section.Heading
              "depth", Json.int section.Depth
              "anchor", Json.str section.Slug
              "line", Json.ofOption Json.int section.Location.Line
              "headingPath", Json.strings section.Location.HeadingPath ]

    let private detailJson (corpus: Corpus) (ingested: IngestedArtifact) =
        let artifact = ingested.Reading.Artifact

        let findings =
            // Unknown keys are already listed as extensions; the corresponding
            // informational notices stay in findings.json.
            corpus.Findings
            |> List.filter (fun f ->
                f.Subject = Some artifact.Key
                && (match f.Code with
                    | UnknownKey _ -> false
                    | _ -> true))
            |> List.map findingJson

        match summaryJson ingested with
        | JObject fields ->
            JObject(
                [ "schemaVersion", Json.str Version.ContractSchema ]
                @ fields
                @ [ "frontMatter", frontMatterLabel ingested.Reading.FrontMatter
                    "parserProfile", Json.str Version.ParserProfile
                    "bibliography", Json.strings artifact.Bibliography
                    "extensions",
                    artifact.Extensions
                    |> List.map (fun ext ->
                        Json.obj [ "key", Json.str ext.Key; "raw", Json.str ext.RawValue; "location", location ext.Location ])
                    |> Json.arr
                    "outline", artifact.Sections |> List.map sectionOutline |> Json.arr
                    "content", Json.str (contentPath artifact.Key)
                    "findings", Json.arr findings ]
            )
        | other -> other

    let private contentJson (ingested: IngestedArtifact) =
        let artifact = ingested.Reading.Artifact

        Json.obj
            [ "schemaVersion", Json.str Version.ContractSchema
              "artifact", keyJson artifact.Key
              "format", Json.str "markdown-source"
              "sections",
              artifact.Sections
              |> List.map (fun section ->
                  Json.obj
                      [ "heading", Json.str section.Heading
                        "depth", Json.int section.Depth
                        "anchor", Json.str section.Slug
                        "location", location section.Location
                        "body", Json.str section.Body ])
              |> Json.arr ]

    let private neighbourhoodJson (corpus: Corpus) (key: ArtifactKey) =
        let outgoing = corpus.Canonical |> List.filter (fun e -> e.From = key)

        let incoming =
            corpus.Canonical |> List.filter (fun e -> match e.Target with Resolves target -> target = key | _ -> false)

        Json.obj
            [ "schemaVersion", Json.str Version.ContractSchema
              "artifact", keyJson key
              "canonical", Json.obj [ "outgoing", outgoing |> List.map canonicalJson |> Json.arr; "incoming", incoming |> List.map canonicalJson |> Json.arr ]
              // Each derived edge here is a backlink whose basis is one of the
              // `incoming` canonical edges above, referenced by id: the
              // explanation is in this same file (TEK-REL-003).
              "derived", corpus.Derived |> List.filter (fun e -> e.From = key) |> List.map (derivedJson false) |> Json.arr
              "unsupported", Json.str "Claim, evidence, support and contradiction relations are not declared by this corpus (OQ-TEK-001) and are not inferred." ]

    /// Relations a consumer may ask for that this corpus cannot supply
    /// (TEK-CON-004). Named, not inferred.
    let unsupportedRelations =
        [ "supports"; "contradicts"; "evidence-for"; "evidence-against"; "derived-from"; "produced-by"; "answers"; "raises"; "validates"; "invalidates"; "depends-on" ]

    let private countBy (label: 'T -> string) (items: 'T list) =
        items
        |> List.countBy label
        |> List.sortBy fst
        |> List.map (fun (name, count) -> name, Json.int count)
        |> Json.obj

    let private targetStatus (edge: CanonicalEdge) =
        match edge.Target with
        | Resolves _ -> "resolves"
        | OutOfScope _ -> "out-of-scope"
        | Dangling _ -> "dangling"
        | NotALink _ -> "not-a-link"

    /// The one place a wall-clock value may appear (TEK-ARC-004, TEK-PUB-002).
    /// It is not listed in the manifest, not part of the content digest and
    /// excluded from determinism comparisons.
    [<Literal>]
    let PublicationRecord = "publication.json"

    let publicationRecord (corpus: Corpus) (contentDigest: string) (publishedAt: DateTimeOffset) : OutputFile =
        { Path = PublicationRecord
          Text =
            Json.serialize (
                Json.obj
                    [ "schemaVersion", Json.str Version.ContractSchema
                      "producer", Json.obj [ "name", Json.str "tekmerion"; "version", Json.str Version.Tekmerion ]
                      "repository", Json.str (RepositoryId.value corpus.Repository)
                      "sourceRevision", Json.ofOption Json.str corpus.Revision
                      "contentDigest", Json.str contentDigest
                      "manifest", Json.str $"{Root}/manifest.json"
                      "publishedAt", Json.str (publishedAt.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'"))
                      "note", Json.str "Not content: excluded from the content digest and from determinism checks." ]
            ) }

    /// Every contract file, manifest last so it can list the others' hashes.
    let project (corpus: Corpus) : OutputFile list =
        let file path json = { Path = path; Text = Json.serializeCompact json }
        let artifacts = corpus.Artifacts

        let perArtifact =
            artifacts
            |> List.collect (fun ingested ->
                let key = ingested.Reading.Artifact.Key

                [ file (artifactPath key) (detailJson corpus ingested)
                  file (contentPath key) (contentJson ingested)
                  file (neighbourhoodPath key) (neighbourhoodJson corpus key) ])

        let indexes =
            [ file $"{Root}/artifacts.json" (Json.obj [ "schemaVersion", Json.str Version.ContractSchema; "artifacts", artifacts |> List.map summaryJson |> Json.arr ])
              file
                  $"{Root}/edges.json"
                  (Json.obj
                      [ "schemaVersion", Json.str Version.ContractSchema
                        "canonical", corpus.Canonical |> List.map canonicalJson |> Json.arr
                        "derived", corpus.Derived |> List.map (derivedJson false) |> Json.arr ])
              file $"{Root}/findings.json" (Json.obj [ "schemaVersion", Json.str Version.ContractSchema; "findings", corpus.Findings |> List.map findingJson |> Json.arr ]) ]

        let listed = indexes @ perArtifact |> List.sortBy (fun f -> f.Path)
        let authored = artifacts |> List.filter (fun (a: IngestedArtifact) -> a.Population = AuthoredResearch)

        let manifest =
            Json.obj
                [ "schemaVersion", Json.str Version.ContractSchema
                  "producer", Json.obj [ "name", Json.str "tekmerion"; "version", Json.str Version.Tekmerion; "parserProfile", Json.str Version.ParserProfile ]
                  "repository", Json.str (RepositoryId.value corpus.Repository)
                  "sourceRevision", Json.ofOption Json.str corpus.Revision
                  "inputDigest", Json.str corpus.InputDigest
                  "state", Json.str (Publication.stateName corpus.State)
                  "validation",
                  Json.obj
                      [ "blocking", Json.int (corpus.Findings |> List.filter (fun f -> Policy.severity f.Code = Blocking) |> List.length)
                        "warning", Json.int (corpus.Findings |> List.filter (fun f -> Policy.severity f.Code = Warning) |> List.length)
                        "informational", Json.int (corpus.Findings |> List.filter (fun f -> Policy.severity f.Code = Informational) |> List.length) ]
                  "conventions",
                  Json.obj
                      [ "null", Json.str "A null value means the source does not declare it. Other absences are objects with an 'absent' field."
                        "derived", Json.str "Every derived edge has derived=true, a rule, and the ids of the canonical edges it was derived from. In an artifact's edges file those canonical edges are its 'incoming' edges."
                        "retrieval", Json.str "Full context for one artifact: its 'detail' file plus its 'edges' file. Section bodies are in 'content'." ]
                  "counts",
                  Json.obj
                      [ "artifacts", Json.int artifacts.Length
                        "byPopulation", countBy (fun (a: IngestedArtifact) -> populationLabel a.Population) artifacts
                        "byKeyKind", countBy (fun (a: IngestedArtifact) -> if ArtifactKey.isDeclared a.Reading.Artifact.Key then "declared" else "path-derived") artifacts
                        "authoredByType", countBy (fun (a: IngestedArtifact) -> match a.Reading.Artifact.TypeSource with UnknownType -> "(none)" | _ -> ArtifactType.label a.Reading.Artifact.Type) authored
                        "canonicalEdgesByRelation", countBy (fun (e: CanonicalEdge) -> CanonicalRelation.label e.Relation) corpus.Canonical
                        "canonicalEdgesByTarget", countBy targetStatus corpus.Canonical
                        "derivedEdges", Json.int corpus.Derived.Length
                        "findingsBySeverity", countBy (fun (f: ValidationFinding) -> severityLabel (Policy.severity f.Code)) corpus.Findings
                        "findingsByCode", countBy (fun (f: ValidationFinding) -> Policy.code f.Code) corpus.Findings ]
                  "unsupportedRelations", Json.strings unsupportedRelations
                  "indexes",
                  Json.obj
                      [ "artifacts", Json.str $"{Root}/artifacts.json"
                        "edges", Json.str $"{Root}/edges.json"
                        "findings", Json.str $"{Root}/findings.json"
                        "artifactDetail", Json.str $"{Root}/artifact/{{a|s}}/{{key}}.json"
                        "artifactEdges", Json.str $"{Root}/edges/{{a|s}}/{{key}}.json"
                        "artifactContent", Json.str $"{Root}/content/{{a|s}}/{{key}}.json" ]
                  "files",
                  listed
                  |> List.map (fun f ->
                      let bytes = Encoding.UTF8.GetBytes f.Text

                      Json.obj [ "path", Json.str f.Path; "bytes", Json.int bytes.Length; "sha256", Json.str (Hashing.sha256Bytes bytes) ])
                  |> Json.arr ]

        listed @ [ { Path = $"{Root}/manifest.json"; Text = Json.serialize manifest } ]
