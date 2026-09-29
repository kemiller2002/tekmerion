namespace Tekmerion.Domain

// ---------------------------------------------------------------------------
// Publication lifecycle (TEK-STA-001..004, TEK-PUB-003, TEK-REC-002; doc 07 G.2)
//
// Tier 2 decides; tier 4 performs. The host asks `capabilities` what it may do,
// performs the effect named by `Publication.effect`, and reports what happened
// as a `PublicationEvent`. `Publication.apply` is the single transition
// authority. Output is written to a staging location and promoted atomically,
// so any failure before promotion leaves the last known-good publication
// untouched by construction.
// ---------------------------------------------------------------------------

/// Content digest of a projected or staged output tree.
type ContentDigest = ContentDigest of string

type DeploymentReceipt = DeploymentReceipt of string

type ParseSummary =
    { Parsed: int
      WithoutFrontMatter: int
      Quarantined: int }

type LinkSummary =
    { CanonicalEdges: int
      DerivedEdges: int
      Dangling: int }

/// Stage at which a run stopped. Every case is before promotion or is the
/// promotion itself, which the host performs as an atomic replace; therefore
/// no failure can leave a partially written live publication.
type FailureStage =
    | DuringDiscovery
    | DuringParse
    | DuringProjection
    | DuringStaging
    | DuringPromotion

type PublicationState =
    | Unloaded
    | Discovered of sources: int
    | NothingToPublish
    | Parsed of ParseSummary
    | Linked of ParseSummary * LinkSummary
    | Assessed of Assessment
    | Projected of PublishableEvidence * ContentDigest
    | Staged of PublishableEvidence * ContentDigest
    /// Live output replaced by the staged tree. This is the new last known-good.
    | Promoted of PublishableEvidence * ContentDigest
    | Deploying of ContentDigest * AttemptId
    | Published of ContentDigest * DeploymentReceipt
    | DeploymentFailed of ContentDigest * reason: string
    | DeploymentUnknown of ContentDigest * AttemptId * reason: string
    /// The run stopped; the live output is whatever was last promoted.
    | Stopped of FailureStage * reason: string

type PublicationCapability =
    | Discover
    | Parse
    | Link
    | Assess
    | Project
    | Stage
    | Promote
    | Deploy
    | ReconcileDeployment
    | Restart

/// Effects are requested as data; tier 4 performs them (TEK-STA-003).
type RequestedEffect =
    | ReadCorpus
    | WriteStaging of ContentDigest
    | PromoteStaging of ContentDigest
    | DeployPublication of ContentDigest
    | ObserveDeployment of AttemptId

type PublicationEvent =
    | SourcesDiscovered of count: int
    | CorpusParsed of ParseSummary
    | ReferencesLinked of LinkSummary
    | ObligationsEvaluated of ValidationFinding list
    | OutputProjected of ContentDigest
    | OutputStaged of ContentDigest
    | StagingPromoted of EffectOutcome<ContentDigest>
    | DeploymentAttempted of AttemptId
    | DeploymentReported of EffectOutcome<DeploymentReceipt>
    | DeploymentObserved of ReconciliationObservation<DeploymentReceipt>
    | StageFailed of FailureStage * reason: string
    | RestartRequested

type PublicationRefusal =
    | IllegalTransition of state: string * event: string
    | StagedDigestMismatch of expected: ContentDigest * actual: ContentDigest

