# GH-16 inventory: Tekmerion Echelon foundations (read-only)

Date: 2026-09-29. Repository: `/home/user/tekmerion` (branch `work/gh-13-canonical-requirements` @ `06ce715`; `main` = `origin/main` = `146ae09`). The `.echelon/`, `.sde/`, `ros.json` and `tools/` trees are identical between HEAD and `main` (`git diff --stat main -- .echelon .sde ros.json tools` is empty).

**Method and safety.** No file in `/home/user/tekmerion` was modified by this inventory. The pre-existing dirty files (`.ros/context/current.json`, `.ros/events/events.jsonl`, `.ros/telemetry/executions/EXE-20260929T091922004Z-b57e5b52.json`) came from the GH-13 `work start` at 09:19, before this inventory ran. Every lifecycle command, including the read-only ones, ran against a byte copy at `scratchpad/tek` (a `cp -a`). Upgrade dry-runs and one applied upgrade ran only in the throwaway copies `scratchpad/tek-up` and `scratchpad/tek-gh11`. After each command, `git status` on the copy showed only the same three pre-existing dirty paths. Upstream repositories were shallow-cloned into `scratchpad/up/`. The GitHub MCP tools refused every repository except `kemiller2002/tekmerion` ("not configured for this session"), so upstream facts come from git clones, `git ls-remote`, release-asset HTTP probes, the npm registry and NuGet. Native Praxis 3.5.0 and Ordo 1.4.0 were installed into `scratchpad/echelon-home` only. One side effect outside the repo: `npx …repository-operating-system@3.1.4` cached a ros-fs 3.1.4 binary under `~/.cache/ros-fs/3.1.4`. The 3.0.3 binary was already cached there.

## Summary table

