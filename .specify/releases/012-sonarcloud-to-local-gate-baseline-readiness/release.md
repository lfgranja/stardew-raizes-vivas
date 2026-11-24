# Release — 012-sonarcloud-to-local-gate-baseline-readiness

- **Spec:** `specs/012-sonarcloud-to-local-gate-baseline-readiness/spec.md`
- **Feature:** SonarCloud to Local Gate Baseline Readiness
- **Gate:** R7 (SemVer, changelog, rollback, comunicação)
- **Versão do mod:** 0.1.0
- **Data:** 2026-11-10
- **Tipo:** retroativo

## Changelog

- 012-sonarcloud-to-local-gate-baseline-readiness: SonarCloud to Local Gate Baseline Readiness (FRs: FR-001, FR-002, FR-003, FR-004, FR-005, FR-006, FR-009, FR-017; tasks T001–T011).

## Rollback

- Reverter o commit que introduziu a feature; `dotnet build`/`dotnet test` devem permanecer verdes. Sem migração de schema de save nesta release (rollback seguro).

## Comunicação

- Notas de release do mod (CHANGELOG) atualizadas com esta feature.

## Veredito

RELEASED
