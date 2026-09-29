namespace Tekmerion.Domain.Tests

open Xunit
open Tekmerion.Domain

module ResearchTests =

    [<Fact>]
    let ``TEK-EVD-001 OQ-TEK-001 no authoring contract is approved yet`` () =
        // Changing this requires a decision record answering OQ-TEK-001.
        Assert.Empty AuthoringContracts.approved

    [<Fact>]
    let ``TEK-INT-003 coverage must name its population and rule`` () =
        Assert.True(Result.isError (ScopedCoverage.create "" "declares id" 1 2))
        Assert.True(Result.isError (ScopedCoverage.create "authored documents" "" 1 2))

    [<Fact>]
    let ``TEK-INT-003 coverage rejects impossible ratios`` () =
        Assert.True(Result.isError (ScopedCoverage.create "authored documents" "declares id" 3 2))
        Assert.True(Result.isError (ScopedCoverage.create "authored documents" "declares id" -1 2))

    [<Fact>]
    let ``TEK-INT-003 coverage is described with its scope`` () =
        let coverage =
            ScopedCoverage.create "authored documents" "declares an id" 49 117
            |> Result.defaultWith failwith

        Assert.Equal("49 of 117 authored documents (declares an id)", ScopedCoverage.describe coverage)

    [<Fact>]
    let ``TEK-FID-002 known zero coverage is legal and distinct from no coverage`` () =
        Assert.True(Result.isOk (ScopedCoverage.create "declared claims" "has evidence" 0 0))

    [<Fact>]
    let ``TEK-STA-004 an unknown effect outcome is reconciled only by observation`` () =
        let unknown: EffectOutcome<string> = Unknown(AttemptId "deploy-1", "timeout")
        Assert.Equal(Succeeded "run-42", Reconciliation.reconcile unknown (ObservedSucceeded "run-42"))
        Assert.Equal(Failed "rejected", Reconciliation.reconcile unknown (ObservedFailed "rejected"))

        Assert.Equal(
            Unknown(AttemptId "deploy-1", "still pending"),
            Reconciliation.reconcile unknown (StillUnknown "still pending")
        )

    [<Fact>]
    let ``a recorded outcome is not overturned by a later observation`` () =
        let succeeded: EffectOutcome<string> = Succeeded "run-42"
        Assert.Equal(succeeded, Reconciliation.reconcile succeeded (ObservedFailed "late"))
