# Idea Intake: Quality Gate pytest wiring and dead suppression cleanup

- **Slug**: quality-gate-pytest-wiring
- **Created**: 2026-11-20T01:20Z
- **Source**: repo path — `scripts/quality-gate/tests/test_analyzer_config.py`, `LivingRoots/Services/CompostingBinService.cs`, `.github/workflows/quality-gate.yml`, `.specify/memory/STATE.md`
- **Type**: fix + compliance

## Idea (as captured)

> "Efetuar os próximos ciclos 80/20 em modo autônomo até extinguir completamente o trabalho que não possa ser completado sem intervenção humana ou autorização explícita do Software Engineer" (sessão 2026-11-20); candidato C2 do `STATE.md`: "Wire `pytest scripts/quality-gate/tests` into CI + remove dead `#pragma warning disable S8969` in `CompostingBinService.cs:115`" / "P1 (pragma S8969) fica adiado por exigir Gate 3 com atestação humana real".

## Restated

O gate Python de qualidade é executado apenas localmente e, no estado verificado de 2026-11-20, falha: a política de supressões aponta um `#pragma` sem justificativa e os testes de versão de analyzer esperam pins já superados por bumps mesclados. Propõe-se corrigir os pins nos testes, remover a supressão morta e executar a suíte pytest no CI, fail-closed.

## Origin & Context

- **Raised by**: maintainer (autorização explícita nesta sessão); dívida registrada em `.specify/memory/STATE.md` (P1/C2).
- **Trigger**: `PYTHONPATH=scripts/quality-gate python3 -m pytest scripts/quality-gate/tests -q` → 177 passed / 3 failed em 2026-11-20T01:19Z; `scripts/ci/aggregate-gate.sh` e `sdd-gate.py` verdes não cobrem a suíte Python.

## First-Glance Unknowns

- [NEEDS CLARIFICATION: remover o pragma em `LivingRoots/Services/` exige Gate 3 — machine-pass headless é suficiente ou é owner-gated?] — resolvido em `research.md`: machine-pass é automatizável; `verified` E permanece owner-gated.
- [NEEDS CLARIFICATION: qual workflow/job recebe o step pytest sem quebrar o boundary sem-toolchain do `sdd-gate`?] — resolvido em `research.md`/`concept.md`.
