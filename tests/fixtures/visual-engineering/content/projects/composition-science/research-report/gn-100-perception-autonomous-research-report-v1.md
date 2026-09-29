---
title: "Autonomous Research Report: Perception as the Foundation of Composition"
id: "RPT-GN100-001"
document_type: "research_report"
project: composition-science
version: "1.0"
status: "working"
date: "2026-07-19"
authors:
  - Kevin Miller
  - ChatGPT
purpose: >
  Records repeated hypothesis-driven research cycles investigating how visual
  perception constrains composition, grouping, search, memory, and hierarchy.
summary: >
  The investigation rejects simple linear and threshold-based models of
  composition. Perceptual performance is better modeled as task-dependent
  competition among object organizations, shaped by eccentricity, grouping,
  similarity, attention, search history, and required representational precision.
genome_nodes: [GN-100, GN-130, GN-210, GN-220, GN-510, GN-520]
candidate_laws: [LAW-001, LAW-002, LAW-006, LAW-007, LAW-008, LAW-009, LAW-010, LAW-011, LAW-012, LAW-013]
confidence:
  evidence: "High"
  interpretation: "Moderate-High"
  proposed_model: "Moderate"
  recommendations: "High"
evidence_level: "A-C"
tags: [visual-perception, grouping, crowding, visual-search, working-memory, composition, autonomous-research]
llm_ingest: true
machine_readable: true
purposes:
  - integrate
  - verify
audiences:
  - executive
  - practitioner
  - researcher
---

# Executive Summary

## What Was Accomplished

This research program tested five foundational assumptions in the emerging Composition Science framework:

1. Whether spacing can be treated as an independent and approximately universal cause of grouping.
2. Whether crowding is primarily a local distance threshold.
3. Whether visual attention is driven mainly by immediate physical salience.
4. Whether visual working memory imposes a fixed item limit that can be translated directly into composition rules.
5. Whether perception can be modeled as a simple linear pipeline from sensation to grouping, attention, memory, and decision.

None of these assumptions survives in its simplest form.

## Major Discoveries

### Perceptual organization is probabilistic and competitive

Spacing changes the likelihood of grouping, but proximity competes with similarity, connectedness, common region, global configuration, task, prior selection, and processing time. There is no credible basis for a universal spacing threshold that independently determines grouping.

### Crowding is not merely local clutter

Target recognition in peripheral vision is strongly constrained by eccentricity and neighbor spacing, but global configuration can increase or reduce crowding. Adding elements can sometimes improve recognition by reorganizing flankers into a separate group. This contradicts the simple claim that more nearby elements always produce more interference.

### Composition acts by creating perceptual objects

Grouping cues determine which elements are encoded as units, which compete for selection, and which become available to working memory. The relevant unit is often the perceptual object or structured ensemble rather than the individual visible element.

### Attention depends on more than salience

Immediate visual features guide attention, but task goals, scene structure, previous selection, reward history, learned regularities, and observer expertise also influence search. A salient element is not guaranteed to win, and a visually subtle element may be found quickly when the observer has learned where or what to expect.

### Working-memory limits are conditional

The familiar estimate of roughly three to four objects is useful under many laboratory conditions, but it is not a universal design constant. Performance depends on object complexity, required precision, grouping, encoding strategy, attention, and scene structure.

## Revised Understanding

> Composition changes performance by changing the probability and stability of competing perceptual organizations. Those organizations alter crowding, attentional guidance, object encoding, memory load, and action selection.

This is more defensible than a one-way chain in which spacing creates groups, groups reduce search, and reduced search automatically improves decisions.

## Confidence

- Empirical findings: **High**
- Cross-study interpretation: **Moderate to High**
- Unified model: **Moderate**
- Direct UI thresholds or token values: **Low**

## Remaining Uncertainty

The largest unresolved issue is ecological transfer. Most foundational evidence comes from controlled arrays, letters, dots, simple features, and short tasks. The mechanisms are real, but their quantitative translation into interfaces, architecture, editorial pages, dashboards, and other complex compositions remains under-tested.

---

# Knowledge Delta

## What We Knew Before

- Proximity influences grouping.
- Crowding limits peripheral recognition.
- Attention is limited.
- Working memory is limited.
- Grouping could plausibly reduce search and memory demands.

## What We Know Now

