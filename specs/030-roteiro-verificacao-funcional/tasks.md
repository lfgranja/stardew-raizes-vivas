# Tasks: Roteiro de verificação funcional

**Input**: Design documents de `/specs/030-roteiro-verificacao-funcional/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: a própria matriz é a verificação; nenhum teste de código novo

**Organization**: tarefas agrupadas por user story; cada fase é um incremento verificável.

## Phase 1: Setup

- [x] T001 Confirmar âncora `.specify/feature.json` → `specs/030-roteiro-verificacao-funcional` e branch `feature/030-roteiro-verificacao-funcional`
- [x] T002 [P] Executar gates determinísticos iniciais (Camada A) e registrar saídas com data/hora

## Phase 2: Foundational (bloqueia as histórias)

- [x] T003 Mapear as 11 features implementadas para os filtros xUnit exatos (B1–B11)
- [x] T004 [P] Mapear os 7 specs com `test.md` para seus cenários Gate 3 e registrar os sem cenário

## Phase 3: User Story 1 — Matriz auditável (Priority: P1)

**Goal**: matriz persistida com comando, expectativa, resultado e limite de honestidade para toda feature implementada.
**Independent Test**: abrir `verification-matrix.md` e executar B1 com 0 falhas.

- [x] T005 [P] [US1] Persistir `specs/030-roteiro-verificacao-funcional/verification-matrix.md` com as Camadas A/B/C/D
- [x] T006 [US1] Executar os 11 grupos B1–B11 e preencher as contagens observadas na matriz
- [x] T007 [US1] Registrar as features sem cenário Gate 3 como lacuna de evidência, não como falha

## Phase 4: User Story 2 — Evidência end-to-end coerente (Priority: P2)

**Goal**: threshold do cenário hardening coerente com `PoorModerateBandEdge = 31f` e bundle revalidado.
**Independent Test**: `grep` do threshold e reexecução do cenário com 3 sinais.

- [x] T008 [US2] Reler o bloco `thresholds` completo e aplicar `"max": 31`, `exclusiveMin: false`, `exclusiveMax: true`, descrição "[0, 31)"
- [x] T009 [US2] Varrer os demais cenários quanto a limites legados e listar ocorrências sem alterá-las
- [x] T010 [US2] Reexecutar o Gate 3 do cenário alterado sob Xvfb e verificar o bundle com `--verify-bundle`
- [x] T011 [US2] Anexar o novo `runId` e o veredito a `specs/004-playable-compost-loop/test.md`

## Phase 5: User Story 3 — Estado honesto (Priority: P3)

**Goal**: notas datadas com contagens e `[NV]`; nenhuma atestação promovida.
**Independent Test**: comparar notas com saídas de gate e checar ausência de `verified`.

- [x] T012 [P] [US3] Registrar nota datada em `.specify/memory/STATE.md` com âncora, ciclo, gates e dívidas
- [x] T013 [P] [US3] Registrar nota datada em `docs/PROGRESSO.md`/`OBJETIVO.md`/`ROADMAP.md` preservando append-only
- [x] T014 [US3] Registrar o desfecho de #811/#812 (merge observado) e manter D1/E/SC-004 human-gated

## Phase 6: Polish & Cross-Cutting

- [x] T015 Reexecutar os gates da Camada A no fechamento e colar saídas
- [x] T016 Confirmar zero diff em `LivingRoots/**` e persistir `validate.md` (R4) e `release.md` (R7)
- [x] T017 Abrir PR reviewable para `dev` via `gh pr create` com relatório de evidências; sem auto-merge/force-push/delete

## Dependencies

T001 → T002 → T003/T004 (paralelos) → T005 → T006 → T007 → T008 → T009 → T010 → T011 → T012/T013/T014 (paralelos) → T015 → T016 → T017.
