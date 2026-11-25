namespace Tekmerion.Core

open System
open Tekmerion.Domain

/// What the host asked for: Forma, an optional theme on top of it and an
/// optional brand. Paths inside the theme are relative to the theme
/// directory, '/'-separated, as the host listed them.
type PresentationRequest =
    { Forma: bool
      /// Every file in the theme directory, or `None` without `--theme`.
      ThemeFiles: string list option
      /// Theme stylesheets to link, in load order.
      ThemeStylesheets: string list
      Brand: string option }

/// How the pages are presented (TEK-ECH-006). Stylesheets are
/// output-relative and in load order: Forma first, then the theme's.
type Presentation =
    { Stylesheets: string list
      /// Value of `data-forma-brand` on `<html>`.
      Brand: string option
      /// (theme-relative source, output-relative destination) for every
      /// theme file copied verbatim, so relative `url()`s keep resolving.
      ThemeAssets: (string * string) list }

/// Pure planning of the optional presentation layer. Pages stay readable
/// semantic HTML without any of it (TEK-ECH-003).
module Presentation =

    [<Literal>]
    let FormaStylesheet = "assets/forma/all.css"

    [<Literal>]
    let ThemeRoot = "assets/theme/"

    let none =
        { Stylesheets = []
          Brand = None
          ThemeAssets = [] }

    /// A brand is a lowercase token, as Forma's `data-forma-brand` values are.
    let isBrand (value: string) =
        not (String.IsNullOrEmpty value)
        && Char.IsAsciiLetterLower value[0]
        && value |> Seq.forall (fun c -> Char.IsAsciiLetterLower c || Char.IsAsciiDigit c || c = '-')

    /// A theme path is accepted only if it is already a normalized,
    /// non-escaping repository-style path (the same rule the output uses).
    let private normalized (path: string) =
        match RepoPath.create path with
        | Ok repoPath when RepoPath.value repoPath = path -> true
        | _ -> false

    let plan (request: PresentationRequest) : Result<Presentation, string list> =
        let themeFiles = request.ThemeFiles |> Option.defaultValue []

        let problems =
            [ match request.ThemeFiles, request.Forma with
              | Some _, false -> "--theme requires --forma: a theme layers on Forma's all.css"
              | _ -> ()
              match request.ThemeFiles with
              | Some [] -> "--theme names a directory with no files"
              | _ -> ()
              if request.ThemeFiles.IsNone && not request.ThemeStylesheets.IsEmpty then
                  "--theme-css requires --theme"
              if request.ThemeFiles.IsSome && request.ThemeStylesheets.IsEmpty then
                  "--theme requires at least one --theme-css naming a stylesheet to link"
              for path in themeFiles |> List.filter (normalized >> not) do
                  $"theme file '{path}' is not a safe relative path"
              for css in request.ThemeStylesheets |> List.filter (fun css -> not (normalized css)) do
                  $"--theme-css '{css}' is not a safe relative path"
              for css in request.ThemeStylesheets |> List.filter (fun css -> normalized css && request.ThemeFiles.IsSome && not (List.contains css themeFiles)) do
                  $"--theme-css '{css}' is not a file in the --theme directory"
              for css in request.ThemeStylesheets |> List.countBy id |> List.filter (fun (_, n) -> n > 1) |> List.map fst do
                  $"--theme-css '{css}' is listed more than once"
              match request.Brand with
              | Some brand when not (isBrand brand) -> $"--brand '{brand}' must be a lowercase token (a-z, 0-9, '-')"
              | _ -> () ]

        if not problems.IsEmpty then
            Error problems
        else
            Ok
                { Stylesheets =
                    [ if request.Forma then FormaStylesheet
                      yield! request.ThemeStylesheets |> List.map (fun css -> ThemeRoot + css) ]
                  Brand = request.Brand
                  ThemeAssets =
                    themeFiles
                    |> List.sortWith (fun a b -> String.CompareOrdinal(a, b))
                    |> List.map (fun path -> path, ThemeRoot + path) }
