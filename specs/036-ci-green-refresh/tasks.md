# Tasks: 036-ci-green-refresh

## Phase 1: Setup

- [x] T001 Confirmar âncora e feature.json → `specs/036-ci-green-refresh`
- [x] T002 Registrar R1 go em `.specify/assessments/ci-green-refresh/decision.md`

## Phase 2: Foundational

- [x] T003 Persistir assessment em `.specify/assessments/ci-green-refresh/`
- [x] T004 Escrever spec, checklist, plan e tasks em `specs/036-ci-green-refresh/`

## Phase 3: US1

- [x] T005 [P] [US1] Acrescentar `atualizacao_ci` em `specs/024-pilha-escalonamentos-nivel5/registry/escalations.yaml`
- [x] T006 [P] [US1] Registrar refresh em `.specify/memory/STATE.md`
- [x] T007 [US1] Registrar refresh em `docs/PROGRESSO.md`

## Phase 4: Polishing

- [x] T008 Rodar analyze, converge, R4 validate, R7 release
- [ ] T009 Executar gates e abrir PR reviewable para `dev/`

## Dependencies

T001→T005/T006/T007 → T008 → T009.
