---
id: REQ-TEK-TRACE
title: Tekmerion Requirements Traceability
version: 1.0.0
status: accepted-baseline
created: 2026-09-29
updated: 2026-09-29
work_item: GH-13
related_documents:
  - docs/requirements/tekmerion-requirements.md
---

# Requirements Traceability

Every earlier requirement maps to one or more `TEK-*` identifiers, or is
explicitly marked with its disposition. This table is checked by
`tests/integration/requirements-baseline.test.mjs`: every `TEK-*` identifier
cited here must exist in `tekmerion-requirements.md`, and every source row
listed in the checked sections must be present.

## A. REQ-RP-VNEXT 0.2.0 (`docs/vnext/15-requirements-v0.2.0.md`)

| Source | Disposition | Tekmerion requirement(s) |
| --- | --- | --- |
| R1.1 | carried | TEK-CAN-002 |
| R1.2 | carried | TEK-ING-002 |
| R1.3 | carried | TEK-ING-003 |
| R1.4 | carried | TEK-CAN-003 |
| R2.1 | carried | TEK-FID-001, TEK-FID-002 |
| R2.2 | carried (full-corpus target) | TEK-REL-005 |
| R2.3 | carried | TEK-REL-004 |
| R2.4 | carried, generalised to every value origin | TEK-REL-002, TEK-CAN-005 |
| R3.1 | carried | TEK-IDY-001 |
| R3.2 | carried | TEK-IDY-003 |
| R3.3 | carried | TEK-IDY-002, TEK-IDY-004 |
| R3.4 | carried | TEK-IDY-006 |
| R3.5 | carried | TEK-IDY-006 |
| R3.6 | carried | TEK-IDY-007 |
| R4.1 | carried | TEK-ARC-001 |
| R4.2 | carried | TEK-EXP-002 |
| R4.3 | carried | TEK-EXP-001 |
| R4.4 | carried | TEK-ARC-002 |
| R4.5 | carried | TEK-ARC-003 |
| R4.6 | carried; timestamp conflict resolved (OQ-TEK-009) | TEK-ARC-004 |
| R5.1 | carried | TEK-STA-001 |
| R5.2 | carried; non-public content added | TEK-VAL-002, TEK-IDY-005 |
| R5.3 | carried | TEK-VAL-003 |
| R5.4 | carried | TEK-STA-003 |
| R6.1 | carried | TEK-CON-001 |
| R6.2 | carried | TEK-CON-002 |
| R6.3 | carried with the #17 threshold | TEK-CON-003 |
| R6.4 | carried | TEK-PRV-001 |
| R6.5 | carried | TEK-CON-005 |
| R7.1 | carried | TEK-EXP-004 |
| R7.2 | carried; gated views named | TEK-EXP-008, TEK-EXP-009, TEK-EXP-010 |
| R7.3 | carried | TEK-EXP-006 |
| R7.4 | carried | TEK-PRV-002 |
| R7.5 | carried | TEK-EXP-006, TEK-EXP-007 |
| R8.1 | carried | TEK-TST-002 |
| R8.2 | carried | TEK-TST-003 |
| R8.3 | carried | TEK-TST-004 |
| R13 | carried by reference | TEK-ECH-001 |
| Q1 | partly resolved, remainder retained | OQ-TEK-001, TEK-REL-001 |
| Q2 | retained | OQ-TEK-002 |
| Q3 | resolved-provisional | OQ-TEK-007 |
| Q4 | resolved | OQ-TEK-008 |

## B. Original draft (`input-documents/research-publisher-vnext-requirements.txt`)

