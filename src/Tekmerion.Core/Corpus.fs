namespace Tekmerion.Core

open System
open Tekmerion.Domain

type SourceFile = { Path: RepoPath; Text: string }

type Population =
    | AuthoredResearch
    | MachineGenerated

/// What a host supplies: file contents it has read, and the repository's file
/// list so references to real-but-unpublished files are out of scope rather
/// than dangling. The ingest is a pure function of this value.
type IngestInput =
    { Repository: RepositoryId
      Included: SourceFile list
      /// Frontier records; kept only when their declared origin document is
      /// in `Included` (scope rule `origin-in-scope`).
      FrontierCandidates: SourceFile list
      RepositoryFiles: Set<string>
      /// Source commit, when known (e.g. from configuration or CI).
      Revision: string option }

type IngestedArtifact =
    { Reading: ArtifactReading
      Population: Population }

type Corpus =
    { Repository: RepositoryId
      Artifacts: IngestedArtifact list
      Canonical: CanonicalEdge list
      Derived: DerivedEdge list
      Findings: ValidationFinding list
      Assessment: Assessment
      /// The publication lifecycle state reached by ingest (projection is
      /// performed by the host through Contracts).
      State: PublicationState
      /// SHA-256 over the sorted (path, content hash) pairs of every input.
      InputDigest: string
      /// Source commit of the research, when the host knows it.
      Revision: string option }

