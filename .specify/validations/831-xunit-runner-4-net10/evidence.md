# Evidência — PR 835 (xunit.runner.visualstudio 4.0.1)

| Passo | Comando/ação | Saída observada `[EV]` | Resultado |
|---|---|---|---|
| 1 | `dotnet restore --locked-mode` com `net6.0` | `NU1701: Package 'xunit.runner.visualstudio 4.0.1' was restored using '.NETFramework...' instead of 'net6.0'`, `RC=1` | FAIL reproduzido |
| 2 | `unzip -l xunit.runner.visualstudio.4.0.1.nupkg` | apenas `build/net472` e `build/net8.0`; nenhum asset `net6.0` | confirmado |
| 4 | `dotnet restore --force-evaluate` / `--locked-mode` após o ajuste | quatro projetos restaurados, `RC=0` | PASS |
| 5 | `dotnet build -c Release --no-restore -m:2` | `0 Error(s)`, `RC=0` | PASS |
| 6 | `dotnet test --no-build -c Release --filter FullyQualifiedName~ScenarioValidatorTests` | `Passed! 0 failed, 3 passed`, `RC=0` | PASS |
| 7 | `PYTHONPATH=scripts/quality-gate python -m pytest scripts/quality-gate/tests -q` | `180 passed`, `RC=0` | PASS |
| 7b | `bash scripts/ci/aggregate-gate.sh` | `[aggregate-gate] ALL STEPS PASSED`, `RC=0` | PASS |
| 8 | `gh pr checks 835 --watch --interval 30` no head `e7b7b6f` | `RC=0`; `Build and Test`, `Check Formatting`, `Quality Gate / static`, `build-analysis`, `sca`, `gate`, `sdd-gate`, `verify`, `Check Protected Paths`, SonarCloud = `pass`; codeql/semgrep = `skipping` | PASS |

Roteiro versionado: `.specify/validations/831-xunit-runner-4-net10/test-roadmap.md`. URL do PR: https://github.com/lfgranja/stardew-raizes-vivas/pull/835 . Green local não substitui o green remoto observado no head publicado. `[NV]`: produção, deploy e promoção em `main`.
