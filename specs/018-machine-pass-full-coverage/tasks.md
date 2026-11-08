# Tasks: 018 Machine-Pass Full Coverage

**Status (reconciled 2026-11-07, ciclo 80/20 MDMV adaptado):** Plano 018 amplamente realizado no repositório, mas **não totalmente auditado** nesta sessão. Marcação abaixo é baseada em evidência verificável no filesystem/test.md/código e em checks executados neste ciclo (`dotnet build` mod principal 0 erros; `scripts/tests/test-017-agrochemical.sh` 23/23; `scripts/tests/test-017-claims.sh` PASS). **Não promovido acima da evidência.** Farmhand split-screen headless: **bloqueado/parcial-factível** (T002). `dotnet format --verify-no-changes`, testes unitários direcionados e smoke suite excederam timeout da ferramenta nesta sessão — **não atestados**.

**Input**: `specs/018-machine-pass-full-coverage/research.md` (authoritative), `plan.md`, plus `specs/017-joja-agrochemical-tradeoff/` (reference) and `specs/010-headless-verification-runner/` (runner base).
**Tests**: Required. TDD red-first where new code is added (Wave 0 commands, Wave 4 mulch seam). Scenario authoring is test-first by definition (the scenario IS the test).
**Organization**: Waves 0–5; Wave 0 unblocks everything. Verification tasks (Wave 5) attest last.
**Task count**: 38 tasks (T001–T038).

## Reconciliation notes (evidence)

- 005 `test.md`: machine-pass host; claim cita healthy/poor/moderate (`run-20261107T112602Z-5b08`, `114448Z-4b61`, `114623Z-3365`).
- 014 `test.md`: machine-pass host; claim cita matriz + unknown/measured-zero (`014750Z-14b6`, `122915Z-2564`, `123141Z-3531`, `123257Z-08c9`).
- 017 `test.md`: machine-pass host (`run-20261107T193756Z-6d97`); `verification/claim-records.md` validado (12 linhas) neste ciclo.
- 013 `test.md`: machine-pass host (`run-20261107T205338Z-08f8`).
- 004 `test.md`: machine-pass host hardening (`run-20261105T053313Z-535d`); cenários moderate/healthy existem — cobertura completa por run requer auditoria.
- Verificação parser: `lr_give_mulch`/`lr_apply_mulch`/`lr_split_screen` presentes em `LivingRoots.Verification/Queue/QueueCommandParser.cs`; `GrantMulch`/`ApplyMulch` em `LivingRoots.Verification/ModEntry.cs`.
- T002: `verification/t002-farmhand-host-guard-attempt.md`, `verification/cycle-2026-11-07-80-20.md`.

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

