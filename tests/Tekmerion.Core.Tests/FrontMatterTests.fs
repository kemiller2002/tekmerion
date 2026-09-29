namespace Tekmerion.Core.Tests

open Xunit
open Tekmerion.Core

module FrontMatterTests =

    let entries text =
        match FrontMatter.split text with
        | WithFrontMatter(yaml, line, _, _) ->
            match FrontMatter.parse yaml line with
            | Entries parsed -> parsed
            | Malformed(reason, _) -> failwith reason
        | other -> failwithf "%A" other

    [<Fact>]
    let ``TEK-ING-002 block scalars, flow lists and nested maps parse`` () =
        let parsed =
            entries "---\npurpose: |\n  line one\n  line two\ngenome_nodes: [GN-100, GN-130]\nconfidence:\n  evidence: \"High\"\n---\nbody"

        Assert.Equal(YScalar "line one\nline two\n", (parsed |> List.find (fun e -> e.Key = "purpose")).Value)
        Assert.Equal(YSequence [ YScalar "GN-100"; YScalar "GN-130" ], (parsed |> List.find (fun e -> e.Key = "genome_nodes")).Value)
        Assert.Equal(YMapping [ "evidence", YScalar "High" ], (parsed |> List.find (fun e -> e.Key = "confidence")).Value)

    [<Fact>]
    let ``TEK-FID-001 dates and placeholders stay verbatim strings`` () =
        let parsed = entries "---\ndate: YYYY-MM-DD\ncreated: 2026-07-21\n---\n"
        Assert.Equal(YScalar "YYYY-MM-DD", parsed.[0].Value)
        Assert.Equal(YScalar "2026-07-21", parsed.[1].Value)

    [<Fact>]
    let ``plain null is null but quoted null is text`` () =
        let parsed = entries "---\nsupersedes: null\nsuperseded_by:\nlabel: \"null\"\n---\n"
        Assert.Equal<YamlValue list>([ YNull; YNull; YScalar "null" ], parsed |> List.map (fun e -> e.Value))

    [<Fact>]
    let ``TEK-PRV-001 entries carry their file line and verbatim text`` () =
        let parsed = entries "---\nid: RP-COMP-005\nrelated_artifacts:\n  - TH-COMP-005\n  - DF-COMP-002\n\ntitle: X\n---\n"
        let related = parsed |> List.find (fun e -> e.Key = "related_artifacts")
        Assert.Equal(3, related.Line)
        Assert.Equal("related_artifacts:\n  - TH-COMP-005\n  - DF-COMP-002", related.Raw)

    [<Fact>]
    let ``malformed YAML is a typed outcome with a line`` () =
        match FrontMatter.split "---\nid: [unclosed\ntitle: x\n---\n" with
        | WithFrontMatter(yaml, line, _, _) ->
            match FrontMatter.parse yaml line with
            | Malformed(_, Some _) -> ()
            | other -> failwithf "expected Malformed, got %A" other
        | other -> failwithf "%A" other

    [<Fact>]
    let ``unterminated front matter is detected, not swallowed`` () =
        match FrontMatter.split "---\nid: X\nbody without closing fence" with
        | UnterminatedFrontMatter _ -> ()
        | other -> failwithf "%A" other

    [<Fact>]
    let ``CRLF and BOM input splits like LF input`` () =
        match FrontMatter.split "﻿---\r\nid: X\r\n---\r\nbody" with
        | WithFrontMatter(yaml, 2, "body", 4) -> Assert.Equal("id: X", yaml)
        | other -> failwithf "%A" other
