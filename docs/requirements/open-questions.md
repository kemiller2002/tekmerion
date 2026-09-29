---
id: REQ-TEK-OQ
title: Tekmerion Open Questions and Resolutions
version: 1.0.0
status: accepted-baseline
created: 2026-09-29
updated: 2026-09-29
work_item: GH-13
related_documents:
  - docs/requirements/tekmerion-requirements.md
  - docs/vnext/15-requirements-v0.2.0.md
  - docs/vnext/17-requirements-section-29-answers.md
---

# Open Questions and Resolutions

Every question inherited from earlier requirement work is either **resolved**
here (with the evidence and the reversibility of the answer) or **retained**
with an owner, what it blocks, and the rule Tekmerion follows until it is
answered. Nothing is silently dropped.

Disposition values: `resolved` (answered from evidence), `resolved-provisional`
(reasonable reversible decision taken by the implementing agent; owner may
overturn), `retained` (needs an owner decision).

## Retained — need an owner decision

### OQ-TEK-001 — Will research gain a sub-document object authoring contract?

- **Origin:** `17-requirements-section-29-answers.md` ("one question the draft
  does not ask"); v0.2 Q1; `05` E.3.
- **Question:** Will the research format (Praxis/ROS) define declared identity
  and provenance for claims, findings, evidence items and their typed
  relations (`supports`, `contradicts`, `evidence-for`, …)?
- **Owner:** Praxis/ROS format owner (research authoring), with Tekmerion as a
  consumer.
- **Blocks:** every `gated` requirement: `TEK-REL-006`, `TEK-EXP-009`,
  `TEK-EVD-001`–`003`, `TEK-UNC-001`, `TEK-SYN-001`.
- **Rule until answered:** Tekmerion may define the types and a *proposed*
  authoring contract, and may validate instances declared through a contract,
  but produces zero instances from prose. `evidenceIds`, `hypothesisIds` and
  `theoryIds` (present in the legacy engine and seed corpus, zero times in real
  research) are parsed as canonical typed document links when present
  (`TEK-REL-001`), which is the only part of v0.2 Q1 answerable without the
  owner.
- **Recommended next action:** record a Praxis requirement proposing a minimal
  declared-object block (id, kind, source span, relations by id). Recorded as
  local backlog item; not filed upstream without the owner.

### OQ-TEK-002 — Are frontier record ids stable across source edits?

- **Origin:** v0.2 Q2, `10-url-and-identity.md` J.5, risk R10.
- **Owner:** frontier generator owner (visual-engineering research tooling).
- **Blocks:** advertising `/a/RFR-…` URLs as permanent.
- **Rule until answered:** frontier records are addressed by declared id like
  any artifact, but the UI and manifest label frontier ids as
  `stability: unconfirmed`. The slice does not promise their permanence.

### OQ-TEK-003 — Cross-repository aggregation

- **Origin:** draft §29 Q6.
- **Owner:** product owner.
- **Blocks:** nothing in the slice.
- **Rule until answered:** single-repository publication; keys in machine
  contracts carry repository identity (`TEK-IDY-008`) so aggregation is not
  designed out.

### OQ-TEK-004 — Access control and non-public research

- **Origin:** draft §29 Q14, #11 §14.
- **Owner:** product owner (security/privacy-sensitive; escalation required by
  the constitution).
- **Blocks:** `TEK-SEC-005`.
- **Rule until answered:** all published research is assumed public; content
  explicitly marked non-public fails closed (`TEK-SEC-004`).

### OQ-TEK-005 — Deployment targets beyond GitHub Pages

- **Origin:** draft §29 Q15.
- **Owner:** product owner. **Blocks:** nothing. **Rule:** output stays
  portable static files; GitHub Pages is the supported target.

### OQ-TEK-006 — Canonical meaning of generated diagrams

- **Origin:** draft §29 Q21. No diagrams were found in the sampled corpus.
- **Owner:** research format owner. **Blocks:** nothing. **Rule:** diagrams are
  presentation-only until declared otherwise.

### OQ-TEK-014 — When is the npm package renamed to `@echelon-foundry/tekmerion`?

- **Origin:** GH-15; `TEK-IDN-003`.
- **Owner:** product owner (release/publishing authority).
- **Why it needs the owner:** `publish.yml` publishes on every push to `main`
  through npm trusted publishing. A new package name needs its trusted
  publisher configured on npmjs.com by the account owner before the first
  publish, and consumers must be told about the new name. Both are
  outward-facing and not reversible by a commit.
- **Blocks:** the package/executable rename part of `TEK-IDN-003` and the
  deprecation notice of `TEK-IDN-004`.
- **Rule until answered:** the lifecycle identity, manifest path and product
  documentation are Tekmerion (migration 2→3); the package keeps publishing as
  `@echelon-foundry/research-publisher`; `tekmerion` is shipped as an
  additional executable alias; no deprecation notice is emitted yet.
- **Recommended next action:** configure the trusted publisher for
  `@echelon-foundry/tekmerion`, then change `package.json#name`,
  `Identity.PackageName`/`ExecutableName`, the `research:*` → `tekmerion:*`
  script migration (configuration version 4) and the deprecation notice in
  one release.

## Resolved

### OQ-TEK-007 — Consume `frontier-graph.json` or re-derive from Markdown? — `resolved-provisional`

- **Origin:** v0.2 Q3.
- **Answer:** re-derive from the canonical Markdown frontier records.
  `frontier-graph.json` is a generated artifact and, under `TEK-CAN-001`,
  never a source of truth; it MAY be read as a cross-check whose disagreement
  with the Markdown becomes a warning finding.
- **Reversibility:** low cost. **Decided by:** GH-13 (Claude), 2026-09-29.

### OQ-TEK-008 — Is there another requirements draft? — `resolved`

- **Origin:** v0.2 Q4.
- **Answer:** issue #11 (2026-09-27) is the owner's current requirements input;
  it and the original draft are the complete set. `REQ-TEK` 1.0.0 reconciles
  both. PR #12's `REQ-RP-PROV` is a further, unmerged proposal tracked by
  `TEK-PRV-004`.

### OQ-TEK-009 — Publication timestamp vs determinism — `resolved`

- **Origin:** conflict between draft §10.5 (manifest has publication timestamp)
  and v0.2 R4.6 (byte-identical builds).
- **Answer:** content artifacts carry no wall-clock value. The publication
  timestamp lives only in a separate publication record that is excluded from
  the determinism comparison and is not content-addressed (`TEK-ARC-004`,
  `TEK-PUB-002`).

### OQ-TEK-010 — Can Tekmerion mutate research? — `resolved`

- **Origin:** conflict between #11 §12 (agents propose transitions; Tekmerion
  supports the full lifecycle) and draft §30.10 / v0.2 R1.4 (publisher never
  writes research).
