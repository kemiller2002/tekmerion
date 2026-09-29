---
id: ARCH-TEK-EXPERIENCE
title: Tekmerion Static Research Experience (first vertical slice)
version: 1.0.0
status: accepted
created: 2026-09-29
updated: 2026-09-29
work_item: GH-18
related_documents:
  - docs/architecture/tekmerion-ingestion.md
  - research/decisions/DF-TEK-2026-0003--static-research-experience.md
  - docs/vnext/09-research-experience.md
---

# Tekmerion Static Research Experience

`tekmerion ingest … --forma node_modules/@echelon-foundry/design-system/dist/all.css`
now writes a static site beside the `/data/v1/` contracts. Every page is a
script-free, semantic HTML document generated in F# (`Tekmerion.Core/Html.fs`,
`Site.fs`). Navigation uses ordinary links, so browser back/forward,
bookmarking and sharing work natively with JavaScript and WASM disabled
(TEK-EXP-001). All links are relative, so the site works under any base path.

## Pages

| Address | Content | Requirements |
| --- | --- | --- |
| `/` | Projects, and authored artifacts with no declared project | — |
| `/p/{project}/` | Purpose and where to start (declared `entryPoint` artifacts only); open frontier; current research position (verbatim status plus Tekmerion's reading); in-flight work (outstanding obligations named in statuses, and warnings); integrity. No "recent activity", because dates are not trustworthy | TEK-EXP-004 |
| `/a/{id}/`, `/s/{path-key}/` | Record header; identity (declared, or path-derived with the reason); type and its source; declared metadata with explicit "not declared"; source and provenance trail linking to the pinned revision and line; canonical relationships declared by this artifact; derived relationships pointing here, each with **"Why is this shown?"**; preserved extension metadata; validation notes; content | TEK-EXP-005, TEK-REL-003, TEK-PRV-002 |
| `/integrity/` | Every finding by severity | TEK-INT-001 |
| `/research/{legacy-slug}/` | Redirect stub (meta refresh plus canonical link) for each URL the legacy publisher emitted for an in-scope file, taken from its committed catalog | TEK-IDY-006 |
| `/404.html` | Fallback for GitHub Pages | — |

"Why is this shown?" names the derivation rule and each canonical declaration
it came from: the declaring artifact (linked), the relation, the reference as
written, and the exact source location (linked to the pinned commit and line).

## Safety

- Markdown bodies are rendered by Markdig with raw HTML disabled, so
  `<script>` becomes text.
- Only `http(s):` and `mailto:` links survive as external links, with
  `rel="noopener noreferrer"`.
- Relative links to published artifacts are rewritten to their stable pages.
  Other relative links and images become plain text, so nothing points at an
  unpublished or unsafe target.
- All other text goes through an HTML model that escapes by construction
  (TEK-SEC-001/003).

## Forma and Visual Engineering

- Forma `@echelon-foundry/design-system` **0.2.0**, pinned exactly in
  `package.json`. The stylesheet is copied unmodified from the pinned package
  into `assets/forma/all.css` at build time. No Forma CSS is copied into the
  repository and no local stylesheet exists (doc 18 §3).
- Patterns used: `ef-record-header`, `ef-status-lozenge` (label text always
  carries the meaning), `ef-key-value-list`, `ef-provenance-trail`,
  `ef-disclosure`, `ef-obligation-panel`/`ef-obligation`, `ef-data-grid`,
  `ef-cluster`, `ef-stack`, `ef-visually-hidden`.
- Without `--forma` the pages are unstyled semantic HTML, which is an explicit
  degradation (TEK-ECH-003).

**Visual Engineering report** (context 1.0.0, source commit `0be73c8`; `verify`
passed). I read `AGENT-INSTRUCTIONS`, `UI-FOUNDATIONS`,
`UI-DECISION-CHECKLIST` and `UI-ANTI-PATTERNS` in full. I skimmed the
987-line `RESEARCH-INDEX` by heading rather than reading all of it.

Principles applied:

- One primary path per view: identity → provenance → relationships → content.
- Recognition versus verification: the record header serves first glance; the
  provenance trail and the "why" disclosures serve deliberate verification.
- Grouping by meaning: canonical and derived relationships are separate
  sections.
- Status never relies on colour: lozenges carry the text.
- Semantic HTML first, with a skip link and landmarks.
- Source order equals visual order: no CSS reordering.
- Missing data is designed for, not hidden: "not declared" is shown.

## Verification performed

| Check | Result |
| --- | --- |
| `SiteTests` (13 tests): script-free pages, one `h1` per page, **every internal link on every page resolves**, EX → REP → back links, 9 "why" disclosures on RP-COMP-005 naming declaring file, key and line, canonical and derived sections separate, no title-derived address, 77/77 legacy URLs redirect to the right artifact, pinned-revision source links, project sections, deterministic rendering, hostile Markdown escaped | pass |
| `tests/e2e/slice-navigation.mjs` in Chromium, **JavaScript disabled**: EX-COMP-011 → RP-COMP-005, back, forward, → originating frontier record; "why" names the canonical declaration; legacy URL redirects; first Tab reaches the skip link | pass (8/8) |
| 320px horizontal overflow (measured, not gated) | EX-COMP-011: 0px. Project page: **11px** (source not identified; no rendered box exceeds the viewport). RP-COMP-005: **46px**, from a fenced preformatted diagram in the research content |

## Open obligations (GH-18 remains active)

1. **Limen interactive layer: not implemented.** The slice has no behaviour
   beyond native links, so the Limen boundary is still empty and
   `limen verify --strict` passes with an empty boundary. Exit criterion M.4-6
   ("non-empty boundary") and #18's "back/forward through Limen" are **not
   met**. Native links provide correct history today. A Limen engine (F#→WASM)
   and kernel for copy-link (TEK-EXP-012) and in-page filtering is the next
   increment.
2. **Forma gaps (recorded, not forked locally):** no page shell or inline
   gutter primitive, so content touches the viewport edge; no `pre` overflow
   rule, which causes the 46px overflow at 320px. Both are recorded as local
   backlog items against Forma.
3. The source of the 11px overflow on the project page is unidentified.
4. Screen-reader, forced-colours and 200% zoom checks have not been run.
   WCAG 2.2 AA is **not claimed** (TEK-ACC-001).
