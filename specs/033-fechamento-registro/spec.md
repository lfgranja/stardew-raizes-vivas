# Feature Specification: Fechamento autônomo dos ciclos 031 e 032

**Feature Branch**: `feature/033-fechamento-registro`
**Created**: 2026-11-22
**Status**: Draft
**Input**: Assessment `autonomous-closeout` (go / Option A).

## Clarifications

### Session 2026-11-22

- Q: Gates locais interrompidos por teto viram PASS? → A: Não. Registrados como parciais; checks remotos são a verdade do merge.
- Q: Reescrever validate/release 031/032? → A: Não; append-only nas âncoras.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Desfecho auditável (P1)

Como maintainer, quero uma nota datada com os merges, checks e limites dos ciclos 031/032, para responder sem reconstruir logs.

**Why this priority**: é o pedido de encerramento honesto da orquestração.

**Independent Test**: ler `STATE.md` e `docs/PROGRESSO.md` e encontrar os IDs `c3c904e`, `686fea7`, #817, #818 e a lista de `[NV]` restantes.

**Acceptance Scenarios**:

1. **Given** os rollups ao vivo, **When** a nota é lida, **Then** os dois merges e os checks obrigatórios verdes aparecem com data/hora.
2. **Given** os gates locais com teto, **When** a nota é lida, **Then** aparecem como parciais, nunca como `ALL STEPS PASSED`.
3. **Given** D1/E/SC-004/tag/`RUNNER_AUDIT_STORE`/aprovador, **When** a nota é lida, **Then** permanecem `[NV]`.

## Edge Cases

- Log de job expirar antes da leitura → run IDs e trechos persistidos no corpo do ciclo.
- Leitor interpretar "fechamento" como tudo resolvido → lista explícita de `[NV]` remanescentes.

## Requirements *(mandatory)*

- **FR-001**: `STATE.md` DEVE receber nota append-only datada com merges #817/#818, commits, checks e limites locais.
- **FR-002**: `docs/PROGRESSO.md` DEVE receber nota datada equivalente.
- **FR-003**: Nenhuma afirmação de `ALL STEPS PASSED` PODE ser feita sem saída local completa.
- **FR-004**: Nenhum artefato dos ciclos 031/032 PODE ser reescrito.
- **FR-005**: Nenhum arquivo executável, workflow, contrato ou cenário PODE ser alterado.
- **FR-006**: O ciclo DEVE fechar cadeia SDD completa e executar gates determinísticos do alvo.

## Success Criteria *(mandatory)*

- **SC-001**: 2/2 âncoras com nota datada e IDs verificáveis (baseline 0/2).
- **SC-002**: 0 afirmações de gate local completo sem saída (baseline 0).
- **SC-003**: `aggregate-gate.sh` ALL STEPS PASSED no HEAD do ciclo.
- **SC-004**: `release-gate.sh` com saída registrada; se interrompido por teto, registrado como parcial.
- **SC-005**: diff sem `LivingRoots/`, workflows, `scripts/ci/`, cenários ou contratos.

## Assumptions

- `gh` autenticado; git repo local sincronizado com `origin/dev`.
