# Changelog

Releases of `@echelon-foundry/research-publisher` (product: Tekmerion). The
`Publish npm Package` workflow publishes on every push to `main`: the version in
`package.json` when it is ahead of npm, otherwise the next patch (see
[`docs/releasing.md`](docs/releasing.md)). Patch releases published that way are
not listed individually.

## 0.2.0 — unreleased

### Added

- **Echelon theme support in `tekmerion ingest`** (TEK-ECH-006). Three
  optional, backward-compatible flags layer a pinned theme over Forma:
  - `--theme DIR` copies a theme's compiled directory (for example
    `node_modules/@echelon-foundry/echelon-theme/dist`) byte for byte to
    `assets/theme/`, so its relative `url()`s, such as the self-hosted fonts,
    resolve. Symbolic links, hidden and system entries are skipped. Requires
    `--forma`.
  - `--theme-css CSS` (repeatable) links a stylesheet from that directory after
    Forma's `all.css`, in the order given.
  - `--brand NAME` sets `data-forma-brand` on every page's `<html>`.

  Refused combinations are argument errors (exit `2`) and leave the previous
  publication untouched. Without the new flags the output is byte-identical to
  0.1.x, and pages stay readable without any stylesheet (TEK-ECH-003).
- Tekmerion's own slice publishes with **Echelon theme 1.1.0**
  (`echelon.css`, `echelon-fonts.css`, `echelon-app.css`, brand `echelon`). The
  theme is pinned by release-asset tarball and declared in `conditor.json`
  through Conditor's opt-in.

### Changed

- An unreadable `--forma` stylesheet is now reported with the Aegis fault
  `TEKMERION.ASSETS.READ_FAILED` (was `TEKMERION.SOURCE.READ_FAILED`). The exit
  code is unchanged (`6`).
- `Tekmerion.Core.SiteOptions` replaces `FormaStylesheet: string option` with
  `Stylesheets: string list` and `Brand: string option`. The F# engine is not
  part of the npm package, so no package consumer is affected.
- Requirements baseline REQ-TEK 1.2.0 adds TEK-ECH-006.

### Notes

- The F# Tekmerion engine (`src/Tekmerion.Cli`) is still not shipped in the npm
  package; the new flags are available from a source build
  (`dotnet run --project src/Tekmerion.Cli -- ingest …`).
