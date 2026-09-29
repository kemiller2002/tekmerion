---
id: PROJECT-CHARTER-tekmerion
title: Tekmerion Project Charter
status: draft
version: 0.2.0
created: 2026-09-17
updated: 2026-09-29
supersedes: []
amendments:
  - date: 2026-09-29
    author: Claude (anthropic/claude-code), work item GH-15
    reason: product renamed from Research Publisher to Tekmerion (GH-11/GH-15); purpose and first outcome taken from issue #11 and REQ-TEK
    fields: [id, title, purpose, users, first bounded outcome, success criteria]
---

# Tekmerion project charter

Tekmerion was formerly named Research Publisher (charter 0.1.0).

## Purpose

Turn durable repository research into an evidence-faithful research system:
read canonical research state, validate it, expose its structure and
provenance, and project it into human- and machine-readable publications —
without fabricating identities, relationships, dates or conclusions the
research does not declare. (Issue #11; `docs/requirements/tekmerion-requirements.md`.)

## Intended users

Researchers and agents who author and consume repository research
(ROS/Praxis Markdown), and readers of the published research. Specific user
roles and their priorities have not been validated with users; this is an open
question, not a finding.

## First bounded outcome

The first vertical slice on the real `visual-engineering` / `composition-science`
corpus: honest typed ingestion, versioned machine contracts, a script-free
research site with provenance navigation, and deterministic publication
(issues #17–#19; `docs/vnext/13-vertical-slice.md`).

## Included

- Canonical requirements with stable identifiers (REQ-TEK).
- A typed F# domain with legal state transitions.
- The first vertical slice and its operational proof.
- Migration from the Research Publisher identity.

## Excluded (until a decision reopens them)

- Inferring claims, findings, evidence or their relations from prose
  (gated on OQ-TEK-001).
- Timeline views over untrustworthy dates.
- Access-controlled or private publication (OQ-TEK-004).
- A database, server or large frontend framework without measured need.

## Success criteria

- Slice exit criteria in `docs/vnext/13-vertical-slice.md` §M.4 are met with
  recorded evidence, or explicitly left open with the reason.
- A successor can install, ingest, validate, publish, reproduce and upgrade
  from repository records alone.
- No published value is fabricated.

## Owners and decision authority

Product owner: repository owner (kemiller2002). Open owner decisions are listed
in `docs/requirements/open-questions.md`.
