namespace Tekmerion.Core

open System
open System.Text.RegularExpressions
open Tekmerion.Domain

/// Frontier records declare their links in the body, in fixed labelled forms
/// written by the frontier generator:
///
///   ## Evidence trace
///   - Origin document: [path](../../../content/…/file.md)
///   ## Dependencies
///   - [RFR-544ACDA1](./RFR-544ACDA1.md)
///
/// These are explicit, labelled links, extracted with their line so the edge
/// is traceable to the exact declaration (TEK-PRV-001). Nothing else in the
/// body is read as a relationship.
module Frontier =

    let private link = Regex(@"\[[^\]]*\]\(([^)\s]+)\)", RegexOptions.CultureInvariant)
    let private originLine = Regex(@"^\s*[-*]\s*Origin document:\s*(.+)$", RegexOptions.CultureInvariant)
    let private dependencyLine = Regex(@"^\s*[-*]\s*(\[[^\]]*\]\([^)\s]+\))\s*$", RegexOptions.CultureInvariant)

    let private linesOf (section: Section) =
        let first = (section.Location.Line |> Option.defaultValue 0) + 1

        section.Body.Split('\n')
        |> Array.mapi (fun index line -> first + index, line)
        |> List.ofArray

    let private declared (relation: CanonicalRelation) (section: Section) (line: int) (target: string) =
        { Relation = relation
          Value = References.classify target
          Location = { section.Location with Line = Some line } }

    let references (artifact: Artifact) : DeclaredReference list =
        match artifact.Type with
        | FrontierRecord ->
            let sectionNamed (name: string) =
                artifact.Sections
                |> List.filter (fun s -> String.Equals(s.Heading.Trim(), name, StringComparison.OrdinalIgnoreCase))

            let origins =
                sectionNamed "Evidence trace"
                |> List.collect (fun section ->
                    linesOf section
                    |> List.choose (fun (line, text) ->
                        let origin = originLine.Match text

                        if origin.Success then
                            let target = link.Match origin.Groups.[1].Value

                            if target.Success then
                                Some(declared OriginDocument section line target.Groups.[1].Value)
                            else
                                Some(declared OriginDocument section line origin.Groups.[1].Value)
                        else
                            None))

            let prerequisites =
                sectionNamed "Dependencies"
                |> List.collect (fun section ->
                    linesOf section
                    |> List.choose (fun (line, text) ->
                        let dependency = dependencyLine.Match text

                        if dependency.Success then
                            Some(declared Prerequisite section line (link.Match(dependency.Groups.[1].Value).Groups.[1].Value))
                        else
                            None))

            origins @ prerequisites
        | _ -> []
