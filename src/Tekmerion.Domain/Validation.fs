namespace Tekmerion.Domain

/// Every condition validation can report. Severity is a function of the code
/// (see `Policy.severity`), so a caller cannot make a dangling reference
/// blocking or a duplicate id a warning (TEK-VAL-002, TEK-VAL-003).
type FindingCode =
    // Blocking: the only conditions that stop publication.
    | DuplicateId of id: string
    | DuplicateUrl of url: string
    | UnreadableSource of reason: string
    | OutputPathEscape of path: string
    | LostPublishedUrl of url: string
    | NonPublicContent of marker: string
    // Warnings: legitimate states of live research, surfaced not fixed.
    | DanglingReference of reference: ReferenceValue
    | BareFilenameReference of reference: string
    | InvalidDeclaredId of raw: string * reason: string
    | MalformedFrontMatter of reason: string
    | ContradictoryMetadata of keys: string list
    | DependencyCycle of members: string list
    | SupersessionCycle of members: string list
    | NewerFormatVersion of declared: string
    // Informational: never a defect.
    | MissingId
    | MissingDeclaredType
    | NoFrontMatter
    | UnknownKey of key: string
    | OutOfScopeReference of path: string
    | Orphan

type Severity =
    | Blocking
    | Warning
    | Informational

type ValidationFinding =
    { Code: FindingCode
      Subject: ArtifactKey option
      Location: SourceLocation option
      Message: string
      Remedy: string option }

[<RequireQualifiedAccess>]
module Policy =

    let severity (code: FindingCode) : Severity =
        match code with
        | DuplicateId _
        | DuplicateUrl _
        | UnreadableSource _
        | OutputPathEscape _
        | LostPublishedUrl _
        | NonPublicContent _ -> Blocking
        | DanglingReference _
        | BareFilenameReference _
        | InvalidDeclaredId _
        | MalformedFrontMatter _
        | ContradictoryMetadata _
        | DependencyCycle _
        | SupersessionCycle _
        | NewerFormatVersion _ -> Warning
        | MissingId
        | MissingDeclaredType
        | NoFrontMatter
        | UnknownKey _
        | OutOfScopeReference _
        | Orphan -> Informational

    /// Stable machine code for contracts and tests (TEK-VAL-001).
    let code (code: FindingCode) =
        match code with
        | DuplicateId _ -> "duplicate-id"
        | DuplicateUrl _ -> "duplicate-url"
        | UnreadableSource _ -> "unreadable-source"
        | OutputPathEscape _ -> "output-path-escape"
        | LostPublishedUrl _ -> "lost-published-url"
        | NonPublicContent _ -> "non-public-content"
        | DanglingReference _ -> "dangling-reference"
        | BareFilenameReference _ -> "bare-filename-reference"
        | InvalidDeclaredId _ -> "invalid-declared-id"
        | MalformedFrontMatter _ -> "malformed-front-matter"
        | ContradictoryMetadata _ -> "contradictory-metadata"
        | DependencyCycle _ -> "dependency-cycle"
        | SupersessionCycle _ -> "supersession-cycle"
        | NewerFormatVersion _ -> "newer-format-version"
        | MissingId -> "missing-id"
        | MissingDeclaredType -> "missing-declared-type"
        | NoFrontMatter -> "no-front-matter"
        | UnknownKey _ -> "unknown-key"
        | OutOfScopeReference _ -> "out-of-scope-reference"
        | Orphan -> "orphan"

/// An unmet condition derived from unresolved work (TEK-VAL-005). A blocking
/// obligation prevents publication and nothing else.
type Obligation =
    { Finding: ValidationFinding
      Severity: Severity }

[<RequireQualifiedAccess>]
module Obligation =

    let ofFinding (finding: ValidationFinding) =
        { Finding = finding
          Severity = Policy.severity finding.Code }

    let isBlocking (obligation: Obligation) = obligation.Severity = Blocking

/// The result of evaluating every obligation. `Publishable` can only be built
/// by `Assessment.evaluate` when no obligation blocks, so "publishable with a
/// blocking finding" is unrepresentable (draft §4.4, TEK-STA-001).
type Assessment =
    private
    | PublishableAssessment of warnings: Obligation list
    | UnpublishableAssessment of blocking: NonEmpty<Obligation> * others: Obligation list

/// Proof that an assessment permits projection. Only obtainable from an
/// assessment with no blocking obligations.
type PublishableEvidence = private PublishableEvidence of Obligation list

[<RequireQualifiedAccess>]
module Assessment =

    let evaluate (findings: ValidationFinding list) : Assessment =
        let obligations = findings |> List.map Obligation.ofFinding
        let blocking, others = obligations |> List.partition Obligation.isBlocking

        match NonEmpty.create blocking with
        | Some blockers -> UnpublishableAssessment(blockers, others)
        | None -> PublishableAssessment others

    let obligations (assessment: Assessment) =
        match assessment with
        | PublishableAssessment others -> others
        | UnpublishableAssessment(blocking, others) -> NonEmpty.toList blocking @ others

    let blocking (assessment: Assessment) =
        match assessment with
        | PublishableAssessment _ -> []
        | UnpublishableAssessment(blocking, _) -> NonEmpty.toList blocking

    let permitsPublication (assessment: Assessment) : PublishableEvidence option =
        match assessment with
        | PublishableAssessment others -> Some(PublishableEvidence others)
        | UnpublishableAssessment _ -> None

    let hasWarnings (assessment: Assessment) =
        obligations assessment |> List.exists (fun obligation -> obligation.Severity = Warning)

[<RequireQualifiedAccess>]
module PublishableEvidence =

    let obligations (PublishableEvidence obligations) = obligations
