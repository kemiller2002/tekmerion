namespace Tekmerion.Domain.Tests

open Xunit
open Tekmerion.Domain
open Tekmerion.Domain.Tests.Fixtures

module RelationshipTests =

    [<Fact>]
    let ``TEK-REL-002 TEK-REL-003 each backlink carries the canonical edge that justifies it`` () =
        let edge = sourceRepEdge ex011
        let backlink = Derivation.backlinks [ edge ] |> List.exactlyOne
        Assert.Equal(rep, backlink.From)
        Assert.Equal(ex011, backlink.To)
        Assert.Equal(Backlink SourceRep, backlink.Relation)
        Assert.Equal(edge, NonEmpty.head backlink.Basis)
        Assert.Equal(Some "source_rep", (NonEmpty.head backlink.Basis).Declared.FrontMatterKey)

    [<Fact>]
    let ``TEK-FID-001 dangling and non-link references derive nothing`` () =
        let dangling = { sourceRepEdge ex011 with Target = Dangling(IdReference "RP-COMP-999") }
        let prose = { sourceRepEdge ex012 with Target = NotALink "Composition Science Library" }
        Assert.Empty(Derivation.backlinks [ dangling; prose ])

    [<Fact>]
    let ``TEK-ARC-004 backlink derivation is order independent`` () =
        let edges = [ sourceRepEdge ex012; sourceRepEdge ex011 ]
        Assert.Equal<DerivedEdge list>(Derivation.backlinks edges, Derivation.backlinks (List.rev edges))

    [<Fact>]
    let ``TEK-INT-002 an acyclic prerequisite graph has no cycles`` () =
        Assert.Empty(Cycles.find [ "a", "b"; "b", "c"; "a", "c" ])

    [<Fact>]
    let ``TEK-INT-002 cycles are reported as sorted components`` () =
        let cycles = Cycles.find [ "c", "a"; "a", "b"; "b", "c"; "c", "d"; "e", "e" ]
        Assert.Equal<string list list>([ [ "a"; "b"; "c" ]; [ "e" ] ], cycles)

    [<Fact>]
    let ``TEK-INT-004 effective-current follows supersession and keeps history`` () =
        let successor =
            function
            | "v1" -> Some "v2"
            | "v2" -> Some "v3"
            | _ -> None

        match Supersession.effectiveCurrent successor "v1" with
        | Ok chain ->
            Assert.Equal("v3", chain.Current)
            Assert.Equal<string list>([ "v1"; "v2" ], chain.History)
        | Error cycle -> failwithf "unexpected cycle %A" cycle

    [<Fact>]
    let ``a supersession cycle is reported, not resolved`` () =
        let successor =
            function
            | "a" -> Some "b"
            | "b" -> Some "a"
            | _ -> None

        Assert.Equal(Error [ "a"; "b"; "a" ], Supersession.effectiveCurrent successor "a")
