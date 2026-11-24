# Deep Research: Machine-Pass for 100% of Applicable Paths of 100% of Features (017 Joja / Agrochemical MVP and siblings)

**Feature dir:** `specs/018-machine-pass-full-coverage` (new; this report is the Phase-0 research artifact)
**Date:** 2026-11-07 (post-Phase-13 closure of 017; 127/127 tasks `[x]`)
**Status:** Research complete — implementation waves sequenced below; nothing in this doc promotes any existing claim.
**Skills consulted (via `npx skills use`, not installed):** `cylixlee/skills@stardew-valley-smapi` (SMAPI mod architecture, manifest, debugging, release), `rbergman/dark-matter-marketplace@game-verification` (instrument-blind-spot tagging, determinism, mutation checks, perf/UI-walk rules), `parallel-web/parallel-agent-skills@parallel-deep-research` (requires `parallel-cli`, unavailable in this environment — its guidance was applied manually: multi-source corroboration, freshness caveats, explicit unknowns), `omer-metin/skills-for-antigravity@test-architect` (Java/Spring — irrelevant; not used). `npx skills find` was run for "headless game verification", "SMAPI stardew mod testing", "stardew valley", "e2e end to end verification", "test architect invariants", "multiplayer farmhand verification", "headless game automation", "research deep thorough".
**Web sources reached (live):** `smapi.io` (SMAPI 4.5.2 / SDV 1.6.14+), `docs.junimoserver.com` (dedicated-server E2E harness, REST API, test-harness reference), `github.com/Pathoschild/SMAPI` (repo landing). `stardewvalleywiki.com` returned 403 for the two pages attempted; `websearch` returned no results in this environment. JunimoServer was assessed as **not** the right vehicle for this mod's CI (see §7).

---

## 1. Goal and honest scope

**Goal:** every *applicable* path of every feature that ships runnable code is attested by a
`host-only-machine-pass` (or, where the runner structurally caps the claim, a documented
`partial`/`blocked` result) before tomorrow night's human validation.

**Hard honesty floor (from `specs/010-headless-verification-runner/contracts/test-md-template.md` and
`GOVERNANCE-ANNEX.md`):**
- A **farmhand / split-screen** run is **never** `machine-pass`. It is capped at honesty
  W/R and recorded as `partial`, even on a clean pass with three agreeing signals. The runner's
  `farmhand_breach` detection and the `hostGuard` skip line exercise the **guard branch only**;
  transport (a real second client joining) is not tested.
- `verified` (human E) is never auto-assigned by the runner; only a human may write it.
- `d1-farmhand-convergence` requires a real farmhand session.
- `human-verified` requires an explicit human attestation.
- SC-004 comprehension (≥90%) requires a real participant run.
- Render legibility, a11y, balance (`P=10` provisional), and cross-machine determinism are
  **outside** machine-pass by construction.

**Conclusion up front:** "100% machine-pass for 100% of features" is an **overclaim** for
farmhand/transport paths. The honest, achievable target is:
- **Host-path machine-pass** for every feature × applicable host path (measured / unknown /
  clamped-zero / refusal / conflict / host-authority guard), plus
- **Partial (guard-exercised)** for farmhand paths (split-screen `debug split`), and
- **Automated-only** for everything the runner cannot see (unknown baseline for 014/004, persistence,
  composition), plus
- **Blocked with reason** for D1 transport / human comprehension / render legibility.

This is the bar tomorrow night's human validation completes, not something the runner can
substitute.

---

## 2. What "machine-pass" means in this repository (definition, blind spots)

Source of truth: `specs/010-headless-verification-runner/` (runner base) + `specs/016-gate3-runner-hardening/`
(hardening) + `scripts/lib/*` + `LivingRoots.Verification/`.

### 2.1 Mechanism

1. `scripts/verify-scenario.sh --scenario <file> --no-acquire --game-path … --skip-build` launches
   SMAPI under Xvfb with an isolated save copy (`save_prepare` rewrites `HOME`/`XDG_*`;
   `RUNNER_ORIGINAL_HOME` preserves Python imports).
