# Implementation Plan: CI green refresh

**Branch**: `feature/036-ci-green-refresh` | **Date**: 2026-11-22

## Technical Context

- Markdown/YAML; zero código.
- Âncora: `dev` @ `b3eaa94`; FONTE_FACTORY `b1deab1` (read-only).
- Gate: `aggregate-gate.sh` + `release-gate.sh`; GitHub/`gh`.
- Touch: `escalations.yaml`, `STATE.md`, `PROGRESSO.md`, artefatos 036.
- Excluded: main, tags, código, workflows, scripts, cenários, contratos.

## Constitution Check

I–VI: PASS (documentação).

## Rollback

Reverter commits do PR.
