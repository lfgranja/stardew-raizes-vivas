# Implementation Plan: 018 — Machine-Pass Full Coverage (research-driven)

**Branch**: `018-machine-pass-full-coverage` (planned) | **Date**: 2026-11-07
**Spec**: [`research.md`](research.md) (this folder) — research-first; a formal `spec.md` is to be produced by `/speckit.specify` if the team wants the full MDMV gate set. The research artifact is the authoritative intent for this plan.
**Input**: `specs/017-joja-agrochemical-tradeoff/` (current best-practice reference for scenario authoring, verification commands, claim honesty), `specs/010-headless-verification-runner/` (runner base), `specs/016-gate3-runner-hardening/` (hardening).

## Summary

Close the gap between "host-only machine-pass for the measured path of three features" and
"host machine-pass for every applicable path of every feature that ships runnable code", while
honestly recording every path that is structurally incapable of being `machine-pass` (farmhand
transport, SC-004 comprehension, render legibility, balance) as `partial`/`blocked` with
reasons — never fabricating a run. Work is dominated by **scenario authoring** and, for mulch,
**new verification commands**; no production gameplay behavior changes are required except the
mulch verification seam. All new evidence is append-only and classed honestly.

## Technical Context

- **Language/Version**: C# (latest) on .NET 6 (unchanged).
- **Primary Dependencies**: SMAPI, `Pathoschild.Stardew.ModBuildConfig`, xUnit + Moq + FsCheck (unchanged).
- **Storage**: Stardew save/mod data via existing seams (`ISoilHealthService.SaveData/LoadData`, `IModDataService`); no new persistence.
- **Testing**: `dotnet test` (xUnit), Moq, FsCheck; Gate 3 via `scripts/verify-scenario.sh` (016-hardened); offline bundle check; claims validator `scripts/tests/test-017-claims.sh`.
- **Target Platform**: Stardew Valley 1.6 + SMAPI (Linux dev; Windows/macOS supported by game).
- **Project Type**: SMAPI mod (in place).
- **Performance Goals**: negligible — scenario authoring only; new verification commands mirror the agrochemical seam (O(1) per apply).
- **Constraints**: DDD layering; `ModEntry.cs` sole composition root; host-authoritative farm state (Constitution VI); no Harmony fertilizer hook; no `Data/Shops`; no second source of truth; `ModConstants` for tunables; i18n parity for any new player-facing strings; Gate 3 honesty/provenance rules (append-only evidence, no retroactive promotion, no fabricated runs); farmhand runs capped at `partial` by runner design.

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. DDD (NON-NEGOTIABLE) | PASS (planned) | New verification commands follow the Phase-8 pattern: contract in `Domain/Interfaces`, impl in `Services`, wiring in `ModEntry.cs`; scenario authoring touches no domain logic. |
| II. Security-First Data Handling | PASS | `lr_give_mulch`/`lr_apply_mulch` must reuse `SanitizePathSegments`/bounds validation; soil values clamped 0–100 via existing semantics; no new user-influenced paths. |
| III. Async-Only Concurrency | PASS | Reuse existing synchronous snapshot/lock patterns; no blocking async. |
| IV. TDD | PASS (planned) | New verification commands get unit tests (mocked seams); scenario thresholds are the E2E assertion; mutation-check each new scenario assertion once (game-verification skill rule 5). |
| V. Simplicity / YAGNI | PASS with explicit scope | Mulch seam is the only new code; everything else is scenario authoring. No farmhand transport harness (infeasible, documented blocked). |
| VI. Multiplayer First-Class | PASS with explicit boundary | Farmhand paths exercised only via split-screen guard (partial, never machine-pass); D1 transport recorded blocked with reason; no second source of truth. |

**Gate result: PASS (planned).** No constitution violations; the farmhand `partial` cap is a runner design constraint, not a violation.

## Project Structure

### Documentation (this feature)

```
specs/018-machine-pass-full-coverage/
├── research.md        # this research (authoritative)
├── plan.md            # this file
├── tasks.md           # sequenced waves (Wave 0..5)
├── quickstart.md      # commands per scenario
└── (spec.md produced later by /speckit.specify if the team wants the full MDMV gate set)
```

