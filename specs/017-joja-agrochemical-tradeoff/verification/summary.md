# Implementation & Verification Summary — Spec 017 (Joja / Agrochemical MVP)

**Date:** 2026-11-06  
**Purpose:** Evidence-derived final status for the 017 MVP. Every statement below is drawn from
recorded Phase 7 artifacts (T048–T054). This summary is not an independent attestation and does
not upgrade any claim beyond its recorded evidence class.

**Source artifacts:**

- [`automated-tests.md`](automated-tests.md) + `automated-tests-targeted.log`, `automated-tests-full.log` (T048, T050)
- [`persistence-roundtrip.md`](persistence-roundtrip.md) + `persistence-roundtrip.log` (T049)
- [`static-checks.md`](static-checks.md) + `static-checks.log` (T051)
- [`manual-review.md`](manual-review.md) (T052)
- [`claim-records.md`](claim-records.md) (T053)
- [`honesty-review.md`](honesty-review.md) (T054)
- [`provisioning-validation.md`](provisioning-validation.md), [`host-only-gate3.md`](host-only-gate3.md), [`sc004-comprehension-protocol.md`](sc004-comprehension-protocol.md) (Phase 6 context)

## Status by claim

| Claim | Evidence class | Recorded status | What the evidence establishes |
|---|---|---|---|
| Targeted 017 service/controller contract and invariant tests | `automated` | **complete** | 60/60 cases passed on the final tree: 13 service, 3 persistence, 19 controller, 25 content parity. Exit code 0. |
| Content key presence/parity and lexical tone proxies | `automated` | **complete** | 25/25 content-parity cases pass across `default`/`en`/`pt`. Lexical proxy only; semantic meaning is not proven by this claim. |
| Full solution regression suite with 017 included | `automated` | **complete** | 1816 total: 1812 passed, 0 failed, 4 skipped. The 4 skips are pre-existing spec 009 deferred harvest-hook tests. |
| Persistence round-trip through `ISoilHealthService.SaveData`/`LoadData` (mocked `IModDataService`) | `automated` (mocked seam) | **complete** | 3/3 cases pass. C2 unknown baseline applies as measured `90` for `P=10`; measured `50` reloads as `40`. A clamped-to-zero tile is omitted by the existing sparse cache and reloads as unknown. |
| Structured copy review of touched 017 keys | `qualitative-follow-up` (AI-assisted) | **complete** | 10 keys × 3 locales reviewed for shortcut/cost/attribution/refusal meaning and cozy/non-punitive tone. Not human attestation; does not prove in-game rendering. |
| Formatting gate | `automated` (static) | **complete** | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` exits 0. Build is exercised by the passing test runs. |
| Host-only Gate 3 machine-pass | `planned-run-not-executed` | **infeasible** | No 017 scenario can be expressed: the runner has no agrochemical action, item-grant path, or right-click simulation. No log/bundle/verdict exists. Not a pass. |
| D1 farmhand convergence | `d1-farmhand-convergence` | **pending** | No real farmhand session was recorded. |
| Human `verified` attestation | `human-verified` | **pending** | No human attestation exists. Automated, AI-assisted copy review, and any host-only machine evidence do not satisfy it. |
| SC-004 comprehension (≥90% identify shortcut and cost) | `qualitative-follow-up` | **pending** | Protocol finalized (`sc004-v1`) but no participant run exists. Protocol text is not evidence. |
| Manual/playtest S1–S5 provisioning | n/a (prerequisite) | **blocked** | The scoped `lr_give_agrochemical` console grant is documented but not implemented; no manual run was performed. |

## Implemented behavior covered by the automated claims

- One mod-intercepted Joja input (`LivingRoots.JojaAgrochemical` / `(O)LivingRoots.JojaAgrochemical`),
  `Type="Basic"`, no `Data/Shops` entry, no `HoeDirt.fertilizer` write, and no 017 Harmony hook.
- Bounded soil penalty using the provisional `P=10`; measured baselines clamp at `[0,100]`, an
  unknown (`NaN`) baseline uses `100` and reports `BaselineWasUnknown`, and a measured `0` refuses
  without writing.
- Controller ordering: authority → target → input → per-tile first-wins lock → service call →
  lock release → consume-after-commit → feedback → FR-009 soil-health broadcast.
- Refusals (unsupported authority, invalid target, missing input, conflict, soil at minimum) do not
  consume the item, mutate soil, emit false-success feedback, or broadcast.
- Localized item name/description and all success/refusal messages ship in `default.json`,
  `en.json`, and `pt.json`; the item registration references the translation keys with a safe
  fallback.

## Honesty boundaries

- **No host-only machine pass, human verification, D1, or SC-004 completion is claimed.**
- Host-only Gate 3 is `infeasible` and recorded as `planned-run-not-executed`, not a pass.
- The persistence claim is limited to the mocked save/load seam. Real disk persistence, save-file
  compatibility, and real-game behavior are not proven.
- The copy review was performed by the implementation agent, not an independent human.
- `P=10` remains provisional and tunable; no final balance claim is made.
- The C1 test is a service-dependency boundary check, not a full harvest/growth composition proof.
- Fallback-only tests cannot promote FR-007, FR-013, or SC-002 to satisfied.
- The RED gate (`red-phase.md` + `red-phase-run.log`) is historical failing-run evidence, not a
  passing result. An interim full-suite failure (unreferenced translation-key constants, fixed in
  `5dd838d`) is disclosed in `automated-tests.md`; the final-tree run is the recorded evidence.
- Prior verification artifacts were not rewritten or retroactively promoted.

## Remaining work to close the open claims

1. Implement the scoped `lr_give_agrochemical` verification-only console command (or record a
   different documented mechanism) to unblock manual S1–S5 runs.
2. Run a real host-only session once the runner can express the interaction; record a new bundle
   and classify it as `host-only-machine-pass` only.
3. Run a real farmhand session for D1 convergence.
4. Obtain an explicit human `verified` attestation.
5. Execute the `sc004-v1` protocol with at least the recorded denominator and append the evidence
   note.
