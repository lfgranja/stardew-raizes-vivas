# Decision: Roteiro de verificação funcional

- **Slug**: roteiro-verificacao-funcional
- **Decided**: 2026-11-21
- **Verdict**: go
- **Artifacts reviewed**: intake.md yes | research.md yes | problem.md yes | concept.md yes

## Scorecard

| Criterion | Rating | Justification |
|---|---|---|
| Problem validity | strong | Pedido literal do Software Engineer e deriva 34→31 lida nos dois lados. |
| Evidence strength | strong | Gates, contagens, logs do `gh` e leitura de código executados nesta sessão; nenhuma afirmação remota não verificada como fundamento. |
| Value vs. inaction | strong | Sem a matriz, a resposta ao pedido não é auditável; com ela, revisores reutilizam o artefato. |
| Feasibility / appetite | adequate | Option A é small; único risco é a revalidação do cenário editado. |
| Strategic fit | strong | Respeita a constituição v2.0.0, a distinção `machine-pass` ≠ `verified` e o gate autoritativo do alvo. |
| Risk posture | adequate | Riscos identificados e limitados por non-goals, contingência de restauração e fail-closed de merge. |

## Verdict & Rationale

**go**. O problema é real e verificável por comando, a evidência é local e executada nesta sessão, e a Option A tem appetite small com risco contido. É o único candidato da fila 80/20 que entrega o pedido sem depender de humano/owner; #811/#812 dependem de segredo owner-gated (agravado por um pin de teste em #811) e D1/atestado/SC-004 são human-gated. Nenhuma afirmação `verified` é promovida.

## If go — Handoff to `/speckit-specify`

- **Problem**: falta de matriz auditável feature→verificação e deriva entre cenário Gate 3 e constantes de domínio.
- **Chosen approach**: Option A — matriz versionada + correção da deriva + revalidação do bundle.
- **In scope**: matriz B1–B11; correção de `compost-growth-hardening.scenario.json`; reexecução do Gate 3 afetado; notas datadas em STATE/docs; condução de PRs #811/#812 conforme gates ao vivo.
- **Out of scope**: código de jogo, D1, atestação E, SC-004, publicação, segredos, merge com check obrigatório vermelho, importação de gates da factory.
- **Success metrics**: 11/11 grupos 0 falhas; gates verdes no fechamento; `"max": 31` e bundle `verdict=pass` com 3 sinais; 0 `verified` promovido.
- **Carried-forward open questions**: persistência da matriz (resolvida: artefato `verification-matrix.md`); escopo de reexecução Gate 3 (resolvido: mínimo obrigatório + primários); precedência merge/check (resolvida: autorização cobre o ato, não o bypass).
