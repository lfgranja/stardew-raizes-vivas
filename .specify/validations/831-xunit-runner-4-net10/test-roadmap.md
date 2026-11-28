# Roteiro de testes — bump xunit.runner.visualstudio 4.0.1

- **ALVO**: `/home/luis/development/stardew-raizes-vivas`; PR [#835](https://github.com/lfgranja/stardew-raizes-vivas/pull/835), base `dev`, herdando o commit `91b6cf6` do PR [#831](https://github.com/lfgranja/stardew-raizes-vivas/pull/831).
- **Sessão**: `stardew-raizes-vivas - desbloqueio CI do bump SonarAnalyzer (#830)`; `session_id` `20261127T233246Z-lrv-dependabot-830-unblock`.
- **Escopo**: tornar o bump 2.8.2 → 4.0.1 compatível sem suprimir `NU1701` (que indicaria incompatibilidade real). Superfície CLI/headless; sem UI de produto.

## Passos

1. Reproduzir localmente com `net6.0`: `dotnet restore --locked-mode` falha com `NU1701` porque o pacote 4.0.1 só traz assets `net8.0`/`net472`.
2. Confirmar pelo nupkg que 4.0.1 não tem assets `net6.0`; portanto `NoWarn` não seria correção válida.
3. Aplicar a correção: teste e runner para `net10.0`, `System.Text.Json` 11.0.0 no teste, remoção das sobreposições CVE do `net6.0`, `RuntimeHelpers.GetUninitializedObject` e `System.Threading.Lock` nos fixtures.
4. `dotnet restore --force-evaluate` e `--locked-mode` com `GamePath=/home/luis/stardew-assemblies`: esperado `RC=0`.
5. `dotnet build -c Release --no-restore -m:2`: esperado `0 Error(s)` — em `net10.0` o analisador S9422 exige `System.Threading.Lock`, já aplicado.
6. `dotnet test --no-build -c Release --filter FullyQualifiedName~ScenarioValidatorTests`: esperado `3 passed` — este era o teste que valores `14` vs `0` com `net8.0`/STJ 11.
7. `PYTHONPATH=scripts/quality-gate python -m pytest scripts/quality-gate/tests -q` e `bash scripts/ci/aggregate-gate.sh`: esperado `180 passed` e `ALL STEPS PASSED`.
8. Publicar e observar `gh pr checks 835 --watch --interval 30` no head exato: esperado todos os checks `pass` ou `skipping` por design.
9. ACR independente, achados advisory.

## Rollback

`git revert` do merge em `dev` restaura `net6.0`, o lockfile e as APIs de teste anteriores; nenhum dado é afetado.
