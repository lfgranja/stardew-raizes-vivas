# Feature Specification: Promoção dev→main CI-green na fila SWE

**Feature Branch**: `feature/035-fila-swe`
**Created**: 2026-11-22

## Clarifications

- CI do PR #821 é GREEN; o merge em `main` exige 1 aprovador distinto — owner/SWE-gated.
- Tag/release `v*.*.*` é owner-only; este ciclo não cria tag.

## User Scenarios & Testing

### US1 — Bloqueio acionável para o SWE (P1)

Como Software Engineer, quero ver na fila oficial que o PR dev→main está CI-green e que o único bloqueio é a aprovação, para decidir o merge.

**Independent Test**: ler `escalations.yaml` e encontrar item com `status: escalado-swe` referenciando #821 e `reviewDecision=REVIEW_REQUIRED`.

**Acceptance Scenarios**:

1. PR #821 com CI green e `REVIEW_REQUIRED` está registrado com data/hora e run IDs.
2. Nenhum merge em `main`, tag ou release foi executado por este ciclo.
3. STATE/PROGRESSO registram o próximo passo como decisão do SWE.

## Requirements

- **FR-001**: Acrescentar item `ESC-LR-20261122-008` em `specs/024-pilha-escalonamentos-nivel5/registry/escalations.yaml` (append-only, sem remover histórico).
- **FR-002**: Registrar em `.specify/memory/STATE.md` e `docs/PROGRESSO.md` o CI green do PR #821 e o bloqueio de aprovação.
- **FR-003**: Nenhum merge em `main`, tag `v*.*.*`, release pública ou aprovação simulada.
- **FR-004**: Nenhum código, workflow, cenário ou contrato alterado.
- **FR-005**: Cadeia SDD completa e gates determinísticos do alvo.

## Success Criteria

- **SC-001**: 1 item novo na fila (baseline 0).
- **SC-002**: 0 operações owner-gated executadas.
- **SC-003**: `aggregate-gate.sh` ALL STEPS PASSED; pytest/i18n verdes.
- **SC-004**: diff sem código/workflows/scripts/cenários/contratos.

## Assumptions

- Fila `escalations.yaml` é o registro canônico de escalonamentos (spec 024).
