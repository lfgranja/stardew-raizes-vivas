# Decision: Fechar [NV] de causa-raiz remota dos PRs #790–#796

- **Slug**: nv-remote-log-closure
- **Decided**: 2026-11-22T03:34:24Z
- **Verdict**: go
- **Artifacts reviewed**: intake.md | research.md | problem.md | concept.md

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | O prompt pede explicitamente a resolução máxima dos `[NV]`; a causa step-level é verificável com comando determinístico. |
| Evidence strength | strong | Três `gh run view --log-failed` independentes, com run IDs e timestamps, lidos nesta sessão. |
| Value vs. inaction | adequate | Valor de auditoria/decisão; os PRs já estão encerrados, então não desbloqueia merges. |
| Feasibility / appetite | strong | Option A é small, docs-only, sem paths protegidos. |
| Strategic fit | strong | Reforça a regra "nenhuma afirmação remota sem observação ao vivo" da constituição e do prompt. |
| Risk posture | strong | Riscos identificados (retenção de logs, generalização indevida) mitigados por citação literal e escopo explícito. |

## Verdict & Rationale

**go.** Problema real e explicitamente solicitado, evidência high-confidence obtida ao vivo e opção small sem owner auth. A ausência de desbloqueio de merge é registrada como limite de valor, não como bloqueio.

## If go — Handoff to `/speckit-specify`

- **Problem**: `[NV]` step-level de #790–#796 permanece em âncoras de estado apesar de logs disponíveis.
- **Chosen approach**: Option A — documento de pesquisa novo + notas datadas nas âncoras, append-only.
- **In scope / out of scope**: escopo = pesquisa, STATE.md, docs/PROGRESSO.md, docs/research/index.md, docs/quality-gate.md e artefatos do ciclo 031; fora = histórico 025/030, humano/owner-gated, workflows/contratos.
- **Success metrics**: 0 `NV` sem evidência nas duas âncoras-alvo; ≥3 run IDs citados; gates locais verdes.
- **Carried-forward open questions**: nenhuma bloqueante; demais `[NV]` permanecem humano/owner-gated e devem continuar marcados.
