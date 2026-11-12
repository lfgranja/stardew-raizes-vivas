# Living Roots v0.2.0-beta (draft)

> **DRAFT — not published.** Review and adjust before tagging/releasing. Replace placeholders in `[brackets]`.
> **Honesty boundary:** this draft must not claim human verification, live multiplayer (D1) attestation, or SC-004 completion unless separately evidenced.

## Summary

Living Roots (Raízes Vivas) is an agroecological Stardew Valley mod focused on **per-tile soil health (0–100)** and a coherent loop of compost, mulch, growth/quality effects, and bounded tradeoffs — designed with host-authoritative multiplayer in mind.

**This is a beta prerelease** intended to gather real play feedback and compatibility reports.

## Highlights (candidate 0.2.0-beta)

- **Per-tile soil health** with persistence, decay, and in-game feedback/visualization.
- **Compost loop:** composter/compost application affecting soil health.
- **Mulch (soil cover):** mulch mechanics integrated with soil behavior.
- **Growth & harvest outcomes:** soil health influences crop growth and harvest quality/extra outcomes (implemented tiers/seams per current codebase).
- **Harmony harvest seam:** targeted patching for harvest-related behavior with fallbacks/safeguards as implemented.
- **Joja agrochemical tradeoff (MVP):** bounded, recoverable tradeoff with explicit feedback (SC-004 comprehension protocol not yet executed).
- **Heirloom seeds pipeline (initial slice — Crioulo Parsnip):** planting bonus path covered by headless end-to-end scenario; **harvest/seed-save/natural acquisition flows and D1 are not yet Gate 3-covered**.

## Requirements & compatibility

- **Stardew Valley:** 1.6.14+ (tested baseline target: **1.6.15** — confirm before release)
- **SMAPI:** 4.4.0+ minimum (manifest); tested baseline target: **SMAPI 4.5.2** — confirm before release
- **Platforms:** PC (Windows/macOS/Linux/Steam Deck) where SMAPI runs. Consoles/mobile not supported.
- **Mod type:** C# SMAPI mod using Harmony (`EnableHarmony`).
- **Dependencies:** none declared (confirm).

## Installation

1. Install [SMAPI](https://smapi.io/).
2. Download the release zip and extract it so you have `Mods/LivingRoots/manifest.json` (single mod folder).
3. Launch Stardew Valley through SMAPI.

## Multiplayer notes (important)

- Living Roots is designed with **host-authoritative, farm-scoped** state in mind for relevant flows.
- **Live multiplayer convergence (D1, real farmhand session) has not been attested.** Multiplayer behavior should be treated as **experimental** in this beta.
- Recommended for beta testing: all players run the **same mod version**; back up saves before use; report issues with SMAPI logs and clear host/farmhand context.

## Verification status (read before reporting as “verified”)

- **Automated tests (snapshot 2026-11-10, HEAD `c07fbab`):** 1997 passed / 0 failed / 4 skipped; `release-gate.sh` **ALL STEPS PASSED** (format, locked restore, build Release, tests, smoke 50/51, docs markers, protected paths).
- **End-to-end (real game, headless, host-only):** 7 scenarios `machine-pass` (compost, growth, mulch, harvest quality, Harmony seam, Joja tradeoff, heirloom planting-only) with verdict pass/honesty R.
- **Not yet:** human-verified playthroughs, **D1** live farmhand attestation, **SC-004** (017) execution, and 020 Gate 3 beyond planting (harvest/seed-save/natural acquisition). `machine-pass` ≠ `verified`.

> If the release-gate/test snapshot differs for the tagged commit, update this section with the exact commit hash, date/time (UTC), and results.

## Known limitations / caveats

- Beta software: expect rough edges; backup saves.
- Harmony patches can conflict with other mods that alter related game behavior.
- Heirloom pipeline is an initial slice (planting covered in headless E2E); additional flows pending evidence/coverage.
- Multiplayer: no live D1 attestation yet.
- Visual/UX issues that are not capturable by headless signals may remain; in-game confirmation is still needed for some reported cases.

## Feedback & bug reports

Please include:
- SMAPI log link: https://smapi.io/log (upload your log and share the URL)
- What you expected vs what happened, and **steps to reproduce**
- Single-player or multiplayer (**host/farmhand**), save context, day/season if relevant
- Stardew Valley + SMAPI + Living Roots versions and mod list (or full log)

Report at: `[GitHub Issues URL]` / `[Nexus Bugs tab URL]`

## AI disclosure (draft — complete with facts before publishing)

> Nexus Mods requires accurate AI-use disclosure for new uploads (policy effective 2026-11-03). Confirm the latest requirements at publish time.

- **Category:** `[None / AI-Assisted / AI-Generated Content / AI Media]` *(select accurately)*
- **Tools/models (if any):** `[list]`
- **What AI outputs remain in the final mod (if any):** `[specific files/code/assets/text]`
- **Human development/review/testing:** `[brief evidence: version control history, maintainer review/testing; reference release-gate/test results]`
- **AI-generated art/audio/media in mod or page assets:** `[none, or list explicitly]`

## Credits & license

- **Author:** lfgranja
- **License:** MIT (see repository `LICENSE`)
- **Thanks:** SMAPI/Pathoschild ecosystem and Stardew Valley modding community. `[Add any additional credits/assets/attributions]`

## Changelog (0.2.0-beta vs 0.1.0 internal)

- `[Curated, user-facing changes since 0.1.0 — include 020 heirloom slice, any fixes, and MP/sync-relevant work as applicable]`
- `[Note: HEAD/provenance must be reconciled with manifest/zip version before release]`
