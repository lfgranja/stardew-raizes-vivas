# Release — Spec 036 (Refresh do CI green)

- **Slug**: ci-green-refresh
- **Date**: 2026-11-22
- **SemVer**: `0.1.11` (PATCH — atualização de evidência de escalonamento; zero mudança jogável)
- **Validation**: `.specify/validations/036-ci-green-refresh/validate.md` — PENDENTE até saída real do `release-gate.sh`

## Changelog

- `FR-001`: `atualizacao_ci` acrescentada ao item `ESC-LR-20261122-008` com HEAD `b3eaa94`, RC=0 e 05:40:50Z; registro anterior preservado.
- `FR-002`: `STATE.md`/`PROGRESSO.md` registram o refresh e o smoke 031 concluído (51/50/0/1).
- `FR-003`: nenhum merge em `main`, tag/release ou aprovação simulada.
- `FR-004`: nenhum código/workflow/cenário/contrato alterado.
- `FR-005`: cadeia SDD completa e gates determinísticos do alvo.

## Rollback

`git revert` dos commits do PR.

## Distribution

- Sem tag/release pública — owner-only.
- PR reviewable via `gh pr create` para `dev`; sem auto-merge/force-push/delete.
