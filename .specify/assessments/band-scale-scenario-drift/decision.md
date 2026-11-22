# Decision: Alinhar bandas 31/71 nos cenários ativos 004 e 005

- **Slug**: band-scale-scenario-drift
- **Decided**: 2026-11-22T03:49:24Z
- **Verdict**: go
- **Artifacts reviewed**: intake.md | research.md | problem.md | concept.md

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | Contradição verificável entre cenários executáveis e `ModConstants`. |
| Evidence strength | strong | Citações exatas, bundles com valor 80, investigação read-only independente. |
| Value vs. inaction | adequate | Remove dívida ativa de verificação; não desbloqueia humans. |
| Feasibility / appetite | strong | 4 linhas + 2 runs headless; sem código. |
| Strategic fit | strong | Reforça honestidade do roteiro de verificação (ciclo 030). |
| Risk posture | strong | Riscos conhecidos elimitados: registrar `partial` sem promover, nunca editar marcadores à mão. |

## Verdict & Rationale

**go.** Evidência high-confidence, escopo cirúrgico, nenhuma autorização owner/humana necessária para `machine-pass` host-only.

## If go — Handoff to `/speckit-specify`

- **Problem**: dois cenários ativos declaram Healthy `[67,100)` contra `[71,100]` do domínio.
- **Chosen approach**: Option A.
- **In scope / out of scope**: 2 cenários + artefatos SDD + 2 runs Gate 3; fora 010, código, workflows, humano-gated.
- **Success metrics**: 4 linhas alteradas; 2 runs pass/R/3 sinais; gates verdes.
- **Carried-forward open questions**: nenhuma.
