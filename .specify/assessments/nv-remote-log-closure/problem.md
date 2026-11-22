# Problem Definition: Fechar [NV] de causa-raiz remota dos PRs #790–#796

- **Slug**: nv-remote-log-closure
- **Created**: 2026-11-22T03:34:24Z
- **Inputs used**: intake.md | research.md | user input

## Problem Statement

O estado registrado do projeto ainda marca como `[NV]` a causa-raiz step-level dos checks vermelhos dos PRs de dependências #790–#796, embora os logs do GitHub Actions estejam disponíveis e já tenham sido lidos nesta sessão; leitores do STATE.md não conseguem distinguir, com base no artefato, falha de provisão de segredo de regressão de código.

## Affected Users & Stakeholders

- **Users**: maintainer e agentes de orquestração que consultam `STATE.md` e `docs/quality-gate.md` para decidir o próximo passo.
- **Stakeholders**: maintainer (dono das decisões de CI/segredos), revisores futuros da trilha SDD.

## Goals

- Substituir a afirmação `[NV]` sobre a causa step-level por evidência datada, com comandos, run IDs e trechos de log.
- Preservar a regra de que todo status remoto/CI/proteção não observado ao vivo permanece `[NV]`.
- Manter os demais `[NV]` humano/owner-gated explicitly marcados e não promovidos.

## Non-Goals

- Alterar código de jogo, workflow, segredos, branch protection, tags ou releases.
- Reescrever artefatos históricos de assessments/validations/releases já mesclados.
- Reabrir PRs #790–#796 ou reexecutar CI deles.
- Tentar resolver D1, atestação humana E de Gate 3, SC-004, `RUNNER_AUDIT_STORE` ou aprovação de revisor distinto.

## Success Metrics

- Nenhuma ocorrência remanescente da frase "step-level root cause ... [NV]" em `STATE.md` e `docs/quality-gate.md` sem a evidência de log correspondente (baseline: 2 ocorrências).
- Pelo menos 3 run IDs com trechos de log citados no artefato persistido (baseline: 0).
- `aggregate-gate.sh` e `release-gate.sh` permanecem verdes após a mudança (baseline: verdes no HEAD de referência).

## Cost of Inaction

A dívida informativa permanece: cada novo ciclo re-registra o mesmo `[NV]`, e a decisão sobre CI depende de re-leitura repetida de logs em vez de evidência persistida.

## Open Questions

- [NEEDS CLARIFICATION: os arquivos históricos devem receber notas de correção?] — resolvido: não; append-only em artefatos novos e âncoras de estado, sem reescrita histórica.
