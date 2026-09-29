---
id: ARCH-TEK-INGESTION
title: Tekmerion Ingestion and Machine Contracts (first vertical slice)
version: 1.0.0
status: accepted
created: 2026-09-29
updated: 2026-09-29
work_item: GH-17
related_documents:
  - docs/architecture/tekmerion-domain.md
  - research/decisions/DF-TEK-2026-0002--typed-ingestion-slice.md
  - docs/vnext/13-vertical-slice.md
---

# Tekmerion Ingestion and Machine Contracts

## Pipeline

```
host (Tekmerion.Cli, tier 4, Aegis)          core (Tekmerion.Core, tier 3, pure)
─────────────────────────────────────        ─────────────────────────────────────────
read config ─ list repository ─ read files ─▶ Reading.read  (front matter, sections, keys)
                                             Frontier.references (labelled body links)
                                             Corpus.ingest (scope, resolve, validate,
                                                            derive backlinks, assess)
                                             Contracts.project (/data/v1 files)
stage ─ verify ─ promote ◀────────────────── (files, digest)
```

Every stage is a legal `Publication.apply` transition (GH-14). The run reaches
`Promoted` only after staging has been verified on disk.

## Commands

```
tekmerion validate --config tekmerion.config.json [--root DIR] [--json]
tekmerion ingest   --config tekmerion.config.json [--root DIR] --out DIR [--json]
```

Exit codes: `0` success, `1` publication blocked (nothing written), `2` invalid
arguments, `6` environment failure (an Aegis fault; the previous publication is
unchanged).

Configuration (`tekmerion.config.json`, schema 1):

```json
{
  "schemaVersion": 1,
  "repository": "kemiller2002/visual-engineering",
  "include": ["content/projects/composition-science/**/*.md"],
  "frontier": { "records": "research/frontier/records/*.md", "scope": "origin-in-scope" },
  "repositoryFiles": "optional path to a file listing repository paths"
}
```

## Contracts (`/data/v1/`, schema 1.0.0)

| File | Purpose |
| --- | --- |
| `manifest.json` | Entry point: producer version, input digest, lifecycle state, conventions, counts, index paths, and the path/bytes/sha256 of every other file |
| `artifacts.json` | One row per artifact for listing and filtering. No HTML |
| `artifact/{a\|s}/{key}.json` | Detail: identity, type and type source, verbatim status and derived class, extensions (unknown keys, verbatim), outline, provenance, findings |
| `edges/{a\|s}/{key}.json` | Canonical outgoing and incoming edges plus derived backlinks. Each backlink's basis ids point at the `incoming` edges in the same file |
| `content/{a\|s}/{key}.json` | Section bodies as Markdown source, with locations |
| `edges.json`, `findings.json` | Whole-corpus indexes |

Conventions: `null` means not declared by the source; other absences are
objects with an `absent` field. `derived: true` marks every non-canonical edge.
`a/` is a declared id and `s/` is a path-derived key.

## Slice results (pinned snapshot `visual-engineering@7ef65a3`)

| Measure | Value | Requirement |
| --- | --- | --- |
| Slice documents read | 23 (22 with front matter, 1 archived duplicate without) | M.4-1 |
| Frontier records in scope | 55 | TEK-QST-001 |
| Declared ids / path keys (authored) | 9 / 14 | TEK-IDY-001/002 |
| `source_rep` edges resolving to RP-COMP-005 | 4 of 4 (legacy engine: 0) | M.4-2 |
| Canonical edges | 104: origin-document 55, prerequisite 33, related-document 5, source-rep 4, typed-link:tests 7 | TEK-REL-001 |
| Derived backlinks | 97 (one per resolved canonical edge) | TEK-REL-002 |
| Findings | 0 blocking, 7 warnings (unresolved `tests` ids), 334 informational | TEK-VAL-002/003 |
| RP-COMP-005 full context | 2 fetches, 17,186 bytes | TEK-CON-003 (≤ 20 KB) |
| Fabricated legacy defaults in output | none (`2026-07-22`, `General Research`) | M.4-3 |
| Two builds | byte-identical | TEK-ARC-004 |

## Full-corpus probe (not a gate)

`content/**/*.md` plus in-scope frontier records of `visual-engineering@7ef65a3`,
Release build, 3 consecutive runs on the session container:

| Measure | Value |
| --- | --- |
| Artifacts | 589 (144 authored, 445 of 520 frontier records) |
| Canonical edges | 809: origin-document 445, prerequisite 267, related-document 86, source-rep 4, typed-link:tests 7. Of these, 778 resolve, 7 are out of scope, 18 dangling, 6 not links |
| Findings | 0 blocking, 32 warnings (10 bare filenames, 4 contradictory type keys, 18 dangling) |
| Ingest wall time | 1,629 / 1,788 / 1,845 ms (includes .NET start-up) |
| Output size | 17 MB |

**TEK-REL-005 (≥ 850 canonical edges on the full corpus) is not met** under
this scope: 809 were measured. The 75 frontier records whose origin document is
outside `content/**` are excluded by `origin-in-scope`. Widening the scope is a
configuration decision, not something to tune until the number passes. The
requirement stays open.

## Known limitations

- HTML is produced by GH-18 (see `tekmerion-experience.md`).
- Legacy URL redirects are emitted by GH-18 from the committed legacy catalog.
- `publication.json` (wall-clock record, TEK-PUB-002) is GH-19.
- The frontier-record id stability question (OQ-TEK-002) is unresolved, so
  frontier URLs are not advertised as permanent.
- Promotion is two renames, not one atomic operation. An interruption between
  them is repaired by `recover` on the next run, which the tests cover.
