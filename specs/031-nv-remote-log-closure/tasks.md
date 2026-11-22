# Tasks: 031-nv-remote-log-closure

**Input**: [plan.md](plan.md) · [spec.md](spec.md) · [data-model.md](data-model.md) · [research.md](research.md)

**Convention**: `- [ ] [TaskID] [P?] [Story?] Descrição com caminho exato.`

## Phase 1: Setup

- [x] T001 Confirmar âncora, feature.json e worktree isolado em `.specify/feature.json`
- [x] T002 Registrar decisão 80/20 e R1 go em `.specify/assessments/nv-remote-log-closure/decision.md`

## Phase 2: Foundational

- [x] T003 Persistir intake, research, problem, concept e decision em `.specify/assessments/nv-remote-log-closure/`
- [x] T004 Escrever spec, clarify, checklist de requisitos, plano, research de fase 0, data-model e quickstart em `specs/031-nv-remote-log-closure/`
- [x] T005 Gerar checklist de qualidade de requisitos em `specs/031-nv-remote-log-closure/checklists/evidence-quality.md` e revisar 15 itens

## Phase 3: User Story 1 — Evidência remota persistida (P1)

**Independent Test**: ler `research/2026-11-22-remote-nv-closure.md`, conferir 3 run IDs, trechos literais e timestamp.

- [x] T006 [P] [US1] Criar `research/2026-11-22-remote-nv-closure.md` com comandos, run IDs 44641194770/44664287245/44664266743, step `Fetch game assemblies` e trechos de erro
- [x] T007 [P] [US1] Indexar a nova pesquisa em `docs/research/index.md` com nota datada

## Phase 4: User Story 2 — Âncoras de estado honestas (P2)

**Independent Test**: ler as quatro âncoras e distinguir `[NV]` resolvido de humano/owner-gated.

- [x] T008 [P] [US2] Acrescentar nota datada em `.specify/memory/STATE.md` com o `[NV]` resolvido e a lista explícita dos `[NV]` preservados
- [x] T009 [P] [US2] Acrescentar nota datada em `docs/PROGRESSO.md` com a âncora do ciclo 031 e o limite de honestidade
- [x] T010 [US2] Atualizar a seção `[NV] boundary` em `docs/quality-gate.md:48` com evidência datada, preservando a regra geral para afirmações remotas não observadas

## Phase 5: Polishing & Cross-Cutting

- [x] T011 Rodar `analyze` read-only e registrar 0 achados críticos; corrigir na fonte se houver conflito
- [x] T012 Rodar converge append-only, R4 validate e R7 release em `specs/031-nv-remote-log-closure/` e `.specify/{validations,releases}/`
- [ ] T013 Executar gates determinísticos (`aggregate-gate.sh`, `release-gate.sh`, pytest quality-gate, i18n parity) e verificar em `git diff --name-only dev...HEAD` que nenhum artefato histórico 025/030 nem path de `LivingRoots/`, `.github/workflows/` ou `scripts/ci/` foi alterado (FR-005/FR-007/SC-005)
- [ ] T014 Abrir PR reviewable para `dev/` via `gh pr create`, sem merge/auto-merge/force-push

## Dependencies

- T001 → T002 → T003/T004 → T005 → T006/T007 → T008/T009/T010 → T011 → T012 → T013 → T014.
- US1 e US2 são independentes entre si; US2 depende apenas da existência do documento de pesquisa (T006).

## Parallel Execution Examples

```bash
# Após T007: T008, T009 e T010 tocam arquivos distintos
```

## Implementation Strategy

MVP = US1 (evidência persistida). Em seguida US2 (âncoras). Nenhuma mudança de código; rollback = reversão de commits documentais.
