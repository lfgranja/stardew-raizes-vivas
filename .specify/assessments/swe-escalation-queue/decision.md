# Decision: Promoção dev→main CI-green na fila SWE

- **Slug**: swe-escalation-queue
- **Decided**: 2026-11-22T05:32:37Z
- **Verdict**: go
- **Artifacts reviewed**: intake | research | problem | concept

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | Bloqueio humano real e verificado ao vivo. |
| Evidence strength | strong | `gh pr checks --watch` RC=0 + proteção lida via API. |
| Value vs. inaction | strong | Transforma CI green em item acionável para o SWE. |
| Feasibility / appetite | strong | Um item YAML + duas âncoras. |
| Strategic fit | strong | Respeita owner-gating e proteção de branch. |
| Risk posture | strong | Nenhuma autorização simulada. |

## Verdict & Rationale

**go.** Registro documental, determinístico, sem merge em main.

## Handoff

- **Problem**: bloqueio de aprovação não registrado na fila oficial.
- **Chosen**: Option A.
- **In scope**: escalations.yaml, STATE, PROGRESSO, artefatos 035.
- **Out**: merge/tag/release/código.
- **Metrics**: 1 item novo; 0 merges em main.
