# Decision: Quality Gate pytest wiring and dead suppression cleanup

- **Slug**: quality-gate-pytest-wiring
- **Date**: 2026-11-20T01:20Z
- **Verdict**: **go**

## Scoring

| Criterion | Rating | Justification |
|---|---|---|
| Problem validity | strong | Gate Python vermelho (177/3) observado por comando nesta sessão; gates "verdes" não o cobrem. |
| Evidence strength | strong | Falhas, pins, pragma e ausência de pytest no CI lidos/executados localmente com paths e linhas. |
| Value vs. cost of inaction | strong | Corrige dívida P1 antiga; cada bump futuro repete o drift. |
| Feasibility / appetite fit | strong | Option A: ~4 arquivos, mudanças lineares, gates locais disponíveis. |
| Strategic fit | strong | Alinha constitution (TDD, YAGNI, gate 3) e política de analyzer documentation. |
| Risk posture | strong | Riscos identificados (S8969, workflow contract, Gate 3) e mitigados por verificação prévia; sem secrets/auto-merge. |

## Rationale

Todos os critérios `strong`; opção recomendada com appetite small e evidência reproduzível. Encaminha para `/speckit-specify` com slug `quality-gate-pytest-wiring`. Gate 3 será executado como `machine-pass` headless; `verified` E e merge permanecem owner-gated **[NV]**.

## Non-goals carried forward

Csprojs, D1, atestação humana, tag/release, segredos, `RUNNER_AUDIT_STORE`, merge/auto-merge.
