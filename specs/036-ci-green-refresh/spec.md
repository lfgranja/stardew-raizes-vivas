# Feature Specification: Atualizar CI green final do PR 821

**Feature Branch**: `feature/036-ci-green-refresh`
**Created**: 2026-11-22

## Clarifications

- Mantido o registro do green anterior (05:31:56Z); novo green é atualização datada do mesmo item.
- Merge em `main` segue owner-gated.

## User Scenarios & Testing

### US1 — Item de escalar atualizado (P1)

Como SWE, quero ver no item `ESC-LR-20261122-008` o HEAD e o timestamp do CI green mais recente, para decidir com evidência atual.

**Independent Test**: ler `escalations.yaml` e encontrar `atualizacao_ci` com `b3eaa94` e `05:40:50Z`.

**Acceptance Scenarios**:

1. O item contém dois registros de CI green com HEADs e timestamps distintos, sem apagar o primeiro.
2. `reviewDecision=REVIEW_REQUIRED` e nenhum merge em `main`/tag realizado.
3. YAML parseia com 8 itens.

## Requirements

- **FR-001**: acrescentar `atualizacao_ci` ao item `ESC-LR-20261122-008` com HEAD `b3eaa94`, RC=0, timestamp 05:40:50Z.
- **FR-002**: notas datadas em `STATE.md` e `docs/PROGRESSO.md`.
- **FR-003**: nenhum merge em main/tag/release/aprovação simulada.
- **FR-004**: nenhum código/workflow/cenário/contrato alterado.
- **FR-005**: cadeia SDD completa e gates determinísticos.

## Success Criteria

- SC-001: 1 atualização datada; SC-002: 0 owner ops; SC-003: gates verdes; SC-004: diff sem código.

## Assumptions

- Fila permite update via PR conforme cabeçalho do arquivo.
