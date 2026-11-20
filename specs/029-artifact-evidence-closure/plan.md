# Implementation Plan: Fechar dívida de artefatos e evidência

**Branch**: `029-artifact-evidence-closure` | **Date**: 2026-11-20 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification de `/specs/029-artifact-evidence-closure/spec.md`

## Summary

Corrigir os dois candidatos `.archify/` que falham a verificação determinística, manter as âncoras documentais criadas com fatos locais e registrar revisão de qualidade de requisitos de resíduos com evidência auditável. Nenhuma alteração em `LivingRoots/**`; nenhuma atestação humana promovida.

## Technical Context

**Language/Version**: não aplicável ao entregável versionado; utilitário `archify` (Node) e scripts shell/Python 3 já presentes no repositório.

**Primary Dependencies**: `~/.agents/skills/archify/bin/archify.mjs`, `scripts/ci/aggregate-gate.sh`, `.specify/scripts/bash/*`, `git`/`gh` (somente leitura remota).

**Storage**: arquivos Markdown/JSON versionados (`specs/`, `.specify/`, `docs/`) e candidatos `.archify/` (gerados, gitignored).

**Testing**: `archify validate --json`, `bash scripts/ci/aggregate-gate.sh`, `git diff --name-only`, inspeção de notas datadas.

**Target Platform**: ambiente de desenvolvimento Linux do alvo; nenhum runtime de jogo necessário.

**Project Type**: reconciliação de documentação/artefatos (sem código de jogo).

**Performance Goals**: verificação dos 5 candidatos em menos de 60 s; gate agregado em menos de 30 s.

**Constraints**: zero diff em `LivingRoots/**`; nenhum `verified` humano; nenhum segredo copiado; nenhum merge com check vermelho; histórico append-only.

**Scale/Scope**: 5 candidatos archify, 2 âncoras documentais, resíduos de checkboxes com evidência já registrada.

## Constitution Check

*GATE: passa antes da Phase 0 e reavaliado após a Phase 1.*

| Princípio (constituição v2.0.0) | Resultado |
|---|---|
| I — DDD/camadas | Não aplicável: nenhum código de domínio alterado. |
| II — Segurança de dados | PASS: nenhum input de jogador; nenhum segredo; paths validados. |
| III — Async-only | Não aplicável: nenhum I/O assíncrono no entregável. |
| IV — TDD | PASS: verificações determinísticas executadas antes e depois; baseline 3/5 registrada. |
| V — Simplicidade/YAGNI | PASS: correção mínima dos candidatos, sem novo utilitário. |
| VI — Multiplayer first-class | Não aplicável: nenhuma mudança de estado de farm. |

Gate 3 (jogo real) N/A: zero alteração em `LivingRoots/**`. Nenhuma violação; seção Complexity Tracking não utilizada.

## Project Structure

### Documentation (this feature)

```text
specs/029-artifact-evidence-closure/
├── plan.md              # este arquivo
├── research.md          # decisões da Phase 0
├── data-model.md        # entidades da Phase 1
├── quickstart.md        # guia de validação da Phase 1
├── contracts/           # não aplicável (projeto interno, sem API externa)
└── tasks.md             # Phase 2 (/speckit.tasks)
```

### Source Code (repository root)

```text
.archify/
├── workflow-heirloom-pipeline-20261109-191500/candidate.json   # corrigido (fases/rotas/mainPath)
└── workflow-verification-pipeline-20261105-015049/candidate.json # convertido para lanes/nodes/edges
docs/
├── PROGRESSO.md         # âncora de estado verificado
└── research/index.md    # índice de pesquisa
OBJETIVO.md, ROADMAP.md, .specify/memory/STATE.md  # notas append-only datadas
```

**Structure Decision**: nenhum código-fonte de jogo é tocado; todas as mudanças versionadas são documentação/artefatos SDD. Os candidatos `.archify/` são locais/gitignored e não entram no PR.

## Complexity Tracking

Sem violações de constituição; seção não utilizada.

