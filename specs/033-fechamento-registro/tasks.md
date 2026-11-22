# Tasks: 033-fechamento-registro

## Phase 1: Setup

- [x] T001 Confirmar âncora `dev` @ `686fea7`, feature.json → `specs/033-fechamento-registro`
- [x] T002 Registrar R1 go em `.specify/assessments/autonomous-closeout/decision.md`

## Phase 2: Foundational

- [x] T003 Persistir artefatos de assessment em `.specify/assessments/autonomous-closeout/`
- [x] T004 Escrever spec, clarify, checklists, plan e tasks em `specs/033-fechamento-registro/`

## Phase 3: US1 — Desfecho auditável (P1)

- [ ] T005 [P] [US1] Acrescentar nota datada em `.specify/memory/STATE.md` com merges #817/#818, commits, checks e limites locais
- [ ] T006 [P] [US1] Acrescentar nota datada em `docs/PROGRESSO.md` com o mesmo conteúdo e a fila `[NV]` restante

## Phase 4: Polishing

- [ ] T007 Rodar analyze read-only, hook atômico, converge, R4 validate e R7 release
- [ ] T008 Executar `aggregate-gate.sh`, `release-gate.sh`, pytest e i18n; registrar saídas
- [ ] T009 Abrir PR reviewable para `dev/` via `gh pr create`, sem merge/auto-merge/force-push

## Dependencies

T001→T004 → T005/T006 → T007 → T008 → T009.
