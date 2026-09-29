---
id: DF-TEK-2026-0004
title: Staged Research Publisher → Tekmerion migration
document_type: decision-record
status: accepted
version: "1.0"
created: 2026-09-29
updated: 2026-09-29
work_item: GH-15
related_documents:
  - research/decisions/DF-TEK-2026-0001--product-identity-and-requirements-baseline.md
  - docs/requirements/open-questions.md
  - docs/upgrading.md
decided_by: Claude (Anthropic, claude-code runtime) executing GH-15; reversible
---

# DF-TEK-2026-0004 — Staged identity migration

## Context

DF-TEK-2026-0001 fixed the naming contract. The package publishes to npm on
every push to `main` through trusted publishing, so a package rename is an
outward-facing release event, not a repository edit.

## Decision

Stage the migration:

1. **Now (configuration version 3, lifecycle migration 2→3):**
   - The lifecycle tool identity becomes `tekmerion`, and its record moves to
     `.echelon/tekmerion.json`.
   - Existing installations are detected by `.echelon/research-publisher.json`
     and migrated through the ordinary plan/execute path. The migration is
     dry-runnable, idempotent and conflict-aware. It is recoverable because the
     new record is written before the legacy one is removed, and an interrupted
     run resumes from the new record.
   - No repository content changes, so shared and user-owned files are
     untouched.
   - A manifest at the legacy path that belongs to another tool is refused.
   - This repository was migrated with the same command.
2. **Now (additive):**
   - A `tekmerion` executable alias.
   - Package repository, homepage and bugs URLs pointing at
     `kemiller2002/tekmerion`.
   - Current-product documentation and the charter.
3. **Later, one release, owner-gated (OQ-TEK-014):**
   - The npm package name.
   - `Identity.PackageName`/`ExecutableName`.
   - The `tekmerion:*` scripts (configuration version 4) with `research:*`
     kept as deprecated aliases.
   - The deprecation notice.

## Alternatives rejected

- **Rename everything, including the npm package, now.** Rejected: the first
  publish under a new name needs owner-side trusted-publisher setup. Merging
  would either fail the publish workflow or publish without consumer
  communication.
- **Hand-edit this repository's `.echelon/research-publisher.json`.**
  Rejected: that would falsify tool state (TEK-MIG-003). The migration command
  was used instead.
- **Delete the legacy record first, then write the new one.** Rejected: there
  would be a window with no installation record.

## Evidence

- `IdentityMigrationTests` (7): detection, dry run with no disk change, content
  preservation, idempotency, interrupted resume, locally edited files
  preserved, foreign manifest refused.
- `node bin/research-publisher.js upgrade` on this repository applied 3
  changes, and a second run reported no changes needed.