### Source Code (repository root) — only Wave 0 + Wave 4 touch code

```
LivingRoots/
├── Verification/Queue/QueueCommandParser.cs   # + lr_give_mulch, lr_apply_mulch (arity/validate)
├── Verification/ModEntry.cs                  # + GrantMulch, ApplyMulch handlers
├── Domain/Interfaces/ILivingRootsModApi.cs   # + ApplyMulch doc (host-only)
├── Services/LivingRootsModApi.cs             # + ApplyMulch impl
└── ModEntry.cs                              # + mulch wiring (composition root only)

specs/<feature>/scenarios/
├── <feature>-<path>.scenario.json            # new host-path scenarios
└── macros/
    ├── agrochemical-apply.txt                # existing
    ├── split-screen.txt                      # NEW (W0.1) — farmhand guard
    └── place-bin.txt                         # NEW (W0.2) — compost fixture
```

No new projects/solutions; no Harmony; no `Data/Shops`.

## Waves (summary; full task list in tasks.md)

- **Wave 0 (infra, 1–2 days):** create missing macros (`split-screen.txt`, `place-bin.txt`) and test `debug split` headless; add `lr_give_mulch`/`lr_apply_mulch` commands + API seam + wiring; fix `modVersion` declarations (0.4.0 → 0.1.0) or document declared-not-observed.
- **Wave 1 (017 paths, ~1 day):** unknown-baseline, clamped, refused-at-minimum, conflict, farmhand-guard scenarios + runs.
- **Wave 2 (005 + 014, ~1.5 days):** run 005 (currently partial), add poor/moderate variants; 014 unknown + measured-zero harvest scenarios; 014 farmhand-guard (if W0.1 succeeds).
- **Wave 3 (004, ~1 day):** moderate/healthy compost scenarios; 004 farmhand-guard.
- **Wave 4 (013 mulch, ~2–3 days):** mulch commands + scenario + farmhand-guard.
- **Wave 5 (evidence + honesty, ~1 day):** claim rows, validate/quickstart addenda, honesty review, smoke suite, macros fix, modVersion fix.

## Success Criteria (measurable)

- **SC-MP-1**: Every feature with a host-runnable scenario (004, 005, 014, 017) has at least one `host-only-machine-pass` (honesty R) recorded against the post-Wave-0 tree, OR an explicitly recorded `planned-run-not-executed`/`infeasible`/`blocked` claim with reason (FR-015 schema).
- **SC-MP-2**: Every host path that is exercisable with existing or Wave-0-added commands has a passing scenario with 3 agreeing signals and offline bundle verifier exit 0.
- **SC-MP-3**: Every farmhand path is exercised as a split-screen guard scenario and recorded as `partial` (never `machine-pass`), with the honesty cap disclosed in claim rows.
- **SC-MP-4**: 013 mulch has at least one host `machine-pass` (decay exemption path) OR a recorded infeasibility with reason.
- **SC-MP-5**: No claim is promoted above its evidence class; D1/human-verified/SC-004 remain `pending`/`blocked` until real evidence; prior bundles remain retained (append-only provenance).
- **SC-MP-6**: Every new scenario assertion is mutation-checked once (game-verification skill rule 5) and recorded.
- **SC-MP-7**: Determinism: each new scenario runs twice on the same seed and diffs the event log before its first promotion.
- **SC-MP-8**: No fabrication: no invented scenario, log, bundle, or pass; infeasible runs recorded with reason and capability missing.

## Complexity Tracking

No constitution violations. The farmhand `partial` cap and the D1/SC-004 blocked statuses are deliberate honesty boundaries (Principle VI + FR-015), not violations. The mulch verification seam is the only new code; everything else is scenario authoring and documentation. Complexity is justified by the explicit "100% of applicable paths" goal.<|end|><|start|>startup<|message|>analysis<|message|>Plan written. Now tasks.md with TDD ordering (test tasks first, implementation after; waves). I'll structure by waves with explicit [P] markers. Verification tasks last.<|end|><|start|>start of reasoning<|channel|>analysis<|message|>Now tasks.md with TDD ordering.<|end|><|start|>start of reasoning<|channel|>analysis<|message|>xml<|message|><rfl:parameter name="content">
---
description: "Task list for 018 — Machine-Pass Full Coverage (research-driven plan)"
---