| Component | Installed? | Installed version (manifest) | Executing version | Latest upstream | Ownership model | Upgrade path | Verify command → result | Applicable (docs/vnext/18) | Used vs installed |
|---|---|---|---|---|---|---|---|---|---|
| **ROS** (`./ros`, `ros.json`, `.echelon/ros.json`, `.ros/installation.json`) | Yes | `.echelon/ros.json` 3.1.1 (config v1, 88 artifacts). `ros.json.rosVersion` 3.0.3. Legacy `.ros/installation.json` 1.2.1 | **ros-fs 3.0.3** (`./ros --version`) | npm `latest` 3.1.4. Git tag / native release v3.5.0 | Per-file tool-owned / user-owned / shared / generated in `.echelon/ros.json` | `npx --package=@echelon-foundry/repository-operating-system@3.1.4 ros upgrade` (dry-run is clean). **Do not** run `./ros upgrade`: it is a 3.0.3 downgrade | `./ros verify --json` → valid:true, state `upgrade-required`. `./ros doctor` warns `upgrade-available` "3.0.3" | ROS is the base contract (AGENTS.md). Not named in doc 18 | Used operationally (CI `ros-validation.yml`, `readiness-work.yml`). Not a code dependency |
| **Praxis** (ROS successor) | No native install. The repo holds the ROS-named installation only | n/a (no `.echelon/toolchain.json`) | none on PATH | v3.5.0 (native bundles from v3.3.0 on). `package.json` 3.5.0 still named `@echelon-foundry/repository-operating-system` | Same manifest (`.echelon/ros.json`). 3.5.0 adds shared `.echelon/toolchain.json` | `praxis upgrade` (native). **Blocked**: see Gaps G1–G2 | `praxis verify --json` (scratch, 3.5.0) → valid, `upgrade-required`, upgradeAvailable 3.5.0 | Same as ROS | n/a |
| **Ordo / SDE** (`.sde/`, `.echelon/sde.json`) | Yes | 1.3.0 (config v2, 19 files, source rev `5e686d2`) | `npx @echelon-foundry/sde@1.3.0` → 1.3.0 | npm 1.3.0. Native/GitHub v1.4.0 (`distribution/package.json` 1.4.0) | `.sde/` payload hash-pinned in `.sde/MANIFEST.json`. `.echelon/sde.json` is the install record | `ordo upgrade` (native 1.4.0). Dry-run: replace payload 1.3.0→1.4.0 (19 files) and rewrite `.echelon/sde.json` | `sde@1.3.0 verify --json` → passed. Structural finding SDE-STRUCT-001 on `tools/ros_cli.mjs` (1085 lines). `ordo@1.4.0 status` → `upgrade-required` | Ordo is the domain/state layer in doc 18 §5 | Method docs only. No code reference to `.sde` in src/site/tests |
| **Limen** (`.echelon/limen.json`, `limen.config.json`) | Yes | 0.6.1 | CI runs unpinned `npx --yes @echelon-foundry/typescript-wasm-kernel` (npm latest 0.6.2) | 0.6.2 (npm, CHANGELOG 2026-09-21). Git tag only v0.6.1 | `limen-verify.yml` tool-owned. `limen.config.json` user-owned | `npx @echelon-foundry/typescript-wasm-kernel@0.6.2 upgrade` | 0.6.1 and 0.6.2 `verify --json` → ok. **0.6.2 `verify --strict` → exit 3, LIMEN011** "installation older than CLI". CI "Limen verify" fails on main (runs 94, 95) | Browser/application interaction boundary (doc 18 §5) | Installed only. `limen.config.json` has empty `engine`/`kernel` boundaries. No code references |
| **Aegis** | **No** | n/a | n/a | NuGet `EchelonFoundry.Aegis.Core` 1.0.0 (also `.Integration.GitHub`, `.Store.GitHub` 1.0.0). net8.0 | n/a (NuGet PackageReference) | `dotnet add package EchelonFoundry.Aegis.Core --version 1.0.0` (pinned) | n/a | **Required** at build, filesystem, parsing, browser/WASM and other operational boundaries (doc 18 §2) | **Not referenced anywhere.** fsproj PackageReferences are only the test SDK and xunit |
| **Forma** | **No** | n/a | n/a | npm `@echelon-foundry/design-system` 0.2.0. Upstream `package.json` 0.3.0 unpublished. Tags v0.1.0, v0.2.0 | n/a (npm dependency) | Add a pinned dependency | n/a | **Required for interactive browser UI** (§3) | Not referenced. The "Forma" grep hits are substrings of "Format". No `<ef-*>` |
| **Visual Engineering** (`.visual-engineering/`, `.echelon/visual-engineering*.json`) | Yes | 1.0.0 (config v3, context 1.0.0, source `0be73c8`) | `npx @echelon-foundry/visual-engineering@1.0.0` → 1.0.0 | npm 1.0.0. Tag `visual-engineering-v1.0.0`. CHANGELOG top is 1.0.0 | Tool-owned guidance files, generated index/sources/context, shared `.gitignore` / `AGENTS.md` / config | Current. `npx @echelon-foundry/visual-engineering upgrade` when a newer release exists | `status`/`verify`/`doctor --json` → passed, healthy, "installation matches this release" | Not named in doc 18 (it is agent-context research) | Agent context only. `.visual-engineering/release.json` (ui-context-v0.1.0) is a stale, unmanaged leftover |
| **Communication Engineering** (`.communication-engineering/`, `.echelon/communication-engineering*.json`) | Yes | 1.0.0 (config v1) | No pinned runner. Verified with upstream HEAD source `4590d2f` (`bin/communication-engineering.mjs`, 1.0.0) | GitHub `package.json` 1.0.0. **npm `latest` is 0.1.0** (2026-08-15). 1.0.0 not published | 5 tool-owned files plus a `shared-region` in AGENTS.md | Per upstream docs, `npm exec --package=github:kemiller2002/communication-engineering#<sha> communication-engineering -- upgrade` | `status`/`verify --strict --json` (upstream source) → ok | Not named in doc 18 | Agent context only |
| **Folio** | **No** | n/a | n/a | Upstream `@echelon-foundry/print-components` 0.3.0. **Not on npm (404)**. No tags | n/a | Not installable as a pinned release yet | n/a | Required only when printable/PDF/paginated output exists (§4 item 10) | Not referenced. No print/PDF surface found |
| **Research Publisher** (product, `.echelon/research-publisher.json`) | Yes (self-hosting source repo) | 0.1.0 (config v2) | Local build `research-publisher-lifecycle` 0.1.0 | npm 0.1.12 (publish.yml auto-bumps past `package.json` 0.1.0) | user-owned config, shared prompt, tool-owned manifest, generated `dist` / `.research-publisher` / `build-reports` | Product's own `upgrade` | `node bin/research-publisher.js verify --json` (built in scratch) → passed. Warns `declared-dependency` | The product itself | Used (`src/`, `bin/`) |

