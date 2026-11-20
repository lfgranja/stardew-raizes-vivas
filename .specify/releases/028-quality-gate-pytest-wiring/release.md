# Release — Spec 028 (Quality Gate pytest wiring)

- **Slug**: 028-quality-gate-pytest-wiring
- **Date**: 2026-11-20
- **SemVer**: `0.1.3` (PATCH — nenhuma mudança de comportamento jogável; infraestrutura de gate + remoção de supressão morta)
- **Validation**: `.specify/validations/028-quality-gate-pytest-wiring/validate.md` — aprovado

## Changelog

- `FR-001` / `T001`: pins de analyzer nos testes alinhados a `10.36.0.15337` (Sonar) e `11.0.100` (NetAnalyzers).
- `FR-002` / `T003`: removida supressão `#pragma warning disable/restore S8969` morta em `LivingRoots/Services/CompostingBinService.cs` (ex-`:115,:120`); lógica preservada.
- `FR-003` / `T006`: CI `Quality Gate / static` instala `requirements-dev.txt` e executa `python -m pytest scripts/quality-gate/tests -q`, fail-closed.
- `FR-004`: nenhum `continue-on-error`, secret, job novo ou mudança de `needs`.
- `FR-005`: Gate 3 machine-pass host-only via runner headless; sem `verified`.
- `FR-006`: estado registrado em `.specify/memory/STATE.md`.

## Rollback

`git revert` do commit do PR restaura os 4 arquivos de código/config; nenhuma mudança de save/schema/i18n. Se o pytest voltar a 177/3, reaplicar pins; se S8969 reaparecer, corrigir a origem (não re-suprimir).

## Distribution

- **Nenhuma tag/release pública** — decisão do maintainer.
- PR reviewable via `gh pr create` para `dev`; **sem merge/auto-merge/force-push/delete**.
- Check obrigatório verde observado ao vivo, aprovador distinto e revisão deliberada de `.github/workflows/**` permanecem **[NV]**.