# Tasks: 018 Machine-Pass Full Coverage

**Input**: `specs/018-machine-pass-full-coverage/research.md` (authoritative), `plan.md`, plus `specs/017-joja-agrochemical-tradeoff/` (reference) and `specs/010-headless-verification-runner/` (runner base).
**Tests**: Required. TDD red-first where new code is added (Wave 0 commands, Wave 4 mulch seam). Scenario authoring is test-first by definition (the scenario IS the test).
**Organization**: Waves 0–5; Wave 0 unblocks everything. Verification tasks (Wave 5) attest last.
**Task count**: 38 tasks (T001–T038).

## Execution Rules (must follow)

- **Scaffolds/templates before hand-written implementation** where applicable (LSP code actions first).
- **TDD / RED gate** for Wave 0 new commands: write the failing unit test, then implement.
- **Parallel waves ([P], disjoint files)**: scenario authoring across features is parallel; shared files (`QueueCommandParser.cs`, `ModEntry.cs`, `ILivingRootsModApi.cs`) are serialized.
- **Single writer for verification commands**: Wave 0's `lr_give_mulch`/`lr_apply_mulch` + API seam are one serialized sequence (like 017's T036–T042).
- **Shared tooling**: never run `dotnet build/test` from more than one agent at a time; queue it in this session.
- **Atomic commit hook**: commit after each completed file/edit with staged-file check.
- **Verification attestation**: only Wave 5 tasks attest. Agents must not self-attest.

## Format: `[ID] [P?] [Story?] Description`

## Path Conventions

- Mod code: `LivingRoots/`
- Verification mod: `LivingRoots.Verification/`
- Tests: `LivingRoots.Tests/`
- Feature scenarios: `specs/<NNN>-<feature>/scenarios/`
- Runner: `scripts/verify-scenario.sh` + `scripts/lib/*`

---

## Phase 1: Wave 0 — Runner infra (BLOCKS everything else)

- [ ] T001 Verify context: read `specs/018-machine-pass-full-coverage/{research.md,plan.md}`, `specs/010-headless-verification-runner/contracts/scenario-v3.schema.json`, `LivingRoots.Verification/Queue/QueueCommandParser.cs`, `LivingRoots.Verification/ModEntry.cs`, `LivingRoots/Controllers/ModController.cs` (mulch route ~2207), `LivingRoots/Services/MulchApplicationService.cs` (`TryApplyMulch`), `LivingRoots/Domain/Interfaces/IMulchApplicationService.cs`, and `specs/017-…/scenarios/macros/agrochemical-apply.txt`.
- [ ] T002 [P] Create `specs/010-headless-verification-runner/fixtures/valid/macros/split-screen.txt` (and `specs/010-…/scenarios/macros/split-screen.txt` if any fixture expects it) with the `debug split` command; run the `farmhand-host-guard` fixture end-to-end to confirm `Context.IsMainPlayer` flips to false headless. If it fails, record the failure and downgrade farmhand scenarios to documented `blocked` (no fabrication). Commit the macro or the failure note.
- [ ] T003 [P] Create `specs/010-headless-verification-runner/fixtures/valid/macros/place-bin.txt` mirroring the `place-bin` fixture's expected command sequence (read the fixture to derive it). Commit.
- [ ] T004 [P] Fix `environment.modVersion` declarations: set `0.1.0` in `specs/004/…/compost-growth-hardening.scenario.json`, `specs/005/…/soil-health-growth.scenario.json`, `specs/014/…/soil-health-harvest-quality.scenario.json`, `specs/017/…/agrochemical-tradeoff.scenario.json`, and all 010 fixtures; re-run `scripts/tests/test-017-agrochemical.sh` + `bash scripts/tests/test-017-claims.sh` (schema-validator) to confirm no regression. Commit.
- [ ] T005 Scaffold `lr_give_mulch <count>` and `lr_apply_mulch <x> <y> [location]` in `QueueCommandParser.cs` (arity + validation) and `ModEntry.cs` handlers (GrantMulch into host inventory, ApplyMulch delegating to `IMulchApplicationService.TryApplyMulch` via a host-only API seam) — follow the Phase-8 agrochemical pattern (`GrantAgrochemical`, `api.ApplyAgrochemical`). Scaffolds only: `NotImplementedException`/safe placeholders, no behavior. Scaffolds compile.
- [ ] T006 Write RED tests for the Wave-0 commands: `LivingRoots.Tests/Services/MulchVerificationCommandsTests.cs` (or extend existing Mulch tests) — `GrantMulch` host-gated, slot placement, `ApplyMulch` eligibility + refusal, no false success; also `LivingRoots.Tests/Controllers/ModControllerMulchTests.cs` if a controller seam is exercised. Mocked seams only.
- [ ] T007 Implement the mulch verification commands (`GrantMulch`, `ApplyMulch`) and wire `ILivingRootsModApi.ApplyMulch` + `ModEntry.BindMulchApplication` (composition root only). Run targeted tests → GREEN. Atomic commits.
- [ ] T008 TDD characterization: run `bash scripts/tests/test-017-agrochemical.sh` + `bash scripts/tests/test-017-claims.sh` once after Wave 0 to confirm no regression. Record result in the Wave-0 commit message.

