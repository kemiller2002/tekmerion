---
id: DF-TEK-2026-0003
title: Static research experience — F# HTML projection, Markdig, pinned Forma, evidence-based redirects
document_type: decision-record
status: accepted
version: "1.1"
created: 2026-09-29
updated: 2026-10-06
work_item: GH-18
related_documents:
  - docs/architecture/tekmerion-experience.md
  - research/decisions/DF-TEK-2026-0002--typed-ingestion-slice.md
decided_by: Claude (Anthropic, claude-code runtime) executing GH-18; reversible
---

# DF-TEK-2026-0003 — Static research experience

## Decisions

1. **HTML is a tier-3 projection in F#** (`Html.fs`, `Site.fs`), built from the
   same corpus as the contracts. It has no template engine and no JavaScript.
   The small HTML model escapes text by construction. `Trusted` markup is
   accepted only from Markdig.
2. **Markdig 1.4.0 (pinned) with raw HTML disabled** renders Markdown bodies.
   Research bodies are real CommonMark with tables, so a hand-written
   renderer would be a new source of defects. Links are rewritten in the
   Markdig AST: resolvable repository links become site links, `http(s)`/`mailto`
   links are kept with no opener privileges, and everything else becomes
   text.
3. **Forma, consumed and not copied**: pinned exactly as a dev dependency.
   The host takes `--forma <all.css>` and emits it unchanged as an asset.
   Originally Forma 0.2.0 from npm, because 0.3.0 was unpublished (inventory
   G10). Since WI-0018 (v1.1) the pin is Forma 0.4.1, the `echelon-current`
   channel version, as its immutable GitHub release tarball. Forma 0.3.0
   added owl margins (`--ef-stack-space`) to `.ef-stack` on top of its `gap`;
   the page `main` sets `--ef-stack-space: 0` so `gap` stays the single
   spacing mechanism, and the 404 page wraps its heading in a `header` like
   every other page, because the stack now zeroes its children's margins.
4. **Relative links everywhere**, so the output works under a project Pages
   path, a custom domain, or `file://` without a base-path setting.
5. **Legacy URLs come from evidence.** The previously published
   `dist/data/research-catalog.json` maps each old URL to its source path. The
   legacy slug algorithm is not re-implemented, because doing so would
   reproduce its title-derivation defect and guess where the catalog already
   knows. Stubs use `meta refresh` plus `rel=canonical`. A collision with an
   emitted page is `LostPublishedUrl` (blocking).
6. **The project view shows only declared structure.** Its purpose comes only
   from artifacts declaring `entryPoint: true`. Tekmerion never picks one.
   There is no activity timeline.
7. **Limen is deferred, not faked.** Nothing in the slice needs non-native
   behaviour, so no JavaScript is shipped. The Limen boundary stays empty and
   the related acceptance criteria stay open (see the experience document).

## Alternatives rejected

- **Keep Astro for the new pages.** Rejected: the target core is F#
  (TEK-ARC-001), and Astro would re-introduce the Node rendering path whose
  data model caused the defects found in discovery. The legacy Astro site
  still works unchanged (TEK-ARC-006).
- **A local stylesheet for gutters and `pre` overflow.** Rejected: doc 18 §3.3
  and §3.7 put this in Forma. The gaps are recorded instead.
- **Client-side rendering from the JSON contracts.** Rejected: TEK-EXP-001
  requires pages to be readable without JavaScript.

## Reversibility

The renderer and patterns are isolated in `Site.fs`. Forma can be upgraded by
changing the pin. Redirect stubs are regenerated on every build.
