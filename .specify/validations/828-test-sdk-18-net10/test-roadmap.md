# Roteiro de testes — bump Microsoft.NET.Test.Sdk 18.12.0

- **ALVO**: `/home/luis/development/stardew-raizes-vivas`; PR [#834](https://github.com/lfgranja/stardew-raizes-vivas/pull/834), base `dev`, herdando o commit `5f64baa` do PR [#828](https://github.com/lfgranja/stardew-raizes-vivas/pull/828).
- **Sessão**: `stardew-raizes-vivas - desbloqueio CI do bump SonarAnalyzer (#830)`; `session_id` `20261127T233246Z-lrv-dependabot-830-unblock`.
- **Escopo**: tornar o bump de `Microsoft.NET.Test.Sdk` 17.13.0 → 18.12.0 compatível com o conjunto de testes sem suprimir gates. Superfície CLI/headless; sem UI de produto, sem captura de tela fingida.

## Passos

1. Confirmar o head do PR de origem e o diff herdado: `git log --oneline -2` e `gh pr diff 828`. Esperado: dois arquivos do pacote de teste alterados.
2. Reproduzir a falha: com `net6.0`, `Build and Test` aborta com `Could not find testhost for test source .../net6.0/LivingRoots.Tests.dll` (run 46084036753).
3. Aplicar a correção mínima coerente: teste e runner para `net10.0`, `System.Text.Json` 11.0.0 explicitamente no teste, remoção das sobreposições CVE herdadas do `net6.0`, `RuntimeHelpers.GetUninitializedObject` no lugar de `FormatterServices`, e `System.Threading.Lock` no fixture. Nenhum `NoWarn` — o gate proíbe supressão global.
4. `GamePath=/home/luis/stardew-assemblies dotnet restore --force-evaluate` e `dotnet restore --locked-mode`: esperado `RC=0` nos quatro projetos.
5. `GamePath=/home/luis/stardew-assemblies dotnet build -c Release --no-restore -m:2`: esperado `0 Error(s)`.
6. `dotnet test --no-build -c Release --filter FullyQualifiedName~SoilHealthServiceTests`: esperado `78 passed`.
7. `PYTHONPATH=scripts/quality-gate python -m pytest scripts/quality-gate/tests -q` e `bash scripts/ci/aggregate-gate.sh`: esperado `180 passed` e `ALL STEPS PASSED`.
8. Publicar e observar `gh pr checks 834 --watch --interval 30` no head exato: esperado todos os checks `pass` ou `skipping` por design.
9. ACR independente: um revisor em contexto limpo, achados advisory, sem aprovação de plataforma.

## Rollback

`git revert` do merge em `dev` restaura `net6.0`, o lockfile anterior e as APIs de teste; nenhum dado ou save é afetado.
