# Tasks: Fechar dívida de artefatos e evidência

**Input**: Design documents de `/specs/029-artifact-evidence-closure/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: não solicitados como código; verificação determinística via `archify validate` e `aggregate-gate.sh`.

**Organization**: tarefas agrupadas por user story; cada fase é um incremento verificável.

## Phase 1: Setup

- [x] T001 Confirmar âncora `.specify/feature.json` → `specs/029-artifact-evidence-closure` e baseline 3/5 em `specs/029-artifact-evidence-closure/research.md`
- [x] T002 [P] Registrar notas datadas de estado verificado em `OBJETIVO.md` e `ROADMAP.md` sem apagar histórico

## Phase 2: Foundational (bloqueia as histórias)

- [x] T003 [P] Corrigir fases disjuntas, corredores e `mainPath` em `.archify/workflow-heirloom-pipeline-20261109-191500/candidate.json`
- [x] T004 [P] Converter `layout/components/connections` para `lanes/nodes/edges` preservando nós/arestas em `.archify/workflow-verification-pipeline-20261105-015049/candidate.json`

## Phase 3: User Story 1 — Diagramas validados (Priority: P1)

**Goal**: 5 de 5 candidatos aprovados na verificação determinística.
**Independent Test**: executar os cinco comandos de `specs/029-artifact-evidence-closure/quickstart.md`; esperado `ok:true` em todos.

- [x] T005 [US1] Executar `archify validate` nos cinco candidatos e registrar saídas em `specs/029-artifact-evidence-closure/validate-evidence.md`
- [x] T006 [US1] Reparar até 3 iterações qualquer candidato ainda inválido, registrando a razão em `specs/029-artifact-evidence-closure/research.md` (5 iterações documentadas — desvio registrado)

## Phase 4: User Story 2 — Âncoras documentais (Priority: P2)

**Goal**: âncoras com nota datada, contagens verificadas e `[NV]` explícito.
**Independent Test**: `ls docs/PROGRESSO.md docs/research/index.md` e `grep` da data e de `[NV]`.

- [x] T007 [P] [US2] Revisar `docs/PROGRESSO.md` para garantir data, âncora, contagens e `[NV]` em afirmações remotas
- [x] T008 [P] [US2] Revisar `docs/research/index.md` para garantir índice completo e marcação `[NV]`

## Phase 5: User Story 3 — Resíduos de evidência (Priority: P3)

**Goal**: resíduos com evidência auditável revisados como qualidade de requisitos; human-gated intactos.
**Independent Test**: comparar notas com fontes citadas e confirmar ausência de `verified`/D1/SC-004 promovidos.

- [x] T009 [US3] Registrar em `.specify/memory/STATE.md` a revisão de resíduos com fonte de evidência citada e rótulo de revisão de qualidade
- [x] T010 [US3] Manter pendentes e `[NV]` os itens que dependem de humano/owner em `.specify/memory/STATE.md`

## Phase 6: Polish & Cross-Cutting

- [x] T011 Executar `bash scripts/ci/aggregate-gate.sh` e anexar saída a `specs/029-artifact-evidence-closure/validate-evidence.md`
- [x] T012 Confirmar `git diff --name-only origin/dev | grep '^LivingRoots/'` vazio e nenhum `verified` reivindicado
- [x] T013 Persistir `validate.md` aprovado em `.specify/validations/029-artifact-evidence-closure/validate.md` e `release.md` em `.specify/releases/029-artifact-evidence-closure/release.md`
- [x] T014 Abrir PR para `dev/` via `gh pr create` com commits convencionais em PT-BR e relatório de evidências
- [x] T015 Observar `gh pr checks`; mesclar somente se todos os checks obrigatórios estiverem verdes

## Dependencies

- T001 → T002 → T003/T004 (paralelos) → T005 → T006 (condicional) → T007/T008 (paralelos) → T009 → T010 → T011 → T012 → T013 → T014 → T015.
- US1 é o MVP; US2 e US3 podem ser verificadas de forma independente após T002.
- Nenhuma tarefa depende de segredo, participante humano ou aprovação externa além de T015.

## Parallel Execution Examples

- Após T002: T003 e T004 em paralelo (arquivos distintos em `.archify/`).
- Após T006: T007 e T008 em paralelo (arquivos distintos em `docs/`).

## Implementation Strategy

MVP = Phase 1 + Phase 2 + US1. Incrementos seguintes adicionam âncoras, resíduos e entrega, sem tocar em `LivingRoots/**`.