/// discover → parse → type → validate → **resolve** → (project) for GH-17.
module Corpus =

    let private finding code subject location message remedy =
        { Code = code
          Subject = subject
          Location = location
          Message = message
          Remedy = remedy }

    let private readWithFrontier (repository: RepositoryId) (revision: string option) (population: Population) (file: SourceFile) =
        let reading = Reading.readAt repository revision file.Path file.Text

        { Reading = { reading with References = reading.References @ Frontier.references reading.Artifact }
          Population = population }

    let private digest (files: SourceFile list) =
        files
        |> List.sortBy (fun file -> RepoPath.value file.Path)
        |> List.map (fun file -> $"{RepoPath.value file.Path}\u0000{Hashing.sha256Text file.Text}")
        |> fun lines -> Hashing.sha256Text (String.Join("\n", lines))

    type private Index =
        { ById: Map<string, ArtifactKey list>
          ByPath: Map<string, ArtifactKey>
          RepositoryFiles: Set<string> }

    let private resolve (index: Index) (from: ArtifactKey) (declaringFile: RepoPath) (reference: DeclaredReference) =
        let target =
            match reference.Value with
            | IdReference id ->
                match index.ById.TryFind id with
                | Some [ key ] -> Resolves key
                | Some(key :: _) -> Resolves key // duplicate ids are reported as blocking separately
                | _ -> Dangling reference.Value
            | FileRelativePath _
            | RepoRelativePath _ ->
                let candidates = References.candidatePaths declaringFile reference.Value
                let inScope = candidates |> List.tryPick (fun p -> index.ByPath.TryFind(RepoPath.value p))

                match inScope with
                | Some key -> Resolves key
                | None ->
                    match candidates |> List.tryFind (fun p -> index.RepositoryFiles.Contains(RepoPath.value p)) with
                    | Some existing -> OutOfScope(reference.Value, existing)
                    | None -> Dangling reference.Value
            | ProseTitle text
            | ExternalUrl text
            | Unparsed text -> NotALink text

        { From = from
          Relation = reference.Relation
          Reference = reference.Value
          Target = target
          Declared = reference.Location }

    let private describe (value: ReferenceValue) =
        match value with
        | RepoRelativePath text
        | FileRelativePath text
        | IdReference text
        | ProseTitle text
        | ExternalUrl text
        | Unparsed text -> text

    let private edgeFindings (edge: CanonicalEdge) =
        let relation = CanonicalRelation.label edge.Relation

        let bare =
            if References.isBareFilename edge.Reference then
                [ finding
                      (BareFilenameReference(describe edge.Reference))
                      (Some edge.From)
                      (Some edge.Declared)
                      $"'{describe edge.Reference}' is a bare filename; it is resolved relative to the declaring file."
                      (Some "Use a repository-relative path or a declared id.") ]
            else
                []

        match edge.Target with
        | Dangling value ->
            finding
                (DanglingReference value)
                (Some edge.From)
                (Some edge.Declared)
                $"{relation} reference '{describe value}' does not resolve to any artifact or repository file. It is shown as unresolved, not removed."
                (Some(
                    match value with
                    | IdReference _ ->
                        "No artifact declares this id. It may name an object inside another document; sub-document identity is not yet an authoring contract (OQ-TEK-001), so Tekmerion does not infer it."
                    | _ -> "If the target exists, reference it by declared id or repository path; if it is future work, no change is needed."
                ))
            :: bare
        | OutOfScope(value, path) ->
            finding
                (OutOfScopeReference(RepoPath.value path))
                (Some edge.From)
                (Some edge.Declared)
                $"{relation} reference '{describe value}' names {RepoPath.value path}, which exists in the repository but is outside the published scope."
                None
            :: bare
        | Resolves _
        | NotALink _ -> bare

    let private duplicateFindings (artifacts: IngestedArtifact list) =
        let ids =
            artifacts
            |> List.choose (fun a ->
                match a.Reading.Artifact.Key with
                | Declared id -> Some(ArtifactId.value id, a.Reading.Artifact)
                | PathDerived _ -> None)
            |> List.groupBy fst
            |> List.filter (fun (_, group) -> group.Length > 1)
            |> List.collect (fun (id, group) ->
                group
                |> List.map (fun (_, artifact) ->
                    finding
                        (DuplicateId id)
                        (Some artifact.Key)
                        (Some artifact.Location)
                        $"Id '{id}' is declared by {group.Length} files; ids must be unique."
                        (Some "Give each artifact its own id.")))

        let urls =
            artifacts
            |> List.map (fun a -> ArtifactKey.url a.Reading.Artifact.Key, a.Reading.Artifact)
            |> List.groupBy fst
            |> List.filter (fun (_, group) -> group.Length > 1)
            |> List.filter (fun (_, group) ->
                // Two files with the same declared id already produce DuplicateId.
                group |> List.map (fun (_, a) -> a.Key) |> List.distinct |> List.length > 1
                || group |> List.forall (fun (_, a) -> not (ArtifactKey.isDeclared a.Key)))
            |> List.collect (fun (url, group) ->
                group
                |> List.map (fun (_, artifact) ->
                    finding (DuplicateUrl url) (Some artifact.Key) (Some artifact.Location) $"{group.Length} artifacts map to {url}." None))

        ids @ urls

    let private cycleFindings (canonical: CanonicalEdge list) =
        let resolvedPairs relation =
            canonical
            |> List.filter (fun e -> e.Relation = relation)
            |> List.choose (fun e ->
                match e.Target with
                | Resolves target -> Some(ArtifactKey.value e.From, ArtifactKey.value target)
                | _ -> None)

        let report code relationName pairs =
            Cycles.find pairs
            |> List.map (fun members ->
                finding
                    (code members)
                    None
                    None
                    $"""Declared {relationName} edges form a cycle: {String.Join(" -> ", members)}."""
                    (Some "Review the declared dependencies; a cycle cannot be satisfied in order."))

        report DependencyCycle "prerequisite" (resolvedPairs Prerequisite)
        @ report SupersessionCycle "supersession" (resolvedPairs SupersededBy)

    let private orphanFindings (artifacts: IngestedArtifact list) (canonical: CanonicalEdge list) =
        let connected =
            canonical
            |> List.collect (fun e ->
                match e.Target with
                | Resolves target -> [ e.From; target ]
                | _ -> [])
            |> set

        artifacts
        |> List.map (fun a -> a.Reading.Artifact)
        |> List.filter (fun artifact -> not (connected.Contains artifact.Key))
        |> List.map (fun artifact ->
            finding Orphan (Some artifact.Key) (Some artifact.Location) "No resolved canonical relationship connects this artifact to another in scope." None)

    let private step state event =
        match Publication.apply state event with
        | Ok next -> next
        | Error refusal -> failwithf "illegal publication transition: %A" refusal

    /// Ordering used everywhere output is produced (TEK-ARC-004).
    let private byKey (artifact: Artifact) = ArtifactKey.url artifact.Key, RepoPath.value artifact.Location.Path

    let ingest (input: IngestInput) : Corpus =
        let authored = input.Included |> List.map (readWithFrontier input.Repository input.Revision AuthoredResearch)
        let includedPaths = input.Included |> List.map (fun f -> RepoPath.value f.Path) |> set

        let frontier =
            input.FrontierCandidates
            |> List.map (readWithFrontier input.Repository input.Revision MachineGenerated)
            |> List.filter (fun candidate ->
                candidate.Reading.References
                |> List.exists (fun r ->
                    r.Relation = OriginDocument
                    && References.candidatePaths candidate.Reading.Artifact.Location.Path r.Value
                       |> List.exists (fun p -> includedPaths.Contains(RepoPath.value p))))

        let artifacts =
            authored @ frontier |> List.sortBy (fun a -> byKey a.Reading.Artifact)

        let state0 = step Unloaded (SourcesDiscovered artifacts.Length)

        let index =
            { ById =
                artifacts
                |> List.choose (fun a ->
                    match a.Reading.Artifact.Key with
                    | Declared id -> Some(ArtifactId.value id, a.Reading.Artifact.Key)
                    | PathDerived _ -> None)
                |> List.groupBy fst
                |> List.map (fun (id, keys) -> id, keys |> List.map snd)
                |> Map.ofList
              ByPath =
                artifacts
                |> List.map (fun a -> RepoPath.value a.Reading.Artifact.Location.Path, a.Reading.Artifact.Key)
                |> Map.ofList
              RepositoryFiles = input.RepositoryFiles }

        let parseSummary =
            { Parsed = artifacts |> List.filter (fun a -> a.Reading.FrontMatter = FrontMatterPresent) |> List.length
              WithoutFrontMatter = artifacts |> List.filter (fun a -> a.Reading.FrontMatter = FrontMatterAbsent) |> List.length
              Quarantined =
                artifacts
                |> List.filter (fun a -> match a.Reading.FrontMatter with FrontMatterMalformed _ -> true | _ -> false)
                |> List.length }

        let state1 = if artifacts.IsEmpty then state0 else step state0 (CorpusParsed parseSummary)

        let canonical =
            artifacts
            |> List.collect (fun a ->
                a.Reading.References
                |> List.map (resolve index a.Reading.Artifact.Key a.Reading.Artifact.Location.Path))
            |> List.sortBy (fun e -> ArtifactKey.url e.From, RepoPath.value e.Declared.Path, e.Declared.Line, CanonicalRelation.label e.Relation, describe e.Reference)

        let derived = Derivation.backlinks canonical

        let linkSummary =
            { CanonicalEdges = canonical.Length
              DerivedEdges = derived.Length
              Dangling = canonical |> List.filter (fun e -> match e.Target with Dangling _ -> true | _ -> false) |> List.length }

        let state2 = if artifacts.IsEmpty then state1 else step state1 (ReferencesLinked linkSummary)

        let findings =
            (artifacts |> List.collect (fun a -> a.Reading.Findings))
            @ (canonical |> List.collect edgeFindings)
            @ duplicateFindings artifacts
            @ cycleFindings canonical
            @ orphanFindings artifacts canonical
            |> List.sortBy (fun f ->
                (match Policy.severity f.Code with
                 | Blocking -> 0
                 | Warning -> 1
                 | Informational -> 2),
                Policy.code f.Code,
                f.Subject |> Option.map ArtifactKey.url |> Option.defaultValue "",
                f.Location |> Option.map (fun l -> RepoPath.value l.Path, l.Line) |> Option.defaultValue ("", None),
                f.Message)

        let state3 = if artifacts.IsEmpty then state2 else step state2 (ObligationsEvaluated findings)

        { Repository = input.Repository
          Artifacts = artifacts
          Canonical = canonical
          Derived = derived
          Findings = findings
          Assessment = Assessment.evaluate findings
          State = state3
          InputDigest = digest (input.Included @ input.FrontierCandidates)
          Revision = input.Revision }
