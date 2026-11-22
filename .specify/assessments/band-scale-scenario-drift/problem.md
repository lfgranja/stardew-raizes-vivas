# Problem Definition: Alinhar bandas 31/71 nos cenários ativos 004 e 005

- **Slug**: band-scale-scenario-drift
- **Created**: 2026-11-22T03:49:24Z
- **Inputs used**: intake.md | research.md

## Problem Statement

Dois cenários Gate 3 ativos descrevem Healthy como `[67,100)` enquanto o domínio implementa `[71,100]`; leitores da evidência automatizada veem um contrato executável contradizendo o código, e a guarda existente exclui esses arquivos em vez de corrigi-los.

## Affected Users & Stakeholders

- **Users**: revisores de Gate 3 e agentes que consultam cenários como fonte de verdade das bandas.
- **Stakeholders**: maintainer (dono da política de verificação), responsável pelo roteiro de verificação funcional (030).

## Goals

- Fazer os dois cenários ativos declararem a banda Healthy correta.
- Reexecutar Gate 3 para ambos e persistir a nova evidência via runner.
- Manter fixtures/schemas 010 e código de jogo intactos.

## Non-Goals

- Renomear ou reescrever fixtures históricos `boundary-*`.
- Alterar `exclusiveMax`, `factor`, sementes ou comandos dos cenários.
- Alterar `LivingRoots/`, `.github/workflows/` ou `scripts/ci/`.
- Promover `machine-pass` a `verified` humano.

## Success Metrics

- 4 linhas alteradas (2 por cenário), 0 mudanças fora dos 2 cenários ativos + artefatos SDD (baseline: 0).
- 2 novos runs Gate 3 com `verdict=pass`, 3 sinais concordantes e `--verify-bundle` exit 0.
- `aggregate-gate.sh` e `release-gate.sh` ALL STEPS PASSED (baseline: verdes).

## Cost of Inaction

A contradição permanece como dívida registrada; cada auditoria de banda precisa re-explicar a exceção.

## Open Questions

- Nenhuma.