- Proximity is one cue in a competitive organization process.
- Crowding depends on global grouping as well as local spacing.
- Attention is guided jointly by stimulus, goals, history, and scene structure.
- Memory capacity depends on the unit and precision of representation.
- Layout quality cannot be predicted from spacing, density, or item count alone.
- Perceptual organization is the likely bridge between low-level visual variables and higher-level composition outcomes.

## Assumptions Removed

- More whitespace always improves perception.
- More flankers always worsen recognition.
- Salience alone determines attentional priority.
- Three or four visible items is a universal design maximum.
- Grouping cues have a fixed hierarchy.
- Perception behaves as a strictly feed-forward sequence.

---

# Research Log

## Cycle 1 — Can Proximity Support Universal Spacing Bands?

### Objective
Determine whether evidence supports fixed weak, moderate, and strong spacing bands for design.

### Hypothesis
Grouping strength is primarily determined by relative distance, allowing universal spacing ratios to predict perceived groups.

### Evidence Found
Dot-lattice research demonstrated lawful and approximately probabilistic relationships between relative spacing and grouping direction. The repetition-discrimination method also produced objective behavioral effects for proximity, similarity, common region, and connectedness.

### Evidence Against
Grouping remains multistable; similarity interacts with proximity; different organizations compete; grouping probability is continuous; task and processing time can alter the observed organization; and dot-lattice results do not establish transfer to semantically rich UI elements.

### Falsification Attempt
If universal spacing bands existed, comparable spacing ratios should produce stable grouping across element types, tasks, visual fields, and competing cues. The literature does not establish this invariance.

### Conclusion
**Reject fixed universal spacing bands. Retain probabilistic proximity.**

### Confidence
High.

### Next Step
Investigate whether crowding supplies a more biologically stable spacing constraint.

---

## Cycle 2 — Is Crowding a Local Distance Law?

### Objective
Determine whether crowding can supply a universal biological spacing rule.

### Hypothesis
Peripheral recognition fails when flankers fall within a critical distance approximately proportional to target eccentricity, permitting a single local spacing rule.

### Evidence Found
A large body of work confirms that nearby flankers impair peripheral identification and that critical spacing often scales with eccentricity. This is one of the strongest quantitative constraints in vision science.

### Evidence Against
Critical spacing varies with stimulus and task. Radial and tangential arrangements differ. Target-flanker similarity matters. Attention and eye-movement preparation alter performance. Global flanker configuration matters. Adding more flankers can reduce crowding when those elements group together and segregate from the target.

### Falsification Attempt
If crowding were purely local, remote elements should not improve recognition and adding flankers should never help. Uncrowding and global-configuration experiments contradict both predictions.

### Conclusion
**Retain eccentricity-scaled separation as a baseline risk factor, not a complete law.**

### Confidence
High.

### Next Step
Determine whether grouping protects targets by creating object boundaries.

---

## Cycle 3 — Does Grouping Reduce Interference by Creating Object Boundaries?

### Objective
Identify the mechanism by which enclosure, connectedness, similarity, alignment, and spacing influence recognition.

### Hypothesis
Grouping cues reduce interference when they organize distractors into an object or ensemble that excludes the target.

### Evidence Found
Grouped flankers can reduce crowding independently of target distance. Common region and connectedness create strong units. Grouping principles can also improve visual working-memory performance under some conditions.

### Evidence Against
Grouping does not always help. A target grouped with distractors can become harder to individuate. Strong connectedness can impose an incorrect structure. Benefits depend on whether the organization aligns with the task. Top-down effects also depend on available processing time.

### Falsification Attempt
If grouping were universally beneficial, any increase in grouping strength should improve search and memory. This is false.

### Conclusion
**Propose Organizational Fit as a central composition variable.**

### Confidence
Moderate to High.

### Next Step
Test whether attentional guidance can be predicted from physical organization alone.

---

## Cycle 4 — Is Visual Salience the Primary Driver of Attention?

### Objective
Determine whether contrast, color, size, motion, and uniqueness are sufficient to model hierarchy.

### Hypothesis
Elements with the strongest physical feature contrast receive attention first and therefore dominate composition.

### Evidence Found
Feature differences can produce efficient search and pop-out-like behavior. Physical salience is a real and important contributor to hierarchy.