[<RequireQualifiedAccess>]
module Publication =

    let stateName (state: PublicationState) =
        match state with
        | Unloaded -> "unloaded"
        | Discovered _ -> "discovered"
        | NothingToPublish -> "nothing-to-publish"
        | Parsed _ -> "parsed"
        | Linked _ -> "linked"
        | Assessed assessment ->
            match Assessment.permitsPublication assessment with
            | Some _ when Assessment.hasWarnings assessment -> "publishable-with-warnings"
            | Some _ -> "publishable"
            | None -> "unpublishable"
        | Projected _ -> "projected"
        | Staged _ -> "staged"
        | Promoted _ -> "promoted"
        | Deploying _ -> "deploying"
        | Published _ -> "published"
        | DeploymentFailed _ -> "deployment-failed"
        | DeploymentUnknown _ -> "deployment-unknown"
        | Stopped _ -> "stopped"

    let private eventName (event: PublicationEvent) =
        match event with
        | SourcesDiscovered _ -> "sources-discovered"
        | CorpusParsed _ -> "corpus-parsed"
        | ReferencesLinked _ -> "references-linked"
        | ObligationsEvaluated _ -> "obligations-evaluated"
        | OutputProjected _ -> "output-projected"
        | OutputStaged _ -> "output-staged"
        | StagingPromoted _ -> "staging-promoted"
        | DeploymentAttempted _ -> "deployment-attempted"
        | DeploymentReported _ -> "deployment-reported"
        | DeploymentObserved _ -> "deployment-observed"
        | StageFailed _ -> "stage-failed"
        | RestartRequested -> "restart-requested"

    /// Legal next actions, computed from state (TEK-STA-002).
    let capabilities (state: PublicationState) : PublicationCapability list =
        match state with
        | Unloaded -> [ Discover ]
        | Discovered _ -> [ Parse ]
        | NothingToPublish -> [ Restart ]
        | Parsed _ -> [ Link ]
        | Linked _ -> [ Assess ]
        | Assessed assessment ->
            match Assessment.permitsPublication assessment with
            | Some _ -> [ Project ]
            | None -> [ Restart ]
        | Projected _ -> [ Stage ]
        | Staged _ -> [ Promote ]
        | Promoted _ -> [ Deploy ]
        | Deploying _ -> []
        | Published _ -> [ Restart ]
        | DeploymentFailed _ -> [ Deploy; Restart ]
        | DeploymentUnknown _ -> [ ReconcileDeployment ]
        | Stopped _ -> [ Restart ]

    /// The effect a capability requires the host to perform, if any.
    let effect (state: PublicationState) (capability: PublicationCapability) : RequestedEffect option =
        match state, capability with
        | Unloaded, Discover -> Some ReadCorpus
        | Projected(_, digest), Stage -> Some(WriteStaging digest)
        | Staged(_, digest), Promote -> Some(PromoteStaging digest)
        | (Promoted(_, digest) | DeploymentFailed(digest, _)), Deploy -> Some(DeployPublication digest)
        | DeploymentUnknown(_, attempt, _), ReconcileDeployment -> Some(ObserveDeployment attempt)
        | _ -> None

    let private deployed digest (outcome: EffectOutcome<DeploymentReceipt>) attempt =
        match outcome with
        | Succeeded receipt -> Published(digest, receipt)
        | Failed reason -> DeploymentFailed(digest, reason)
        | Unknown(_, reason) -> DeploymentUnknown(digest, attempt, reason)

    /// The single transition authority. Anything not listed is refused.
    let apply (state: PublicationState) (event: PublicationEvent) : Result<PublicationState, PublicationRefusal> =
        let refuse () =
            Error(IllegalTransition(stateName state, eventName event))

        match state, event with
        | Unloaded, SourcesDiscovered 0 -> Ok NothingToPublish
        | Unloaded, SourcesDiscovered count when count > 0 -> Ok(Discovered count)
        | Discovered _, CorpusParsed summary -> Ok(Parsed summary)
        | Parsed summary, ReferencesLinked links -> Ok(Linked(summary, links))
        | Linked _, ObligationsEvaluated findings -> Ok(Assessed(Assessment.evaluate findings))
        | Assessed assessment, OutputProjected digest ->
            match Assessment.permitsPublication assessment with
            | Some evidence -> Ok(Projected(evidence, digest))
            | None -> refuse ()
        | Projected(evidence, projected), OutputStaged staged ->
            if projected = staged then Ok(Staged(evidence, staged))
            else Error(StagedDigestMismatch(projected, staged))
        | Staged(evidence, digest), StagingPromoted(Succeeded promoted) ->
            if promoted = digest then Ok(Promoted(evidence, digest))
            else Error(StagedDigestMismatch(digest, promoted))
        | Staged _, StagingPromoted(Failed reason) -> Ok(Stopped(DuringPromotion, reason))
        | Staged _, StagingPromoted(Unknown(_, reason)) ->
            // Atomic replace either happened or did not; the host must observe
            // which before anything else proceeds, so the run stops here.
            Ok(Stopped(DuringPromotion, $"promotion outcome unknown: {reason}"))
        | (Promoted(_, digest) | DeploymentFailed(digest, _)), DeploymentAttempted attempt ->
            Ok(Deploying(digest, attempt))
        | Deploying(digest, attempt), DeploymentReported outcome -> Ok(deployed digest outcome attempt)
        | DeploymentUnknown(digest, attempt, reason), DeploymentObserved observation ->
            Ok(deployed digest (Reconciliation.reconcile (Unknown(attempt, reason)) observation) attempt)
        | (Unloaded | Discovered _ | Parsed _ | Linked _ | Assessed _ | Projected _ | Staged _), StageFailed(stage, reason) ->
            Ok(Stopped(stage, reason))
        | (NothingToPublish | Assessed _ | Published _ | DeploymentFailed _ | Stopped _), RestartRequested ->
            match state with
            | Assessed assessment when (Assessment.permitsPublication assessment).IsSome -> refuse ()
            | _ -> Ok Unloaded
        | _ -> refuse ()

    /// Whether the live output may have changed in this state. Before
    /// promotion it cannot: the last known-good publication is intact
    /// (TEK-PUB-003).
    let liveOutputChanged (state: PublicationState) =
        match state with
        | Promoted _
        | Deploying _
        | Published _
        | DeploymentFailed _
        | DeploymentUnknown _ -> true
        | Stopped(DuringPromotion, _) -> true
        | Unloaded
        | Discovered _
        | NothingToPublish
        | Parsed _
        | Linked _
        | Assessed _
        | Projected _
        | Staged _
        | Stopped _ -> false
