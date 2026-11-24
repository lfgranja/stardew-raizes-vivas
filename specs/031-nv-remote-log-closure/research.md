# Phase 0 Research: 031-nv-remote-log-closure

- **Date**: 2026-11-22T03:36Z
- **Resolved unknowns**: nenhum remanescente.

## Decision 1 — Persistir citação literal em vez de link

- **Decision**: registrar run IDs, step names, timestamp e trecho exato de `##[error]` no documento de pesquisa versionado.
- **Rationale**: `gh run view ... --log-failed` depende de retenção remota; o artefato versionado é a única evidência que sobrevive a expiração.
- **Alternatives considered**: apenas link para o run (insuficiente — quebra); re-leitura em cada sessão (custoso e não auditável).

## Decision 2 — Append-only nas âncoras

- **Decision**: novas notas datadas no fim das seções existentes de `STATE.md`/`docs/PROGRESSO.md`/`docs/research/index.md`/`docs/quality-gate.md`.
- **Rationale**: o histórico de 025 e 030 registra honestamente o que era conhecido na data; reescrevê-lo falsificaria a trilha.
- **Alternatives considered**: editar os arquivos históricos (rejeitado).

## Decision 3 — Gate 3 N/A

- **Decision**: não executar `verify-scenario.sh` neste ciclo.
- **Rationale**: zero alteração em `LivingRoots/`; a constituição exige Gate 3 para mudanças em código executável, não para documentação.
- **Alternatives considered**: reexecutar por hábito (desperdício de tempo de game headless).

## Decision 4 — CLI e gate do alvo

- **Decision**: `gh` para leitura remota; `scripts/release-gate.sh` + `scripts/ci/aggregate-gate.sh` como gates autoritativos.
- **Rationale**: Bloco 0.a — factory é Forgejo/`fj`/`check_production_gate.py`; alvo é GitHub/`gh`/release-gate.
- **Alternatives considered**: importar gate da factory (rejeitado — decisão de governança do alvo).
