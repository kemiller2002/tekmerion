---
authors:
- Kevin Miller
- ChatGPT
date: 2026-07-18
document_type: specification
id: SPEC-001
project: composition-science
purpose: |
  Defines how knowledge is represented and linked across the Composition
  Science ecosystem.
status: working
title: Composition Science Knowledge Graph Specification
version: 1.0
purposes:
  - orient
  - integrate
  - reference
audiences:
  - practitioner
  - researcher
  - contributor
---

# Composition Science Knowledge Graph Specification

## Vision

The project is not a collection of documents. It is a connected
knowledge graph where every concept, observation, experiment, law, and
implementation can be traced to evidence.

## Core Node Types

  Prefix   Type             Purpose
  -------- ---------------- ------------------------------
  GN       Genome Node      Canonical concepts
  LAW      Candidate Law    Testable hypotheses
  EVD      Evidence         Research papers and findings
  OBS      Observation      Raw observations
  EXP      Experiment       Validation studies
  MET      Metric           Quantitative measures
  CASE     Case Study       Real-world examples
  RULE     Design Rule      Practical guidance
  COMP     Component        UI/design components
  IMPL     Implementation   HTML, CSS, tokens
  GOV      Governance       Project governance

## Relationship Types

Every relationship should be directional.

-   supports
-   contradicts
-   derives_from
-   belongs_to
-   influences
-   measures
-   validates
-   predicts
-   implements
-   references
-   supersedes

## Example

``` text
EVD-042 --supports--> LAW-013
LAW-013 --belongs_to--> GN-511
LAW-013 --influences--> GN-520
MET-007 --measures--> GN-511
EXP-003 --validates--> LAW-013
RULE-004 --derives_from--> LAW-013
COMP-021 --implements--> RULE-004
```

## Required Metadata

Every node should have:

-   Stable ID
-   Title
-   Type
-   Version
-   Status
-   Confidence
-   Author
-   Creation Date
-   Last Updated
-   References

## Confidence Model

Confidence applies to nodes and relationships.

-   Very High
-   High
-   Moderate
-   Low
-   Very Low
-   Unknown

## Design Principles

1.  Every claim should be traceable.
2.  Relationships are first-class data.
3.  Contradictory evidence is preserved.
4.  Nothing becomes a design rule without evidence.
5.  Every implementation traces back to biological or cognitive
    mechanisms whenever possible.

## Roadmap

Phase 1: - Complete ontology. - Assign IDs. - Link existing documents.

Phase 2: - Build relationship graph. - Add evidence and confidence.

Phase 3: - Generate visual graph. - Enable semantic queries.

## Long-Term Goal

The graph becomes the reasoning engine behind future tools that explain,
evaluate, and generate composition decisions instead of merely copying
existing designs.
