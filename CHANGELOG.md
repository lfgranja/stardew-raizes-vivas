# Changelog

All notable changes to **Living Roots** (Stardew Valley SMAPI mod) should be documented in this file.

> **Release status:** no public releases/tags published yet. Internal mod version in `manifest.json` is currently `0.1.0`; repository HEAD may include work beyond that manifest label. Any public `0.2.0-beta` release must reconcile version/provenance and state evidence boundaries honestly (`machine-pass` host-only ≠ human-verified; 0 D1; 0 SC-004).

## [Unreleased / 0.2.0-beta draft]

### Added
- Heirloom seeds pipeline initial slice (Crioulo Parsnip template): data/content registration and planting bonus path (see spec 020). *Gate 3 machine-pass (host-only) covers planting only; harvest/seed-save/natural acquisition/D1 pending.*
- Joja agrochemical tradeoff MVP (spec 017): bounded, recoverable soil-health tradeoff with explicit feedback. *SC-004 comprehension protocol not executed.*
- Harmony harvest seam and related harvest-quality/growth/soil feedback integrations as implemented across recent specs (see `specs/` and release records).

### Changed
- [Reconcile and list user-facing changes since last public/internal baseline before release.]

### Fixed
- [List notable fixes with issue/PR/spec refs as appropriate.]

### Verification notes (draft — update for tagged release commit)
- Deterministic snapshot 2026-11-10T02:40Z (`c07fbab`): `release-gate.sh` ALL STEPS PASSED; 1997 passed / 0 failed / 4 skipped (2001); smoke 50/51.
- Deterministic gate (RC-prep evidence): **2026-11-14T21:22:43Z (UTC)** commit **`27ed81b5d49773cd679a04a43142e267359bd13c`** — `release-gate.sh` **ALL STEPS PASSED** (format, locked restore, build Release, tests, smoke launch-free **51 files / 50 passed / 0 failed / 1 skipped**, docs markers, protected paths). Re-run on the exact tagged RC commit before publishing.
- End-to-end (headless, host-only): 7 scenarios `machine-pass` (004,005,013,014,015,017,020 plant-only). No human attestation, no live D1 farmhand attestation, no SC-004.
- R4/R7 trail (prep, not published): `.specify/assessments/0.2.0-beta/decision.md`, `.specify/validations/0.2.0-beta/validate.md`, `.specify/releases/0.2.0-beta/release.md`. Replace/update with exact commit hash, UTC, and evidence for the actual release candidate/tag.

### Known limitations
- Beta: back up saves; multiplayer (D1 live convergence) not attested; Harmony may interact with other mods; heirloom flows beyond planting pending evidence.

## [0.1.0] - internal (unreleased)

- Initial internal baseline (soil health core, compost/mulch/growth/quality foundations and earlier specs as recorded in repository). No public tag/release.
