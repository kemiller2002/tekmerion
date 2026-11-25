namespace Tekmerion.Core.Tests

open Xunit
open Tekmerion.Core

/// Pure planning of Forma, an optional theme and a brand (TEK-ECH-006).
module PresentationTests =

    let echelonFiles =
        [ "asset-manifest.json"
          "echelon-app.css"
          "echelon-fonts.css"
          "echelon.css"
          "fonts/manrope-latin-wght-normal.woff2"
          "fonts/licenses/manrope-OFL.txt" ]

    let request =
        { Forma = true
          ThemeFiles = Some echelonFiles
          ThemeStylesheets = [ "echelon.css"; "echelon-fonts.css"; "echelon-app.css" ]
          Brand = Some "echelon" }

    let problems request =
        match Presentation.plan request with
        | Ok plan -> failwithf "expected a refusal, got %A" plan
        | Error problems -> problems

    [<Fact>]
    let ``TEK-ECH-006 theme stylesheets load after Forma, in the order given`` () =
        match Presentation.plan request with
        | Error problems -> failwithf "%A" problems
        | Ok plan ->
            Assert.Equal<string list>(
                [ "assets/forma/all.css"
                  "assets/theme/echelon.css"
                  "assets/theme/echelon-fonts.css"
                  "assets/theme/echelon-app.css" ],
                plan.Stylesheets
            )

            Assert.Equal(Some "echelon", plan.Brand)

    [<Fact>]
    let ``TEK-ECH-006 every theme file is copied under assets/theme so relative urls resolve`` () =
        match Presentation.plan request with
        | Error problems -> failwithf "%A" problems
        | Ok plan ->
            Assert.Contains(("fonts/manrope-latin-wght-normal.woff2", "assets/theme/fonts/manrope-latin-wght-normal.woff2"), plan.ThemeAssets)
            Assert.Equal(echelonFiles.Length, plan.ThemeAssets.Length)
            // Ordinal order, whatever order the host listed them in.
            Assert.Equal<string list>(plan.ThemeAssets |> List.map fst, plan.ThemeAssets |> List.map fst |> List.sortWith (fun a b -> System.String.CompareOrdinal(a, b)))

    [<Fact>]
    let ``TEK-ECH-003 nothing requested is plain semantic HTML`` () =
        let plan =
            Presentation.plan { Forma = false; ThemeFiles = None; ThemeStylesheets = []; Brand = None }

        Assert.Equal(Ok Presentation.none, plan)

    [<Fact>]
    let ``Forma alone is the existing single stylesheet`` () =
        match Presentation.plan { Forma = true; ThemeFiles = None; ThemeStylesheets = []; Brand = None } with
        | Ok plan -> Assert.Equal<string list>([ "assets/forma/all.css" ], plan.Stylesheets)
        | Error problems -> failwithf "%A" problems

    [<Fact>]
    let ``a brand needs no theme`` () =
        match Presentation.plan { Forma = true; ThemeFiles = None; ThemeStylesheets = []; Brand = Some "echelon" } with
        | Ok plan -> Assert.Equal(Some "echelon", plan.Brand)
        | Error problems -> failwithf "%A" problems

    [<Fact>]
    let ``a theme without Forma is refused`` () =
        Assert.Contains("--theme requires --forma: a theme layers on Forma's all.css", problems { request with Forma = false })

    [<Fact>]
    let ``a theme stylesheet without a theme is refused`` () =
        Assert.Contains("--theme-css requires --theme", problems { request with ThemeFiles = None })

    [<Fact>]
    let ``a theme with nothing to link is refused`` () =
        Assert.Contains("--theme requires at least one --theme-css naming a stylesheet to link", problems { request with ThemeStylesheets = [] })

    [<Fact>]
    let ``an empty theme directory is refused`` () =
        Assert.Contains("--theme names a directory with no files", problems { request with ThemeFiles = Some [] })

    [<Fact>]
    let ``a theme stylesheet must be a file in the theme`` () =
        Assert.Contains("--theme-css 'missing.css' is not a file in the --theme directory", problems { request with ThemeStylesheets = [ "missing.css" ] })

    [<Theory>]
    [<InlineData("../all.css")>]
    [<InlineData("/etc/passwd")>]
    [<InlineData("fonts/../../x.css")>]
    [<InlineData("./echelon.css")>]
    [<InlineData("fonts\\x.css")>]
    let ``a theme stylesheet must be a safe, normalized relative path`` (css: string) =
        Assert.Contains($"--theme-css '{css}' is not a safe relative path", problems { request with ThemeStylesheets = [ css ] })

    [<Fact>]
    let ``a theme file that would escape the output is refused`` () =
        Assert.Contains("theme file '../escape.css' is not a safe relative path", problems { request with ThemeFiles = Some("../escape.css" :: echelonFiles) })

    [<Fact>]
    let ``a stylesheet listed twice is refused`` () =
        Assert.Contains("--theme-css 'echelon.css' is listed more than once", problems { request with ThemeStylesheets = [ "echelon.css"; "echelon.css" ] })

    [<Theory>]
    [<InlineData("Echelon")>]
    [<InlineData("echelon\" onload=\"x")>]
    [<InlineData("")>]
    [<InlineData("1echelon")>]
    [<InlineData("ech elon")>]
    let ``a brand must be a lowercase token`` (brand: string) =
        Assert.Contains($"--brand '{brand}' must be a lowercase token (a-z, 0-9, '-')", problems { request with Brand = Some brand })

    [<Theory>]
    [<InlineData("echelon")>]
    [<InlineData("acme-2")>]
    let ``brand tokens`` (brand: string) = Assert.True(Presentation.isBrand brand)