### Evidence Against
Attentional priority is also shaped by current goals, target templates, scene semantics, previous selections, location probability, reward, learned value, expertise, and eye-movement strategy. Training can substantially improve search for complex conjunctions.

### Falsification Attempt
If salience were sufficient, repeated experience and expectation should not change search efficiency for identical displays. They do.

### Conclusion
**Reject salience-only hierarchy. Propose Multi-Source Priority.**

### Confidence
High.

### Next Step
Investigate whether memory limits supply a stable upper bound on visible complexity.

---

## Cycle 5 — Is Working Memory a Fixed Three-to-Four Item Design Limit?

### Objective
Evaluate whether the common three-to-four-item estimate can become a direct composition law.

### Hypothesis
Interfaces should expose no more than three or four meaningful items at once because visual working memory cannot maintain more.

### Evidence Found
Many experiments support severe limits in active visual maintenance. Object-based accounts often estimate capacity around three to four representations under specific change-detection conditions.

### Evidence Against
Capacity estimates vary with stimulus complexity, required precision, grouping, strategy, attention, meaningful structure, and long-term knowledge. Natural scenes are not adequately represented by arrays of isolated colored items.

### Falsification Attempt
If three to four items were universal, meaningful structure or grouping should not increase effective performance. They do.

### Conclusion
**Reject a universal three-to-four visible-item rule. Retain limited active representation as a mechanism.**

### Confidence
High.

### Next Step
Integrate grouping, crowding, attention, and memory into a shared model.

---

## Cycle 6 — Does the Existing Linear Pipeline Survive Falsification?

### Objective
Evaluate the chain: Spacing → Grouping → Reduced Search → Lower Memory Load → Better Decisions.

### Hypothesis
The chain is generally causal and can serve as the primary architecture of Composition Science.

### Evidence Found
Each link can occur in some conditions.

### Evidence Against
Attention can alter grouping. Task goals can override stimulus organization. Search history changes selection. Grouping can worsen identification. Memory templates guide search. Eye movements alter crowding. Decisions determine which distinctions matter. Global organization changes local appearance.

### Falsification Attempt
If the pipeline were strictly linear, later-stage variables such as task, memory, and expectation should not change earlier perceptual organization. They do.

### Conclusion
**Replace the linear pipeline with a recurrent Organizational Competition Model.**

### Confidence
Moderate to High.

### Next Step
Validate the model in realistic composition tasks.

---

# Confirmed Findings

## CF-001 — Peripheral recognition is constrained by crowding
Nearby elements can impair identification of otherwise visible targets, especially outside central vision.

**Confidence:** Very High

## CF-002 — Critical spacing often scales with eccentricity
Eccentricity is a major predictor of crowding risk, although the proportionality is not a universal constant.

**Confidence:** High

## CF-003 — Global configuration can modify local crowding
Elements beyond the nearest flankers can improve or worsen recognition by changing perceptual organization.

**Confidence:** High

## CF-004 — Grouping cues are objectively measurable
Proximity, similarity, common region, and connectedness produce quantifiable behavioral effects.

**Confidence:** High

## CF-005 — Grouping cues compete
No single cue has a fixed universal dominance ranking across all tasks and timing conditions.

**Confidence:** High

## CF-006 — Attention is guided by multiple sources
Physical features, task goals, scene structure, and selection history jointly determine search priority.

**Confidence:** Very High

## CF-007 — Visual working memory is limited but not defined by one universal item count
Capacity depends on the representational unit, required precision, structure, and strategy.

**Confidence:** High

## CF-008 — Visibility and recognition are distinct
A target can be detectable without being identifiable, especially in peripheral clutter.

**Confidence:** Very High

---

# Rejected Hypotheses

## RH-001 — Universal spacing bands
The evidence supports probabilistic effects of relative distance but not invariant weak, moderate, and strong UI spacing ratios.

## RH-002 — More whitespace is always better
Increasing separation can improve individuation but can also destroy useful groups, increase scan distance, or weaken task-relevant structure.

## RH-003 — More flankers always increase crowding
Additional elements can reduce crowding by grouping flankers together and separating them from the target.

## RH-004 — Salience determines hierarchy
Feature contrast contributes to selection but does not uniquely determine it.