- [x] T001 Verify context: read `specs/018-machine-pass-full-coverage/{research.md,plan.md}`, `specs/010-headless-verification-runner/contracts/scenario-v3.schema.json`, `LivingRoots.Verification/Queue/QueueCommandParser.cs`, `LivingRoots.Verification/ModEntry.cs`, `LivingRoots/Controllers/ModController.cs` (mulch route ~2207), `LivingRoots/Services/MulchApplicationService.cs` (`TryApplyMulch`), `LivingRoots/Domain/Interfaces/IMulchApplicationService.cs`, and `specs/017-…/scenarios/macros/agrochemical-apply.txt`. **Evidência:** verificação realizada no ciclo 2026-11-07 (inspeção de arquivos/código).
- [x] T002 [P] Create `specs/010-headless-verification-runner/fixtures/valid/macros/split-screen.txt` (and `specs/010-…/scenarios/macros/split-screen.txt` if any fixture expects it) with the `debug split` command; run the `farmhand-host-guard` fixture end-to-end to confirm `Context.IsMainPlayer` flips to false headless. If it fails, record the failure and downgrade farmhand scenarios to documented `blocked` (no fabrication). Commit the macro or the failure note. **Evidência:** `verification/t002-farmhand-host-guard-attempt.md` + `cycle-2026-11-07-80-20.md`. **Status honesto:** macro/`lr_split_screen` presentes; headless `isMainPlayer` flip **não demonstrado** (SMAPI `ICommandHelper.Trigger` indisponível no build referenciado / mecanismo parcial). Farmhand guard: **blocked-feasible/partial, nunca machine-pass**.
- [x] T003 [P] Create `specs/010-headless-verification-runner/fixtures/valid/macros/place-bin.txt` mirroring the `place-bin` fixture's expected command sequence (read the fixture to derive it). Commit. **Evidência:** arquivos existem em `specs/010-headless-verification-runner/fixtures/valid/macros/place-bin.txt`, `invalid/`, `runner-invalid/`.
- [x] T004 [P] Fix `environment.modVersion` declarations: set `0.1.0` in `specs/004/…/compost-growth-hardening.scenario.json`, `specs/005/…/soil-health-growth.scenario.json`, `specs/014/…/soil-health-harvest-quality.scenario.json`, `specs/017/…/agrochemical-tradeoff.scenario.json`, and all 010 fixtures; re-run `scripts/tests/test-017-agrochemical.sh` + `bash scripts/tests/test-017-claims.sh` (schema-validator) to confirm no regression. Commit. **Evidência parcial:** cenários 004/017 amostrados = `0.1.0`; `test-017-agrochemical.sh` 23/23 e `test-017-claims.sh` PASS neste ciclo. **Sweep completo 010 fixtures não revalidado nesta sessão.**
- [x] T005 Scaffold `lr_give_mulch <count>` and `lr_apply_mulch <x> <y> [location]` in `QueueCommandParser.cs` (arity + validation) and `ModEntry.cs` handlers (GrantMulch into host inventory, ApplyMulch delegating to `IMulchApplicationService.TryApplyMulch` via a host-only API seam) — follow the Phase-8 agrochemical pattern (`GrantAgrochemical`, `api.ApplyAgrochemical`). Scaffolds only: `NotImplementedException`/safe placeholders, no behavior. Scaffolds compile. **Evidência:** comandos/handlers presentes no parser e no Verification `ModEntry.cs` (grep/verificação de código); build principal passa.
- [~] T006 Write RED tests for the Wave-0 commands: `LivingRoots.Tests/Services/MulchVerificationCommandsTests.cs` (or extend existing Mulch tests) — `GrantMulch` host-gated, slot placement, `ApplyMulch` eligibility + refusal, no false success; also `LivingRoots.Tests/Controllers/ModControllerMulchTests.cs` if a controller seam is exercised. Mocked seams only. **Evidência pendente:** sequência RED-first não auditada neste ciclo; testes unitários direcionados não reexecutados (timeout).
- [~] T007 Implement the mulch verification commands (`GrantMulch`, `ApplyMulch`) and wire `ILivingRootsModApi.ApplyMulch` + `ModEntry.BindMulchApplication` (composition root only). Run targeted tests → GREEN. Atomic commits. **Evidência parcial:** implementação presente; targeted tests/green **não atestados** nesta sessão.
- [x] T008 TDD characterization: run `bash scripts/tests/test-017-agrochemical.sh` + `bash scripts/tests/test-017-claims.sh` once after Wave 0 to confirm no regression. Record result in the Wave-0 commit message. **Evidência:** 23/23 e PASS neste ciclo (2026-11-07).

## Phase 2: Wave 1 — 017 paths (highest value)