## Phase 2: Wave 1 — 017 paths (highest value)

- [ ] T009 [P] Author `specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-unknown-baseline.scenario.json` (schemaVersion 3, host, init `lr_set_soil_health 60 15 NaN`, apply once via macro, threshold soilHealthStateDelta multiplier factor 0.9, cropPhaseDelta 1.0, smapiLog isMainPlayer eq true). Validate with `scripts/lib/scenario-validate.sh` (or runner dry-run). Commit.
- [ ] T010 [P] Author `agrochemical-clamped.scenario.json` (soil 5 → apply → post 0; threshold multiplier factor 0.0, tolerance 0.0001). Commit.
- [ ] T011 [P] Author `agrochemical-refused-at-minimum.scenario.json` (soil 0 → apply; expectedVerdict `partial` or `fail`; thresholds: soilHealthStateDelta multiplier 1.0 (unchanged), smapiLog refusal key present). Commit.
- [ ] T012 [P] Author `agrochemical-conflict.scenario.json` (soil 50; two apply steps; second refused; soil unchanged factor 1.0; smapiLog refusal). Commit (macro variant needed).
- [ ] T013 [P] Author `agrochemical-farmhand-guard.scenario.json` (playerRole farmhand, macro split-screen, apply; threshold smapiLog isMainPlayer eq false; expectedVerdict `partial`; never `machine-pass`). Commit — only if W0.1 succeeded; otherwise record as blocked with reason.
- [ ] T014 Run the 017 new scenarios via `verify-scenario.sh` (host ones first, in parallel only if disjoint bundles; farmhand one last). For each clean host pass, let the runner promote `test.md` (append-only bundles). Record verdicts. Commit the bundle artifacts + `host-only-gate3-017-paths-run.md` (append-only run record). Never fabricate a run; infeasible ones recorded with reason.

## Phase 3: Wave 2 — 005 and 014 paths

- [ ] T015 Run the existing `specs/005-soil-health-to-crop-growth/scenarios/soil-health-growth.scenario.json` (currently `partial`) end-to-end; promote via runner if clean host pass; record verdict. Commit.
- [ ] T016 [P] Author `soil-health-growth-poor.scenario.json` and `soil-health-growth-moderate.scenario.json` for 005 (poor 10 → 1.5×, moderate 50 → 1.0×), mirroring the healthy scenario's structure. Commit.
- [ ] T017 [P] Author `soil-health-harvest-unknown.scenario.json` for 014: `lr_set_soil_health 60 15 NaN` then `lr_harvest_matrix 60 15`; threshold on the Unknown-tier quality outcome (verify `HarvestPatches`/`IHarvestQualityService` NaN handling first — read the code; if Unknown is treated as Poor, assert poor-tier outcomes). Commit.
- [ ] T018 [P] Author `soil-health-harvest-measured-zero.scenario.json` (soil 0 + harvest_matrix; assert poor-tier outcomes; no soil mutation). Commit.
- [ ] T019 [P] Author `soil-health-harvest-farmhand-guard.scenario.json` (playerRole farmfarm, split-screen, partial) — only if W0.1 succeeded. Commit or record blocked.
- [ ] T020 Run the 005/014 new scenarios + the 005 existing run; promote via runner; record verdicts + bundles; append-only run records. Never fabricate.

