# Bug Verification: protected-paths-tracked-files

- **Slug**: protected-paths-script-missing
- **Tested**: 2026-10-18
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified (bug-specific); 3 pre-existing, unrelated test failures remain on the branch — see Residual Risks

## Summary

O sintoma original (`Protected Paths` falha com 10 erros porque DLLs proprietárias de Stardew Valley / SMAPI estão rastreadas no `.gamepath/`) **não se reproduz** após o fix: `scripts/check-protected-paths.sh` passa com `exit 0` (`all checks passed`), nenhum basename `BANNED_TRACKED` aparece em `git ls-files`, e `git check-ignore -v` confirma que `.gamepath/` (linha 481 do `.gitignore`) cobre os 4 caminhos de `PROTECTED`. A suíte `dotnet test` do repo apresenta **3 falhas preexistentes e não relacionadas** ao fix (comprovado executando os mesmos testes no commit pai `b361ab3`, antes da remoção dos DLLs).

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reprodução (pós-fix) — script passa | `bash scripts/check-protected-paths.sh` | pass | Exit 0; `all checks passed.` (antes: 10 falhas, exit 1) |
| Check 1 (`.gitignore` integrity) | saída do script (Check 1) | pass | `.env`, `.env.local`, `server.pem`, `secrets.json`, `credentials.json`, `.gamepath/*.dll` todos `ok`; `.env.example` `tracked` (esperado) |
| Check 2 (banned tracked files) | saída do script (Check 2) | pass | Nenhum `BANNED_TRACKED` basename rastreado |
| Índice limpo (DLLs proibidos) | `git ls-files \| grep -E "Stardew Valley\.dll\|..."` | pass | Vazio (nenhum DLL proibido rastreado) |
| `.gitignore` `.gamepath/` ativa | `git check-ignore -v -- .gamepath/*.dll` | pass | 4/4 resolvidos por `.gitignore:481:.gamepath/` |
| Escopo do commit de fix | `git diff --name-only a225466^ a225466` | pass | Apenas 6 DLLs + 3 docs (`.specify/bugs/...`); nenhum `.cs` alterado |
| Arquivos `.gamepath/` restantes | `git ls-files \| grep .gamepath/` | pass | Só assets do mod (`.ogg`, `.png`, `i18n/*.json`, `manifest.json`, `LivingRoots.dll`) — não proibidos |
| Workflow referência intacta | `grep -n check-protected-paths .github/workflows/protected-paths.yml` | pass | Linha 33 ainda chama `./scripts/check-protected-paths.sh` |
| Build (Release) | `dotnet build Stardew-LivingRoots.sln -c Release` | pass | `Compilação com êxito.` 0 erros, 15 warnings (`CS8602`, ver abaixo) |
| Suíte de testes completa | `dotnet test Stardew-LivingRoots.sln` | **fail** | 3 falhas / 1260 aprovados / 1 ignorado — **preexistentes** (ver Residual Risks) |
| Regressão — testes no commit pai | `dotnet test ... -f "ProductionSourceGuardTests\|UnicodeNormalizer_NormalizesFullwidthCharacters"` em `b361ab3` (worktree) | fail (idêntico) | Mesmas 3 falhas antes do fix → não são regressão |

## Output Excerpts

Script pós-fix:
```
==> Check 1: protected .gitignore entries are in force
    ok    .env                                                       ignored
    ok    .gamepath/Stardew Valley.dll                               ignored
    ok    .gamepath/MonoGame.Framework.dll                           ignored
    ok    .gamepath/StardewValley.GameData.dll                       ignored
    ok    .gamepath/smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll  ignored
==> Check 2: no prohibited assembly is tracked
    ok    Stardew Valley.dll not tracked
    ok    StardewModdingAPI.dll not tracked
    ...
protected-path preflight: all checks passed.
```

Build:
```
Compilação com êxito.
    15 Aviso(s)
    0 Erro(s)
```

Suíte de testes (pós-fix):
```
Com falha! – Com falha:     3, Aprovado:  1260, Ignorado:     1, Total:  1264, Duração: 1 m 3 s
```

