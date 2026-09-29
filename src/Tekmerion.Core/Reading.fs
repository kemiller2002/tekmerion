namespace Tekmerion.Core

open System
open Tekmerion.Domain

/// A reference exactly as declared, before resolution.
type DeclaredReference =
    { Relation: CanonicalRelation
      Value: ReferenceValue
      Location: SourceLocation }

type FrontMatterState =
    | FrontMatterPresent
    | FrontMatterAbsent
    | FrontMatterMalformed of reason: string

/// Everything read from one source file: the typed artifact, the references it
/// declares, and the findings reading produced.
type ArtifactReading =
    { Artifact: Artifact
      References: DeclaredReference list
      Findings: ValidationFinding list
      FrontMatter: FrontMatterState }

/// discover → **parse → type** (GH-17). Reads one file into the domain without
/// defaulting anything: absent keys stay `NotDeclared` (TEK-FID-001).
module Reading =

    /// Keys whose values become typed fields. Every other key is preserved as
    /// an Extension with its verbatim text (TEK-ING-003).
    let private typeKeys = [ "document_type"; "artifactType" ]

    let private relationKeys: (string * CanonicalRelation) list =
        [ "source_rep", SourceRep
          "related_documents", RelatedDocument
          "relatedDocuments", RelatedDocument
          "related_artifacts", RelatedDocument
          "related_theory", RelatedDocument
          "supersedes", Supersedes
          "superseded_by", SupersededBy
          "supersededBy", SupersededBy
          "evidenceIds", TypedLink "evidenceIds"
          "hypothesisIds", TypedLink "hypothesisIds"
          "theoryIds", TypedLink "theoryIds"
          "tests", TypedLink "tests" ]

    let private fieldKeys =
        set
            [ "id"
              "title"
              "project"
              "status"
              "date"
              "created"
              "updated"
              "summary"
              "abstract"
              "purposes"
              "audiences"
              "references" ]

    let private modelledKeys =
        Set.unionMany [ fieldKeys; set typeKeys; relationKeys |> List.map fst |> set ]

    let private finding code subject location message remedy =
        { Code = code
          Subject = subject
          Location = location
          Message = message
          Remedy = remedy }

    /// The artifact-type directory in `content/projects/<project>/<type>/<file>`
    /// or the frontier records directory; anything else is not inferred.
    let private directoryType (path: RepoPath) =
        match (RepoPath.value path).Split('/') |> List.ofArray with
        | "content" :: "projects" :: _ :: segment :: _ :: _ -> Some segment
        | "research" :: "frontier" :: "records" :: [ _ ] -> Some "frontier-record"
        | _ -> None

    let private scalar (entry: FrontMatterEntry) : Knowable<string> =
        match FrontMatter.texts entry.Value with
        | [ single ] -> Known single
        | [] -> Absent Absence.NotDeclared
        | many -> Known(String.Join(", ", many))

    /// `revision` is the source commit when the host knows it; it is carried
    /// into every location read from this file (TEK-PRV-003) and is never
    /// guessed.
    let readAt (repository: RepositoryId) (revision: string option) (path: RepoPath) (text: string) : ArtifactReading =
        let fileLocation = { SourceLocation.ofFile repository path with Commit = revision }
        let atEntry (entry: FrontMatterEntry) : SourceLocation = fileLocation |> SourceLocation.atKey entry.Key (Some entry.Line)

        let entries, body, bodyStartLine, state, splitFindings =
            match FrontMatter.split text with
            | FrontMatterSplit.NoFrontMatter body ->
                [], body, 1, FrontMatterAbsent,
                [ finding FindingCode.NoFrontMatter None (Some fileLocation) "The file has no front matter; its body is published as written." None ]
            | UnterminatedFrontMatter body ->
                let reason = "front matter opened with --- but never closed"

                [], body, 1, FrontMatterMalformed reason,
                [ finding (MalformedFrontMatter reason) None (Some fileLocation) reason (Some "Close the front matter with a line containing only ---.") ]
            | WithFrontMatter(yaml, yamlLine, body, bodyLine) ->
                match FrontMatter.parse yaml yamlLine with
                | Entries parsed -> parsed, body, bodyLine, FrontMatterPresent, []
                | Malformed(reason, line) ->
                    [], body, bodyLine, FrontMatterMalformed reason,
                    [ finding
                          (MalformedFrontMatter reason)
                          None
                          (Some { fileLocation with Line = line })
                          $"Front matter could not be read: {reason}. Metadata is not applied; the body is still published."
                          (Some "Correct the YAML syntax at the reported line.") ]

        let entry key = entries |> List.tryFind (fun (e: FrontMatterEntry) -> e.Key = key)
        let known (key: string) = entry key |> Option.map scalar |> Option.defaultValue (Absent Absence.NotDeclared)
        let listOf (key: string) = entry key |> Option.map (fun (e: FrontMatterEntry) -> FrontMatter.texts e.Value) |> Option.defaultValue []

        // Identity: a declared id that cannot be an ArtifactId is kept as text
        // and reported; the artifact falls back to its path key.
        let declaredIdText = known "id"

        let declaredId, idFindings =
            match declaredIdText with
            | Known raw ->
                match ArtifactId.create raw with
                | Ok id -> Some id, []
                | Error reason ->
                    None,
                    [ finding
                          (InvalidDeclaredId(raw, reason))
                          None
                          (entry "id" |> Option.map atEntry)
                          $"Declared id '{raw}' cannot be used as an address: {reason}. The artifact is addressed by its path key."
                          (Some "Use only letters, digits, '.', '_' and '-' in ids.") ]
            | Absent _ -> None, []

        let key = ArtifactKey.assign declaredId path
        let subject = Some key

        // Type: declared wins; directory inference is recorded as inference.
        let declaredTypes =
            typeKeys
            |> List.choose (fun (k: string) -> entry k |> Option.bind (fun (e: FrontMatterEntry) -> match scalar e with Known raw -> Some(k, raw, e) | Absent _ -> None))

        let artifactType, typeSource, typeFindings =
            match declaredTypes with
            | (typeKey, raw, _) :: rest ->
                let contradiction =
                    rest
                    |> List.filter (fun (_, other, _) -> ArtifactType.normalise other <> ArtifactType.normalise raw)
                    |> List.map (fun (otherKey, _, e) ->
                        finding
                            (ContradictoryMetadata [ typeKey; otherKey ])
                            subject
                            (Some(atEntry e))
                            $"'{typeKey}' and '{otherKey}' declare different types; both are preserved and '{typeKey}' (first in the file) is used for the type facet."
                            (Some "Declare the type once."))

                ArtifactType.classify raw, DeclaredType(typeKey, raw), contradiction
            | [] ->
                let missing =
                    finding MissingDeclaredType subject (Some fileLocation) "No document_type or artifactType is declared." (Some "Add document_type to the front matter.")

                match directoryType path with
                | Some segment -> ArtifactType.classify segment, InferredFromDirectory segment, [ missing ]
                | None -> Other "unknown", UnknownType, [ missing ]

        let references =
            relationKeys
            |> List.collect (fun (relationKey, relation) ->
                match entry relationKey with
                | None -> []
                | Some(e: FrontMatterEntry) ->
                    FrontMatter.texts e.Value
                    |> List.map (fun value ->
                        ({ Relation = relation
                           Value = References.classify value
                           Location = atEntry e }: DeclaredReference)))

        let extensions =
            entries
            // `abstract` is only a summary fallback; when `summary` is also
            // declared, the abstract is preserved rather than dropped.
            |> List.filter (fun (e: FrontMatterEntry) -> not (modelledKeys.Contains e.Key) || (e.Key = "abstract" && (entry "summary").IsSome))
            |> List.map (fun (e: FrontMatterEntry) ->
                ({ Key = e.Key
                   RawValue = e.Raw
                   Location = atEntry e }: Extension))

        let unknownKeyFindings =
            extensions
            |> List.map (fun (ext: Extension) ->
                finding (UnknownKey ext.Key) subject (Some ext.Location) $"Front-matter key '{ext.Key}' is preserved verbatim as extension content." None)

        let missingIdFinding =
            match declaredIdText with
            | Absent _ ->
                [ finding MissingId subject (Some fileLocation) "No id is declared; the artifact is addressed by a stable path key." (Some "Add an id to give the artifact a permanent address.") ]
            | Known _ -> []

        let summary =
            match known "summary" with
            | Known text -> Known text
            | Absent _ -> known "abstract"

        let artifact: Artifact =
            { Key = key
              DeclaredIdText = declaredIdText
              Type = artifactType
              TypeSource = typeSource
              Title = known "title"
              Project = known "project"
              Status = known "status" |> Knowable.map StatusReading.read
              Date = known "date"
              Created = known "created"
              Updated = known "updated"
              Summary = summary
              Purposes = listOf "purposes"
              Audiences = listOf "audiences"
              Bibliography = listOf "references"
              Sections = Markdown.sections fileLocation body bodyStartLine
              Extensions = extensions
              Location = fileLocation }

        let subjectOf (f: ValidationFinding) = { f with Subject = subject }

        { Artifact = artifact
          References = references
          Findings = (splitFindings @ idFindings @ typeFindings @ missingIdFinding @ unknownKeyFindings) |> List.map subjectOf
          FrontMatter = state }

    let read (repository: RepositoryId) (path: RepoPath) (text: string) : ArtifactReading = readAt repository None path text
