---
id: ECH-TEK-FOUNDATIONS
title: Tekmerion Echelon Foundations — State and Applicability
version: 1.0.0
status: accepted
created: 2026-09-29
updated: 2026-09-29
work_item: GH-16
related_documents:
  - docs/echelon/gh16-inventory-2026-09-29.md
  - docs/vnext/18-shared-application-foundations.md
  - docs/requirements/tekmerion-requirements.md
---

# Echelon Foundations — State and Applicability

Evidence: [`gh16-inventory-2026-09-29.md`](gh16-inventory-2026-09-29.md), a
read-only inventory in which every claim cites a file, a command or an upstream
source. This page records the state **after** the GH-16 upgrades, each
component's applicability under `TEK-ECH-001` (doc 18), and the gaps recorded
against the systems that own them.

## Current state (2026-09-29, after GH-16)

| Component | Installed | Executing | Upgrade performed | Verification | Applicable | Used |
| --- | --- | --- | --- | --- | --- | --- |
| ROS / Praxis (compat name) | 3.1.4 | ros-fs 3.1.4 | 3.1.1 → 3.1.4 via `npx --package=@echelon-foundry/repository-operating-system@3.1.4 ros upgrade`. Dry run: 0 conflicts, 3 changes | `./ros status` gives `installed`, `upgradeAvailable: null`. `./ros doctor`: no problems. `./ros validate`: passed | Yes, the repository operating contract | Yes (work protocol, telemetry, CI) |
| Ordo / SDE | 1.3.0 | npm 1.3.0 | none; npm-current | `npx @echelon-foundry/sde@1.3.0 verify`: passed. One structural review signal, SDE-STRUCT-001 on `tools/ros_cli.mjs` | Yes (doc 18 §5) | Yes, as method. `src/Tekmerion.Domain` follows the tier 1–2 contract (GH-14) |
| Limen | 0.6.2 | 0.6.2 | 0.6.1 → 0.6.2 via `npx @echelon-foundry/typescript-wasm-kernel@0.6.2 upgrade`. Dry run: 1 change | `verify --strict`: passed. This clears the LIMEN011 failure of the unpinned CI workflow on `main` | Yes, for the interactive browser boundary (TEK-EXP-002) | Not yet: the boundary is empty until #18 |
| Visual Engineering | 1.0.0 | 1.0.0 | none; current | `verify`: passed | Yes, as a UI validation input (TEK-ACC-003) | Agent context |
| Communication Engineering | 1.0.0 | upstream source 1.0.0 | none; current. npm has only 0.1.0 (G8) | `verify --strict` (upstream source): ok | Yes, as a validation input for consequential communication | Agent context |
| Aegis | not installed | — | not yet. `EchelonFoundry.Aegis.Core` 1.0.0 is on NuGet | — | **Required** at F# host boundaries (doc 18 §2) | **Gap.** Adoption is planned with the first Tekmerion host (GH-17 ingestion), because `Tekmerion.Domain` (tiers 1–2) must not reference it. Local backlog WI-0012 |
| Forma | not installed | — | not yet. npm 0.2.0 is the only pinnable release | — | **Required** once interactive UI exists (doc 18 §3). Static pages are semantic HTML | Planned for #18 |
| Folio | not installed | — | none | — | **Not applicable.** Tekmerion emits no printable, PDF or paginated artifact (doc 18 §4.10, OQ-TEK-012). No pinned release exists either (G9) | No |

## Decisions and constraints

- **The ROS CLI's own `./ros upgrade` was not used.** It would have run the
  3.0.3 CLI against a 3.1.1 installation, which is a silent downgrade that
  rewrites the shared `AGENTS.md` and drops six artifacts (inventory §1.5). The
  pinned 3.1.4 package is the supported path the tool documents.
- **Hold at ROS 3.1.4.** Praxis ≥ 3.3.0 ships native-only release assets, and
  the repository launcher cannot fetch them (G1, reproduced in a scratch copy).
  Moving to native Praxis waits for an upstream fix. `ros` therefore remains
  the supported executable in this repository (TEK-ECH-005).
- **`ros.json` `rosVersion` still reads 3.0.3.** It is shared configuration
  that the 3.1.4 launcher no longer consults (it reads `.echelon/ros.json`
  first). An attempt to hand-edit it for consistency was withheld, because
  manual edits to tool configuration are outside the lifecycle mechanism. It
  is recorded as WI-0010 for an owner decision.
- **`limen-verify.yml` stays unpinned.** It is tool-owned, and pinning it
  locally would fork a tool-owned file. The gap is recorded against Limen (G7).
- **Unmerged branches.** Do not merge `work/gh-11-readiness`: its manifest
  records hashes for files it did not commit (inventory §11). Its intended
  outcome, ROS 3.1.4 and Limen 0.6.2, is delivered here through the lifecycle
  commands. `chore/echelon-capabilities-2026-09-21-v2` is superseded. The
  Echelon changes on `work/gh-7-…-impl` are superseded.

## Gaps recorded against owning systems

GitHub access in this session is scoped to `kemiller2002/tekmerion`, so the
gaps are recorded as local backlog items that name the owning system. Filing
them upstream is the next step for someone with access to those repositories.

| Backlog | Owner | Gap |
| --- | --- | --- |
| WI-0004 | Praxis | G1: releases ≥ 3.3.0 have no `ros-fs` assets, so the repository launcher returns 404 |
| WI-0005 | Praxis | G2: the seeded `toolchain.json` pin disagrees with the CLI version |
| WI-0006 | Praxis | G4: no downgrade guard, and `doctor` recommends the downgrade |
| WI-0007 | Limen | G7: the tool-owned CI workflow runs an unpinned `verify --strict` |
| WI-0008 | Communication Engineering | G8: 1.0.0 is not on npm, and the installed commit is unrecorded |
| WI-0009 | Folio / Forma | G9/G10: no pinned published release, and Forma 0.3.0 is unpublished |
| WI-0010 | Tekmerion (owner decision) | `ros.json` `rosVersion` legacy fallback |
| WI-0012 | Tekmerion | adopt Aegis at F# host boundaries (GH-17) |

The inventory's G3 (stale Praxis npm channel), G5 (incomplete upgrade-bot
commits), G6 (`echelon doctor` blind spot), G11 (Aegis has no release tags) and
G12 (Visual Engineering leftover `release.json`) are recorded in the inventory
for the owners and do not block Tekmerion.
