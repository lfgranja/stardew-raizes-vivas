# Release — Spec 037 (Dedupe de transições)

- **Slug**: 037-dedupe-transitions
- **Date**: 2026-11-22
- **SemVer**: `0.1.12` (PATCH — correção de registro de evidência; zero mudança jogável)
- **Validation**: `.specify/validations/037-dedupe-transitions/validate.md` — PENDENTE até smoke completo

## Changelog

- `FR-001`/`FR-002`: removidas transições 275–277 (0-based) de `promotion-triggers.json`, preservando o primeiro conjunto para `run-20261122T041058Z-73a0`; formato minified mantido (diff de 1 linha).
- `FR-003`: nenhum código/workflow/cenário/contrato/bloco GATE3 alterado.
- `FR-004`: `test-spec-consistency.sh` → PASS (1683 checks).
- `FR-005`: cadeia SDD completa; gates determinísticos do alvo.
- Sem bug ID: defeito introduzido nesta sessão, registrado em research/validate.

## Rollback

`git revert` do commit restaura as duplicatas e o RED.

## Distribution

- Sem tag/release pública — owner-only.
- PR reviewable via `gh pr create` para `dev`; sem auto-merge/force-push/delete.
