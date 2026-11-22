# Decision: Correção de atribuição de evidência e SemVer

- **Slug**: evidence-attribution-fix
- **Decided**: 2026-11-22T05:19:30Z
- **Verdict**: go
- **Artifacts reviewed**: intake | research | problem | concept

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | Metadado ausente e afirmação superatribuída verificados por leitura direta. |
| Evidence strength | strong | `gh` ao vivo + leitura dos artefatos mesclados. |
| Value vs. inaction | adequate | Corrige honestidade sem desbloquear humano. |
| Feasibility / appetite | strong | 5 arquivos documentais. |
| Strategic fit | strong | Regra "nenhuma afirmação remota sem observação ao vivo". |
| Risk posture | strong | Sem código, sem merge de terceiros. |

## Verdict & Rationale

**go.** Correção documental, determinística, sem owner auth.

## Handoff

- **Problem**: SemVer ausente; atribuição dos logs ampliada além do observado.
- **Chosen**: Option A.
- **In scope**: release 032, research 031, STATE, PROGRESSO, quality-gate + artefatos 034.
- **Out**: código/owner-gated.
- **Metrics**: SemVer presente; 0 superatribuição.