## 1. ROS: the 3.0.3 / 3.1.1 split, explained from evidence

1. **The launcher picks the executing version.** `./ros` calls `tools/ros_fs_launcher.mjs`, whose `rosVersion()` reads **`ros.json` `.rosVersion`** and nothing else. It then downloads `ros-fs-<rid>` from `github.com/kemiller2002/repository-operating-system/releases/download/v<ver>` into `~/.cache/ros-fs/<ver>/<rid>`. `ros.json` pins `"rosVersion": "3.0.3"`, and the cache holds only `~/.cache/ros-fs/3.0.3`. `./ros --version` prints `ros-fs 3.0.3`.
2. **The manifest moved but the pin and launcher did not.**
   - `git log` on `.echelon/ros.json` shows 38bc96d (→3.0.3, 2026-09-17), then 9d6ba1d "chore: upgrade Echelon engineering capabilities" (→3.1.0), then eddafa2 "chore: reconcile Echelon capability manifests" (→3.1.1). The last two are co-authored by "Echelon Upgrade Bot".
   - Neither commit touched `ros.json`'s `rosVersion` or `tools/ros_fs_launcher.mjs`. `ros.json` is **shared** ownership, so upgrade preserves it.
   - Praxis `docs/upgrading.md` says: "`npx --package=...@<version> ros upgrade` does the same thing for a single run without changing the pin."
3. **Upstream fixed this in 3.1.3, and Tekmerion stopped at 3.1.1.** Praxis README "New in 3.1.3": "Launcher version authority: installed repositories now launch the version recorded in `.echelon/ros.json`; `ros.json.rosVersion` is only the legacy fallback."
   - Tekmerion's launcher sha256 is `dea9a573…`. That equals the upstream starter launcher at v3.0.3 and v3.1.1.
   - v3.5.0/HEAD has `416b34f9…`, which reads `.echelon/ros.json.installedVersion` first (diff v3.1.1..v3.5.0).
   - Folio and Forma already carry the `416b…` launcher with rosVersion 3.1.4.
4. **Result.** The 3.0.3 CLI reads an installation recorded by 3.1.1. So `status` reports `cliVersion 3.0.3`, `installedVersion 3.1.1`, `state upgrade-required`, `upgradeAvailable "3.0.3"`, and `doctor` says "Run 'ros upgrade'".
5. **That remedy is a downgrade and would break things.** `./ros upgrade --dry-run --json` (scratch) plans two changes:
   - `update-managed-file AGENTS.md`. The 3.0.3 CLI treats AGENTS.md as tool-owned, but since 3.1.1 it is **shared** and carries the Visual Engineering and Communication Engineering regions.
   - Rewrite `.echelon/ros.json` to 3.0.3 with 82 artifacts, dropping the six 3.1.x artifacts (`docs/ordo-observation.md`, five `schemas/ros-*.schema.json`).
   - No downgrade guard fired, and `upgrade --check` exits 3.
6. **The correct legal path is 3.1.4.** `npx --package=@echelon-foundry/repository-operating-system@3.1.4 ros upgrade --dry-run --json` (scratch) gives:
   - `conflicts: []`, `migrations: []`.
   - Changes: update `tools/ros_fs_launcher.mjs` (→`416b…`), update `docs/work-protocol.md`, and record 3.1.4 in `.echelon/ros.json`.
   - `.github/workflows/ros-validation.yml` becomes `shared`. AGENTS.md stays `shared` and is preserved.
   - Assets for v3.1.4 exist: `checksums.txt` returns HTTP 200.
