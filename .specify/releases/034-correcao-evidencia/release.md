# Release — Spec 034 (Correção de atribuição de evidência)

- **Slug**: 034-correcao-evidencia
- **Date**: 2026-11-22
- **SemVer**: `0.1.9` (PATCH — metadado/atribuição documental; zero mudança jogável)
- **Validation**: `.specify/validations/034-correcao-evidencia/validate.md` — PENDENTE até saída real do `release-gate.sh`

## Changelog

- `FR-001`: SemVer `0.1.7` restaurado em `.specify/releases/032-*/release.md`.
- `FR-002`/`FR-003`: atribuição dos logs restrita a 19–20/11 em `research/2026-11-22-remote-nv-closure.md`, `STATE.md`, `PROGRESSO.md` e `docs/quality-gate.md`; extensão a 18/11 marcada como inferência.
- `FR-004`: nenhum código, workflow, cenário, contrato ou artefato 031–033 reescrito além das âncoras nomeadas.
- `FR-005`: cadeia SDD completa e gates determinísticos do alvo.

## Rollback

`git revert` dos commits do PR.

## Distribution

- Sem tag/release pública — decisão do maintainer.
- PR reviewable via `gh pr create`; sem auto-merge/force-push/delete.
