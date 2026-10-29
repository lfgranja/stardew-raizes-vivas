# Quickstart: Headless Game Verification Runner

**Feature**: 010-headless-verification-runner
**Date**: 2026-10-26

This guide is the validation/run entry point for the runner. It does not duplicate implementation details; see `plan.md`, `data-model.md`, and `contracts/` for those.

## Prerequisites

- Linux host with `Xvfb`, `gh`, `curl`, `sha256sum`, `jq`, `dotnet` SDK, `flock`, `mktemp`, `realpath`.
- `STARDEW_GAME_PATH` pointing at a valid Stardew Valley 1.6.x + SMAPI 4.4.0+ install (or a cache miss will trigger acquisition).
- `BUILDDEPS_TOKEN` (fine-grained PAT, `Contents: read` + `Metadata: read`) for acquisition on cache miss, OR a pre-cached valid install (no token needed for a cache hit).
- The mod built and deployed (the runner can rebuild/deploy, or use `--skip-build`).
- `compat-matrix.json` present in the repo (version gate) and `gamepath.lock.json` containing the authoritative SHA-256 for the game archive (integrity gate).
- The target `testMdPath` exists and contains exactly one `<!-- GATE3:BEGIN -->` … `<!-- GATE3:END -->` block; `ROADMAP.md` and `COMPLETION_SUMMARY.md` contain their own GATE3 blocks. Missing, duplicated, or inverted markers fail fast with exit `14` before launch (FR-016/FR-017).

## Invocation

Local (interactive):

```bash
./scripts/verify-scenario.sh --scenario specs/010-headless-verification-runner/fixtures/valid/compost-growth.scenario.json
```

CI (non-interactive, machine-readable):

```bash
./scripts/verify-scenario.sh --scenario <path> --ci
```

Common flags:

- `--scenario <path>` — a single scenario JSON **file** or a **directory** (required). A directory discovers `*.scenario.json` (fallback `*.json`) in lexicographic order and aggregates the suite per C-SCN-34; no discovered scenario exits `20`. A file runs exactly that scenario.
- `--scenario-glob <pattern>` — directory mode only: override the discovery glob (`*.scenario.json` by default, fallback `*.json`). Discovery order and suite aggregation are unchanged. Combining it with a file `--scenario` exits `14`.
- `--game-path <dir>` — override `STARDEW_GAME_PATH` for this invocation; if neither is present, exit `14` (usage). It does not change the FR-001 three-state rules: a set-but-invalid path is a **cache miss** (acquire when a token is available; otherwise exit `9`), and a valid cache is reused without network. The resolved source is recorded in the bundle.
- `--skip-build` — skip the rebuild/deploy step and use the assemblies already deployed under the game's `Mods/` folder. Deployment preflight still runs (a missing deployed mod/verification assembly exits `12`; a missing required game assembly exits `9`), and the bundle's environment record retains `buildSkipped: true` so the run never implies the tested binary was rebuilt from the current commit.
- `--milestone-save <name>` — select the read-only source milestone save folder for this run (overrides the scenario's `milestoneSave` source selection; the scenario's `environment.saveId` must still match the selected milestone manifest). The name must match the manifest's `saveName`; an unknown or mismatched name exits `14`. The source folder must remain byte-identical (FR-018/SC-005).
- `--override <dotted.key>=<value>` — repeatable; only declared environment keys (`environment.gameVersion`, `environment.smapiVersion`, `environment.modVersion`, `environment.saveId`, `environment.config.*`). Applied after schema validation, re-validated against the version matrix, and recorded in `manifest.json#/environmentOverrides`. Unknown keys, or any attempt to override `testMdPath`, `expectedSignals`, `thresholds`, `playerRole`, or `hostGuard`, exits `14`.
- `--force-refresh` — bypass the game-file cache and re-acquire.
- `--no-acquire` — disable network acquisition entirely: a valid cache proceeds without network or token; a cache miss exits `9`; combining with `--force-refresh` exits `14`.
- `--quarantine-owner <name>` — override quarantine owner resolution.
- `--ignore-exit-code <list>` — demote the listed comma-separated exit codes to 0 (e.g., `18` for an expected-failure lane, `20` for skipped). It changes only the process exit code; the recorded verdict, evidence, and JUnit/Allure result are unchanged. **It can never demote an integrity/security failure (FR-024/FR-025: checksum, archive-member, extraction, token-invalid, partial-download) or a `21` claim/honesty overreach.** In those cases the runner records `ignoreExitCodeRefused: true` with the requested list and exits with the original code.
- `--ci` — non-interactive mode: emit JUnit XML + Allure JSON, no prompts.
- `--verify-bundle <dir>` — offline validation of an existing evidence bundle (schema, digests, refusal-site liveness) without launching the game; exits `0` valid, `19` schema/digest/mandatory-evidence failure, `21` claim/honesty overreach.
- `--check-docs` — report drift between the bundle verdict and `test.md`/root-doc GATE3 blocks; exits `0` with no drift, `21` on drift, and `14` when a marker block is missing or malformed. Does not run a scenario.