7. **Validation state.** `./ros status --json` shows `validation: passed`, 0 findings, 7 work items. `verify` returns valid:true with no failures. My hash check found all 88 `.echelon/ros.json` artifacts matching. `.ros/installation.json` (legacy 1.2.1 snapshot, 76 files) is kept as migration history, per Praxis `docs/upgrading.md` ("legacy snapshot is left in place").
8. `tools/ros_cli.mjs` (the 1085-line Node CLI from the 1.2.1 install, c55c183) is not in the `.echelon/ros.json` managed set. It is still the target of SDE-STRUCT-001.

## 2. Praxis

- `https://github.com/kemiller2002/repository-operating-system` and `…/praxis` resolve to the same repository (identical HEAD `dc2cca5`, 2026-09-29, "Merge pull request #102: durable work checkpoints…"). README title: "Praxis — Repository Operating System (ROS) compatibility name: `@echelon-foundry/repository-operating-system`".
- **Yes, a `praxis` CLI exists.**
  - README "New in 3.2.0": "`praxis` is the preferred native command while `ros` remains supported". Also: "Echelon bootstrap: `echelon setup`, `install`, `upgrade`, `doctor`", and "Repository toolchain pinning: `.echelon/toolchain.json`".
  - Install command: `curl -fsSL https://raw.githubusercontent.com/kemiller2002/praxis/main/scripts/install-native.sh | sh`, then `praxis init|status|verify`.
  - `docs/native-installation.md`: "Praxis is the product name for the repository operating system previously exposed as ROS … Existing repository-local ROS installations remain compatible". The npm package "remains supported as a compatibility distribution channel".
- **Migration from ROS.** There is no separate ROS→Praxis migration. Same manifest `.echelon/ros.json`, same `tool: "ros"`, configuration version 1. `praxis upgrade` is `ros upgrade` (the native binary reports `ros-fs 3.5.0`).
- Tags: v3.0.0 … v3.1.4, then v3.3.0, v3.4.0, v3.5.0 (no v3.2.0 tag). Release assets:
  - v3.0.3, v3.1.1 and v3.1.4 have `checksums.txt` + `ros-fs-linux-x64` (200).
  - v3.3.0, v3.4.0 and v3.5.0 have **only** `praxis-<rid>.tar.gz` + `native-checksums.txt`. `ros-fs-linux-x64` and `checksums.txt` return 404.
- npm publishing stopped at 3.1.4 (`npm view … time`). `package.json` on main says 3.5.0.
- **Native 3.5.0 run (scratch).**
  - `praxis status/verify --json`: valid, `upgrade-required`, upgradeAvailable 3.5.0.
  - `praxis upgrade --dry-run --json`: 0 conflicts. Creates `.echelon/toolchain.json` (shared), 4 tool-owned docs and 2 `praxis-remote-*` schemas. Updates the launcher, templates, schemas and several docs. Records 94 artifacts.
  - Applying it in `tek-up` showed two defects. The seeded `.echelon/toolchain.json` pins `"praxis": "3.4.0"` (not 3.5.0) and `"ordo": "1.4.0"`. And after the upgrade `./ros --version` **fails**: "failed to obtain the linux-x64 binary for version 3.5.0: …/repository-operating-system/releases/download/v3.5.0/checksums.txt failed: 404". The upgraded launcher reads 3.5.0 from `.echelon/ros.json`, but no ros-fs asset exists for 3.3.0+.
- `echelon inventory --json` (scratch) lists: native ordo 1.4.0 and praxis 3.5.0; repo components communication-engineering 1.0.0, limen 0.6.1, research-publisher 0.1.0, ros 3.1.1, sde 1.3.0, visual-engineering 1.0.0; npmPackages [].
- `echelon doctor --json` returns `warning` with the single finding ECHELON-DOC-030 "No .echelon/toolchain.json is present". It does **not** flag the launcher/manifest version mismatch.

## 3. Ordo / SDE

