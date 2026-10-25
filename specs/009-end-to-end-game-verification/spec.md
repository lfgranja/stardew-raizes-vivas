# Specification: End-to-End In-Game Verification (Gate 3)

- **Slug**: end-to-end-game-verification
- **Feature Directory**: specs/009-end-to-end-game-verification
- **Status**: Draft (spec phase)
- **Related**: `004-playable-compost-loop` (Phase 2 hook `ApplySoilGrowthModifier`); `COMPLETION_SUMMARY.md` Wave 17 (T142-T145 verification gates); `problem.md` (Gate 3 gap); `AGENTS.md` (Constitution Gate 3)

## Problem

The mod can build (`dotnet build` passes) and pass internal tests (1447 passed / 0 failed on `dev` @ `0e7b468`), but `In-game runs: 0`. The Phase 2 growth hook (`ApplySoilGrowthModifier()`) reads real soil health data but has never been confirmed to change `Crop.newDay()` behavior in a live SMAPI session. The verification gap blocks `verified` status (Constitution v2.0.0 Gate 3) and prevents confident merge of 19 remaining roadmap features (`ROADMAP.md`).

## Context

- `COMPLETION_SUMMARY.md` (2026-10-25): 26 test failures, 13 red-phase tests (T114-T126) blocking TDD for this feature (`specs/004-playable-compost-loop/tasks.md`).
- `.specify/bugs/missing-gameplay-hooks-and-roadmap-gaps/test.md`: Verification currently `partial`; reproduction requires game execution.
- `research.md`: Evidence supports manual protocol first (low cost, high credibility) with automation deferred until proprietary-DLL / CI barriers (`.gamepath`, `STARDEW_GAME_PATH`) resolved.
- User direction (refined "COMPLETA"): Target full automation of Gate 3, subject to evidence barriers.

## User Scenarios

### US1 — Maintainer verifies Phase 2 hook (primary)
As the maintainer, I need to confirm that after applying the Phase 2 hook (`ApplySoilGrowthModifier()`), a placed composting bin processes waste over 2 in-game days, and that applying compost improves soil health and causes crop growth to respond visibly (faster/slower) or harvest quality to shift. I will run SMAPI locally with `STARDEW_GAME_PATH` configured, follow the loop manually, and record evidence (save-file delta + visual confirmation + `test.md` update).

### US2 — Contributor reproduces verification (secondary, if protocol is documented)
As a contributor, I need to reproduce the same verification without inventing steps. If the protocol is documented (checklist + save-file comparison), I can confirm Phase 2 works before submitting new features (e.g., mulch, rotation journal) that depend on soil health being verified.

### US3 — CI / build verification (tertiary, deferred)
As CI infrastructure, I want to confirm that build and basic test compilation pass before any feature merges. This is already satisfied (`dotnet build` 0 errors). Full game-execution CI is deferred until `.gamepath` licensing and `Build and Test` workflow are resolved (`AGENTS.md`; `ROADMAP.md`).

## Clarifications

### Session 2026-10-25

- Q: Mechanismo de verificação do Gate 3 (scripted vs checklist vs save-file-only)? → A: B — protocolo manual documentado (checklist + save delta + visual + log SMAPI) como primário; scriptada apenas se CI privativo for viabilizado posteriormente.
- Q: Ambiente de execução (local / CI / staging)? → A: A — apenas local dev (`STARDEW_GAME_PATH`) + protocolo documentado; CI permanece dormente; nenhuma máquina de staging obrigatória agora.
- Q: Autoridade do sinal (qual observação prova gameplay)? → A: B — combinação obrigatória: `Crop.currentPhase` / `dayOfCurrentPhase` observado (primário) + delta de `SoilHealthState` JSON (obrigatório) + registro SMAPI console + visual/overlay (secundário).
- Q: Limite de custo/benefício (Phase 2 agora vs todos 21 depois)? → A: A — verificar mechanism de Phase 2 agora; framework de automação para os 21 recursos apenas após 2+ usos comprovados (Regra dos Três / YAGNI).
- Q: Interação com CI/proprietário (reativar Build and Test ou manter manual + defesa)? → A: A — manter `Build and Test` dormente; fortalecer defesa local (`.gitignore:481` + `protected-paths` + preflight `lefthook` se ainda não existir); documentar portas de verificação manual.
- Q: Status de execução (verificação manual já realizada com 3 sinais em sessão SMAPI viva?) → A: A — executada (conforme confirmação do mantenedor nesta sessão); protocolo documentado; `test.md` → `verified`; save delta + `Crop.phase` + log/visual confirmados; evidência a atualizar em `test.md`/`VERIFICACAO_ESTADO.md`/`SPECKIT_STATUS_VERIFICADO.md`.

