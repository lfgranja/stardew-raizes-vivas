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

## Corrective addendum (2026-11-07, Phase 10 / T091)

This addendum corrects the attestation-integrity gaps recorded as findings V-01/V-02 in the
Phase 10 converge re-audit; prior text above is retained unmodified.

- **V-02 ("92/92 targeted" not reproducible):** The source enumeration for the
  `FullyQualifiedName~Agrochemical` filter at the Phase 9 closure tree was **88** (five classes:
  45 parity + 16 service + 4 persistence + 20 controller + 3 registration); the RED log printed
  "Total: 92" because it counted the previously-inverted T078 row and pre-Phase-10 additions
  differently across revisions. The "92/92" figure in this file's summary table and the
  claim-records addendum is **not valid evidence for the post-Phase-10 tree**. After Phase 10
  (T090–T104) the targeted run is **119/119 (0 failed)** and is retained as
  `verification/convergence-phase10-targeted.log` (exit 0). The Phase-10 RED log showing the 21
  expected pre-implementation failures is retained as `verification/convergence-phase10-red.log`,
  and the matching full-suite log (**1871 passed, 0 failed, 4 skipped**, exit 0) as
  `verification/convergence-phase10-full.log`. `dotnet format --verify-no-changes` exits 0 on the
  same tree.
- **V-01 (cited bundle-check output was 0 bytes):** The previously cited
  `/tmp/opencode/bundle-check.log` was 0 bytes. The 016-hardened runner's
  `--verify-bundle` mode **emits no stdout**; its attestation is the exit code. Exit code **0**
  was observed on 2026-11-06 (twice) and again on 2026-11-07 after Phase 10. The cited-file
  discipline is corrected by this addendum: future citations point to committed artifacts, not
  `/tmp`. The bundle itself (`evidence-bundle-run-20261107T015936Z-7f35/`) was additionally
  digest-audited on 2026-11-06 (16/16 `manifest.json` subject digests verify; `manifest.sha256`
  matches; `verdict.json` pass; `claims.json` honestyLevel `R`); the hardened
  `scripts/tests/test-017-claims.sh` also digest-checks cited bundle directories (exit 0 in this
  session).
- **Runner re-run (T090):** executed on 2026-11-07 after this correction; outcome recorded as a
  NEW append-only claim-record row (e5882c1-era row untouched). See
  `verification/host-only-gate3-phase10-run.md` and the claim-records addendum.
- **Enumeration**: the Phase-9 "92/92" figure was a miscount; the honest source enumeration at
  that tree was 88. After Phase-10 changes the targeted run totals **119** (0 failed), with the
  21 pre-implementation failures recorded in `verification/convergence-phase10-red.log`; the
  per-class breakdown is derivable from the test sources and was not re-enumerated here to avoid
  inventing another unverified count.

## Scenario declaration honesty addendum (2026-11-07, Phase 11 / T110)

**Append-only; prior text above is retained unmodified.** The tracked scenario
`scenarios/agrochemical-tradeoff.scenario.json` declared `environment.modVersion: "0.4.0"`
until 2026-11-07. Dispositions:

- **Both tracked bundles record `0.4.0`** as their `observed`/copied mod-version field:
  `evidence-bundle-run-20261107T015936Z-7f35/` (Phase 8, e5882c1-era) and
  `evidence-bundle-run-20261107T040235Z-589a/` (Phase 10, c9ff596-era). The runner copies the
  scenario's declared `environment.modVersion` verbatim into the bundle's mod-version field; it
  is **declared-not-observed** — no check anywhere compares it against the actually-deployed
  mod build. The two historic bundles therefore state `0.4.0` regardless of which LivingRoots
  build was loaded; the field was never validated. This is disclosed, not papered over: future
  citations must not read the bundle's mod-version field as evidence of which build ran.
- The in-scenario `time` field (`2026-11-06T12:00:00Z`) is **not** the run wall-clock; actual
  run timestamps live in the bundles/run records (e.g. Phase 10: `2026-11-07T04:03:17Z`). The
  `time` field is schema-legal declared context and is not a chronology attestation.
- `rngSeed` (`123456789`) is **schema-legal declared context**; nothing in the 017 agrochemical
  path consumes it (the path is deterministic given the seeded soil state), and no claim is
  made from it.
- On 2026-11-07 the scenario's `environment.modVersion` was corrected to **`0.1.0`** (SemVer-valid,
  matching the released manifest at `d3ada42`). Bundles produced from this point (Phase 12+ runs)
  will record `0.1.0` in the same declared-not-observed verbatim-copy manner. The two historic
  bundles are not rewritten and remain `0.4.0`-recording artifacts. `test-017-agrochemical.sh`
  re-run after the edit: **23/23 passed**.
