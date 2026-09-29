---
id: DF-TEK-2026-0001
title: Tekmerion product identity, naming contract and canonical requirements baseline
document_type: decision-record
status: accepted
version: "1.0"
created: 2026-09-29
updated: 2026-09-29
work_item: GH-13
related_documents:
  - docs/requirements/tekmerion-requirements.md
  - docs/requirements/open-questions.md
  - docs/requirements/traceability.md
  - docs/vnext/15-requirements-v0.2.0.md
decided_by: Claude (Anthropic, claude-code runtime) executing GH-13 under owner instruction; reversible until the first release under the new names
---

# DF-TEK-2026-0001 — Product identity and requirements baseline

## Context

The repository was renamed from `research-publisher` to `tekmerion`, and issue
#11 widened the product from "Markdown → website" to an evidence-faithful
research system. Requirements were spread across an unversioned draft
(`input-documents/…txt`), `REQ-RP-VNEXT` 0.2.0 (draft), the normative shared
foundations document (doc 18), and #11. None had stable identifiers suitable
for traceability, and #11 and the drafts contained unreconciled conflicts
(mutation vs read-only, timestamp vs determinism, severity vocabularies,
sub-document objects the corpus cannot supply).

Package metadata still names `kemiller2002/research-publisher`, package
`@echelon-foundry/research-publisher`, executable `research-publisher`.

## Decision

1. `docs/requirements/tekmerion-requirements.md` (`REQ-TEK` 1.0.0) is the single
   canonical requirement set, with `TEK-<AREA>-<NNN>` identifiers that are never
   reused, and a class per requirement (`now`, `slice`, `target`, `gated`,
   `compat`, `deferred`, `superseded`). Earlier documents are kept unmodified as
   evidence.
2. The naming contract (`TEK-IDN-003`): package `@echelon-foundry/tekmerion`,
   executable `tekmerion`, config `tekmerion.config.json`, manifest
   `.echelon/tekmerion.json`, cache `.tekmerion/`, F# prefix `Tekmerion.`,
   environment prefix `TEKMERION_`.
3. Sub-document research objects are a `gated` class: types may exist; instances
   require an authoring contract (`OQ-TEK-001`).

## Alternatives

- **Keep `@echelon-foundry/research-publisher` and rename only the display
  name.** Rejected: #11 §19 requires a deliberate rename, and a mismatched
  package name perpetuates the stale identity the issue exists to remove.
- **Unscoped `tekmerion` package.** Rejected: every sibling Echelon capability
  (`@echelon-foundry/visual-engineering`, `…/typescript-wasm-kernel`,
  `…/repository-operating-system`) is scoped; consistency aids discovery.
- **Keep `.mjs` configuration.** Rejected for the new name: the target core is
  F#, and an executable JavaScript config would require a Node runtime to read
  configuration. The legacy `.mjs` remains read through the deprecation period
  (`TEK-IDN-004`).
- **Concatenate the drafts and #11.** Rejected: they conflict; see
  `open-questions.md` OQ-TEK-009…011.

## Evidence

- `docs/vnext/16-final-analysis.md` §4–5 and `05-requirements-gap-analysis.md`
  E.3 (no sub-document identity in the corpus).
- `package.json` (stale repository URLs, observed 2026-09-29).
- `.echelon/*.json` package naming conventions (observed 2026-09-29).

## Consequences

- #14 models the domain against `REQ-TEK`; #15 implements the naming contract
  and migration; tests cite `TEK-*` identifiers.
- Names are not binding on consumers until #15 ships a release with
  compatibility aliases.

## Reversibility

Requirements baseline: low cost (versioned change control). Names: low cost
until first publication under the new names; afterwards a further migration.
