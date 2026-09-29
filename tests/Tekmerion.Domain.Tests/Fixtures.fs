namespace Tekmerion.Domain.Tests

open Tekmerion.Domain

/// Identifiers and paths taken from the real composition-science corpus
/// (docs/vnext/13-vertical-slice.md), not synthetic stand-ins.
module Fixtures =

    let ok (result: Result<'T, string>) =
        match result with
        | Ok value -> value
        | Error reason -> failwith reason

    let repository = RepositoryId.create "kemiller2002/visual-engineering" |> ok

    let path raw = RepoPath.create raw |> ok

    let declared raw = ArtifactKey.Declared(ArtifactId.create raw |> ok)

    let location raw = SourceLocation.ofFile repository (path raw)

    let rep = declared "RP-COMP-005"
    let ex011 = declared "EX-COMP-011"
    let ex012 = declared "EX-COMP-012"

    let sourceRepEdge from =
        { From = from
          Relation = SourceRep
          Reference = IdReference "RP-COMP-005"
          Target = Resolves rep
          Declared =
            location "content/projects/composition-science/experiment-report/example.md"
            |> SourceLocation.atKey "source_rep" (Some 4) }

    let finding code =
        { Code = code
          Subject = None
          Location = None
          Message = Policy.code code
          Remedy = None }
