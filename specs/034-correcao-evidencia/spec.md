# Feature Specification: Correção de atribuição de evidência e SemVer

**Feature Branch**: `feature/034-correcao-evidencia`
**Created**: 2026-11-22

## Clarifications

- Logs verificados são de 19–20/11; nenhuma verificação direta dos runs de 18/11 nesta sessão.
- A resolução do `[NV]` permanece como mecanismo verificado; apenas a atribuição é restringida.

## User Scenarios & Testing

### US1 — Metadado e atribuição corretos (P1)

Como revisor, quero o SemVer do release 032 e uma atribuição exata dos logs ao `[NV]`, para não superinterpretar a evidência.

**Independent Test**: ler release 032 (SemVer `0.1.7`) e as âncoras (menção explícita a 19–20/11 + inferência para 18/11).

**Acceptance Scenarios**:

1. `release.md` 032 contém `0.1.7` PATCH.
2. Nenhuma âncora afirma verificação direta dos logs de 18/11.
3. O mecanismo `BUILDDEPS_TOKEN`/Dependabot permanece documentado como verificado em 19–20/11.

## Requirements

- **FR-001**: Restaurar `- **SemVer**: 0.1.7` em `.specify/releases/032-*/release.md`.
- **FR-002**: Acrescentar nota datada em `research/2026-11-22-remote-nv-closure.md` restringindo a atribuição.
- **FR-003**: Acrescentar notas em `.specify/memory/STATE.md`, `docs/PROGRESSO.md`, `docs/quality-gate.md` com a mesma ressalva.
- **FR-004**: Nenhum código/workflow/cenário/contrato alterado; nenhum artefato 031–033 reescrito além das âncoras nomeadas.
- **FR-005**: Cadeia SDD completa e gates determinísticos do alvo.

## Success Criteria

- **SC-001**: SemVer presente (baseline 0).
- **SC-002**: 0 afirmações de verificação direta de 18/11 (baseline implícita).
- **SC-003**: `aggregate-gate.sh` ALL STEPS PASSED; pytest/i18n verdes.
- **SC-004**: Diff sem código/workflows/scripts/cenários/contratos.

## Assumptions

- `gh run list` com limite 300 é suficiente para afirmar que os runs de 18/11 não foram consultados nesta sessão.
