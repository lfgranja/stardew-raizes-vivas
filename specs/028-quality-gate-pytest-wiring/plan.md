# Implementation Plan: Quality Gate pytest wiring

**Feature**: `specs/028-quality-gate-pytest-wiring/spec.md`
**Created**: 2026-11-20T01:24Z
**Technical context**: .NET 6 / C# latest, xUnit + Python 3.12 quality gate, GitHub Actions, gate autoritativo `scripts/release-gate.sh`.

## Architecture / seams

- Nenhuma mudança de arquitetura de produção. O bloco `CompostingBinService.cs:115-120` fica funcionalmente idêntico; apenas as diretivas de pragma são removidas.
- Testes de contrato Python em `scripts/quality-gate/tests/` são a fonte da verdade para pins de analyzer e política de supressão.
- CI: `.github/workflows/quality-gate.yml` job `static` permanece game-free; job `gate` (needs) inalterado.

## Phases

1. **Fase 1 — contratos Python (green-first)**: atualizar pins em `test_analyzer_config.py`; rodar pytest e confirmar que só resta a falha do pragma.
2. **Fase 2 — supressão morta**: remover `#pragma warning disable/restore S8969` (2 linhas). Rodar pytest completo → 180/0; build Release → 0 erros.
3. **Fase 3 — CI wiring**: adicionar steps em `quality-gate.yml:41` (install `requirements-dev.txt` + pytest). Rodar testes de contrato de workflow.
4. **Fase 4 — Gate 3 e gates locais**: `verify-scenario.sh` (compost) + `--verify-bundle`; `release-gate.sh`, `aggregate-gate.sh`, `sdd-gate.py`; i18n-parity se aplicável.
5. **Fase 5 — evidência e entrega**: `validate.md` (R4), `release.md` (R7), `STATE.md`, PR reviewable via `gh` — **sem merge**.

## Test strategy

- `PYTHONPATH=scripts/quality-gate python3 -m pytest scripts/quality-gate/tests -q` (180/0).
- `python3 -m pytest scripts/quality-gate/tests/test_workflow_contract.py test_workflow_build_analysis.py test_ci_secret_guards.py -q`.
- `dotnet build Stardew-LivingRoots.sln -c Release -m:1` com `GamePath=/home/luis/stardew-assemblies`.
- `scripts/release-gate.sh` (7 steps) com `GamePath` válido.
- `scripts/verify-scenario.sh --scenario specs/004-.../compost-growth-hardening.scenario.json --no-acquire --game-path ... --skip-build` + `--verify-bundle`.

## Risk mitigation

- S8969: verificado que o bloco não contém `!`; rebuild com `TreatWarningsAsErrors` é a checagem determinística.
- Workflow contract: lido `test_workflow_contract.py` — nenhuma asserção de step-list exata.
- Gate 3: rodar com game real headless; honesty R, host-only; `verified` E não reivindicado.
- Merge: **[NV]** — aprovador distinto + checks verde observados; nunca auto-merge.

## Rollback

Reverter os 4 arquivos (`test_analyzer_config.py`, `CompostingBinService.cs`, `quality-gate.yml`, `STATE.md`) via `git revert` do commit do PR; nenhuma mudança de schema/save.
