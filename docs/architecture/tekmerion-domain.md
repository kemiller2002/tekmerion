---
id: ARCH-TEK-DOMAIN
title: Tekmerion Domain Model and Legal Transitions
version: 1.0.0
status: accepted
created: 2026-09-29
updated: 2026-09-29
work_item: GH-14
related_documents:
  - docs/requirements/tekmerion-requirements.md
  - docs/vnext/06-fsharp-domain-model.md
  - docs/vnext/07-sde-state-model.md
  - .sde/architecture/FOUR-TIER-ARCHITECTURE.md
---

# Tekmerion Domain Model and Legal Transitions

`src/Tekmerion.Domain` is SDE tiers 1–2: what can be true, and what legal change
may happen. It references `FSharp.Core` and the base class library only; it
performs no I/O, reads no clock and knows no serialization format. Hosts
(parsing, filesystem, HTML/JSON projection, deployment) are later tiers that
ask it for capabilities, perform the requested effects and report what
happened.

It refines the discovery proposal in `docs/vnext/06` and `07` against
`REQ-TEK`. It is deliberately small: every type exists because a requirement
names it.

## Module map

| File | Owns | Requirements |
| --- | --- | --- |
| `Knowledge.fs` | `Absence`, `Knowable<'T>`, `NonEmpty<'T>` | TEK-FID-001, TEK-FID-002 |
| `Identity.fs` | `RepositoryId`, `RepoPath`, `ArtifactId`, `PathKey`, `ArtifactKey` | TEK-IDY-001…004, 008, TEK-SEC-006 |
| `Provenance.fs` | `SourceLocation`, `AgentIdentity`, `Actor`, `Assertion`, `Origin`, `Sourced<'T>` | TEK-PRV-001, 003, TEK-CAN-005, TEK-AGT-003/004 |
| `Artifacts.fs` | `ArtifactType`, `TypeSource`, `StatusReading`, `ReferenceValue`, `Resolution`, `Extension`, `Section`, `Artifact` | TEK-ING-002/003/006, TEK-FID-004 |
| `Relationships.fs` | `CanonicalEdge`, `DerivedEdge`, `Derivation`, `Cycles`, `Supersession` | TEK-REL-001…004, TEK-INT-002, 004 |
| `Validation.fs` | `FindingCode`, `Policy.severity`, `Obligation`, `Assessment`, `PublishableEvidence` | TEK-VAL-001…005 |
| `Research.fs` | `ResearchQuestion`, `NegativeKnowledge`, `Source`, `ResearchWorkspace`, `ScopedCoverage`, gated `DeclaredObject`/`DeclaredRelation` | §14 of REQ-TEK |
| `Review.fs` | review lifecycle, `HumanApproval` | TEK-REV-001/002, TEK-STA-005 |
| `Effects.fs` | `EffectOutcome`, `Reconciliation` | TEK-STA-004 |
| `Publication.fs` | publication lifecycle, capabilities, requested effects | TEK-STA-001…004, TEK-PUB-003, TEK-REC-002 |

## Origin of values

Every exposed value is **authored** (declared at a `SourceLocation`),
**derived** (named rule + non-empty basis of source locations) or **asserted**
(actor, time, reason, location). Absence is one of **not declared**,
**unknown**, **unavailable (reason)** or **not applicable (reason)**, and is
never zero. `Known 0` is the only zero.

## Gated research objects

`DeclaredObject` (claim, finding, evidence, observation, inference,
conclusion, recommendation) and `DeclaredRelation` (supports, contradicts,
evidence-for/against, contextual, insufficient, depends-on) exist as types.
Their only constructors require an `AuthoringContract` present in
`AuthoringContracts.approved`, which is empty until `OQ-TEK-001` is answered by
a decision record. There is no constructor that accepts prose. Consequently no
instance can exist today, which is the honest state of the corpus.

## Publication lifecycle

```
Unloaded ─SourcesDiscovered 0─▶ NothingToPublish
   │ SourcesDiscovered n
   ▼
Discovered ─CorpusParsed─▶ Parsed ─ReferencesLinked─▶ Linked ─ObligationsEvaluated─▶ Assessed
                                                                          │
                            publishable / publishable-with-warnings ◀─────┤──▶ unpublishable (Restart only)
                                        │ OutputProjected
                                        ▼
                                    Projected ─OutputStaged─▶ Staged ─StagingPromoted─▶ Promoted
                                                                                          │ DeploymentAttempted
                                                                                          ▼
                              DeploymentFailed ◀─Failed─ Deploying ─Succeeded─▶ Published
                                                            │ Unknown
                                                            ▼
                                                 DeploymentUnknown ─DeploymentObserved─▶ (reconciled)
Any state before promotion ─StageFailed─▶ Stopped (live output untouched)
```

