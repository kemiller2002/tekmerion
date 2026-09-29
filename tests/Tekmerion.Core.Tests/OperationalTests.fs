namespace Tekmerion.Core.Tests

open System
open System.IO
open System.Text.Json
open Xunit
open Tekmerion.Core
open Tekmerion.Cli
open Tekmerion.Core.Tests.Support

/// Operational proof of the first slice (GH-19), through the real host path.
module OperationalTests =

    let aegis () = fst (quietAegis ())

    let formaCss = Path.Combine(repositoryRoot, "node_modules", "@echelon-foundry", "design-system", "dist", "all.css")

    let ingestTo (out: string) =
        let options = { options (Some out) with Forma = (if File.Exists formaCss then Some formaCss else None) }
        Commands.ingest (aegis ()) options

    /// Relative path -> bytes for a whole output tree.
    let tree (root: string) =
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
        |> Seq.map (fun f -> Path.GetRelativePath(root, f).Replace('\\', '/'), File.ReadAllBytes f)
        |> Map.ofSeq

    let withoutRecord (files: Map<string, byte array>) = files |> Map.remove Contracts.PublicationRecord

    [<Fact>]
    let ``TEK-ARC-004 two full builds are byte-identical apart from the publication record`` () =
        let first = Path.Combine(tempDirectory (), "site")
        let second = Path.Combine(tempDirectory (), "site")
        Assert.Equal(ExitCode.Success, (ingestTo first).ExitCode)
        Assert.Equal(ExitCode.Success, (ingestTo second).ExitCode)
        let a = withoutRecord (tree first)
        let b = withoutRecord (tree second)
        Assert.Equal<string list>(a |> Map.keys |> List.ofSeq, b |> Map.keys |> List.ofSeq)

        for KeyValue(path, bytes) in a do
            Assert.True((bytes = b.[path]), $"{path} differs between builds")

    [<Fact>]
    let ``TEK-PUB-004 output is disposable: delete it and rebuild the same bytes`` () =
        let out = Path.Combine(tempDirectory (), "site")
        ingestTo out |> ignore
        let before = withoutRecord (tree out)
        Directory.Delete(out, true)
        Assert.Equal(ExitCode.Success, (ingestTo out).ExitCode)
        let after = withoutRecord (tree out)
        Assert.True((before = after))

    [<Fact>]
    let ``TEK-PUB-002 the manifest ties output to revision, version, schema and validation`` () =
        let out = Path.Combine(tempDirectory (), "site")
        ingestTo out |> ignore
        use manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(out, "data/v1/manifest.json")))
        let root = manifest.RootElement
        Assert.Equal("7ef65a35fced016a663bad6e3d74918c03eb208f", root.GetProperty("sourceRevision").GetString())
        Assert.Equal(Version.ContractSchema, root.GetProperty("schemaVersion").GetString())
        Assert.Equal(Version.Tekmerion, root.GetProperty("producer").GetProperty("version").GetString())
        Assert.Equal(0, root.GetProperty("validation").GetProperty("blocking").GetInt32())
        Assert.Equal(7, root.GetProperty("validation").GetProperty("warning").GetInt32())

    [<Fact>]
    let ``every file the manifest lists is on disk with the recorded hash and size`` () =
        let out = Path.Combine(tempDirectory (), "site")
        ingestTo out |> ignore
        use manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(out, "data/v1/manifest.json")))

        for entry in manifest.RootElement.GetProperty("files").EnumerateArray() do
            let bytes = File.ReadAllBytes(Path.Combine(out, entry.GetProperty("path").GetString()))
            Assert.Equal(entry.GetProperty("bytes").GetInt32(), bytes.Length)
            Assert.Equal(entry.GetProperty("sha256").GetString(), Hashing.sha256Bytes bytes)

    [<Fact>]
    let ``TEK-ARC-004 the publication time appears only in the publication record`` () =
        let out = Path.Combine(tempDirectory (), "site")
        ingestTo out |> ignore
        use record = JsonDocument.Parse(File.ReadAllText(Path.Combine(out, Contracts.PublicationRecord)))
        let publishedAt = record.RootElement.GetProperty("publishedAt").GetString()
        let minute = publishedAt.Substring(0, 16) // yyyy-MM-ddTHH:mm

        for KeyValue(path, bytes) in withoutRecord (tree out) do
            Assert.False(Text.Encoding.UTF8.GetString(bytes).Contains minute, $"{path} contains the publication time")

    let write (root: string) (relative: string) (text: string) =
        let full = Path.Combine(root, relative)
        Directory.CreateDirectory(Path.GetDirectoryName full) |> ignore
        File.WriteAllText(full, text)

    /// Every legitimate-but-awkward state at once, through the real CLI path.
    let adversarialRepository () =
        let root = tempDirectory ()
        write root "content/projects/p/research-note/malformed.md" "---\nid: [unclosed\n---\n# Still published\n"
        write root "content/projects/p/research-note/unknown.md" "---\nid: UNK-1\nfuture_key: {nested: [1, 2]}\n---\n# Unknown content\n"
        write root "content/projects/p/research-note/dangling.md" "---\nid: DNG-1\nrelated_documents:\n  - NOPE-404\n  - ../../../../../../etc/passwd\n---\n"
        write root "content/projects/p/research-note/contradictory.md" "---\nid: CON-1\ndocument_type: experiment_report\nartifactType: research-note\n---\n"
        write root "content/projects/p/research-note/bare.md" "no front matter at all\n\n<script>alert(1)</script>\n"
        write root "research/frontier/records/RFR-CYCLE001.md" "---\nid: RFR-CYCLE001\ndocument_type: research_frontier_record\n---\n## Evidence trace\n\n- Origin document: [u](../../../content/projects/p/research-note/unknown.md)\n\n## Dependencies\n\n- [RFR-CYCLE002](./RFR-CYCLE002.md)\n"
        write root "research/frontier/records/RFR-CYCLE002.md" "---\nid: RFR-CYCLE002\ndocument_type: research_frontier_record\n---\n## Evidence trace\n\n- Origin document: [u](../../../content/projects/p/research-note/unknown.md)\n\n## Dependencies\n\n- [RFR-CYCLE001](./RFR-CYCLE001.md)\n"
        write root "tekmerion.config.json" """{"schemaVersion":1,"repository":"example/adversarial","include":["content/**/*.md"],"frontier":{"records":"research/frontier/records/*.md"}}"""
        root

    let run root out = Commands.ingest (aegis ()) { ConfigPath = Path.Combine(root, "tekmerion.config.json"); Root = None; Out = Some out; Forma = None; Json = true }

    [<Fact>]
    let ``TEK-TST-005 malformed, unknown, dangling, contradictory and cyclic input publish with warnings`` () =
        let root = adversarialRepository ()
        let out = Path.Combine(root, "site")
        let result = run root out
        Assert.Equal(ExitCode.Success, result.ExitCode)
        let codes = result.Corpus.Value.Findings |> List.map (fun f -> Tekmerion.Domain.Policy.code f.Code) |> set

        for expected in [ "malformed-front-matter"; "unknown-key"; "dangling-reference"; "contradictory-metadata"; "no-front-matter"; "dependency-cycle" ] do
            Assert.Contains(expected, codes)

        Assert.True(File.Exists(Path.Combine(out, "a/UNK-1/index.html")))
        Assert.Contains("future_key", File.ReadAllText(Path.Combine(out, "a/UNK-1/index.html")))

        for KeyValue(_, bytes) in tree out do
            Assert.DoesNotContain("<script>alert", Text.Encoding.UTF8.GetString bytes)

    [<Fact>]
    let ``TEK-PUB-003 adding a duplicate id blocks and keeps the last known-good publication`` () =
        let root = adversarialRepository ()
        let out = Path.Combine(root, "site")
        Assert.Equal(ExitCode.Success, (run root out).ExitCode)
        let good = tree out
        write root "content/projects/p/research-note/duplicate.md" "---\nid: UNK-1\n---\n"
        let blocked = run root out
        Assert.Equal(ExitCode.Blocked, blocked.ExitCode)
        Assert.True((tree out = good), "the published output changed after a blocked build")

    [<Fact>]
    let ``TEK-REC-002 a build interrupted mid-promotion is recovered by the next build`` () =
        let root = adversarialRepository ()
        let out = Path.Combine(root, "site")
        run root out |> ignore
        // Simulate a crash after the live output was moved aside and before
        // the staged output replaced it, with stale staging left behind.
        Directory.Move(out, Output.previousOf out)
        Directory.CreateDirectory(Output.stagingOf out) |> ignore
        File.WriteAllText(Path.Combine(Output.stagingOf out, "partial.json"), "{")

        let result = run root out
        Assert.Equal(ExitCode.Success, result.ExitCode)
        Assert.NotEmpty(result.Published.Value.Recovered)
        Assert.False(File.Exists(Path.Combine(out, "partial.json")))
        Assert.False(Directory.Exists(Output.previousOf out))
        Assert.True(File.Exists(Path.Combine(out, "data/v1/manifest.json")))
