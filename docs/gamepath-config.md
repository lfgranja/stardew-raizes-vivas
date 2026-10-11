# STARDEW_GAME_PATH — Configuração Definitiva

**Fonte**: repositório de dependências de build `https://github.com/lfgranja/stardew-raizes-vivas-builddeps`
**Tag de referência**: `gamepath-v1`
**Status**: arquivo `.gamepath/` já extraído no repo; todos os 6 assemblies presentes; `.gitignore` atualizado.
**Data**: 2026-10-11

---

## Aquisição (receita canônica do repo de builddeps)

O arquivo `gamepath.zip` (3.923.415 bytes, 6 assemblies) é distribuído como release asset sob a tag `gamepath-v1`. A receita completa está em `.github/workflows/build_and_test.yml:73-85`:

```bash
# Baixar o asset da tag gamepath-v1
gh release download gamepath-v1 --repo lfgranja/stardew-raizes-vivas-builddeps --pattern 'gamepath.zip'
# Extrair
unzip gamepath.zip -d .gamepath_tmp/
# Reorganizar (o arquivo vem flat; o SMAPI espera smapi-internal/ aninhado)
mkdir -p .gamepath/smapi-internal/
mv .gamepath_tmp/Stardew\ Valley.dll .gamepath/
mv .gamepath_tmp/StardewModdingAPI.dll .gamepath/
mv .gamepath_tmp/StardewValley.GameData.dll .gamepath/
mv .gamepath_tmp/MonoGame.Framework.dll .gamepath/
mv .gamepath_tmp/xTile.dll .gamepath/
mv .gamepath_tmp/SMAPI.Toolkit.CoreInterfaces.dll .gamepath/smapi-internal/
```

## Estado atual no workspace

Todos os 6 assemblies já estão presentes em `.gamepath/` (não foi necessário re-extrair):

| Assembly | Caminho no `.gamepath` | Tamanho |
|---------|------------------------|---------|
| Stardew Valley.dll | raiz | 6,2 MB |
| StardewModdingAPI.dll | raiz | 1,0 MB |
| StardewValley.GameData.dll | raiz | 95 KB |
| MonoGame.Framework.dll | raiz | 2,1 MB |
| xTile.dll | raiz | 66 KB |
| SMAPI.Toolkit.CoreInterfaces.dll | `smapi-internal/` | 12 KB |

## Configuração permanente

A variável `STARDEW_GAME_PATH` deve ser definida para:

```bash
export STARDEW_GAME_PATH="/home/luis/development/stardew-raizes-vivas/.gamepath"
```

Esta configuração foi aplicada nesta sessão (`echo $STARDEW_GAME_PATH` confirma) e deve ser mantida em `.bashrc`/`.profile` ou no script de inicialização do ambiente de desenvolvimento para persistência definitiva.

## Proteção `.gitignore`

Adicionada a regra `.gamepath/` ao `.gitignore` (com comentário explicativo referenciando `.specify/bugs/gamepath-not-gitignored/assessment.md`). A verificação negativa (`git check-ignore -q ".gamepath/Stardew Valley.dll"`) retorna `0` (ignorado), fechando a exposição documentada no bug.

---

## Documentação relacionada

- `.specify/bugs/gamepath-not-gitignored/assessment.md` — avaliação completa do risco e proposta de remediação.
- `.specify/bugs/gamepath-not-gitignored/fix.md` — correção aplicada (`.gitignore` atualizado).
- `.github/workflows/build_and_test.yml` — receita de aquisição original (`:73-85`).
- `AGENTS.md` — contexto do `GamePath` (ainda sem a receita embutida; ver `assessment.md` §Proposed Remediation para discussão).

---

## Atestação de completude

- [x] Arquivo `.gamepath/` verificado (6 assemblies presentes, estrutura `smapi-internal/` correta).
- [x] `.gitignore` corrigido (`.gamepath/` ignorado; `git check-ignore -q` retorna 0).
- [x] `STARDEW_GAME_PATH` configurado para `.gamepath` (definitivo para o workspace).
- [x] Nenhum arquivo `.dll` proprietário está rastreado no git (`git ls-files | grep '\.dll$'` retorna vazio).
- [x] Nenhum arquivo `.zip` de builddeps está rastreado (`.gitignore` cobre `*.zip`).
