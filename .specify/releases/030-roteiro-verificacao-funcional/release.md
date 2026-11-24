# Release — Spec 030 (Roteiro de verificação funcional)

- **Slug**: 030-roteiro-verificacao-funcional
- **Date**: 2026-11-21
- **SemVer**: `0.1.5` (PATCH — artefatos de verificação e evidência; zero mudança de comportamento jogável)
- **Validation**: `.specify/validations/030-roteiro-verificacao-funcional/validate.md` — APPROVED

## Changelog

- `FR-001`/`FR-002`/`T005`/`T006`: matriz `verification-matrix.md` com 11 grupos, comandos exatos e 1378 execuções / 0 falhas.
- `FR-003`/`T002`/`T015`: gates autoritativos do alvo executados; factory gates marcados N/A conforme Bloco 0.a.
- `FR-004`/`T008`: `compost-growth-hardening` alinhado a `PoorModerateBandEdge = 31f` (`[0, 31)`).
- `FR-005`/`T010`/`T011`: bundle `run-20261121T232308Z-64cf` com 3 sinais concordantes e `runId` registrado em `specs/004/test.md` pelo runner.
- `FR-006`/`T009`: limites legados em 004/005/010 inventariados e registrados como dívida, sem alteração.
- `FR-007`/`T012`/`T013`: notas datadas e append-only em `STATE.md`, `PROGRESSO.md`, `OBJETIVO.md`, `ROADMAP.md`.
- `FR-009`/`T014`: nenhuma promoção a `verified`; D1/E/SC-004 mantidos `[NV]`.
- `FR-010`: PRs #811 e #812 conduzidos com pin/lock corrigidos, sincronizados com `dev` por merge e mesclados (`048342b`, `18640ce`).
- Nenhum bug ID registrado: nenhum bug reportado em `.specify/bugs/` para este ciclo.

## Rollback

`git revert` dos commits do PR restaura o cenário, `test.md`, as notas de estado e os artefatos SDD; nenhuma mudança de save/schema/i18n/código de jogo. Se o Gate 3 falhar após revert, reaplicar a correção de threshold a partir de `verification-matrix.md`, nunca suprimindo o validador.

## Distribution

- **Nenhuma tag/release pública** — decisão do maintainer.
- PR reviewable via `gh pr create` para `dev`; sem auto-merge/force-push/delete de branch.
- Merge somente com checks obrigatórios verdes observados ao vivo; aprovador distinto não simulado (política do alvo exige 0 reviews; autorização explícita do prompt registra a decisão).
