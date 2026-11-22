# Requirements Quality Checklist: Deriva de banda em cenários

**Purpose**: Unit tests for requirements writing — avalia os requisitos, não a implementação.
**Created**: 2026-11-22
**Feature**: [spec.md](../spec.md)
**Ownership**: `[x]` = revisão de qualidade de requisitos, não conclusão de implementação.

## Requirement Completeness

- [x] CHK001 — Estão identificados os dois cenários ativos e os campos exatos a alterar? [Completeness, Spec §FR-001/002]
- [x] CHK002 — Está definido o limite máximo de linhas alteradas? [Completeness, Spec §FR-003]
- [x] CHK003 — Está especificado o conjunto de comandos de reexecução? [Completeness, Spec §FR-004]
- [x] CHK004 — Está definido o tratamento de `partial`/W? [Completeness, Spec §Edge Cases]

## Requirement Clarity

- [x] CHK005 — A banda Healthy está escrita com notação de intervalo inequívoca? [Clarity, Spec §FR-001]
- [x] CHK006 — "Promoção pelo runner" está distinguida de edição manual? [Clarity, Spec §FR-005]

## Requirement Consistency

- [x] CHK007 — Os valores 31/71 do spec coincidem com `ModConstants` e `HealthTierClassifier`? [Consistency, Spec §Background]
- [x] CHK008 — A exclusão de 010 é consistente entre spec, plan e research? [Consistency, Spec §FR-006]

## Acceptance Criteria Quality

- [x] CHK009 — O critério "4 linhas" é objetivamente contável? [Measurability, Spec §SC-002]
- [x] CHK010 — O critério de sinais é verificável sem interpretação? [Measurability, Spec §SC-003]

## Scenario Coverage

- [x] CHK011 — Falha de `--verify-bundle` possui requisito de não-promoção? [Coverage, Spec §Edge Cases]
- [x] CHK012 — A guarda existente que exclui 004/005 é tratada explicitamente? [Coverage, Spec §Edge Cases]

## Notes

- 12 itens gerados e revisados em 2026-11-22T03:51Z; `[x]` não afirma implementação.
