namespace Tekmerion.Core

open System
open System.Globalization
open System.Text

/// Minimal JSON model and a writer whose output depends only on the value:
/// keys keep the order given, two-space indentation, `\n` newlines, trailing
/// newline. Deterministic by construction (TEK-ARC-004).
type Json =
    | JNull
    | JBool of bool
    | JInt of int64
    | JString of string
    | JArray of Json list
    | JObject of (string * Json) list

module Json =

    let str (value: string) = JString value
    let int (value: int) = JInt(int64 value)
    let arr (values: Json list) = JArray values
    let obj (fields: (string * Json) list) = JObject fields
    let strings (values: string list) = values |> List.map JString |> JArray
    let ofOption (mapping: 'T -> Json) (value: 'T option) = value |> Option.map mapping |> Option.defaultValue JNull

    let private escape (builder: StringBuilder) (text: string) =
        builder.Append('"') |> ignore

        for character in text do
            match character with
            | '"' -> builder.Append("\\\"") |> ignore
            | '\\' -> builder.Append("\\\\") |> ignore
            | '\n' -> builder.Append("\\n") |> ignore
            | '\r' -> builder.Append("\\r") |> ignore
            | '\t' -> builder.Append("\\t") |> ignore
            | '<' -> builder.Append("\\u003c") |> ignore // safe to inline in HTML
            | '>' -> builder.Append("\\u003e") |> ignore
            | '&' -> builder.Append("\\u0026") |> ignore
            | ' ' -> builder.Append("\\u2028") |> ignore
            | ' ' -> builder.Append("\\u2029") |> ignore
            | c when c < ' ' -> builder.Append("\\u").Append((uint16 c).ToString("x4", CultureInfo.InvariantCulture)) |> ignore
            | c -> builder.Append(c) |> ignore

        builder.Append('"') |> ignore

    let rec private write (builder: StringBuilder) (indent: int) (value: Json) =
        let pad (level: int) = builder.Append(' ', level * 2) |> ignore

        match value with
        | JNull -> builder.Append("null") |> ignore
        | JBool true -> builder.Append("true") |> ignore
        | JBool false -> builder.Append("false") |> ignore
        | JInt number -> builder.Append(number.ToString(CultureInfo.InvariantCulture)) |> ignore
        | JString text -> escape builder text
        | JArray [] -> builder.Append("[]") |> ignore
        | JObject [] -> builder.Append("{}") |> ignore
        | JArray items ->
            builder.Append("[\n") |> ignore

            items
            |> List.iteri (fun index item ->
                pad (indent + 1)
                write builder (indent + 1) item
                builder.Append(if index < items.Length - 1 then ",\n" else "\n") |> ignore)

            pad indent
            builder.Append(']') |> ignore
        | JObject fields ->
            builder.Append("{\n") |> ignore

            fields
            |> List.iteri (fun index (key, item) ->
                pad (indent + 1)
                escape builder key
                builder.Append(": ") |> ignore
                write builder (indent + 1) item
                builder.Append(if index < fields.Length - 1 then ",\n" else "\n") |> ignore)

            pad indent
            builder.Append('}') |> ignore

    let rec private writeCompact (builder: StringBuilder) (value: Json) =
        match value with
        | JArray items ->
            builder.Append('[') |> ignore

            items
            |> List.iteri (fun index item ->
                if index > 0 then builder.Append(',') |> ignore
                writeCompact builder item)

            builder.Append(']') |> ignore
        | JObject fields ->
            builder.Append('{') |> ignore

            fields
            |> List.iteri (fun index (key, item) ->
                if index > 0 then builder.Append(',') |> ignore
                escape builder key
                builder.Append(':') |> ignore
                writeCompact builder item)

            builder.Append('}') |> ignore
        | scalar -> write builder 0 scalar

    /// Same value, no insignificant whitespace; for per-artifact shards
    /// fetched by browsers and agents (TEK-CON-003).
    let serializeCompact (value: Json) : string =
        let builder = StringBuilder()
        writeCompact builder value
        builder.Append('\n').ToString()

    let serialize (value: Json) : string =
        let builder = StringBuilder()
        write builder 0 value
        builder.Append('\n').ToString()
