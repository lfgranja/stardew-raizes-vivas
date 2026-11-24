# Concept: Promoção dev→main CI-green na fila SWE

- **Slug**: swe-escalation-queue
- **Created**: 2026-11-22T05:32:37Z
- **Recommended option**: Option A

## Options

### Option A — Item append-only na fila existente

- **Sketch**: acrescentar `ESC-LR-20261122-008` em `escalations.yaml` com `tipo_023: autorizacao`, bloqueio = 1 aprovação, evidência do CI green; notas em STATE/PROGRESSO.
- **Appetite**: small
- **Trade-offs**: acionável e auditável; nenhuma autorização simulada.
- **Rabbit holes**: confundir CI green com merge autorizado.

### Option B — Mesclar em main

- **Sketch**: usar privilégio de admin para mesclar apesar de `REVIEW_REQUIRED`.
- **Appetite**: small
- **Trade-offs**: viola proteção e a própria diretiva de escalar para SWE; rejeitado.

## Recommendation

**Option A**.

## Out of Scope

Merge em main, tag/release, código, workflows, cenários, contratos.

## Assumptions to Validate

`aggregate-gate.sh` aceita R4/R7 do ciclo.