2. The runner injects a file queue (`commands-<gen>.txt`) of `lr_*` console commands into
   `LivingRoots.Verification`'s `QueueCommandParser`; the mod executes them on the game thread.
3. Three signals are captured:
   - `soilHealthStateDelta` — `soil-health-before-after.json` (before/after/delta per tile)
   - `cropPhaseDelta` — `crop-phase-dump.json` (and `crop-phase-before.json` cross-check vs save)
   - `smapiLog` — full (or excerpt) SMAPI log
4. `PredicateEvaluator` (CLI) checks declared thresholds; `verdict_evaluate` derives
   `pass/fail/errored/partial/skipped` + `flaky`; `promotion.sh` promotes `test.md` to
   `machine-pass` only on a clean, non-flaky host pass with three agreeing signals and honesty
   level ≥ R.
5. Evidence bundle: manifest + detached SHA-256 anchor + claims + retention receipt + verdict +
   artifacts/evaluation/; offline re-check via `--verify-bundle` (exit 0 confirmed for the 017 bundle
   `evidence-bundle-run-20261107T040235Z-589a`).

### 2.2 Scenario contract (schema v3, `contracts/scenario-v3.schema.json`)

- `playerRole`: `host` (default) or `farmhand`.
- `initCommands`: `macro` (file) or `console` (command) — run before steps.
- `steps`: `sleep`, `warp`, `harvest`, `applyCompost`, `insertWaste`, `setSoilHealth`, `observe`,
  `dumpCropPhase`, `runMacro`.
- `thresholds`: `cmp`, `between`, `multiplier`, `sentinel`, `unchanged`, `allOf`/`anyOf`/`not`.
- `expectedSignals`: subset of the three mandatory signals.
- `expectedVerdict`: `pass` (positive) / `fail`/`errored`/`partial`/`skipped` (negative/boundary).
- `determinism`: idempotent + declared sources.
- Honesty level: `R` (runner) / `W` (walk) / `E` (human). `machine-pass` requires `R`+.

### 2.3 Verification console commands (`lr_*`, from `QueueCommandParser.cs`)

| Command | Arity | Purpose |
|---|---|---|
| `lr_set_soil_health <x> <y> <health> [location]` | 3–4 | Seed measured soil (float, incl. `NaN`/`Infinity` via `float.TryParse`); the 017 unknown path is exercisable today. |
| `lr_place_bin <x> <y> [location]` | 2–3 | Place a composting bin (spec 004). |
| `lr_insert_waste <x> <y> <itemId> <count> [location]` | 4–5 | Feed waste into a bin. |
| `lr_give_agrochemical <count>` | 1 | Grant the 017 item into the host inventory (validation-only, host-gated). |
| `lr_apply_agrochemical <x> <y> [location]` | 2–3 | Apply the 017 input (host path; farmhand refused). |
| `lr_plant_crop <x> <y> <seedId> [location]` | 3–4 | Plant a verification-only crop (`LivingRoots.Verification.LongPhaseCrop`). |
| `lr_harvest_matrix <baseX> <baseY>` | 2 | Probe harvest quality at soil 10/50/90 (014). |
| `lr_advance_days <days>` | 1 | Advance the game clock (`_daysRemaining`/`_startNextDayPending`). |
| `lr_dump_crop_phase <label>` | 1 | Dump crop phase/phaseDays/isMainPlayer to `crop-phase-dump.json`. |
| `lr_dump_soil_health <label>` | 1 | Dump soil health to `soil-health-before-after.json`. |
| `lr_quit` | 0 | Terminate the game. |

**Missing for full coverage (implementation gaps, not research):** `lr_give_mulch` / `lr_apply_mulch` (013 mulch), `lr_apply_compost` (currently `applyCompost` is a *step* action, not a console command — compost goes through `lr_place_bin` + `lr_insert_waste` + `lr_advance_days`), `lr_set_farmhand`/`lr_join` (farmhand transport — infeasible single-process), `lr_give_compost` (not needed; bins produce it).

### 2.4 Macros

