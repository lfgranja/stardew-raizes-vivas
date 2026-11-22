# Idea Research: Remover transições duplicadas de promoção

- **Slug**: promotion-dedupe
- **Created**: 2026-11-22T05:52Z
- **Evidence confidence (overall)**: high

## Data & Constraints

- `bash scripts/tests/test-spec-consistency.sh` no HEAD `5d0cb14` → `spec-consistency: FAIL (3 of 1698 checks)`: `transitions[275]` (`test.md`), `[276]` (`ROADMAP.md`), `[277]` (`COMPLETION_SUMMARY.md`), todas com `runId=run-20261122T041058Z-73a0`.
- `promotion-triggers.json` tem 278 transições; pares duplicados com `recordedAt` 04:14:30–31 e 04:15:18–19.
- Causa: promoção manual via `promotion_apply` com shim de `compgen -G` reaplicou um run que o runner já havia auto-promovido.
- Correção: remover exatamente os índices 275–277 (0-based), mantendo o primeiro conjunto (04:14:30–31).
- `test-spec-consistency.sh` só verifica unicidade de `(component, runId)`; qualquer conjunto remanescente satisfaz o contrato.

## Evidence Against

- Remover transições é uma mutação de histórico; mitigação: são duplicatas exatas criadas nesta sessão, não trabalho alheio, e o primeiro conjunto é preservado.

## Sources

- Log local `/home/luis/lr-033-release-gate.log`; `scripts/tests/test-spec-consistency.sh`; JSON no HEAD.
