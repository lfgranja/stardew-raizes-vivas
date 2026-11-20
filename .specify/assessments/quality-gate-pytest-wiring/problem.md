# Problem: Quality Gate pytest wiring and dead suppression cleanup

- **Slug**: quality-gate-pytest-wiring
- **Date**: 2026-11-20T01:20Z

## Problem statement

Mantenedores e colaboradores confiam no gate de qualidade local, mas a suíte pytest não é executada em nenhum gate automático e, no estado verificado de 2026-11-20, falha (177/3): dois testes de pin de analyzer divergem dos bumps já mesclados e um terceiro aponta uma supressão Sonar morta em código de produção. O resultado é sinal verde em gates parciais (`sdd-gate`, `aggregate-gate`) convivendo com um gate Python vermelho não observado — risco de assimilar drift de política como "normal".

## Users / stakeholders

- **Usuário primário**: maintainer e colaboradores que rodam gates antes de abrir PR.
- **Stakeholders**: CI (`quality-gate.yml`), revisores de PR, política de analyzing estático do projeto.
- Não há usuário final do mod afetado diretamente — a mudança é de infraestrutura de verificação.

## Goals

1. Suíte pytest local verde (0 failures) com pins que reflitam os csprojs reais.
2. Supressões em `LivingRoots/` todas justificadas ou removidas — política aplicável e verificável.
3. Suíte pytest executada automaticamente no CI, fail-closed, sem adicionar toolchain ao boundary `sdd-gate`.
4. Evidência de Gate 3 (machine-pass headless) para a mudança em arquivo executável, sem promover a `verified`.

## Non-goals

- Não alterar versões de pacotes nos csprojs (bumps já mesclados e verdes).
- Não adicionar secrets, jobs novos, `continue-on-error`, auto-merge ou alterar proteções de branch.
- Não refatorar `ComportingBinService` além da remoção das duas linhas do pragma.
- Não resolver owner-gated (D1, atestação E, tag/release, RUNNER_AUDIT_STORE, segredos Dependabot).

## Success metrics

- `PYTHONPATH=scripts/quality-gate python3 -m pytest scripts/quality-gate/tests -q` → **180 passed / 0 failed** ou superior (baseline 177/3).
- `test_every_repository_suppression_is_documented` verde sem `NoWarn`/`nosemgrep`.
- `.github/workflows/quality-gate.yml` contém step pytest em `static` com `requirements-dev.txt`; testes de contrato de workflow permanecem verdes.
- `dotnet build`/`dotnet test` Release, `dotnet format --verify-no-changes`, `aggregate-gate.sh`, `sdd-gate.py`, `release-gate.sh` permanecem verdes.
- Gate 3 `machine-pass` host-only com bundle válido (`--verify-bundle` exit 0).

## Baseline (cost of inaction)

Drift de política se acumula: cada bump de analyzer exigirá correção manual dos testes locais; CI continua sem cobertura dos gates Python; a dívida P1 do `STATE.md` permanece indefinidamente "adiada por Gate 3".

## Open questions carried forward

- Gate 3 para mudança comment-only em `LivingRoots/Services/`: resolvido — `machine-pass` automatizável; `verified` E permanece **[NV]**.
- Merge do PR: **[NV]** — exige check obrigatório verde observado ao vivo + aprovador distinto + revisão deliberada; nenhum auto-merge.