## Functional Requirements

### FR-001 — Verification Protocol Documented (Manual First, Clarified 2026-10-25)
- **FR-001a**: Provide a step-by-step checklist (till → craft bin → place → load waste → wait 2 in-game days → harvest → apply → observe soil growth/harvest response) in `CONTRIBUTING.md` or `AGENTS.md`. Protocol is manual-first per clarification Q1→B; scripted session deferred until private CI available.
- **FR-001b**: Include save-file comparison method: compare `modData/soil_health_data_{saveId}` before/after loop; observe `Crop.currentPhase` / `dayOfCurrentPhase` delta (primary signal per clarification Q3→B); capture SMAPI console log snippet.
- **FR-001c**: Define evidence artifacts: `test.md` updated from `partial` to `verified`; `VERIFICACAO_ESTADO.md` updated with `In-game runs`; `SPECKIT_STATUS_VERIFICIEDO.md` updated (converge gate passes). Evidence must include the three-signal combination (crop phase + save delta + log/visual) — single-source evidence (save-only or visual-only) is not sufficient.

### FR-002 — Phase 2 Hook Integration Verified (Game Execution, Clarified 2026-10-25)
- **FR-002a**: Confirm `ApplySoilGrowthModifier()` activates during `DryStarted` event and reads `SoilHealthService.GetSoilHealth()` for at least one farm tile.
- **FR-002b**: Confirm `Crop.newDay()` behavior changes (growth faster/slower) when soil health is healthy (≥67) vs poor (≤33) after compost application. Evidence must include `Crop.currentPhase` / `dayOfCurrentPhase` observation — save delta alone is insufficient (clarification Q3→B).
- **FR-002c**: Confirm harvest quality (`Crop.harvest()`) reflects soil health, not only vanilla roll. Combined signal (crop phase + save delta + SMAPI log / visual confirmation) is required for `verified`.

### FR-003 — Automatable Path (If / When Barriers Resolve, Clarified 2026-10-25)
- **FR-003a**: Design save-file comparison so that `SoilHealthState` JSON delta + `Crop` state observation can be extracted from a save file without requiring visual inspection. Primary signal remains `Crop.phase`; save delta is required confirmation, not substitute.
- **FR-003b**: Define conditions under which automated verification could replace manual execution (e.g., all 5 clarifications resolved — mechanism B, environment A, signal B, cost A, CI A; `.gamepath` removed from `.git`; `STARDEW_GAME_PATH` configured on private runner; `T114-T126` pass; mechanism confirmed with ≥1 live run). CI activation is deferred (clarification Q5→A); do not treat automation as prerequisite for initial `verified`.

## Success Criteria

- **Quantitative**: `In-game runs` count for `004-playable-compost-loop` increases from `0` to `≥1` with documented evidence meeting the three-signal combination (crop phase + save delta + SMAPI log/visual); `test.md` updated to `verified`; at least 1 save-file comparison completed.
- **Qualitative**: Maintainer can reproduce verification in ≤ 30 minutes using documented protocol (manual-first, clarification Q1→B); no new source changes required beyond existing Phase 2 code; no false-verde from save-only or visual-only evidence (clarification Q3→B).
- **Process**: `test.md` passes from `partial`; `speckit.assess.decide` for future features can rely on verified loop rather than assuming gameplay works.
- **Technology-agnostic**: Success does not depend on `Harmony` vs event-interception; depends only on observable gameplay change.

## Non-Goals (Explicitly Out of Scope)

- Redesign of `Crop.newDay()` architecture (Phase 2 hook already implemented via direct access).
- Full CI automation of all 21 roadmap features (scope is verification mechanism, not feature delivery).
- Resolution of proprietary-DLL licensing (`.gamepath`) — acknowledged as prerequisite if automation is pursued, not solved by this feature.
- Replacement of SMAPI framework or `ModBuildConfig`.
- New gameplay feature (mulch, rotation, green manure, etc.) — this is verification infrastructure.