- [x] T009 [P] Author `specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-unknown-baseline.scenario.json` (schemaVersion 3, host, init `lr_set_soil_health 60 15 NaN`, apply once via macro, threshold soilHealthStateDelta multiplier factor 0.9, cropPhaseDelta 1.0, smapiLog isMainPlayer eq true). Validate with `scripts/lib/scenario-validate.sh` (or runner dry-run). Commit. **Evidência:** arquivo existe.
- [x] T010 [P] Author `agrochemical-clamped.scenario.json` (soil 5 → apply → post 0; threshold multiplier factor 0.0, tolerance 0.0001). Commit. **Evidência:** arquivo existe.
- [x] T011 [P] Author `agrochemical-refused-at-minimum.scenario.json` (soil 0 → apply; expectedVerdict `partial` or `fail`; thresholds: soilHealthStateDelta multiplier 1.0 (unchanged), smapiLog refusal key present). Commit. **Evidência:** arquivo existe.
- [~] T012 [P] Author `agrochemical-conflict.scenario.json` (soil 50; two apply steps; second refused; soil unchanged factor 1.0; smapiLog refusal). Commit (macro variant needed). **Evidência parcial/honesta (pass 3):** existe `agrochemical-refused-contention.scenario.json` (seam determinístico autorizado via `lr_acquire/release_host_lock`; recusa/sem mutação/soil `1.0×`; 017 `test.md` append-only cita `run-20261107T193756Z-6d97`, bundle `--verify-bundle` exit 0 neste ciclo). **Não é idêntico** a “duas applies sequenciais” literais; **atesta invariantes de conflito/recusa** por mecanismo diferente. Gap literal de baixo valor/residual.
- [ ] T013 [P] Author `agrochemical-farmhand-guard.scenario.json` (playerRole farmhand, macro split-screen, apply; threshold smapiLog isMainPlayer eq false; expectedVerdict `partial`; never `machine-pass`). Commit — only if W0.1 succeeded; otherwise record as blocked with reason. **Status:** **blocked-feasible** por T002.
- [~] T014 Run the 017 new scenarios via `verify-scenario.sh` (host ones first, in parallel only if disjoint bundles; farmhand one last). For each clean host pass, let the runner promote `test.md` (append-only bundles). Record verdicts. Commit the bundle artifacts + `host-only-gate3-017-paths-run.md` (append-only run record). Never fabricate a run; infeasible ones recorded with reason. **Evidência parcial:** 017 `test.md` mostra machine-pass host (`run-20261107T193756Z-6d97`); claim-records válidos. Cobertura por cenário individual não auditada integralmente neste ciclo.

## Phase 3: Wave 2 — 005 and 014 paths

- [x] T015 Run the existing `specs/005-soil-health-to-crop-growth/scenarios/soil-health-growth.scenario.json` (currently `partial`) end-to-end; promote via runner if clean host pass; record verdict. Commit. **Evidência:** 005 `test.md` machine-pass (healthy `5b08`; claim cita todos os paths host aplicáveis).
- [x] T016 [P] Author `soil-health-growth-poor.scenario.json` and `soil-health-growth-moderate.scenario.json` for 005 (poor 10 → 1.5×, moderate 50 → 1.0×), mirroring the healthy scenario's structure. Commit. **Evidência:** arquivos existem; runs citados no 005 `test.md`.
- [x] T017 [P] Author `soil-health-harvest-unknown.scenario.json` for 014: `lr_set_soil_health 60 15 NaN` then `lr_harvest_matrix 60 15`; threshold on the Unknown-tier quality outcome (verify `HarvestPatches`/`IHarvestQualityService` NaN handling first — read the code; if Unknown is treated as Poor, assert poor-tier outcomes). Commit. **Evidência:** arquivo existe; 014 `test.md` cita unknown run.
- [x] T018 [P] Author `soil-health-harvest-measured-zero.scenario.json` (soil 0 + harvest_matrix; assert poor-tier outcomes; no soil mutation). Commit. **Evidência:** arquivo existe; 014 `test.md` cita measured-zero run.
- [ ] T019 [P] Author `soil-health-harvest-farmhand-guard.scenario.json` (playerRole farmhand, split-screen, partial) — only if W0.1 succeeded. Commit or record blocked. **Status:** blocked-feasible (T002).
- [x] T020 Run the 005/014 new scenarios + the 005 existing run; promote via runner; record verdicts + bundles; append-only run records. Never fabricate. **Evidência parcial:** 005/014 `test.md` com machine-pass/runs citados; registros append-only detalhados não auditados integralmente neste ciclo.

