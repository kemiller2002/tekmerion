---
id: DF-TEK-2026-0002
title: Typed ingestion for the first vertical slice — boundaries, dependencies and relationship rules
document_type: decision-record
status: accepted
version: "1.0"
created: 2026-09-29
updated: 2026-09-29
work_item: GH-17
related_documents:
  - docs/architecture/tekmerion-ingestion.md
  - docs/requirements/tekmerion-requirements.md
  - research/decisions/DF-TEK-2026-0001--product-identity-and-requirements-baseline.md
decided_by: Claude (Anthropic, claude-code runtime) executing GH-17; reversible
---

# DF-TEK-2026-0002 — Typed ingestion for the first vertical slice

## Context

GH-17 has to read the real `visual-engineering` / `composition-science`
corpus honestly: parse → type → validate → resolve → project, with no
fabricated data, full provenance, and versioned machine contracts. It is the
first Tekmerion code that touches files and a third-party format (YAML).

## Decisions

1. **Three projects, following the SDE tiers.** `Tekmerion.Domain` (tiers 1–2,
   GH-14), `Tekmerion.Core` (tier 3: parsing, resolution, validation and
   contract projection, all pure functions over supplied text), and
   `Tekmerion.Cli` (tier 4: the only project that touches the filesystem).
2. **YamlDotNet 18.1.0 (pinned).** The slice corpus uses block scalars
   (`purpose: |`), flow sequences (`[GN-100, GN-130]`) and nested maps
   (`confidence: {evidence: …}`). A hand-written YAML subset parser would
   misread real research. The library is used only through its representation
   model, so scalars stay strings: dates are not coerced, and `YYYY-MM-DD` is
   not rejected. Each key's verbatim source text is sliced separately so that
   unknown keys round-trip exactly.
3. **Aegis at every host boundary** (doc 18 §2): configuration, repository
   listing, source reads, staging, promotion and recovery all go through
   `Aegis.capture`, with Tekmerion fault codes (`TEKMERION.*`), a
   standard-error sink, and blocking persistence, so a short-lived process does
   not lose fault events. Decoding invalid UTF-8 and malformed YAML are
   expected data outcomes, reported as findings rather than Aegis faults
   (doc 18 §2.5).
4. **FSharp.Core 10.1.400 in the host and its tests.** Aegis.Core 1.0.0
   requires it. The existing .NET 8 SDK compiles against it, so no SDK change
   was needed. The domain and core libraries keep the SDK default.
5. **Relationship keys.** Beyond the v0.2 set, the slice corpus declares
   explicit id lists under `related_artifacts` and `related_theory` (read as
   related-document links) and `tests` (a typed link). These are declared ids
   in named fields, not inferences. `references:` remains bibliography.
   Frontier records declare their origin document and prerequisites in fixed,
   labelled body lines (`- Origin document: [..](..)`, the `## Dependencies`
   list). Only those labelled lines are read, with their line numbers. REQ-TEK
   is amended to 1.1.0 (TEK-REL-001).
6. **Resolution has four outcomes**: resolves, out-of-scope (the file exists in
   the repository but is not published), dangling (a warning), and not-a-link
   (prose, URLs).
7. **Frontier scope rule `origin-in-scope`.** A frontier record is published
   only when its declared origin document is published.
8. **Contracts** are split into listing (`artifacts.json`), per-artifact detail,
   per-artifact edges and per-artifact content. Derived edges reference their
   basis by id, and that basis is in the same edges file, so explaining a
   relationship needs no extra fetch. Per-artifact shards are compact JSON.
   Nothing is stamped with a wall-clock time.
9. **Output is staged, verified and promoted** by two directory renames, with
   recovery on the next run. The live output is untouched until promotion.

## Alternatives rejected

- **A hand-written YAML subset parser.** Rejected on evidence: block scalars
  and nested maps occur in the slice.
- **Inferring links from body prose or bibliography.** Rejected: TEK-FID-003
  and C.3.
- **Treating `HY-`/`LAW-` ids under `tests` as resolved sub-document objects.**
  Rejected. No artifact declares them, and sub-document identity is gated on
  OQ-TEK-001. They stay visible as dangling warnings.
- **Raising the SDK to .NET 10 for Aegis.** Unnecessary, because the package pin
  suffices, and it would widen the change.

## Evidence

- Slice survey of `visual-engineering@7ef65a3`: 23 documents (22 + archived
  duplicate), 55 frontier records with an in-scope origin.
- Golden tests: `tests/Tekmerion.Core.Tests/GoldenCorpusTests.fs`.
- Full-corpus probe (recorded in `docs/architecture/tekmerion-ingestion.md`).

## Reversibility

The YAML library can be replaced behind `FrontMatter.parse`. The relationship
key set is data in `Reading.relationKeys`. Contracts are versioned under
`/data/v1/`.
