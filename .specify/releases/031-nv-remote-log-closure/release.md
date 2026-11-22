# Release — Spec 031 (Fechar [NV] de causa-raiz remota)

- **Slug**: 031-nv-remote-log-closure
- **Date**: 2026-11-22
- **SemVer**: `0.1.6` (PATCH — evidência documental e estado verificado; zero mudança de comportamento jogável)
- **Validation**: `.specify/validations/031-nv-remote-log-closure/validate.md` — **PENDENTE** até a saída real de `scripts/release-gate.sh` (job `bg_8`); não reivindicar APPROVED antes disso

## Changelog

- `FR-001`/`FR-002`/`T006`: `research/2026-11-22-remote-nv-closure.md` persiste comandos `gh`, run IDs 44641194770/44664287245/44664266743, step `Fetch game assemblies` e trechos literais de `BUILDDEPS_TOKEN is not set`.
- `FR-003`/`FR-006`: promoção limitada à causa step-level dos PRs #790–#796; demais `[NV]` humano/owner-gated preservados.
- `FR-004`/`T007`/`T008`/`T009`/`T010`: notas datadas e append-only em `docs/research/index.md`, `.specify/memory/STATE.md`, `docs/PROGRESSO.md` e `docs/quality-gate.md`.
- `FR-005`: nenhum artefato histórico 025–030 reescrito; verificação no diff do ciclo.
- `FR-007`/`SC-005`: zero diff em `LivingRoots/`, `.github/workflows/` e `scripts/ci/`.
- `FR-008`: cadeia SDD completa — assess (5 artefatos), specify, clarify (0/3), plan + fase 0, checklist 16/16 + 15/15 revisados, tasks (14), analyze read-only, converge, R4 validate, R7 release.
- Nenhum bug ID: nenhum bug reportado em `.specify/bugs/` para este ciclo.

## Rollback

`git revert` dos commits do PR restaura as quatro âncoras e remove o documento de pesquisa e os artefatos SDD. Nenhuma mudança de save/schema/i18n/código de jogo/workflow.

## Distribution

- **Nenhuma tag/release pública** — decisão do maintainer.
- PR reviewable via `gh pr create` para `dev`; sem auto-merge/force-push/delete de branch.
- Merge somente com checks obrigatórios verdes observados ao vivo; nenhum aprovador distinto simulado.
