# Phase 9 Convergence Re-Verification — Spec 017 (Joja / Agrochemical MVP)

**Date:** 2026-11-06 · **Purpose:** append-only closure evidence for the Phase 9 convergence
tasks (T064–T089 in `tasks.md`). Prior artifacts (`automated-tests.md`, `static-checks.md`,
`claim-records.md`, `host-only-gate3-run.md`, …) are **not rewritten** by this file; this
records the post-convergence tree separately. No claim is promoted by this re-run.

**Scope:** code-and-artifacts verification of the convergence remediation only. The host-only
Gate 3 bundle (`evidence-bundle-run-20261107T015936Z-7f35`) was re-checked **offline** only;
no new headless run was executed, so the `host-only-machine-pass` claim is unchanged (and still
covers the measured 50 → 40 path only, not the C2 unknown-baseline path).

## Commands and results

| Check | Command | Result |
|---|---|---|
| Build + targeted 017 suite | `GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release --filter "FullyQualifiedName~Agrochemical"` | **92/92 passed, 0 failed.** Includes the RED-turned-green rows: `TrySetSoilHealth_*` seam tests, refusal NaN-sentinel / `WasClamped=false`, unmappable-tile `RefusedInvalidTarget`, refusal 3-element parity (15 rows), bounded/recoverable clause (3 locales), pt `applied` magnitude + clamped full-penalty comparison, inverted T078 `WithoutService` test, T087 registration wiring |
| Full regression suite | `GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release` | **1844 passed, 0 failed, 4 skipped** (skips are the pre-existing spec-009 harvest-hook deferrals). Was 1812 passed / 4 skipped before convergence (+32 cases) |
| Formatting gate | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | exit **0** |
| Shell smoke suite | `SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh` | **50 files, 49 passed, 1 skipped, 0 failed** (one file skipped for environment reasons) |
| 017 claim-register validator | `bash scripts/tests/test-017-claims.sh` | exit **0** — 11 rows, schema-legal enums, 6/6 complete-row source paths exist, D1/human-verified/SC-004 remain non-`complete` (honesty rules hold) |
| Offline Gate 3 bundle check | `./scripts/verify-scenario.sh --verify-bundle evidence-bundle-run-20261107T015936Z-7f35` | exit **0** — bundle intact; `test.md` remains `machine-pass`, honesty `R` (host-only) |

Raw outputs: targeted run stdout (inline), full suite log at `/tmp/opencode/full-017.log`, RED
log at `/tmp/opencode/red-017.log` (pre-implementation failures, all expected), bundle log at
`/tmp/opencode/bundle-check.log`.

## What this does NOT prove (honesty boundaries)

- The host-only Gate 3 was **not re-run**; the machine-pass claim is unchanged in scope (host,
  measured 50 → 40 only). It is not human verification, not D1, not SC-004.
- T079 (zero-stack inventory slot clear) and T069 (icon strategy) are **not** observable in a
  live session from this re-run; the clear follows the `TryPlaceBin` slot-by-reference pattern
  and is best-effort with headless skip semantics, and the item ships with the **vanilla
  springobjects fallback sprite** (explicit acceptance recorded in
  `contracts/content-registration.md`); inventory render legibility remains unverified in-game.
- The C1 no-double-counting claim remains a dependency-boundary check plus lexical proxies;
  **no behavioral composition proof exists or is claimed** (`verification/double-counting-risk.md`).
- The measured-zero outcome does not survive save/load (sparse-cache zero omission, pre-existing);
  see `verification/measured-zero-persistence.md`.
- Normal-play acquisition remains formally **descoped**; `lr_give_agrochemical` is verification-only
  (`verification/provisioning-validation.md`).
- D1 (farmhand convergence), `human-verified`, and SC-004 remain **pending**; nothing here
  promotes them. SC-004 protocol (`sc004-v1`) has still never been run.

## Provenance

New RED-wave failing-test evidence is in `/tmp/opencode/red-017.log` (30 expected failures).
Wave commits: Wave A `1cd0265` `fbbad22` `20f380b` `e79100b` `f8d0061` `16ed2ad` `de52e6a`
`0fa5bfa` `cc6ee1a` `18dd04e` `28125f4` `6361d46` `3ae46fc` `564ced1` `ad619e4` `619e3c5`
`938aa3c` `00e26fe` `0c5e21c` `26a1265` `5de6ebf` · Wave B `e9b8044` `f5cabb7` `97336e8`
`412d94d` `f28029c` `4e5b199` `ea7bd2a` `5ef7569` `8b56cb3` `6124518` `6384151` `e323894`
`e7cf687` `fdf4b7e` `c3f7297`. Unrelated working-tree files (`TooltipRenderer.cs`, tooltip
tests, `.specify/bugs/soil-health-unknown-tooltip-flashing/`) were not part of this feature and
were not modified.
