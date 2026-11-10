# Release — 015-harmony-harvest-seam

- **Spec:** `specs/015-harmony-harvest-seam/spec.md`
- **Feature:** Add Lib.Harmony and Harmony Patch for Crop.harvest (HarvestQualityService Seam)
- **Gate:** R7 (SemVer, changelog, rollback, comunicação)
- **Versão do mod:** 0.1.0
- **Data:** 2026-11-10
- **Tipo:** retroativo

## Changelog

- 015-harmony-harvest-seam: Add Lib.Harmony and Harmony Patch for Crop.harvest (HarvestQualityService Seam) (FRs: FR-001, FR-002, FR-003, FR-004, FR-005, FR-006, FR-007, FR-008, FR-009, FR-010, FR-011, FR-012, FR-013, FR-014; tasks T001–T067).

## Rollback

- Reverter o commit que introduziu a feature; `dotnet build`/`dotnet test` devem permanecer verdes. Sem migração de schema de save nesta release (rollback seguro).

## Comunicação

- Notas de release do mod (CHANGELOG) atualizadas com esta feature.

## Veredito

RELEASED
