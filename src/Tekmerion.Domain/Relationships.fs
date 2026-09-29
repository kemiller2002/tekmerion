namespace Tekmerion.Domain

/// Relations encoded by research itself (TEK-REL-001).
type CanonicalRelation =
    | RelatedDocument
    | SourceRep
    | Supersedes
    | SupersededBy
    /// frontier record -> the document it originated from, as declared in the
    /// record's evidence trace (the frontier pipeline calls the inverse
    /// `originates`; that inverse is a derived backlink here)
    | OriginDocument
    /// frontier record -> prerequisite frontier record
    | Prerequisite
    /// explicit typed id list (`evidenceIds`, `hypothesisIds`, `theoryIds`)
    | TypedLink of field: string

/// A relationship declared in canonical source at a known location.
type CanonicalEdge =
    { From: ArtifactKey
      Relation: CanonicalRelation
      Reference: ReferenceValue
      Target: Resolution
      Declared: SourceLocation }

/// Relations Tekmerion computes for navigation. A separate type so they can
/// never be mistaken for research (TEK-REL-002, ADR 0009).
type DerivedRelation =
    | Backlink of CanonicalRelation
    | SupersessionChain

/// A derived edge always carries the canonical edges it was derived from, so
/// "why am I seeing this relationship?" always has an answer (TEK-REL-003).
/// An empty basis is unrepresentable.
type DerivedEdge =
    { From: ArtifactKey
      To: ArtifactKey
      Relation: DerivedRelation
      Rule: string
      Basis: NonEmpty<CanonicalEdge> }

type Relationship =
    | Canonical of CanonicalEdge
    | Derived of DerivedEdge

[<RequireQualifiedAccess>]
module CanonicalRelation =

    let label (relation: CanonicalRelation) =
        match relation with
        | RelatedDocument -> "related-document"
        | SourceRep -> "source-rep"
        | Supersedes -> "supersedes"
        | SupersededBy -> "superseded-by"
        | OriginDocument -> "origin-document"
        | Prerequisite -> "prerequisite"
        | TypedLink field -> $"typed-link:{field}"

[<RequireQualifiedAccess>]
module Derivation =

    [<Literal>]
    let BacklinkRule = "backlink: inverse of one resolved canonical edge"

    /// One backlink per resolved canonical edge, in a deterministic order.
    /// Dangling and non-link references produce nothing: there is no target to
    /// point back from (TEK-FID-001).
    let backlinks (edges: CanonicalEdge list) : DerivedEdge list =
        edges
        |> List.choose (fun edge ->
            match edge.Target with
            | Resolves target ->
                Some
                    { From = target
                      To = edge.From
                      Relation = Backlink edge.Relation
                      Rule = BacklinkRule
                      Basis = NonEmpty.singleton edge }
            | OutOfScope _
            | Dangling _
            | NotALink _ -> None)
        |> List.sortBy (fun edge ->
            ArtifactKey.value edge.From, ArtifactKey.value edge.To, string edge.Relation)

/// Cycle detection over declared edges only (TEK-INT-002). Pure, deterministic
/// and independent of edge semantics, so it serves `prerequisite` edges now and
/// declared evidence dependencies once they exist.
[<RequireQualifiedAccess>]
module Cycles =

    let private successors (edges: ('k * 'k) list) : Map<'k, 'k list> =
        edges
        |> List.groupBy fst
        |> List.map (fun (source, pairs) -> source, pairs |> List.map snd |> List.distinct)
        |> Map.ofList

    /// Nodes reachable from `start` through one or more edges.
    let private reachable (graph: Map<'k, 'k list>) (start: 'k) : Set<'k> =
        let next node = graph |> Map.tryFind node |> Option.defaultValue []

        let rec visit (seen: Set<'k>) (frontier: 'k list) =
            match frontier with
            | [] -> seen
            | node :: rest ->
                let fresh = next node |> List.filter (fun candidate -> not (seen.Contains candidate))
                visit (fresh |> List.fold (fun acc item -> Set.add item acc) seen) (fresh @ rest)

        visit Set.empty (next start)

    /// Every strongly connected component that contains a cycle (including a
    /// self-loop), each sorted, the list sorted. Empty when acyclic.
    let find (edges: ('k * 'k) list) : 'k list list =
        let graph = successors edges
        let nodes = edges |> List.collect (fun (a, b) -> [ a; b ]) |> List.distinct |> List.sort
        let reach = nodes |> List.map (fun node -> node, reachable graph node) |> Map.ofList

        nodes
        |> List.filter (fun node -> reach.[node].Contains node)
        |> List.map (fun node ->
            nodes |> List.filter (fun other -> reach.[node].Contains other && reach.[other].Contains node))
        |> List.distinct
        |> List.sort

/// Effective-current projection over declared supersession (TEK-INT-004).
/// History is returned, not discarded.
[<RequireQualifiedAccess>]
module Supersession =

    type Chain<'k> =
        { Current: 'k
          /// From the starting record to (excluding) the current one.
          History: 'k list }

    /// Follow `successor` until a record has none. A cycle is reported with its
    /// path rather than looping or picking a winner.
    let effectiveCurrent (successor: 'k -> 'k option) (start: 'k) : Result<Chain<'k>, 'k list> =
        let rec follow (path: 'k list) (node: 'k) =
            if path |> List.contains node then
                Error(List.rev (node :: path))
            else
                match successor node with
                | None -> Ok { Current = node; History = List.rev path }
                | Some next -> follow (node :: path) next

        follow [] start
