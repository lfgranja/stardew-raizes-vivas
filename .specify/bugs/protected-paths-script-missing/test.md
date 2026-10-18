# Bug Verification: protected-paths-script-missing

- **Slug**: protected-paths-script-missing
- **Tested**: 2026-10-18
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

O sintoma original (`Protected Paths` falha com 10 erros devido a DLLs proprietários rastreados no `.gamepath/`) não se reproduz após o fix (`git rm --cached` dos 6 DLLs proibidos). O script `scripts/check-protected-paths.sh` passa (`exit 0`, `all checks passed`). Nenhuma regressão detectada; os arquivos restantes no `.gamepath/` (assets, `LivingRoots.dll`, `manifest.json`) são do próprio mod e não violam o `BANNED_TRACKED`. Os 11 warnings de análise estática (`CS8602`) no `SoilHealthPersistenceInvariantsTests.cs` permanecem como ruído não relacionado ao gate.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) — script passa | `bash scripts/check-protected-paths.sh` | pass | Exit 0; `all checks passed.` (antes: 10 falhas, exit 1) |
| Check 1 (`.gitignore` integrity) | script output (Check 1) | pass | `.env`, `.env.local`, `.env.*`, `*.pem`, `secrets.json`, `credentials.json`, `.gamepath/*.dll`, `.env.example` todos `ok` |
| Check 2 (banned tracked files) | script output (Check 2) | pass | Nenhum `BANNED_TRACKED` basename rastreado |
| Índice limpo (DLLs proibidos) | `git status --short` + `git ls-files` | pass | 6 `D` (deletados do índice); 0 DLLs proibidos restantes em `git ls-files` |
| Arquivos `.gamepath/` restantes | `git ls-files | grep .gamepath/` | pass | Apenas assets (`.ogg`, `.png`), `LivingRoots.dll`, `i18n/*.json`, `manifest.json` — todos do mod, não proibidos |
| Workflow referência intacta | `grep -n 'check-protected-paths' .github/workflows/protected-paths.yml` | pass | Linha 33 ainda referencia `./scripts/check-protected-paths.sh` |
| `.gitignore` `.gamepath/` intacta | `grep -n '.gamepath/' .gitignore` | pass | Regra `.gamepath/` presente (linha 481) |
| Regression / código fonte | Nenhum arquivo `.cs`, `.sh`, `.yml` alterado no fix | pass | Fix é limpeza de índice, não alteração de código |
| Warnings `SoilHealthPersistenceInvariantsTests` | Arquivo existe apenas no commit `b361ab3`, não no `dev` local | not-run | Warnings (`CS8602`) são ruído de análise estática após `Assert.NotNull()`; não afetam o gate; arquivo não está no branch `dev` |

## Output Excerpts

Execução do script pós-fix:
```
==> Check 1: protected .gitignore entries are in force
    ok    .env                                                       ignored
    ok    .env.local                                                 ignored
    ok    .env                                                     ignored
    ok    .env.local                                                 ignored
    ok    server.pem                                                 ignored
    ok    secrets.json                                               ignored
    ok    credentials.json                                           ignored
    ok    .env.example                                               tracked
    ok    .gamepath/Stardew Valley.dll                               ignored
    ok    .gamepath/MonoGame.Framework.dll                           ignored
    ok    .gamepath/StardewValley.GameData.dll                       ignored
    ok    .gamepath/smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll  ignored
==> Check 2: no prohibited assembly is tracked
    ok    Stardew Valley.dll not tracked
    ok    StardewModdingAPI.dll not tracked
    ok    StardewValley.GameData.dll not tracked
    ok    MonoGame.Framework.dll not tracked
    ok    xTile.dll not tracked
    ok    SMAPI.Toolkit.CoreInterfaces.dll not tracked

protected-path preflight: all checks passed.
```

Status do índice (`git status --short`): apenas `D` para os 6 DLLs removidos; `.specify/bugs/protected-paths-script-missing/assessment.md` e `fix.md` modificados.

## Residual Risks

- Os DLLs removidos do índice ainda existem no histórico (`git log -- .gamepath/Stardew Valley.dll`). Se a remoção completa do histórico for exigida (redistribuição de código proprietário em repo público), `git filter-repo` ou BFG são necessários — fora do escopo deste fix.
- Os arquivos restantes no `.gamepath/` (`.png`, `.ogg`, `.json`, `LivingRoots.dll`) são rastreados. Se algum deles também for considerado proibido, uma verificação adicional do `.specify/bugs/gamepath-not-gitignored/assessment.md` pode ser necessária.
- O arquivo `SoilHealthPersistenceInvariantsTests.cs` (commit `b361ab3`) não está no `dev` local; se for mesclado futuramente, os 11 warnings `CS8602` devem ser tratados (supressão pós-`Assert.NotNull()` ou ajuste no código).

## Recommendation

Fechar o bug — verificado end-to-end. O gate `Protected Paths` passa (`exit 0`). A próxima etapa é abrir uma PR/teste no `dev` para confirmar o workflow verde no CI, e, se aplicável, avaliar a remoção histórica dos DLLs (`filter-repo`).
