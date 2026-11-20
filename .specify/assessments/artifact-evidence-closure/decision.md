# Decision: Fechar dívida de artefatos e evidência

- **Slug**: artifact-evidence-closure
- **Decided**: 2026-11-20
- **Verdict**: go
- **Artifacts reviewed**: intake.md yes | research.md yes | problem.md yes | concept.md yes

## Scorecard

| Criterion | Rating | Justification |
|---|---|---|
| Problem validity | adequate | Falhas reproduzidas por `archify validate` (2/5) e ausência de âncoras documentais constatada por `ls`. |
| Evidence strength | adequate | Toda a evidência é local e executada nesta sessão; nenhuma afirmação remota é usada como fundamento. |
| Value vs. inaction | adequate | Fecha dívida registrada desde o ciclo 026 e melhora a distinguishabilidade entre evidência local e `[NV]`; não desbloqueia PRs. |
| Feasibility / appetite | adequate | Option A é small; conversão de schema do candidato antigo é o único ponto incerto. |
| Strategic fit | strong | Respeita a constituição v2.0.0 e não toca em código de jogo, atestação humana ou Gate 3. |
| Risk posture | adequate | Riscos identificados (perda de layout, escopo cosmético) e limitados por non-goals explícitos. |

## Verdict & Rationale

**go**. O problema é real e verificável, a evidência é local e executada, e a Option A tem appetite small com risco contido. É o único candidato da fila 80/20 que não depende de owner/humano: SC-004 exige participantes 18+ com consentimento, PRs #811/#812 exigem segredo do Dependabot e os demais itens são owner-gated. Nenhuma afirmação `verified` é promovida.

## If go — Handoff to `/speckit-specify`

- **Problem**: artefatos de arquitetura falham as próprias verificações e faltam âncoras documentais confiáveis.
- **Chosen approach**: Option A — validar e reconciliar artefatos existentes.
- **In scope**: correção dos 2 candidatos `.archify/` inválidos; manutenção das âncoras `docs/PROGRESSO.md`/`docs/research/index.md`; registro de revisão de qualidade de requisitos com evidência; notas datadas em `STATE.md`.
- **Out of scope**: código de jogo, Gate 3 real, SC-004, D1, atestação E, publicação, segredos, aprovação de revisor, merge de PR com check vermelho, HTMLs em `docs/architecture/`.
- **Success metrics**: 5/5 candidatos `ok:true`; âncoras presentes; `aggregate-gate.sh` ALL STEPS PASSED; zero diff em `LivingRoots/**`; nenhum `verified` humano reivindicado.
- **Carried-forward open questions**: conversão vs substituição do `candidate.json` de `workflow-verification-pipeline`; quais resíduos de checkboxes possuem evidência auditável suficiente.