## RH-005 — Three to four visible items is a universal composition limit
The estimate applies to specific forms of active maintenance, not to all visible or meaningfully structured information.

## RH-006 — Perception is a one-directional pipeline
Task, memory, attention, eye movements, and prior learning feed back into perceptual organization.

---

# Open Questions

| Rank | Question | Importance | Expected Value | Effort |
|---|---|---|---|---|
| 1 | How accurately do laboratory measures predict performance in real interfaces? | Critical | Very High | High |
| 2 | Can Organizational Fit be measured independently of task performance? | Critical | Very High | Medium-High |
| 3 | Can spacing, enclosure, similarity, alignment, and connectedness be combined into a predictive function? | High | High | High |
| 4 | How should composition models account for changing eccentricity as users move their eyes? | High | High | High |
| 5 | How much effective composition reflects stable perception versus learned visual grammar? | High | High | Medium |
| 6 | Do analogous organization principles govern auditory, tactile, spatial, and temporal composition? | Moderate | Medium-High | High |

---

# Emerging Patterns

## EP-001 — The unit of perception is relational
Across grouping, crowding, search, and memory, performance depends on relations among elements rather than isolated element properties.

## EP-002 — Structure can both compress and conceal
Grouping can reduce effective complexity by creating units, but the same grouping can hide distinctions or bind targets to distractors.

## EP-003 — Biological constraints define risk zones, not complete design rules
Eccentricity, acuity, contrast sensitivity, and crowding provide strong constraints, but cognitive organization and learning determine many real outcomes.

## EP-004 — Composition is active, not static
Observers move their eyes, form expectations, learn regularities, and change strategies. A screenshot is not the entire stimulus.

## EP-005 — The same visual variable can help or hurt
Similarity can support grouping or increase interference. Connectedness can clarify objects or impose false units. Contrast can guide attention or create competition.

---

# Proposed Models

## PM-001 — Organizational Competition Model

At any moment, a visual field supports multiple candidate organizations. Each receives evidence from spatial relations, similarity, connectedness, enclosure, alignment, common fate, learned schemas, and task goals. Attention and eye movements stabilize some organizations and suppress others. Performance depends on whether the stabilized organization exposes the distinctions required for action.

```text
Organization Score(O, t) =
    Spatial Evidence
  + Feature Similarity
  + Boundary / Connection Evidence
  + Global Configuration
  + Task Relevance
  + Selection History
  + Learned Probability
  - Competing Organization Strength
  - Crowding / Interference
  - Precision Cost
```

The expression is conceptual. Current evidence does not justify fixed weights.

### Predictions

1. Increasing spacing will not always improve performance.
2. Adding elements can improve performance when they reorganize distractors.
3. Strong visual hierarchy can fail when it conflicts with task expectations.
4. Grouping benefits depend on whether group boundaries match task boundaries.
5. Familiar layouts can outperform physically clearer layouts when learned expectations strongly guide selection.
6. Layout effects change as fixation changes.

### Confidence
Moderate.

## PM-002 — Organizational Fit

The correspondence between perceptually encoded units and the units required to complete a task.

### Predicted Outcomes of Higher Fit

- faster search;
- fewer incorrect bindings;
- reduced working-memory coordination;
- more accurate action selection;
- improved confidence;
- lower dependence on training.

### Confidence
Moderate to High.

## PM-003 — Multi-Source Priority Field

A dynamic attentional priority distribution combining physical feature contrast, target relevance, scene semantics, spatial probability, selection history, reward history, inhibition, and current fixation.

### Implication
Hierarchy cannot be evaluated from visual salience alone.

### Confidence
High.

---

# Candidate Law Revisions

## LAW-002 — Peripheral Separation Cost
The separation required for reliable individuation generally increases with eccentricity, but the effective cost is moderated by target-flanker similarity, global grouping, configuration, attention, and eye-movement state.

## LAW-006 — Structural Encoding
Elements are processed and remembered partly according to the perceptual units and relations imposed by the composition. Structure reduces effective complexity only when those units align with the task.

## LAW-007 — Cue Competition
Grouping cues contribute evidence to competing organizations. Dominance depends on cue strength, compatibility, task, timing, and learned interpretation rather than a fixed hierarchy.

## LAW-013 — Probabilistic Proximity
Relative distance changes the probability of spatial grouping, but its effect is conditional on competing cues, global configuration, element identity, task, and viewing conditions.

