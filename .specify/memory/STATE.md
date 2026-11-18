# Session State — living-roots (repo-wide resume point)

> **Supersedes** the stale 2026-10-22 block (feature 008 complete). That history remains in git (`git log -p -- .specify/memory/STATE.md`). This file records the resume point as of **2026-11-10** (see block below; the 2026-11-09 block is preserved verbatim after it as the previous verified state).

## Decision recorded 2026-11-18 (autonomous orchestration — BLOCO 5, before BLOCO 6)

- **Anchor**: ALVO `/home/luis/development/stardew-raizes-vivas` @ local `dev` `30ce5c9`; `origin/dev` `af12158` (10 local docs-only commits by the owner are NOT mine — left intact, not pushed). FONTE_FACTORY `ce3d42132edb9466b75eab968be775ebdea38985` (read-only; its `check_production_gate.py` is NOT the target authority).
- **Authoritative target gate**: `scripts/release-gate.sh` (7 steps: format, locked restore, Release build, full tests, launch-free smoke, docs markers, protected paths) + Gate 3 real SMAPI via `scripts/verify-scenario.sh`. `check_production_gate.py` — **N/A, factory-only, absent in target (BLOCO 0.a/0.b)**. Merge authority verified live via `gh api`: required check = `sdd-gate` only, `strict: true`, 0 reviews, admin permission present → merge allowed after green CI; auto-merge stays OFF.
- **Ranking formula (denominator declared)**: `score = (Impacto × Evidência) / (Esforço × Risco)`, each 1–5; Esforço/Risco inverted so 1 = low. Max theoretical = 25/1 = 25 → normalized % = score/25 × 100. Judgment grounded in executed gates + live `gh` rollup, not activity volume.

| # | Candidate | I | E | Effort | Risk | score | norm | Owner/human-gated? |
|---|-----------|---|---|--------|------|-------|------|--------------------|
| C1 | Make secret-dependent CI failures explicit (CI elevation; workflows + docs + deterministic test) | 5 | 5 | 2 | 2 | 6.25 | 25% | no |
| C2 | Trivy ignore expiry re-audit (`.trivyignore` expires 2026-11-30) | 3 | 4 | 1 | 3 | 4.00 | 16% | only if a NEW suppression is needed |
| C3 | Quality-gate fragility hardening (0% jscpd ceiling, 0.05% coverage ε) | 4 | 4 | 2 | 3 | 2.67 | 11% | no |
| C4 | Reconcile stale spec checklists (003/001/010 verify + tick) | 3 | 4 | 3 | 2 | 2.00 | 8% | no |
| C5 | Stale bug-fix doc facts (`build-and-test-failures/fix.md`, `check-formatting-ci-failure/fix.md`) | 2 | 5 | 2 | 2 | 2.50 | 10% | no |
| C6 | Dependency-pin policy extension | 4 | 3 | 3 | 4 | 1.00 | 4% | no |

