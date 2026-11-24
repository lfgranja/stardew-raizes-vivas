# Problem Definition: Fechar dívida de artefatos e evidência

- **Slug**: artifact-evidence-closure
- **Created**: 2026-11-20
- **Inputs used**: intake.md yes | research.md yes | user input yes

## Problem Statement

O repositório acumula artefatos de arquitetura e evidência que não passam nas próprias verificações determinísticas (2 de 5 candidatos archify inválidos) e carece de âncoras documentais mínimas, de modo que um leitor ou agente não consegue distinguir estado verificado de afirmação remota não confirmada. Isso importa agora porque os demais candidatos de maior impacto estão bloqueados por owner/humano, e a dívida cosmética é a única fila executável sem autorização adicional.

## Affected Users & Stakeholders

- **Users**: mantenedor e leitores do repositório — precisam de diagramas regeneráveis e de um registro de estado confiável.
- **Stakeholders**: Software Engineer (decide o que é owner-gated); revisores de PR (precisam de evidência verificável, não de afirmações).

## Goals

- Tornar os 5 candidatos archify válidos sob `archify validate --json`, sem perder o conteúdo existente.
- Manter âncoras documentais (`docs/PROGRESSO.md`, `docs/research/index.md`) com fatos locais verificados e `[NV]` explícito em afirmações remotas.
- Registrar a revisão de qualidade de requisitos de resíduos de specs **sem** afirmar implementação nova nem promover `machine-pass` a `verified`.

## Non-Goals

- Não alterar código de jogo (`LivingRoots/**`) nem executar Gate 3 de jogo real.
- Não executar SC-004 (exige participantes humanos) nem simular atestado E ou D1.
- Não mesclar PRs com checks vermelhos, não copiar segredos, não reescrever histórico.

## Success Metrics

- `archify validate` nos 5 candidatos: 5/5 `ok:true` (baseline: 3/5 em 2026-11-20).
- Âncoras documentais existentes e com nota datada de estado verificado (baseline: 2 ausentes antes desta sessão).
- `bash scripts/ci/aggregate-gate.sh` → ALL STEPS PASSED (baseline: já verde; regressão = falha).
- Nenhuma alteração em `LivingRoots/**` e nenhum `verified` humano reivindicado (baseline: 0).

## Cost of Inaction

A dívida permanece registrada em `STATE.md` indefinidamente; diagramas não podem ser regenerados de forma confiável e leitores continuam sem distinção clara entre evidência local e `[NV]`. Nenhum bloqueio de merge é removido — o custo é decisório/documental, não de gameplay.

## Open Questions

- [NEEDS CLARIFICATION: converter o candidato de schema antigo ou substituí-lo por um novo candidate.json validado?]
- [NEEDS CLARIFICATION: quais resíduos de checkboxes possuem evidência auditável suficiente nesta sessão?]

