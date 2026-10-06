# Work Queue

| ID | Work | Status | Tags | Priority |
|---|---|---|---|---|
| GH-11 | Converge Research Publisher implementation baseline | active | readiness,bootstrap | high |
| GH-13 | GH-13 | complete |  |  |
| GH-14 | GH-14 | complete |  |  |
| GH-15 | GH-15 | complete |  |  |
| GH-16 | GH-16 | active |  |  |
| GH-17 | GH-17 | complete |  |  |
| GH-18 | GH-18 | active |  |  |
| GH-19 | GH-19 | complete |  |  |
| ROS-INSTALL-1-2-1 | ROS-INSTALL-1-2-1 | complete |  |  |
| WI-0001 | Install Echelon Foundry SDE 1.2.0 | active | mechanical | medium |
| WI-0002 | Upgrade ROS installation from 1.2.1 to 3.0.3 | complete |  | high |
| WI-0003 | Research Publisher vNext discovery and architecture analysis | active |  | high |
| WI-0004 | Praxis gap G1: native releases >=3.3.0 ship no ros-fs assets, so the repo launcher 404s after upgrade; blocks moving past ROS 3.1.4 (see docs/echelon/foundations.md) | captured | echelon,gh-16 | medium |
| WI-0005 | Praxis gap G2: praxis upgrade 3.5.0 seeds .echelon/toolchain.json with praxis 3.4.0 while recording 3.5.0 | captured | echelon,gh-16 | medium |
| WI-0006 | Praxis gap G4: ROS CLI older than installation plans a silent downgrade and doctor recommends it; add a downgrade guard | captured | echelon,gh-16 | medium |
| WI-0007 | Limen gap G7: tool-owned limen-verify.yml runs unpinned npx verify --strict, breaking consumer CI on every release (doc 18 section 1 forbids floating versions) | complete | echelon,gh-16 | medium |
| WI-0008 | Communication Engineering gap G8: 1.0.0 not on npm and installed commit not recorded in manifest | captured | echelon,gh-16 | medium |
| WI-0009 | Folio gap G9 / Forma gap G10: no pinned published release (Folio) / 0.3.0 unpublished (Forma) | captured | echelon,gh-16 | medium |
| WI-0010 | Tekmerion: ros.json rosVersion still 3.0.3 (legacy fallback, ignored by the 3.1.4 launcher); align only through a supported lifecycle path or owner decision | captured | echelon,gh-16 | medium |
| WI-0011 | Re-run the vNext gap analysis against the requirements draft that landed mid-discovery | active |  | high |
| WI-0012 | Tekmerion: adopt EchelonFoundry.Aegis.Core 1.0.0 at F# host boundaries (doc 18 section 2); planned with the GH-17 ingestion host | captured | echelon,gh-16 | medium |
| WI-0013 | Forma gap G13: no page shell / inline gutter primitive; Tekmerion pages touch the viewport edge (docs/architecture/tekmerion-experience.md) | captured | gh-18 | medium |
| WI-0014 | Forma gap G14: no pre overflow rule; preformatted research diagrams overflow 46px at 320px on RP-COMP-005 | captured | gh-18 | medium |
| WI-0015 | GH-18 open: Limen interactive layer (F# engine + kernel: copy stable link, in-page relationship filter); boundary currently empty | captured | gh-18 | medium |
| WI-0016 | GH-18 open: unidentified 11px horizontal overflow at 320px on the project page | captured | gh-18 | medium |
| WI-0017 | Upgrade Limen to 0.7.1 to match the echelon-current channel | complete |  | medium |
| WI-0018 | Upgrade Forma to 0.4.1 to match the echelon-current channel | complete |  | medium |
| WI-0019 | Move tekmerion to Praxis 3.7.1 (ROS -> Praxis rename) and Ordo 1.4.0 | complete | praxis, ordo, toolchain | medium |
| WI-0020 | Move tekmerion to Ordo 1.4.1 | complete | ordo, toolchain | medium |
