# Idea Research: Atualizar CI green final do PR 821

- **Slug**: ci-green-refresh
- **Created**: 2026-11-22T05:41:28Z
- **Evidence confidence (overall)**: high

## Users & Demand

- O SWE precisa que a fila reflita o HEAD mais recente do PR, não um rollup já superado por merge posterior. — [source: prompt + `gh`] (confidence: high)

## Data & Constraints

- `gh pr view 821` → `headRefOid=b3eaa94`, `state=OPEN`, `mergeable=MERGEABLE`, `mergeStateStatus=BLOCKED`, `reviewDecision=REVIEW_REQUIRED` (2026-11-22T05:41Z).
- `gh pr checks 821 --watch --interval 30` → RC=0 às 05:40:50Z; nenhum check não-pass/skipping.
- Item `ESC-LR-20261122-008` na fila registra o green anterior (05:31:56Z, HEAD `4b92cd8`).

## Evidence Against

- Registrar dois green pode parecer duplicação; mitigação: manter o registro histórico e acrescentar atualização datada ao mesmo item (arquivo permite update via PR).

## Sources

- GitHub API/`gh`; arquivo `specs/024-pilha-escalonamentos-nivel5/registry/escalations.yaml`.
