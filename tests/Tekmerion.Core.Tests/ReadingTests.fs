namespace Tekmerion.Core.Tests

open Xunit
open Tekmerion.Domain
open Tekmerion.Core
open Tekmerion.Core.Tests.Support

module ReadingTests =

    let file = "content/projects/composition-science/research-note/example.md"

    [<Fact>]
    let ``TEK-ING-002 snake_case and camelCase keys are both read`` () =
        let snake = read file "---\ndocument_type: experiment_report\nrelated_documents:\n  - RP-COMP-005\n---\n"
        let camel = read file "---\nartifactType: experiment-report\nrelatedDocuments:\n  - RP-COMP-005\n---\n"

        for reading in [ snake; camel ] do
            Assert.Equal(ExperimentReport, reading.Artifact.Type)
            Assert.Equal(IdReference "RP-COMP-005", (reading.References |> List.exactlyOne).Value)

    [<Fact>]
    let ``TEK-IDY-002 a missing id yields a path key and a MissingId notice`` () =
        let reading = read file "---\ntitle: Example\n---\nbody"
        Assert.False(ArtifactKey.isDeclared reading.Artifact.Key)
        Assert.Equal(Absent Absence.NotDeclared, reading.Artifact.DeclaredIdText)
        Assert.Contains(reading.Findings, fun f -> f.Code = MissingId)

    [<Fact>]
    let ``TEK-IDY-003 the title never becomes identity`` () =
        let a = read file "---\ntitle: First title\n---\n"
        let b = read file "---\ntitle: Renamed completely\n---\n"
        Assert.Equal(a.Artifact.Key, b.Artifact.Key)

    [<Fact>]
    let ``TEK-FID-001 absent metadata stays absent`` () =
        let artifact = (read file "---\ntitle: Example\n---\n").Artifact

        for value in [ artifact.Date; artifact.Created; artifact.Updated; artifact.Project; artifact.Summary ] do
            Assert.Equal(Absent Absence.NotDeclared, value)

        Assert.Equal(Absent Absence.NotDeclared, artifact.Status)

    [<Fact>]
    let ``TEK-ING-003 unknown keys are preserved verbatim with their location`` () =
        let reading = read file "---\nllm_ingest: true\ncandidate_laws: [LAW-001, LAW-002]\n---\n"
        let laws = reading.Artifact.Extensions |> List.find (fun e -> e.Key = "candidate_laws")
        Assert.Equal("candidate_laws: [LAW-001, LAW-002]", laws.RawValue)
        Assert.Equal(Some 3, laws.Location.Line)
        Assert.Equal(2, reading.Artifact.Extensions.Length)

    [<Fact>]
    let ``TEK-VAL-007 contradictory type keys are both kept and reported`` () =
        let reading = read file "---\ndocument_type: experiment_report\nartifactType: research-note\n---\n"
        Assert.Contains(reading.Findings, fun f -> f.Code = ContradictoryMetadata [ "document_type"; "artifactType" ])
        Assert.Equal(DeclaredType("document_type", "experiment_report"), reading.Artifact.TypeSource)

    [<Fact>]
    let ``an undeclared type inferred from the directory says so`` () =
        let reading = read file "---\ntitle: x\n---\n"
        Assert.Equal(InferredFromDirectory "research-note", reading.Artifact.TypeSource)
        Assert.Contains(reading.Findings, fun f -> f.Code = MissingDeclaredType)

    [<Fact>]
    let ``an id that cannot be an address is kept as text and reported`` () =
        let reading = read file "---\nid: has space\n---\n"
        Assert.Equal(Known "has space", reading.Artifact.DeclaredIdText)
        Assert.False(ArtifactKey.isDeclared reading.Artifact.Key)
        Assert.Contains(reading.Findings, fun f -> match f.Code with InvalidDeclaredId _ -> true | _ -> false)

    [<Fact>]
    let ``TEK-ING-006 references are bibliography, never links`` () =
        let reading = read file "---\nreferences:\n- EVD-001\n- Shannon, C. E. (1948)\n---\n"
        Assert.Equal<string list>([ "EVD-001"; "Shannon, C. E. (1948)" ], reading.Artifact.Bibliography)
        Assert.Empty reading.References

    [<Fact>]
    let ``null supersession is a legitimate state, not a reference`` () =
        let reading = read file "---\nsupersedes: null\nsuperseded_by: null\n---\n"
        Assert.Empty reading.References
        Assert.DoesNotContain(reading.Findings, fun f -> Policy.severity f.Code <> Informational)

    [<Fact>]
    let ``summary wins but an abstract is preserved, not dropped`` () =
        let reading = read file "---\nsummary: S\nabstract: A\n---\n"
        Assert.Equal(Known "S", reading.Artifact.Summary)
        Assert.Contains(reading.Artifact.Extensions, fun e -> e.Key = "abstract")

    [<Fact>]
    let ``sections keep heading paths and ignore headings inside code fences`` () =
        let reading = read file "---\nid: X-1\n---\n# Top\ntext\n```\n# not a heading\n```\n## Child\nmore"
        let headings = reading.Artifact.Sections |> List.map (fun s -> s.Heading)
        Assert.Equal<string list>([ "Top"; "Child" ], headings)
        let child = reading.Artifact.Sections |> List.last
        Assert.Equal<string list>([ "Top"; "Child" ], child.Location.HeadingPath)
        Assert.Equal(Some 9, child.Location.Line)
