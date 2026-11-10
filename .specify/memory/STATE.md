# Session State — living-roots (repo-wide resume point)

> **Supersedes** the stale 2026-10-22 block (feature 008 complete). That history remains in git (`git log -p -- .specify/memory/STATE.md`). This file records the resume point as of **2026-11-09**.

## Current verified state (2026-11-09, final snapshot)

- Branch `dev` = `origin/dev` = `cd90f2f`; **0 commits ahead**; working tree clean; no PRs; 0 tags; 0 public releases; release **0.1.0** (spec 017) shipped repo-scoped only (no public channel).
- Build Release **0 errors** (`dotnet build Stardew-LivingRoots.sln -c Release -m:1`; 14 pre-existing warnings; `-m:1` is the recorded host mitigation for `msbuild-release-build-sigabrt`).
- Tests: **1997 passed / 0 failed / 4 skipped** (2001).
- Specs closed (0 open tasks, artifacts committed): 002, 005, 008, 012, 013, 016, **017**, **020**.
- Specs nearly closed: 004 (65/67 — T034 D1 humano, T037 playtest), 014 (131/133 — T109 D1 blocked, T110 deferred).
- Specs partial: 015 (58/65), 018 (24/28; 4 farmhand-guards blocked-feasible), 019 (5/7; owner actions), 009 (55/58; manual).
- Specs stale/open: 010 (150/203), 001 (10/86), 003 (28/117), 011 (42/88). Repo totals: **1198 done / 284 open (1482; 80.8%)**; active track (002 + 004–020): **968/988 (98.0%)**.
- Gate 3: **machine-pass host-only** in 7 specs — 004 `7374`, 005 `4cc1`, 013 `1ea4`, 014 `088b`, 015 `29cf`, 017 `6938`, 020 `6021` (all `verdict: pass`, honesty R). **No human E attestation anywhere. D1 (real farmhand) never run. SC-004 (017) pending.**
- `machine-pass` ≠ `verified` — do not promote any claim without human/D1 evidence (`VERIFICATION_PROTOCOL.md`). Spec 020 Gate 3 covers planting only (harvest/seed-save/acquisition not attested).
- Bugs: 30/40 with assessment+fix+test; 19/40 with a positive result (17 `verified` + 1 `verified (bug-specific)` + 1 `pass`); 10 `partial`; 1 obsolete `failed`; 2 empty dirs.

## Blockers not to "fix" autonomously

- Any tag, public release, Nexus/CurseForge channel, branch protection / secrets / auto-merge (spec 019 T006/T007) — **owner decision only**. `dev` itself is synced with `origin/dev`.
- D1 live session (014 T109, 015 T049/T057, 018 farmhand-guards) — needs a real second player/environment; the headless runner is host-only by construction.
- Human E Gate 3 — needs a human with the game.
- T110 (34/67→31/71 historical contract) — deferred deliberately.

## Highest-value next actions

1. Maintainer decision: tag `0.1.0` / public release / Nexus channel — or record an explicit "do not publish" decision. `dev` is already synced.
2. Human E 3-signal roteiro → promote `machine-pass` → `verified` (start with 004/005/013/014/017/020).
3. D1 environment + session (unblocks 014/015/018).
4. SC-004 (017): run `specs/017-joja-agrochemical-tradeoff/verification/sc004-comprehension-protocol.md`.
5. Close/document residual tasks (004/014/015/018/019) and decide spec 010 scope — `/speckit.converge` append-only.
6. Next gameplay slice: rotation/polyculture MVP **or** full Joja/monoculture; heirloom pipeline (020) is done and unblocks Three Sisters/Green Manure/Rotation Journal/CSA.

## Pointers

- Vision/state: `OBJETIVO.md`, `ROADMAP.md` (2026-11-09 final snapshot blocks at top).
- Releases: `.specify/releases/017-joja-agrochemical-tradeoff/release.md`; validation: `.specify/validations/017-joja-agrochemical-tradeoff/validate.md`.
- Research: `research/2026-11-09-journey-balance-and-outlook.md` (balance/outlook), `research/2026-11-09-web-research-ecosystem-status.md` (+ raw outputs), `research/2026-11-07-mdmv-80-20-cycle-ecosystem-and-reconciliation.md`, `research/2026-11-05-web-research-ecosystem-status.md`.
- Constitution: `.specify/memory/constitution.md` v2.0.0 (Principle VI non-negotiable).
