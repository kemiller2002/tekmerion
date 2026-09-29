namespace Tekmerion.Core

open System
open System.Net
open System.Text

/// A minimal HTML model. Text is always escaped when rendered; the only way
/// to emit markup that did not come from this model is `Trusted`, used solely
/// for Markdig output rendered with raw HTML disabled (TEK-SEC-001).
type Html =
    | Element of tag: string * attributes: (string * string) list * children: Html list
    | VoidElement of tag: string * attributes: (string * string) list
    | Text of string
    | Trusted of string
    | Fragment of Html list

module Html =

    let private encode (text: string) = WebUtility.HtmlEncode text

    let rec private write (builder: StringBuilder) (node: Html) =
        let attributes (pairs: (string * string) list) =
            for name, value in pairs do
                builder.Append(' ').Append(name).Append("=\"").Append(encode value).Append('"') |> ignore

        match node with
        | Element(tag, attrs, children) ->
            builder.Append('<').Append(tag) |> ignore
            attributes attrs
            builder.Append('>') |> ignore
            children |> List.iter (write builder)
            builder.Append("</").Append(tag).Append('>') |> ignore
        | VoidElement(tag, attrs) ->
            builder.Append('<').Append(tag) |> ignore
            attributes attrs
            builder.Append('>') |> ignore
        | Text text -> builder.Append(encode text) |> ignore
        | Trusted markup -> builder.Append(markup) |> ignore
        | Fragment nodes -> nodes |> List.iter (write builder)

    let render (node: Html) =
        let builder = StringBuilder("<!doctype html>\n")
        write builder node
        builder.Append('\n').ToString()

    let el tag attrs children = Element(tag, attrs, children)
    let text value = Text value
    let empty = Fragment []
    let a href children = el "a" [ "href", href ] children
    let p children = el "p" [] children
    let h level children = el $"h{level}" [] children
    let ul children = el "ul" [] children
    let li children = el "li" [] children
    /// Text with line-break opportunities after `/`, `-`, `_` and `.`, so long
    /// paths and identifiers reflow at 320px instead of overflowing
    /// (TEK-ACC-002) without any local CSS.
    let breakable (value: string) =
        let rec split (acc: Html list) (current: System.Text.StringBuilder) (rest: char list) =
            match rest with
            | [] -> List.rev (Text(current.ToString()) :: acc)
            | c :: tail when c = '/' || c = '-' || c = '_' || c = '.' ->
                current.Append(c) |> ignore
                split (VoidElement("wbr", []) :: Text(current.ToString()) :: acc) (System.Text.StringBuilder()) tail
            | c :: tail ->
                current.Append(c) |> ignore
                split acc current tail

        Fragment(split [] (System.Text.StringBuilder()) (List.ofSeq value))

    let code value = el "code" [] [ breakable value ]

    /// Verbatim multi-line source text: each line kept, joined by `<br>`.
    let verbatim (value: string) =
        el "code" [] [ value.Split('\n') |> List.ofArray |> List.map breakable |> List.reduce (fun a b -> Fragment [ a; VoidElement("br", []); b ]) ]
    let section attrs children = el "section" attrs children

    /// Forma status lozenge. The label carries the meaning; the state only
    /// adds redundant emphasis, so status never depends on colour alone.
    let lozenge (state: string) (label: string) =
        el "span" [ "class", "ef-status-lozenge"; "data-state", state ] [ text label ]

    /// Forma key/value list over a native description list.
    let keyValues (rows: (string * Html) list) =
        el "dl" [ "class", "ef-key-value-list" ] (rows |> List.map (fun (key, value) -> el "div" [] [ el "dt" [] [ text key ]; el "dd" [] [ value ] ]))

    let disclosure (summary: string) (children: Html list) =
        el "details" [ "class", "ef-disclosure" ] [ el "summary" [] [ text summary ]; el "div" [ "class", "ef-disclosure__content" ] children ]

    /// Relative href from one output page to another, so the site works under
    /// any base path (a project Pages site, a custom domain, or file://).
    let relative (fromPage: string) (target: string) =
        let fromDir = fromPage.Split('/') |> Array.rev |> Array.skip 1 |> Array.rev
        let targetParts = target.Split('/')

        let common =
            Seq.zip fromDir targetParts
            |> Seq.takeWhile (fun (x, y) -> x = y)
            |> Seq.length

        let up = Array.replicate (fromDir.Length - common) ".."
        let down = targetParts |> Array.skip common
        let joined = String.Join("/", Array.append up down)
        if joined = "" then "./" else joined
