namespace Tekmerion.Domain.Tests

open Xunit
open Tekmerion.Domain
open Tekmerion.Domain.Tests.Fixtures

module IdentityTests =

    [<Theory>]
    [<InlineData("REP-BDE-0001")>]
    [<InlineData("EVREG-BDE-001")>]
    [<InlineData("EX-COMP-011")>]
    [<InlineData("RFR-B186B831")>]
    [<InlineData("ADR-WC-INDEX-0001")>]
    let ``TEK-IDY-001 every observed id grammar is accepted verbatim`` (raw: string) =
        let id = ArtifactId.create raw |> ok
        Assert.Equal(raw, ArtifactId.value id)

    [<Theory>]
    [<InlineData("")>]
    [<InlineData("has space")>]
    [<InlineData("a/b")>]
    [<InlineData("../escape")>]
    [<InlineData("<script>")>]
    let ``TEK-IDY-001 ids unusable as a URL segment are rejected, not repaired`` (raw: string) =
        Assert.True(Result.isError (ArtifactId.create raw))

    [<Theory>]
    [<InlineData("../outside.md")>]
    [<InlineData("content/../../outside.md")>]
    [<InlineData("/etc/passwd")>]
    [<InlineData("C:/Windows/file.md")>]
    let ``TEK-SEC-006 a repository path cannot escape the root`` (raw: string) =
        Assert.True(Result.isError (RepoPath.create raw))

    [<Fact>]
    let ``repository paths are normalised`` () =
        Assert.Equal("content/projects/a.md", RepoPath.value (path @".\content\.\projects\a.md"))

    [<Fact>]
    let ``TEK-IDY-002 a missing id yields a path-derived key, never a declared one`` () =
        let key = ArtifactKey.assign None (path "content/projects/composition-science/research-note/x.md")
        Assert.False(ArtifactKey.isDeclared key)
        Assert.StartsWith("/s/", ArtifactKey.url key)

    [<Fact>]
    let ``TEK-IDY-003 the path key depends only on the path`` () =
        let file = path "content/projects/composition-science/research-note/x.md"
        let first = PathKey.ofPath file
        let second = PathKey.ofPath (path "content/projects/composition-science/research-note/x.md")
        Assert.Equal(first, second)
        Assert.NotEqual(first, PathKey.ofPath (path "content/projects/composition-science/research-note/y.md"))
        Assert.Matches("^[0-9a-f]{16}$", PathKey.value first)

    [<Fact>]
    let ``TEK-IDY-004 a declared id is addressed at /a/{id}/`` () =
        Assert.Equal("/a/RP-COMP-005/", ArtifactKey.url rep)
        Assert.Equal(rep, ArtifactKey.assign (ArtifactId.create "RP-COMP-005" |> Result.toOption) (path "any.md"))
