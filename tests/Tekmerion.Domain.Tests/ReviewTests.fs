namespace Tekmerion.Domain.Tests

open System
open Xunit
open Tekmerion.Domain
open Tekmerion.Domain.Tests.Fixtures

module ReviewTests =

    let agent =
        Agent
            { Provider = "anthropic"
              Runtime = "claude-code"
              Model = Known "claude-opus-5-5"
              Session = Absent Absence.Unknown }

    let at = DateTimeOffset(2026, 9, 29, 0, 0, 0, TimeSpan.Zero)

    let challenge kind =
        { Kind = kind
          Raised =
            { Actor = agent
              At = at
              Reason = "no declared evidence"
              Location = location "content/projects/composition-science/x.md" } }

    let approval =
        { Reviewer = "research owner"
          At = at
          Location = location "content/projects/composition-science/x.md" }

    let run commands =
        commands |> List.fold (fun state command -> state |> Result.bind (fun s -> Review.decide s command)) (Ok Unreviewed)

    [<Fact>]
    let ``TEK-STA-005 a reviewed artifact can be approved by a human`` () =
        Assert.Equal(Ok(Approved approval), run [ StartReview agent; Approve approval ])

    [<Fact>]
    let ``TEK-REV-002 approval is illegal while challenges are open`` () =
        let result = run [ StartReview agent; RaiseChallenge(challenge UnsupportedClaim); Approve approval ]
        Assert.Equal(Error(NotLegalInState("challenged", "approve")), result)

    [<Fact>]
    let ``TEK-STA-002 capabilities never offer approval while challenged`` () =
        match run [ StartReview agent; RaiseChallenge(challenge CircularSupport) ] with
        | Ok state -> Assert.DoesNotContain(CanApprove, Review.capabilities state)
        | Error refusal -> failwithf "%A" refusal

    [<Fact>]
    let ``challenges accumulate rather than replace one another`` () =
        match run [ StartReview agent; RaiseChallenge(challenge StaleEvidence); RaiseChallenge(challenge DependencyGap) ] with
        | Ok(Challenged(_, challenges)) ->
            Assert.Equal<ChallengeKind list>(
                [ StaleEvidence; DependencyGap ],
                challenges |> NonEmpty.toList |> List.map (fun c -> c.Kind)
            )
        | other -> failwithf "%A" other

    [<Fact>]
    let ``TEK-STA-002 every capability offered is accepted and nothing else is`` () =
        let commandFor capability =
            match capability with
            | CanStartReview -> StartReview agent
            | CanRaiseChallenge -> RaiseChallenge(challenge UnsupportedClaim)
            | CanResolveChallenges -> ResolveChallenges (challenge UnsupportedClaim).Raised
            | CanRequestChanges -> RequestChanges "rework"
            | CanApprove -> Approve approval
            | CanReopen -> Reopen (challenge UnsupportedClaim).Raised

        let all = [ CanStartReview; CanRaiseChallenge; CanResolveChallenges; CanRequestChanges; CanApprove; CanReopen ]

        let states =
            [ Unreviewed
              InReview agent
              Challenged(agent, NonEmpty.singleton (challenge UnsupportedClaim))
              ChangesRequested(agent, "rework")
              Approved approval ]

        for state in states do
            let offered = Review.capabilities state

            for capability in all do
                let accepted = Result.isOk (Review.decide state (commandFor capability))
                Assert.True((List.contains capability offered) = accepted, $"{state} / {capability}")