## Phase 4: Wave 3 — 004 paths

- [~] T021 [P] Author `compost-growth-moderate.scenario.json` and `compost-growth-healthy.scenario.json` for 004, mirroring `compost-growth-hardening.scenario.json` (poor 10). Thresholds: phaseDays multiplier consistent with the growth modifier (verify the modifier formula first — read `SeasonalDecayMultiplier`/compost application code). Commit. **Evidência:** arquivos existem. Threshold/fórmula não revalidada neste ciclo.
- [ ] T022 [P] Author `compost-growth-farmhand-guard.scenario.json` (split-screen, partial) — only if W0.1 succeeded. Commit or record blocked. **Status:** blocked-feasible (T002).
- [~] T023 Run the 004 new scenarios via the runner; promote via runner if clean; record verdicts + bundles. Never fabricate. **Evidência parcial:** 004 `test.md` machine-pass hardening (`535d`); cobertura moderate/healthy por run não explícita na claim lida.

## Phase 5: Wave 4 — 013 mulch (largest)

- [x] T024 Read `LivingRoots/Services/MulchApplicationService.cs` (`TryApplyMulch`) and `LivingRoots/Controllers/ModController.cs` mulch route (~2207) to determine signals (mulch doesn't mutate soil → soilHealthStateDelta multiplier 1.0; cropPhaseDelta depends on decay exemption). Document the threshold plan in a new `specs/013-mulch-soil-cover/scenarios/README.md` (or in the scenario's `summary`) before authoring. **Evidência (pass 3):** criado `specs/013-mulch-soil-cover/scenarios/README.md` com tabela de sinais/thresholds e referências; resumo honesto (assertions autoritativas = JSON + runner).
- [~] T025 Implement `lr_give_mulch <count>` and `lr_apply_mulch <x> <y> [location]` in `QueueCommandParser.cs` + `ModEntry.cs` handlers + `ILivingRootsModApi`/`LivingRootsModApi` host-only seam, following the Phase-8 agrochemical pattern (TDD: RED tests in `LivingRoots.Tests/Services/MulchVerificationCommandsTests.cs` first; then implement; then GREEN). Atomic commits. **Evidência parcial:** implementação/parser+Verification ModEntry presentes; seam Mod API/TDD/green não auditados integralmente.
- [x] T026 Author `specs/013-mulch-soil-cover/scenarios/mulch-decay-exemption.scenario.json` (plant crop, set soil 50, give mulch, apply mulch, advance 2 days, dump crop phase; threshold cropPhaseDelta multiplier 1.0 — mulch exempts from decay). Commit. **Evidência:** arquivo existe.
- [x] T027 [P] Author `specs/013-mulch-soil-cover/scenarios/mulch-refused-on-ineligible.scenario.json` (ineligible target → refusal; smapiLog threshold). Commit. **Evidência:** arquivo existe.
- [ ] T028 [P] Author `specs/013-mulch-soil-cover/scenarios/mulch-farmhand-guard.scenario.json` (split-screen, partial) — only if W0.1 succeeded. Commit or record blocked. **Status:** blocked-feasible (T002).
- [~] T029 Run the mulch scenarios via the runner; promote via runner if clean; record verdicts + bundles. Never fabricate. Also create `specs/013-mulch-soil-cover/test.md` per the 010 template (currently absent) and record the run. **Evidência parcial:** 013 `test.md` existe e mostra machine-pass (`run-20261107T205338Z-08f8`); cobertura de todos os cenários mulch/artefatos detalhados não auditada.

## Phase 6: Wave 5 — evidence + honesty closure (attests last)

