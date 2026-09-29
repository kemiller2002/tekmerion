namespace Tekmerion.Domain.Tests

open Xunit
open Tekmerion.Domain
open Tekmerion.Domain.Tests.Fixtures

module PublicationTests =

    let digest = ContentDigest "sha256:abc"
    let parse = { Parsed = 22; WithoutFrontMatter = 1; Quarantined = 0 }
    let links = { CanonicalEdges = 4; DerivedEdges = 4; Dangling = 0 }

    let run events =
        events |> List.fold (fun state event -> state |> Result.bind (fun s -> Publication.apply s event)) (Ok Unloaded)

    let upToAssessed findings =
        [ SourcesDiscovered 23; CorpusParsed parse; ReferencesLinked links; ObligationsEvaluated findings ]

    let publishable = upToAssessed [ finding MissingId; finding (DanglingReference(IdReference "RP-COMP-999")) ]

    let stateOf events =
        match run events with
        | Ok state -> state
        | Error refusal -> failwithf "%A" refusal

    [<Fact>]
    let ``TEK-STA-001 the full lifecycle reaches Published`` () =
        let events =
            publishable
            @ [ OutputProjected digest
                OutputStaged digest
                StagingPromoted(Succeeded digest)
                DeploymentAttempted(AttemptId "run-1")
                DeploymentReported(Succeeded(DeploymentReceipt "actions-run-1")) ]

        Assert.Equal(Published(digest, DeploymentReceipt "actions-run-1"), stateOf events)

    [<Fact>]
    let ``TEK-VAL-003 warnings are publishable with warnings`` () =
        Assert.Equal("publishable-with-warnings", Publication.stateName (stateOf publishable))

    [<Fact>]
    let ``TEK-STA-001 an unpublishable corpus cannot be projected`` () =
        let blocked = upToAssessed [ finding (DuplicateId "EX-COMP-011") ]
        Assert.Equal("unpublishable", Publication.stateName (stateOf blocked))
        Assert.DoesNotContain(Project, Publication.capabilities (stateOf blocked))
        Assert.True(Result.isError (run (blocked @ [ OutputProjected digest ])))

    [<Fact>]
    let ``no sources is a terminal state, not a failure`` () =
        Assert.Equal(NothingToPublish, stateOf [ SourcesDiscovered 0 ])

    [<Fact>]
    let ``TEK-PUB-003 TEK-REC-002 a failure before promotion leaves live output untouched`` () =
        for failing in
            [ publishable @ [ StageFailed(DuringProjection, "render") ]
              publishable @ [ OutputProjected digest; StageFailed(DuringStaging, "disk full") ]
              publishable @ [ OutputProjected digest; OutputStaged digest; StagingPromoted(Failed "rename refused") ] ] do
            let state = stateOf failing
            Assert.False(Publication.liveOutputChanged state && (match state with Stopped(DuringPromotion, _) -> false | _ -> true))

            match state with
            | Stopped _ -> Assert.Equal<PublicationCapability list>([ Restart ], Publication.capabilities state)
            | other -> failwithf "expected Stopped, got %A" other

    [<Fact>]
    let ``TEK-STA-003 writes are requested only after projection`` () =
        let writes state =
            Publication.capabilities state
            |> List.choose (Publication.effect state)
            |> List.filter (function
                | WriteStaging _
                | PromoteStaging _
                | DeployPublication _ -> true
                | _ -> false)

        for prefix in [ []; [ SourcesDiscovered 23 ]; [ SourcesDiscovered 23; CorpusParsed parse ]; publishable ] do
            Assert.Empty(writes (stateOf prefix))

        Assert.Equal<RequestedEffect list>([ WriteStaging digest ], writes (stateOf (publishable @ [ OutputProjected digest ])))

    [<Fact>]
    let ``staging a different tree than was projected is refused`` () =
        Assert.Equal(
            Error(StagedDigestMismatch(digest, ContentDigest "sha256:other")),
            run (publishable @ [ OutputProjected digest; OutputStaged(ContentDigest "sha256:other") ])
        )

    [<Fact>]
    let ``TEK-STA-004 an unknown deployment must be reconciled before anything else`` () =
        let unknown =
            publishable
            @ [ OutputProjected digest
                OutputStaged digest
                StagingPromoted(Succeeded digest)
                DeploymentAttempted(AttemptId "run-2")
                DeploymentReported(Unknown(AttemptId "run-2", "timeout")) ]

        let state = stateOf unknown
        Assert.Equal<PublicationCapability list>([ ReconcileDeployment ], Publication.capabilities state)
        Assert.Equal(Some(ObserveDeployment(AttemptId "run-2")), Publication.effect state ReconcileDeployment)
        Assert.True(Result.isError (run (unknown @ [ RestartRequested ])))

        Assert.Equal(
            Published(digest, DeploymentReceipt "actions-run-2"),
            stateOf (unknown @ [ DeploymentObserved(ObservedSucceeded(DeploymentReceipt "actions-run-2")) ])
        )

    [<Fact>]
    let ``a failed deployment can be retried without rebuilding`` () =
        let failed =
            publishable
            @ [ OutputProjected digest
                OutputStaged digest
                StagingPromoted(Succeeded digest)
                DeploymentAttempted(AttemptId "run-3")
                DeploymentReported(Failed "pages unavailable") ]

        Assert.Contains(Deploy, Publication.capabilities (stateOf failed))
        Assert.Equal(Deploying(digest, AttemptId "run-4"), stateOf (failed @ [ DeploymentAttempted(AttemptId "run-4") ]))

    [<Fact>]
    let ``TEK-STA-002 events outside the current state are refused`` () =
        Assert.Equal(
            Error(IllegalTransition("unloaded", "output-projected")),
            Publication.apply Unloaded (OutputProjected digest)
        )

    [<Fact>]
    let ``informational findings alone leave the corpus plainly publishable`` () =
        Assert.Equal("publishable", Publication.stateName (stateOf (upToAssessed [ finding MissingId; finding Orphan ])))