- **CHOSEN: C1 — "Make secret-dependent CI failures explicit"**, feature `025-ci-secret-guard-explicitness` (resolved by `.specify/scripts/bash/create-new-feature.sh --json --dry-run`, not by branch name). Rationale: (a) BLOCO 5 priority rule puts "elevar a CI" first when CI does not inspire merge confidence — 6 open dependabot PRs (#790–#796) are RED on Build and Test / Check Formatting / build-analysis / gate; (b) `code-style.yml:48-51` already fails fast with `::error::BUILDDEPS_TOKEN is not set` while `build_and_test.yml:73-112` and `quality-gate.yml:213-228` do not, so failures are ambiguous; (c) all touch paths are disjoint from the owner's 10 unpushed docs commits; (d) no runnable C# → Gate 3 real-game is N/A, only Gate 1/2 + SDD gates apply. Risk kept low by failing CLOSED (never `continue-on-error` on secret-dependent jobs) per `.specify/bugs/ci-skipped-job-false-green/assessment.md`.
- **Evidence baseline**: local `docs/0.2.0-beta-prep.md:62` records `release-gate.sh ALL STEPS PASSED` on `27ed81b` (2026-11-14); today's run steps 1–3 OK and tests in progress. Mainline CI runs green on `origin/dev`; the 6 red PRs are dependency-bump/actor-scoped **[NV: remote logs not re-read here]**. Doc drift (OBJETIVO/ROADMAP/README/STATE pinned to `c07fbab`, "0 ahead") is recorded as DEBT, not edited in this PR — those files are owned by the unpushed local commits.
- **Escalation not needed**: chosen candidate requires no tag, release, branch-protection, secrets, D1 session, or human attestation. These remain owner-only and are NOT simulated.
- Recorded at ~2026-11-18T10:52Z immediately before BLOCO 6 execution.

## Delivery record 2026-11-18 (autonomous cycle 025 — Bloco 8)

- **Merge**: PR [#797](https://github.com/lfgranja/stardew-raizes-vivas/pull/797) `feat(ci): add explicit secret-guard diagnostics and contract test` merged into `dev/` at **2026-11-18T11:23:57Z**, merge commit `a3711dc`. 17 atomic commits, 137 reviewable code LOC. No auto-merge, no force-push, no history rewrite.
- **CI observed live before merge**: `mergeStateStatus: CLEAN`; SUCCESS on `Build and Test`, `Check Formatting`, `Check Protected Paths`, `sdd-gate`, `verify`, `Quality Gate / {static,sca,build-analysis,gate}`; `codeql` SKIPPED by design; `Semgrep OSS` NEUTRAL. Branch protection on `dev`: only `sdd-gate` required, `strict: true`, 0 reviews, admin permission present — no approver was simulated.
- **Local gates re-run in the feature worktree**: `dotnet format` / locked restore / Release build / docs markers / protected paths exit 0; `dotnet test -c Release --no-build` green on rerun **1999 passed / 0 failed / 4 skipped (2003)** after one flaky FsCheck property seed (isolated reruns 1 fail / 2 pass; no C# touched); `scripts/ci/sdd-gate.py` **PASS (0 errors, 28 warnings)**. New contract test RED `4 passed / 1 failed` (named `build_and_test.yml`) → GREEN `5 passed`.
- **What changed**: fail-fast `::error::BUILDDEPS_TOKEN is not set` in `build_and_test.yml`; deterministic workflow-contract test excluding best-effort and owner-gated tag releases; CI trust-boundary section in `docs/quality-gate.md`; runtime `.specify/queue/` gitignore entry; full 025 SDD trail (assess → R1 go → specify → R3 → plan → 18/18 checklist → tasks → read-only analyze → implement → converge → R4 validate → R7 release).
- **Debts carried (not silently fixed)**: 11 pre-existing failures in the local `scripts/quality-gate/tests` suite at `origin/dev` (stale pins/suppression-policy expectations; 0 new failures, 5 new tests pass); hosted `Quality Gate` runs the `quality_gate` CLI, not pytest. The FsCheck visualization-invalidation property is flaky. `OBJETIVO.md`/`ROADMAP.md`/`README.md` still pin `c07fbab` and "0 commits ahead" — those files are owned by the maintainer's 10 unpushed local docs commits and were deliberately left untouched.
- **Owner-only, not simulated**: tag/public release, Nexus channel, branch protection, secrets, auto-merge, D1 live two-player session, human Gate 3 attestation, SC-004. See spec 019 T006/T007 and the pre-existing blockers block below.
- **[NV]**: step-level root cause of dependabot PRs #790–#796 was not re-read from remote logs; their red status was observed live but is not promoted to a verified cause. The six dependabot branches and `stash@{0}` (owner, 2026-11-01, `ModEntry.cs`) were left intact.
- **Next 80/20 (not executed here)**: (1) reconcile the 11 stale quality-gate pytest expectations (CI confidence, S–M, no owner auth); (2) re-audit `.trivyignore` expiry 2026-11-30 (S, owner auth only if a new suppression is needed); (3) maintainer decision on tag `0.2.0` (owner-only, escalate — do not self-authorize).

## Current verified state (2026-11-10)

- Branch `dev` = `origin/dev` (HEAD `c07fbab`); **0 commits ahead**; working tree clean; no PRs; **0 tags; 0 public releases**.
- `release-gate.sh` → **`ALL STEPS PASSED`** (execução fresca 2026-11-10T02:40Z no HEAD): format, restore locked, build Release `-m:1`, testes **1997 passed / 0 failed / 4 skipped (2001)**, smoke launch-free **50/51 (1 skip)**, docs markers, protected paths.
- `dotnet format --verify-no-changes` → exit 0.
- **Specs fechadas (8/20)**: 002, 005, 008, 012, 013, 016, **017**, **020**. Contagens inalteradas: 1198 done / 284 open (80,8%); trilha ativa **968/988 (98,0%)**.
- Gate 3 **machine-pass host-only** em **7** specs: 004 `7374`, 005 `4cc1`, 013 `1ea4`, 014 `088b`, 015 `29cf`, 017 `6938`, 020 `6021`. **0 human-verified; 0 D1; 0 SC-004.**
- Proveniência: HEAD contém 020; manifest `0.1.0`; zip 0.1.0 contém assets 020; sem `release.md` da 020. **Publicar `0.2.0`** é o bump correto (primeira pública, não 0.1.0).
- Qualidade: `Quality Gate / static` vermelho (148 linhas jscpd vs baseline 111) — pré-existente, fora do `release-gate.sh`.
- **Mercado 2026-11-10 (Nexus 403; confiança M):** Wildroot Chronicles (breeding, ZH nativo, ~85 endorsements) capturou a atenção; **Harvest Ledger** (rotação, PT-BR confirmado, 15 endorsements); ReGen Valley não verificável; Joja Seeds broken 1.6; **nenhuma tradução não-inglesa de mod de agroecologia detectada**; janela PT-BR/EN aberta.
- **Ecossistema 2026-11-10:** SDV 1.6.15; **1.7 sem data/RC**; 1.6.16 não verificado; SMAPI **4.5.2**; **compat list sunset confirmado**; **update check NÃO descontinuado**.
- **Distribuição 2026-11-10:** Nexus **AI policy vigente desde 2026-11-03** (tags AI-Generated/AI Media/AI Assisted; carência até 2027-01-02; uploads novos declaram já); **JunimoServer v1.4.0 (2026-01-17)**; D1 = duas instâncias reais; `UpdateKeys` `Nexus:<id>` + `GitHub:lfgranja/LivingRoots`; `lfgranja.LivingRoots` UniqueID válido (checar colisão); ModDrop: ausência de proibição verificada.
- Bugs: **30/40** A+F+T; **19/40** positivos; **10 partial**; 1 failed/obsoleto; 2 vazios; 5 só assessment.
- **Readiness:** ready for distribution técnica; **não** for public release yet — decision pending.

## Blockers not to "fix" autonomously

- Any tag, public release, Nexus/CurseForge channel, branch protection / secrets / auto-merge (spec 019 T006/T007) — **owner decision only**.
- D1 live session — needs a second real player/environment; headless runner host-only.
- Human E Gate 3 — needs a human with the game.
- T110 (34/67→31/71) — deferred.

## Highest-value next actions

1. Maintainer decision: **tag/release `0.2.0` pública** — or record “do not publish”. GitHub first; Nexus depois dos assets + disclosure IA.
2. Human E 3-signal roteiro → promote `machine-pass` → `verified` (004/005/013/014/017/020).
3. D1 environment + session (unblocks 014/015/018); combine with human session.
4. SC-004 (017): run `specs/017-joja-agrochemical-tradeoff/verification/sc004-comprehension-protocol.md`.
5. Next gameplay slice: **rotation/polyculture 0.3.0** (M) — `/speckit.specify` a partir de `rotation-journal.md`.
6. Hygiene: heirloom harvest/seed-save Gate 3 (S); jscpd subset (S–M); dedup 010 (S); decision on `Quality Gate / static`.

## Pointers

- Vision/state: `OBJETIVO.md`, `ROADMAP.md` (top annotation 2026-11-10).
- Research: `research/2026-11-10-progress-and-gaps.md`; 2026-11-09 baselines (`journey-balance-and-outlook.md`, `web-research-ecosystem-status.md`).
- Releases: `.specify/releases/017-joja-agrochemical-tradeoff/release.md`; validations 017/020.
- Constitution: `.specify/memory/constitution.md` v2.0.0 (Principle VI non-negotiable).

## Previous verified state (2026-11-09, superseded — preserved verbatim)

- Branch `dev` = `origin/dev`; **0 commits ahead**; working tree clean; no PRs; 0 tags; 0 public releases; release **0.1.0** (spec 017) shipped repo-scoped only (no public channel).
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

## Decision recorded 2026-11-18 (orchestration session c3f1 — BLOCO 5, before BLOCO 6)

- **Anchor**: ALVO `/home/luis/development/stardew-raizes-vivas`; local `dev` `30ce5c9` (34 behind, READ-ONLY); base `origin/dev` `4b54c30`. FONTE_FACTORY `c81f0f8` (read-only; prior session recorded `ce3d421`). NNN resolved by `create-new-feature.sh --json --dry-run` on origin/dev state = **026** (specs 024 and 025 exist upstream).
- **Authoritative gate (BLOCO 0.a)**: target `scripts/ci/aggregate-gate.sh` (sdd-gate + policy drift + protected paths + docs markers) and `scripts/release-gate.sh`; Gate 3 via `scripts/verify-scenario.sh`. Factory `check_production_gate.py` / `hub_docs_gate.py` are **absent in target → N/A**. CI provider GitHub Actions (`gh`); dev protection requires `sdd-gate` + `Build and Test`, 0 approving reviews, enforce_admins=true — no distinct approver pending, auto-merge stays OFF. The constitution's "human review and merge" clause governs constitutional amendments (Governance section), not ordinary PRs.
- **Factory SDD scripts probed on target** (materialized from clean `c81f0f8` into a disposable worktree, then removed): `check_sdd_completeness.py` FAIL (20 gaps, baselined=0), `check_work_order_dag.py` FAIL (duplicate tasks), `check_task_id_uniqueness.py` FAIL (2 duplicate IDs), `check_converge_append_only.py` FAIL (2 unbaselined). All failures are factory-baseline/convention artifacts → **not target authority, not imported** (BLOCO 0.a).
- **Ranking formula (denominator declared)**: `score = (Impacto × Evidência) / (Esforço × Risco)`, each 1–5, 1 = low effort/risk; max 25 → norm = score/25 × 100. Ordered by mission impact first, never by activity volume.

| # | Candidate | I | E | Esf | Risco | score | norm |
|---|-----------|---|---|-----|-------|-------|------|
| C1 | Stabilize overlay cache invalidation under interleaved events (required `Build and Test` RED on dev) | 5 | 5 | 2 | 2 | **6.25** | **25%** |
| C2 | Fix 2 of 5 invalid archify candidates (heirloom phase overlap; verification wrong schema) | 2 | 5 | 2 | 1 | 5.00 | 20% |
| C3 | Reconcile stale docs pinned to `c07fbab` / "0 ahead" | 2 | 5 | 2 | 1 | 5.00 | 20% |
| C4 | Import 4 factory SDD gate scripts into target CI | 4 | 4 | 3 | 2 | 2.67 | 11% |
| C5 | Merge 7 open dependabot PRs | 3 | 3 | 5 | 5 | 0.36 | 1% |

- **CHOSEN: C1**. Evidence: live CI run `44309131539` on `dev` push after PR 800 = 2003 tests / 1998 passed / **1 failed** / 4 skipped; sole failure `VisualizationCacheInvalidationInvariantsTests.RenderOverlays_ForAnyDisableEnableInterleaving_RebuildsOnFirstFrameAfterReEnableWithinBudget`, FsCheck StdGen `(1549256609,297700084)`. Local reproduction on `30ce5c9`: **4 of 5 runs failed** with distinct seeds and a concrete 23-event counterexample — input-dependent interleaving, not merely CI timing. `Build and Test` is a **required** check on `dev`, so a red mainline blocks every merge; BLOCO 5 priority rule puts CI elevation first. C5 is fail-closed by design (`BUILDDEPS_TOKEN` absent for dependabot) and owner-gated; C4's gates produce false RED on target conventions.
- Owner-only items remain untouched and unsimulated: tag/public release, Nexus, branch protection, secrets, auto-merge, D1 live session, human Gate 3 attestation, SC-004. `stash@{0}` and 7 dependabot branches are not owned — not touched.
- Recorded at 2026-11-18T~11:50Z immediately before BLOCO 6 execution.