- **Answer:** the build/validate/publish pipeline never writes research
  (`TEK-CAN-003`). Research-state mutation is a separate, explicitly invoked,
  legal, provenance-recording transition through a declared authoring
  contract (`TEK-CAN-004`).

### OQ-TEK-011 — Validation severities — `resolved`

- **Origin:** draft §5.6 (information/warning/error/fatal) vs G.5 (blocking /
  warning / informational).
- **Answer:** three levels; `error` and `fatal` both map to `blocking`
  (`TEK-VAL-004`). Only the enumerated conditions block (`TEK-VAL-002`).

### OQ-TEK-012 — Is PDF/print export in scope? — `resolved`

- **Origin:** draft §29 Q22, §9.7; doc 18 §4.
- **Answer:** no printable/PDF/paginated artifact is emitted, so Folio is not
  yet applicable (doc 18 §4.10). Print styles for HTML views are CSS only. The
  first requirement that introduces paginated output activates Folio.

### OQ-TEK-013 — Product naming contract — `resolved-provisional`

- **Origin:** #11 §19, #15.
- **Answer:** `TEK-IDN-003`; rationale in `DF-TEK-2026-0001`. Reversible until
  the first published release under the new names.

### §29 questions answered by discovery

Questions 1–5, 7–13, 16–20, 23–25 of draft §29 are answered from evidence in
`docs/vnext/17-requirements-section-29-answers.md` and are carried into
requirements as mapped in `traceability.md`. Questions 6, 14, 15, 21 are
`OQ-TEK-003`–`006` above.