| Source | Disposition | Tekmerion requirement(s) |
| --- | --- | --- |
| §0 purpose / canonical source / layering | carried; product identity widened by #11 | TEK-CAN-001, TEK-ARC-005 |
| §1.1 ROS | carried | TEK-CAN-002, TEK-ING-007 |
| §1.2 SDE/Ordo | carried | TEK-STA-001, TEK-STA-002, TEK-STA-003, TEK-VAL-005 |
| §1.3 Limen | carried | TEK-EXP-002 |
| §1.4 F# | carried | TEK-ARC-001 |
| §1.5 dependency discipline | carried | TEK-ARC-003 |
| §2.1 repository input | refined (config-driven) | TEK-ING-001 |
| §2.2 existing compatibility | carried | TEK-CAN-002 |
| §2.3 parsing | refined: documents, not unmarked sub-document objects | TEK-ING-002, TEK-ING-003, TEK-EVD-001 |
| §2.4 unknown sections | carried | TEK-ING-003 |
| §2.5 parse errors | carried | TEK-ING-004 |
| §2.6 partial failure | carried | TEK-ING-005 |
| §3.1 runtime model | carried | TEK-ARC-001 |
| §3.2 stable identity | refined for absent ids | TEK-IDY-001, TEK-IDY-002 |
| §3.3 relationship vocabulary | gated (vocabulary kept as target) | TEK-REL-001, TEK-REL-006, TEK-FID-003 |
| §3.4 provenance | carried | TEK-PRV-001, TEK-REL-003 |
| §3.5 derived state | carried | TEK-CAN-005, TEK-REL-002 |
| §4 SDE domain states | carried | TEK-STA-001 |
| §4.3 navigation state | carried | TEK-EXP-007 |
| §4.4 invalid state prevention | carried | TEK-STA-001, TEK-STA-002 |
| §5 validation | carried; severities reconciled | TEK-VAL-001, TEK-VAL-004, TEK-IDY-005, TEK-INT-001 |
| §6.1–6.2 projections, artifact view | carried | TEK-EXP-005 |
| §6.3 finding view | gated | TEK-EXP-009 |
| §6.4 evidence view | gated | TEK-EXP-009 |
| §6.5 source view | target | TEK-SRC-001 |
| §6.6 experiment view | carried as document view; sub-links gated | TEK-EXP-005, TEK-EXP-009 |
| §6.7 theory view | carried as document view; support/contradiction gated | TEK-EXP-005, TEK-EXP-009 |
| §6.8 question view | carried (frontier records) | TEK-QST-001 |
| §6.9 timeline | deferred | TEK-EXP-010 |
| §6.10 relationship view | carried | TEK-EXP-006 |
| §6.11 health view | carried | TEK-EXP-004, TEK-INT-003 |
| §7 navigation | carried | TEK-EXP-006, TEK-EXP-007, TEK-IDY-003, TEK-PRV-002 |
| §8 search | target | TEK-EXP-011 |
| §9 UI, accessibility, print, copying | carried | TEK-ACC-001, TEK-ACC-002, TEK-EXP-012, OQ-TEK-012 |
| §10 publishing model | carried | TEK-PUB-001, TEK-PUB-002, TEK-PUB-003 |
| §11 incremental publishing | carried | TEK-PUB-005 |
| §12 machine-readable outputs | carried | TEK-CON-001, TEK-CON-002 |
| §13 extension model | carried | TEK-ING-003, TEK-ING-007 |
| §14 security | carried | TEK-SEC-001, TEK-SEC-002, TEK-SEC-003 |
| §15 performance | carried | TEK-PRF-001, TEK-PRF-002 |
| §16 observability | carried | TEK-VAL-001, TEK-PRF-001 |
| §17 testing | carried | TEK-TST-001, TEK-TST-005 |
| §18 backward compatibility | carried | TEK-CAN-002, TEK-IDY-006, TEK-ARC-006 |
| §19 versioning | carried | TEK-CON-001, TEK-ING-007 |
| §20 integrity principles | carried | TEK-FID-001, TEK-FID-005, TEK-FID-006 |
| §21 agent-friendly design | carried | TEK-AGT-001, TEK-CON-003 |
| §22 authoring feedback | carried | TEK-VAL-001, TEK-VAL-006 |
| §23 developer experience | carried; naming in TEK-IDN-003 | TEK-IDN-003 |
| §24 configuration | carried | TEK-ING-001 |
| §25 themes | carried via Forma | TEK-EXP-003 |
| §26 long-term preservation | carried | TEK-EXP-001 |
| §27 non-goals | carried | §23 of REQ-TEK |
| §28 delivery sequence | superseded by issue decomposition #13–#19 | — |
| §29 questions | answered or retained | OQ-TEK-001 … OQ-TEK-013 |
| §30.1 cross-repository | target | TEK-IDY-008 |
| §30.2 attachments | target | TEK-SRC-001 |
| §30.3 citation links | target | TEK-EXP-012 |
| §30.4 diff/evolution | target | TEK-PRV-003 |
| §30.5 evidence dependency | target over declared edges | TEK-INT-005 |
| §30.6 traceability chain | gated | TEK-EVD-001 |
| §30.7 bidirectional navigation | carried | TEK-REL-002 |
| §30.8 graceful degradation | carried | TEK-EXP-001 |
| §30.9 deterministic derived data | carried | TEK-ARC-004 |
| §30.10 no hidden mutation | carried and reconciled | TEK-CAN-003, TEK-CAN-004 |
| §31 definition of success | carried; sub-document navigation gated | §24 of REQ-TEK, TEK-EXP-009 |

