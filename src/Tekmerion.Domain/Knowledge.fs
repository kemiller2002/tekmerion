namespace Tekmerion.Domain

/// Why a value is not known. Each case is a different fact about the world and
/// they must never collapse into one another, nor into zero (TEK-FID-002).
[<RequireQualifiedAccess>]
type Absence =
    /// The canonical source does not declare the value.
    | NotDeclared
    /// Nobody has determined the value, or its capability is unmapped.
    | Unknown
    /// The value exists in principle but could not be obtained here.
    | Unavailable of reason: string
    /// The value has no meaning for this subject.
    | NotApplicable of reason: string

/// A value that may legitimately be absent. `Known 0` is a real zero; absence
/// is always one of the explicit `Absence` cases, never a default
/// (TEK-FID-001, TEK-FID-002, ADR 0010).
type Knowable<'T> =
    | Known of 'T
    | Absent of Absence

[<RequireQualifiedAccess>]
module Knowable =

    let notDeclared<'T> : Knowable<'T> = Absent Absence.NotDeclared

    let ofOption (value: 'T option) : Knowable<'T> =
        match value with
        | Some known -> Known known
        | None -> notDeclared

    let toOption (value: Knowable<'T>) : 'T option =
        match value with
        | Known known -> Some known
        | Absent _ -> None

    let map (mapping: 'T -> 'U) (value: Knowable<'T>) : Knowable<'U> =
        match value with
        | Known known -> Known(mapping known)
        | Absent absence -> Absent absence

    let isKnown (value: Knowable<'T>) =
        match value with
        | Known _ -> true
        | Absent _ -> false

/// A list that cannot be empty. Used wherever an empty collection would be an
/// illegal state (e.g. a derived relationship with no basis).
type NonEmpty<'T> = private NonEmpty of head: 'T * tail: 'T list

[<RequireQualifiedAccess>]
module NonEmpty =

    let create (items: 'T list) : NonEmpty<'T> option =
        match items with
        | head :: tail -> Some(NonEmpty(head, tail))
        | [] -> None

    let singleton (item: 'T) = NonEmpty(item, [])

    let toList (NonEmpty(head, tail)) = head :: tail

    let head (NonEmpty(head, _)) = head

    let map (mapping: 'T -> 'U) (NonEmpty(head, tail)) = NonEmpty(mapping head, List.map mapping tail)

    let append (item: 'T) (NonEmpty(head, tail)) = NonEmpty(head, tail @ [ item ])
