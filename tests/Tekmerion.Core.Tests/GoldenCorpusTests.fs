namespace Tekmerion.Core.Tests

open System.Text
open Xunit
open Tekmerion.Domain
open Tekmerion.Core
open Tekmerion.Core.Tests.Support

/// The first vertical slice on the pinned real corpus
/// (visual-engineering@7ef65a3, composition-science). Numbers here are
/// measured from that snapshot, not targets; a change in them is a change in
/// behaviour that must be explained (TEK-TST-003).
module GoldenCorpusTests =

    let corpus () = golden.Force()

    let authored () = corpus().Artifacts |> List.filter (fun a -> a.Population = AuthoredResearch)

    [<Fact>]
    let ``GH-17 all 23 slice documents and their 55 frontier records are read`` () =
        Assert.Equal(23, authored().Length)
        Assert.Equal(55, corpus().Artifacts |> List.filter (fun a -> a.Population = MachineGenerated) |> List.length)

    [<Fact>]
    let ``GH-17 no slice document fails to parse`` () =
        let states = authored () |> List.countBy (fun a -> a.Reading.FrontMatter) |> Map.ofList
        Assert.Equal(22, states.[FrontMatterPresent])
        Assert.Equal(1, states.[FrontMatterAbsent]) // the archived duplicate, as discovery found
        Assert.False(states.ContainsKey(FrontMatterMalformed ""))

    [<Fact>]
    let ``TEK-IDY-001 declared ids remain declared and missing ids remain missing`` () =
        let declared =
            authored ()
            |> List.choose (fun a -> match a.Reading.Artifact.Key with Declared id -> Some(ArtifactId.value id) | _ -> None)
            |> List.sort

        Assert.Equal<string list>(
            [ "DF-COMP-002"; "EX-COMP-011"; "EX-COMP-012"; "GOV-001"; "ONT-001"; "RP-COMP-005"; "RPT-GN100-001"; "SPEC-001"; "TH-COMP-005" ],
            declared
        )

        Assert.Equal(14, authored () |> List.filter (fun a -> not (ArtifactKey.isDeclared a.Reading.Artifact.Key)) |> List.length)

    [<Fact>]
    let ``TEK-REL-001 the four source_rep edges resolve to RP-COMP-005`` () =
        let rep = (artifactById (corpus ()) "RP-COMP-005").Key

        let sources =
            corpus().Canonical
            |> List.filter (fun e -> e.Relation = SourceRep)
            |> List.map (fun e ->
                Assert.Equal(Resolves rep, e.Target)
                ArtifactKey.value e.From)
            |> List.sort

        Assert.Equal<string list>([ "DF-COMP-002"; "EX-COMP-011"; "EX-COMP-012"; "TH-COMP-005" ], sources)

    [<Fact>]
    let ``GH-17 canonical edges by relation match the snapshot`` () =
        let counts = corpus().Canonical |> List.countBy (fun e -> CanonicalRelation.label e.Relation) |> Map.ofList
        Assert.Equal(55, counts.["origin-document"])
        Assert.Equal(33, counts.["prerequisite"])
        Assert.Equal(5, counts.["related-document"])
        Assert.Equal(4, counts.["source-rep"])
        Assert.Equal(7, counts.["typed-link:tests"])

    [<Fact>]
    let ``TEK-REL-002 TEK-REL-003 every derived edge is a backlink of a resolved canonical edge`` () =
        let resolved = corpus().Canonical |> List.filter (fun e -> match e.Target with Resolves _ -> true | _ -> false)
        Assert.Equal(resolved.Length, corpus().Derived.Length)

        for edge in corpus().Derived do
            let basis = NonEmpty.head edge.Basis
            Assert.Contains(basis, resolved)
            Assert.Equal(Resolves edge.From, basis.Target)
            Assert.Equal(basis.From, edge.To)

    [<Fact>]
    let ``TEK-EXP-006 RP-COMP-005 reaches its experiments and its originating frontier records`` () =
        let rep = (artifactById (corpus ()) "RP-COMP-005").Key
        let backlinks = corpus().Derived |> List.filter (fun e -> e.From = rep)
        let sourceRep = backlinks |> List.filter (fun e -> e.Relation = Backlink SourceRep) |> List.map (fun e -> ArtifactKey.value e.To)
        let frontier = backlinks |> List.filter (fun e -> e.Relation = Backlink OriginDocument)
        Assert.Equal<string list>([ "DF-COMP-002"; "EX-COMP-011"; "EX-COMP-012"; "TH-COMP-005" ], List.sort sourceRep)
        Assert.Equal(5, frontier.Length)
        Assert.All(frontier, fun e -> Assert.StartsWith("RFR-", ArtifactKey.value e.To))

    [<Fact>]
    let ``TEK-VAL-003 the slice is publishable with warnings and nothing blocks`` () =
        Assert.Equal("publishable-with-warnings", Publication.stateName (corpus().State))
        Assert.Empty(Assessment.blocking (corpus().Assessment))

    [<Fact>]
    let ``TEK-REL-004 the seven unresolved tests ids are visible warnings`` () =
        let dangling = corpus().Findings |> List.filter (fun f -> match f.Code with DanglingReference _ -> true | _ -> false)
        Assert.Equal(7, dangling.Length)
        Assert.All(dangling, fun f -> Assert.Equal(Some 11, f.Location |> Option.bind (fun l -> l.Line)))

    [<Fact>]
    let ``TEK-FID-001 the template's placeholder date is kept verbatim`` () =
        let template =
            authored ()
            |> List.map (fun a -> a.Reading.Artifact)
            |> List.find (fun a -> RepoPath.value a.Location.Path = "content/projects/composition-science/template/composition-science-markdown-template-v1.md")

        Assert.Equal(Known "YYYY-MM-DD", template.Date)

    [<Fact>]
    let ``TEK-FID-001 no legacy default appears anywhere in the contracts`` () =
        let text = Contracts.project (corpus ()) |> List.map (fun f -> f.Text) |> String.concat "\n"
        Assert.DoesNotContain("2026-07-22", text)
        Assert.DoesNotContain("General Research", text)
        Assert.DoesNotContain("\"author\":\"unknown\"", text)

    [<Fact>]
    let ``TEK-ING-003 unknown keys survive into the artifact contract`` () =
        let rep = artifactById (corpus ()) "RP-COMP-005"
        Assert.Contains(rep.Extensions, fun e -> e.Key = "source_package")

        let detail =
            Contracts.project (corpus ())
            |> List.find (fun f -> f.Path = "data/v1/artifact/a/RP-COMP-005.json")

        Assert.Contains("\"key\":\"source_package\"", detail.Text)

    [<Fact>]
    let ``TEK-CON-003 full context for RP-COMP-005 is two fetches under 20 KB`` () =
        let files = Contracts.project (corpus ()) |> List.map (fun f -> f.Path, f.Text) |> Map.ofList
        let detail = files.["data/v1/artifact/a/RP-COMP-005.json"]
        let edges = files.["data/v1/edges/a/RP-COMP-005.json"]
        let bytes = Encoding.UTF8.GetByteCount detail + Encoding.UTF8.GetByteCount edges
        Assert.True(bytes <= 20 * 1024, $"{bytes} bytes")
        Assert.Contains("\"data/v1/edges/a/RP-COMP-005.json\"", detail)

    [<Fact>]
    let ``TEK-CON-002 no contract contains rendered HTML`` () =
        for file in Contracts.project (corpus ()) do
            Assert.DoesNotContain("<p>", file.Text)
            Assert.DoesNotContain("<div", file.Text)

    [<Fact>]
    let ``TEK-ARC-004 two independent ingests project byte-identical contracts`` () =
        let aegis, _ = quietAegis ()
        let again = match Tekmerion.Cli.Commands.load aegis (options None) with Ok c -> c | Error r -> failwithf "%A" r.Messages
        Assert.Equal<OutputFile list>(Contracts.project (corpus ()), Contracts.project again)

    [<Fact>]
    let ``TEK-ARC-004 ingest does not depend on input order`` () =
        let aegis, _ = quietAegis ()
        let reloaded = match Tekmerion.Cli.Commands.load aegis (options None) with Ok c -> c | Error r -> failwithf "%A" r.Messages

        let reversed =
            Corpus.ingest
                { Repository = repository
                  Included = reloaded.Artifacts |> List.filter (fun a -> a.Population = AuthoredResearch) |> List.map (fun a -> a.Reading.Artifact.Location.Path) |> List.rev |> List.map (fun p -> { Path = p; Text = System.IO.File.ReadAllText(System.IO.Path.Combine(fixtureRoot, RepoPath.value p)) })
                  FrontierCandidates = reloaded.Artifacts |> List.filter (fun a -> a.Population = MachineGenerated) |> List.map (fun a -> a.Reading.Artifact.Location.Path) |> List.rev |> List.map (fun p -> { Path = p; Text = System.IO.File.ReadAllText(System.IO.Path.Combine(fixtureRoot, RepoPath.value p)) })
                  RepositoryFiles = System.IO.File.ReadAllLines(System.IO.Path.Combine(fixtureRoot, "repository-files.txt")) |> Set.ofArray }

        Assert.Equal<OutputFile list>(Contracts.project reloaded, Contracts.project reversed)

    [<Fact>]
    let ``TEK-PRV-001 every canonical edge points to its declaring line`` () =
        for edge in corpus().Canonical do
            Assert.True(edge.Declared.Line.IsSome, CanonicalRelation.label edge.Relation)
            Assert.True(System.IO.File.Exists(System.IO.Path.Combine(fixtureRoot, RepoPath.value edge.Declared.Path)))

    [<Fact>]
    let ``TEK-ING-008 authored and machine-generated populations are counted apart`` () =
        let manifest = Contracts.project (corpus ()) |> List.find (fun f -> f.Path = "data/v1/manifest.json")
        Assert.Contains("\"authored\": 23", manifest.Text)
        Assert.Contains("\"machine-generated\": 55", manifest.Text)