- `.echelon/sde.json`: package `@echelon-foundry/sde`, 1.3.0, config v2, `managedArtifacts [".sde", ".echelon/sde.json"]`.
- `.sde/VERSION` 1.3.0. `.sde/MANIFEST.json` gives sdeVersion 1.3.0, method 0.2, sourceRevision `5e686d2…`. All 19 file hashes match.
- Upstream `kemiller2002/ordo` README: "prefer the native GitHub Release distribution … `ordo init|status|verify` … historical `sde` command remains an alias, and npm remains a supported compatibility distribution". `distribution/package.json` is `@echelon-foundry/sde` 1.4.0. Tags: v0.5.0, v1.4.0. Release v1.4.0 has `ordo-linux-x64.tar.gz` + `native-checksums.txt` (200). npm `latest` is 1.3.0.
- `docs/upgrading.md` (ordo): payload version and configuration version move independently. Migration 1→2 = `0001-echelon-installation-record`.
- Commands run:
  - `npx @echelon-foundry/sde@1.3.0 verify --json`: passed, no problems. Structural SDE-STRUCT-001 "strong-review" on `tools/ros_cli.mjs`.
  - `status --json`: installed, `upgradeAvailable: null` (1.3.0 is npm-latest).
  - Native `ordo` 1.4.0 `status`: `upgrade-required`.
  - `upgrade --dry-run --json`: replace payload (19 files) and write the record. No refusal.
- Usage: nothing in `src/`, `site/`, `tests/` references `.sde`.

## 4. Limen

