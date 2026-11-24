# Idea Intake: Headless Verification Runner for Gate 3

- **Slug**: headless-verification-runner
- **Created**: 2026-11-02
- **Source**: repo path `specs/010-headless-verification-runner/` (codebase pointer)
- **Type**: new-capability

## Idea (as captured)

A headless, reproducible verification runner that launches Stardew Valley via SMAPI under Xvfb, drives the Living Roots mod through a scenario (till → compost → harvest → save → reload), captures the 3 Gate 3 signals (SoilHealthState JSON delta + Crop.phase/phaseDays delta + SMAPI log), and produces a `machine-pass` verdict — automating the manual Gate 3 that is currently 0/1 runs. Scope includes a bash orchestrator (`scripts/verify-scenario.sh`), C# companion projects (`LivingRoots.Verification`, `LivingRoots.Verification.Runner`), a public mod API (`ILivingRootsModApi`), scenario JSON specs, and CI integration. Status: Draft, 0/161 tasks implemented.

## Restated

Build an automated, headless harness that runs the mod in a real (but display-less) Stardew Valley instance, executes a scripted scenario, and captures verifiable evidence for Gate 3 — replacing the manual 30-minute in-game session with a reproducible, machine-attestable run.

## Origin & Context

- **Raised by**: maintainer (lfgranja) — spec 010 authored 2026-10 (see `specs/010-headless-verification-runner/spec.md`).
- **Trigger**: Gate 3 (in-game verification) has 0 runs; the harvest-quality effects (Poor → 0, Living → boost) cannot be attested as "verified" without evidence. The headless runner is the enabler for continuous verification and for closing T047/T048/T049/T052 (spec 015 Phase 8).

## First-Glance Unknowns

- [NEEDS CLARIFICATION: whether the runner should reuse the existing `scripts/smoke-validation.sh` infrastructure or be a separate orchestrator]
- [NEEDS CLARIFICATION: how the C# companion communicates with the running game process (SMAPI mod API vs. file-based IPC)]
- [NEEDS CLARIFICATION: whether `verified` attestation remains human-only (E) or can be partially automated]
- [NEEDS CLARIFICATION: acceptance of the 161-task scope vs. a smaller MVP that only captures the 3 Gate 3 signals]
- [NEEDS CLARIFICATION: CI runner availability (self-hosted vs. GitHub-hosted with gamepath.zip)]