namespace Tekmerion.Core.Tests

open System.IO
open Xunit
open Tekmerion.Cli
open Tekmerion.Core.Tests.Support

/// `ingest --forma --theme --theme-css --brand` through the real host path
/// (TEK-ECH-006): assets are copied byte for byte, linked in load order, and
/// every refusal leaves the previous publication untouched.
module ThemeTests =

    let aegis () = fst (quietAegis ())

    let write (root: string) (relative: string) (bytes: byte array) =
        let target = Path.Combine(root, relative)
        Directory.CreateDirectory(Path.GetDirectoryName target) |> ignore
        File.WriteAllBytes(target, bytes)

    let text (value: string) = System.Text.Encoding.UTF8.GetBytes value

    /// Not valid UTF-8: a font must survive the copy unchanged.
    let fontBytes = [| 0x77uy; 0x4fuy; 0x46uy; 0x32uy; 0xffuy; 0xfeuy; 0x00uy; 0xc3uy |]

    let repository () =
        let root = tempDirectory ()
        write root "content/projects/p/research-note/a.md" (text "---\nid: NOTE-1\ntitle: A note\n---\n# A note\n\nBody.\n")
        write root "tekmerion.config.json" (text """{"schemaVersion":1,"repository":"example/research","include":["content/**/*.md"]}""")
        write root "forma/all.css" (text ".forma-stack{gap:1rem}\n")
        write root "theme/brand.css" (text "[data-forma-brand=\"acme\"]{--x:1}\n")
        write root "theme/fonts.css" (text "@font-face{font-family:A;src:url(\"fonts/a.woff2\")}\n")
        write root "theme/fonts/a.woff2" fontBytes
        root

    let themed root out =
        { IngestOptions.defaults with
            ConfigPath = Path.Combine(root, "tekmerion.config.json")
            Out = Some out
            Forma = Some(Path.Combine(root, "forma/all.css"))
            Theme = Some(Path.Combine(root, "theme"))
            ThemeStylesheets = [ "brand.css"; "fonts.css" ]
            Brand = Some "acme"
            Json = true }

    let notePage out =
        Directory.EnumerateFiles(out, "index.html", SearchOption.AllDirectories)
        |> Seq.find (fun f -> f.Replace('\\', '/').Contains "/a/")
        |> File.ReadAllText

    [<Fact>]
    let ``TEK-ECH-006 the theme is copied verbatim and linked after Forma in the given order`` () =
        let root = repository ()
        let out = Path.Combine(root, "site")
        let result = Commands.ingest (aegis ()) (themed root out)

        Assert.Equal(ExitCode.Success, result.ExitCode)
        Assert.Equal<byte array>(fontBytes, File.ReadAllBytes(Path.Combine(out, "assets/theme/fonts/a.woff2")))
        Assert.Equal<byte array>(File.ReadAllBytes(Path.Combine(root, "theme/fonts.css")), File.ReadAllBytes(Path.Combine(out, "assets/theme/fonts.css")))
        Assert.True(File.Exists(Path.Combine(out, "assets/forma/all.css")))

        let html = notePage out
        Assert.Contains("<html lang=\"en\" data-forma-brand=\"acme\">", html)

        let forma = html.IndexOf "../../assets/forma/all.css"
        let brand = html.IndexOf "../../assets/theme/brand.css"
        let fonts = html.IndexOf "../../assets/theme/fonts.css"
        Assert.True(forma > 0 && forma < brand && brand < fonts, "stylesheets are not in load order")

    [<Fact>]
    let ``TEK-ARC-004 a themed build is byte-identical across runs`` () =
        let root = repository ()
        let tree out =
            Directory.EnumerateFiles(out, "*", SearchOption.AllDirectories)
            |> Seq.map (fun f -> Path.GetRelativePath(out, f).Replace('\\', '/'), File.ReadAllBytes f)
            |> Seq.filter (fun (path, _) -> path <> Tekmerion.Core.Contracts.PublicationRecord)
            |> List.ofSeq
            |> List.sortBy fst

        let first = Path.Combine(root, "one")
        let second = Path.Combine(root, "two")
        Assert.Equal(ExitCode.Success, (Commands.ingest (aegis ()) (themed root first)).ExitCode)
        Assert.Equal(ExitCode.Success, (Commands.ingest (aegis ()) (themed root second)).ExitCode)
        Assert.True((tree first = tree second))

    [<Fact>]
    let ``TEK-ECH-003 without a theme there is no theme directory and no brand`` () =
        let root = repository ()
        let out = Path.Combine(root, "site")
        let options = { themed root out with Theme = None; ThemeStylesheets = []; Brand = None }
        Assert.Equal(ExitCode.Success, (Commands.ingest (aegis ()) options).ExitCode)
        Assert.False(Directory.Exists(Path.Combine(out, "assets/theme")))
        Assert.Contains("<html lang=\"en\">", notePage out)

    let refusedWithPreviousIntact (change: IngestOptions -> IngestOptions) (expected: string) =
        let root = repository ()
        let out = Path.Combine(root, "site")
        Directory.CreateDirectory out |> ignore
        File.WriteAllText(Path.Combine(out, "index.html"), "last known good")

        let result = Commands.ingest (aegis ()) (change (themed root out))

        Assert.Equal(ExitCode.InvalidArguments, result.ExitCode)
        Assert.Contains(result.Messages, fun m -> m.Contains expected)
        Assert.Equal("last known good", File.ReadAllText(Path.Combine(out, "index.html")))

    [<Fact>]
    let ``a theme stylesheet that is not in the theme is an argument error`` () =
        refusedWithPreviousIntact (fun o -> { o with ThemeStylesheets = [ "missing.css" ] }) "is not a file in the --theme directory"

    [<Fact>]
    let ``a theme without Forma is an argument error`` () =
        refusedWithPreviousIntact (fun o -> { o with Forma = None }) "--theme requires --forma"

    [<Fact>]
    let ``a theme directory that does not exist is an argument error`` () =
        refusedWithPreviousIntact (fun o -> { o with Theme = Some(Path.Combine(tempDirectory (), "absent")) }) "is not a directory"

    [<Fact>]
    let ``an unsafe brand is an argument error`` () =
        refusedWithPreviousIntact (fun o -> { o with Brand = Some "acme\"><script>" }) "must be a lowercase token"

    [<Fact>]
    let ``an unreadable Forma stylesheet is an environment failure, reported through Aegis`` () =
        let root = repository ()
        let config, collector = quietAegis ()
        let options = { themed root (Path.Combine(root, "site")) with Forma = Some(Path.Combine(root, "missing.css")) }
        let result = Commands.ingest config options
        Assert.Equal(ExitCode.EnvironmentFailure, result.ExitCode)
        Assert.Contains("TEKMERION.ASSETS.READ_FAILED", collector.Codes)

    [<Fact>]
    let ``symbolic links in the theme directory are not followed`` () =
        let root = repository ()
        let outside = Path.Combine(tempDirectory (), "secret.txt")
        File.WriteAllText(outside, "outside the theme")

        let linked =
            try
                File.CreateSymbolicLink(Path.Combine(root, "theme", "secret.txt"), outside) |> ignore
                true
            with _ -> false

        // Platforms that cannot create links have nothing to follow.
        if linked then
            let out = Path.Combine(root, "site")
            Assert.Equal(ExitCode.Success, (Commands.ingest (aegis ()) (themed root out)).ExitCode)
            Assert.False(File.Exists(Path.Combine(out, "assets/theme/secret.txt")))