## Phase 4: Wave 3 — 004 paths

- [ ] T021 [P] Author `compost-growth-moderate.scenario.json` and `compost-growth-healthy.scenario.json` for 004, mirroring `compost-growth-hardening.scenario.json` (poor 10). Thresholds: phaseDays multiplier consistent with the growth modifier (verify the modifier formula first — read `SeasonalDecayMultiplier`/compost application code). Commit.
- [ ] T022 [P] Author `compost-growth-farmhand-guard.scenario.json` (split-screen, partial) — only if W0.1 succeeded. Commit or record blocked.
- [ ] T023 Run the 004 new scenarios via the runner; promote via runner if clean; record verdicts + bundles. Never fabricate.

## Phase 5: Wave 4 — 013 mulch (largest)

- [ ] T024 Read `LivingRoots/Services/MulchApplicationService.cs` (`TryApplyMulch`) and `LivingRoots/Controllers/ModController.cs` mulch route (~2207) to determine signals (mulch doesn't mutate soil → soilHealthStateDelta multiplier 1.0; cropPhaseDelta depends on decay exemption). Document the threshold plan in a new `specs/013-mulch-soil-cover/scenarios/README.md` (or in the scenario's `summary`) before authoring.
- [ ] T025 Implement `lr_give_mulch <count>` and `lr_apply_mulch <x> <y> [location]` in `QueueCommandParser.cs` + `ModEntry.cs` handlers + `ILivingRootsModApi`/`LivingRootsModApi` host-only seam, following the Phase-8 agrochemical pattern (TDD: RED tests in `LivingRoots.Tests/Services/MulchVerificationCommandsTests.cs` first; then implement; then GREEN). Atomic commits.
- [ ] T026 Author `specs/013-mulch-soil-cover/scenarios/mulch-decay-exemption.scenario.json` (plant crop, set soil 50, give mulch, apply mulch, advance 2 days, dump crop phase; threshold cropPhaseDelta multiplier 1.0 — mulch exempts from decay). Commit.
- [ ] T027 [P] Author `specs/013-mulch-soil-cover/scenarios/mulch-refused-on-ineligible.scenario.json` (ineligible target → refusal; smapiLog threshold). Commit.
- [ ] T028 [P] Author `specs/013-mulch-soil-cover/scenarios/mulch-farmhand-guard.scenario.json` (split-screen, partial) — only if W0.1 succeeded. Commit or record blocked.
- [ ] T029 Run the mulch scenarios via the runner; promote via runner if clean; record verdicts + bundles. Never fabricate. Also create `specs/013-mulch-soil-cover/test.md` per the 010 template (currently absent) and record the run.

## Phase 6: Wave 5 — evidence + honesty closure (attests last)

