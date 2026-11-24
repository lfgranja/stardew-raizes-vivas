# Specification Quality Checklist: Matriz de verificação

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-11-21
**Feature**: [spec.md](../spec.md)

> Ownership: `[x]` significa que o revisor determinou que o critério de qualidade de requisitos está satisfeito. Não afirma implementação concluída.

## Requirement Completeness

- [x] CHK001 — Are all implemented gameplay features enumerated as distinct verification groups? [Completeness, Spec §FR-001]
- [x] CHK002 — Is the exact command for each group specified, rather than an area to "cover"? [Clarity, Spec §FR-002]
- [x] CHK003 — Are the authoritative gates of the target named explicitly, excluding factory-only gates? [Consistency, Spec §FR-003]
- [x] CHK004 — Is the band drift expressed with the old and new literal values? [Measurability, Spec §FR-004]
- [x] CHK005 — Is the revalidation evidence (signals and honesty level) defined for the edited scenario? [Acceptance Criteria, Spec §FR-005]
- [x] CHK006 — Is the treatment of other legacy thresholds specified as report-only? [Boundary, Spec §FR-006]
- [x] CHK007 — Are the required note fields (date, head, anchor, counts, `[NV]`) enumerated? [Completeness, Spec §FR-007]

## Acceptance Criteria Quality

- [x] CHK008 — Is "0 falhas" quantified per group with an exact count to be pasted? [Measurability, Spec §SC-001]
- [x] CHK009 — Are gate results required at both the start and the close of the cycle? [Measurability, Spec §SC-002]
- [x] CHK010 — Are `exclusiveMin`/`exclusiveMax` values part of the acceptance criteria for the corrected predicate? [Clarity, Spec §SC-003]

## Scenario Coverage & Edge Cases

- [x] CHK011 — Are environment failures (missing `GamePath`, harness timeout) defined as provisioning rather than red gates? [Edge Case, Spec §Edge Cases]
- [x] CHK012 — Is the runner-rejection contingency defined with a precise restoration source? [Recovery, Spec §Edge Cases]
- [x] CHK013 — Are features without a Gate 3 scenario explicitly classified as an evidence gap? [Coverage, Spec §User Story 1]
- [x] CHK014 — Are human-only items (D1, human E, SC-004) bounded so no machine result can be promoted? [Non-Functional, Spec §FR-009]
- [x] CHK015 — Is the merge precondition (live green required checks) distinguished from the explicit merge authorization? [Ambiguity, Spec §FR-010]

## Dependencies & Assumptions

- [x] CHK016 — Is the zero-diff constraint on `LivingRoots/**` stated as an invariant? [Assumption, Spec §FR-008]

## Notes

- Revisão de requisitos realizada em 2026-11-21; nenhum item afirma implementação.
