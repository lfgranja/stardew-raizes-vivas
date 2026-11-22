# Release — Spec 035 (Promoção dev→main na fila SWE)

- **Slug**: 035-fila-swe
- **Date**: 2026-11-22
- **SemVer**: `0.1.10` (PATCH — registro de escalonamento; zero mudança jogável)
- **Validation**: `.specify/validations/035-fila-swe/validate.md` — PENDENTE até saída real do `release-gate.sh`

## Changelog

- `FR-001`: `ESC-LR-20261122-008` adicionado à fila oficial com status `escalado-swe`.
- `FR-002`: `STATE.md` e `PROGRESSO.md` registram PR #821 CI-green e bloqueio de aprovação.
- `FR-003`: nenhum merge em `main`, tag/release ou aprovação simulada.
- `FR-004`: nenhum código/workflow/cenário/contrato alterado.
- `FR-005`: cadeia SDD completa e gates determinísticos do alvo.

## Rollback

`git revert` dos commits do PR.

## Distribution

- Sem tag/release pública — owner-only.
- PR reviewable via `gh pr create` para `dev`; sem auto-merge/force-push/delete.
