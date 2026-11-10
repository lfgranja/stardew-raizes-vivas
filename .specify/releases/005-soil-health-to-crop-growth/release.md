# Release — 005-soil-health-to-crop-growth

- **Spec:** `specs/005-soil-health-to-crop-growth/spec.md`
- **Feature:** Soil Health → Crop Growth
- **Gate:** R7 (SemVer, changelog, rollback, comunicação)
- **Versão do mod:** 0.1.0
- **Data:** 2026-11-10
- **Tipo:** retroativo

## Changelog

- 005-soil-health-to-crop-growth: Soil Health → Crop Growth (FRs: FR-001, FR-002, FR-003, FR-004, FR-005, FR-006, FR-007, FR-008, FR-009, FR-012; tasks T001–T121).

## Rollback

- Reverter o commit que introduziu a feature; `dotnet build`/`dotnet test` devem permanecer verdes. Sem migração de schema de save nesta release (rollback seguro).

## Comunicação

- Notas de release do mod (CHANGELOG) atualizadas com esta feature.

## Veredito

RELEASED
