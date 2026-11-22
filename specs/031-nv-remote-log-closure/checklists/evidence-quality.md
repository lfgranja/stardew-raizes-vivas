# Requirements Quality Checklist: Evidência Remota e Honestidade

**Purpose**: Unit tests for requirements writing — avalia a qualidade dos requisitos, não a implementação.
**Created**: 2026-11-22
**Feature**: [spec.md](../spec.md)
**Ownership**: `[x]` significa revisão de qualidade de requisitos pelo revisor, não conclusão de implementação.

## Requirement Completeness

- [x] CHK001 — Estão definidos os campos obrigatórios da evidência remota persistida? [Completeness, Spec §FR-001]
- [x] CHK002 — Está especificado o conjunto mínimo de execuções remotas independentes? [Completeness, Spec §FR-002]
- [x] CHK003 — Está definido o escopo da promoção do `[NV]`? [Completeness, Spec §FR-003]
- [x] CHK004 — Estão nomeadas todas as âncoras de estado que recebem nota? [Completeness, Spec §FR-004]

## Requirement Clarity

- [x] CHK005 — "Evidência verificada ao vivo" possui critério temporal explícito? [Clarity, Spec §FR-001]
- [x] CHK006 — O termo "humano/owner-gated" está discriminado dos `[NV]` resolvidos? [Clarity, Spec §FR-006]

## Requirement Consistency

- [x] CHK007 — As regras de append-only são consistentes entre spec, plan e research? [Consistency, Spec §FR-005]
- [x] CHK008 — A decisão de Gate 3 N/A é consistente com a ausência de diff em `LivingRoots/`? [Consistency, Spec §FR-007]

## Acceptance Criteria Quality

- [x] CHK009 — O critério de run IDs é objetivamente contável? [Measurability, Spec §SC-002]
- [x] CHK010 — O critério de escopo zero-code é verificável por comando? [Measurability, Spec §SC-005]
- [x] CHK011 — Os critérios de gate são observáveis sem interpretação subjetiva? [Measurability, Spec §SC-003/004]

## Scenario Coverage

- [x] CHK012 — O cenário de expiração de logs remotos possui requisito correspondente? [Coverage, Spec §Edge Cases]
- [x] CHK013 — O cenário de leitor apressado possui requisito de não-promoção? [Coverage, Spec §Edge Cases]

## Edge Case Coverage

- [x] CHK014 — PRs encerrados e artefatos históricos têm tratamento definido? [Edge Case, Spec §FR-005]

## Dependencies & Assumptions

- [x] CHK015 — A dependência de `gh` autenticado e a autoridade do gate do alvo estão registradas? [Assumption, Spec §Assumptions]

## Notes

- 15 itens gerados em 2026-11-22 e revisados em 2026-11-22T03:38Z: todos os critérios de qualidade de requisitos estão satisfeitos. `[x]` não afirma implementação.
