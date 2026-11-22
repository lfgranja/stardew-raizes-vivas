# Problem Definition: Fechamento autônomo dos ciclos 031 e 032

- **Slug**: autonomous-closeout
- **Created**: 2026-11-22T05:01:49Z
- **Inputs used**: intake.md | research.md

## Problem Statement

O desfecho dos dois últimos ciclos autônomos está espalhado entre logs de jobs com tetos diferentes e rollups remotos; sem uma nota datada nas âncoras, um leitor não consegue distinguir "mesclado com checks verdes" de "gate local interrompido por limite de tempo", nem quais `[NV]` ainda exigem humano.

## Affected Users & Stakeholders

- **Users**: maintainer, revisores futuros, agentes que retomam a sessão.
- **Stakeholders**: dono da política de verificação e do roteiro SDD.

## Goals

- Consolidar em `STATE.md` e `docs/PROGRESSO.md` o que foi verificado ao vivo, com datas e IDs.
- Declarar explicitamente que gates locais com teto não viraram PASS.
- Declarar a fila residual como integralmente humano/owner-gated.

## Non-Goals

- Reescrever artefatos dos ciclos 031/032 ou alterar código, cenários, workflows, contratos.
- Simular atestação humana, D1, SC-004, tag/release ou aprovador.

## Success Metrics

- 2 âncoras com nota datada e IDs de merge/checks (baseline: 0).
- 0 afirmações de `ALL STEPS PASSED` sem saída local completa.
- Gates determinísticos do alvo verdes no HEAD do ciclo.

## Cost of Inaction

Risco de leitura errada do estado e repetição de trabalho já concluído.

## Open Questions

- Nenhuma.
