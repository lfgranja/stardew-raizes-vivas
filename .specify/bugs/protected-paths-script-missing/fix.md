# Bug Fix: protected-paths-script-missing

- **Slug**: protected-paths-script-missing
- **Fixed**: 2026-10-18
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

O script `scripts/check-protected-paths.sh` existe e funciona corretamente; a falha (`exit 1`, 10 erros) era causada por arquivos proprietários rastreados no `.gamepath/` e basenames proibidos no índice. A correção removeu esses arquivos do índice (`git rm --cached`) sem deletar os arquivos de trabalho (preservados pelo `.gitignore`). O script agora passa (`all checks passed`). Os 11 warnings `CS8602` no `SoilHealthPersistenceInvariantsTests.cs` (commit `b361ab3`) são ruído de análise estática (post-`Assert.NotNull`) e não bloqueiam o gate.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `.gamepath/Stardew Valley.dll` | `git rm --cached` | Removido do índice; arquivo local preservado por `.gitignore` |
| `.gamepath/MonoGame.Framework.dll` | `git rm --cached` | Idem |
| `.gamepath/StardewValley.GameData.dll` | `git rm --cached` | Idem |
| `.gamepath/StardewModdingAPI.dll` | `git rm --cached` | Idem |
| `.gamepath/xTile.dll` | `git rm --cached` | Idem |
| `.gamepath/smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll` | `git rm --cached` | Idem |
| `.specify/bugs/protected-paths-script-missing/assessment.md` | updated | Adicionada seção `Additional CI Observations (Warnings)` sobre `SoilHealthPersistenceInvariantsTests.cs` |

## Diff Highlights

Os arquivos `.gamepath/*.dll` proibidos foram removidos do índice (`git status` mostra `D ` para cada um). Nenhum arquivo fonte (`.cs`, `.sh`, `.yml`) foi alterado.

## Tests Added or Updated

- Nenhum arquivo de teste alterado (o fix é no índice/repo, não no código).
- Local verification: `bash scripts/check-protected-paths.sh` → `all checks passed.` (exit 0).

## Local Verification

- `bash scripts/check-protected-paths.sh` → `protected-path preflight: all checks passed.` (exit 0).
- `git ls-files | grep -i "Stardew Valley.dll"` → sem resultado (não rastreado).
- `git status --short` → apenas `D` para os 6 DLLs removidos; `.specify/bugs/protected-paths-script-missing/assessment.md` modificado; nenhum arquivo do `.gamepath/` restante removido indevidamente.

## Deviations from Assessment

Nenhuma. A correção preferida (`git rm --cached`) foi aplicada exatamente. Os arquivos de trabalho permanecem no `.gamepath/` (não deletados), conforme `.gitignore`.

## Follow-ups

- Confirmar com `git commit` (se necessário para o PR) que o índice está limpo.
- Se a remoção completa do histórico (`git filter-repo`) for exigida (redistribuição de código proprietário em repo público), abrir uma tarefa separada.
- Os warnings `CS8602` em `SoilHealthPersistenceInvariantsTests.cs` (commit `b361ab3`) podem ser silenciados com `#pragma warning disable CS8602` após `Assert.NotNull()` se o arquivo for integrado ao branch `dev`.
- Testar o workflow `.github/workflows/protected-paths.yml` após push para confirmar `Protected Paths` verde.
