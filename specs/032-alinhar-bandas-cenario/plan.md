# Implementation Plan: Alinhar bandas 31/71 nos cenários ativos

**Branch**: `feature/032-alinhar-bandas-cenario` | **Date**: 2026-11-22 | **Status**: Draft

## Technical Context

- **Stack**: JSON de cenário + runner SMAPI headless; nenhum código C# alterado.
- **Âncora**: `dev` @ `f2bf6c7`; FONTE_FACTORY `b1deab15...` (somente leitura). NNN=032 forçado via `--number 032` porque o ciclo 031 está em voo em outro worktree (colisão de auto-resolução registrada).
- **Gate authority**: `scripts/release-gate.sh` + `scripts/ci/aggregate-gate.sh`; Gate 3 via `scripts/verify-scenario.sh`.
- **Touch points**: 2 cenários (4 linhas), 2 `test.md` (notas append-only via runner), `STATE.md`/`docs/PROGRESSO.md`, artefatos SDD.
- **Excluded**: `LivingRoots/`, `.github/workflows/`, `scripts/ci/`, `specs/010-*`, fixtures `boundary-*`.
- **Gate 3**: obrigatório para os 2 cenários alterados (machine-pass host-only).

## Constitution Check

| Principle | Impact | Status |
|-----------|--------|--------|
| I. DDD | nenhum domínio tocado | PASS |
| II. Security | nenhuma credencial; --no-acquire | PASS |
| III. Async | nenhum código | PASS |
| IV. TDD | runner é a verificação | PASS |
| V. YAGNI | 4 linhas | PASS |
| VI. Multiplayer | nenhum estado compartilhado alterado | PASS |

## Phase 0 — Research

Ver [research.md](research.md): correção cirúrgica; sem renomeação; 80 continua aprovando.

## Phase 1 — Design

- [data-model.md](data-model.md) — entidades documentais.
- Sem `contracts/`: nenhuma interface externa nova; o contrato é o próprio JSON de cenário.
- [quickstart.md](quickstart.md) — comandos de verificação.

## Rollback

Reverter os commits do ciclo e reexecutar Gate 3 com os thresholds antigos; nenhum efeito em save/schema/código.