## Expected outcome (pass)

1. Exit code `0` (PASS), with `flaky: false`.
2. `evidence-bundle-<run-id>/` created with `manifest.json`, the **mandatory detached `manifest.sha256`** anchor, `claims.json`, `artifacts/`, `junit.xml`, and `allure.json`; `attempts/` is present with each `attempts/<n>/attempt.json` plus its per-attempt manifest (`attempts/warmup/` additionally when the discarded warmup ran).
3. All three signals present: SoilHealthState delta, Crop phase delta, SMAPI log (elapsed ms).
4. `artifacts/anomaly-report.json` present whenever an FR-009 anomaly is recorded (in-process dump vs. save-file divergence, a missing signal, or a signal mismatch); its absence is valid only when the run recorded no anomaly.
5. `test.md` (at `testMdPath`) updated from `partial` to **`machine-pass`** with file paths to all three signals, replacing only the `<!-- GATE3:BEGIN -->` … `<!-- GATE3:END -->` block.
6. `ROADMAP.md` and `COMPLETION_SUMMARY.md` Gate 3 status mirrored as `machine-pass` via marker injection.
7. Original save byte-identical (SC-005); the source milestone save selected by `--milestone-save` is also unchanged (FR-018).

### Status vocabulary and claim root

The runner recognizes exactly three statuses:

| Status | Produced by | Condition |
|--------|-------------|-----------|
| `partial` | runner | Initial state, or any `fail`/`errored`/`partial`/`skipped` result, or a flaky pass |
| `machine-pass` | runner | Clean, non-flaky `pass`; all three signals agree; honesty level ≥ R; expectedVerdict matches |
| `verified` | **human only** | A maintainer's honesty-level **E** attestation layered on a `machine-pass` bundle; the runner never assigns it |

The **claim root** is the GATE3 block in the scenario's `testMdPath` (`specs/<NNN-slug>/test.md`), backed by the bundle's `claims.json` and detached `manifest.sha256`. `ROADMAP.md` and `COMPLETION_SUMMARY.md` are mirrors of that root claim, not independent sources.

**Remediation (2026-10-27):** the helper project (`LivingRoots.Verification.Runner/`), the three-status ladder, and the claim-root rule are recorded in `research.md` § *Remediation (2026-10-27)*.

## Validation scenarios

Twelve cases are mandatory: **VS-01 through VS-10, plus VS-09b and VS-09c**. T136 in `tasks.md` must exercise all twelve against the fake host — the ten numbered cases VS-01, VS-02, VS-03, VS-04, VS-05, VS-06, VS-07, VS-08, VS-09 and VS-10 plus the two additional cases VS-09b and VS-09c; a run that skips any of them is incomplete. The table is the index; each subsection states the observation.

| ID | Case | Expected exit | Promotion |
|----|------|---------------|-----------|
| VS-01 | Happy path (feature 004) | `0` | `machine-pass` |
| VS-02 | Threshold violation | `18` | none (`partial`) |
| VS-03 | Incomplete signal set | `19` | none (`partial`) |
| VS-04 | Cache hit, no token | `0` | `machine-pass` |
| VS-05 | Cache miss, acquisition | `0` | `machine-pass` |
| VS-06 | Concurrent instances | `0` for both | `machine-pass` per instance |
| VS-07 | Version mismatch (cache miss first) | `19` with token; `9` without | none (`partial`) |
| VS-08 | Flaky scenario | `0` | none (stays `partial`, `flaky: true`) |
| VS-09 | Quarantined scenario | `20` | none (`skipped`) |
| VS-09b | Expected failure | `18`, or `0` only with `--ignore-exit-code 18` | none (`partial`) |
| VS-09c | Offline acquisition ban | `9` on cache miss; `0` on cache hit; `14` for `--no-acquire --force-refresh` | `machine-pass` only on the cache-hit branch |
| VS-10 | Farmhand host-guard | `0` when the guard holds; `18` on breach | none (always `partial`) |

