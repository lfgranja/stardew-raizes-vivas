# Converge: 018 — machine-pass full coverage (honest audit)

**Date:** 2026-11-09
**Scope:** audit of work done in the 018 cycle series (passes 7→20) and remaining gate items.
This document is documentation of current state. **It asserts no verification that did not run.**

## Baseline and lineage

- Feature origin: 018 from the MDMV roadmap (machine-pass attestation for host paths; farmhand/split-screen deliberately excluded from `machine-pass` until T002.
- Audit series: passes 7–20 under `verification/cycle-*.md`. This converge anchors at **pass 20** commit `e01f8ee`.
- No commit was pushed, merged, tagged, or published as part of this audit; nothing in this repository has been `git push`ed during any 018 pass.

## What the work covered (real, evidence-attached)

On this host (`fedora`, .NET SDK 10.0.400, game install `/home/luis/.local/share/Steam/steamapps/common/Stardew Valley`), the following were executed for real and recorded in ledgers/file notes append-only:

- **Determinism 2× (same seed, same save-mode, same binaries back-to-back, same environment):** 18 of 19 scenario files pass this audit —
  `004-hardening, 004-healthy, 004-moderate, 005-healthy, 005-poor, 005-moderate, 013-expiry, 013-bare-tile, 013-decay-exemption, 014-quality, 014-zero, 014-unknown, 015-harmony, 017-clamped, 017-tradeoff, 017-unknown-baseline, 017-refused-contention` plus `013-refused-on-ineligible`.
  Evidence in `verification/determinism-checks.md` (pass 13/15/16/17/18/19/20 sections) and the per-feature `test.md` append-only notes. Byte-identity of SMAPI logs is NOT claimed; semantic evidence (soil/crop-phase/predicates/verdict) is what was compared and matched.
- **017 `agrochemical-refused-at-minimum`: deterministic fail at predicate-1.**
  `pre.phaseDays` 4 → actual 5 on this build (soil 0 → poor → `SoilGrowthPoorMultiplier` applied by the phase capture). One earlier `135531Z-7c4e` machine-pass was a capture-order flake. The file is **not** counted as green and is not mutation-referenced. This is the single known-stale assertion in 018.
- **Mutation negative controls: 15 of 19** scenario files killed at the intended predicate (break production line → headless Gate 3 → verify failure at the declared threshold → restore → rebuild → post-restore rerun passes).
  Details in `verification/mutation-checks.md`. Mutant bundles (`honestyLevel: W`, `verdict: errored`) are **never** cited as machine-pass.
- Inert mutant documented as evidence of a class, never a kill (pass 20's `Tier=Poor, IsInScope=false` attempt; 12× `unmeasured-actual` in pass 15's selection-layer mutant).
- Bundles produced this audit: 282 `evidence-bundle-run-*` directories observed locally (152 with a `manifest.json`, considered evidence); 12 legacy retain-only failures from an earlier era; 94 observed `artifacts/`-only dirs (not bundles). Every bundle referenced by a claim record or GATE3 pointer was re-verified exit 0 in the cycle during which it was produced.
- Deterministic gates rerun in passes 11–20: format, locked restore, build (Release, single node), full suite (1937 passed / 4 skipped / 0 failed), smoke suite (launch-free; pass 14 re-certified; pass 12 pre-push rerun shows a known flake to re-verify before push), docs markers, doc links, protected paths.
- Bug flow completed in this series for the host-orchestration SIGABRT (`msbuild-release-build-sigabrt`): assessment, fix record (mitigation already in tree), verification report — recorded in `.specify/bugs/`.
- `scripts/release-gate.sh` (spec 019) reproduced and green on this host — full format → locked restore → build → tests → smoke → docs → protected paths: `ALL STEPS PASSED` (commit `4860cf2` lineage; spec 019 records verification).

## What is claimed and what is NOT

**Claimed:** host-only machine-pass attestations on the eligible paths, as recorded by runner promotions and local deterministic reruns.
**Not claimed, never claimed, and not in scope:**
- Human `verified`/honesty-level E attestation (FR-016).
- Farmhand/split-screen headless `isMainPlayer` flip (T002 — unverified; `blocked-feasible`).
- SC-004 visual comprehension, render legibility, balance/playtest.
- Farmhand guard, D1 transport, multi-player sync overnight — Constitution refers to farm state host-authoritative; no farmhand symbol was promoted.
- No public release, no SMAPI publish, no marketplace upload has been done by this program.
- No claim is "auto-merge"/"production-ready"; CI/release gates are local-only proposals (spec 019). Branch protection, required checks, auto-merge, publishing, and push remain blocked pending explicit owner authority.

## Residual work (honest inventory)

Open/partial items in `tasks.md` (selected, current as of pass 20):

- T034/T035: determinism 18/19 ready; kills 15/19. No feasible decision-seam kill existed for 013-decay-exemption / 013-refused-on-ineligible / 017-refused-contention; their evidence is 2× only. `017-refused-at-minimum` fails deterministically at a stale predicate-1; a spec decision (predicate rewrite vs. scenario deletion vs. modifier reordering) is required before green or before removal from scope.
- T002: farmhand split-screen headless `isMainPlayer` flip — blocked-feasible; records remain blocked/partial.
- T012: 017 literal "two applies in sequence" — residual honest gap; no change made.
- T013/T019/T022/T028: farmhand scenarios — blocked by T002.
- T014, T023, T025, T029, T031, T032: partial/parked items, each with append-only notes in the ledger recorded honestly (including the failed attempt and the 17-vs-17 reflection on automated vs. manual GATE3 promotions).
- Push/merge/release/branch protection: not done — explicit authority required.
- Launch-free smoke: re-certified green at pass 14; pass 12 rerun shows one known flake class — revalidate before any pre-push.
- `017-refused-at-minimum` spec decision is the single determinism blocker to 19/19.

## Risks and limitations

- The host is memory-constrained (swap exhausted during several runs); transient MSBuild node death (`MSB4166`) was observed and is recorded; mitigation `-m:1` in the gate.
- Evidence bundles self-certify integrity (SHA-256 anchor) independently of this document.
- This audit was authored by the same program that performed the work; its evidence paths are independently re-checkable and are listed in ledgers.
