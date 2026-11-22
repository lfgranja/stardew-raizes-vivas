# Tasks: 032-alinhar-bandas-cenario

**Input**: [plan.md](plan.md) · [spec.md](spec.md) · [data-model.md](data-model.md) · [research.md](research.md)

## Phase 1: Setup

- [x] T001 Confirmar âncora e resolver NNN=032 via `--number` (colisão com ciclo 031 em voo) em `.specify/feature.json`
- [x] T002 Registrar R1 go em `.specify/assessments/band-scale-scenario-drift/decision.md`

## Phase 2: Foundational

- [x] T003 Persistir intake/research/problem/concept/decision em `.specify/assessments/band-scale-scenario-drift/`
- [x] T004 Escrever spec, clarify, checklists, plan, research de fase 0, data-model e quickstart em `specs/032-alinhar-bandas-cenario/`

## Phase 3: US1 — Contrato de cenário coerente (P1)

**Independent Test**: ler os 2 cenários e confirmar `min: 71` e `[71, 100)`.

- [x] T005 [P] [US1] Alterar `min: 67 → 71` e a descrição para `[71, 100)` em `specs/004-playable-compost-loop/scenarios/compost-growth-healthy.scenario.json`
- [x] T006 [P] [US1] Alterar `min: 67 → 71` e a descrição para `[71, 100)` em `specs/005-soil-health-to-crop-growth/scenarios/soil-health-growth.scenario.json`
- [x] T007 Verificar que o diff dos 2 cenários é exatamente 4 linhas e nenhum campo protegido mudou

## Phase 4: US2 — Evidência promovida pelo runner (P2)

**Independent Test**: 2 runs `pass` com 3 sinais e blocos atualizados pelo runner.

- [x] T008 [P] [US2] Compilar o avaliador no worktree (`dotnet build LivingRoots.Verification.Runner/... -c Release`) e reexecutar Gate 3 de `compost-growth-healthy` com `--no-acquire --skip-build`, registrando run ID/veredito
- [x] T009 [P] [US2] Reexecutar Gate 3 de `soil-health-growth` com `--no-acquire --skip-build` (avaliador já compilado) e registrar run ID/veredito
- [x] T010 [US2] Registrar notas append-only em `specs/004/test.md`, `specs/005/test.md` e `docs/PROGRESSO.md`, sem editar blocos de marcadores manualmente

## Phase 5: Polishing & Cross-Cutting

- [ ] T011 Rodar `analyze` read-only, `converge`, R4 validate e R7 release
- [ ] T012 Executar gates determinísticos (`aggregate-gate.sh`, `release-gate.sh`, pytest, i18n) e verificar escopo do diff
- [ ] T013 Abrir PR reviewable para `dev/` via `gh pr create`, sem merge/auto-merge/force-push

## Dependencies

- T001→T004 (artefatos) → T005/T006 → T007 → T008/T009 → T010 → T011 → T012 → T013.
- T008 e T009 são independentes; T010 depende de ambos.

## Implementation Strategy

MVP = US1; evidência = US2. Nenhuma mudança de código; rollback = revert + reexecução dos cenários antigos.
