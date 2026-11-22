# Concept: Roteiro de verificação funcional

- **Slug**: roteiro-verificacao-funcional
- **Created**: 2026-11-21
- **Recommended option**: Option A

## Options

### Option A — Matriz versionada + correção da deriva

- **Sketch**: persistir a matriz feature→comando→resultado no artefato do ciclo 030, executar gates e testes por feature, corrigir o threshold obsoleto do cenário de compostagem e revalidar o bundle Gate 3 correspondente.
- **Appetite**: small
- **Trade-offs**: produz artefato reutilizável e evidência executada; custa execução de Gate 3 e exige disciplina de atualização futura.
- **Rabbit holes**: reexecução dos 20 cenários; rejeição do cenário editado pelo validador de schema.

### Option B — Relatório efêmero na sessão

- **Sketch**: responder ao pedido apenas com a saída dos comandos, sem artefato versionado nem correção.
- **Appetite**: small
- **Trade-offs**: menor custo imediato; nada fica auditável após a sessão e a deriva permanece.
- **Rabbit holes**: nenhum relevante.

### Option C — Cobertura total incluindo humano

- **Sketch**: tentar também D1, atestação E e SC-004 para "todas" as formas de verificação.
- **Appetite**: large
- **Trade-offs**: cobriria o gap humano; exige participantes 18+ com consentimento, segundo jogador e autorização — não executável por agente.
- **Rabbit holes**: simulação de atestação, falsa promoção a `verified`, violação da constituição v2.0.0.

## Recommendation

Option A. Ataca diretamente o pedido, usa apenas evidência local determinística, corrige um defeito concreto e não toca em código de jogo nem em atestação humana. Option B falha o objetivo de auditabilidade; Option C é human-gated por definição.

## Out of Scope (for the recommended option)

- Código de jogo (`LivingRoots/**`), D1, atestação E, SC-004, tag/release pública, segredos do Dependabot.
- Reexecução dos 20 cenários: executar os primários já registrados como `machine-pass` apenas se necessário; o cenário alterado é obrigatório.

## Assumptions to Validate

- O runner aceita a edição de `"max": 34` → `31` sem mudança de schema.
- O gate autoritativo permanece verde no fechamento do ciclo.
- A autorização explícita do prompt cobre o ato de mesclar, mas não permite bypass de check obrigatório vermelho.
