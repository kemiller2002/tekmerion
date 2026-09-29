namespace Tekmerion.Core.Tests

open Xunit
open Tekmerion.Domain
open Tekmerion.Core
open Tekmerion.Core.Tests.Support

/// Malformed, hostile and contradictory input (TEK-TST-005). Built from small
/// documents shaped like the real corpus; canonical research is not altered.
module AdversarialTests =

    let source file text = { Path = path file; Text = text }

    let ingest (files: SourceFile list) =
        Corpus.ingest
            { Repository = repository
              Included = files
              FrontierCandidates = []
              RepositoryFiles = files |> List.map (fun f -> RepoPath.value f.Path) |> set }

    let codes (corpus: Corpus) = corpus.Findings |> List.map (fun f -> Policy.code f.Code)

    [<Fact>]
    let ``TEK-IDY-005 a duplicated declared id blocks publication`` () =
        let corpus =
            ingest
                [ source "content/projects/p/experiment-report/a.md" "---\nid: EX-COMP-011\n---\n"
                  source "content/projects/p/experiment-report/b.md" "---\nid: EX-COMP-011\n---\n" ]

        Assert.Equal("unpublishable", Publication.stateName corpus.State)
        Assert.Equal(2, codes corpus |> List.filter ((=) "duplicate-id") |> List.length)

    [<Fact>]
    let ``TEK-ING-005 malformed front matter is quarantined, not fatal`` () =
        let corpus =
            ingest
                [ source "content/projects/p/research-note/bad.md" "---\nid: [unclosed\n---\n# Still readable\n"
                  source "content/projects/p/research-note/good.md" "---\nid: GOOD-1\n---\n" ]

        Assert.Contains("malformed-front-matter", codes corpus)
        Assert.Equal("publishable-with-warnings", Publication.stateName corpus.State)
        let bad = corpus.Artifacts |> List.find (fun a -> RepoPath.value a.Reading.Artifact.Location.Path = "content/projects/p/research-note/bad.md")
        Assert.Equal<string list>([ "Still readable" ], bad.Reading.Artifact.Sections |> List.map (fun s -> s.Heading))

    [<Fact>]
    let ``TEK-SEC-001 script in research is carried as escaped text, never markup`` () =
        let corpus = ingest [ source "content/projects/p/research-note/x.md" "---\nid: X-1\ntitle: \"<script>alert(1)</script>\"\n---\n<script>alert(2)</script>\n" ]

        for file in Contracts.project corpus do
            Assert.DoesNotContain("<script>", file.Text)

        let content = Contracts.project corpus |> List.find (fun f -> f.Path = "data/v1/content/a/X-1.json")
        Assert.Contains("\\u003cscript\\u003ealert(2)", content.Text)

    [<Fact>]
    let ``TEK-SEC-006 a traversal reference resolves to nothing and is reported`` () =
        let corpus = ingest [ source "content/projects/p/research-note/x.md" "---\nid: X-1\nrelated_documents:\n  - ../../../../../etc/passwd\n---\n" ]
        Assert.Contains("dangling-reference", codes corpus)

    [<Fact>]
    let ``TEK-INT-002 a declared prerequisite cycle is reported, not resolved`` () =
        let record id dependency =
            source
                $"research/frontier/records/{id}.md"
                $"---\nid: {id}\ndocument_type: research_frontier_record\n---\n# {id}\n\n## Dependencies\n\n- [{dependency}](./{dependency}.md)\n"

        let corpus = ingest [ record "RFR-AAAA0001" "RFR-AAAA0002"; record "RFR-AAAA0002" "RFR-AAAA0001" ]
        let cycle = corpus.Findings |> List.find (fun f -> Policy.code f.Code = "dependency-cycle")
        Assert.Equal(DependencyCycle [ "RFR-AAAA0001"; "RFR-AAAA0002" ], cycle.Code)
        Assert.Equal("publishable-with-warnings", Publication.stateName corpus.State)

    [<Fact>]
    let ``references to real files outside the published scope are not dangling`` () =
        let corpus =
            Corpus.ingest
                { Repository = repository
                  Included = [ source "content/projects/p/research-note/x.md" "---\nid: X-1\nrelated_documents:\n  - content/concepts/composition-index.md\n---\n" ]
                  FrontierCandidates = []
                  RepositoryFiles = set [ "content/projects/p/research-note/x.md"; "content/concepts/composition-index.md" ] }

        Assert.Contains("out-of-scope-reference", codes corpus)
        Assert.DoesNotContain("dangling-reference", codes corpus)

    [<Fact>]
    let ``an empty corpus is NothingToPublish, not a failure`` () =
        Assert.Equal(NothingToPublish, (ingest []).State)

    [<Fact>]
    let ``frontier records outside the scope are not published`` () =
        let corpus =
            Corpus.ingest
                { Repository = repository
                  Included = [ source "content/projects/p/research-note/x.md" "---\nid: X-1\n---\n" ]
                  FrontierCandidates =
                    [ source "research/frontier/records/RFR-IN000001.md" "---\nid: RFR-IN000001\ndocument_type: research_frontier_record\n---\n## Evidence trace\n\n- Origin document: [x](../../../content/projects/p/research-note/x.md)\n"
                      source "research/frontier/records/RFR-OUT00001.md" "---\nid: RFR-OUT00001\ndocument_type: research_frontier_record\n---\n## Evidence trace\n\n- Origin document: [y](../../../content/projects/q/y.md)\n" ]
                  RepositoryFiles = set [] }

        Assert.Equal<string list>(
            [ "RFR-IN000001"; "X-1" ],
            corpus.Artifacts |> List.map (fun a -> ArtifactKey.value a.Reading.Artifact.Key) |> List.sort
        )