- `.echelon/limen.json`: package `@echelon-foundry/typescript-wasm-kernel`, 0.6.1. `limen-verify.yml` is tool-owned and `limen.config.json` is user-owned. Both hashes match.
- `limen.config.json` has `boundary.engine: []` and `kernel: []`.
- Upstream `kemiller2002/limen` `package.json` is 0.6.2 (bin `limen`, `typescript-wasm-kernel`). CHANGELOG `[0.6.2] — 2026-09-21`: "Language-aware browser capability detection" (F#/C# false-positive fix). `[Unreleased]` covers the repo rename and federation.
- `.github/workflows/limen-verify.yml` runs **unpinned** `npx --yes @echelon-foundry/typescript-wasm-kernel verify --strict`, which resolves to 0.6.2. Locally, 0.6.2 `verify --strict --json` returns exit 3, LIMEN011 "This repository was initialized by version 0.6.1; the CLI running is 0.6.2", remedy `npx @echelon-foundry/typescript-wasm-kernel upgrade`. This matches the "Limen verify" failure on main runs 94 and 95 (GitHub Actions API).
- 0.6.2 `status --json`: `upgrade-required`, availableVersion 0.6.2.
- Usage: no Limen/WASM code in src/site/tests. Installed only.

## 5. Aegis

- Upstream `kemiller2002/aegis`: `src/Aegis.Core/Aegis.Core.fsproj` has PackageId `EchelonFoundry.Aegis.Core`, Version 1.0.0, net8.0. Also `EchelonFoundry.Aegis.Integration.GitHub` and `.Store.GitHub` 1.0.0.
- NuGet flatcontainer lists `1.0.0` for Core and Integration.GitHub. Publishing is by tag `aegis-v*` (`publish-nuget.yml`), but `git ls-remote --tags` shows no tags.
- Tekmerion: no occurrence of `Aegis` or `EchelonFoundry` in src/site/tests/scripts/sln/props. The only PackageReferences are Microsoft.NET.Test.Sdk, xunit and xunit.runner.visualstudio.
- Doc 18 §2.1 requires it: "Every .NET/F# host that owns an operational boundary MUST reference and use `EchelonFoundry.Aegis.Core`". The F# lifecycle CLI (`src/ResearchPublisher.Lifecycle.*`, net8.0) does filesystem and parsing I/O, so this is an open requirement gap.

## 6. Forma

- Upstream `kemiller2002/forma`: `package.json` `@echelon-foundry/design-system` 0.3.0, exports `./tokens.css`, `./components.css`, `./all.css`, `./patterns/*`, …
- npm `latest` is 0.2.0 (2026-09-23), plus `main` 0.2.0-main.167.1. Tags v0.1.0, v0.2.0. `docs/CONSUMING-FORMA.md` and `docs/AEGIS-INTEGRATION.md` exist.
- Tekmerion: `site/package.json` has no dependencies. No `design-system` or `<ef-` usage.
- Applicable per doc 18 §3 "required for interactive browser UI". Whether the Astro research site counts as *interactive* UI is a judgement call. Doc 18 §4 says "ordinary static web pages remain semantic HTML plus Forma where interactive".

## 7. Visual Engineering

- Manifest: 1.0.0, config v3, context 1.0.0. Ownership: `AGENT-INSTRUCTIONS`, `UI-*` tool-owned. `RESEARCH-INDEX`, `context.json`, `sources.json` generated. `.gitignore`, `AGENTS.md` and its config shared.
- My plain file-hash check shows the shared `.gitignore`/`AGENTS.md` hashes differ, which is expected because other tools edited those shared files. The VE CLI reports "no tool maintained content was modified locally".
- `npx @echelon-foundry/visual-engineering@1.0.0 status|verify|doctor --json`: all pass. Strict-only checks `up-to-date` and `version-compatibility` pass. Doctor says "The installation matches this release".
- Upstream: `npm/package.json` 1.0.0. CHANGELOG top "## 1.0.0". Tags include `visual-engineering-v1.0.0`. npm `latest` 1.0.0. So it is **current**.
- Oddities:
  - `.gitignore` has a VE-managed block ignoring `.visual-engineering/`, yet the eight files are tracked (`git ls-files`).
  - `.visual-engineering/release.json` (`ui-context-v0.1.0`, 85 docs) is tracked but not in the VE manifest and not in `context.json`. It is a stale pre-1.0 leftover.
- Usage: agent context only (the AGENTS.md region). `tests/unit/normalize.test.mjs` only mentions a fixture path.

## 8. Communication Engineering

- Manifest: 1.0.0, config v1. Five tool-owned files plus `AGENTS.md#communication-engineering-region` (`shared-region`).
- Upstream `package.json` 1.0.0, bin `communication-engineering`. **npm `latest` 0.1.0** (published 2026-08-15, only version). Upstream `docs/lifecycle-installation.md`: "Until an npm release is configured, a consuming repository may execute a pinned GitHub revision … `npm exec --package=github:kemiller2002/communication-engineering#<commit-sha>` … Pin the commit."
- The installed commit is not recorded in `.echelon/communication-engineering.json`. **Unknown** which upstream SHA installed it; 9d6ba1d's bot commit does not say.
- Upstream HEAD `bin/communication-engineering.mjs status --json` → installed 1.0.0 = cli 1.0.0. `verify --strict --json` → ok.

## 9. Folio

- Upstream `kemiller2002/folio` `package.json` `@echelon-foundry/print-components` 0.3.0, `publishConfig.access public`. **npm returns 404**. No git tags.
- README "Installed engineering foundation" lists ROS 3.1.4, SDE 1.3.0, VE 1.0.0, CE 1.0.0, Limen 0.6.2. That is a useful reference baseline.
- Tekmerion: no print/PDF surface found and no references. Doc 18 §4.10: "If no printable/PDF/paginated output exists in a release, Folio need not be installed". There is also no pinned release to install (doc 18 §1 forbids floating or branch dependencies).

## 10. Research Publisher (product manifest)

- `.echelon/research-publisher.json` (`echelon.tool-installation/1`): 0.1.0, config v2. Records config (user-owned), marking prompt (shared), itself (tool-owned) and generated outputs, plus 7 managed scripts.
- In scratch, `node scripts/build-lifecycle.mjs` built `runtimes/linux-x64`. Then `research-publisher --version` → 0.1.0 and `status --json` → installed, config valid.
- `verify --json`: passed, one warning `declared-dependency` ("@echelon-foundry/research-publisher is not listed in package.json").
- The npm `latest` of 0.1.12 comes from `publish.yml` "Select the next npm version" auto-bumping past local `package.json` 0.1.0. The version in the repo lags what is published.
- Still branded "Research Publisher": `ros.json` `name`/`project`, `package.json` repository URL `kemiller2002/research-publisher`, `.ros/installation.json` project. `@echelon-foundry/tekmerion` does not exist on npm.

## 11. Unmerged branches: Echelon manifest changes (`git diff main...origin/<b>`)

- **`origin/work/gh-11-readiness`** (2 ahead / 2 behind; `8db64fe` "Converge application readiness lifecycle").
  - Changes: `.echelon/ros.json` 3.1.1→3.1.4 (`ros-validation.yml` → shared, new hashes for `docs/work-protocol.md` and `tools/ros_fs_launcher.mjs` `416b…`), `.echelon/limen.json` 0.6.1→0.6.2, and a new `readiness-upgrade.yml` (pinned ROS 3.1.4 / SDE 1.3.0 / Limen 0.6.2 convergence).
  - **The branch is internally inconsistent.** The commit step's `git add` list omits `tools/` and `docs/`, so the manifest records new hashes while the files are still the old bytes (`dea9…`, `268e…`).
  - Verified in a scratch clone: both `./ros verify` (3.0.3) and ROS 3.1.4 `verify` return **valid:false, state invalid**, `managed-artifact-modified` for both files.
  - `ros.json` rosVersion stays 3.0.3. **Do not merge as-is.**
- **`origin/chore/echelon-capabilities-2026-09-21-v2`** (5 ahead / 6 behind). Only `.echelon/ros.json` 3.1.0→3.1.1 (AGENTS.md → shared, `.gitignore` hash). This is the same content as main's eddafa2 (squash-merged). Superseded, no action.
- **`origin/work/gh-7-semantic-projection-vnext-impl`** (6 ahead / 27 behind; merge-base 83aae68).
  - Echelon changes: ROS 3.0.3→3.1.0, SDE 1.2.0→1.3.0 payload, Limen →0.6.1, VE 1.0.0 install. It also **bumps `ros.json` rosVersion 3.0.3→3.1.0** (plus JSON reformatting), which main never did.
  - It has no Communication Engineering and is behind main (ROS 3.1.1).
  - Merging would conflict with or regress main's manifests. The Echelon part is superseded, except that it shows the rosVersion pin is supposed to move with upgrades.

## Gaps to record against owning systems

- **G1 (Praxis) — native releases break the repo-local launcher.** From v3.3.0 on, releases publish only `praxis-<rid>.tar.gz` + `native-checksums.txt`. The shipped starter launcher (`starter/greenfield/tools/ros_fs_launcher.mjs`, even at v3.5.0) still downloads `ros-fs-<rid>` + `checksums.txt` from `repository-operating-system/releases/download/v<ver>`. Any repo upgraded to ≥3.3.0 gets a `./ros` that fails with 404. Reproduced in `scratchpad/tek-up`. CI `ros-validation.yml` (`./ros registry check`, `./ros validate`) would break.
- **G2 (Praxis) — seeded toolchain pin disagrees with the CLI.** `praxis upgrade` 3.5.0 seeds `.echelon/toolchain.json` with `"praxis": "3.4.0"` while recording `installedVersion 3.5.0`. It also writes a native toolchain pin into repos whose CI uses npm/`./ros`.
- **G3 (Praxis) — npm channel stale.** npm stopped at 3.1.4. `package.json` is 3.5.0, and PACKAGE-USAGE notes the trusted-publisher config must be moved to `kemiller2002/praxis`. The "compatibility distribution channel" is therefore not current.
- **G4 (ROS 3.0.3 CLI) — silent downgrade.** No downgrade guard: `upgrade` with CLI < installedVersion plans a downgrade, would overwrite a shared AGENTS.md, and drops artifacts. `doctor` recommends it (`upgradeAvailable: "3.0.3"`). Fixed indirectly by the 3.1.3 launcher, but older CLIs and launchers remain in the field.
- **G5 (Echelon Upgrade Bot / rollout workflow) — incomplete commits.**
  - 9d6ba1d/eddafa2 moved `.echelon/ros.json` to 3.1.x without moving `ros.json.rosVersion` or the launcher.
  - The gh-11 `readiness-upgrade.yml` commit step omits `tools/` and `docs/`, which produces an invalid installation.
- **G6 (echelon doctor).** Does not detect "repo launcher runs version X but `.echelon/ros.json` records Y".
- **G7 (Limen) — unpinned CI workflow.** The tool-owned `limen-verify.yml` runs unpinned `npx … verify --strict`, so every Limen release turns consumer CI red until they upgrade. That also contradicts doc 18 §1's "no floating versions". Tag v0.6.2 is missing, although npm and the CHANGELOG have 0.6.2.
- **G8 (Communication Engineering).** 1.0.0 is not on npm (latest 0.1.0). The installation manifest does not record the source commit, so the installed revision cannot be reproduced.
- **G9 (Folio).** `@echelon-foundry/print-components` has no published release or tag, so no pinned artifact exists for doc 18 §4.1.
- **G10 (Forma).** `package.json` 0.3.0 is unpublished (npm 0.2.0). Consumers can pin only 0.2.0.
- **G11 (Aegis).** NuGet 1.0.0 exists but there are no `aegis-v*` git tags, so provenance from release to source is not traceable via tags.
- **G12 (Visual Engineering, minor).** Its managed `.gitignore` block ignores `.visual-engineering/` while the repo tracks those files. The stale `release.json` is not cleaned up by the lifecycle.
- **Tekmerion-local (not an upstream gap).**
  - Aegis is required by doc 18 §2 but absent.
  - `research-publisher` naming and `package.json` version lag.
  - `tools/ros_cli.mjs` is an unmanaged 1085-line legacy file (SDE-STRUCT-001).
  - The CI "Validate Research" workflow also fails on main (run 72). The cause was not investigated, so it is unknown.

## Recommended legal upgrade sequence (recommendations only; nothing applied)

1. **Do not run `./ros upgrade`** (3.0.3 CLI, downgrade). Do not merge `origin/work/gh-11-readiness` as-is. Treat `chore/echelon-capabilities-2026-09-21-v2` as superseded, and don't take gh-7-impl's Echelon changes.
2. **ROS → 3.1.4 (npm, last release with ros-fs assets).**
   - Run `npx --yes --package=@echelon-foundry/repository-operating-system@3.1.4 ros upgrade --dry-run --json` and review it. Expected: launcher, `docs/work-protocol.md`, manifest; 0 conflicts.
   - Then apply the same command without `--dry-run`.
   - Commit **all** changed files, including `tools/ros_fs_launcher.mjs` and `docs/`.
   - Optionally set `ros.json.rosVersion` to 3.1.4. It is the shared legacy fallback, so the new launcher ignores it once `.echelon/ros.json` exists, but keeping it aligned avoids confusion.
   - Verify with `./ros --version` (expect 3.1.4), `./ros verify --json`, and `./ros status --json` (expect `installed`).
3. **Limen → 0.6.2.** `npx --yes @echelon-foundry/typescript-wasm-kernel@0.6.2 upgrade --dry-run`, then `upgrade`, then `verify --strict`. Consider pinning `@0.6.2` in `limen-verify.yml`. Once edited, that file is no longer rewritten by `limen upgrade`, per its header.
4. **SDE stays at 1.3.0 on npm** (current there). Move to Ordo 1.4.0 only together with the native toolchain (step 6): `ordo upgrade --dry-run --json`, then `ordo upgrade`, then `ordo verify --json`.
5. **VE 1.0.0 and CE 1.0.0 are current.**
   - VE: no action. Optionally remove the stale `.visual-engineering/release.json` after confirming with the VE owner.
   - CE: until npm has 1.0.0, pin the GitHub commit when upgrading, and record that SHA.
6. **Praxis native (≥3.3.0) waits for G1 and G2.** Do not upgrade the repo installation past 3.1.4 until Praxis ships a launcher that works with native assets, or `ros-fs` assets on native releases. At that point: install the native toolchain, commit a correct `.echelon/toolchain.json`, run `praxis upgrade --dry-run --json` → `praxis upgrade` → `praxis verify`, and switch CI to the documented `praxis-setup` action.
7. **Doc 18 foundations.**
   - Aegis: add a pinned `EchelonFoundry.Aegis.Core` 1.0.0 PackageReference to the F# lifecycle host and route filesystem and parsing boundaries through it, with tests.
   - Forma: decide explicitly (with a recorded rationale) whether the Astro site has interactive UI. If yes, pin `@echelon-foundry/design-system@0.2.0` exactly.
   - Folio: record "not applicable until printable/PDF output exists", and in any case no pinned release exists (G9).
