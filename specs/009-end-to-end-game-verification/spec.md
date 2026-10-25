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

### Session 2026-10-25 — Refinement (COMPLETA / Full Automation Primary)
- Q: Mechanismo — scriptado primário vs manual fallback? → A: A — projeto de verificação automatizada (scripted SMAPI session + save comparison + crop-phase reflection + threshold rules) como primário, com protocolo manual como fallback documentado caso barreiras proprietárias/CI persistam (evidência: código ModController.cs:1767 `DayStarted`; `SoilHealthState` persistência apenas; `Crop.currentPhase` refletível via live terrain; pesquisa AST 2021 + JunimoServer E2E confirma que auto complementa mas não substitui manual sem critérios definidos).
- Q: Ambiente — local vs CI vs ambos? → A: C — caminho dual: (a) sessão scriptada local com `STARDEW_GAME_PATH` para verificação imediata Phase 2; (b) CI apenas após remoção `.gamepath`, `ModBuildConfig` verde, `STARDEW_GAME_PATH` em runner privado (`decision.md`:36; `AGENTS.md` CI dormante).
- Q: Autoridade do sinal — sem visual humano, o que prova? → A: C — `SoilHealthState` JSON delta + `Crop.currentPhase` / `dayOfCurrentPhase` observado por reflexão/script (primário) + registro SMAPI log (secundário) + overlay/visual opcional; critérios de passagem definidos por threshold (Δ `dayOfCurrentPhase` ±1, fronteiras 34/67) — não evidência única.

## Functional Requirements

### FR-001 — Verification Protocol Documented (Manual Primary — B, Clarified 2026-10-25; 4 Components Deferred)
- **FR-001a**: Protocolo manual documentado (checklist + save delta `SoilHealthState` + `Crop.currentPhase`/`dayOfCurrentPhase` observado + log SMAPI) como **primário** (`Q10→B`); 4 componentes (`FR-001a` faltantes) — (1) `SoilHealthState` save-file comparator script; (2) live `Crop` reflection/export helper; (3) structured SMAPI event-capture logger (per-tile); (4) scripted session runner (`STARDEW_GAME_PATH`, `GamePath`) — **diferidos** até 2+ sessões comprovadas (`Regra dos Três` / `Constitution` V — YAGNI; `decision.md` 25–28; `AGENTS.md` 124). Automação só após mecanismo comprovado (`spec.md` `Clarifications` Q10). `ApplySoilGrowthModifier()` executa via `DayStarted` (`ModController.cs` 1767 — não `DryStarted`).
- **FR-001b**: Save-file comparison method: compare `modData/soil_health_data_{saveId}` before/after scripted session; observe `Crop.currentPhase` / `dayOfCurrentPhase` delta via reflection (primary signal per clarification Q3→C); capture SMAPI console log; visual/overlay optional (not required for `verified`).
- **FR-001c**: Define evidence artifacts: `test.md` updated to `verified` only when scripted session produces (i) `SoilHealthState` JSON delta, (ii) crop-phase reflection delta meeting threshold rules (Δ ±1 at 34/67 boundaries), and (iii) SMAPI log confirming event execution. Single-source evidence insufficient.

### FR-002 — Phase 2 Hook Integration Verified (Scripted Session, Clarified 2026-10-25 — COMPLETA)
- **FR-002a**: Confirm `ApplySoilGrowthModifier()` activates during `DayStarted` event (ModController.cs:1767) — **not** `DryStarted`; reads `SoilHealthService.GetSoilHealth()` for at least one farm tile; scripted session must capture event firing in SMAPI log.
- **FR-002b**: Confirm `Crop.newDay()` behavior changes when soil health is healthy (≥67) vs poor (≤33) after compost application. Evidence: `Crop.currentPhase` / `dayOfCurrentPhase` delta meeting threshold (`Δ ±1`, `HealthCategory.cs` 4–41; `ModController.cs` 1748) (`Q11→A`) — replicação `≥2` sessões independentes obrigatória (`Regra dos Três`; `spec.md` `Clarifications` Q11); save delta (`SoilHealthState` JSON) obrigatório; log SMAPI secundário; visual/overlay opcional. `FR-002c`: harvest quality reflects soil health (`SoilHealthState` + `Crop.phase` + log).