## C. Issue #11 requirement sections

| Source | Disposition | Tekmerion requirement(s) |
| --- | --- | --- |
| #11 §1 workspace and lifecycle | target; negative knowledge | TEK-WSP-001, TEK-WSP-002, TEK-WSP-003, TEK-NEG-001 |
| #11 §2 questions and planning | slice (frontier) + target | TEK-QST-001, TEK-QST-002, TEK-QST-003 |
| #11 §3 sources and acquisition | target | TEK-SRC-001, TEK-SRC-002, TEK-SRC-003, TEK-SRC-004 |
| #11 §4 evidence model | gated | TEK-EVD-001, TEK-EVD-002, TEK-INT-002, TEK-FID-003 |
| #11 §5 claims/findings | gated | TEK-EVD-001, TEK-EVD-003, TEK-EVD-004 |
| #11 §6 contradiction | gated / now | TEK-UNC-001, TEK-FID-005 |
| #11 §7 uncertainty | now / target | TEK-UNC-002, TEK-UNC-003, TEK-FID-002 |
| #11 §8 synthesis | gated / target | TEK-SYN-001, TEK-SYN-002 |
| #11 §9 review and challenge | target | TEK-REV-001, TEK-REV-002, TEK-REV-003, TEK-STA-005 |
| #11 §10 corpus integrity | slice / target | TEK-INT-001, TEK-INT-002, TEK-INT-003, TEK-INT-004 |
| #11 §11 search and exploration | target / slice | TEK-EXP-011, TEK-REL-003 |
| #11 §12 agent interface | now / target | TEK-AGT-002, TEK-AGT-003, TEK-AGT-004, TEK-CON-006 |
| #11 §13 publication | slice | TEK-PUB-001, TEK-PUB-002, TEK-PUB-003, TEK-IDY-006 |
| #11 §14 security and privacy | now / slice / deferred | TEK-SEC-001, TEK-SEC-004, TEK-SEC-005 |
| #11 §15 reliability and recovery | target / slice | TEK-REC-001, TEK-REC-002, TEK-REC-003, TEK-PUB-004 |
| #11 §16 performance | slice | TEK-PRF-001, TEK-PRF-002, TEK-PUB-005 |
| #11 §17 accessibility | slice | TEK-ACC-001, TEK-ACC-002, TEK-ACC-003 |
| #11 §18 testing | now / slice | TEK-TST-001, TEK-TST-005 |
| #11 §19 migration | compat | TEK-IDN-003, TEK-IDN-004, TEK-MIG-001, TEK-MIG-002 |
| #11 §20 Echelon integration | now | TEK-ECH-002, TEK-ECH-003, TEK-ECH-005 |

## D. Shared application foundations (`docs/vnext/18-shared-application-foundations.md`)

| Source | Disposition | Tekmerion requirement(s) |
| --- | --- | --- |
| doc18 §1 normative scope | incorporated by reference | TEK-ECH-001, TEK-ECH-004 |
| doc18 §2 Aegis | incorporated by reference | TEK-ECH-001 |
| doc18 §3 Forma | incorporated by reference | TEK-ECH-001, TEK-EXP-003 |
| doc18 §4 Folio | incorporated; not yet applicable | TEK-ECH-001, OQ-TEK-012 |
| doc18 §6 definition of done | incorporated by reference | TEK-ECH-001 |

## E. Proposed, not yet merged

| Source | Disposition | Tekmerion requirement(s) |
| --- | --- | --- |
| PR #12 REQ-RP-PROV R14.1–R14.14 | principle carried; detail normative on merge | TEK-PRV-004 |
