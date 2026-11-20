# Checklist: Qualidade de requisitos — Fechar dívida de artefatos e evidência

**Purpose**: Validar qualidade, clareza e completude dos requisitos (não da implementação).
**Created**: 2026-11-20
**Feature**: [spec.md](../spec.md)

> `[x]` = revisor confirmou que o critério de qualidade de requisitos está satisfeito. Não afirma implementação concluída.

## Requirement Completeness

- [x] CHK001 — Os critérios de sucesso para os cinco candidatos de diagrama estão explicitados? [Completeness, Spec §Success Criteria SC-001]
- [x] CHK002 — Os requisitos para âncoras documentais incluem data, âncora e contagens verificadas? [Completeness, Spec §FR-004]
- [x] CHK003 — A exclusão explícita de código de jogo e atestação humana está definida? [Completeness, Spec §FR-007/FR-008]

## Requirement Clarity

- [x] CHK004 — O termo "revisão de qualidade de requisitos" está diferenciado de "implementação"? [Clarity, Spec §FR-006]
- [x] CHK005 — A distinção entre `machine-pass` e `verified` está especificada sem ambiguidade? [Clarity, Spec §FR-007]
- [x] CHK006 — O resultado esperado da verificação dos candidatos é mensurável? [Clarity, Spec §SC-001]

## Requirement Consistency

- [x] CHK007 — As métricas SC-001 a SC-005 são consistentes com FR-001 a FR-009? [Consistency]
- [x] CHK008 — A política de `[NV]` é aplicada de forma consistente entre spec, plano e âncoras? [Consistency, Spec §FR-005]

## Acceptance Criteria Quality

- [x] CHK009 — Cada requisito funcional possui critério de aceite observável? [Acceptance Criteria]
- [x] CHK010 — Os critérios de sucesso são tecnologicamente agnósticos e verificáveis? [Measurability, Spec §SC-001..SC-005]

## Scenario Coverage & Edge Cases

- [x] CHK011 — Os cenários de falha dos candidatos (schema antigo, fases sobrepostas) estão cobertos? [Coverage, Spec §Edge Cases]
- [x] CHK012 — O comportamento quando não há evidência auditável para um resíduo está definido? [Edge Case, Spec §Edge Cases]

## Dependencies & Assumptions

- [x] CHK013 — As suposições sobre `.archify/` ser gitignored e `docs/architecture/` ausente estão registradas? [Assumption, Spec §Assumptions]
- [x] CHK014 — A dependência do utilitário `archify` e a não-autoridade dos scripts da factory estão documentadas? [Dependency, Spec §FR-001/plan §Technical Context]

