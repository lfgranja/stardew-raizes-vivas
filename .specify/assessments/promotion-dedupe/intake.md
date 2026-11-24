# Idea Intake: Remover transições duplicadas de promoção

- **Slug**: promotion-dedupe
- **Created**: 2026-11-22T05:52Z
- **Source**: log do release-gate 033 (`bg_21`) + leitura do JSON no HEAD
- **Type**: fix

## Idea (as captured)

> `[release-gate FAIL] step 5 failed: launch-free smoke suite` — `test-spec-consistency.sh`: 3 duplicatas de component/runId.

## Restated

Remover as três entradas duplicadas de `promotion-triggers.json` para o run `run-20261122T041058Z-73a0`, preservando o primeiro conjunto e a trilha append-only.

## Origin & Context

- **Raised by**: gate determinístico do alvo (RED real, não interrupção).
- **Trigger**: `bg_21` terminou `exit 15` com `[release-gate FAIL]`; 51 arquivos, 49 passed, 1 failed.