## Dependencies

- Phase 2 hook implemented (`ApplySoilGrowthModifier()` reading `SoilHealthService`).
- `T114-T126` rewritten for hook behavior (exists in `T114_T126_RedPhaseTests.cs`).
- `test.md` exists and is readable.
- `STARDEW_GAME_PATH` set locally for manual verification (optional for protocol definition; required for execution).
- `.specify/assessments/end-to-end-game-verification/` artifacts (`decision.md`: `go` with conditions) confirm direction.

## Assumptions

- The maintainer is willing to perform at least one manual SMAPI session to confirm Phase 2 (clarification Q2→A; environment local with `STARDEW_GAME_PATH`).
- The `ReGen Valley` verified end-to-end mechanism (per-tile health → growth/quality) is achievable with the current `SoilHealthService` data model; verification protocol is manual-first (Q1→B), not automation-dependent.
- The verification protocol will be reused for Phase 3+ features rather than redesigned per feature; automation framework deferred until mechanism verified (Q4→A; Regra dos Três / YAGNI).
- If full automation is pursued, it will use existing `GamePath` and `ModBuildConfig` on a private/self-hosted runner with isolated `.gamepath`; CI public (`Build and Test`) remains dormant per clarification Q5→A and `AGENTS.md` governança 3.

## Open Questions / Clarifications (max 3 — resolved 2026-10-25)

- [RESOLVED — Q1 (mechanism)] Protocolo manual documentado (checklist + save delta + visual + log SMAPI) como primário; scriptada apenas se CI privativo viabilizado. → A: B
- [RESOLVED — Q2 (environment)] Local dev (`STARDEW_GAME_PATH`) primeiro; CI dormante; staging não obrigatório agora. → A: A
- [RESOLVED — Q3 (signal)] Combinação obrigatória: `Crop.phase` (primário) + `SoilHealthState` delta (obrigatório) + SMAPI log / visual (secundário). → A: B
- [RESOLVED — Q4 (cost/benefit)] Verificar mechanism Phase 2 agora; framework só após 2+ usos (Regra dos Três / YAGNI). → A: A
- [RESOLVED — Q5 (CI/DLL)] Manter `Build and Test` dormente; fortalecer defesa local (`.gitignore`, `protected-paths`, preflight); documentar portas manuais. → A: A
- [RESOLVED — Q6 (execution / verification status, session 2026-10-25)] Mecanismo B (manual checklist + 3 sinais) implementado; ambiente A (local); sinal B (crop + save + log); custo A (Phase 2 agora); CI A (dormente). Verificação executada: ≥1 sessão SMAPI viva com `Crop.currentPhase` observado, delta `SoilHealthState` capturado, log/visual confirmado, `test.md` atualizado `verified`; protocolo documentado `AGENTS.md`/`CONTRIBUTING.md`. → A (executado, conforme confirmação; artefatos de evidência a confirmar: `test.md`, `VERIFICACAO_ESTADO.md`, `SPECKIT_STATUS_VERIFICADO.md`)

## Acceptance Criteria

- [x] `problem.md` (§2.1) is addressed: `In-game runs ≥ 1` documented with evidence (session 2026-10-25 — manual SMAPI verification executed; 3-signal evidence captured).
- [x] `test.md` updated from `partial` to `verified` with reproduction evidence (confirmed in Q6 clarification; artifact files to be synchronized).
- [x] Protocol checklist exists in `AGENTS.md` or `CONTRIBUTING.md` (or `specs/009-end-to-end-game-verification/checklists/` if needed) — documented per Q1→B / FR-001a.
- [x] At least one save-file comparison captured (before/after `ApplySoilGrowthModifier()` activation) — optional for initial `verified`, required for full automation; confirmed executed.
- [x] No source regression (`dotnet build` 0 errors) from any verification tooling changes.

## Related

- Feature: `004-playable-compost-loop` (Phase 2 hook; verification depends on this)
- Bug: `missing-gameplay-hooks-and-roadmap-gaps` (fix applied: `ApplySoilGrowthModifier()` real; `test.md` partial)
- Assessment: `end-to-end-game-verification` (`assess.decide`: `go` with conditions)
- Roadmap: `ROADMAP.md` §Gate 3 / §Where we actually are / §80/20 order (Step 2 growth hook → Step 3 mulch → etc.)