- [ ] T030 For each new machine-pass: append claim rows to `specs/<feature>/verification/claim-records.md` (create the file if absent, following 017's schema) — append-only; class `host-only-machine-pass` for host runs, `partial` for farmhand guard runs, `planned-run-not-executed`/`blocked` for infeasible ones; never promote above evidence. Create `specs/<feature>/verification/<feature>-gate3.md` run records (append-only, like 017's `host-only-gate3-run.md`).
- [ ] T031 Update each feature's `test.md` only via runner promotion (never hand-edit the GATE3 block); update `quickstart.md`/`verification/summary.md` addenda (append-only) to cite the new bundles; add `verification/convergence-…` notes where appropriate.
- [ ] T032 Fix the 010 fixture gap: create the missing `macros/split-screen.txt` and `macros/place-bin.txt` (or fix fixture references to existing macros) and re-run the 010 fixture validation suite; record outcome.
- [ ] T033 Refresh `environment.modVersion` declarations in all scenario files (0.4.0 → 0.1.0) and re-run `bash scripts/tests/test-017-agrochemical.sh` + `bash scripts/tests/test-017-claims.sh` to confirm no regression.
- [ ] T034 Mutation-check each new scenario assertion once (game-verification skill rule 5): break the relevant line, watch the scenario fail at the expected threshold, restore; record the mutation in the commit message or a ledger (`verification/mutation-checks.md`). No self-attestation.
- [ ] T035 Determinism check: run each new scenario twice on the same seed; diff the event logs; record the result in the run record. No self-attestation.
- [ ] T036 Run the full shell smoke suite (`SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh`) after all new scenarios land; record outcome (file count, pass/fail/skip) in `verification/summary.md` addendum. If a run fails, halt and report; do not fabricate.
- [ ] T037 Run the 016-hardened runner's offline bundle check (`--verify-bundle`) on every new bundle; record exit codes in the run records. Record any infeasible run as `planned-run-not-executed` with reason + capability missing.
- [ ] T038 Produce a final honest inventory update: append a dated section to `specs/018-machine-pass-full-coverage/research.md` (or a new `verification/final-inventory.md`) listing each feature × path with its achieved class (machine-pass / partial / blocked) and the evidence artifact path; state explicitly that machine-pass ≠ human-verified, D1 transport = blocked/pending, SC-004 = pending, render legibility = manual, balance = playtest. No claim promotion.

## Dependencies & Execution Order

- **Wave 0 (Phase 1)** blocks everything.
- **Wave 1 (Phase 2)** depends on Wave 0 (macros + mulch commands not needed for 017, but split-screen macro needed for 017 farmhand-guard scenario).
- **Wave 2 (Phase 3)** depends on Wave 0; 005's run (T015) can start immediately (no new commands).
- **Wave 3 (Phase 4)** depends on Wave 0 (split-screen macro); compost commands already exist.
- **Wave 4 (Phase 5)** depends on Wave 0 (new mulch commands).
- **Wave 5 (Phase 6)** depends on all prior waves; T030 (claim rows) depends on T014/T020/T023/T029 verdicts; T038 last.

## Parallel Opportunities

| Wave | Parallelizable | Notes |
|---|---|---|
| 0 | T002, T003 (macros), T004 (modVersion) | Disjoint files. T005-T007 serialized (shared parser/ModEntry). |
| 1 | T009-T013 (scenario authoring) | Disjoint files. T014 (run) serialized after authoring; runs are long. |
| 2 | T016-T019 (scenario authoring) | Disjoint files. T015/T020 serialized (runs). |
| 3 | T021 (authoring) | T022 depends on T002; T023 serialized (run). |
| 4 | T026-T028 (authoring) | T025 serialized first (commands). T029 serialized (run). |
| 5 | T030-T038 | T030 depends on prior verdicts; T034-T037 serialized (shared dotnet/runner tooling). |

## Notes

- `[P]` = disjoint files only. Shared files (`QueueCommandParser.cs`, `ModEntry.cs`, `ILivingRootsModApi.cs`) require sequencing/same subagent.
- The farmhand `partial` cap is **by design** (GOVERNANCE-ANNEX): split-screen runs never `machine-pass`. Do not attempt to promote them. D1 transport remains `blocked` unless a two-instance harness is funded (out of scope for this repo's CI).
- `human-verified`, `d1-farmhand-convergence`, and SC-004 are **never** machine-pass. Tomorrow night's human validation supplies those.
- No code behavior changes in Waves 1-3 (only scenario authoring + existing-command reuse). Wave 4 adds the mulch verification seam (the only new code).
- Do not claim "100% machine-pass": the honest target is host machine-pass + farmhand partial + automated-only + blocked-with-reason. State this in every report and every claim row.
- `P=10` stays provisional; no final balance claim.
- Atomic commits after each completed task/file edit; `git diff --cached --stat` before committing; never `git add -A`.
- Verification tasks (Wave 5) are the only attesters. No agent self-attests.