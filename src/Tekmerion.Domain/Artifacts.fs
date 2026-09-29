namespace Tekmerion.Domain

open System

/// Observed artifact families. `Other` is a required case, not a failure: the
/// corpus uses 22 `document_type` values and new ones appear without notice.
type ArtifactType =
    | ResearchExecutionPackage
    | ResearchReport
    | ResearchNote
    | ResearchFramework
    | ResearchJournal
    | EvidenceRegistry
    | HypothesisRegistry
    | TheoryRegistry
    | ExperimentReport
    | DecisionRecord
    | FrontierRecord
    | Other of raw: string

/// How the type was established, so a declared type is never confused with an
/// inferred one.
type TypeSource =
    | DeclaredType of key: string * raw: string
    | InferredFromDirectory of segment: string
    | InferredFromIdPrefix of prefix: string
    | UnknownType

[<RequireQualifiedAccess>]
module ArtifactType =

    /// `experiment_report`, `Experiment-Report` and `experiment-report` are one
    /// concept (02 B.1); separators and case are normalised before matching.
    let normalise (raw: string) =
        raw.Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-')

    let classify (raw: string) : ArtifactType =
        match normalise raw with
        | "research-execution-package"
        | "research-package"
        | "rep" -> ResearchExecutionPackage
        | "research-report" -> ResearchReport
        | "research-note" -> ResearchNote
        | "research-framework" -> ResearchFramework
        | "research-journal"
        | "journal" -> ResearchJournal
        | "evidence-registry" -> EvidenceRegistry
        | "hypothesis-registry" -> HypothesisRegistry
        | "theory-registry" -> TheoryRegistry
        | "experiment-report" -> ExperimentReport
        | "decision-record" -> DecisionRecord
        | "frontier-record"
        | "research-frontier-record" -> FrontierRecord
        | _ -> Other raw

    let label (artifactType: ArtifactType) =
        match artifactType with
        | ResearchExecutionPackage -> "research-execution-package"
        | ResearchReport -> "research-report"
        | ResearchNote -> "research-note"
        | ResearchFramework -> "research-framework"
        | ResearchJournal -> "research-journal"
        | EvidenceRegistry -> "evidence-registry"
        | HypothesisRegistry -> "hypothesis-registry"
        | TheoryRegistry -> "theory-registry"
        | ExperimentReport -> "experiment-report"
        | DecisionRecord -> "decision-record"
        | FrontierRecord -> "frontier-record"
        | Other raw -> normalise raw

/// Status is free text (37 observed values). The derived class is a filter aid
/// published beside the verbatim text, never instead of it (TEK-FID-004).
type StatusClass =
    | Draft
    | Active
    | Complete
    | Verified
    | Superseded
    | Open
    | Unclassified

type StatusReading =
    { Text: string
      Class: StatusClass
      /// Outstanding obligations encoded in the phrase, e.g. `human-study-pending`.
      Outstanding: string list }

[<RequireQualifiedAccess>]
module StatusReading =

    let private classOf (normalised: string) =
        let has (word: string) = normalised.Contains(word, StringComparison.Ordinal)

        if has "supersed" then Superseded
        elif has "verified" then Verified
        elif has "draft" then Draft
        elif normalised = "open" || normalised.StartsWith "open-" then Open
        elif has "candidate" || has "proposed" then Active
        elif has "complete" || has "accepted" || has "canonical" then Complete
        elif has "active" || has "working" || has "in-progress" then Active
        else Unclassified

    /// The phrase `computational-pilot-complete-human-study-pending` reads as
    /// stage `complete` with outstanding `human-study-pending`.
    let read (text: string) : StatusReading =
        let normalised = ArtifactType.normalise text
        let marker = "-complete-"
        let index = normalised.IndexOf(marker, StringComparison.Ordinal)

        let outstanding =
            if index >= 0 && normalised.EndsWith "-pending" then
                [ normalised.Substring(index + marker.Length) ]
            else
                []

        let stage =
            if index >= 0 then normalised.Substring(0, index + marker.Length - 1) else normalised

        { Text = text
          Class = classOf stage
          Outstanding = outstanding }

/// The value kinds measured in reference fields (03 C.2).
type ReferenceValue =
    | RepoRelativePath of string
    | FileRelativePath of string
    | IdReference of string
    /// Prose naming a document. Not a link, never warned about.
    | ProseTitle of string
    | Unparsed of string

/// Outcome of resolving one reference against the artifact set.
type Resolution =
    | Resolves of ArtifactKey
    /// Legal research state; a visible warning, never dropped (TEK-REL-004).
    | Dangling of ReferenceValue
    | NotALink of text: string

/// A front-matter key or body section Tekmerion does not model, preserved
/// verbatim with its location (TEK-ING-003).
type Extension =
    { Key: string
      RawValue: string
      Location: SourceLocation }

type Section =
    { Heading: string
      Depth: int
      Slug: string
      Body: string
      Location: SourceLocation }

/// One canonical research artifact as read from source. Every optional field
/// is `Knowable`: absence is reported, never defaulted (ADR 0010).
type Artifact =
    { Key: ArtifactKey
      /// Raw `id:` as written, even when it failed to become a valid ArtifactId.
      DeclaredIdText: Knowable<string>
      Type: ArtifactType
      TypeSource: TypeSource
      Title: Knowable<string>
      Project: Knowable<string>
      Status: Knowable<StatusReading>
      Created: Knowable<string>
      Updated: Knowable<string>
      Summary: Knowable<string>
      Purposes: string list
      Audiences: string list
      /// `references:` entries are citations, never links (TEK-ING-006).
      Bibliography: string list
      Sections: Section list
      Extensions: Extension list
      Location: SourceLocation }
