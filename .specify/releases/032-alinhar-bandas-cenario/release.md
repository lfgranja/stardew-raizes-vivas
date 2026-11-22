# Release — Spec 032 (Alinhar bandas 31/71 nos cenários ativos)

- **Slug**: 032-alinhar-bandas-cenario
- **Date**: 2026-11-22
- **SemVer**: `0.1.7` (PATCH — correção de contrato de verificação; zero mudança de comportamento jogável)
- **Validation**: `.specify/validations/032-alinhar-bandas-cenario/validate.md` — APPROVED

## Changelog

- `FR-001`/`FR-002`/`T005`/`T006`: `compost-growth-healthy` e `soil-health-growth` passam a declarar Healthy `[71, 100)`, alinhados a `ModConstants.ModerateHealthyBandEdge = 71f`.
- `FR-003`/`T007`: exatamente 4 linhas alteradas nos dois cenários; campos protegidos preservados.
- `FR-004`/`FR-005`/`T008`/`T009`: dois runs Gate 3 host-only com `pass`, honestidade R e 3 sinais; blocos promovidos pelo mecanismo do runner.
- `FR-006`: `LivingRoots/`, workflows, `scripts/ci/`, contratos/fixtures `specs/010-*` e fixtures `boundary-*` intactos; única exceção runner-maintained: `promotion-triggers.json`.
- `FR-007`/`T010`: notas append-only em `specs/004/test.md`, `specs/005/test.md` e `docs/PROGRESSO.md`.
- `FR-008`: cadeia SDD completa — assess, specify, clarify (0/3), plan + fase 0, checklists 16/16 + 12/12, tasks (13), analyze, converge, R4 validate, R7 release.
- Nenhum bug ID: nenhum bug reportado em `.specify/bugs/` para este ciclo.

## Rollback

`git revert` dos commits restaura os thresholds `67` e os blocos anteriores; reexecutar os dois cenários para regenerar evidência. Nenhuma mudança de save/schema/i18n/código de jogo.

## Distribution

- **Nenhuma tag/release pública** — decisão do maintainer.
- PR reviewable via `gh pr create` para `dev`; sem auto-merge/force-push/delete de branch.
- Merge somente com checks obrigatórios verdes observados ao vivo; nenhum aprovador distinto simulado.
