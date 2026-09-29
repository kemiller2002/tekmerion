# Tekmerion handoff

Last updated 2026-09-29 by Claude (anthropic / claude-code / claude-opus-5-5,
session `session_01Aq9waXCL6GD3697TfG9Tw6`). Token usage and cost were not
exposed by this runtime and are recorded as unavailable in each execution
record under `.ros/telemetry/executions/`.

## Objective

Tekmerion vNext (#11): turn the former Research Publisher into an
evidence-faithful research system, starting with a real vertical slice.

## State (branches are stacked; each PR targets the one below it)

| Work item | Branch | PR | ROS state |
| --- | --- | --- | --- |
| GH-13 canonical requirements (REQ-TEK 1.1.0) | `work/gh-13-canonical-requirements` | #20 → `main` | complete |
| GH-14 typed domain + legal transitions | `work/gh-14-typed-domain` | #21 | complete |
| GH-16 Echelon foundations (ROS 3.1.4, Limen 0.6.2, inventory) | `work/gh-16-echelon-foundations` | #22 | active (Aegis now used by the host; Forma used by the site; Praxis native blocked upstream) |
| GH-17 ingestion + `/data/v1` contracts | `work/gh-17-ingestion` | #23 | complete |
| GH-18 static research experience | `work/gh-18-research-experience` | #24 | active (Limen layer open) |
| GH-19 operational proof + baselines | `work/gh-19-operational-proof` | #25 | complete |
| GH-15 identity migration (config 3) | `work/gh-15-identity-migration` | see PR list | complete for repository scope; npm rename owner-gated (OQ-TEK-014) |

Per-item continuation notes: `.ros/work/items/GH-*.md`. Backlog of recorded
gaps: `./ros work` (WI-0004 … WI-0016).

## Where things are

- Requirements: `docs/requirements/` (REQ-TEK, traceability, open questions).
- Architecture: `docs/architecture/` (domain, ingestion, experience, operations).
- Decisions: `research/decisions/DF-TEK-2026-000{1..4}`.
- Echelon state: `docs/echelon/foundations.md`.
- Code: `src/Tekmerion.{Domain,Core,Cli}`, tests in `tests/Tekmerion.*`.

## Validation (at the top of the stack)

- `dotnet test ResearchPublisher.Lifecycle.sln` passes on SDK 8.0.131 and 10.0.112.
- `npx vitest run` passes (41).
- `./ros validate`, `./ros verify`, `limen verify --strict`, `sde verify`,
  and `visual-engineering verify` pass.

## Open decisions for the owner

OQ-TEK-001 (sub-document authoring contract), 002 (frontier id stability),
003–006, 014 (npm package rename). See `docs/requirements/open-questions.md`.

## Next recommended actions

1. Review and merge the stack bottom-up (#20 first).
2. GH-18: Limen interactive layer (WI-0015); report Forma gaps WI-0013/14 upstream.
3. Decide OQ-TEK-014, then ship the package rename as configuration version 4.
4. Decide whether to publish the Tekmerion site to Pages (currently legacy site).
