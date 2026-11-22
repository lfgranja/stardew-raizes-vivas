# Feature Specification: Remover transições duplicadas de promoção

**Feature Branch**: `feature/037-dedupe-transitions`
**Created**: 2026-11-22

## Clarifications

- RED确认 por `test-spec-consistency.sh` no HEAD `5d0cb14`; não é interrupção de harness.
- Primeiro conjunto de transições (04:14:30–31) é preservado.

## User Scenarios & Testing

### US1 — Registro de promoção sem duplicatas (P1)

Como revisor, quero `promotion-triggers.json` sem pares `(component, runId)` repetidos, para que o gate determinístico fique verde.

**Independent Test**: `bash scripts/tests/test-spec-consistency.sh` → exit 0.

**Acceptance Scenarios**:

1. 0 duplicatas em `.transitions` (baseline 3).
2. Transições de outros runs permanecem intactas e ordenadas.
3. Nenhum bloco `GATE3:BEGIN/END` é editado à mão.

## Requirements

- **FR-001**: remover exatamente os índices 275–277 (0-based) de `.transitions`.
- **FR-002**: manter os 275 primeiros elementos e a ordem relativa.
- **FR-003**: nenhum código/workflow/cenário/contrato/bloco GATE3 alterado.
- **FR-004**: rodar `test-spec-consistency.sh` e o gate autoritativo do alvo.
- **FR-005**: cadeia SDD completa com artefatos persistidos.

## Success Criteria

- SC-001: 0 duplicatas (baseline 3).
- SC-002: `test-spec-consistency.sh` exit 0.
- SC-003: `aggregate-gate.sh` ALL STEPS PASSED; pytest/i18n verdes.
- SC-004: diff sem código/workflows/scripts/cenários/contratos.

## Assumptions

- `jq` preserva a ordem dos elementos restantes.
