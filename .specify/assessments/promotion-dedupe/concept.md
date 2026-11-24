# Concept: Remover transições duplicadas de promoção

- **Slug**: promotion-dedupe
- **Created**: 2026-11-22T05:52Z
- **Recommended option**: Option A

## Options

### Option A — Remover exatamente os índices 275–277

- **Sketch**: exclusão das três entradas duplicadas com `jq del(.transitions[275:278])`, preservando o restante byte-a-byte.
- **Appetite**: small
- **Trade-offs**: correção cirúrgica e determinística; nenhuma reescrita de histórico alheio.
- **Rabbit holes**: renumerar índices ou tocar em outros runs.

### Option B — Reconstruir o arquivo do zero

- **Rejeitado**: perderia proveniência e formato.

## Recommendation

**Option A**.

## Out of Scope

Código, GATE3 blocks, contratos, workflows, merge em main.

## Assumptions

`jq` produz JSON válido e mantém a ordem das demais transições.
