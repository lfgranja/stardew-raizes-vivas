# Design (for approval): harness/schema evolution — cropPhaseDelta-optional / bare-tile soil proofs (013 SC-004, 7-day expiry)

**Date:** 2026-11-07  
**Status:** **PROPOSED — requires explicit design/approval before implementation** (trust-root change: 010 runner schema/validator/verdict/signals).  
**MDMV flow:** assess/decide (autonomous) → design (this doc) → **explicit approval gate** → plan/tasks/implement (TDD, append-only evidence) → verify (runner + smoke/010 tests) → converge.

## 1) Problem (evidence-backed, defensible)
013 mulch decay exemption/expiry is implemented (`MulchDurationDays=7`, `MulchApplicationService.TickMulch` decrements first; `SoilDecayService` ticks mulch for all HoeDirt then applies decay only to **bare** `HoeDirt` where `crop==null`, `IsMulched => MulchDecayMultiplier=0`; spring `DailyDecayRate*0.5=1/day`). Defensible timing (no guessing): 6 day-start ticks keep mulch active during decay checks (7→6…2→1), 7th tick removes before decay → decay applies day 7 (50→49 over 7 advances).

**Machine-pass blocked under v3/v2:** `contracts/scenario-v3.schema.json` requires exactly 3 `expectedSignals` `[soilHealthStateDelta,cropPhaseDelta,smapiLog]` (min/max/unique). `scenario-validate.sh` enforces schema/rules; `signals.sh` requires all 3 artifacts (`verdict.sh`/`verdict_evaluate`: missing `signals_complete` → `partial`). A **bare** HoeDirt target (required for the actual decay path) has **no crop**, so `cropPhaseDelta` cannot be honestly attested for the observed target. Multi-day `phaseDays` on a secondary/cropped workaround cannot be asserted without guessing thresholds; cropped primary makes soil 1.0x non-probative. **We will not fabricate or guess.**

## 2) Goal / non-goals
- **Goal:** enable **defensible, no-guess** runner `machine-pass` for bare-tile soil proofs (013 SC-004, 7-day/SC-005) by making `cropPhaseDelta` **explicitly optional only under a constrained, declared policy**, while preserving 3-signal as the default/strong path.
- **Non-goals:** weaken evidence integrity, allow dropping signals to force passes, change production gameplay, or make farmhand/split-screen `machine-pass`. No retroactive promotion.

## 3) Options considered
| Option | Pros | Cons / risk |
|---|---|---|
| **A. v4 constrained optional cropPhaseDelta** (recommended) | Immutable new schema; backward-compatible (v2/v3 unchanged, runner selects by `schemaVersion`); explicit opt-in; minimal harness change; enables bare-tile proofs | Trust-root change; must update validator/signals/verdict/010 tests; slightly weaker evidence when opted-in (must be declared/honest) |
| B. Multi-target signal binding | Preserves 3 signals conceptually | Large refactor (evaluator/pre/threshold target binding), higher regression risk, 80/20 poor |
| C. Sentinel/dummy cropPhase on bare tile | Avoids schema change? | Dishonest/hacky; breaks semantics; rejected |

## 4) Proposed design (Option A, **v4**)

### 4.1 Contract (new immutable schema)
- Add `specs/010-headless-verification-runner/contracts/scenario-v4.schema.json` with new `$id` (URN, versioned, not reused), `schemaVersion: const 4`. **Keep v2/v3 files/behavior unchanged.**
- `expectedSignals`: allow `[soilHealthStateDelta, smapiLog]` **or** `[soilHealthStateDelta, cropPhaseDelta, smapiLog]` (and order-insensitive set equality semantics enforced by validator; schema uses `contains`/`minItems`/`maxItems`/`uniqueItems`). **`soilHealthStateDelta` and `smapiLog` always required.** `cropPhaseDelta` optional.
- New optional property `signalPolicy` (strict object) with:
  - `cropPhaseDelta` ∈ `{ "required", "not-applicable" }`, default `"required"`.
  - `not-applicableReason` (string, minLength 10) **required when** `cropPhaseDelta == "not-applicable"` (forces explicit justification, e.g. bare HoeDirt/no-crop decay-path target).
- **Constraint:** if `expectedSignals` omits `cropPhaseDelta`, then `signalPolicy.cropPhaseDelta` MUST be `"not-applicable"` and reason present. If `cropPhaseDelta` present, policy must be `"required"` or absent. (validator cross-field; schema `if/then` where possible.)
- `expectedSignals` in evidence manifest/claims remains the **declared** set (honest).

