# Concept: Alinhar bandas 31/71 nos cenários ativos 004 e 005

- **Slug**: band-scale-scenario-drift
- **Created**: 2026-11-22T03:49:24Z
- **Recommended option**: Option A

## Options

### Option A — Correção cirúrgica dos dois cenários ativos

- **Sketch**: trocar `min: 67 → 71` e a descrição para `[71, 100)` em `compost-growth-healthy` e `soil-health-growth`; reexecutar Gate 3 para ambos; registrar notas append-only.
- **Appetite**: small
- **Trade-offs**: elimina a deriva nos contratos executáveis ativos com risco mínimo (valor observado 80 continua aprovando); deixa os fixtures 010 como dívida contratual histórica, explicitamente fora da guarda.
- **Rabbit holes**: editar manualmente blocos `GATE3:BEGIN/END`; renomear fixtures; mexer em `exclusiveMax`.

### Option B — Corrigir todos os schemas/fixtures 010

- **Sketch**: atualizar exemplos e nomes de fixtures para 31/71.
- **Appetite**: medium
- **Trade-offs**: aparência de consistência total, mas reescreve histórico contratual, quebra rastreabilidade de README/tasks e não muda nenhum veredito.
- **Rabbit holes**: renomeação em cascata, CI best-effort directory run, perda de proveniência.

### Option C — Manter dívida

- **Sketch**: acrescentar outra nota de dívida.
- **Appetite**: small
- **Trade-offs**: zero risco, mas nenhum valor entregue e o pedido de extinguir trabalho autônomo fica incompleto.
- **Rabbit holes**: nenhum.

## Recommendation

**Option A**. É a única que remove a deriva real com evidência determinística e sem reescrita histórica.

## Out of Scope (for the recommended option)

- Fixtures/schemas 010, código de jogo, workflows, segredos, D1, atestação humana E, SC-004, tag/release, aprovador distinto.

## Assumptions to Validate

- Os dois runs Gate 3 terminam `pass` com 3 sinais (a validar no implement); se `partial`/W, registrar sem promover.
- O runner atualiza os blocos `GATE3:BEGIN/END` (validado em ciclos anteriores).
