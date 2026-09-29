namespace Tekmerion.Domain

/// Identifies one attempt at an external effect, so a later observation can be
/// reconciled with it and retries stay idempotent.
type AttemptId = AttemptId of string

/// What the host reports after performing an external effect. `Unknown` is a
/// first-class outcome — a timeout or lost connection does not mean failure,
/// and never means success (TEK-STA-004, SDE tier 4).
type EffectOutcome<'Receipt> =
    | Succeeded of 'Receipt
    | Failed of reason: string
    | Unknown of attempt: AttemptId * reason: string

/// A later, independent observation used to reconcile an unknown outcome.
type ReconciliationObservation<'Receipt> =
    | ObservedSucceeded of 'Receipt
    | ObservedFailed of reason: string
    | StillUnknown of reason: string

[<RequireQualifiedAccess>]
module Reconciliation =

    /// Resolve an unknown outcome from an observation. Known outcomes are not
    /// reopened: a second observation cannot overturn a recorded receipt.
    let reconcile
        (outcome: EffectOutcome<'Receipt>)
        (observation: ReconciliationObservation<'Receipt>)
        : EffectOutcome<'Receipt> =
        match outcome, observation with
        | Unknown _, ObservedSucceeded receipt -> Succeeded receipt
        | Unknown _, ObservedFailed reason -> Failed reason
        | Unknown(attempt, _), StillUnknown reason -> Unknown(attempt, reason)
        | (Succeeded _ | Failed _), _ -> outcome
