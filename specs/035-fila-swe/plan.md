# Implementation Plan: Fila SWE

**Branch**: `feature/035-fila-swe` | **Date**: 2026-11-22

## Technical Context

- Stack: Markdown/YAML; zero código.
- Âncora: `dev` @ `4b92cd8`; FONTE_FACTORY `b1deab1` (read-only).
- Gate: `scripts/ci/aggregate-gate.sh` + `scripts/release-gate.sh`; CI GitHub/`gh`.
- Touch points: `escalations.yaml`, `STATE.md`, `PROGRESSO.md`, artefatos 035.
- Excluded: main, tags, código, workflows, scripts, cenários, contratos.

## Constitution Check

I–VI: PASS (documentação; nenhum estado compartilhado tocado).

## Rollback

Reverter os commits do PR restaura a fila e as âncoras.