---

# Recommendations

| Priority | Research Direction | Expected Value | Effort | Rationale |
|---|---|---:|---:|---|
| 1 | Build a factorial cue-competition dataset using realistic UI fragments | Very High | High | Tests transfer from laboratory grouping to composition |
| 2 | Define and operationalize Organizational Fit | Very High | Medium | Bridges perception and task performance |
| 3 | Create a fixation-aware layout measurement protocol | High | High | Converts eccentricity and crowding into dynamic measures |
| 4 | Map selection-history research into hierarchy | High | Medium | Prevents salience-only rules |
| 5 | Audit candidate laws for hidden linear assumptions | High | Medium | Aligns the framework with recurrent processing |
| 6 | Develop an evidence schema for effect sizes and boundary conditions | High | Low | Improves future autonomous research |
| 7 | Compare UI, editorial, architecture, and visualization for shared patterns | Medium-High | High | Tests universality across disciplines |

## Immediate Highest-Value Next Step

Create **EXP-001: Composition Cue Competition Benchmark**.

The benchmark should vary proximity, alignment, common region, connectedness, similarity, target eccentricity, task, exposure duration, and familiarity. Dependent measures should include grouping judgment, search time, identification accuracy, eye movements, confidence, and delayed recall.

To minimize new human experiments initially, phase one should mine and normalize comparable conditions from existing published experiments. New testing should be reserved for gaps that cannot be resolved from prior data.

---

# Bibliography

## Academic — Reviews and Integrative Models

- Wolfe, J. M. (2021). Guided Search 6.0: An updated model of visual search. *Psychonomic Bulletin & Review*. https://pmc.ncbi.nlm.nih.gov/articles/PMC8965574/
- Wolfe, J. M. (2020). Forty years after Feature Integration Theory. https://pmc.ncbi.nlm.nih.gov/articles/PMC7039157/
- Wolfe, J. M. (2017). Five factors that guide attention in visual search. https://pmc.ncbi.nlm.nih.gov/articles/PMC9879335/
- Wagemans, J., et al. (2012). A century of Gestalt psychology in visual perception. *Psychological Bulletin*. https://pubmed.ncbi.nlm.nih.gov/22845751/
- Strasburger, H. (2020). Seven myths on crowding and peripheral vision. https://pmc.ncbi.nlm.nih.gov/articles/PMC7238452/
- Strasburger, H., Rentschler, I., & Jüttner, M. (2011). Peripheral vision and pattern recognition: A review. https://pubmed.ncbi.nlm.nih.gov/22207654/
- Herzog, M. H., Sayim, B., Chicherov, V., & Manassi, M. (2015). Crowding, grouping, and object recognition. https://pubmed.ncbi.nlm.nih.gov/26024452/
- Luck, S. J., & Vogel, E. K. (2013). Visual working memory capacity. https://pmc.ncbi.nlm.nih.gov/articles/PMC3729738/
- Brady, T. F., Konkle, T., & Alvarez, G. A. (2011). A review of visual memory capacity. https://pmc.ncbi.nlm.nih.gov/articles/PMC3405498/
- Oberauer, K. (2019). Working memory and attention. https://pmc.ncbi.nlm.nih.gov/articles/PMC6688548/
- Luck, S. J., et al. (2024). Visual working memory for natural scenes. https://pmc.ncbi.nlm.nih.gov/articles/PMC11365787/

## Academic — Perceptual Grouping

- Kubovy, M., & Wagemans, J. (1995). Grouping by proximity and multistability in dot lattices. *Psychological Science*, 6, 225–234.
- Kubovy, M., Holcombe, A. O., & Wagemans, J. (1998). On the lawfulness of grouping by proximity. *Cognitive Psychology*, 35, 71–98.
- Kubovy, M., & van den Berg, M. (2008). A probabilistic model of grouping by proximity and similarity. *Psychological Review*, 115, 131–154.
- Palmer, S. E., Brooks, J. L., & Nelson, R. An objective method for studying perceptual grouping. https://pubmed.ncbi.nlm.nih.gov/17515217/
- Beck, D. M., & Palmer, S. E. (2002). Top-down influences on perceptual grouping. https://pubmed.ncbi.nlm.nih.gov/12421056/
- Villalba-García, C., et al. (2021). Competition between perceptual grouping cues. https://pubmed.ncbi.nlm.nih.gov/33818202/
- Seymour, K., et al. (2008). Perceptual grouping in the human brain. https://pubmed.ncbi.nlm.nih.gov/18955906/
- Peterson, D. J., & Berryhill, M. E. (2013). Similarity benefits visual working memory. https://pubmed.ncbi.nlm.nih.gov/23702981/