Mesmos 3 testes no commit pai `b361ab3` (worktree temporário):
```
[xUnit.net] LivingRoots.Tests.ProductionSourceGuardTests.ProductionSources_NoUnreferencedConstantsSurvive [FAIL]
[xUnit.net] LivingRoots.Tests.ProductionSourceGuardTests.FalseComment_InvalidDropInShake_MatchesCode [FAIL]
[xUnit.net] LivingRoots.Tests.Invariants.SecuritySanitizationInvariantsTests.UnicodeNormalizer_NormalizesFullwidthCharacters [FAIL]
Com falha! – Com falha:     3, Aprovado:    13, Ignorado:     0, Total:    16
```

Mensagens de erro (idênticas pré e pós-fix):
```
Constants.cs not found at /home/luis/development/stardew-raizes-vivas/LivingRoots/Constants.cs.
Assert.Equal() Failure: Expected: "ABC"  Actual: "ＡＢＣ"
```

## Residual Risks

- **3 falhas preexistentes, não relacionadas ao fix** (provado: idênticas no commit pai `b361ab3`):
  1. `ProductionSourceGuardTests.ProductionSources_NoUnreferencedConstantsSurvive` — espera `LivingRoots/Constants.cs`, que não existe (o arquivo real é `LivingRoots/ModConstants.cs`). Teste desatualizado.
  2. `ProductionSourceGuardTests.FalseComment_InvalidDropInShake_MatchesCode` — mesma causa (`Constants.cs` ausente).
  3. `SecuritySanitizationInvariantsTests.UnicodeNormalizer_NormalizesFullwidthCharacters` — espera `"ABC"` mas recebe `"ＡＢＣ"`; a normalização (provavelmente FormKC/FormC) não está sendo aplicada no caminho testado.
  - Nenhuma dessas falhas lê `.gamepath/` ou as DLLs removidas; o commit de fix não alterou nenhum arquivo `.cs`.
- **15 warnings `CS8602`** em `LivingRoots.Tests/Invariants/SoilHealthPersistenceInvariantsTests.cs` (linhas 69, 70, 91, 93, 120, 121, 123, 151, 152, 154, 214–218) — ruído de análise estática pós-`Assert.NotNull()`; não afetam o gate.
- **Histórico do git**: os DLLs removidos ainda existem no histórico (`git log --all -- .gamepath/Stardew Valley.dll` → `a225466`, `7bd4bba`). Em repo público, se for exigida remoção completa do histórico (redistribuição de código proprietário), usar `git filter-repo`/BFG — fora do escopo deste fix.
- **`fix.md` impreciso**: descreve a remoção como `git rm --cached` com "arquivo local preservado por `.gitignore`". O commit `a225466` mostra `D` (`Bin ... -> 0 bytes`) e o working tree não contém os DLLs — ou seja, foram **deletados de fato**, não apenas desrastreados. O resultado é o mesmo (e mais seguro), mas a redação deve ser corrigida.
- **CI não exercido**: o workflow `Protected Paths` só fica verde após push/PR no `dev` (o commit está em `origin/fix/protected-paths-script-confirm`, não em `dev`). A verificação aqui foi local; o gate do GitHub Actions ainda precisa ser confirmado em CI.
- Diretório não rastreado presente: `.specify/bugs/sonarcloud-reliability-rating/` (não relacionado a este fix).

## Recommendation

**Fechar o bug** — a reprodução local do sintoma não ocorre mais e o fix é verificado end-to-end para o escopo do `Protected Paths`. Duas ações separadas recomendadas:
1. Abrir PR do `fix/protected-paths-script-confirm` para `dev` e confirmar o workflow `Protected Paths` verde no GitHub Actions (última etapa do gate).
2. Triar as 3 falhas preexistentes de teste (`Constants.cs` → `ModConstants.cs`; normalização Unicode) e corrigir a redação do `fix.md` — não são bloqueios para este bug, mas deixam a suíte do branch não-verde.
