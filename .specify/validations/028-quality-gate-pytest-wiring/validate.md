# Validation — Spec 028 (Quality Gate pytest wiring)

- **Slug**: 028-quality-gate-pytest-wiring
- **Validated**: 2026-11-20
- **Validation status**: aprovado (guardado como machine-pass; nenhuma afirmação `verified`)

## Gate 1 — Static checks & tests

- `PYTHONPATH=scripts/quality-gate python3 -m pytest scripts/quality-gate/tests -q`: **180 passed / 0 failed** (baseline 177/3; red-phase 179/1 após corrigir pins, green após remover o pragma) — 2026-11-20T01:27Z.
- `PYTHONPATH=scripts/quality-gate python3 -m pytest scripts/quality-gate/tests/test_workflow_contract.py scripts/quality-gate/tests/test_workflow_build_analysis.py scripts/quality-gate/tests/test_ci_secret_guards.py -q`: **31 passed**.
- Parser YAML: `sorted(d['jobs'])` → `['build-analysis', 'codeql', 'gate', 'sca', 'static']` (estrutura preservada).
- `GamePath=/home/luis/stardew-assemblies dotnet build Stardew-LivingRoots.sln -c Release -m:1 --no-restore`: **0 erros**, 13 warnings pré-existentes (NETSDK1138/TFM support) — 2026-11-20T01:33Z.
- `dotnet format Stardew-LivingRoots.sln --verify-no-changes` e `dotnet restore --locked-mode`: ver results abaixo (bloco de evidências anexado quando o gate terminar).

## Gate 2 — Product-level quality bar

- Pins em `test_analyzer_config.py:20-21` agora refletem `LivingRoots.csproj:20-21` e `LivingRoots.Tests.csproj:57-58` (`10.36.0.15337` / `11.0.100`).
- Nenhuma `#pragma warning disable` em `LivingRoots/` sem justificativa; `test_every_repository_suppression_is_documented` verde sem `NoWarn`/`nosemgrep`.
- Bloco `CompostingBinService.ApplyAddWasteResult` funcionalmente idêntico (apenas diretivas removidas).
- CI: `.github/workflows/quality-gate.yml` job `static` ganhou install de `requirements-dev.txt` + step pytest; `gate` (needs `[static, sca, build-analysis]`) continua fail-closed; nenhum `continue-on-error`, secret ou job novo.

## Gate 3 — End-to-end (host-only machine-pass)

- Runner: `scripts/verify-scenario.sh --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json --no-acquire --game-path "$HOME/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build --timeout 900`.
- Resultado/bundle: **[preenchido pelo runner — ver emissão ao vivo abaixo]**.
- `machine-pass` ≠ `verified` (constituição v2.0.0; `VERIFICATION_PROTOCOL.md`). `verified` (E) é atestação humana e **não** é reivindicada aqui.

## Scope & claims

- Nenhuma tag/release, mudança de CI provider/CLI, auto-merge, force-push ou delete de branch.
- Nenhuma afirmação remota/CI/proteção sem marcação **[NV]**.

## Outstanding / follow-ups (recorded, not blocking)

- D1 (sessão real host+farmhand), atestação E de Gate 3, SC-004, tag/release `0.2.0`, `RUNNER_AUDIT_STORE` (#523) — owner-gated **[NV]**.
- Merge do PR depende de check obrigatório verde observado ao vivo + aprovador distinto + revisão deliberada de `.github/workflows/**` — **[NV]**.
- `docs/PROGRESSO.md`, `docs/research/index.md`, scripts SDD da factory — N/A no alvo conforme Bloco 0.a.

---
- **Runner**: `./scripts/verify-scenario.sh --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json --no-acquire --game-path "$HOME/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build --timeout 900` (2026-11-20T01:23Z).
- **Bundle**: `evidence-bundle-run-20261120T012343Z-6b29/` — `runId=run-20261120T012343Z-6b29`, `verdict=pass`, `honestyLevel=R`.
- **Verdict**: `signalsComplete=true`, `signalsAgree=true`, `thresholdsOk=true`, `farmhandBreach=false`, `declaredSignals=[soilHealthStateDelta, cropPhaseDelta, smapiLog]`.
- **Offline trust root**: `./scripts/verify-scenario.sh --verify-bundle evidence-bundle-run-20261120T012343Z-6b29` → **exit 0** (2026-11-20T01:37Z).
- Duas tentativas anteriores no mesmo cenário produziram bundles `partial`/honesty W (`011938Z-7537`) e um bundle incompleto (`011936Z-3dc6`); o `pass`/R final é o da execução668 acima, verificado offline.
- **`machine-pass` host-only. `verified` (E) não é reivindicado.**
## Appendix — Gate 3 live evidence (append-only)

Evidência anexada acima em 2026-11-20T01:37Z; nenhuma reescrita de registro anterior.
