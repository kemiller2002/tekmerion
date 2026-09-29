---
id: REQ-TEK
title: Tekmerion Canonical Requirements
version: 1.0.0
status: accepted-baseline
created: 2026-09-29
updated: 2026-09-29
owners:
  - tekmerion-product
work_item: GH-13
supersedes:
  - docs/vnext/15-requirements-v0.2.0.md (REQ-RP-VNEXT 0.2.0, draft)
  - input-documents/research-publisher-vnext-requirements.txt (unversioned draft, 550b1b9)
incorporates:
  - GitHub issue #11 (Tekmerion vNext umbrella, requirements sections 1-20)
  - docs/vnext/18-shared-application-foundations.md (normative, retained in place)
related_documents:
  - docs/requirements/traceability.md
  - docs/requirements/open-questions.md
  - docs/vnext/16-final-analysis.md
  - docs/vnext/13-vertical-slice.md
  - docs/vnext/17-requirements-section-29-answers.md
  - research/decisions/DF-TEK-2026-0001--product-identity-and-requirements-baseline.md
tags: [requirements, tekmerion, canonical]
---

# Tekmerion Canonical Requirements

Tekmerion (formerly **Research Publisher**) is an evidence-faithful research
system. It reads durable research state from a repository, validates it,
exposes its structure, and projects it into human-readable and machine-readable
publications. Publishing is one projection of research state, not the product's
identity.

This document is the **single canonical requirement set**. Earlier documents
remain as historical evidence and are not edited to say "Tekmerion"; where they
conflict with this document, this document governs. Every normative requirement
has a stable identifier. Mapping from every earlier requirement is in
[`traceability.md`](traceability.md); open decisions are in
[`open-questions.md`](open-questions.md).

## How to read this document

### Identifier contract

`TEK-<AREA>-<NNN>`. An identifier is **never reused or renumbered**. A
requirement that changes meaning gets a new identifier; the old one is marked
`superseded` with a pointer. Wording may be clarified without changing the
identifier only when the meaning, the classification and the tests that cite it
are unchanged. Tests, work items and commits cite these identifiers.

### Classification

