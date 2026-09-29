namespace Tekmerion.Core

open System.Text
open System.Text.RegularExpressions

/// Repository-relative glob matching for discovery configuration
/// (TEK-ING-001). Supports `**` (any depth), `*` (within one segment) and `?`.
module Glob =

    let toRegex (pattern: string) : Regex =
        let builder = StringBuilder("^")

        let rec emit (index: int) =
            if index < pattern.Length then
                match pattern.[index] with
                | '*' when index + 1 < pattern.Length && pattern.[index + 1] = '*' ->
                    if index + 2 < pattern.Length && pattern.[index + 2] = '/' then
                        builder.Append("(?:.*/)?") |> ignore
                        emit (index + 3)
                    else
                        builder.Append(".*") |> ignore
                        emit (index + 2)
                | '*' ->
                    builder.Append("[^/]*") |> ignore
                    emit (index + 1)
                | '?' ->
                    builder.Append("[^/]") |> ignore
                    emit (index + 1)
                | character ->
                    builder.Append(Regex.Escape(string character)) |> ignore
                    emit (index + 1)

        emit 0
        Regex(builder.Append("$").ToString(), RegexOptions.CultureInvariant)

    let matches (pattern: string) =
        let regex = toRegex pattern
        fun (path: string) -> regex.IsMatch path