### VS-01 — Happy path (feature 004)

Run the runner against the 004 compost→growth scenario on a host with a cached valid install. Expect exit `0`, the full evidence bundle (including `manifest.sha256`, `attempts/`, and `artifacts/anomaly-report.json` only if an anomaly occurred), and `test.md` promoted from `partial` to `machine-pass`.

### VS-02 — Threshold violation

Tamper the scenario threshold (e.g., expect poor soil to accelerate). Expect exit `18` (FAIL), `test.md` NOT promoted (stays `partial`), and a diagnostic in the bundle naming the violated threshold with observed vs. expected values.

### VS-03 — Incomplete signal set

Simulate a missing Crop-phase dump. Expect exit `19` with verdict `partial` (not `errored`), `artifacts/anomaly-report.json` naming the missing signal, and no auto-promotion to `machine-pass`.

### VS-04 — Cache hit (no token)

Run with a valid cached install and no `BUILDDEPS_TOKEN`. Expect no network access, exit `0`.

### VS-05 — Cache miss (acquisition)

Delete the cached install; run with `BUILDDEPS_TOKEN`. Expect acquisition, checksum verification, extraction, then normal run.

### VS-06 — Concurrent instances

Run two instances simultaneously. Expect distinct display numbers (via `-displayfd`), distinct save copies (via per-instance `mktemp`), no collision, both complete with correct verdicts, original saves byte-identical.

### VS-07 — Version mismatch (cache miss first, then hard-fail)

Point `--game-path` (or `STARDEW_GAME_PATH`) at an install whose game/SMAPI version is outside `compat-matrix.json`. **A set-but-invalid install (bad path, failed version check, or failed checksum) is a cache miss, not an unconditional version abort (FR-001).** With a valid `BUILDDEPS_TOKEN`, expect acquisition and checksum verification first, then the version gate to HARD-FAIL with exit `19` before any Xvfb/game launch (use a fixture whose acquired tree is still outside the matrix). Without a token (or with `--no-acquire`), expect exit `9` (missing dependency), naming the normalized path and the absent token but never its value, with no network attempt. Exit `19` is therefore conditional on acquisition succeeding; never claim it unconditionally.

### VS-08 — Flaky scenario

Simulate a transient failure on attempt 1 that passes on retry. Expect exit `0` (PASS) with `flaky: true` metadata, but **no promotion**: `test.md` stays `partial` and `attempts/` retains every attempt. Quarantine logic is exercised only after 2 flaky runs in a rolling 14-day window.

### VS-09 — Quarantined scenario

Run a quarantined scenario. Expect exit `20` (NO_TESTS) with JUnit `<skipped>`, not a failure.

### VS-09b — Expected failure

Run a negative scenario declaring `expectedVerdict: fail` whose assertion is intentionally violated. Expect the verdict to remain `fail` and exit `18`; in an explicitly expected-failure lane, opt into `--ignore-exit-code 18` to demote only the process exit code. A mismatch (including an unexpected pass) is `errored` with exit `19` and an `expected-verdict-mismatch` diagnostic in the bundle. No branch promotes `test.md`.

### VS-09c — Offline acquisition ban

Run with `--no-acquire` and no cached install. Expect exit `9` (missing dependency) without any network attempt; with a valid cache, expect a normal run and no network access. `--no-acquire --force-refresh` exits `14`.

### VS-10 — Farmhand host-guard

Run a farmhand-tagged scenario via `debug split`. Expect the `Soil growth modifier skipped: not main player.` skip line. Absence of that line **with** execution evidence (the elapsed-ms trace line, or a `phaseDays` mutation) is a host-guard breach: verdict `fail`, exit `18`, non-retryable, never quarantined, recorded as a host-guard diagnostic in the bundle. Even when the guard holds, a farmhand/`debug split` run is **ineligible for `machine-pass`** and stays `partial` with the qualifier "host-authority guard exercised, transport not verified".

## Fallback

On hosts where the runner cannot execute, use the manual SC-009 protocol (`VERIFICATION_PROTOCOL.md` §3, `gate3-verification-contract.md`) per FR-021. The runner and manual protocol produce equivalent evidence bundles; both can reach `machine-pass`. Because the manual session is performed and judged by a maintainer, that maintainer may additionally record the human honesty-level **E** attestation and promote the claim to `verified`; the automated runner never performs that promotion itself.