# Release — 009-end-to-end-game-verification

- **Spec:** `specs/009-end-to-end-game-verification/spec.md`
- **Feature:** End-to-End In-Game Verification (Gate 3)
- **Gate:** R7 (SemVer, changelog, rollback, comunicação)
- **Versão do mod:** 0.1.0
- **Data:** 2026-11-10
- **Tipo:** retroativo

## Changelog

- 009-end-to-end-game-verification: End-to-End In-Game Verification (Gate 3) (FRs: FR-001, FR-002, FR-003, FR-004; tasks T001–T056).

## Rollback

- Reverter o commit que introduziu a feature; `dotnet build`/`dotnet test` devem permanecer verdes. Sem migração de schema de save nesta release (rollback seguro).

## Comunicação

- Notas de release do mod (CHANGELOG) atualizadas com esta feature.

## Veredito

RELEASED
