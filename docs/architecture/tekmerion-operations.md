---
id: ARCH-TEK-OPERATIONS
title: Tekmerion Slice — Operational Proof and Baselines
version: 1.0.0
status: accepted
created: 2026-09-29
updated: 2026-09-29
work_item: GH-19
related_documents:
  - docs/architecture/tekmerion-ingestion.md
  - docs/architecture/tekmerion-experience.md
  - docs/baselines/slice-2026-09-29.json
---

# Operational Proof and Baselines (first vertical slice)

## What is proven, and where

| #19 acceptance | Evidence |
| --- | --- |
| Two builds from identical inputs are byte-identical | `OperationalTests`: two full CLI builds are compared file by file. The CI workflow `tekmerion-slice.yml` runs `diff -r` on two ingests |
| Versioned manifest tied to source revision, Tekmerion version, schema versions and validation summary, with no wall-clock values in content | `data/v1/manifest.json` records `sourceRevision`, `producer.version`, `schemaVersion`, `parserProfile`, `validation` and per-file sha256. The publish time lives only in `publication.json`, which is outside the manifest and outside the content digest. A test checks the timestamp appears in no other file |
| Fatal failure leaves last-known-good intact | `OperationalTests`: after a good build, adding a duplicate id exits 1 and leaves the output tree byte-identical. `OutputTests` checks that the live output does not change before promotion |
| Derived caches and indexes can be deleted and rebuilt | Tekmerion keeps no cache: the output is the only derived state. `OperationalTests` deletes the output, rebuilds, and gets identical bytes |
| Malformed, unknown, dangling, duplicate, contradictory and interrupted cases | `OperationalTests` (a mixed adversarial repository through the CLI, duplicate blocking, interrupted-promotion recovery), `AdversarialTests`, `ReadingTests`, `FrontMatterTests` |
| Baseline for build time, output size, index size and browser payload | Below, and `docs/baselines/slice-2026-09-29.json`, produced by `scripts/tekmerion-baseline.mjs` |
| End to end, from corpus ingest to published navigation | CI `tekmerion-slice.yml`: ingest, then `tests/e2e/slice-navigation.mjs` in Chromium with JavaScript disabled |
| Repository and Echelon gates | 2026-09-29, this branch: `./ros verify` valid; `./ros doctor` 0 errors and 0 warnings; `./ros validate` passed; `limen verify --strict` passed; `sde verify` passed (one pre-existing structural review signal on `tools/ros_cli.mjs`); `visual-engineering verify` passed |

## Baseline (measured 2026-09-29, not a budget)

Slice fixture: `visual-engineering@7ef65a3`, 23 documents and 55 frontier
records. Release build, 5 runs, on a 4-vCPU Intel Xeon @ 2.10 GHz session
container.

| Measure | Value |
| --- | --- |
| Full ingest → render → stage → verify → promote, wall time including .NET start-up | min 811 ms, median 824 ms, max 839 ms |
| Output | 399 files, 4,423,214 bytes (4.4 MB of HTML and JSON) |
| `manifest.json` / `artifacts.json` / `edges.json` / `findings.json` | 42,911 / 67,199 / 100,909 / 161,752 bytes |
| Bounded retrieval, RP-COMP-005 (2 fetches) | 18,022 bytes |
| Browser payload, RP-COMP-005 page | 126,858 bytes (HTML + Forma CSS 102,460), **0 scripts** |
| Browser payload, project page | 143,299 bytes (HTML + CSS), 0 scripts |

The full-corpus probe (589 artifacts, 1.6–1.8 s) is recorded in
`tekmerion-ingestion.md`. The legacy publisher took 5.6 s for 756 documents,
but that figure included Astro rendering and Pagefind indexing, so the two are
not directly comparable.

No thresholds are set. The next measurement against the same fixture is what
turns this record into a trend (TEK-PRF-001).

## Limits of this proof

- The publication record's time is the host clock and is not signed.
- Promotion uses two directory renames with recovery, not a single atomic
  operation.
- Deployment to GitHub Pages is not exercised; `tekmerion-slice.yml` uploads
  the site as a build artifact only. The Pages workflow still publishes the
  legacy site.
- Browser checks run in Chromium only.
