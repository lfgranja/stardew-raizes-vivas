# Release — 003-composting-tests

- **Spec:** `specs/003-composting-tests/spec.md`
- **Feature:** Composting State Machine Tests
- **Gate:** R7 (SemVer, changelog, rollback, comunicação)
- **Versão do mod:** 0.1.0
- **Data:** 2026-11-10
- **Tipo:** retroativo

## Changelog

- 003-composting-tests: Composting State Machine Tests (FRs: FR-1, FR-2, FR-3, FR-4, FR-5, FR-6, FR-7, FR-017, FR-018, FR-019; tasks T001–T117).

## Rollback

- Reverter o commit que introduziu a feature; `dotnet build`/`dotnet test` devem permanecer verdes. Sem migração de schema de save nesta release (rollback seguro).

## Comunicação

- Notas de release do mod (CHANGELOG) atualizadas com esta feature.

## Veredito

RELEASED
