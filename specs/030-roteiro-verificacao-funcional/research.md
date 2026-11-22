# Research: Roteiro de verificação funcional

- **Feature**: 030-roteiro-verificacao-funcional
- **Date**: 2026-11-21

## Decision: reutilizar gates e artefatos existentes

Usar `scripts/release-gate.sh` (7 etapas), `scripts/ci/aggregate-gate.sh` (4 etapas), `i18n-parity-check.py`, pytest de quality-gate e os cenários versionados em `specs/*/scenarios/`. Nenhum runner novo.

**Rationale**: São os gates autoritativos do alvo, já executados em ciclos 026–029; recriá-los introduziria uma segunda fonte de verdade.

**Alternatives considered**: importar os quatro scripts SDD e `check_production_gate.py` da factory — rejeitado (Bloco 0.a: divergência de CI/CLI/gate; ausentes no alvo; baselines da factory produzem RED falso). Criar runner próprio — rejeitado (YAGNI).

## Decision: matriz persistida como `verification-matrix.md`

**Rationale**: responde ao pedido de forma auditável e reutilizável; `docs/` não ganha nova convenção.

**Alternatives considered**: relatório apenas na sessão — rejeitado (não auditável); novo `docs/verification/` — rejeitado (convenção duplicada).

## Decision: corrigir apenas a deriva confirmada

`compost-growth-hardening.scenario.json:80` → `"max": 31`, `exclusiveMin: false`, `exclusiveMax: true`, descrição "Poor interval [0, 31)". Os limites legados de `compost-growth-healthy` (`67`) e `compost-growth-moderate` (`exclusiveMax: false`) são listados, não alterados.

**Rationale**: evidência literal lida; as demais exigiriam reexecução de bundles adicionais sem pedido explícito.

**Alternatives considered**: corrigir os três de uma vez — rejeitado neste ciclo por escopo e risco de invalidar três bundles.

## Decision: precedência de merge

Autorização explícita do prompt cobre o ato de mesclar; check obrigatório vermelho bloqueia. `gh` é a CLI do alvo (GitHub), não `fj` (Forgejo/factory).

**Rationale**: docs/quality-gate.md e constituição definem o gate do alvo; nenhuma aprovação é simulada.
