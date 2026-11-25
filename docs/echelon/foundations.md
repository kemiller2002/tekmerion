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
| Limen | 0.9.1 | 0.9.1 | 0.7.1 → 0.9.1 (WI-0026) via `npx --yes @echelon-foundry/limen@0.9.1 upgrade` (1 change: the installation manifest). 0.7.0 → 0.7.1 via `npx --yes @echelon-foundry/limen@0.7.1 upgrade` (1 change: the installation manifest; the pinned workflow reads `installedVersion`, so it was not rewritten). Earlier, 0.6.2 → 0.7.0 via `npx --yes @echelon-foundry/limen@0.7.0 upgrade` (package renamed from `@echelon-foundry/typescript-wasm-kernel`); that dry run made 2 changes and rewrote the unedited workflow to the pinned form | `verify --strict`: `not-applicable`, exit 0. With the old empty boundary 0.7.0 reports `not-configured` (LIMEN012, exit 8), so `limen.config.json` now declares `boundary.notApplicable` with a rationale | Not yet: no browser or WASM boundary code exists. Applies to the interactive browser boundary (TEK-EXP-002) | No. Replace `notApplicable` with engine and kernel paths when #18 lands |
| Visual Engineering | 1.0.0 | 1.0.0 | none; current | `verify`: passed | Yes, as a UI validation input (TEK-ACC-003) | Agent context |
| Communication Engineering | 1.0.0 | upstream source 1.0.0 | none; current. npm has only 0.1.0 (G8) | `verify --strict` (upstream source): ok | Yes, as a validation input for consequential communication | Agent context |
| Aegis | not installed | — | not yet. `EchelonFoundry.Aegis.Core` 1.0.0 is on NuGet | — | **Required** at F# host boundaries (doc 18 §2) | **Gap.** Adoption is planned with the first Tekmerion host (GH-17 ingestion), because `Tekmerion.Domain` (tiers 1–2) must not reference it. Local backlog WI-0012 |
| Forma | 1.0.1 | 1.0.1 | 0.4.1 → 1.0.1 (WI-0026): `conditor upgrade --current` against echelon-current 1.42.0, with Forma declared in `conditor.json` through Conditor's opt-in so Conditor owns the pin. Earlier 0.2.0 → 0.4.1 (WI-0018). Migration: Forma 1.0 renames `ef-` to `forma-`; the shipped `tools/NamespaceMigrator` codemod rewrote `src/Tekmerion.Core` (Site.fs, Html.fs), the site test and the e2e script, and `--check` is clean. The Echelon brand left Forma; Tekmerion does not adopt the optional `@echelon-foundry/echelon-theme`, so pages render with Forma's neutral palette | Every Forma class the pages use exists in 1.0.1; slice built twice byte-identical; e2e navigation passes; 320px overflow on `p/composition-science/` falls from 11px to 0px, `a/RP-COMP-005/` stays 46px (recorded, not gated) | Yes. The static pages are Forma-styled (doc 18 §3) | Yes. `--forma node_modules/@echelon-foundry/design-system/dist/all.css` in the slice workflow (GH-18) |
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
- **`limen-verify.yml` is pinned from Limen 0.7.0.** It is tool-owned; Limen
  0.7.0 closed G7 upstream, and `upgrade` rewrote the unedited workflow to read
  `installedVersion` from `.echelon/limen.json` and run that exact version.
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
| WI-0007 | Limen | G7: the tool-owned CI workflow runs an unpinned `verify --strict`. Resolved by Limen 0.7.0 (pinned workflow) |
| WI-0008 | Communication Engineering | G8: 1.0.0 is not on npm, and the installed commit is unrecorded |
| WI-0009 | Folio / Forma | G9/G10: no pinned published release, and Forma 0.3.0 is unpublished |
| WI-0010 | Tekmerion (owner decision) | `ros.json` `rosVersion` legacy fallback |
| WI-0012 | Tekmerion | adopt Aegis at F# host boundaries (GH-17) |

The inventory's G3 (stale Praxis npm channel), G5 (incomplete upgrade-bot
commits), G6 (`echelon doctor` blind spot), G11 (Aegis has no release tags) and
G12 (Visual Engineering leftover `release.json`) are recorded in the inventory
for the owners and do not block Tekmerion.
