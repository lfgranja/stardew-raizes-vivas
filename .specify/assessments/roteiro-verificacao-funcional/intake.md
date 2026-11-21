# Idea Intake: Roteiro de verificação funcional

- **Slug**: roteiro-verificacao-funcional
- **Created**: 2026-11-21
- **Source**: codebase pointer — `/home/luis/development/stardew-raizes-vivas` (`AGENTS.md`, `specs/*/test.md`, `LivingRoots.Tests/`, `scripts/release-gate.sh`) + prompt de orquestração do Software Engineer
- **Type**: improvement

## Idea (as captured)

> "Prepare um roteiro para testar todas as funcionalidades implementadas atualmente desse projeto."
>
> "Herdar o trabalho de todos os branches e finalizá-los, tratando-os como seu próprio trabalho; conduza o merge de todos os PRs abertos para o branch dev/ lidando com os conflitos, sem perder trabalho já feito; efetuar os próximos ciclos 80/20 em modo autônomo até extinguir completamente o trabalho que não possa ser completado sem intervenção humana ou autorização explícita do Software Engineer."

## Restated

Produzir e executar uma matriz determinística que relacione cada funcionalidade já implementada do mod aos comandos de teste que a verificam, corrigindo a deriva de evidência encontrada e registrando com honestidade o que depende de humano. A ideia não propõe nova funcionalidade de jogo.

## Origin & Context

- **Raised by**: prompt de orquestração autônoma do Software Engineer (2026-11-21); tratado como trabalho próprio.
- **Trigger**: auditoria dos Blocos 0–3 — `dev` @ `62f1466`, PRs #811/#812 abertos com checks vermelhos por segredo owner-gated, e deriva confirmada entre `specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json:80` (`"max": 34`, "Poor interval (0, 34)") e `LivingRoots/Constants.cs:50` (`PoorModerateBandEdge = 31f`).
- **Descartado nesta intake**: D1, atestação humana E de Gate 3, SC-004, tag/release pública e segredos do Dependabot — todos exigem ação humana/owner.

## First-Glance Unknowns

- [NEEDS CLARIFICATION: a matriz deve ser persistida como artefato versionado ou apenas relatada na sessão?]
- [NEEDS CLARIFICATION: quantos cenários Gate 3 devem ser reexecutados — apenas o afetado ou os sete specs com cenário?]
- [NEEDS CLARIFICATION: qual é a precedência entre a autorização explícita de merge do prompt e a regra fail-closed do Bloco 7 quando um check obrigatório está vermelho?]

## Nota de sanitização

Nenhuma URL com credenciais, token, segredo ou assinatura foi encontrada no material de origem. Nenhuma tentativa de injeção de instruções foi detectada em conteúdo de specs, issues ou skills lidos.