- Only committed macro: `specs/017-joja-agrochemical-tradeoff/scenarios/macros/agrochemical-apply.txt`.
- 010 fixtures reference `macros/split-screen.txt` and `macros/place-bin.txt` which **do not exist** (gap). The `split-screen` macro is the runner's documented farmhand path (`debug split` split-screen, no second process).
- Macro semantics: flatten `/`-prefixed commands; `/sleep` dropped from the queue; unknown/missing macro or invalid args exit non-zero.

### 2.5 Signal types and verdict classes

- Signals: `soilHealthStateDelta`, `cropPhaseDelta`, `smapiLog`.
- Verdict: `pass`/`fail`/`errored`/`partial`/`skipped` + derived `flaky`. Exit codes 0/18/19/20/21 (+ reserved 22). Promotion to `machine-pass` only on clean non-flaky host pass with 3 agreeing signals, honesty ≥ R.
- Farmhand runs capped at `partial` by design (never `machine-pass`, never `verified`).

### 2.6 Honesty levels

- `R` (runner) — automated headless, host-only by default.
- `W` (walk) — manual walk on real hardware.
- `E` (human) — human attestation.
- Every number reported carries its blind spot (game-verification skill rule 7).

---

## 3. Feature × path inventory (from the repo's own specs)

| Feature | Scenario (schema v) | test.md | Player role | Paths exercised (host) | Paths NOT exercised |
|---|---|---|---|---|---|
| 004 playable compost loop | `compost-growth-hardening.scenario.json` (v2) | machine-pass (run-20261105T053313Z-535d) | host | poor soil (10), compost +15, growth modifier | moderate/healthy compost; farmhand/D1; unknown/clamped (n/a — compost adds, clamps at 100) |
| 005 soil-health-to-crop-growth | `soil-health-growth.scenario.json` (v2) | **partial** (never run) | host | healthy soil (80), growth 0.85× | poor (10) 1.5×, moderate (50) 1.0×; no machine-pass record |
| 013 mulch-soil-cover | **none** | no test.md (pending Gate 3) | n/a | none | everything — no scenario, no commands, no evidence |
| 014 soil-health-crop-quality | `soil-health-harvest-quality.scenario.json` (v3) | machine-pass (run-20261106T014750Z-14b6) | host | harvest matrix at soil 10/50/90 (poor/moderate/healthy tiers) | unknown (NaN) baseline → quality roll; farmhand/D1 (blocked) |
| 017 joja-agrochemical | `agrochemical-tradeoff.scenario.json` (v3) | machine-pass (run-20261107T040235Z-589a @ c9ff596) | host | measured 50 → 40 (P=10), no double-counting | unknown (NaN) baseline → 90; clamped (soil 5 → 0); measured-zero refusal (soil 0); conflict (two applies); farmhand refusal (guard) |

Infra (010 runner, 016 hardening): not features; their own test.md is n/a; fixtures exercise farmhand-host-guard / farmhand-unchanged / farmhand-without-host-guard / health-tier-invalid-value.

## 4. Feasibility classification (machine-pass vs partial vs blocked)

