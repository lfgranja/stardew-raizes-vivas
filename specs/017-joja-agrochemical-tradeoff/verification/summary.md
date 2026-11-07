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
- [`provisioning-validation.md`](provisioning-validation.md), [`host-only-gate3.md`](host-only-gate3.md), [`host-only-gate3-run.md`](host-only-gate3-run.md), `../evidence-bundle-run-20261107T015936Z-7f35/`, [`sc004-comprehension-protocol.md`](sc004-comprehension-protocol.md) (Phase 6/8 context and run evidence)

## Status by claim

| Claim | Evidence class | Recorded status | What the evidence establishes |
|---|---|---|---|
| Targeted 017 service/controller contract and invariant tests | `automated` | **complete** | 60/60 cases passed on the final tree: 13 service, 3 persistence, 19 controller, 25 content parity. Exit code 0. |
| Content key presence/parity and lexical tone proxies | `automated` | **complete** | 25/25 content-parity cases pass across `default`/`en`/`pt`. Lexical proxy only; semantic meaning is not proven by this claim. |
| Full solution regression suite with 017 included | `automated` | **complete** | 1816 total: 1812 passed, 0 failed, 4 skipped. The 4 skips are pre-existing spec 009 deferred harvest-hook tests. |
| Persistence round-trip through `ISoilHealthService.SaveData`/`LoadData` (mocked `IModDataService`) | `automated` (mocked seam) | **complete** | 3/3 cases pass. C2 unknown baseline applies as measured `90` for `P=10`; measured `50` reloads as `40`. A clamped-to-zero tile is omitted by the existing sparse cache and reloads as unknown. |
| Structured copy review of touched 017 keys | `qualitative-follow-up` (AI-assisted) | **complete** | 10 keys × 3 locales reviewed for shortcut/cost/attribution/refusal meaning and cozy/non-punitive tone. Not human attestation; does not prove in-game rendering. |
| Formatting gate | `automated` (static) | **complete** | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` exits 0. Build is exercised by the passing test runs. |
| Host-only Gate 3 machine-pass | `host-only-machine-pass` | **complete** | A clean, non-flaky **host** run passed the 016-hardened runner: measured 50 → 40 after one provisional `P=10`, `phaseDays` unchanged, host `isMainPlayer` true; three signals present and agreeing; 3/3 thresholds pass; offline verifier exit 0 (`verification/host-only-gate3-run.md`, `evidence-bundle-run-20261107T015936Z-7f35/`, `test.md` `machine-pass`, honesty `R`). Not D1/human/SC-004; does not cover the C2 unknown-`NaN` baseline path. |
| D1 farmhand convergence | `d1-farmhand-convergence` | **pending** | No real farmhand session was recorded. |
| Human `verified` attestation | `human-verified` | **pending** | No human attestation exists. Automated, AI-assisted copy review, and any host-only machine evidence do not satisfy it. |
| SC-004 comprehension (≥90% identify shortcut and cost) | `qualitative-follow-up` | **pending** | Protocol finalized (`sc004-v1`) but no participant run exists. Protocol text is not evidence. |
| Manual/playtest S1–S5 provisioning | n/a (prerequisite) | **blocked** | The validation-only grant is implemented for the verification queue (`lr_give_agrochemical`), not as a player-facing spawn/shop/grant. No manual S1–S5 run was performed. |

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

- **No human verification, D1, or SC-004 completion is claimed.**
- The host-only Gate 3 run is recorded as `host-only-machine-pass` only (not `verified`/D1/SC-004), and only covers the measured 50 → 40 path; the C2 unknown-`NaN` baseline path is covered by automated tests, not by this run.
- The persistence claim is limited to the mocked save/load seam. Real disk persistence beyond the runner's isolated save copy is not proven outside the Gate 3 run.
- The copy review was performed by the implementation agent, not an independent human.
- `P=10` remains provisional and tunable; no final balance claim is made.
- The C1 test is a service-dependency boundary check, not a full harvest/growth composition proof.
- Fallback-only tests cannot promote FR-007, FR-013, or SC-002 to satisfied.
- The RED gate (`red-phase.md` + `red-phase-run.log`) is historical failing-run evidence, not a passing result. An interim full-suite failure (unreferenced translation-key constants, fixed in `5dd838d`) is disclosed in `automated-tests.md`; the final-tree run is the recorded evidence.
- Prior verification artifacts were not rewritten or retroactively promoted; the Phase 8 run supersedes the 2026-11-06 infeasibility record but does not rewrite it. The three failed attempts are recorded in `host-only-gate3-run.md` and are not cited as passing evidence.

## Remaining work to close the open claims

1. Run a real farmhand session for D1 convergence.
2. Obtain an explicit human `verified` attestation.
3. Execute the `sc004-v1` protocol with at least the recorded denominator and append the evidence note.
4. If a player-facing manual S1–S5 playtest is desired, add a scoped player-visible provisioning path (or record the ad hoc grant as a deviation); the verification-queue grant is not a player-facing path.

## Phase 9 addendum (2026-11-06) — scope and counts for the convergence tree

**Append-only; the summary above is unchanged.** This summary remains the Phase-7/8
attestation. The host-only Gate 3 row above is **scoped to commit `e5882c1`** (the Phase-8
runner-wiring tree; `test.md` records the machine-pass at that commit). Phase 9 (T064–T089)
changed the commit path (`TrySetSoilHealth` seam, refusal-result honesty, i18n copy,
registration wiring), so the Phase-7/8 counts and the e5882c1-era machine-pass must not be read
as attesting the post-Phase-9 tree. The current-tree attestation lives in
[`convergence-rerun.md`](convergence-rerun.md) and the T091/T090 updates (targeted-log retention
repair and, once run, the post-Phase-9 host-only re-run record).

Re-stated counts for the post-Phase-9 tree (per `convergence-rerun.md` and the Phase 9
closure): targeted 017 source-enumerated **88** cases (AgrochemicalApplicationServiceTests 16,
AgrochemicalPersistenceTests 4, ModControllerAgrochemicalTests 20, AgrochemicalContentParityTests
45, AgrochemicalRegistrationTests 3) — the 2026-11-06 re-run reported 92/92 and the reconciliation
of that figure is recorded append-only under T091; full suite **1844** passed / 0 failed / **4
skipped** (pre-existing spec-009 harvest-hook deferrals). Neither re-stated figure promotes any
claim above its evidence class.

Further scope caveats, unchanged in substance but re-flagged against the current tree:

- C1 no-double-counting remains a dependency-boundary check plus lexical proxies; no
  behavioral composition proof is claimed ([`double-counting-risk.md`](double-counting-risk.md)).
- The measured-zero outcome does not survive save/load (sparse-cache zero omission); see
  [`measured-zero-persistence.md`](measured-zero-persistence.md).
- T079 (zero-stack inventory slot clear) and T069 (icon strategy) remain unobserved in a live
  session; the item ships with the vanilla springobjects fallback sprite (explicit acceptance
  in `contracts/content-registration.md`).
- Normal-play acquisition remains formally descoped; `lr_give_agrochemical` is verification-only.
- D1, human `verified`, SC-004 remain `pending`. Nothing in this addendum promotes them.

Prior rows and the "Remaining work" list are unchanged; items 1–4 there remain open.
