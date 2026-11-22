# Phase 0 Research: 032-alinhar-bandas-cenario

- **Date**: 2026-11-22T03:50Z

## Decision 1 — Correção cirúrgica

- **Decision**: alterar apenas `min: 67 → 71` e a descrição para `[71, 100)` nos dois cenários ativos.
- **Rationale**: 4 linhas; valor observado 80 continua aprovando; risco mínimo.
- **Alternatives**: schemas/fixtures 010 (rejeitado — histórico contratual); nenhuma ação (rejeitado — dívida ativa).

## Decision 2 — Sem renomeação de fixtures

- **Decision**: manter `boundary-33-poor`, `boundary-67-healthy` etc. intactos.
- **Rationale**: consumidores usam glob; nomes são rastreabilidade histórica (`fixtures/README.md:12-16`).
- **Alternatives**: renomear (rejeitado — quebra proveniência sem ganho de comportamento).

## Decision 3 — `exclusiveMax` inalterado

- **Decision**: manter `max: 100`, `exclusiveMax: true`.
- **Rationale**: nenhuma leitura real de exatamente 100 evidenciada; contrato separado.
- **Alternatives**: `false` (rejeitado — especulação).

## Decision 4 — Gate 3 obrigatório, honestidade R

- **Decision**: reexecutar ambos com `--no-acquire --game-path ... --skip-build`; aceitar apenas `pass` com 3 sinais; `partial`/W é registrado, não promovido.
- **Rationale**: constituição Gate 3 + protocolo de verificação; blocos são promovidos pelo runner.
- **Alternatives**: reutilizar bundles antigos (rejeitado — não cobrem o novo predicado).
