namespace Tekmerion.Core.Tests

open System
open System.Text.RegularExpressions
open Xunit
open Tekmerion.Domain
open Tekmerion.Core
open Tekmerion.Core.Tests.Support

/// The static research experience on the real slice (GH-18).
module SiteTests =

    let legacy =
        lazy
            (let json = System.IO.File.ReadAllText(System.IO.Path.Combine(fixtureRoot, "legacy-catalog.json"))
             use document = System.Text.Json.JsonDocument.Parse json

             document.RootElement.GetProperty("records").EnumerateArray()
             |> Seq.map (fun r -> r.GetProperty("url").GetString().Substring("/Visual-Engineering/".Length), r.GetProperty("sourcePath").GetString())
             |> List.ofSeq)

    let options =
        { Title = "Visual Engineering Research"
          FormaStylesheet = Some "assets/forma/all.css"
          SourceLink = fun l -> l.Commit |> Option.map (fun c -> $"https://github.com/kemiller2002/visual-engineering/blob/{c}/{RepoPath.value l.Path}")
          LegacyUrls = legacy.Force() }

    let site = lazy (Site.render (golden.Force()) options)
    let pages () = fst (site.Force()) |> List.map (fun f -> f.Path, f.Text) |> Map.ofList
    let page path = (pages ()).[path]

    let hrefs (html: string) =
        Regex.Matches(html, "href=\"([^\"]*)\"") |> Seq.map (fun m -> m.Groups.[1].Value) |> List.ofSeq

    /// Resolve a relative href against a page path to an output file path.
    let resolve (fromPage: string) (href: string) =
        let target = href.Split('#').[0]
        let baseDir = fromPage.Split('/') |> Array.rev |> Array.skip 1 |> Array.rev |> List.ofArray

        let parts =
            target.Split('/')
            |> Array.fold (fun (acc: string list) part -> match part with ".." -> List.take (acc.Length - 1) acc | "." | "" -> acc | p -> acc @ [ p ]) baseDir
            |> fun segments -> segments

        let joined = String.Join("/", parts)
        if target.EndsWith "/" || target = "" then (if joined = "" then "index.html" else joined + "/index.html") else joined

    [<Fact>]
    let ``TEK-EXP-001 every page is script-free semantic HTML`` () =
        for KeyValue(path, html) in pages () do
            Assert.DoesNotContain("<script", html)
            Assert.Contains("<html lang=\"en\">", html)

            if not (path.StartsWith "research/") then
                Assert.Contains("<main id=\"main\"", html)
                Assert.Equal(1, Regex.Matches(html, "<h1[ >]").Count)

    [<Fact>]
    let ``the main stack spaces its children once (Forma 0.3.0+ owl margins zeroed)`` () =
        for KeyValue(path, html) in pages () do
            if not (path.StartsWith "research/") then
                Assert.Contains("<main id=\"main\" class=\"ef-stack\" style=\"--ef-stack-space: 0\">", html)

    [<Fact>]
    let ``every artifact has exactly one page at its stable address`` () =
        for ingested in golden.Force().Artifacts do
            Assert.True((pages ()).ContainsKey(Site.pageOf ingested.Reading.Artifact.Key))

    [<Fact>]
    let ``TEK-EXP-006 every internal link resolves to a generated file`` () =
        let all = pages ()
        let external (href: string) = href.StartsWith "http" || href.StartsWith "mailto:" || href.StartsWith "#"

        let broken =
            [ for KeyValue(path, html) in all do
                  for href in hrefs html do
                      if not (external href) then
                          let target = resolve path href

                          if not (all.ContainsKey target) && not (target.StartsWith "data/v1/") && target <> "assets/forma/all.css" then
                              yield $"{path} -> {href}" ]

        Assert.Empty broken

    [<Fact>]
    let ``TEK-EXP-006 experiment report links to its research package and back`` () =
        let ex = page "a/EX-COMP-011/index.html"
        Assert.Contains("href=\"../RP-COMP-005/\"", ex)
        let rep = page "a/RP-COMP-005/index.html"
        Assert.Contains("href=\"../EX-COMP-011/\"", rep)

    [<Fact>]
    let ``TEK-REL-003 each derived link explains itself with its canonical declaration`` () =
        let rep = page "a/RP-COMP-005/index.html"
        Assert.Equal(9, Regex.Matches(rep, "Why is this shown\\?").Count)
        // The source_rep backlink names the declaring file, key and line.
        Assert.Contains("ex-comp-011-context-reliability-reversal.md:10 (front-matter key source_rep)", rep.Replace("<wbr>", ""))
        Assert.Contains("Frontier records originating here", rep)

    [<Fact>]
    let ``TEK-EXP-005 canonical and derived relationships are separate sections`` () =
        let rep = page "a/RP-COMP-005/index.html"
        let declared = rep.IndexOf "Declared by this artifact (canonical)"
        let derived = rep.IndexOf "Pointing here (derived for navigation)"
        Assert.True(declared > 0 && derived > declared)

    [<Fact>]
    let ``TEK-FID-001 absent metadata is shown as not declared, never filled in`` () =
        let rep = page "a/RP-COMP-005/index.html"
        Assert.Contains("<dt>Created</dt><dd><em>not declared</em></dd>", rep)
        Assert.DoesNotContain("2026-07-22", String.Join("", (pages ()).Values))

    [<Fact>]
    let ``TEK-IDY-003 no page address is derived from a title`` () =
        let artifactPages = (pages ()).Keys |> Seq.filter (fun p -> p.StartsWith "a/" || p.StartsWith "s/") |> List.ofSeq
        Assert.Equal(golden.Force().Artifacts.Length, artifactPages.Length)
        Assert.All(artifactPages, fun p -> Assert.Matches("^(a/[A-Za-z0-9._-]+|s/[0-9a-f]{16})/index\\.html$", p))

    [<Fact>]
    let ``TEK-IDY-006 every previously published slice URL redirects to its stable address`` () =
        let _, lost = site.Force()
        Assert.Empty lost
        let all = pages ()

        for legacyPath, sourcePath in legacy.Force() do
            let stubPath = legacyPath.TrimEnd('/') + "/index.html"
            let stub = all.[stubPath]
            let target = Regex.Match(stub, "url=([^\"]+)\"").Groups.[1].Value
            let key = golden.Force().Artifacts |> List.find (fun a -> RepoPath.value a.Reading.Artifact.Location.Path = sourcePath)
            Assert.Equal(Site.pageOf key.Reading.Artifact.Key, resolve stubPath target)

    [<Fact>]
    let ``TEK-PRV-002 source provenance links to the pinned revision`` () =
        let rep = page "a/RP-COMP-005/index.html"
        Assert.Contains("https://github.com/kemiller2002/visual-engineering/blob/7ef65a35fced016a663bad6e3d74918c03eb208f/content/projects/composition-science/research-execution-package/rp-comp-005-visual-scene-construction.md", rep)

    [<Fact>]
    let ``TEK-EXP-004 the project page shows purpose, frontier, position, in-flight work and integrity`` () =
        let project = page "p/composition-science/index.html"

        for heading in [ "Purpose and where to start"; "Open frontier"; "Current research position"; "In-flight work and obligations"; "Integrity" ] do
            Assert.Contains(heading, project)

        Assert.DoesNotContain("Recent activity", project)

    [<Fact>]
    let ``TEK-ARC-004 site rendering is deterministic`` () =
        Assert.Equal<OutputFile list>(fst (Site.render (golden.Force()) options), fst (Site.render (golden.Force()) options))

    [<Fact>]
    let ``TEK-SEC-001 hostile markdown is escaped and unsafe links are dropped`` () =
        let corpus =
            Corpus.ingest
                { Repository = repository
                  Included =
                    [ { Path = path "content/projects/p/research-note/x.md"
                        Text = "---\nid: X-1\n---\n# Body\n<script>alert(1)</script>\n\n[click](javascript:alert(2)) [ok](https://example.org)\n" } ]
                  FrontierCandidates = []
                  RepositoryFiles = set [ "content/projects/p/research-note/x.md" ]
                  Revision = None }

        let html = (fst (Site.render corpus { options with LegacyUrls = [] })) |> List.find (fun f -> f.Path = "a/X-1/index.html")
        Assert.DoesNotContain("<script>", html.Text)
        Assert.DoesNotContain("javascript:", html.Text)
        Assert.Contains("&lt;script&gt;", html.Text)
        Assert.Contains("href=\"https://example.org\" rel=\"noopener noreferrer\"", html.Text)