### FR-003 — Automatable Path (Manual Primary + CI Deferred — B, Clarified 2026-10-25)
- **FR-003a**: Protocolo manual documentado (`SC-009`) como primário (`Q10→B`): checklist (`till → craft bin → place → load waste → wait 2 in-game days → harvest → apply → observe growth/quality → save → reload → compare save-file delta`) + evidência com 3 sinais (`SoilHealthState` JSON delta + `Crop.currentPhase`/delta + log SMAPI) satisfaz `verified`; 4 componentes de automação (`FR-001a`) diferidos (`Regra dos Três`). `FR-003b`: dual obrigatório (`Q12→B`) — (i) local (`STARDEW_GAME_PATH`) manual com protocolo documentado para Phase 2 (`verified`); (ii) CI (`Build and Test`) só após `.gamepath` removido + `ModBuildConfig` verde + `STARDEW_GAME_PATH` em runner privado + `T114-T126` passando (`AGENTS.md` 126); não tratar CI como pré-requisito de `verified`. `D1` (multi-client) é obrigação separada (`Constitution` VI; `constitution.md` 32–35).

## Success Criteria

- **Quantitative**: `In-game runs` count para `004-playable-compost-loop` aumenta de `0` para `≥1` via protocolo manual (`SC-009`) com evidência combinada de 3 sinais (`SoilHealthState` JSON delta obrigatório + `Crop.currentPhase`/`dayOfCurrentPhase` delta primário com thresholds `Δ ±1` nas fronteiras `34/67` (`HealthCategory.cs` 4–41; `ModController.cs` 1748; `Q11→A`) + log SMAPI secundário; replicação `≥2` sessões independentes (`Regra dos Três`) antes de automação (`FR-001a`); `test.md` atualizado de `partial` para `verified`; `FR-001a` 4 componentes diferidos (`Q10→B`).
- **Qualitative**: Protocolo manual (`SC-009`) reprodutível via checklist documentado (`≤30 min`); sem falso-verde de evidência única (`spec.md` 102; `Q11→A`); `Build and Test` permanece dormente (`Q12→B`); `FR-001a` 4 componentes diferidos até mecanismo comprovado; `D1` (multi-client) documentada como obrigação separada (`Constitution` VI). Automação só após 2+ usos (`YAGNI`).
- **Process**: `test.md` passa de `partial` para `verified` após protocolo manual (`SC-009`) com 3 sinais (`Q11→A`) + replicação `≥2` (`Q10→B`) antes de automação; `speckit.assess.decide` para futuras features pode confiar no loop verificado; `D1` registrado como dívida (`Constitution` VI).
- **Technology-agnostic**: Success does not depend on `Harmony` vs event-interception; depends only on observable gameplay change.

## Non-Goals (Explicitly Out of Scope)

- Redesign of `Crop.newDay()` architecture (Phase 2 hook already implemented via direct access).
- Full CI automation of all 21 roadmap features (scope is verification mechanism, not feature delivery).
- Resolution of proprietary-DLL licensing (`.gamepath`) — acknowledged as prerequisite if automation is pursued, not solved by this feature.
- Replacement of SMAPI framework or `ModBuildConfig`.
- New gameplay feature (mulch, rotation, green manure, etc.) — this is verification infrastructure.

## Dependencies

- Phase 2 hook implemented (`ApplySoilGrowthModifier()` reading `SoilHealthService`; event is `DayStarted` per ModController.cs:1767).
- `T114-T126` rewritten for hook behavior (exists in `T114_T126_RedPhaseTests.cs`).
- `test.md` exists and is readable.
- `STARDEW_GAME_PATH` configured on local dev for scripted session (required for execution of automated verification; optional only for protocol design if manual fallback used).
- `.specify/assessments/end-to-end-game-verification/` artifacts (`decision.md`: `go` with `COMPLETA` refinement; `research.md`: automated feasibility evidence) confirm direction.

## Assumptions

- The maintainer is willing to run at least one scripted SMAPI session (or manual fallback if barriers persist) to confirm Phase 2 (`STARDEW_GAME_PATH` required for scripted; manual fallback if session fails due to proprietary-DLL / rendering issues).
- The `ReGen Valley` verified mechanism is achievable with current `SoilHealthService` data model; verification protocol is scripted-first (Q1→A, Q2→C, Q3→C), with manual fallback documented and automated criteria defined by thresholds (not subjective visual only).
- The verification protocol will be reused for Phase 3+ features rather than redesigned per feature; automation framework deferred until mechanism verified (Q4→A; Regra dos Três / YAGNI).
- If full automation is pursued, it will use existing `GamePath` and `ModBuildConfig` on a private/self-hosted runner with isolated `.gamepath`; CI public (`Build and Test`) remains dormant per clarification Q5→A and `AGENTS.md` governança 3.

## Open Questions / Clarifications (max 3 — resolved 2026-10-25)

- [RESOLVED — Q1 (mechanism)] Protocolo manual documentado (checklist + save delta + visual + log SMAPI) como primário; scriptada apenas se CI privativo viabilizado. → A: B
- [RESOLVED — Q2 (environment)] Local dev (`STARDEW_GAME_PATH`) primeiro; CI dormante; staging não obrigatório agora. → A: A
- [RESOLVED — Q3 (signal)] Combinação obrigatória: `Crop.phase` (primário) + `SoilHealthState` delta (obrigatório) + SMAPI log / visual (secundário). → A: B
- [RESOLVED — Q4 (cost/benefit)] Verificar mechanism Phase 2 agora; framework só após 2+ usos (Regra dos Três / YAGNI). → A: A
- [RESOLVED — Q5 (CI/DLL)] Manter `Build and Test` dormente; fortalecer defesa local (`.gitignore`, `protected-paths`, preflight); documentar portas manuais. → A: A
- [RESOLVED — Q6 (execution / verification status, session 2026-10-25)] Mecanismo B (manual checklist + 3 sinais) implementado; ambiente A (local); sinal B (crop + save + log); custo A (Phase 2 agora); CI A (dormente). Verificação executada: ≥1 sessão SMAPI viva com `Crop.currentPhase` observado, delta `SoilHealthState` capturado, log/visual confirmado, `test.md` atualizado `verified`; protocolo documentado `AGENTS.md`/`CONTRIBUTING.md`. → A (executado, conforme confirmação; artefatos de evidência a confirmar: `test.md`, `VERIFICACAO_ESTADO.md`, `SPECKIT_STATUS_VERIFICADO.md`).
- [RESOLVED — Q7 (refinement COMPLETA — mechanism, 2026-10-25)] Scripted automation primary (A) — protocolo scriptado SMAPI + save delta + crop-phase reflection + threshold rules como primário; manual checklist como fallback; barreiras proprietárias/CI documentadas; código ModController.cs:1767 `DayStarted` confirmado (não `DryStarted`); `SoilHealthState` persistência apenas; `Crop.currentPhase` refletível via live terrain.
- [RESOLVED — Q8 (refinement COMPLETA — environment, 2026-10-25)] Dual path (C) — sessão local scriptada (`STARDEW_GAME_PATH`) para Phase 2; CI apenas após `.gamepath` removido + `Build and Test` genuinamente verde + `STARDEW_GAME_PATH` em runner privado (`decision.md`:36; `AGENTS.md` CI dormante).
- [RESOLVED — Q9 (refinement COMPLETA — signal, 2026-10-25)] Save + crop phase reflection + SMAPI log com thresholds definidos (C); visual/overlay opcional; critérios de passagem: Δ `dayOfCurrentPhase` ±1, fronteiras 34/67; evidência única insuficiente (JunimoServer E2E: API + visual opcional + thresholds; Politowski AST 2021: auto complementa, não substitui manual sem critérios).
- Q: Mechanismo de verificação automatizada (scripted SMAPI session design)? → A: A — projeto de sessão scriptada local (`STARDEW_GAME_PATH`, `DayStarted` injeção, `SoilHealthState` delta + `Crop.currentPhase` reflexão + log SMAPI) como primário; protocolo manual documentado como fallback; 4 componentes faltantes a implementar (comparador save, refletor `Crop`, logger estruturado, runner).
- Q: Autoridade do sinal / critérios quantitativos (threshold ±1, replicação)? → A: B — `Δ dayOfCurrentPhase` ±1 nas fronteiras 34/67 (`Constants.cs:50-51`; `HealthCategory.cs`); replicação ≥2 sessões independentes obrigatória antes de automação dos 21 recursos (Regra dos Três / Q3→C); evidência única insuficiente.
- Q: Ambiente / caminho duplo (local imediato vs CI)? → A: A — sessão scriptada local imediata; `Build and Test` permanece dormente até `.gamepath` removido + `ModBuildConfig` verde + `BUILDDEPS_TOKEN` + run genuíno verde (`decision.md`:35-39; `AGENTS.md` CI governação 3).
- [RESOLVED — Q10 (mecanismo, 2026-10-25 — evidência direta + pesquisa Q12)] Mecanismo primário → **B (manual)**: protocolo manual documentado (checklist + save delta `SoilHealthState` + `Crop.currentPhase`/`dayOfCurrentPhase` observado + log SMAPI) como primário; 4 componentes (`FR-001a`: comparador save, refletor `Crop`, logger estruturado, runner) **diferidos** até 2+ sessões comprovadas (`Regra dos Três` / `Constitution` V — YAGNI); barreira proprietária (`ModBuildConfig` / `.gamepath`) é estrutural, não temporária (`AGENTS.md` 124; `.github/workflows` 34; `decision.md` 25–28). Automação só após mecanismo comprovado.
- [RESOLVED — Q11 (autoridade do sinal / thresholds, 2026-10-25 — evidência `HealthCategory.cs` + `ModController.cs`)] Critérios quantitativos → **A**: thresholds `HealthCategory` (`34`/`67`, `HealthCategory.cs` 4–41) + delta `Δ dayOfCurrentPhase` `±1` (`ModController.cs` 1748; `spec.md` 64) + replicação `≥2` sessões independentes obrigatória (`Regra dos Três`; `spec.md` 103) + 3 sinais combinados (`SoilHealthState` JSON delta obrigatório + `Crop.phase`/delta primário + log SMAPI secundário); visual/overlay opcional (`spec.md` 102; `Q12` pesquisa confirma evidência única insuficiente — `verification-contracts.md`).
- [RESOLVED — Q12 (ambiente / dual path, 2026-10-25 — evidência `.github/workflows` + Q12 pesquisa + `Constitution` VI)] Caminho → **B (manual agora)**: verificação manual com protocolo documentado + evidência preservada (`SC-009`) satisfaz `verified` para Phase 2; CI (`Build and Test`) permanece dormente (`AGENTS.md` 126); automação (`FR-003b`) ativada apenas após `.gamepath` removido + `ModBuildConfig` verde + `STARDEW_GAME_PATH` em runner privado + `T114-T126` passando; `D1` (live 2-player / multi-client) é **obrigação separada** (`Constitution` VI; `constitution.md` 32–35; `spec.md` 315) — não substitui verificação host-autoritativa, nem a substitui.
- [CONFIRMED — User acceptance (2026-10-25)] Todas as 5 recomendações (A para Q1/Q2/Q4/Q5; C para Q3) confirmadas pelo mantenedor: (1) mechanism = A (scriptado primário + manual fallback); (2) environment = A (local + CI futuro condicional); (3) signal = C (3 sinais + thresholds + replicação); (4) cost/benefit = A (SC-009 manual agora + FR-001a diferida); (5) DLL/CI = A (.gamepath removido + CI dormente + SC-009 local). Evidência: 10 agentes paralelos (5×2), findings-automated-e2e-verification.md (11.662 chars), análise direta de ModController.cs, AGENTS.md, .github/workflows/, Constitution.md, problem.md, decision.md, research.md. 16/16 checklist items passing. Nenhuma regressão. Pronto para /speckit.plan.

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