| Class | Meaning |
| --- | --- |
| `now` | Applies to every change from this baseline onward, including the current (legacy) implementation where it touches the subject. |
| `slice` | Required by the first vertical slice (issues #17, #18, #19). Also binding afterwards. |
| `target` | Target capability. Binding direction for design; not yet required to be implemented. Must not be designed out. |
| `gated` | Target capability that **cannot** be implemented honestly until a named precondition (usually an authoring contract) exists. Implementing it before the gate opens violates `TEK-FID-001`. |
| `compat` | Compatibility / migration obligation with a defined end. |
| `deferred` | Explicitly out of scope until re-opened by a decision; the reason is recorded. |
| `superseded` | Retained for traceability only. |

Keywords MUST / MUST NOT / SHOULD / MAY are used in the RFC 2119 sense.

### The evidence constraint that shapes everything else

Discovery against the real corpus (`docs/vnext/00`–`17`) established that the
corpus provides strong **document-level** identity and relationships and **no
canonical identity for any sub-document research object** — no claim, finding,
evidence item, or support/contradiction relation carries an identifier anywhere
(`16-final-analysis.md` §4–5, `05-requirements-gap-analysis.md` E.3). Tekmerion
therefore distinguishes three things throughout:

1. **Types** Tekmerion understands (it may define `Claim`, `Evidence`,
   `Finding`, `Supports`, `Contradicts`, …).
2. **Authoring contracts** that let research declare instances of those types
   with explicit identity and provenance.
3. **Instances**, which exist only when declared through such a contract (or
   another approved deterministic contract). Tekmerion never manufactures an
   instance from prose, similarity, position, or absence.

---

## 1. Product identity — `IDN`

- **TEK-IDN-001** `now` — The product name is **Tekmerion**. Current and
  normative documentation, CLI output, generated output, and package metadata
  MUST use it. *(#11 §19, #13)*
- **TEK-IDN-002** `now` — Historical records (discovery reports, ADRs, decision
  logs, journals, telemetry, prior requirement drafts) MUST NOT be rewritten to
  the new name. A blind global search-and-replace is forbidden. *(#13, #15)*
- **TEK-IDN-003** `compat` — The naming contract is: repository
  `kemiller2002/tekmerion`; npm package `@echelon-foundry/tekmerion`; executable
  `tekmerion`; user configuration `tekmerion.config.json`; Echelon manifest
  `.echelon/tekmerion.json`; generated cache root `.tekmerion/`; F# namespace
  and project prefix `Tekmerion.`; environment-variable prefix `TEKMERION_`.
  The rationale and alternatives are in `DF-TEK-2026-0001`. The names become
  binding for a published release only when #15 ships the migration. *(#11 §19,
  #15)*
- **TEK-IDN-004** `compat` — The legacy executable `research-publisher`, the
  legacy `research:*` npm scripts, and `research-publisher.config.mjs` MUST keep
  working as deprecated aliases for a declared deprecation period of at least
  one minor release after the first Tekmerion release, emitting a deprecation
  notice that names the replacement. *(#11 §19, #15)*
- **TEK-IDN-005** `now` — Package metadata (repository, homepage, bugs URLs)
  MUST point at `kemiller2002/tekmerion`. *(#15)*

## 2. Canonical source and research state — `CAN`

- **TEK-CAN-001** `now` — Repository Markdown research artifacts (ROS/Praxis
  format) are the canonical source of research state. No database, JSON store,
  or generated index is ever the source of truth. *(draft §0.2, ADR 0001, #11 §15)*
- **TEK-CAN-002** `now` — Existing valid research MUST NOT require migration,
  conversion or manual edits in order to be read, validated or published.
  *(v0.2 R1.1, draft §18.5)*
- **TEK-CAN-003** `now` — Reading, validating, indexing, projecting and
  publishing MUST NOT write to any canonical research file, nor to
  Praxis/ROS-owned state (`ros.json`, `registries/`, `.ros/`, `schemas/`).
  *(v0.2 R1.4, draft §30.10)*
- **TEK-CAN-004** `target` — Mutation of canonical research state, when
  supported, happens only through explicit, separately invoked transition
  commands that (a) are legal in the current state (`TEK-STA-*`), (b) write
  through a declared authoring contract, (c) record provenance
  (`TEK-AGT-004`), and (d) are atomic or recoverable (`TEK-REC-001`). No build,
  validation or publication step ever mutates research as a side effect. This
  reconciles #11 §12 (agents propose transitions) with draft §30.10 (no hidden
  mutation). *(#11 §1, §12, draft §30.10)*
- **TEK-CAN-005** `now` — Every value Tekmerion exposes is classified by
  origin as exactly one of: **authored** (declared in canonical source),
  **derived** (deterministically computed from authored values by a named
  rule), or **asserted** (a human or agent judgement recorded with provenance).
  Origins MUST be distinguishable in the model, in every machine contract and
  in the UI. *(v0.2 R2.4, draft §3.5, #11 §7)*

## 3. Fidelity and no fabrication — `FID`

- **TEK-FID-001** `now` — No value may be fabricated: no invented identifiers,
  dates, authors, research areas, types, statuses, versions, relationships,
  research objects or metric values. *(v0.2 R2.1, ADR 0010, draft §20.1)*
- **TEK-FID-002** `now` — Absence, unknown, unavailable, not-applicable and
  known-zero are distinct states and MUST remain distinguishable in the model
  and in every output (`null` is not `0`; "not declared" is not "none").
  *(ADR 0010, #11 §7, AGENTS.md telemetry rules)*
- **TEK-FID-003** `now` — Semantic similarity, co-occurrence, shared words,
  proximity or LLM inference is never evidence of a relationship and never
  creates a canonical relationship. *(draft §3.3, #11 §4)*
- **TEK-FID-004** `now` — A derived reading (e.g. a status class) is published
  alongside the verbatim source value, never instead of it. *(ADR 0010, F.4)*
- **TEK-FID-005** `now` — Contradictory research is presented as contradictory;
  Tekmerion never merges disagreeing sources into a synthetic consensus.
  *(draft §20.4, #11 §6)*
- **TEK-FID-006** `now` — Missing or unresolved information is represented as
  missing or unresolved, never guessed or silently repaired. *(draft §20.5,
  §27)*

## 4. Ingestion and corpus compatibility — `ING`

- **TEK-ING-001** `slice` — Discovery is configuration-driven (research roots
  and globs); Praxis/ROS canonical roots are a cross-check, not a constraint,
  because real corpora live outside them. *(v0.2 R1/§2.1 refinement, D.4)*
- **TEK-ING-002** `slice` — The parser MUST accept every observed compatibility
  form: `document_type`/`artifactType`; `-` and `_` casing of type values;
  `related_documents`/`relatedDocuments`; `research_area`/`researchArea`;
  `superseded_by`, `source_rep`, `evidence_level`, `author_agent`,
  `related_projects`; `date`/`created`/`updated`; `abstract`/`summary`;
  file-relative and repository-root-relative paths; free text in link fields;
  absent `id`; absent front matter. *(v0.2 R1.2)*
- **TEK-ING-003** `slice` — Unknown front-matter keys and unknown body sections
  MUST be preserved verbatim and exposed as extension content; never silently
  dropped. An unknown key is informational, never an error. *(v0.2 R1.3, draft
  §2.4, §13.4)*
- **TEK-ING-004** `slice` — Parse problems identify file, section, line where
  practical, expected form, actual form, severity, and whether publication can
  continue. *(draft §2.5)*
- **TEK-ING-005** `slice` — One malformed artifact MUST NOT prevent processing
  of the rest; it is quarantined, reported, and counted. Fatality is decided by
  explicit policy (`TEK-VAL-002`). *(draft §2.6, G.3)*
- **TEK-ING-006** `slice` — Bibliographic `references:` entries are citations,
  not internal links, and MUST NOT be resolved as relationships. *(C.3, §29 Q17)*
- **TEK-ING-007** `target` — A research artifact may declare a format version;
  an artifact declaring a newer format than Tekmerion knows is parsed with the
  newest known profile and produces a warning, not a failure. Each artifact
  records the parser profile that read it. *(draft §1.1, §19.2, G.8)*
- **TEK-ING-008** `target` — Machine-generated records (e.g. frontier records)
  are a distinct population from authored research and MUST NOT be averaged
  with it in counts, coverage or health metrics. *(E.5, R14 risk register)*

## 5. Identity, addresses and URLs — `IDY`

- **TEK-IDY-001** `slice` — A declared `id` is authoritative and permanent.
  Identifiers are never invented. *(v0.2 R3.1, J.4)*
- **TEK-IDY-002** `slice` — An artifact without a declared id receives a stable
  path-derived key (from its repository-relative source path), which is
  explicitly second-class, never presented as a declared id, and never written
  back to the corpus. *(v0.2 R3.3, J.2)*
- **TEK-IDY-003** `slice` — A public URL MUST NOT be derived from a title.
  *(v0.2 R3.2)*
- **TEK-IDY-004** `slice` — Artifacts with a declared id are addressed at
  `/a/{id}/`; others at `/s/{path-key}/`. *(v0.2 R3.3, J.2)*
- **TEK-IDY-005** `slice` — Two artifacts declaring the same id, or two
  artifacts mapping to the same published URL, is a blocking finding.
  *(v0.2 R5.2, draft §5.3)*
- **TEK-IDY-006** `compat` — Every previously published URL within the
  published scope MUST be emitted or redirected; otherwise the build is
  blocked. `/collections/{facet}/{value}/` is preserved. *(v0.2 R3.4, R3.5)*
- **TEK-IDY-007** `slice` — Section anchors are best-effort and MUST degrade to
  the document address rather than 404 or point at the wrong section.
  *(v0.2 R3.6)*
- **TEK-IDY-008** `target` — Identifiers are designed so that multi-repository
  aggregation is possible later (keys are qualified by repository identity in
  machine contracts). Aggregation itself is not required. *(draft §30.1, §29 Q6)*

## 6. Relationships — `REL`

- **TEK-REL-001** `slice` — Canonical relationships are only those encoded by
  the research itself. The observed canonical set is: `related_documents`
  (untyped), `source_rep`, `supersedes`, `superseded_by`, `originates`,
  `prerequisite`, and explicit typed id lists (`evidenceIds`, `hypothesisIds`,
  `theoryIds`) when present. *(v0.2 change summary, C.1, §29 Q2)*
- **TEK-REL-002** `slice` — Derived relationships (backlinks, same-project,
  frontier-of, supersession chains) are a distinct type from canonical
  relationships in code, in every machine contract (`derived: true` plus the
  derivation rule) and in the UI. *(v0.2 R2.4, ADR 0009)*
- **TEK-REL-003** `slice` — Every derived relationship carries the canonical
  facts it was derived from, so a user or agent can answer "why is Tekmerion
  showing me this relationship?" and navigate to the canonical evidence.
  *(#18, #11 §11, draft §3.4)*
- **TEK-REL-004** `slice` — Every reference that fails to resolve appears as a
  visible, non-blocking finding. Silent dropping is forbidden. *(v0.2 R2.3)*
- **TEK-REL-005** `target` — Relationship recall on the full
  `visual-engineering` corpus is at least 850 canonical edges (baseline: the
  legacy engine derives 6). *(v0.2 R2.2)*
- **TEK-REL-006** `gated` — The relationship vocabulary `supports`,
  `contradicts`, `evidence-for`, `evidence-against`, `derived-from`,
  `produced-by`, `answers`, `raises`, `validates`, `invalidates`, `depends-on`
  is a **target vocabulary**. Tekmerion may define types for it; instances
  exist only when declared through an authoring contract.
  **Gate:** `OQ-TEK-001`. *(draft §3.3, v0.2 change summary)*

## 7. Provenance — `PRV`

- **TEK-PRV-001** `slice` — Every published fact carries provenance sufficient
  to locate its original source: repository, repository-relative path, and
  where known the front-matter key, heading path and line. *(v0.2 R6.4, draft
  §3.4, F.7)*
- **TEK-PRV-002** `slice` — Source provenance is reachable in one interaction
  from every artifact view. *(v0.2 R7.4, draft §9.6)*
- **TEK-PRV-003** `target` — Provenance can carry the source commit; the field
  exists from the first slice and may be unpopulated (`null`, not a
  placeholder). *(M.5, draft §30.4)*
- **TEK-PRV-004** `target` — Authorship/contribution provenance blocks declared
  by research artifacts (Praxis provenance interchange) are transported
  verbatim, never reinterpreted, and lineage is kept separate from authorship.
  The detailed rules are the proposed `REQ-RP-PROV` in draft PR #12; they
  become normative here when that PR merges. Until then this requirement binds
  only the principle. *(PR #12, Praxis RQ-ROS-2026-A008/A009/A015)*

## 8. Validation, obligations and findings — `VAL`

- **TEK-VAL-001** `slice` — Validation findings carry a stable code, severity,
  source span, message, and remedy where one exists, and are emitted as a
  machine-readable artifact usable by humans and CI. *(draft §5.6, §5.7, §22.2)*
- **TEK-VAL-002** `slice` — Only these block publication: duplicate id,
  duplicate URL, unreadable source, output path escaping the output root, and a
  previously published URL lost without redirect. Content explicitly marked
  non-public (`TEK-SEC-004`) also blocks. *(v0.2 R5.2, #11 §14)*
- **TEK-VAL-003** `slice` — Legitimate incomplete research states
  (`supersedes: null`, `…-pending`, `Open`, orphans, missing ids, missing
  declared types, dangling references) MUST NOT block publication and MUST NOT
  be presented as defects. *(v0.2 R5.3, G.5)*
- **TEK-VAL-004** `slice` — Severity levels are `blocking`, `warning` and
  `informational`; the draft's `error`/`fatal` map to `blocking`.
  *(draft §5.6 reconciled with G.5)*
- **TEK-VAL-005** `slice` — Obligations are derived from unresolved work and
  exposed with the state they attach to; a blocking obligation prevents the
  transitions it names and nothing else. *(draft §1.2, #11 §1)*
- **TEK-VAL-006** `slice` — The same validation engine runs locally and in CI;
  pull requests receive validation results before publication. *(draft §22.3,
  §22.4)*
- **TEK-VAL-007** `target` — Contradictory metadata inside one artifact (e.g.
  both `artifactType` and `document_type` with different values) is preserved
  in full and reported as a warning; Tekmerion does not pick a winner silently.
  *(#19, TEK-FID-005)*

## 9. State, transitions and capabilities (Ordo/SDE) — `STA`

- **TEK-STA-001** `slice` — Corpus, artifact and publication lifecycles are
  modelled as explicit states with explicit legal transitions; illegal states
  are unrepresentable where practical. *(v0.2 R5.1, draft §1.2, §4)*
- **TEK-STA-002** `slice` — Capabilities (legal actions) are computed from
  state; an illegal action is refused with a typed domain outcome, not an
  exception. *(draft §1.2, §4.4)*
- **TEK-STA-003** `slice` — External effects are explicit and confined to the
  host tier; no writes before `Emit`; no network access during build.
  *(v0.2 R5.4, G.9)*
- **TEK-STA-004** `target` — An external operation whose completion can be
  ambiguous (deployment, remote transition, remote publication) records an
  explicit `unknown` outcome and supports reconciliation; unknown is never
  treated as success. *(#14, work-protocol adapter contract)*
- **TEK-STA-005** `target` — Review state is explicit (`TEK-REV-*`) and human
  approval is a distinct transition from agent generation. *(#11 §9)*

## 10. Architecture — `ARC`

- **TEK-ARC-001** `now` — F# implements discovery, parsing, domain modelling,
  validation, relationship resolution, projection, index generation, URL
  minting and their tests. Any non-F# component requires a documented
  justification. *(v0.2 R4.1, ADR 0004, draft §1.4)*
- **TEK-ARC-002** `now` — Static-first: output is static files; no server, no
  database and no runtime service unless measured evidence justifies one
  through a decision record. *(v0.2 R4.4, ADR 0003, #11 §16)*
- **TEK-ARC-003** `now` — Minimal dependencies; no large frontend framework;
  every new dependency is pinned and justified. *(v0.2 R4.5, draft §1.5, doc 18
  §1)*
- **TEK-ARC-004** `slice` — Output is deterministic: two builds from identical
  inputs are byte-identical for all content artifacts; wall-clock values appear
  only in an explicitly non-content publication record. *(v0.2 R4.6, draft
  §30.9, #19)*
- **TEK-ARC-005** `now` — Tiering follows SDE: semantic model → state
  transitions → projection/orchestration → host effects; dependencies point
  inward. *(G.1)*
- **TEK-ARC-006** `compat` — The legacy Node/Astro publisher remains usable
  until the F# pipeline demonstrates equivalent or better coverage of the
  existing corpus; it is not rewritten before its compatibility role is
  understood. *(draft §18.3, #11)*

## 11. Browser boundary and experience — `EXP`

- **TEK-EXP-001** `slice` — Every artifact is a static semantic HTML page,
  fully readable with JavaScript and WASM disabled. *(v0.2 R4.3, draft §26.1)*
- **TEK-EXP-002** `slice` — Interactive browser behaviour follows Limen: an
  engine that names no browser capability and a thin kernel that owns them;
  research-domain logic is never implemented in ad hoc JavaScript.
  *(v0.2 R4.2, draft §1.3, ADR 0005)*
- **TEK-EXP-003** `slice` — Interactive UI uses Forma (`doc 18` §3) and
  semantic HTML remains the accessibility authority.
- **TEK-EXP-004** `slice` — The project view shows, where supported by
  canonical data: purpose, open frontier, current research position, in-flight
  work and obligations, and integrity — not a file tree and not fabricated
  "recent activity". *(v0.2 R7.1, §29 answer 6)*
- **TEK-EXP-005** `slice` — The artifact view shows identity (declared or
  path-derived, labelled), source and provenance, canonical metadata, canonical
  relationships, derived relationships (distinctly), unknown/extension
  content, and relevant validation findings. *(#18)*
- **TEK-EXP-006** `slice` — Relationship traversal is reachable in one
  interaction from any artifact; navigation round-trips such as
  experiment report → research execution package → back, and research
  execution package → originating frontier records, work with correct browser
  back/forward. *(v0.2 R7.3, R7.5, M.4)*
- **TEK-EXP-007** `slice` — Shareable state (project, route, artifact, lens,
  query, filters) lives in the URL. *(v0.2 R7.5)*
- **TEK-EXP-008** `slice` — Views are built only where the corpus supports
  them. *(v0.2 R7.2)*
- **TEK-EXP-009** `gated` — Finding, evidence, claim and contradiction views.
  **Gate:** `OQ-TEK-001`. *(draft §6.3, §6.4, §6.7; v0.2 R7.2)*
- **TEK-EXP-010** `deferred` — Timeline view. Reason: 735/756 legacy records
  carry a fabricated date; no trustworthy chronology exists yet. Re-open when
  declared dates or repository history are available. *(draft §6.9)*
- **TEK-EXP-011** `target` — Search across research objects with structured
  filters, built from the typed model rather than rendered HTML, static and
  sharded. *(draft §8, ADR 0008, #11 §11)*
- **TEK-EXP-012** `target` — Clipboard copying of stable links, identifiers and
  citations through Limen. *(draft §9.8, §30.3)*

## 12. Machine-readable contracts and agent retrieval — `CON`

- **TEK-CON-001** `slice` — Versioned indexes are published under `/data/v1/`
  with `manifest.json` as the single entry point; every index carries
  `schemaVersion`; breaking changes bump the major version with an upgrade
  path. *(v0.2 R6.1, draft §12.6, §19.3)*
- **TEK-CON-002** `slice` — Metadata indexes contain no rendered HTML.
  *(v0.2 R6.2)*
- **TEK-CON-003** `slice` — Bounded retrieval: the full context of one
  artifact (`RP-COMP-005` as the reference case) is retrievable in ≤ 2 fetches
  and ≤ 20 KB, unless measurement proves the threshold invalid and this
  requirement is revised with evidence. *(v0.2 R6.3, #17)*
- **TEK-CON-004** `slice` — Contracts state which requested relations cannot be
  supplied rather than inferring them. *(K.3)*
- **TEK-CON-005** `compat` — `research-catalog.json` v1.1 remains as a
  compatibility shim for one release after the v1 contracts ship. *(v0.2 R6.5)*
- **TEK-CON-006** `target` — Agents can discover legal actions (capabilities)
  and unresolved obligations from machine contracts, and request transitions
  rather than mutating state. *(#11 §12)*

## 13. Publication — `PUB`

- **TEK-PUB-001** `slice` — Publication is a projection of canonical research
  state; GitHub Pages and GitHub Actions are supported. *(draft §10, #11 §13)*
- **TEK-PUB-002** `slice` — Each publication emits a manifest tying output to
  source revision, Tekmerion version, schema versions and validation summary.
  *(draft §10.5, #11 §13)*
- **TEK-PUB-003** `slice` — A failed build leaves the last known-good
  publication intact. *(draft §10.6, #11 §13, #19)*
- **TEK-PUB-004** `slice` — Derived caches and indexes are disposable and
  rebuildable; canonical research never depends on them. *(#11 §15, #19)*
- **TEK-PUB-005** `now` — A full deterministic rebuild is always possible;
  incremental rebuild is allowed only when dependency invalidation is proven
  correct. *(draft §11, #11 §16)*

## 14. Research lifecycle target capabilities

These incorporate #11 §§1–10. They define direction. Each is `target` unless
marked `gated`; a `gated` requirement's instances require the named authoring
contract first.

### 14.1 Workspace — `WSP`

- **TEK-WSP-001** `target` — A research workspace has explicit identity,
  objective, scope, status, owners/agents, inputs, outputs, constraints and
  completion criteria, read from canonical records. *(#11 §1)*
- **TEK-WSP-002** `target` — Work resumes from repository state alone.
  *(#11 §1, AGENTS.md)*
- **TEK-WSP-003** `target` — Concurrent research efforts do not mix evidence or
  conclusions; every object belongs to exactly one workspace scope.
  *(#11 §1)*

### 14.2 Questions and planning — `QST`

- **TEK-QST-001** `slice` — Frontier records (structured open research
  questions with declared ids) are first-class navigable question objects.
  *(E.1 §6.8, §29 answer 6)*
- **TEK-QST-002** `target` — Primary questions, subquestions, hypotheses,
  assumptions, decision criteria and information needs are typed objects with
  status and "what would resolve this". *(#11 §2)*
- **TEK-QST-003** `target` — Plan evolution preserves why scope changed.
  *(#11 §2)*

### 14.3 Negative knowledge — `NEG`

- **TEK-NEG-001** `target` — Searched-for-but-not-found evidence, failed
  approaches, rejected hypotheses and unresolved unknowns are representable and
  distinct from absence of a record. *(#11 §1)*
- **TEK-NEG-002** `now` — Tekmerion never infers negative knowledge from
  absence: "no record found" is not "searched and found nothing". *(TEK-FID-002)*

### 14.4 Sources and acquisition — `SRC`

- **TEK-SRC-001** `target` — Sources have identity, location/URI, type,
  author/publisher when known, retrieval time, version/date and provenance.
  *(#11 §3)*
- **TEK-SRC-002** `target` — Acquisition failures and inaccessible sources are
  recorded. *(#11 §3)*
- **TEK-SRC-003** `target` — Duplicate/near-duplicate source detection MAY
  propose candidates but MUST NOT conflate distinct versions or merge records
  without an asserted decision. *(#11 §3, TEK-FID-003)*
- **TEK-SRC-004** `target` — Primary/secondary/derived source classification is
  recorded only where knowable; otherwise `unknown`. *(#11 §3)*

### 14.5 Evidence, claims and findings — `EVD`

- **TEK-EVD-001** `gated` — Evidence, claims and findings are independently
  addressable typed objects with stable declared identity and exact source
  location. **Gate:** `OQ-TEK-001`. *(#11 §4, §5)*
- **TEK-EVD-002** `gated` — Evidence polarity (`for`, `against`,
  `contextual`, `insufficient`) is recorded only as declared; Tekmerion never
  forces a polarity. **Gate:** `OQ-TEK-001`. *(#11 §4)*
- **TEK-EVD-003** `gated` — Source-authored claims are distinguished from
  researcher/agent-derived claims; observation, evidence, inference,
  conclusion and recommendation are distinct kinds. **Gate:** `OQ-TEK-001`. *(#11 §5)*
- **TEK-EVD-004** `target` — Supersession never erases history. *(#11 §5,
  §9)*

### 14.6 Contradiction and uncertainty — `UNC`

- **TEK-UNC-001** `gated` — Candidate contradictions may be proposed, but a
  contradiction becomes a relationship only when declared; multiple unresolved
  interpretations coexist; the disagreement dimension (fact, definition,
  method, population, period, assumption, interpretation) is recorded when
  declared. **Gate:** `OQ-TEK-001`. *(#11 §6)*
- **TEK-UNC-002** `now` — Confidence is either declared in source or asserted
  with provenance; it is never cosmetic, and ordinal confidence is not given
  false numeric precision. *(#11 §7)*
- **TEK-UNC-003** `target` — Conclusions expose what evidence could change
  them. *(#11 §7)*

### 14.7 Synthesis — `SYN`

- **TEK-SYN-001** `gated` — Synthesis is generated from traceable declared
  claims/evidence; every material statement traces to its support and
  surfaces contradictory and missing evidence. **Gate:** `OQ-TEK-001`. *(#11 §8)*
- **TEK-SYN-002** `target` — One canonical research state supports multiple
  audience projections; synthesis is regenerable deterministically from a
  frozen state plus declared generation inputs where practical. *(#11 §8)*

### 14.8 Review and challenge — `REV`

- **TEK-REV-001** `target` — Explicit review states with reviewer identity;
  human approval distinguishable from agent generation. *(#11 §9)*
- **TEK-REV-002** `target` — A challenge workflow surfaces unsupported claims,
  dependency gaps, circular support, stale evidence and unresolved
  contradictions from declared structure. *(#11 §9)*
- **TEK-REV-003** `target` — Material corrections create durable history, not
  silent replacement. *(#11 §9)*

### 14.9 Corpus integrity — `INT`

- **TEK-INT-001** `slice` — Referential integrity validation over every
  declared reference (`TEK-REL-004`). *(#11 §10)*
- **TEK-INT-002** `target` — Evidence dependencies and cycles are represented
  explicitly and validated (cycle detection over **declared** dependency edges
  only). *(#11 §4, §10, #14)*
- **TEK-INT-003** `target` — Coverage is always scoped (numerator,
  denominator, population and rule are stated); no global coverage percentage
  is published without its scope. *(#11 §10)*
- **TEK-INT-004** `target` — Effective-current projections (what is current
  after supersession) are derived while historical states are retained.
  *(#11 §10)*
- **TEK-INT-005** `target` — Impact inspection ("if X is withdrawn, what
  depends on it?") over declared dependencies. *(draft §30.5)*

## 15. Agent interface and provenance — `AGT`

- **TEK-AGT-001** `slice` — Agents never need to scrape HTML; structured
  contracts cover artifacts, relationships, validation and source locations.
  *(draft §21)*
- **TEK-AGT-002** `now` — Agent work on this repository records agent
  identity, provider, runtime/model, session, work item, branch and
  timestamps through the repository's provenance mechanism; token usage and
  cost are recorded when available and explicitly marked unavailable
  otherwise. *(#11 §12, AGENTS.md, docs/development-telemetry.md)*
- **TEK-AGT-003** `now` — No agent may record itself as another provider or
  runtime. *(#11 §12)*
- **TEK-AGT-004** `target` — Any canonical-state transition performed through
  Tekmerion records the actor (human or agent identity), provider/runtime,
  time and reason. *(#11 §12, TEK-CAN-004)*

## 16. Security, privacy and trust — `SEC`

- **TEK-SEC-001** `now` — Research content is untrusted input: rendered
  content is sanitized, and research cannot introduce executable script into
  published output. *(draft §14, #11 §14)*
- **TEK-SEC-002** `now` — Credentials never appear in generated output.
  *(draft §14.4)*
- **TEK-SEC-003** `now` — External links are rendered without unnecessary
  privileges (`rel="noopener noreferrer"`). *(draft §14.5)*
- **TEK-SEC-004** `slice` — Publication fails closed for content explicitly
  marked non-public. *(#11 §14)*
- **TEK-SEC-005** `deferred` — Handling of secrets, private sources, PII,
  copyrighted snapshots and access-controlled publication. Reason: requires a
  threat model and an owner decision (`OQ-TEK-004`) before support is
  offered. Tekmerion MUST NOT claim support until then. *(#11 §14, §29 Q14)*
- **TEK-SEC-006** `slice` — Output paths are confined to the output root
  (path-escape is blocking). *(G.5)*

## 17. Reliability and recovery — `REC`

- **TEK-REC-001** `target` — Operations that mutate canonical state are atomic
  or recoverable. *(#11 §15)*
- **TEK-REC-002** `slice` — An interrupted build is safely restartable and
  never leaves a partially written publication in the published location.
  *(#11 §15, #19)*
- **TEK-REC-003** `now` — Backups and exports are ordinary repository
  artifacts. *(#11 §15)*

## 18. Performance — `PRF`

- **TEK-PRF-001** `slice` — Build duration, output size, index size and
  browser payload are measured and recorded as baselines; thresholds are set
  from measurements, not guessed. *(draft §15.4, #11 §16, #19)*
- **TEK-PRF-002** `slice` — The browser never loads the complete corpus by
  default. *(draft §15.1, #11 §16)*

## 19. Accessibility — `ACC`

- **TEK-ACC-001** `slice` — Target WCAG 2.2 AA for published views; no
  conformance claim until the applicable criteria are evaluated. *(draft §9.2,
  #11 §17, Engineering Standards)*
- **TEK-ACC-002** `slice` — Full keyboard operation of core navigation; state
  and relationship meaning never depend on colour alone; layouts work at a
  320 px baseline. *(draft §9, #11 §17, doc 18 §3.7)*
- **TEK-ACC-003** `slice` — Visual Engineering and Communication Engineering
  guidance are validation inputs for UI and consequential communication.
  *(#11 §17, AGENTS.md)*

## 20. Migration — `MIG`

- **TEK-MIG-001** `compat` — An existing installation is detected by
  `.echelon/research-publisher.json` and migrated to `.echelon/tekmerion.json`
  through explicit lifecycle logic. *(#11 §19, #15)*
- **TEK-MIG-002** `compat` — Migration is detectable, deterministic,
  idempotent, dry-runnable, conflict-aware and recoverable, and preserves
  user-owned and locally modified shared content. *(#15)*
- **TEK-MIG-003** `compat` — Tool-owned consumer state is never hand-edited as
  a substitute for migration. *(#15, AGENTS.md)*
- **TEK-MIG-004** `compat` — Migration, idempotency, dry-run, conflict and
  recovery behaviours have automated tests. *(#15)*

## 21. Echelon integration — `ECH`

- **TEK-ECH-001** `now` — `docs/vnext/18-shared-application-foundations.md`
  is normative: Aegis at operational boundaries, Forma for interactive UI,
  Folio when printable/PDF/paginated output exists. It is incorporated by
  reference, not restated. *(v0.2 R13)*
- **TEK-ECH-002** `now` — Echelon components are upgraded only through their
  supported lifecycle mechanisms; installation manifests are never falsified.
  *(#11 §20, #16)*
- **TEK-ECH-003** `now` — Tekmerion remains independently installable; a
  missing optional Echelon component degrades explicitly rather than failing
  unrelated functions. *(#11 §20)*
- **TEK-ECH-004** `now` — A shared capability gap is recorded against the
  owning system rather than implemented as a competing local framework.
  *(doc 18 §1, #16)*
- **TEK-ECH-005** `now` — Praxis naming is used once the repository
  operating-system CLI migration is available through its lifecycle; until
  then the supported `ros` executable is used. *(#11 §20)*

## 22. Testing — `TST`

- **TEK-TST-001** `now` — Tests are implementation evidence and are written
  with the change: domain invariants, legal/illegal transitions, parser
  compatibility, relationship resolution, contracts, migration, and
  end-to-end publication. *(draft §17, #11 §18)*
- **TEK-TST-002** `slice` — Fixtures derive from real research and preserve
  real edge cases; canonical research is never modified to make a test
  easier. *(v0.2 R8.1)*
- **TEK-TST-003** `slice` — A golden-corpus test asserts artifact count, edge
  count by relation, finding count by code, output size and URL set.
  *(v0.2 R8.2)*
- **TEK-TST-004** `slice` — Determinism, the Limen boundary and URL
  preservation are CI gates. *(v0.2 R8.3)*
- **TEK-TST-005** `slice` — Malformed, adversarial, unknown-content,
  duplicate, dangling, contradictory, cyclic, forward-version and interrupted
  cases are exercised. *(draft §17.3, #11 §18, #19)*
- **TEK-TST-006** `now` — Tests cite the requirement identifiers they
  evidence. *(this document)*

## 23. Explicit non-goals

Tekmerion does not: replace repository Markdown as canonical research; require
a database or JSON migration; rewrite research for display; let the frontend
create unsupported conclusions; turn relationships into an opaque graph
visualisation; introduce a server or a large JavaScript framework without
evidence; depend on GitHub APIs for ordinary reading; silently repair
malformed research; or hide validation problems. *(draft §27)*

## 24. First vertical slice

The slice requirements are every `slice` requirement above, exercised on the
real `visual-engineering` / `composition-science` corpus (23 documents, the
`source_rep: RP-COMP-005` cluster). Scope and exit criteria are in
`docs/vnext/13-vertical-slice.md` §M.2 and §M.4, which remain normative for the
slice with these amendments:

1. `TEK-EXP-003` (Forma) and `TEK-ACC-*` apply to the slice's interactive UI.
2. `TEK-REL-003` ("why am I seeing this?") is an exit criterion.
3. `TEK-PUB-003` (last-known-good) and `TEK-REC-002` (interrupted build) are
   exit criteria (#19).
4. Where a slice exit criterion depends on an unavailable Echelon component,
   the gap is recorded (`TEK-ECH-004`) and the criterion stays open; it is not
   marked satisfied.

## 25. Change control

A change to this document requires: a version bump; an entry in the change log
below; updated traceability; and a decision record when the change alters a
boundary, contract, or classification from `gated`/`deferred` to anything
else.

### Change log

| Version | Date | Change | Work item |
| --- | --- | --- | --- |
| 1.0.0 | 2026-09-29 | Canonical baseline reconciling REQ-RP-VNEXT 0.2.0, the original draft, #11 and doc 18 | GH-13 |
