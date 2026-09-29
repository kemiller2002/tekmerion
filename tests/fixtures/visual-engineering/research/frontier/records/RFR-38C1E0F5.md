---
id: RFR-38C1E0F5
title: "Create a shared benchmark and decision threshold: Composition Science Project Constitution"
document_type: research_frontier_record
status: Open
category: Tooling
frontier_score: 392
generated: 2026-07-29
immutable: true
---

# RFR-38C1E0F5 — Create a shared benchmark and decision threshold: Composition Science Project Constitution

## Research opportunity

Create a shared benchmark and decision threshold for the claims or recommendations in “Composition Science Project Constitution.”

## Background

The originating artifact is accepted by the repository publishing inventory with status “approved.” Its Knowledge Model section provides the immediate evidence boundary.

## Evidence trace

- Origin document: [content/projects/composition-science/canonical/composition-science-project-constitution-v1.md](../../../content/projects/composition-science/canonical/composition-science-project-constitution-v1.md)
- Section: `Knowledge Model`
- Specific assumption challenged: The source's treatment in “Knowledge Model” is sufficiently supported for its intended scope.
- Supporting evidence excerpt: “Genome Nodes → Evidence → Observations → Candidate Laws → Experiments → Metrics → Design Rules → Components → Implementations”
- Reason this opportunity exists: How competing methods or implementations compare on a common corpus with explicit utility, safety, and cost thresholds.

## Unknowns

- How competing methods or implementations compare on a common corpus with explicit utility, safety, and cost thresholds.

## Dependencies

- [RFR-2FD4F1EF](./RFR-2FD4F1EF.md)

## Suggested REP and methodology

- Suggested REP: `REP-GOV-001-BENCHMARK`
- Methodology: Curate representative cases, blind ground truth where possible, define baselines and uncertainty-aware metrics, and run reproducible benchmark evaluations.
- Expected outputs: Versioned benchmark, baseline implementations, scoring harness, datasheet, and adoption decision rule.
- Success criteria: Independent teams can reproduce scores and the benchmark discriminates meaningful quality differences without rewarding proxy gaming.
- Recommended agent: `research-engineering-agent`
- Estimated effort: Medium
- Expected knowledge gained: How competing methods or implementations compare on a common corpus with explicit utility, safety, and cost thresholds.

## Evaluation

| Dimension | Score (1–5) |
|---|---:|
| Knowledge gain | 4 |
| Potential impact | 4 |
| Cross-project reuse | 5 |
| Scientific importance | 5 |
| Dependency cost | 5 |
| Implementation difficulty | 3 |
| **Frontier score** | **392** |

Confidence in this opportunity: **moderate**. Status: **Open**.
