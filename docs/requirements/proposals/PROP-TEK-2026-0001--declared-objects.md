---
id: PROP-TEK-2026-0001
title: Proposed authoring contract for declared sub-document research objects
status: proposed
version: 0.1.0
created: 2026-09-29
updated: 2026-09-29
work_item: WI-0017
answers: OQ-TEK-001 (proposal only — acceptance belongs to the Praxis/ROS format owner)
related_documents:
  - docs/requirements/open-questions.md
  - docs/requirements/tekmerion-requirements.md
  - src/Tekmerion.Domain/Research.fs
---

# Proposed authoring contract: declared sub-document research objects

## Problem (evidence)

The real corpus gives documents strong identity but gives **no identity to any
claim, finding or evidence item** (`docs/vnext/16-final-analysis.md` §4–5).
The gap is visible in real research today. In the slice, experiment reports
declare `tests: [HY-COMP-034, LAW-COMP-034, …]`. Those ids are mentioned in
the prose of RP-COMP-005, but no document declares them. Tekmerion therefore
reports them as unresolved (7 warnings) and does not guess. Every `gated`
requirement in REQ-TEK waits on a contract like this one: TEK-REL-006,
TEK-EXP-009, TEK-EVD-001…003, TEK-UNC-001 and TEK-SYN-001.

## Proposal (minimal, additive, optional)

A document **may** declare the objects it defines in a front-matter
`declares` list. Documents that declare nothing are unaffected.

```yaml
declares:
  - id: HY-COMP-034            # required; same grammar as artifact ids; unique repository-wide
    kind: hypothesis           # required; claim | finding | evidence | hypothesis | law |
                               #   observation | inference | conclusion | recommendation
    section: "Hypotheses"      # optional; heading text that holds the statement
    statement: "…"             # optional; verbatim short statement
    authorship: source         # optional; `source`, or `researcher: <actor>` (absent = unknown, never inferred)
    relations:                 # optional; each relation is declared, never derived
      - kind: supports         # supports | contradicts | evidence-for | evidence-against |
                               #   contextual | insufficient | depends-on
        target: EV-COMP-012    # a declared object id or an artifact id
```

Rules:

1. **Identity is declared, never minted.** A missing `id` or `kind` makes that
   entry invalid, and Tekmerion reports it. It does not repair it.
2. **Polarity is declared.** No relation kind is inferred from wording or
   similarity.
3. **Provenance is automatic.** The declaring file, key and line are recorded
   by the reader. Authors write no provenance by hand.
4. **Uniqueness.** A duplicate declared id is a blocking finding, the same rule
   as for artifact ids.
5. **Cycles.** `depends-on` cycles are reported as warnings, using the existing
   `Cycles.find`.
6. **Backward compatibility.** Nothing changes for existing documents. The key
   is new, so older readers preserve it as unknown front matter.

## Alignment with the gated domain types

The gated types in `src/Tekmerion.Domain/Research.fs` were written before this
proposal. They cover most of it. Accepting the proposal as written means
three bounded domain changes, and each would be tested:

| Proposal | Current domain | Change on acceptance |
|---|---|---|
| kinds `hypothesis`, `law` | `DeclaredObjectKind` has no such cases | Add `Hypothesis` and `Law`. The real corpus already uses `HY-` and `LAW-` ids in `tests:` (RP-COMP-005). |
| relation `target` may be an artifact id | `DeclaredRelation` joins two `DeclaredObject`s | Let the target be a declared object *or* an artifact key, with the target kind kept explicit. |
| `authorship` absent means unknown | `ClaimAuthorship` has no unknown case | Hold authorship as `Knowable<ClaimAuthorship>`, so absence stays distinct from a value (TEK-UNC rules). |
| `section` / `statement` | `Declared: SourceLocation` only | Optional fields. They are presentation hints, not identity. |

If the owner narrows the proposal (for example, no `law` kind), the domain
follows the accepted text rather than this draft.

## What Tekmerion does once this is accepted

- Adds `praxis-declared-objects@1` to `AuthoringContracts.approved`, through a
  decision record.
- Reads `declares` into `DeclaredObject` and `DeclaredRelation`. The
  constructors already exist and are gated.
- Resolves `tests: [HY-COMP-034]` against declared objects, so the 7 slice
  warnings resolve only if and when the research declares those objects.
- Opens the gated views: finding, evidence, and support/contradiction.

## Decision needed

This is a research-format decision owned by Praxis/ROS, not a Tekmerion one.
Tekmerion's rule until then is unchanged (OQ-TEK-001).
