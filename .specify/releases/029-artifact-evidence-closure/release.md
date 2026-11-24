# Release — Spec 029 (Fechar dívida de artefatos e evidência)

- **Slug**: 029-artifact-evidence-closure
- **Date**: 2026-11-20
- **SemVer**: `0.1.4` (PATCH — somente artefatos/documentação; zero mudança de comportamento jogável)
- **Validation**: `.specify/validations/029-artifact-evidence-closure/validate.md` — APPROVED

## Changelog

- `FR-001`/`T005`/`T006`: 5/5 candidatos `.archify/` válidos (baseline 3/5); desvio de 5 iterações registrado.
- `FR-002`/`T003`: fases, rotas e `mainPath` do candidato `workflow-heirloom-pipeline` corrigidos.
- `FR-003`/`T004`: candidato `workflow-verification-pipeline` convertido para o schema atual preservando 12 nós e 11 conexões.
- `FR-004`/`T007`/`T008`: âncoras `docs/PROGRESSO.md` e `docs/research/index.md` criadas; notas datadas em `OBJETIVO.md`/`ROADMAP.md`.
- `FR-005`/`FR-006`/`T009`/`T010`: resíduos de evidência registrados em `.specify/memory/STATE.md`; human-gated permanecem `[NV]`.
- `FR-008`/`T012`: zero diff em `LivingRoots/**`.
- `FR-009`/`T011`: gate autoritativo verde no fechamento.
- Nenhum bug ID registrado: não houve bug reportado em `.specify/bugs/` para este ciclo.

## Rollback

`git revert` dos commits do PR restaura os 5 candidatos e os documentos; nenhuma mudança de save/schema/i18n/código de jogo. Se algum candidato voltar a falhar, reaplicar as correções a partir de `validate-evidence.md`, nunca suprimindo o validador.

## Distribution

- **Nenhuma tag/release pública** — decisão do maintainer.
- PR reviewable via `gh pr create` para `dev`; sem auto-merge/force-push/delete de branch alheia.
- Merge somente com checks obrigatórios verdes observados ao vivo; aprovação de revisor distinto permanece **[NV]** se ausente.

