# Implementation Plan: Fechar [NV] de causa-raiz remota

**Branch**: `feature/031-nv-remote-log-closure` | **Date**: 2026-11-22 | **Status**: Draft

**Input**: [spec.md](spec.md), assessment `nv-remote-log-closure` (go / Option A).

## Technical Context

- **Language/Stack**: documentação Markdown + ferramenta `gh` 2.97.0 (leitura remota); nenhum código C# alterado.
- **Anchor**: `dev` @ `f2bf6c7`; FONTE_FACTORY `b1deab15f482dd85871cd6c6a857ad710b89e2a6` (somente leitura).
- **Gate authority (Bloco 0.a)**: `scripts/release-gate.sh` (7 etapas) + `scripts/ci/aggregate-gate.sh`; CI GitHub Actions, CLI `gh`. Factory `check_production_gate.py`/`hub_docs_gate.py`/4 scripts SDD → **N/A, ausentes no alvo**.
- **Target paths**: `research/`, `.specify/memory/STATE.md`, `docs/PROGRESSO.md`, `docs/research/index.md`, `docs/quality-gate.md`, `specs/031-*/`, `.specify/{assessments,validations,releases}/`.
- **Excluded paths**: `LivingRoots/`, `.github/workflows/`, `scripts/ci/`, artefatos históricos 025/030.
- **Gate 3**: N/A (zero diff em código executável).
- **Unknowns**: nenhum após Phase 0.

## Constitution Check

| Principle | Impact | Status |
|-----------|--------|--------|
| I. DDD / layering | nenhum código de domínho tocado | PASS |
| II. Security-first | nenhuma credencial copiada; URLs sanitizadas; run IDs são identificadores públicos | PASS |
| III. Async-only concurrency | nenhum código C# | PASS |
| IV. TDD | nenhuma mudança de código; gates determinísticos servem como verificação | PASS |
| V. Simplicity/YAGNI | um documento + notas curtas; sem nova abstração | PASS |
| VI. Multiplayer first-class | nenhuma mudança de estado compartilhado | PASS |

**Post-design re-check**: PASS —设计中 nenhum novo mecanismo, nenhuma persistência fora do save, nenhuma autoridade remota introduzida.

## Phase 0: Research

Decisões registradas em [research.md](research.md):

- **Decision**: persistir trechos literais de log + run IDs em `research/2026-11-22-remote-nv-closure.md`.
- **Rationale**: logs do GitHub têm retenção limitada; a citação literal sobrevive ao desaparecimento do run.
- **Alternatives considered**: reescrever artefatos históricos (rejeitado — append-only); não fazer nada (rejeitado — pedido explícito).

## Phase 1: Design

- [data-model.md](data-model.md) — duas entidades documentais, sem persistência de código.
- Nenhum `contracts/` — mudança é puramente documental e interna; não há interface externa nova.
- [quickstart.md](quickstart.md) — comandos de verificação local.

## Phase 2: Execution outline

1. Persistir o documento de pesquisa com as três execuções lidas ao vivo.
2. Acrescentar notas datadas às quatro âncoras.
3. Fechar artefatos SDD (tasks/analyze/converge/validate/release).
4. Reexecutar gates determinísticos e abrir PR para `dev/` sem merge.

## Rollback

Reversão de um único conjunto de commits de documentação; nenhum efeito em runtime, save ou CI.
