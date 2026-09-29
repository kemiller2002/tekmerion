namespace Tekmerion.Core.Tests

open System.IO
open Xunit
open Tekmerion.Cli
open Tekmerion.Core.Tests.Support

/// Last-known-good publication and interrupted-build recovery
/// (TEK-PUB-003, TEK-REC-002), exercised on the real filesystem in a temp dir.
module OutputTests =

    let aegis () = fst (quietAegis ())

    let file relative text : Tekmerion.Core.OutputFile = { Path = relative; Text = text }

    let expectOk result =
        match result with
        | Ok value -> value
        | Error fault -> failwithf "%s" (Faults.describe fault)

    [<Fact>]
    let ``staged output replaces the live output only on promotion`` () =
        let out = Path.Combine(tempDirectory (), "site")
        Directory.CreateDirectory out |> ignore
        File.WriteAllText(Path.Combine(out, "old.txt"), "old")
        let config = aegis ()

        Output.stage config out [ file "data/v1/manifest.json" "{}\n" ] |> expectOk |> ignore
        Assert.True(File.Exists(Path.Combine(out, "old.txt")), "live output changed before promotion")

        Output.promote config out |> expectOk
        Assert.False(File.Exists(Path.Combine(out, "old.txt")))
        Assert.Equal("{}\n", File.ReadAllText(Path.Combine(out, "data/v1/manifest.json")))
        Assert.False(Directory.Exists(Output.stagingOf out))
        Assert.False(Directory.Exists(Output.previousOf out))

    [<Fact>]
    let ``TEK-REC-002 an interrupted promotion is rolled back on the next run`` () =
        let out = Path.Combine(tempDirectory (), "site")
        // State after the first rename and before the second: live moved aside.
        Directory.CreateDirectory(Output.previousOf out) |> ignore
        File.WriteAllText(Path.Combine(Output.previousOf out, "index.html"), "last known good")
        Directory.CreateDirectory(Output.stagingOf out) |> ignore

        let recovered = Output.recover (aegis ()) out |> expectOk
        Assert.Equal("last known good", File.ReadAllText(Path.Combine(out, "index.html")))
        Assert.False(Directory.Exists(Output.stagingOf out))
        Assert.Equal(2, recovered.Length)

    [<Fact>]
    let ``TEK-PUB-003 a blocked ingest leaves the previous publication untouched`` () =
        let root = tempDirectory ()
        let out = Path.Combine(root, "site")
        Directory.CreateDirectory out |> ignore
        File.WriteAllText(Path.Combine(out, "index.html"), "last known good")
        Directory.CreateDirectory(Path.Combine(root, "content", "projects", "p", "research-note")) |> ignore
        File.WriteAllText(Path.Combine(root, "content/projects/p/research-note/a.md"), "---\nid: DUP-1\n---\n")
        File.WriteAllText(Path.Combine(root, "content/projects/p/research-note/b.md"), "---\nid: DUP-1\n---\n")
        File.WriteAllText(Path.Combine(root, "tekmerion.config.json"), """{"schemaVersion":1,"repository":"example/research","include":["content/**/*.md"]}""")

        let result = Commands.ingest (aegis ()) { ConfigPath = Path.Combine(root, "tekmerion.config.json"); Root = None; Out = Some out; Json = true }

        Assert.Equal(ExitCode.Blocked, result.ExitCode)
        Assert.Equal("last known good", File.ReadAllText(Path.Combine(out, "index.html")))
        Assert.False(Directory.Exists(Output.stagingOf out))

    [<Fact>]
    let ``TEK-VAL-002 an unreadable source blocks and is reported`` () =
        let root = tempDirectory ()
        Directory.CreateDirectory(Path.Combine(root, "content")) |> ignore
        File.WriteAllBytes(Path.Combine(root, "content/bad.md"), [| 0xffuy; 0xfeuy; 0x00uy; 0xc3uy |])
        File.WriteAllText(Path.Combine(root, "content/good.md"), "---\nid: GOOD-1\n---\n")
        File.WriteAllText(Path.Combine(root, "tekmerion.config.json"), """{"schemaVersion":1,"repository":"example/research","include":["content/*.md"]}""")

        let result = Commands.validate (aegis ()) { ConfigPath = Path.Combine(root, "tekmerion.config.json"); Root = None; Out = None; Json = true }

        Assert.Equal(ExitCode.Blocked, result.ExitCode)
        let corpus = result.Corpus.Value
        Assert.Contains(corpus.Findings, fun f -> f.Code = Tekmerion.Domain.UnreadableSource "the file is not valid UTF-8")
        // The readable file was still read.
        Assert.Equal(1, corpus.Artifacts.Length)

    [<Fact>]
    let ``an I/O failure crosses the boundary as an Aegis fault, not an exception`` () =
        let config, collector = quietAegis ()

        match FileSystem.readBytes config (tempDirectory ()) "missing.md" with
        | Ok _ -> failwith "expected a fault"
        | Error fault -> Assert.Contains("TEKMERION.SOURCE.READ_FAILED", Faults.describe fault)

        Assert.Contains("TEKMERION.SOURCE.READ_FAILED", collector.Codes)

    [<Fact>]
    let ``GH-17 end to end: the fixture ingests and publishes contracts`` () =
        let out = Path.Combine(tempDirectory (), "site")
        let result = Commands.ingest (aegis ()) (options (Some out))
        Assert.Equal(ExitCode.Success, result.ExitCode)
        Assert.True(File.Exists(Path.Combine(out, "data/v1/manifest.json")))
        Assert.True(File.Exists(Path.Combine(out, "data/v1/edges/a/RP-COMP-005.json")))

    [<Fact>]
    let ``invalid configuration is an argument error, not a crash`` () =
        let root = tempDirectory ()
        File.WriteAllText(Path.Combine(root, "tekmerion.config.json"), """{"schemaVersion":2}""")
        let result = Commands.validate (aegis ()) { ConfigPath = Path.Combine(root, "tekmerion.config.json"); Root = None; Out = None; Json = true }
        Assert.Equal(ExitCode.InvalidArguments, result.ExitCode)
        Assert.NotEmpty result.Messages
