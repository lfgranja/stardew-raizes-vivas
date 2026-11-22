# Problem Definition: Remover transições duplicadas de promoção

- **Slug**: promotion-dedupe
- **Created**: 2026-11-22T05:52Z
- **Inputs used**: intake.md | research.md

## Problem Statement

O gate autoritativo do alvo está RED em `dev`: `test-spec-consistency.sh` detecta 3 transições duplicadas em `promotion-triggers.json`, introduzidas por promoção manual redundante nesta sessão.

## Goals

- Restaurar unicidade de `(component, runId)` no registro.
- Fazer `test-spec-consistency.sh` e o smoke passarem.

## Non-Goals

- Reescrever blocos `GATE3`, código, cenários, contratos ou workflows.
- Alterar transições de outros runs.

## Success Metrics

- 0 duplicatas; `test-spec-consistency.sh` exit 0; smoke 51/50/0/1.

## Cost of Inaction

Gate RED bloqueia o PR #821 (dev→main) e qualquer entrega futura.

## Open Questions

- Nenhuma.
