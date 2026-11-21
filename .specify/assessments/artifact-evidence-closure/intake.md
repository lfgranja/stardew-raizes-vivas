# Idea Intake: Fechar dívida de artefatos e evidência

- **Slug**: artifact-evidence-closure
- **Created**: 2026-11-20
- **Source**: codebase pointer — `/home/luis/development/stardew-raizes-vivas` (`.specify/memory/STATE.md`, `.archify/*/`, `specs/*/tasks.md`)
- **Type**: improvement

## Idea (as captured)

> "Efetuar o necessário para resolver todos os [NV] o mais autonomamente possível" e, na fila 80/20, fechar a dívida de artefatos/evidência: dois candidatos `.archify/` inválidos (`workflow-heirloom-pipeline`, `workflow-verification-pipeline`), resíduos documentais/checkboxes e a ausência de âncoras `docs/PROGRESSO.md` e `docs/research/index.md`, sem tocar em código de jogo e sem reescrever histórico.

## Restated

Propor um único ciclo SDD para reconciliar artefatos de arquitetura e evidência que já têm fonte verificada no repositório, eliminando dívidas cosméticas/documentais sem alterar comportamento do mod nem simular atestação humana.

## Origin & Context

- **Raised by**: prompt de orquestração autônoma do Software Engineer (2026-11-20); tratado como trabalho próprio.
- **Trigger**: auditoria Bloco 2/3 de 2026-11-20 — `validate` dos 5 candidatos archify: 3 ok, 2 inválidos; `docs/architecture/` e as duas âncoras de docs ausentes; `STATE.md` registra a mesma dívida desde o ciclo 026.
- **Descartado nesta intake**: SC-004 exige participantes humanos 18+ com consentimento e first-use real (protocolo lido), portanto não é executável por agente; PRs #811/#812 dependem de segredo owner-gated.

## First-Glance Unknowns

- [NEEDS CLARIFICATION: os candidatos `.archify/` são versionados ou apenas gerados localmente?]
- [NEEDS CLARIFICATION: quais checkboxes de specs antigas possuem evidência suficiente para revisão de qualidade sem afirmar implementação nova?]
- [NEEDS CLARIFICATION: a correção dos candidatos exige mudança de schema com risco de perder work-in-flight?]

