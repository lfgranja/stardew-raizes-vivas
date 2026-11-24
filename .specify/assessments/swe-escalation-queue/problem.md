# Problem Definition: Promoção dev→main CI-green na fila SWE

- **Slug**: swe-escalation-queue
- **Created**: 2026-11-22T05:32:37Z
- **Inputs used**: intake.md | research.md

## Problem Statement

O PR de promoção `dev`→`main` atingiu CI integralmente verde, mas o único bloqueio restante — aprovação humana — não está registrado na fila de escalonamentos oficial; sem esse registro, o maintainer não tem um item acionável.

## Goals

- Inserir item append-only na fila com evidência do CI e do bloqueio.
- Registrar em STATE/PROGRESSO que o merge em `main` e a tag/release são owner-gated.

## Non-Goals

- Mesclar em `main`, criar tag, publicar release ou simular aprovação.

## Success Metrics

- 1 novo item `ESC-LR-*` na fila (baseline 0).
- 0 merges em `main` realizados por este ciclo.

## Cost of Inaction

Bloqueio humano invisível na fila; risco de retrabalho ou interpretação de que CI verde autoriza merge.

## Open Questions

- Nenhuma.
