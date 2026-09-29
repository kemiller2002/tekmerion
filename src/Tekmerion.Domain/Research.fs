namespace Tekmerion.Domain

open System

// ---------------------------------------------------------------------------
// Research lifecycle objects (REQ-TEK §14). Kept deliberately small: each type
// exists because a requirement names it, and every optional fact is Knowable.
// ---------------------------------------------------------------------------

/// A research question. Frontier records are the corpus's existing question
/// objects (TEK-QST-001); richer planning is a target (TEK-QST-002).
type ResearchQuestion =
    { Key: ArtifactKey
      Text: Knowable<string>
      Status: Knowable<StatusReading>
      /// Artifacts that declared this question (canonical `originates`).
      RaisedBy: ArtifactKey list
      /// What evidence would resolve it, only when declared.
      ResolutionCriteria: Knowable<string>
      Location: SourceLocation }

/// Knowledge about what is *not* there. Only ever authored: Tekmerion never
/// turns the absence of a record into negative knowledge (TEK-NEG-002), which
/// is why every case requires a declaring location.
type NegativeKnowledgeKind =
    | SearchedNotFound of query: string * scope: string
    | FailedApproach of approach: string
    | RejectedHypothesis of hypothesis: string
    | UnresolvedUnknown of question: string

type NegativeKnowledge =
    { Kind: NegativeKnowledgeKind
      Declared: SourceLocation }

/// Where a source can be found (TEK-SRC-001).
type SourceLocator =
    | RepositoryFile of RepoPath
    | ExternalUri of Uri

type SourceKind =
    | Primary
    | Secondary
    | DerivedSource

type Acquisition =
    | Acquired of retrievedAt: Knowable<DateTimeOffset>
    | AcquisitionFailed of reason: string
    | Inaccessible of reason: string

/// A source identity is declared by research; Tekmerion never mints one.
type SourceIdentity = SourceIdentity of declared: string

type Source =
    { Identity: SourceIdentity
      Locator: SourceLocator
      /// Recorded only where knowable (TEK-SRC-004).
      Kind: Knowable<SourceKind>
      Author: Knowable<string>
      VersionOrDate: Knowable<string>
      Acquisition: Acquisition
      Declared: SourceLocation }

/// A workspace groups one research effort (TEK-WSP-001). Every descriptive
/// field is read from canonical records; none is defaulted.
type ResearchWorkspace =
    { Id: ArtifactKey
      Objective: Knowable<string>
      Scope: Knowable<string>
      Status: Knowable<StatusReading>
      Members: ArtifactKey list
      CompletionCriteria: Knowable<string> }

/// Coverage is only publishable with its scope (TEK-INT-003): the population,
/// the rule, and both counts. The constructor rejects impossible ratios, and
/// there is intentionally no function that yields a bare percentage.
type ScopedCoverage =
    private
        { Population: string
          Rule: string
          Covered: int
          Total: int }

[<RequireQualifiedAccess>]
module ScopedCoverage =

    let create population rule covered total : Result<ScopedCoverage, string> =
        if String.IsNullOrWhiteSpace population || String.IsNullOrWhiteSpace rule then
            Error "coverage requires a named population and rule"
        elif total < 0 || covered < 0 then
            Error "coverage counts cannot be negative"
        elif covered > total then
            Error $"covered ({covered}) exceeds total ({total})"
        else
            Ok
                { Population = population
                  Rule = rule
                  Covered = covered
                  Total = total }

    let describe (coverage: ScopedCoverage) =
        $"{coverage.Covered} of {coverage.Total} {coverage.Population} ({coverage.Rule})"

    let counts (coverage: ScopedCoverage) =
        coverage.Population, coverage.Rule, coverage.Covered, coverage.Total

// ---------------------------------------------------------------------------
// Gated sub-document research objects (TEK-EVD-001, TEK-REL-006, OQ-TEK-001).
//
// Types exist so the target can be designed against. Instances require an
// approved AuthoringContract, which can only be obtained from
// `AuthoringContracts.approved`. That list is empty until the research-format
// owner answers OQ-TEK-001, so no instance can be constructed today — from
// prose or from anything else.
// ---------------------------------------------------------------------------

/// Approval of a deterministic authoring contract under which research
/// declares sub-document objects with explicit identity and provenance.
type AuthoringContract = private AuthoringContract of name: string * version: string

[<RequireQualifiedAccess>]
module AuthoringContracts =

    /// Contracts approved for declaring claims, findings and evidence.
    /// Adding one requires a decision record answering OQ-TEK-001.
    let approved: AuthoringContract list = []

    let describe (AuthoringContract(name, version)) = $"{name}@{version}"

type DeclaredObjectKind =
    | Claim
    | Finding
    | Evidence
    | Observation
    | Inference
    | Conclusion
    | Recommendation

type ClaimAuthorship =
    | SourceAuthored
    | ResearcherDerived of Actor

type DeclaredObjectId = private DeclaredObjectId of string

type DeclaredObject =
    private
        { Id: DeclaredObjectId
          Kind: DeclaredObjectKind
          Authorship: ClaimAuthorship
          Contract: AuthoringContract
          Declared: SourceLocation }

/// Declared relations between declared objects. Polarity is exactly what was
/// declared; Tekmerion never forces one (TEK-EVD-002).
type DeclaredRelationKind =
    | Supports
    | Contradicts
    | EvidenceFor
    | EvidenceAgainst
    | Contextual
    | Insufficient
    | DependsOn

type DeclaredRelation =
    private
        { From: DeclaredObjectId
          To: DeclaredObjectId
          Kind: DeclaredRelationKind
          Contract: AuthoringContract
          Declared: SourceLocation }

[<RequireQualifiedAccess>]
module DeclaredObject =

    /// The only constructor. It needs an approved contract and an explicit
    /// declared id at a source location; nothing here accepts prose.
    let declare
        (contract: AuthoringContract)
        (declaredId: string)
        (kind: DeclaredObjectKind)
        (authorship: ClaimAuthorship)
        (location: SourceLocation)
        : Result<DeclaredObject, string> =
        if not (AuthoringContracts.approved |> List.contains contract) then
            Error $"authoring contract {AuthoringContracts.describe contract} is not approved"
        else
            match ArtifactId.create declaredId with
            | Error reason -> Error reason
            | Ok _ ->
                Ok
                    { Id = DeclaredObjectId declaredId
                      Kind = kind
                      Authorship = authorship
                      Contract = contract
                      Declared = location }

    let id (declared: DeclaredObject) =
        let (DeclaredObjectId raw) = declared.Id
        raw

    let kind (declared: DeclaredObject) = declared.Kind

[<RequireQualifiedAccess>]
module DeclaredRelation =

    let declare
        (contract: AuthoringContract)
        (source: DeclaredObject)
        (target: DeclaredObject)
        (kind: DeclaredRelationKind)
        (location: SourceLocation)
        : Result<DeclaredRelation, string> =
        if not (AuthoringContracts.approved |> List.contains contract) then
            Error $"authoring contract {AuthoringContracts.describe contract} is not approved"
        else
            Ok
                { From = source.Id
                  To = target.Id
                  Kind = kind
                  Contract = contract
                  Declared = location }

    /// Cycles among declared `DependsOn` relations (TEK-INT-002).
    let dependencyCycles (relations: DeclaredRelation list) : string list list =
        relations
        |> List.filter (fun relation -> relation.Kind = DependsOn)
        |> List.map (fun relation ->
            let (DeclaredObjectId a) = relation.From
            let (DeclaredObjectId b) = relation.To
            a, b)
        |> Cycles.find
