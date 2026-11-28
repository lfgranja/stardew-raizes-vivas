# Evidência — PR 834 (Microsoft.NET.Test.Sdk 18.12.0)

| Passo | Comando/ação | Saída observada `[EV]` | Resultado |
|---|---|---|---|
| 2 | Log do run 46084036753 | `Could not find testhost for test source .../net6.0/LivingRoots.Tests.dll`, `Test Run Aborted`, `MSB4181` | FAIL reproduzido |
| 4 | `dotnet restore --force-evaluate` / `--locked-mode` com `GamePath=/home/luis/stardew-assemblies` | quatro projetos restaurados, `RC=0` nas duas execuções | PASS |
| 5 | `dotnet build -c Release --no-restore -m:2` | `0 Error(s)`, `RC=0` | PASS |
| 6 | `dotnet test --no-build -c Release --filter FullyQualifiedName~SoilHealthServiceTests` | `Passed! 0 failed, 78 passed`, `RC=0` | PASS |
| 7 | `PYTHONPATH=scripts/quality-gate python -m pytest scripts/quality-gate/tests -q` | `180 passed`, `RC=0` | PASS |
| 7b | `bash scripts/ci/aggregate-gate.sh` | `[aggregate-gate] ALL STEPS PASSED`, `RC=0` | PASS |
| 8 | `gh pr checks 834 --watch --interval 30` no head `c7681b5` | `RC=0`; `Build and Test`, `Check Formatting`, `Quality Gate / static`, `build-analysis`, `sca`, `gate`, `sdd-gate`, `verify`, `Check Protected Paths`, SonarCloud = `pass`; codeql/semgrep = `skipping` | PASS |

Roteiro versionado: `.specify/validations/828-test-sdk-18-net10/test-roadmap.md`. URL do PR: https://github.com/lfgranja/stardew-raizes-vivas/pull/834 . Green local não substitui o green remoto observado no head publicado. `[NV]`: efeito de produção, deploy, tag e promoção em `main`.
