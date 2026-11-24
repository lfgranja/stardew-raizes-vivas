# Release — 020-heirloom-seeds-pipeline

- **Spec:** `specs/020-heirloom-seeds-pipeline/spec.md`
- **Feature:** Heirloom Seeds / Sementes Crioulas Pipeline (Crioulo Parsnip — one-crop template slice)
- **Gate:** R7 (SemVer, changelog, rollback, comunicação)
- **Versão do mod:** 0.1.0
- **Data:** 2026-11-10
- **Tipo:** retroativo

## Changelog

- 020-heirloom-seeds-pipeline: Heirloom Seeds / Sementes Crioulas Pipeline (Crioulo Parsnip — one-crop template slice) (FRs: FR-001, FR-002, FR-003, FR-004, FR-005, FR-006, FR-007, FR-008, FR-009, FR-010, FR-011, FR-012, FR-013, FR-014, FR-015, FR-016, FR-017, FR-018; tasks T001–T035).

## Rollback

- Reverter o commit que introduziu a feature; `dotnet build`/`dotnet test` devem permanecer verdes. Sem migração de schema de save nesta release (rollback seguro).

## Comunicação

- Notas de release do mod (CHANGELOG) atualizadas com esta feature.

## Veredito

RELEASED