- [~] T030 For each new machine-pass: append claim rows to `specs/<feature>/verification/claim-records.md` (create the file if absent, following 017's schema) — append-only; class `host-only-machine-pass` for host runs, `partial` for farmhand guard runs, `planned-run-not-executed`/`blocked` for infeasible ones; never promote above evidence. Create `specs/<feature>/verification/<feature>-gate3.md` run records (append-only, like 017's `host-only-gate3-run.md`). **Evidência parcial:** 017 claim-records validados; outras features não auditadas.
- [~] T031 Update each feature's `test.md` only via runner promotion (never hand-edit the GATE3 block); update `quickstart.md`/`verification/summary.md` addenda (append-only) to cite the new bundles; add `verification/convergence-…` notes where appropriate. **Evidência parcial:** test.md/GATE3 presentes; atualização manual vs runner não reconstituída aqui.
- [~] T032 Fix the 010 fixture gap: create the missing `macros/split-screen.txt` and `macros/place-bin.txt` (or fix fixture references to existing macros) and re-run the 010 fixture validation suite; record outcome. **Evidência parcial:** macros/place-bin existem; split-screen macro atualizada para `lr_split_screen` mas mecanismo headless bloqueado; suite 010 não reexecutada.
- [~] T033 Refresh `environment.modVersion` declarations in all scenario files (0.4.0 → 0.1.0) and re-run `bash scripts/tests/test-017-agrochemical.sh` + `bash scripts/tests/test-017-claims.sh` to confirm no regression. **Evidência parcial:** checks 017 passaram; sweep completo não revalidado.
- [ ] T034 Mutation-check each new scenario assertion once (game-verification skill rule 5): break the relevant line, watch the scenario fail at the expected threshold, restore; record the mutation in the commit message or a ledger (`verification/mutation-checks.md`). No self-attestation. **Status:** não atestado neste ciclo.
- [ ] T035 Determinism check: run each new scenario twice on the same seed; diff the event logs; record the result in the run record. No self-attestation. **Status:** não atestado neste ciclo.
- [~] T036 Run the full shell smoke suite (`SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh`) after all new scenarios land; record outcome (file count, pass/fail/skip) in `verification/summary.md` addendum. If a run fails, halt and report; do not fabricate. **Status (pass 4, honesto):** rerun completo capturado (`/tmp/opencode/smoke-run-all-pass4.txt`, exit wrapper 124/timeout+SIGTERM) reportou **29 files, 26 passed, 2 failed, 1 skipped — NOT clean pass**. Falhas: `test-ci-workflow.sh` (2 asserts desatualizados vs workflow GitHub-hosted PR #765, intenção a confirmar) e `test-preflight.sh` (seção interrompida por SIGTERM; asserts visíveis ok — provável timeout, não confirmado). Complementar verificado: format exit 0, 115 testes unitários direcionados pass. Ver `verification/cycle-2026-11-07-80-20-pass4.md`.
- [~] T037 Run the 016-hardened runner's offline bundle check (`--verify-bundle`) on every new bundle; record exit codes in the run records. Record any infeasible run as `planned-run-not-executed` with reason + capability missing. **Evidência parcial (pass 3):** `--verify-bundle` **exit 0** para bundles locais correspondentes a refs em `test.md`: `evidence-bundle-run-20261107T120435Z-1ea9` (004 moderate), `120602Z-69ea` (004 healthy), `193756Z-6d97` (017 contention), `205338Z-08f8` (013 mulch). **Não cobre** todos os bundles citados/nem todos os novos; auditoria completa pendente.
- [x] T038 Produce a final honest inventory update: append a dated section to `specs/018-machine-pass-full-coverage/research.md` (or a new `verification/final-inventory.md`) listing each feature × path with its achieved class (machine-pass / partial / blocked) and the evidence artifact path; state explicitly that machine-pass ≠ human-verified, D1 transport = blocked/pending, SC-004 = pending, render legibility = manual, balance = playtest. No claim promotion. **Evidência:** `verification/final-inventory.md` + `verification/cycle-2026-11-07-80-20-pass2.md` (2026-11-07). Inventário conservador; Wave 5 residual (T034/T035/T037/smoke completo, auditoria de bundles/claim rows por cenário) permanece pendente.

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
