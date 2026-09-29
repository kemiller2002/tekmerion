namespace Tekmerion.Domain.Tests

open Xunit
open Tekmerion.Domain

module ArtifactTests =

    [<Theory>]
    [<InlineData("experiment_report")>]
    [<InlineData("experiment-report")>]
    [<InlineData("Experiment_Report")>]
    let ``TEK-ING-002 snake and kebab casings of a type are one type`` (raw: string) =
        Assert.Equal(ExperimentReport, ArtifactType.classify raw)

    [<Fact>]
    let ``an unrecognised type is kept verbatim, not coerced`` () =
        Assert.Equal(Other "crosswalk", ArtifactType.classify "crosswalk")

    [<Fact>]
    let ``TEK-FID-004 the verbatim status survives beside its derived reading`` () =
        let reading = StatusReading.read "computational-pilot-complete-human-study-pending"
        Assert.Equal("computational-pilot-complete-human-study-pending", reading.Text)
        Assert.Equal(Complete, reading.Class)
        Assert.Equal<string list>([ "human-study-pending" ], reading.Outstanding)

    [<Theory>]
    [<InlineData("Applied Analysis — Working Draft")>]
    [<InlineData("canonical-candidate")>]
    [<InlineData("verified")>]
    let ``free-text statuses are never rejected`` (text: string) =
        Assert.Equal(text, (StatusReading.read text).Text)

    [<Fact>]
    let ``an unfamiliar status is Unclassified rather than guessed`` () =
        Assert.Equal(Unclassified, (StatusReading.read "zeta").Class)

    [<Fact>]
    let ``a candidate is not read as complete`` () =
        Assert.Equal(Active, (StatusReading.read "canonical-candidate").Class)
