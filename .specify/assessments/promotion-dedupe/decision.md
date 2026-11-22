# Decision: Remover transições duplicadas de promoção

- **Slug**: promotion-dedupe
- **Decided**: 2026-11-22T05:52Z
- **Verdict**: go
- **Artifacts reviewed**: intake | research | problem | concept

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | Gate RED determinístico, reproduzido localmente. |
| Evidence strength | strong | Saída literal do script com índices e runId. |
| Value vs. inaction | strong | Desbloqueia o gate e o PR #821. |
| Feasibility / appetite | strong | Uma transformação JSON. |
| Strategic fit | strong | Corrige defeito introduzido por esta sessão. |
| Risk posture | strong | Mudança cirúrgica, verificável por script. |

## Verdict & Rationale

**go.** Defeito real, não interrupção; correção determinística sem owner auth.

## Handoff

- **Problem**: 3 duplicatas em `promotion-triggers.json`.
- **Chosen**: Option A.
- **In scope**: JSON + artefatos SDD.
- **Out**: código/merge main/owner-gated.
- **Metrics**: 0 duplicatas; gate verde.
