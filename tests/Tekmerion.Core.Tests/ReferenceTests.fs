namespace Tekmerion.Core.Tests

open Xunit
open Tekmerion.Domain
open Tekmerion.Core
open Tekmerion.Core.Tests.Support

module ReferenceTests =

    [<Theory>]
    [<InlineData("RP-COMP-005")>]
    [<InlineData("RFR-B186B831")>]
    [<InlineData("SPEC-001")>]
    let ``declared id forms are id references`` (raw: string) =
        Assert.Equal(IdReference raw, References.classify raw)

    [<Fact>]
    let ``TEK-FID-003 prose stays prose, never a guessed link`` () =
        Assert.Equal(ProseTitle "Composition Science Research Library", References.classify "Composition Science Research Library")

    [<Fact>]
    let ``markdown links are classified by their target`` () =
        Assert.Equal(FileRelativePath "./RFR-544ACDA1.md", References.classify "[RFR-544ACDA1](./RFR-544ACDA1.md)")

    [<Fact>]
    let ``external URLs are links but not artifact references`` () =
        Assert.Equal(ExternalUrl "https://example.org/paper", References.classify "https://example.org/paper")

    [<Fact>]
    let ``file-relative paths resolve against the declaring directory`` () =
        let candidates =
            References.candidatePaths
                (path "research/frontier/records/RFR-DEB5927A.md")
                (FileRelativePath "../../../content/projects/composition-science/research-execution-package/rp-comp-005-visual-scene-construction.md")

        Assert.Equal<string list>(
            [ "content/projects/composition-science/research-execution-package/rp-comp-005-visual-scene-construction.md" ],
            candidates |> List.map RepoPath.value
        )

    [<Fact>]
    let ``TEK-SEC-006 references escaping the repository produce no candidate`` () =
        Assert.Empty(References.candidatePaths (path "content/a.md") (FileRelativePath "../../../../etc/passwd"))

    [<Fact>]
    let ``fragments are ignored for resolution`` () =
        let candidates = References.candidatePaths (path "content/a/b.md") (FileRelativePath "./c.md#section")
        Assert.Equal<string list>([ "content/a/c.md" ], candidates |> List.map RepoPath.value)
