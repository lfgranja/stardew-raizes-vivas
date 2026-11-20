# Clarification Log — Spec 028

**Feature**: `specs/028-quality-gate-pytest-wiring/spec.md`
**Date**: 2026-11-20T01:35Z

## R3 — Clarification questions

Nenhuma pergunta R3 aberta. As incertezas iniciais (Gate 3 para mudança comment-only; escolha do workflow para pytest) foram resolvidas com evidência local em `.specify/assessments/quality-gate-pytest-wiring/research.md`:

1. **Gate 3**: constituição v2.0.0 exige machine-pass headless para qualquer mudança em runnable code; `verify-scenario.sh` automatiza isso. `verified` E permanece owner-gated.
2. **Workflow**: job `static` de `quality-gate.yml` é game-free, já tem Python 3.12 e `PYTHONPATH`; o job `gate` agrega fail-closed. `sdd-gate`/`aggregate-gate` não recebem toolchain novo.

Nenhuma pergunta ao maintainer foi necessária; nenhuma resposta assumida além das evidências citadas.