- `Projected` and `Staged` carry `PublishableEvidence`, which can only be
  obtained from an assessment with no blocking obligation, so projecting an
  unpublishable corpus is unrepresentable, not merely refused.
- Output is written to staging and promoted by the host as an atomic replace.
  Until `Promoted`, the last known-good publication is untouched.
- An unknown promotion outcome stops the run; an unknown deployment outcome
  permits only reconciliation by observation.

## Invariants and their tests

| # | Invariant | Enforced by | Test(s) |
| --- | --- | --- | --- |
| I1 | Known zero ≠ absence; absence kinds are distinct | `Knowable`, `Absence` | `KnowledgeTests` TEK-FID-002 ×2 |
| I2 | Absent optional values become `NotDeclared`, never a default | `Knowable.ofOption` | `KnowledgeTests` TEK-FID-001 |
| I3 | Declared ids are kept verbatim; unusable ids are rejected, not repaired | `ArtifactId.create` | `IdentityTests` TEK-IDY-001 ×2 |
| I4 | A missing id yields a path key, never a declared id; path keys depend only on path | `ArtifactKey.assign`, `PathKey.ofPath` | `IdentityTests` TEK-IDY-002/003 |
| I5 | Paths cannot escape the repository root | `RepoPath.create` | `IdentityTests` TEK-SEC-006 |
| I6 | Type casings are one type; unknown types kept verbatim | `ArtifactType.classify` | `ArtifactTests` TEK-ING-002 |
| I7 | Status text survives beside its derived reading; unfamiliar status is `Unclassified` | `StatusReading.read` | `ArtifactTests` TEK-FID-004 |
| I8 | Every derived edge has a non-empty canonical basis | `DerivedEdge.Basis : NonEmpty` | `RelationshipTests` TEK-REL-002/003 |
| I9 | Dangling/prose references derive nothing | `Derivation.backlinks` | `RelationshipTests` TEK-FID-001 |
| I10 | Derivation is deterministic | sorted output | `RelationshipTests` TEK-ARC-004 |
| I11 | Cycles over declared edges are found and reported deterministically | `Cycles.find` | `RelationshipTests` TEK-INT-002 ×2 |
| I12 | Effective-current keeps history; supersession cycles are reported | `Supersession.effectiveCurrent` | `RelationshipTests` TEK-INT-004 |
| I13 | Exactly six conditions block; legitimate incompleteness never does | `Policy.severity` | `ValidationTests` TEK-VAL-002/003 |
| I14 | No publication evidence while a blocking obligation exists | `Assessment` (private cases) | `ValidationTests` TEK-STA-001 ×2, `PublicationTests` |
| I15 | No sub-document research instance without an approved contract | `AuthoringContracts.approved = []` | `ResearchTests` TEK-EVD-001 |
| I16 | Coverage always carries population, rule and valid counts | `ScopedCoverage` (private) | `ResearchTests` TEK-INT-003 ×3 |
| I17 | Unknown effect outcomes resolve only by observation; known outcomes are final | `Reconciliation.reconcile` | `ResearchTests` TEK-STA-004 |
| I18 | Human approval cannot be an agent; approval illegal while challenged | `HumanApproval`, `Review.decide` | `ReviewTests` TEK-STA-005, TEK-REV-002 |
| I19 | Offered capabilities are exactly the accepted commands | `Review.capabilities` | `ReviewTests` TEK-STA-002 (exhaustive) |
| I20 | No write is requested before projection; failures before promotion leave live output untouched | `Publication.effect`, `liveOutputChanged` | `PublicationTests` TEK-STA-003, TEK-PUB-003 |
| I21 | Staged/promoted tree must match the projected digest | `Publication.apply` | `PublicationTests` digest mismatch |
| I22 | Unknown deployment permits only reconciliation | `Publication.capabilities` | `PublicationTests` TEK-STA-004 |
| I23 | Domain has no third-party or host dependency | project references | `ArchitectureTests` TEK-ARC-003/005 |

## Not modelled, on purpose

- No generic knowledge graph or ontology (ADR 0002).
- No timeline or chronology type: dates stay verbatim `Knowable<string>` until
  a trustworthy chronology exists (TEK-EXP-010).
- No search, UI or navigation state: those are projection/browser concerns
  (#18).
- Frontier-record and parse-profile specifics are added in #17, where the real
  corpus is read.
