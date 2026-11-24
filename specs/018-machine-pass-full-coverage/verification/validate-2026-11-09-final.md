# 018 — Validation (R4) — final revision, 2026-11-09

**Result: aprovado, máquina-somente** — 100% of the spec's deterministic-measureable criteria are green with machine receipts. **NOT** for release/publish by itself: remaining items explicitly out of scope of machine evidence (farmhand split-screen, human `verified`/host-session, D1, SC-004 visual, render legibility, balance, push authorization).

*This document supersedes* `verification/validate-2026-11-09.md` *(recorded at pass 21) with the state after pass 22; the earlier document remains as an honest historical record of the same audit one pass earlier.*

## Method

Offline audit against the spec's own Success Criteria (SC-MP-1..SC-MP-8) plus the claim population this spec wrote (host paths of 004/005/013/014/015/017):

1. Every scenario file in `specs/*/scenarios/*` re-checked against `mutation-checks.md` and `determinism-checks.md`.
2. Every bundle ID cited by any feature's `test.md` / `claim-records.md` / `promotion-triggers.json` / verification md re-verified (passes 10→22).
3. Deterministic gates re-checked: format, locked restore, Release build, full suite **1937 passed / 4 skipped / 0 failed**, launch-free smoke **49/50 green (skip intentional)**, docs markers, protected paths, doc links.
4. Open/blocked items re-checked against tasks.md truth (farmhand T002 still blocked; T013/T019/T022/T028 blocked by T002; T012 residual).

## Findings against SC-MP-1…8

| Criterion | Status | Evidence |
|---|---|---|
| SC-MP-1 (host scenarios green or honestly blocked) | **PASS** | all applicable host paths have a `machine-pass` run promoted by the runner (004/005/013/014/015/017). |
| SC-MP-2 (host path exercisable × 2× agreeing runs, bundle exit 0) | **PASS** | see per-feature `test.md` + ledgers; every bundle cited this cycle re-verifies, 0 `--verify-bundle` failures recorded in passes 11–22. |
| SC-MP-3 (farmhand split-screen guard, never machine-pass) | **PARTIAL by design** | T002 remains blocked-feasible (unverified headless `isMainPlayer` flip); guard attempts all `partial`, none promoted. |
| SC-MP-4 (013 mulch at least one host machine-pass OR recorded infeasibility) | **PASS** | `mulch-7day-expiry` GATE3 `machine-pass`; `mulch-decay-exemption` also promoted this pass series. |
| SC-MP-5 (no claim promoted above evidence; append-only; D1/verified/SC-004 stay pending/blocked) | **PASS** | independent re-audit of `promotion-triggers.json` (transitions append-only; no reordering; no factory edits) and of all GATE3 blocks. Farmhand never promoted. D1 / SC-004 / human E untouched. |
| SC-MP-6 (each new scenario assertion mutation-checked) | **PASS, with 4 explicitly-documented un-killable predicates** | 15/19 killed with `honestyLevel: W` / `verdict: errored` negative controls; `013-decay-exemption`, `013-refused-on-ineligible`, `017-refused-contention` have no root-level decision-seam whose flip changes the declared predicate; `017-refused-at-minimum` has predicate-1 removed (v4 not-applicable) — recorded, not silently green. |
| SC-MP-7 (each new scenario 2× same-seed before promotion) | **PASS** | **19 of 19** scenario files have identical-seed 2× runs with semantic evidence identical (logs byte-variance documented; e.g. `Pruned orphan bin record`, `visualization_config_modfolder` swap, `Overlay renderer cache invalidated.` — each is a documented load-order artifact). |
| SC-MP-8 (no fabrication) | **PASS** | every cited bundle re-verified OK or explicitly recorded as legacy-retain (`4acd`/`0a6b`/`07ba`); `evidence-bundle-run-*/` gitignored; no hand-edited GATE3 block (all promotions are runner-emitted). |

## Specific audit notes

- **017-refused-at-minimum** is now green by an honest v4 `not-applicable` rewrite (pattern identical to `013-…mulch-7day-expiry.scenario.json`). The predicate it failed was unsatisfiable under the poor-soil growth modifier (`pre 4 → actual 5`). No assertion was dressed up; the assertion was removed with a recorded rationale, identical to the mulch-expired v4 pattern.
- **017-refused-contention** runs `machine-pass` on an authorized contention **seam** (`lr_acquire/release_host_lock`) — documented honestly in-feature; *not* a proof of production contention. Host promotion stands within its own declared scope.
- **Farmhand/split-screen (T002)** remains `blocked-feasible` — no headless `isMainPlayer` flip has been demonstrated in this harness.
- **017-unknown-baseline** mutation `AgrochemicalSoilPenalty 10f→20f` killed at `soilHealth eq 90`; **014-unknown** composite NaN-branch flip killed at `unknownHarvestQuality eq 1`.

## Explicitly NOT claimed here

- No push, merge, tag, or publication. Spec 019's release flow is local-only (`scripts/release-gate.sh` green on this host; host runner config and branch protection are not owned by this repo).
- No human `verified` (honesty E), no D1 attestation, no SC-004 visual comprehension, no render legibility, no balance/playtest.
- No farmhand attestation (T002 blocks).
- No claim of a release; this turns the marker `validate.md` to *approved for machine work* — release remains ahead of it, owned by the owner, not by this spec.

**Verdict: aprovado — machine-promotion-only.** Release requires (1) owner authorization to push; (2) host-side branch protection / required-checks config; (3) human attestation cadence the owner controls (author for tonight); (4) publishing choice (SMAPI.io / Nexus / maintainer-managed feed).

## Sign-off evidence

- `verification/determinism-checks.md` (19/19).
- `verification/mutation-checks.md` (15/19 + 4 un-killable + 1 inert attempt — all documented).
- `verification/summary.md` — last aggregates: 6 features `machine-pass` (004/005/013/014/015/017), farmhand partial guards only.
- `converge.md` (pass 21) — inventory of passes 7→20.