### 4.2 Validator (`scripts/lib/scenario-validate.sh`)
- Select schema: v4→`scenario-v4.schema.json`, v3→v3, else v2 (existing pattern). Validate via jsonschema when available.
- Enforce cross-field `expectedSignals` ↔ `signalPolicy.cropPhaseDelta`/reason; fail `/expectedSignals` or `/signalPolicy` (exit 14) with explicit path.
- Thresholds: if `cropPhaseDelta` not in declared `expectedSignals`, reject any threshold with `signal:"cropPhaseDelta"` as a contract error (`/thresholds/<t>/signal`) — prevents contradictory scenarios.
- v2/v3 behavior unchanged.

### 4.3 Signals (`scripts/lib/signals.sh`)
- **Completeness computed from declared `expectedSignals`** (read from scenario/manifest input), not hardcoded 3. Always require `soilHealthStateDelta` + `smapiLog` artifacts. Require `cropPhaseDelta` artifact **only if declared** (or policy required).
- When `cropPhaseDelta` not declared/applicable: do **not** emit `missing crop phase dump` anomaly; do not fail completeness for absent crop dump. If a crop dump exists, it may still be recorded as an artifact (informational) but is not required for completeness.
- Keep existing anomaly semantics for declared signals (divergence informational as today).

### 4.4 Verdict (`scripts/lib/verdict.sh`)
- `verdict_evaluate`/completeness inputs remain `(signals_complete, signals_agree, thresholds_ok, expected, farmhand_breach)`. `signals_complete` now means **all declared expected signals** complete (including the 2-signal not-applicable case). `signals_agree` must ignore cropPhaseDelta when not declared (no false failure).
- Threshold evaluation unchanged (operates over declared thresholds); combined with validator rule, no crop thresholds in 2-signal mode.

### 4.5 Runner/docs/tests (010/016)
- `scripts/verify-scenario.sh` + `scripts/lib/scenario.sh`/manifest plumbing: pass declared expected signals/policy through; no behavior change for v2/v3.
- `scripts/lib/evidence.sh` manifest: `expectedSignals` reflects declared set (already copied from scenario/env context patterns — verify).
- Update/extend 010 fixture/smoke tests: add **v4 valid** (2-signal bare-tile policy) and **v4 invalid** (omit cropPhaseDelta without policy; cropPhaseDelta threshold while not declared; missing reason). Ensure existing v2/v3 fixtures still pass (regression). 016 red-gate/TDD policy: add tests first for new rules (regression guards), mark unverified/TDD status honestly per deviation policy.
- `scripts/tests/run-all.sh` / 010 fixture suite: must remain green.

### 4.6 013 scenarios enabled (post-implementation, defensible)
- `mulch-bare-tile-decay-exemption` (SC-004): v4, `expectedSignals [soilHealthStateDelta,smapiLog]`, `signalPolicy.cropPhaseDelta=not-applicable` (reason: bare HoeDirt no-crop decay-path target). Init `lr_till`+soil 50 (spring 1/day), apply mulch, advance 6 days → **soil 1.0x (50)**. 3rd signal replaced by explicit not-applicable policy; soil+smapi prove no decay.
- `mulch-7day-expiry` (SC-005): same setup, advance **7 days** → **soil factor 0.98** (50→49, tick removes before decay on day 7; defensible). Tolerance 0.0001.
- Both: `host`, `isMainPlayer`, 2 thresholds, honest summaries. **No guessing.**

## 5) Risks & mitigations
- **Evidence weakening:** mitigated by explicit `signalPolicy`+reason, validator rejection of contradictory thresholds, and keeping 3-signal default; 2-signal mode only for declared not-applicable (bare-tile) cases.
- **Regression in 010/016:** mitigated by keeping v2/v3 immutable, adding v4-specific fixtures/tests, running `SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh` and 010 fixture validation.
- **Trust root change:** requires explicit approval; TDD/regression guards; no silent loosening.

## 6) Acceptance criteria (implementation, if approved)
1. v2/v3 scenarios validate and run unchanged (no regression).
2. v4 2-signal scenario with valid `signalPolicy.not-applicable` + reason validates (exit 0) and can achieve runner `machine-pass` when declared signals complete/agree/thresholds pass.
3. v4 invalid cases rejected (omit cropPhaseDelta without policy/reason; crop threshold when crop not declared).
4. `SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh` green; relevant 010 fixture checks green.
5. 013 SC-004 + 7-day expiry scenarios run sequentially; promoted **only if** runner verdict is `pass` with honest claims. Append-only evidence.

## 7) Explicit approval requested
I can implement this **only with your explicit approval** (trust-root/schema evolution). Please reply with one of:
- **"APPROVE v4 implementation"** (authorize implementing Option A as specified, TDD/verification before completion), or
- requested changes/constraints, or
- **"REJECT"** (keep 013 SC-004/7-day as implementation-verified/blocked with the defensible analysis already recorded).

*(Autonomous work completed now: research, code-timing verification, harness contract analysis, design. No implementation performed.)*