## Academic — Crowding

- Pelli, D. G., et al. (2007). Crowding and eccentricity determine reading rate. https://pubmed.ncbi.nlm.nih.gov/18217835/
- Chung, S. T. L., Levi, D. M., & Legge, G. E. (2001). Spatial-frequency and contrast properties of crowding. https://pubmed.ncbi.nlm.nih.gov/11369047/
- Sayim, B., Westheimer, G., & Herzog, M. H. (2013). Grouping and crowding affect target appearance. https://pubmed.ncbi.nlm.nih.gov/23967164/
- Harrison, W. J., et al. (2013). Eye movement targets are released from visual crowding. https://pubmed.ncbi.nlm.nih.gov/23407951/
- Freeman, J., & Simoncelli, E. P. (2011). Metamers of the ventral stream. https://pubmed.ncbi.nlm.nih.gov/21841776/

## Academic — Visual Search and Learning

- Treisman, A., & Gelade, G. (1980). A feature-integration theory of attention. *Cognitive Psychology*, 12, 97–136.
- Wolfe, J. M. (2020). Major issues in the study of visual search. https://pmc.ncbi.nlm.nih.gov/articles/PMC7250731/
- Kristjánsson, Á., et al. (2006). Neural basis for priming of pop-out. https://pmc.ncbi.nlm.nih.gov/articles/PMC2600429/
- Reavis, E. A., et al. (2018). Learning efficient visual search for feature conjunctions. https://pmc.ncbi.nlm.nih.gov/articles/PMC6035115/
- Zhang, Q., et al. (2022). Visual search training benefits from changes in covert attention and eye movements. https://pmc.ncbi.nlm.nih.gov/articles/PMC9296888/

## Academic — Working Memory

- Cowan, N. (2010). The magical mystery four. https://pmc.ncbi.nlm.nih.gov/articles/PMC2864034/
- Fukuda, K., et al. (2010). Discrete capacity limits in visual working memory. https://pmc.ncbi.nlm.nih.gov/articles/PMC3019116/
- Bengson, J. J., et al. (2016). Effects of strategy on visual working memory capacity. https://pmc.ncbi.nlm.nih.gov/articles/PMC4698363/
- Barton, B., et al. (2013). Visual working memory in human cortex. https://pmc.ncbi.nlm.nih.gov/articles/PMC4752675/

## Books

- Kubovy, M., & Pomerantz, J. R. (Eds.). (1981/2017). *Perceptual Organization*.
- Boff, K. R., Kaufman, L., & Thomas, J. P. (Eds.). (1986). *Handbook of Perception and Human Performance*.

## Industry

No industry source was treated as primary evidence.

## Patents

No patent evidence was necessary for these hypotheses.

## Standards

No current standard directly resolves the investigated perceptual mechanisms. Accessibility standards may provide implementation constraints but are not experimental proof of the model.

## Historical

- Wertheimer, M. (1923). Investigations on Gestalt principles.
- Bouma, H. (1970). Interaction effects in parafoveal letter recognition.

---

# Knowledge Graph Additions

```text
EVD-GRP-001 --supports--> LAW-013
EVD-GRP-002 --supports--> LAW-007
EVD-CRD-001 --supports--> LAW-002
EVD-CRD-002 --contradicts--> RH-003
EVD-SRC-001 --supports--> PM-003
EVD-WM-001 --contradicts--> RH-005

PM-001 --belongs_to--> GN-100
PM-001 --influences--> GN-210
PM-001 --influences--> GN-220
PM-001 --influences--> GN-510
PM-001 --influences--> GN-520

PM-002 --derived_from--> PM-001
PM-002 --predicts--> search_efficiency
PM-002 --predicts--> binding_accuracy
PM-002 --predicts--> task_performance
```

---

# Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | 2026-07-19 | ChatGPT | Initial autonomous research cycles for GN-100 Perception |
