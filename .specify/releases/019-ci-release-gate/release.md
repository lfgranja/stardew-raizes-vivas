# Release — 019-ci-release-gate

- **Spec:** `specs/019-ci-release-gate/spec.md`
- **Feature:** CI / Release Gate Rigoroso (local-only)
- **Gate:** R7 (SemVer, changelog, rollback, comunicação)
- **Versão do mod:** 0.1.0
- **Data:** 2026-11-10
- **Tipo:** retroativo

## Changelog

- 019-ci-release-gate: CI / Release Gate Rigoroso (local-only) (FRs: FR-019; tasks T001–T007).

## Rollback

- Reverter o commit que introduziu a feature; `dotnet build`/`dotnet test` devem permanecer verdes. Sem migração de schema de save nesta release (rollback seguro).

## Comunicação

- Notas de release do mod (CHANGELOG) atualizadas com esta feature.

## Veredito

RELEASED
