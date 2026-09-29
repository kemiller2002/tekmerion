namespace Tekmerion.Domain

open System

/// A challenge raised during review (TEK-REV-002). Each case names a kind of
/// weakness a reviewer can point at in declared structure.
type ChallengeKind =
    | UnsupportedClaim
    | DependencyGap
    | CircularSupport
    | StaleEvidence
    | UnresolvedContradiction
    | StrongestCounterevidence
    | OtherChallenge of string

type Challenge =
    { Kind: ChallengeKind
      Raised: Assertion }

/// Human approval is its own type: an agent identity cannot be placed here, so
/// agent generation can never be recorded as human approval (TEK-STA-005,
/// TEK-REV-001).
type HumanApproval =
    { Reviewer: string
      At: DateTimeOffset
      Location: SourceLocation }

type ReviewState =
    | Unreviewed
    | InReview of reviewer: Actor
    | Challenged of reviewer: Actor * challenges: NonEmpty<Challenge>
    | ChangesRequested of reviewer: Actor * reason: string
    | Approved of HumanApproval

type ReviewCommand =
    | StartReview of Actor
    | RaiseChallenge of Challenge
    | ResolveChallenges of Assertion
    | RequestChanges of reason: string
    | Approve of HumanApproval
    | Reopen of Assertion

type ReviewRefusal =
    | NotLegalInState of state: string * command: string
    | ReviewerMismatch

type ReviewCapability =
    | CanStartReview
    | CanRaiseChallenge
    | CanResolveChallenges
    | CanRequestChanges
    | CanApprove
    | CanReopen

[<RequireQualifiedAccess>]
module Review =

    let private name (state: ReviewState) =
        match state with
        | Unreviewed -> "unreviewed"
        | InReview _ -> "in-review"
        | Challenged _ -> "challenged"
        | ChangesRequested _ -> "changes-requested"
        | Approved _ -> "approved"

    let private commandName (command: ReviewCommand) =
        match command with
        | StartReview _ -> "start-review"
        | RaiseChallenge _ -> "raise-challenge"
        | ResolveChallenges _ -> "resolve-challenges"
        | RequestChanges _ -> "request-changes"
        | Approve _ -> "approve"
        | Reopen _ -> "reopen"

    /// Capabilities are computed from state (TEK-STA-002). Approval is not
    /// available while challenges are unresolved.
    let capabilities (state: ReviewState) : ReviewCapability list =
        match state with
        | Unreviewed -> [ CanStartReview ]
        | InReview _ -> [ CanRaiseChallenge; CanRequestChanges; CanApprove ]
        | Challenged _ -> [ CanRaiseChallenge; CanResolveChallenges; CanRequestChanges ]
        | ChangesRequested _ -> [ CanReopen ]
        | Approved _ -> [ CanReopen ]

    /// The single transition authority. Illegal commands are refused with a
    /// typed outcome rather than raising.
    let decide (state: ReviewState) (command: ReviewCommand) : Result<ReviewState, ReviewRefusal> =
        match state, command with
        | Unreviewed, StartReview reviewer -> Ok(InReview reviewer)
        | InReview reviewer, RaiseChallenge challenge -> Ok(Challenged(reviewer, NonEmpty.singleton challenge))
        | Challenged(reviewer, challenges), RaiseChallenge challenge ->
            Ok(Challenged(reviewer, NonEmpty.append challenge challenges))
        | Challenged(reviewer, _), ResolveChallenges _ -> Ok(InReview reviewer)
        | (InReview reviewer | Challenged(reviewer, _)), RequestChanges reason -> Ok(ChangesRequested(reviewer, reason))
        | InReview _, Approve approval -> Ok(Approved approval)
        | (ChangesRequested _ | Approved _), Reopen assertion -> Ok(InReview assertion.Actor)
        | _ -> Error(NotLegalInState(name state, commandName command))
