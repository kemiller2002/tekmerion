namespace Tekmerion.Domain.Tests

open Xunit
open Tekmerion.Domain

module KnowledgeTests =

    [<Fact>]
    let ``TEK-FID-002 known zero is not absence`` () =
        Assert.NotEqual(Known 0, Absent Absence.NotDeclared)
        Assert.True(Knowable.isKnown (Known 0))

    [<Fact>]
    let ``TEK-FID-002 the absence kinds stay distinct`` () =
        let kinds =
            [ Absence.NotDeclared
              Absence.Unknown
              Absence.Unavailable "runtime does not expose it"
              Absence.NotApplicable "no chronology for frontier records" ]

        Assert.Equal(kinds.Length, kinds |> List.distinct |> List.length)

    [<Fact>]
    let ``TEK-FID-001 an absent option becomes NotDeclared, never a default`` () =
        Assert.Equal(Absent Absence.NotDeclared, Knowable.ofOption (None: string option))
        Assert.Equal(Known "2026-07-21", Knowable.ofOption (Some "2026-07-21"))

    [<Fact>]
    let ``mapping preserves the reason for absence`` () =
        let absent: Knowable<int> = Absent(Absence.Unavailable "offline")
        Assert.Equal(Absent(Absence.Unavailable "offline"), Knowable.map string absent)

    [<Fact>]
    let ``NonEmpty cannot be built from an empty list`` () =
        Assert.True((NonEmpty.create ([]: int list)).IsNone)
        Assert.Equal<int list>([ 1; 2 ], NonEmpty.create [ 1; 2 ] |> Option.get |> NonEmpty.toList)
