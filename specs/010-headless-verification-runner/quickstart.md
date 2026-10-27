# Quickstart: Headless Game Verification Runner

**Feature**: 010-headless-verification-runner
**Date**: 2026-10-26

This guide is the validation/run entry point for the runner. It does not duplicate implementation details; see `plan.md`, `data-model.md`, and `contracts/` for those.

## Prerequisites

- Linux host with `Xvfb`, `gh`, `curl`, `sha256sum`, `jq`, `dotnet` SDK, `flock`, `mktemp`, `realpath`.
- `STARDEW_GAME_PATH` pointing at a valid Stardew Valley 1.6.x + SMAPI 4.4.0+ install (or a cache miss will trigger acquisition).
- `BUILDDEPS_TOKEN` (fine-grained PAT, `Contents: read` + `Metadata: read`) for acquisition on cache miss, OR a pre-cached valid install (no token needed for a cache hit).
- The mod built and deployed (the runner can rebuild/deploy, or use `--skip-build`).
- `compat-matrix.json` present in the repo (version gate).

## Invocation

Local (interactive):

```bash
./scripts/verify-scenario.sh --scenario specs/010-headless-verification-runner/fixtures/004-compost-growth.json
```

CI (non-interactive, machine-readable):

```bash
./scripts/verify-scenario.sh --scenario <path> --ci
```

Common flags:

- `--scenario <path>` — scenario JSON (required).
- `--game-path <dir>` — override `STARDEW_GAME_PATH`.
- `--force-refresh` — bypass the game-file cache and re-acquire.
- `--quarantine-owner <name>` — override quarantine owner resolution.
- `--ignore-exit-code <list>` — demote exit codes to 0 (e.g., `20` for skipped).
- `--ci` — non-interactive mode: emit JUnit XML + Allure JSON, no prompts.
- `--check` — validate bundle without running (for CI gate).

## Expected outcome (pass)

1. Exit code `0` (PASS).
2. `evidence-bundle-<run-id>/` created with `manifest.json`, `claims.json`, `artifacts/`, `junit.xml`, `allure.json`.
3. All three signals present: SoilHealthState delta, Crop phase delta, SMAPI log (elapsed ms).
4. `test.md` (at `testMdPath`) updated from `partial` to `verified` with file paths to all three signals.
5. `ROADMAP.md` and `COMPLETION_SUMMARY.md` Gate 3 status updated via marker injection.
6. Original save byte-identical (SC-005).

## Validation scenarios

### VS-01 — Happy path (feature 004)

Run the runner against the 004 compost→growth scenario on a host with a cached valid install. Expect exit `0`, full evidence bundle, `test.md` promoted to `verified`.

### VS-02 — Threshold violation

Tamper the scenario threshold (e.g., expect poor soil to accelerate). Expect exit `18` (FAIL), `test.md` NOT promoted, diagnostic naming the violated threshold with observed vs. expected values.

### VS-03 — Incomplete signal set

Simulate a missing Crop-phase dump. Expect exit `19` (ERRORED) or `partial`, never auto-promoted to `pass`.

### VS-04 — Cache hit (no token)

Run with a valid cached install and no `BUILDDEPS_TOKEN`. Expect no network access, exit `0`.

### VS-05 — Cache miss (acquisition)

Delete the cached install; run with `BUILDDEPS_TOKEN`. Expect acquisition, checksum verification, extraction, then normal run.

### VS-06 — Concurrent instances

Run two instances simultaneously. Expect distinct display numbers (via `-displayfd`), distinct save copies (via per-instance `mktemp`), no collision, both complete with correct verdicts, original saves byte-identical.

### VS-07 — Version mismatch (hard-fail)

Point `STARDEW_GAME_PATH` at an out-of-range game version. Expect abort before Xvfb launch with a version-mismatch diagnostic (exit 19 / inherited code), no game launch.

### VS-08 — Flaky scenario

Simulate a transient failure on attempt 1 that passes on retry. Expect `flaky: true` metadata, exit `18` (FAIL) flagged, quarantine logic exercised after 2 flaky runs in 14 days.

### VS-09 — Quarantined scenario

Run a quarantined scenario. Expect exit `20` (NO_TESTS) with JUnit `<skipped>`, not a failure.

### VS-10 — Farmhand host-guard

Run a farmhand-tagged scenario via `debug split`. Expect the `Soil growth modifier skipped: not main player.` skip line; absence with execution evidence → host-guard breach (non-retryable).

## Fallback

On hosts where the runner cannot execute, use the manual SC-009 protocol (`VERIFICATION_PROTOCOL.md` §3, `gate3-verification-contract.md`) per FR-021. The runner and manual protocol produce equivalent evidence bundles; `verified` status is achievable via either path.