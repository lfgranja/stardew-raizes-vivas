# Problem Definition: Roteiro de verificação funcional

- **Slug**: roteiro-verificacao-funcional
- **Created**: 2026-11-21
- **Inputs used**: intake.md yes | research.md yes | user input yes

## Problem Statement

Quem mantém ou revisa o Living Roots não dispõe de uma única fonte que relacione cada funcionalidade implementada ao comando determinístico que a verifica; além disso, pelo menos um cenário de evidência end-to-end ainda usa limites de faixa obsoletos, de modo que a evidência pode parecer correta enquanto descreve regra superada. Isso dificulta responder "o que está realmente testado" sem reconstruir a resposta de dezenas de arquivos.

## Affected Users & Stakeholders

- **Users**: mantenedor e revisor de PR — precisam de uma matriz auditável para revisar rapidamente.
- **Stakeholders**: Software Engineer (autoriza merges e define o que é human-gated), jogadores em co-op (afetados por dívidas D1, mas não por este artefato).

## Goals

- Cobrir todas as features implementadas com comando, resultado e limite de honestidade explícitos.
- Eliminar a deriva identificada entre o cenário Gate 3 e as constantes de domínio.
- Registrar o que é executável autonomamente e o que permanece `[NV]`.

## Non-Goals

- Não implementar nova funcionalidade de jogo nem alterar `LivingRoots/**`.
- Não simular D1, atestação humana E de Gate 3, SC-004 ou aprovação de revisor.
- Não importar gates da factory nem reescrever histórico.

## Success Metrics

- 11/11 grupos da matriz xUnit com 0 falhas (baseline: não agregados).
- Gates autoritativos `ALL STEPS PASSED` no início e no fechamento (baseline: aggregate PASS; release em execução).
- 1 deriva de banda corrigida e 1 bundle Gate 3 revalidado com 3 sinais (baseline: `"max": 34`).
- 0 afirmações `verified`/human-promovidas (baseline: 0).

## Cost of Inaction

A pergunta do pedido continua sem resposta auditável; a deriva 34→31 permanece em evidência versionada; revisores repetem a reconstrução manual a cada ciclo.

## Open Questions

- [NEEDS CLARIFICATION: a matriz deve ser persistida como artefato versionado ou apenas relatada na sessão?]
- [NEEDS CLARIFICATION: quantos cenários Gate 3 devem ser reexecutados — apenas o afetado ou os sete specs com cenário?]
- [NEEDS CLARIFICATION: qual é a precedência entre autorização explícita de merge e checks vermelhos?]
