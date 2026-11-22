# Tasks: 037-dedupe-transitions

## Phase 1: Setup

- [x] T001 Confirmar âncora e feature.json → `specs/037-dedupe-transitions`
- [x] T002 Registrar R1 go em `.specify/assessments/promotion-dedupe/decision.md`

## Phase 2: Foundational

- [x] T003 Persistir assessment em `.specify/assessments/promotion-dedupe/`
- [x] T004 Escrever spec, checklist, plan e tasks em `specs/037-dedupe-transitions/`

## Phase 3: US1

- [x] T005 [US1] Remover índices 275–277 de `.transitions` em `specs/010-headless-verification-runner/promotion-triggers.json`
- [x] T006 [US1] Rodar `test-spec-consistency.sh` e confirmar 0 duplicatas

## Phase 4: Polishing

- [x] T007 Rodar analyze, converge, R4 validate, R7 release
- [ ] T008 Executar gates determinísticos e abrir PR reviewable para `dev/`

## Dependencies

T001→T005/T006 → T007 → T008.
