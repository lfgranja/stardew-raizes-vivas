# Concept: Atualizar CI green final do PR 821

- **Slug**: ci-green-refresh
- **Created**: 2026-11-22T05:41:28Z
- **Recommended option**: Option A

## Options

### Option A — Atualização append-only no item existente

- **Sketch**: acrescentar chave `atualizacao_ci` ao item `ESC-LR-20261122-008` com HEAD `b3eaa94`, RC=0 e timestamp; nota em STATE/PROGRESSO.
- **Appetite**: small
- **Trade-offs**: rastreável; nenhuma reescrita do histórico do item.
- **Rabbit holes**: apagar o green anterior.

### Option B — Novo item duplicado

- **Sketch**: criar `ESC-LR-20261122-009` idêntico.
- **Appetite**: small
- **Trade-offs**: polui a fila com duplicata; rejeitado.

## Recommendation

**Option A**.

## Out of Scope

Merge em main, tags, código, workflows, cenários, contratos.

## Assumptions to Validate

YAML permanece válido após a atualização.
