# Idea Intake: Promoção dev→main CI-green na fila SWE

- **Slug**: swe-escalation-queue
- **Created**: 2026-11-22T05:32:37Z
- **Source**: prompt de orquestração (diretiva de babysitting dev→main) + `gh` ao vivo
- **Type**: compliance

## Idea (as captured)

> "prosseguindo com o babysitting do PR para o branch main/ até o CI/CD ficar GREEN, e então, adicionar na fila de escalar para SWE e prosseguir com o próximo ciclo 80/20."

## Restated

Registrar na fila de escalonamentos existente que o PR #821 (`dev`→`main`) está com CI integralmente verde, mas o merge depende de aprovador distinto (SWE), e que tag/release pública permanece owner-gated.

## Origin & Context

- **Raised by**: Software Engineer, via prompt de 2026-11-22.
- **Trigger**: `gh pr checks 821 --watch` terminou `RC=0` às 05:31:56Z; `reviewDecision=REVIEW_REQUIRED`.

## First-Glance Unknowns

- Nenhuma bloqueante.
