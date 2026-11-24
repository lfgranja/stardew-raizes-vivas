# Implementation Plan: Dedupe de transições

**Branch**: `feature/037-dedupe-transitions` | **Date**: 2026-11-22

## Technical Context

- JSON estruturado (minified) + gates determinísticos; zero código C#.
- Âncora: `dev` @ `5d0cb14`; FONTE_FACTORY `b1deab1` (read-only).
- Gate: `test-spec-consistency.sh`, `aggregate-gate.sh`, `release-gate.sh`; GitHub/`gh`.
- Touch: `specs/010-*/promotion-triggers.json`, artefatos SDD.
- Excluded: código, workflows, scripts, cenários, contratos, blocos GATE3.

## Constitution Check

I–VI: PASS (documentação de registro, sem código/estado compartilhado).

## Rollback

Reverter o commit restaura as três transições duplicadas (e o RED).
