# Tasks: Quality Gate pytest wiring

**Feature**: `specs/028-quality-gate-pytest-wiring/spec.md` | **Plan**: `plan.md`
**Legend**: [P] = policy/gate, [T] = test, [I] = implementation, [D] = docs/evidence.

## Phase 1 — Contratos Python (green-first)

- [x] T001 [T] Atualizar `SONAR_VERSION` → `10.36.0.15337` e `NETANALYZERS_VERSION` → `11.0.100` em `scripts/quality-gate/tests/test_analyzer_config.py:18-19`.
- [x] T002 [T] Rodar `PYTHONPATH=scripts/quality-gate python3 -m pytest scripts/quality-gate/tests -q` e confirmar que resta exatamente 1 falha (`test_every_repository_suppression_is_documented`) — evidência de red-phase do pragma.

## Phase 2 — Supressão morta

- [x] T003 [I] Remover `#pragma warning disable S8969` (`:115`) e `#pragma warning restore S8969` (`:120`) em `LivingRoots/Services/CompostingBinService.cs`, preservando `if (result.Accepted) { ApplyAcceptedAddWaste(tile, result); }`.
- [x] T004 [T] Rodar pytest completo → 180 passed / 0 failed (registro no validate.md).
- [x] T005 [P] Rodar `dotnet build Stardew-LivingRoots.sln -c Release -m:1 --no-restore` com `GamePath=/home/luis/stardew-assemblies` → 0 erros.

## Phase 3 — CI wiring

- [x] T006 [I] Em `.github/workflows/quality-gate.yml` job `static`, após `:41`, adicionar:
  - `- name: Install test dependencies` / `run: pip install -r scripts/quality-gate/requirements-dev.txt # NOSONAR`
  - `- name: Quality gate tests` / `run: python -m pytest scripts/quality-gate/tests -q`
- [x] T007 [T] Rodar `PYTHONPATH=scripts/quality-gate python3 -m pytest scripts/quality-gate/tests/test_workflow_contract.py scripts/quality-gate/tests/test_workflow_build_analysis.py scripts/quality-gate/tests/test_ci_secret_guards.py -q` → verdes.
- [x] T008 [T] Parser YAML local: `python3 -c "import yaml,pathlib; d=yaml.safe_load(pathlib.Path('.github/workflows/quality-gate.yml').read_text()); print(sorted(d['jobs']))"`.

## Phase 4 — Gate 3 e gates locais

- [x] T009 [P] `bash scripts/ci/aggregate-gate.sh` → ALL STEPS PASSED; `python3 scripts/ci/sdd-gate.py --check` → 0 erros.
- [x] T010 [P] `dotnet format Stardew-LivingRoots.sln --verify-no-changes` e `dotnet restore --locked-mode` → exit 0.
- [x] T011 [P] Gate 3: `scripts/verify-scenario.sh` (compost-growth-hardening, --no-acquire, --game-path, --skip-build) → machine-pass; `--verify-bundle` exit 0.
- [x] T012 [P] `GamePath=/home/luis/stardew-assemblies scripts/release-gate.sh` → ALL STEPS PASSED.

## Phase 5 — Evidência e entrega

- [x] T013 [D] Escrever `.specify/validations/028-quality-gate-pytest-wiring/validate.md` (R4, aprovado) com gate results, bundle ID e limites honestos.
- [x] T014 [D] Escrever `.specify/releases/028-quality-gate-pytest-wiring/release.md` (R7): SemVer `0.1.3` PATCH, changelog com FR/T IDs, rollback.
- [x] T015 [D] Atualizar `.specify/memory/STATE.md` (append-only) com data, âncora, gates, dívidas e **[NV]** remanescentes.
- [x] T016 [D] Abrir PR reviewable via `gh pr create` para `dev`; **sem merge, sem auto-merge, sem delete de branch, sem force-push**.

**Dependências**: T001→T002→T003→T004/T005→T006→T007/T008→T009..T012→T013→T014→T015→T016. Gate 3 (T011) e release-gate (T012) podem rodar em paralelo após T008.
