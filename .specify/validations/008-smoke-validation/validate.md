# Validation — 008-smoke-validation

- **Spec:** `specs/008-smoke-validation/spec.md`
- **Feature:** Smoke Validation Script
- **Gate:** R4 (validate aprovado antes de release)
- **Tipo:** retroativo — evidência reconstruída a partir do estado verificado do repositório
- **Data:** 2026-11-10
- **Veredito:** APPROVED

## Evidência

- Spec presente: `specs/008-smoke-validation/spec.md`.
- Tasks: 0/0 concluídas (0 pendentes).
- FRs cobertos: FR-001, FR-002, FR-003, FR-004, FR-005, FR-006, FR-007, FR-008, FR-009, FR-010, FR-011, FR-012, FR-013, FR-014, FR-015, FR-016, FR-017 (17 total).
- Tasks referenciadas: T001, T002, T003, T004, T005, T006, T007, T008, T009, T010, T011, T012, T013, T014, T015, T016, T017, T018, T019, T020... (184 total).
- Build/test do mod: `dotnet build Stardew-LivingRoots.sln --configuration Release -m:1` e `dotnet test ...` verdes no estado atual (Gate 1).
- Quality gate local: cobertura, jscpd, semgrep conforme `docs/quality-gate.md` (Gate 2).
- Gate 3 (end-to-end): aplicável quando toca código executável; evidência em `evidence-bundle-run-*` e `VERIFICATION_PROTOCOL.md`.

## Riscos / pendências

- Nenhuma task aberta.
- Evidência retroativa: comandos não re-executados por spec; confiança baseada no estado global verde.

## Veredito

APPROVED