| Path | Feasible class | Why | Required work |
|---|---|---|---|
| Host, measured soil, any feature with a scenario | `machine-pass` | runner supports it today | scenario + thresholds + run |
| Host, unknown (NaN) soil, 017 | `machine-pass` | `lr_set_soil_health` accepts `NaN` (float.TryParse); threshold multiplier factor = post/baseline = 90/100 = 0.9 | new scenario + macro |
| Host, clamped (soil 5 → 0), 017 | `machine-pass` | set soil 5, apply, post = 0; threshold multiplier factor 0.0 with tolerance 0.0001 | new scenario |
| Host, measured-zero refusal (soil 0), 017 | `partial` or `fail`-scenario | refusal leaves soil unchanged → soilHealthStateDelta multiplier 1.0; smapiLog asserts refusal key | new negative scenario with `expectedVerdict: fail`/`partial` |
| Host, conflict (two applies same tile), 017 | `partial`/`fail` scenario | second apply refused; soil unchanged (factor 1.0); smapiLog refusal | new scenario with two apply steps |
| Host, unknown baseline, 014 (harvest) | `machine-pass` possible | set NaN + `lr_harvest_matrix`; threshold on quality roll (cmp) | new scenario; verify the harvest service's Unknown-tier behavior first |
| Host, measured-zero, 014 | n/a (harvest doesn't mutate soil) | soil is read, not written; "clamped" concept doesn't apply to harvest | not a distinct path |
| Host, poor/moderate/healthy, 004/005/014 | `machine-pass` | scenarios + thresholds exist or need variants | small work |
| Host, compost apply + decay, 004 | `machine-pass` | `lr_place_bin` + `lr_insert_waste` + `lr_advance_days` + dump | existing scenario covers poor only; add moderate/healthy |
| Host, mulch apply + decay exemption, 013 | `machine-pass` (after work) | needs `lr_give_mulch` + `lr_apply_mulch` verification commands (Phase-8-style) + scenario + macro | medium work (new commands + service seams) |
| Farmhand refusal (any feature) | **`partial` only — never `machine-pass`** | split-screen `debug split` flips `Context.IsMainPlayer` to false without a second process; exercises the guard branch | create `macros/split-screen.txt`; thresholds assert `isMainPlayer eq false` + no mutation (factor 1.0); honesty capped at W/R |
| Farmhand transport (D1) | **`blocked`** | needs a real second game client joining a host; JunimoServer-class harness requires Docker + Steam + remote VPS; our in-repo runner is single-process | document as blocked (current honest stance); human validation covers it |
| Human comprehension (SC-004) | **n/a — qualitative** | participant run with denominator/pass count; never machine-pass | run the `sc004-v1` protocol |
| Render legibility / a11y | **n/a — felt** | only a person can see it | manual walk |
| Balance (P=10) | **n/a — assumed** | no instrument measures "fun" | playtest |

**Honesty floor:** farmhand paths are capped at `partial` by the runner's own promotion rules (GOVERNANCE-ANNEX: "A farmhand or split-screen run can never satisfy the machine-pass condition"). Any claim of `machine-pass` for a farmhand path would be a promotion violation. The honest target for "100% of paths" is therefore "host machine-pass + farmhand partial + automated-only + blocked-with-reason", not literal 100% machine-pass. Tomorrow night's human validation supplies the `verified`/`felt` layer.

## 5. Concrete work (waves)

### Wave 0 — runner infra (prerequisite for everything below)
- **W0.1** Create `specs/010-headless-verification-runner/fixtures/valid/macros/split-screen.txt` (and `specs/.../scenarios/macros/split-screen.txt` where needed) with the `debug split` command. **Feasibility must be tested first** — run one farmhand-host-guard fixture end-to-end; if `debug split` fails headless, record the failure and fall back to (a) documenting farmhand paths as `blocked` (current stance) or (b) a two-instance harness (out of scope for this repo's CI). Effort: 0.5–1 day + a test run.
- **W0.2** Create `macros/place-bin.txt` referenced by 010 fixtures (content: `lr_place_bin …` + `lr_insert_waste …` or whatever the fixture expects; read `actions-coverage.scenario.json` to derive). Effort: 0.5 day.
- **W0.3** Fix `environment.modVersion` declarations: scenario files say `0.4.0`; manifest is `0.1.0` (post-release) / was `0.0.1`. Either set to `0.1.0` everywhere or document the field as declared-not-observed (already disclosed in T110). Effort: 0.5 day (sed + validator re-run). Note: runner copies it verbatim; never validated; no runtime effect.
- **W0.4** (optional, for hygiene) wire `scripts/tests/test-017-claims.sh` into `run-all.sh` (already auto-discovered by glob `test-*.sh` — verify). Effort: trivial.

### Wave 1 — 017 paths (highest value, already partially there)
- **W1.1** Scenario `agrochemical-unknown-baseline.scenario.json`: init `lr_set_soil_health 60 15 NaN`; apply once; threshold `soilHealthStateDelta` multiplier factor `0.9` (90/100) with tolerance 0.0001; `cropPhaseDelta` multiplier 1.0; `smapiLog` cmp `isMainPlayer` eq true. Reuse `macros/agrochemical-apply.txt`.
- **W1.2** Scenario `agrochemical-clamped.scenario.json`: init soil 5; apply once; threshold multiplier factor `0.0` (post=0) tolerance 0.0001. Reuse macro.
- **W1.3** Scenario `agrochemical-refused-at-minimum.scenario.json`: init soil 0; apply once; `expectedVerdict: fail` or `partial` (runner caps refusal at partial); thresholds: `soilHealthStateDelta` multiplier 1.0 (unchanged), `smapiLog` refusal-key present. Reuse macro.
- **W1.4** Scenario `agrochemical-conflict.scenario.json`: init soil 50; two `lr_apply_agrochemical` steps back-to-back; second refused; soil unchanged (factor 1.0); smapiLog refusal. Needs a two-apply macro variant.
- **W1.5** Scenario `agrochemical-farmhand-guard.scenario.json`: playerRole `farmhand`, `initCommands` macro `split-screen.txt` (depends on W0.1), then apply; threshold `smapiLog` cmp `isMainPlayer` eq false; expectedVerdict `partial` (capped). Never `machine-pass`.
- **W1.6** Run each via `verify-scenario.sh`; record verdict; if clean host pass, promote `test.md` via runner only; append claim rows append-only; keep D1/human/SC-004 pending.
- Effort: ~1 day agent work + runs (each run ~5–10 min under Xvfb).

### Wave 2 — 005 and 014 paths
- **W2.1** 005: run the existing `soil-health-growth.scenario.json` (currently `partial` → never run) → promote if clean. Effort: 1 run.
- **W2.2** 005 variants: poor soil (10, factor 1.5×) and moderate (50, factor 1.0×) scenarios mirroring the healthy one; thresholds already defined by the existing scenario pattern. Effort: 0.5 day authoring + 2 runs.
- **W2.3** 014: unknown-baseline harvest scenario — `lr_set_soil_health 60 15 NaN` then `lr_harvest_matrix 60 15`; threshold on the quality roll (cmp) for the Unknown tier's outcome (verify `HarvestPatches`/`IHarvestQualityService` handling of NaN first — read code; if Unknown is treated as Poor, assert poor-tier outcomes). Effort: 0.5 day + 1 run.
- **W2.4** 014: measured-zero harvest scenario (soil 0 + harvest_matrix) — assert poor-tier outcomes; no soil mutation. Effort: 0.5 day + 1 run.
- **W2.5** 014 farmhand-guard scenario (split-screen, partial, guard-exercise) if W0.1 succeeds.

### Wave 3 — 004 paths
- **W3.1** 004: moderate (50) and healthy (90) compost scenarios mirroring `compost-growth-hardening.scenario.json` (poor 10). Thresholds: phaseDays multiplier (compost +15 → 1.15× on poor; on healthy the +15 clamps at 100 → multiplier 1.0 on phaseDays? Actually phaseDays modifier is separate from soil health; verify the growth modifier formula). Effort: 1 day + 2 runs.
- **W3.2** 004 farmhand-guard scenario (split-screen, partial) if W0.1 succeeds.

### Wave 4 — 013 mulch (largest)
- **W4.1** Implement verification commands in `LivingRoots.Verification`: `lr_give_mulch <count>` (grant mulch item into host inventory, host-gated, like `GrantAgrochemical`) and `lr_apply_mulch <x> <y> [location]` (right-click mulch path via `ModController`/`IMulchApplicationService.TryApplyMulch`, like `api.ApplyAgrochemical`). Effort: 0.5–1 day.
- **W4.2** Extend `ILivingRootsModApi` + `LivingRootsModApi` + `ModEntry` wiring with host-only `ApplyMulch(locationName, tileX, tileY)` delegating to the mulch route (Phase-8 pattern). Effort: 0.5 day.
- **W4.3** Scenario `mulch-decay-exemption.scenario.json`: plant crop, set soil 50, `lr_give_mulch 1`, `lr_apply_mulch`, advance 2 days, dump crop phase; threshold cropPhaseDelta multiplier 1.0 (mulch exempts from decay → no phaseDays loss) OR soilHealthStateDelta unchanged (mulch doesn't mutate soil). Define thresholds against the actual `MulchApplicationService` behavior (read it first). Effort: 1 day + 1 run.
- **W4.4** Scenario `mulch-refused-on-ineligible.scenario.json`: ineligible target → refusal (smapiLog threshold). Effort: 0.5 day + 1 run.
- **W4.5** Farmhand-guard scenario (split-screen, partial) if W0.1 succeeds.

### Wave 5 — evidence + honesty closure
- **W5.1** For each new machine-pass: append claim rows to `claim-records.md` (append-only), run `--verify-bundle`, update `test.md` only via runner promotion, update `quickstart.md`/`summary.md` addenda (append-only), add `verification/<feature>-gate3.md` run records.
- **W5.2** Run the claims validator; run the smoke suite; record outcomes honestly (never promote above evidence).
- **W5.3** Update `specs/010` fixtures: create the missing `macros/split-screen.txt` and `macros/place-bin.txt` (or fix fixture references), re-run `test-017-agrochemical.sh`-style validation for 010.
- **W5.4** Refresh `modVersion` declarations to 0.1.0 (or document declared-not-observed) — all scenario files + 010 fixtures.
- **W5.5** Final honesty review: re-run `verify-scenario.sh --check-docs`; confirm no claim promoted above evidence.

### Effort estimate
- Wave 0: 1–2 days (mostly testing `debug split` headless).
- Wave 1: 1 day.
- Wave 2: 1–1.5 days.
- Wave 3: 1 day.
- Wave 4: 2–3 days (mulch commands + scenario + farmhand).
- Wave 5: 1 day.
- **Total: ~7–9 working days of agent work**, mostly scenario authoring + verification-command implementation + runs (each run 5–10 min under Xvfb).

### What is NOT machine-passable (honesty floor — do not attempt to fabricate)
- **Farmhand/D1 transport** — never `machine-pass` (capped `partial`); a real second client is required for transport claims. Use split-screen guard-exercise (partial) + unit-level stubs + tomorrow's human validation.
- **SC-004 comprehension** — qualitative; protocol `sc004-v1`, never run; tomorrow night's human validation.
- **Render legibility / a11y / font fit** — only a person can see it.
- **Balance (P=10)** — no instrument measures "fun"/temptation; playtest.
- **Cross-machine determinism** — only same-machine tested.
- **C2 unknown-baseline end-to-end** — feasible for 017 (NaN parses); for 014/004 the harvest service's Unknown-tier behavior must be verified first, and if it's not a clean assertion, record `planned-run-not-executed` with reason rather than fabricate.

## 6. Honesty constraints for the report (tag every claim)

Per the `game-verification` skill: every number reported carries its instrument's blind spot. Apply the three tags:
- **Verified** — an artifact exists (test, log, capture), with a stated way it could have failed.
- **Felt** — a person played it and reported a reaction (who, when).
- **Assumed** — reasoning only, labelled as such.

Machine-pass claims are `verified` about what the runner can see and `assumed` about everything in the right-hand column of the instrument table. Tomorrow night's human validation supplies `felt` for legibility/comprehension/fun and `verified` for farmhand transport (if a second client is exercised).

## 7. Tools/capabilities used for this research

- Repo: `specs/010-headless-verification-runner/` (runner base, schema v3, contracts, fixtures, GOVERNANCE-ANNEX), `specs/016-gate3-runner-hardening/` (hardening), `scripts/lib/*` (signals, verdict, injection, evidence, promotion, scenario-validate), `LivingRoots.Verification/Queue/QueueCommandParser.cs`, `LivingRoots.Verification/ModEntry.cs`, `LivingRoots/Controllers/ModController.cs` (mulch/compost/agrochemical routes), `LivingRoots/Services/MulchApplicationService.cs`, `LivingRoots/Services/CompostApplicationService.cs`, `LivingRoots/Services/SoilHealthService.cs`, `LivingRoots/Constants.cs`, all feature specs/quickstarts/verification artifacts.
- Skills (via `npx skills use`, not installed): `cylixlee/skills@stardew-valley-smapi` (SMAPI mod architecture, manifest, debugging, release, cross-platform), `rbergman/dark-matter-marketplace@game-verification` (instrument table, determinism, mutation checks, real-input rules, blind-spot tagging, perf/UI-walk rules — browser-game specific but the principles transfer), `parallel-web/parallel-agent-skills@parallel-deep-research` (requires `parallel-cli`, unavailable — its multi-source corroboration + freshness + explicit-unknowns methodology applied manually), `omer-metin/skills-for-antigravity@test-architect` (Java/Spring — irrelevant to this stack, not used). `npx skills find` executed for 8 queries.
- Web (live): `smapi.io` (SMAPI 4.5.2 / SDV 1.6.14+; log parser; JSON validator), `docs.junimoserver.com` (dedicated-server E2E harness, REST API, test harness, remote-host setup), `github.com/Pathoschild/SMAPI` (repo landing). `stardewvalleywiki.com` returned 403 for both attempted pages; `websearch` returned no results in this environment. JunimoServer assessed as **not** the right vehicle for this mod's CI (Docker + Steam + remote VPS; overkill; our runner is lighter and already works). The `game-verification` skill's principles (determinism, mutation check, real input, blind spots, tag-every-claim) transfer to our runner; its browser-specific instruments (CDP, Playwright, WebGL) do not apply.

## 8. Tomorrow night's human-validation checklist (what machine-pass cannot cover)

For each feature (004, 005, 013, 014, 017), in order of risk:
1. **Comprehension (SC-004)** — run `sc004-v1` protocol: defined participant/context scope; first-use timing (before or immediately after first supported use); prompt asking participants to identify both (a) short-term shortcut/benefit and (b) soil-health cost; binary pass/fail requiring both; evidence note with sample size/denominator, pass count, timing, scope note; target ≥90% pass. Record denominator + pass count. Do not claim complete without a recorded run.
2. **Farmhand transport (D1)** — two real clients: host applies agrochemical/compost/mulch; farmhand observes consistent soil health via `SoilHealthSyncMessage`; farmhand attempt on unsupported path is refused locally with no unsafe mutation; concurrent conflict → first-wins refusal. Record as `d1-farmhand-convergence` only with real evidence.
3. **Render legibility** — item icon (vanilla springobjects fallback), HUD feedback, before-use description, refusal messages ×3 locales, tone. Confirm no color/asset-only meaning. Record per `verification/manual-review.md` (append-only addendum).
4. **Balance/temptation** — is P=10 (provisional) tempting? Is the refusal at minimum health (RefusedSoilAtMinimum) acceptable? Decide whether stronger mechanical short-term benefits are needed (follow-up only if specified with explicit no-double-counting).
5. **Acquisition path** — confirm the descoped (blocked) decision: no in-game acquisition for MVP; item is debug/verification-grant only. Decide whether to ship a scoped acquisition path or keep descoped for 0.2.0.
6. **Measured-zero persistence** — confirm the documented contradiction (sparse-cache zero omission) is acceptable or schedule the persist-zeros follow-up.
7. **Mulch decay exemption** — in-game confirmation that mulched bare tilled tiles don't decay (spec 013).
8. **Compost recovery after agrochemical harm** — confirm regenerative play recovers (US1/AC3).
9. **Tier-boundary crossing** — soil near 31/71 after penalty: confirm category change follows normal rules.

Each item recorded with date, observer, and the honesty tag (verified/felt/assumed) per the game-verification skill.

## 9. Deliverable

This report is the research artifact. The actionable implementation plan is in `plan.md` and `tasks.md` (below). No code was changed by this research; no claims are promoted; no verification artifact is rewritten. The `017` machine-pass (host-only, measured 50→40, honesty R, commit c9ff596) remains the current attestation; the post-Phase-11/12 evidence fidelity work (T119–T127) closed all documentation gaps except the maintainer-decision items (T123, and now the mulch-command scope decision in Wave 4). Tomorrow night's human validation is the next attestation layer (E), not a machine-pass substitute.
