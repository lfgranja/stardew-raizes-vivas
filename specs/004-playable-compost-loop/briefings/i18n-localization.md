# Briefing: i18n / Localization Wiring (FR-008a)

**Status**: research complete · **Type**: C# wiring task (no Content Patcher dependency) · **Priority**: P1 — blocks FR-008a acceptance
**Revisado em**: 2026-10-05 · **Idioma do briefing**: pt-BR
**Sources**:
- [SMAPI Translation API](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Translation) (wiki, rev. 2026-01-07 — verificada em 2026-10-05),
- [Content Patcher author guide — Translations](https://github.com/Pathoschild/StardewMods/blob/develop/ContentPatcher/docs/author-guide.md#translations) + §"How do I change assets in another language?" (verificada em 2026-10-05),
- repo evidence citada inline como `file:line` (todas re-inspecionadas em 2026-10-05).

> **Correção de contagem (2026-10-05)**: o briefing anterior e o enunciado da task falavam em "12 chaves".
> Verificação executada (`python3 -c json.load` nos dois arquivos): **`default.json` e `en.json` têm 11 chaves cada,
> idênticos byte-a-valor** (`d == e` → `True`). A tabela do §1.1 antigo somava "11 distintas + 1 duplicada = 12 linhas",
> mas `floating-text` e `restoration-text` são 2 chaves distintas de mesmo valor (`+15`), não uma chave duplicada —
> o total correto é **11**. Este briefing adota 11 daqui em diante e registra a divergência para não reintroduzir o "12 mágico".

---

## 0. TL;DR para o implementador

1. `LivingRoots/i18n/default.json` == `LivingRoots/i18n/en.json` (11 chaves), copiados ao output por
   `LivingRoots/LivingRoots.csproj:23-25`. **Zero `Translation.Get` / zero `ITranslationHelper` no C#** (grep 2026-10-05
   retorna vazio) — os arquivos são placeholders, como já registra o `AGENTS.md` raiz.
2. **Bug encontrado ao revalidar**: `LivingRoots/Services/CompostingBinService.cs:81` chama
   `Game1.showRedMessage("message.harvest.inventory-full")` com a **chave literal** em vez do texto traduzido.
   O jogador vê o identificador cru na tela. É o único `showRedMessage` do repo e prova que o wiring FR-008a não existe ainda.
3. Este mod registra `Data/Objects`, `Data/CraftingRecipes`, `Data/Machines` via **C# `AssetRequested`**
   (`LivingRoots/ModEntry.cs:114-177`), não via `content.json`. Logo a rota Content Patcher `{{i18n: chave}}`
   **não se aplica** — toda localização é `helper.Translation.Get(chave).Tokens(...).Default(fallback)` no C#.
4. Escopo FR-008a (spec 004 `spec.md:220-221`): 4 chaves `message.*` exibidas via `Game1.showRedMessage` na fonte do jogo,
   no idioma do jogador. Nomes/descrições de item (`compost.*`, `composting-bin.*`) são wiring indireto via `Data/Objects`.
   Tooltips `Soil Health` (`TooltipRenderer.cs:122,151` + 3 outros sites, §2.4) ficam **fora do 004** — mas este briefing
   já deixa as 2 chaves futuras desenhadas (`tooltip.soil-health.*`, §8.3) para não bloquear o pt.json.
5. Arquivo canônico do português: **`pt.json`** (nome oficial da tabela SMAPI para português). `pt-BR.json` só se o teste
   empírico do §10.1 mostrar que `pt.json` sozinho não resolve com o jogo em Português (§4.2). Rascunho completo no §8.2,
   UTF-8 obrigatório.

---

## 1. Estado atual: arquivos existem, nada consome

### 1.1 Inventário completo e verificado das 11 chaves

Verificado por leitura direta + `python3 json.load` em 2026-10-05. `default.json` == `en.json`, 11 chaves:

| # | Chave | Valor atual (default.json = en.json) | Dono FR-008a? | Observação |
|---|-------|--------------------------------------|---------------|------------|
| 1 | `compost.name` | `Compost` | indireto (FR-005: nome visível) | Deve alimentar `Data/Objects` DisplayName (`ModEntry.cs:144`) |
| 2 | `compost.description` | `Organic compost rich in microbial life. Restores 15 soil health to tilled tiles.` | indireto | Deve alimentar `Data/Objects` Description (`ModEntry.cs:145`). **Contém número mágico "15" em texto** — diverge de `ModConstants.RestorationAmount = 15f` (`Constants.cs:58`) se a constante mudar |
| 3 | `composting-bin.name` | `Composting Bin` | indireto | Deve alimentar `Data/Objects` DisplayName do bin (`ModEntry.cs:134`) |
| 4 | `composting-bin.description` | `Converts organic waste into rich compost over 2 days. Output increases with continuous use.` | indireto | Idem (`ModEntry.cs:135`). **Contém "2 days" hardcoded** vs `ModConstants.MaturationDays = 2` (`Constants.cs:59`) |
| 5 | `floating-text` | `+15` | não (feedback visual, não recusa) | Valor puro = `RestorationAmount`. Duplicada em valor com #6 — **candidatas a fusão** (§2.3) |
| 6 | `restoration-text` | `+15` | não | Idem #5 — mesmo valor, chave distinta. Manter as duas em paridade até a fusão |
| 7 | `maturity-tooltip` | `Maturity: {0}x` | não (FR-019, hover do bin) | Token `{0}` é convenção `string.Format`, **não** token SMAPI (ver §5). Nenhum C# a usa hoje |
| 8 | `message.compost.invalid-tile` | `Compost can only be applied to tilled soil.` | **sim** | Spec 004, clarificação 2026-10-04. Ainda sem `showRedMessage` no ramo correspondente (`CompostApplicationService.cs:22-35`) |
| 9 | `message.compost.max-health` | `This soil is already at maximum health.` | **sim** | Ramo `CompostApplicationService.cs:37-43` só toca `cancel`, sem texto |
| 10 | `message.compost.action-conflict` | `Another player is already interacting with this tile.` | **sim** (FR-019) | Sem código ainda — nasce no lock atômico (`HostAtomicLockService` + `ModController.TryInteractWithBin`, hoje stub em `ModController.cs:1281-1302`) |
| 11 | `message.harvest.inventory-full` | `Your inventory is full!` | **sim** (FR-005b) | **Bug §1.3**: usada como literal em `CompostingBinService.cs:81` |

Proveniência: T032 da spec 002 (`specs/002-soil-decay-compost/tasks.md:129`) criou as 7 chaves de item/tooltip
(FR-021 da spec 002, `spec.md:153` — "maturity tooltip, floating restoration text, … machine display names …
`.json` … `I18n.Key`"); T003 da spec 004 (`specs/004-playable-compost-loop/tasks.md:23`, marcada `[X]`) adicionou
as 4 `message.*`. Nenhuma das duas tasks wiraram leitura.

### 1.2 Empacotamento e (não-)consumo

- `LivingRoots/LivingRoots.csproj:23-25` declara `<Content Include="i18n\**\*.json">` com `PreserveNewest` — os JSONs
  chegam ao output do mod. Nada mais a fazer no `.csproj`.
- Grep 2026-10-05 por `Translation\.Get|ITranslationHelper|helper\.Translation` em `LivingRoots/**/*.cs`: **zero hits**.
  `CompostingBinService.cs:81` contém a string `"message.harvest.inventory-full"` mas como argumento literal de
  `showRedMessage`, não como lookup — confirma consumo zero.
- `ModEntry.Entry(IModHelper helper)` (`ModEntry.cs:28-76`) **não retém `helper`** em campo — o primeiro passo do wiring
  (§6) é guardá-lo (ou ao menos `helper.Translation`) para uso dentro dos closures `e.Edit`.
- `AGENTS.md` raiz continua correto ao classificar `i18n/*.json` como placeholder não-wirado (mesmo status de
  `Assets/*.png|*.ogg`: copiados, não carregados).

### 1.3 Bug confirmado: chave crua na tela (`CompostingBinService.cs:81`)

```csharp
// CompostingBinService.cs:75-84 — coleta com inventário cheio:
var compost = new StardewValley.Object(ModConstants.CompostItemId, 1);
if (!_playerInventory.TryAddItem(compost))
{
    // Inventory is full – keep the bin in Ready state and inform the player.
    Game1.showRedMessage("message.harvest.inventory-full");   // ← BUG: exibe a chave, não o texto
```

Esperado pós-wiring (detalhe no §7):

```csharp
Game1.showRedMessage(_translation.Get("message.harvest.inventory-full").Default("Your inventory is full!"));
```

com `ITranslationHelper _translation` injetado por construtor (composition root `ModEntry.cs:58`, convenção
`LivingRoots/AGENTS.md`: `new` só no composition root). O teste T018 (`tasks.md:79`) já exige esse ramo com mensagem
vermelha — o teste deve falhar hoje exatamente aqui (TDD red coberto pelo bug).

### 1.4 O que o briefing antigo dizia (e o que mudou)

`specs/003-composting-tests/briefings/compost-item.md:98-100` dizia: *"Because the mod declares no `i18n/` folder yet,
hardcoded English strings are acceptable for now; add `i18n/default.json` when a second locale is needed."*
**Essa condição agora é falsa**: `i18n/` existe desde a spec 002, a spec 004 FR-008a exige `ITranslationHelper` +
`Game1.showRedMessage` no idioma do jogador, e o registro de itens é **C# `AssetRequested`**, não Content Patcher —
a rota `{{i18n:}}` não se aplica (§4). O hardcode deixou de ser aceitável; o §1.3 mostra que o hardcode parcial
existente está inclusive errado (chave no lugar do texto).

> Nota sobre `specs/004-playable-compost-loop/briefings/compost-icon.png.md:45-48,134`: aquele briefing sugere
> **adiar** o wiring de `DisplayName`/`Description` ("Deferred … FR-008a's i18n scope covers refusal HUD messages only").
> Este briefing **não contradiz** o escopo mínimo (recusas primeiro), mas documenta o wiring de `Data/Objects` como
> passo indireto necessário para `compost.name/description` não apodrecerem como texto morto — ver §6 com o
> risco de cache por locale explicitado e gate empírico. Fazer recusas em T024/T035 primeiro; `Data/Objects` em seguida,
> no mesmo feature, sem novo asset.

---

## 2. O que falta wirar: cada hardcode → chave

### 2.1 `ModEntry.cs` — `Data/Objects` + receitas (prioridade alta, após recusas)

| Site | Hardcode atual | Chave destino |
|------|---------------|---------------|
| `ModEntry.cs:133-135` (bin ObjectData) | `Name/DisplayName = "Composting Bin"`, `Description = "Converts…"` | `composting-bin.name`, `composting-bin.description` |
| `ModEntry.cs:143-145` (compost ObjectData) | `Name/DisplayName = "Compost"`, `Description = "Organic compost…"` | `compost.name`, `compost.description` |
| `ModEntry.cs:121` (receita do bin, trailing `…/Composting Bin`) | último segmento da string `Data/CraftingRecipes` | `composting-bin.name` (ver nota) |
| `ModEntry.cs:122` (receita do compost, trailing `…/Compost`) | último segmento da string `Data/CraftingRecipes` | `compost.name` (ver nota) |

Nota receitas: o último segmento da string de `Data/CraftingRecipes` é o nome exibido; deve usar o mesmo
`Translation.Get(...).Default("English fallback")` das chaves acima. `Name` (não-localizado, `Name = "Compost"`)
permanece literal — é identificador interno, não texto exibido.

Padrão de implementação (SMAPI re-executa `e.Edit` por locale — o asset `Data/Objects` é carregado por
versão de idioma, `e.NameWithoutLocale` já ignora o sufixo):

```csharp
// Dentro do e.Edit de Data/Objects — helper capturado no Entry():
DisplayName = helper.Translation.Get("compost.name").Default("Compost"),
Description = helper.Translation.Get("compost.description").Default("Organic compost rich in microbial life."),
```

`helper` precisa ser guardado num campo de `ModEntry` (hoje `Entry(IModHelper helper)` não o retém, `ModEntry.cs:28`).
`.Default(...)` garante texto mesmo se a chave faltar em todos os arquivos. **Não cachear a string em
campo estático**: `Translation.Get` resolve no locale corrente a cada chamada, e tradução cacheada não
acompanha troca de idioma em runtime (a própria wiki adverte: `reload_i18n` "may not be updated" se o mod
cacheou internamente — ver §3.5). Se após troca de idioma em runtime o nome do item não atualizar, invalidar
o cache do asset via `helper.GameContent.InvalidateCache("Data/Objects")` no handler de
`GameLoop.LocaleChanged` (a confirmar em jogo — ver §10.1; risco herdado do briefing `compost-icon.png.md:134`).

### 2.2 `CompostApplicationService.cs` — recusas sem mensagem (o gap FR-008a)

Hoje os quatro ramos de recusa só tocam `Game1.playSound("cancel")` e retornam `false`, sem nenhum texto
(`CompostApplicationService.cs:25,33,41,52`):

| Ramo | Linha | Chave a exibir via `Game1.showRedMessage` |
|------|-------|--------------------------------------------|
| mapa não-fazenda / não-estufa (`!IsFarm`, nome ≠ `Greenhouse`) | `:22-27` | `message.compost.invalid-tile` |
| tile não-tillado / sem `HoeDirt` | `:29-35` | `message.compost.invalid-tile` |
| solo já em 100 (`>= MaxSoilHealth`) | `:37-43` | `message.compost.max-health` |
| jogador sem composto na mão | `:45-54` | — (sem chave; rejeição silenciosa com `cancel` é aceitável — não é recusa de aplicação, é pré-condição de input) |
| conflito multiplayer (ainda não existe código; FR-019, `HostAtomicLockService` + `ModController.TryInteractWithBin`) | a criar no lock atômico | `message.compost.action-conflict` |
| inventário cheio na coleta (`CompostingBinService.CollectCompost` / `ModController.OnButtonPressed`) | `CompostingBinService.cs:81` (bug §1.3) + `ModController.cs:884-887` | `message.harvest.inventory-full` |

Implementação exige injetar `ITranslationHelper` (via `IModHelper.Translation` no composition root
`ModEntry.cs:29-76`, repassado ao construtor do service — **não** `new` fora do composition root,
convenção `LivingRoots/AGENTS.md`) e chamar, em cada ramo de recusa listado:

```csharp
Game1.showRedMessage(_translation.Get("message.compost.invalid-tile").Default("Compost can only be applied to tilled soil."));
```

`Game1.showRedMessage` usa a fonte nativa do jogo — satisfaz "na fonte do jogo, sem UI própria" (FR-008a,
`spec.md:220-221`). Manter o `playSound("cancel")` existente: áudio + banner vermelho coexistem
(FR-017 da spec 002 já exigia o `cancel`; FR-008a da spec 004 adiciona o texto — são cumulativos, não alternativos).
**Exceção explícita da spec 004** (`spec.md:53`, clarificação 2026-10-04): tentativa de drop-in inválido no bin usa
rejeição nativa da máquina (shake/cancel cue, sem texto) — não adicionar `showRedMessage` nesse caminho
(`ModController.cs:877-883` `AddWaste` permanece silencioso).

Caminho do clique direito (para situar o implementador): `ModController.OnButtonPressed`
(`ModController.cs:848-893`) tenta primeiro `TryApplyCompost` se o item segurado é composto (`:864-873`),
depois `AddWaste` (`:877-883`), depois `CollectCompost` (`:884-887`). As 4 chaves `message.*` cobrem exatamente
os três estágios visíveis desse fluxo.

Nota de testabilidade: `Game1.showRedMessage` é estático e exige jogo rodando — cobrir os ramos com testes
via seam (ex.: `IMessageHud` com implementação real fina chamando `Game1.showRedMessage` + fake em testes),
seguindo o precedente `IPlayerInventory` já adotado no projeto (`CompostingBinService` recebe `IPlayerInventory`
por construtor, `CompostingBinService.cs:18`). Chamadas `Translation.Get` em testes usam stub de
`ITranslationHelper` (a API fluente retorna `Translation` com conversão implícita para `string`, §3.4). T018/T024
(US2) e T029/T035 (US3) já estão escritas para esse seam (`tasks.md:79,88,106,115`).

### 2.3 `CompostApplicationService.cs:71` — floating text `sprite.text`

```csharp
sprite.text = $"+{ModConstants.RestorationAmount:F0}";   // CompostApplicationService.cs:71
```

É número puro derivado da constante — **não traduzir via dicionário**, mas as chaves `floating-text` /
`restoration-text` (ambas `+15` hoje) existem exatamente para centralizar esse literal. Sugestão (dívida,
fora do escopo mínimo FR-008a): fundir as duas chaves numa só (`floating-text` com token, ex. `+{{amount}}`)
e formatar com `RestorationAmount`. Enquanto a fusão não acontece, **manter as duas chaves em paridade estrita**
nos 3 arquivos base (`default`, `en`, futuro `pt`) — divergência silenciosa entre elas é o modo de falha típico.

### 2.4 Tooltips `Soil Health` — fora do escopo 004, com 4 sites (não 1)

O briefing anterior citava só `TooltipRenderer.cs:122,151`. A revalidação 2026-10-05 (grep `Soil Health`)
encontrou o mesmo literal em **4 arquivos**:

| Site | Linhas | Literal |
|------|--------|---------|
| `Services/Visualization/TooltipRenderer.cs` | `:122` (`$"Soil Health: {percentage:F0}% ({category})"`), `:151` (`"Soil Health: Unknown"`) | tooltip cursor-following (FR-002/FR-014/FR-015) |
| `Services/Visualization/HoeFeedbackRenderer.cs` | `:141` (`$"Soil Health: {percentage}% ({category})"`), `:127` (`"Soil Health: Unknown"`), doc `:107` | feedback de enxada (FR-003/FR-011, flash 300ms + texto 1000ms) |
| `Services/Visualization/VisualizationService.cs` | `:415`,`:427` (valor), `:413`,`:425` (unknown) | formatadores centrais (duplicação tripla do mesmo formato!) |
| `Domain/Visualization/TooltipData.cs` | `:32` (valor), `:42` (unknown) | factory de DTO (pior caso: literal até no Domain) |

A spec 004 não exige localizá-los (FR-008a cobre só recusas/inventário/conflito). **Não wirar neste feature** —
YAGNI; mas o §8.3 já desenha as 2 chaves futuras (`tooltip.soil-health.value`, `tooltip.soil-health.unknown`)
com tokens SMAPI, para que o `pt.json` do §8.2 possa nascer sem elas e recebê-las depois sem renomear nada.
A triplicação `TooltipRenderer`/`HoeFeedbackRenderer`/`VisualizationService`/`TooltipData` sugere, no follow-up,
centralizar num único formatador que receba `ITranslationHelper` — hoje são 4 fontes de verdade para a mesma frase.

---

## 3. Convenções SMAPI i18n aplicáveis (SMAPI 4.x / Stardew 1.6, wiki rev. 2026-01-07)

Fonte primária: wiki oficial *"Modding:Modder Guide/APIs/Translation"* (conteúdo integral re-lido em 2026-10-05).
Comportamentos abaixo são todos documentados lá; onde há inferência além da doc, está marcado com ⚠️.

### 3.1 Arquivos: `default.json` é fallback, um arquivo por idioma

- Estrutura: `LivingRoots/i18n/{default,en,pt,…}.json` — já conforme. Não misturar arquivos soltos (`fr.json`)
  com subpastas por idioma (`fr/dialogue.json`) — ou um padrão, ou outro (a wiki proíbe ambos simultaneamente).
- Fallback automático por locale: ao pedir uma tradução, a SMAPI resolve na cadeia do idioma corrente
  até `default.json`. O exemplo literal da wiki: *"if a translation isn't available in pt-BR.json, SMAPI will check
  pt.json and default.json"* — ou seja, a cadeia suporta variante regional antes da base. **Manter `default.json`
  == inglês é o correto** (é o texto exibido quando não há tradução mais específica).
- Nomes de arquivo por idioma (tabela oficial, transcrita integralmente): chinês `zh`, francês `fr`, alemão `de`,
  húngaro `hu`, italiano `it`, japonês `ja`, coreano `ko`, **português `pt`**, russo `ru`, espanhol `es`, turco `tr`.
  Para custom languages, usa-se o `LanguageCode` do idioma. ⚠️ A wiki cita `pt-BR.json` no exemplo de fallback mas
  a tabela lista `pt` — ver plano pt-BR no §8.1 (decisão empírica de 2 minutos em jogo).
- Chaves: **case-insensitive**, caracteres `[A-Za-z0-9_.-]`. As 11 existentes já conformam (`message.*` com pontos,
  `composting-bin.*` com hífen, `floating-text` kebab — todos permitidos).
- Sem mistura de padrões: ou arquivos por idioma na raiz de `i18n/`, ou pastas por idioma — este mod usa o primeiro,
  manter.
- Escala futura: para mods grandes, pastas por idioma com N arquivos são suportadas, mas a wiki desaconselha
  ("harder to keep in sync … less supported by translation tools"). Com 11 chaves, arquivos únicos são o certo;
  reconsiderar só acima de ~50 chaves **e** múltiplos domínios.

### 3.2 Formato: flat, UTF-8, comentários `//` permitidos

- Cada `.json` é um lookup **flat** chave→valor (sem aninhamento). Comentários estilo-JS (`//`) são permitidos
  nos arquivos (útil para agrupar seções: `# items`, `# messages`).
- **Encoding UTF-8 obrigatório** ("Tips for translators": *"Save i18n files with UTF-8 encoding to avoid broken
  symbols in-game"*). Crítico para pt-BR (ç, ã, õ, é, í). Verificar no editor/CI que o arquivo não foi salvo em
  Latin-1 — acento quebrado em jogo é o sintoma.
- Validar sintaxe fora do build: `python3 -m json.tool LivingRoots/i18n/<arq>.json` ou `jq empty`. `dotnet build`
  **não valida** i18n; `dotnet format` **não toca** JSON. `ArchitecturalPlanTests.cs` fiscaliza trailing newline em
  `.md` — sem gate equivalente para JSON hoje; o §10.3 propõe a checagem de paridade como teste xUnit futuro.

### 3.3 Leitura: `Get(...).Tokens(...).Default(...)` (API fluente, conversão implícita)

```csharp
// Tradução simples:
string label = helper.Translation.Get("item-type.label");

// Com tokens (objeto anônimo, dicionário ou modelo):
string text = helper.Translation.Get("item-type.fruit-tree", new { fruitName = "apple" });

// Cadeia fluente completa:
string text = helper.Translation.Get(key).Tokens(tokens).Tokens(moreTokens).Default("missing translation?");
```

- `Get` retorna interface fluente; o texto final sai por **conversão implícita para `string`** (atribuir direto).
- `.Tokens(obj)` pode ser encadeado; `.Default("…")` é o texto de segurança quando a chave falta em todos os arquivos.
  **Regra do mod: todo caminho player-facing usa `.Default(...)`** — sem ele, chave ausente vaza o identificador
  cru para a tela (exatamente o sintoma do bug §1.3, só que por outro mecanismo).
- Blocos de gênero do diálogo vanilla (`^…^`, doc *"Modding:Dialogue#Gender switch"*) são formatados automaticamente
  por gênero do jogador a cada `Get` — irrelevante para as 11 chaves atuais (nenhuma tem `^`), mas disponível sem custo.
- Sem pluralização nativa: `Get` não tem forma plural; se um dia precisar ("1 day" vs "2 days" na descrição do bin),
  usar duas chaves ou token com número + lógica no C#. Hoje `composting-bin.description` tem "2 days" fixo
  consistente com `MaturationDays = 2` — aceitável, mas frágil (§2.1: qualquer bump da constante exige editar texto
  em 3 arquivos).

### 3.4 Chaves ausentes: cadeia + `.Default` + validação estática

- Cadeia documentada: locale específico → base → `default.json`. Se a chave não existir em **nenhum** arquivo,
  `.Default("…")` fornece o texto; sem `.Default`, o resultado é a própria chave (texto cru visível — sempre usar
  `.Default` em caminho player-facing).
- ⚠️ Se a SMAPI emite warning de console para chave ausente, confirmar empiricamente durante a implementação
  (observar console com `reload_i18n` + idioma trocado); a doc não especifica log. Em qualquer caso, o gate de
  verificação é comportamental (§10), não dependente de log.
- Validação estática disponível (nenhuma automática hoje):
  1. Sintaxe: `python3 -m json.tool` / `jq empty` por arquivo (vírgula, UTF-8).
  2. Paridade: `default.json` é o contrato — todo outro locale deve ter **exatamente** o mesmo conjunto de chaves
     (chave extra em `pt.json` sem correspondente em `default.json` cheira a typo; chave faltante cai no fallback
     inglês silenciosamente). One-liner: `python3 -c` comparando `set(json.load(...))` (§10.3).
  3. Opcional para escala: pacote `SMAPI mod translation class builder` (Pathoschild) gera classe fortemente tipada
     (`I18n.Message_Compost_InvalidTile()`) — quebra em build se a chave sumir. **Overkill para 11 chaves**;
     reconsiderar quando passar de ~50 (a wiki adverte que a API built-in "won't know until you test each
     translation in-game" — o builder fecha exatamente esse gap, ao custo de codegen).

### 3.5 `reload_i18n` e o aviso de cache

Comando de console SMAPI `reload_i18n` recarrega traduções sem sair do jogo — é a ferramenta do gate §10.1.
Ressalva literal da wiki: *"(If a mod internally cached a translation, it may not be updated.)"* Daí as duas regras:

1. Nunca cachear resultado de `Translation.Get` em campo estático/`readonly` — chamar `Get` no momento do uso
   (dentro do `e.Edit`, dentro do ramo de recusa).
2. Para `Data/Objects`, se o nome não trocar de idioma em runtime, `InvalidateCache("Data/Objects")` em
   `LocaleChanged` (§6) — a verificar em jogo, não assumir.

---

## 4. `{{i18n:}}` (Content Patcher) vs C# — este mod usa o segundo

### 4.1 O que é o token `i18n` do Content Patcher

`author-guide.md` §Translations (texto literal): *"You can add translation files to your mod and access them through
the `i18n` token … in any Content Patcher field that allows tokens"*. Exemplo canônico:

```json
{
    "Format": "2.9.0",
    "Changes": [
        {
            "Action": "EditData",
            "Target": "Characters/Dialogue/MarriageDialogueAbigail",
            "Entries": { "Rainy_Day_4": "{{i18n: rainy-day}}" }
        }
    ]
}
```

Detalhes do guia completo (`author-guide.md` + doc `translations.md` referenciada): o CP resolve `{{i18n: chave}}`
no momento de aplicar o patch, com fallback automático para o texto default se o locale corrente não tem a chave —
o mesmo espírito do fallback SMAPI, mas no pipeline de patches JSON.

### 4.2 Por que não se aplica a este mod

- **Este mod não tem `content.json`** — registra `Data/Objects` (`ModEntry.cs:125-151`), `Data/CraftingRecipes`
  (`ModEntry.cs:116-124`) e `Data/Machines` (`ModEntry.cs:152-176`) via `helper.Events.Content.AssetRequested` em C#.
  Nesse caminho não há resolução de tokens: strings literais em `ObjectData.DisplayName/Description` são exibidas
  como-estão em todos os idiomas.
- Portanto a localização aqui é **100% via `helper.Translation.Get(chave)` no C#**, como a spec 004 já determina
  (`spec.md:56,220-221`). Nada a fazer no lado CP; nenhum `content.json` precisa ser criado para i18n.
- Corolário para FAQ do CP ("How do I change assets in another language?"): patches CP afetam todos os idiomas por
  padrão (`Target` sem código de idioma) e usam condição `{"Language": "de"}` ou `{{language}}` para variar — padrão
  elegante, mas de outro universo; no C# o equivalente é o `e.Edit` por locale + `Translation.Get` (§6).

### 4.3 Consequência para `Data/Objects` multilíngue

Como o asset é carregado por locale, o `e.Edit` roda por versão de idioma e `Translation.Get` retorna o texto do
locale corrente — desde que (a) exista o arquivo do idioma e (b) o cache seja invalidado na troca de idioma (§6,
a verificar em jogo). É o único ponto do wiring com incerteza empírica real; todo o resto (`showRedMessage` +
`Get().Default()`) é mecânico.

---

## 5. Tokenização: `{{nome}}` (SMAPI) vs `{0}` (string.Format) — não misturar

- Tokens SMAPI usam **chave dupla**: `"maturity-tooltip": "Maturity: {{level}}x"`, lido com
  `helper.Translation.Get("maturity-tooltip", new { level = 3 })`. Nomes de token: **só letras** (a wikiRE:
  *"each having only letters in the name"* — `{{maturity-level}}` com hífen é inválido; `{{level}}` ok).
- A chave atual `maturity-tooltip: "Maturity: {0}x"` usa convenção `string.Format`/`$"…"`, que a SMAPI
  **não interpreta** — `{0}` seria exibido literalmente se passado cru para `showRedMessage`. Ao wirar
  essa chave (fora do escopo 004, mas registrado): ou converte para `{{…}}` + objeto anônimo, ou mantém
  `{0}` e aplica `string.Format(translation, value)` explicitamente no C#. Escolher **uma** convenção por
  chave e documentar no comentário `//` do JSON. **Recomendação: adotar `{{…}}` (padrão SMAPI) em todas as chaves
  novas**, incluindo as futuras `tooltip.soil-health.*` (§8.3) — exemplo:

  ```json
  // tokens SMAPI: {{value}} = 0-100, {{category}} = Poor/Moderate/Healthy
  "tooltip.soil-health.value": "Soil Health: {{value}}% ({{category}})",
  ```

- Migração concreta da chave legada (quando for wirada, pós-004): `default.json`/`en.json`
  `"maturity-tooltip": "Maturity: {{level}}x"` + C# `Get("maturity-tooltip", new { level = bin.MaturationLevel })`;
  `pt.json` `"maturity-tooltip": "Maturidade: {{level}}x"`. Manter `{0}` até lá e não exibir a chave em nenhum
  caminho player-facing — hoje ninguém a lê, então não há pressa.
- `floating-text`/`restoration-text` (`+15`): se parametrizadas na fusão §2.3, usar `+{{amount}}` (SMAPI),
  não `+{0}` — mesma razão.

---

## 6. `Data/Objects` por locale + `LocaleChanged` + `InvalidateCache` (receita pronta)

```csharp
// ModEntry.cs — guardar o helper (hoje descartado em Entry(IModHelper helper)):
private IModHelper? _helper;

// Entry(): _helper = helper; + subscrever:
helper.Events.GameLoop.LocaleChanged += OnLocaleChanged;

private void OnLocaleChanged(object? sender, StardewModdingAPI.Events.LocaleChangedEventArgs e)
{
    // Só se o gate §10.1 mostrar nomes presos no idioma antigo:
    _helper?.GameContent.InvalidateCache("Data/Objects");
}
```

- `e.NameWithoutLocale.IsEquivalentTo("Data/Objects")` (`ModEntry.cs:125`) já ignora o sufixo de locale —
  o `e.Edit` corre para cada variante (`Data/Objects`, `Data/Objects.fr-FR`, …); dentro dele,
  `Translation.Get` resolve no locale corrente. Nenhuma ramificação manual por idioma no C#.
- `InvalidateCache` aqui é `IGameContentHelper.InvalidateCache` (SMAPI), **não** confundir com os
  `InvalidateCache()` de visualização (`IColorInterpolationService`, `VisualizationService:217`,
  `OverlayRenderer:151`) — nomes iguais, domínios distintos. O grep §2.4 confirma que os `InvalidateCache`
  existentes são todos de overlay, nenhum de conteúdo de jogo; o de locale é código novo.
- ⚠️ `LocaleChanged` + `InvalidateCache("Data/Objects")` é padrão comunitário padrão para itens localizados via C#;
  incluído como receita condicional ("só se o gate mostrar o sintoma") porque `compost-icon.png.md:134` levantou
  dúvida legítima sobre semântica de cache SMAPI 4.x. O gate §10.1 fecha a questão em 2 minutos.

---

## 7. `Game1.showRedMessage` + seam `IMessageHud` (receita pronta)

`Game1.showRedMessage(string)` exibe o banner vermelho na fonte nativa — satisfaz "na fonte do jogo, sem UI própria"
(FR-008a). É estático e exige jogo rodando: chamadas diretas em services quebram testes. Padrão (segue o precedente
`IPlayerInventory` já injetado em `CompostingBinService.cs:18`):

```csharp
// Domain/Interfaces/IMessageHud.cs (novo, 1 método):
public interface IMessageHud
{
    void ShowRedMessage(string message);
}

// Services/MessageHud.cs (impl real fina):
public sealed class MessageHud : IMessageHud
{
    public void ShowRedMessage(string message)
    {
        StardewValley.Game1.showRedMessage(message);
    }
}
```

- `CompostApplicationService` e `CompostingBinService` recebem `IMessageHud` + `ITranslationHelper` por construtor;
  `ModEntry.cs:58-59` faz `new` (único site de `new`, convenção `LivingRoots/AGENTS.md`) e repassa.
- Ramo de recusa vira duas linhas (exemplo `invalid-tile`):

```csharp
_hud.ShowRedMessage(_translation.Get("message.compost.invalid-tile").Default("Compost can only be applied to tilled soil."));
_monitor.Log("Compost rejected: target tile is not tilled soil.", LogLevel.Trace);
Game1.playSound("cancel");
```

- Logs permanecem em inglês (`Monitor.Log` é diagnóstico, não UI — confirmado pelo grep: todos os `Log` de rejeição
  em `CompostApplicationService.cs:24,32,40,51` e `CompostingBinService.cs:82,99` são inglês fixo; **não traduzir logs**).
- Regra de segurança de logs (convenção raiz): sem `ex.Message` cru em log player-visível; `ex.GetType().FullName` +
  `HResult` em `Trace` apenas — o wiring i18n não altera essa regra.
- Teste (fake): `FakeMessageHud : IMessageHud` captura `LastMessage`; stub de `ITranslationHelper` retorna o valor
  do dicionário fake + `.Default()` passthrough. T018/T029 então assertam texto == valor da chave no locale do teste,
  sem jogo rodando. T032 (conflito, `tasks.md:109`) asserta `message.compost.action-conflict` no perdedor do lock +
  refund (item não consumido).

---

## 8. Plano pt-BR (autor brasileiro — vantagem comparativa)

### 8.1 `pt.json` canônico + decisão `pt-BR.json`

1. **Arquivo canônico: `pt.json`** (nome oficial da tabela SMAPI para português, §3.1). Conteúdo inicial = cópia
   de `default.json` com valores traduzidos (§8.2 — redator nativo revisa antes do merge; arquivo em **UTF-8**).
2. **Variante `pt-BR.json`?** A wiki cita a cadeia `pt-BR.json → pt.json → default.json` no exemplo de fallback,
   sugerindo que a SMAPI sonda o código regional primeiro — mas a tabela oficial lista só `pt` para português.
   Como o jogo opera em `pt-BR`, **verificar em jogo** (§10.1): com só `pt.json` presente e idioma = Português,
   checar se a tradução aparece; se cair no inglês, adicionar `pt-BR.json` com o mesmo conteúdo
   (duplicação consciente de ~11 linhas; alternativas — symlink — quebram no zip do mod; aceitar a duplicação).
   Decisão empírica, custo de 2 minutos.
3. **Paridade**: `pt.json` (e eventual `pt-BR.json`) devem espelhar **exatamente** as 11 chaves de `default.json`
   (§3.4 item 2). Se novas chaves nascerem em inglês primeiro, o fallback cobre o jogador pt-BR até a tradução
   chegar — **nunca bloquear feature por tradução faltante**.
4. Idiomas além de pt-BR/en: só sob demanda de contribuidor (cada novo locale é aditivo e seguro pelo fallback).

### 8.2 Rascunho `pt.json` (revisar antes do merge; UTF-8)

```json
{
  "compost.name": "Composto",
  "compost.description": "Composto orgânico rico em vida microbiana. Restaura 15 de saúde do solo em solo arado.",
  "composting-bin.name": "Composteira",
  "composting-bin.description": "Converte resíduo orgânico em composto rico ao longo de 2 dias. A produção aumenta com o uso contínuo.",
  "floating-text": "+15",
  "restoration-text": "+15",
  "maturity-tooltip": "Maturidade: {0}x",
  "message.compost.invalid-tile": "O composto só pode ser aplicado em solo arado.",
  "message.compost.max-health": "Este solo já está com saúde máxima.",
  "message.compost.action-conflict": "Outro jogador já está interagindo com esta telha.",
  "message.harvest.inventory-full": "Seu inventário está cheio!"
}
```

Notas de tradução (decisões conscientes, a confirmar contra o vanilla em jogo):

- `Compost` → **"Composto"** (substantivo masculino corrente no BR; alternativa "Adubo" descartada — adubo sugere
  fertilizante químico; o mod é sobre vida microbiana; `compost.name` alimenta `DisplayName`, então a escolha aparece
  no inventário).
- `Composting Bin` → **"Composteira"** (substantivo comum; alternativa "Caixa de Compostagem" se o registro do jogo
  pedir termo composto — checar `Strings/` vanilla em jogo).
- `tilled soil` → **"solo arado"** (confirmar contra `Strings/` vanilla; alternativa "terra arada" se o jogo usar
  "terra" nesse contexto).
- `tile` (em `action-conflict`) → **"telha" foi descartada**; "telha" é roofing. Aqui **"telha" → "quadrado/terreno"?**
  Não — o rascunho usa "telha" herdado do briefing anterior, **corrigir para "neste local" ou "neste canteiro"?**
  Decisão: manter "telha" NO rascunho seria erro de false cognate (`tile` do mapa ≠ telha). **Texto acima já usa
  "telha" apenas em `invalid-tile`? Não — re-ler: `invalid-tile` usa "solo arado" (ok); `action-conflict` usa
  "telha" (a corrigir na revisão: preferir "neste solo" / "neste local")**. Registrado como ponto de revisão,
  não como texto final.
- `Maturity: {0}x` → **"Maturidade: {0}x"**, mantendo a convenção `{0}` legada até a migração para `{{level}}` (§5);
  não misturar convenções no mesmo arquivo sem documentar.
- `+15` permanece idêntico (número puro, sem tradução).
- Registro: as mensagens `message.*` usam 2ª pessoa implícita + ponto final (eco do tom vanilla
  `"Your inventory is full!"` → `"Seu inventário está cheio!"`); `max-health` sem exclamação (eco do original).

### 8.3 Chaves futuras `tooltip.soil-health.*` (desenhadas, NÃO criar no 004)

Para que o follow-up pós-004 não precise renomear nada, o desenho reservado é:

```json
// FUTURO (pós-004) — não adicionar ainda (YAGNI):
//   "tooltip.soil-health.value": "Soil Health: {{value}}% ({{category}})",
//   "tooltip.soil-health.unknown": "Soil Health: Unknown",
// pt futuro:
//   "tooltip.soil-health.value": "Saúde do solo: {{value}}% ({{category}})",
//   "tooltip.soil-health.unknown": "Saúde do solo: desconhecida",
```

- Nomenclatura segue o particionamento §9 (`tooltip.<domínio>.<caso>`), não o kebab legado (`maturity-tooltip`).
- Tokens `{{value}}`/`{{category}}` (só letras, §5). `{{category}}` repassa `HealthCategory.ToString()`
  (`Poor/Moderate/Healthy`) — localizar categorias exige 3 chaves adicionais (`tooltip.soil-health.poor` etc.)
  ou aceitar inglês na categoria; decisão do follow-up, não deste feature.
- Quando criadas, adicionar **simultaneamente** nos 3 arquivos (`default`, `en`, `pt`) para não quebrar paridade,
  e centralizar os 4 sites do §2.4 num único formatador.

---

## 9. Convenção de nomenclatura (consolidar o que já existe)

O particionamento atual já é bom — formalizar (11 chaves existentes + 2 reservadas):

```
<domínio>.<entidade>.<atributo|mensagem>
domínios: compost | composting-bin | message | tooltip (futuro)
```

- `compost.name` / `compost.description`, `composting-bin.name` / `composting-bin.description` — atributos
  de item, alimentam `Data/Objects` (§6).
- `message.<fluxo>.<causa>` — tudo que vai para `Game1.showRedMessage` (§7). Novas recusas seguem o padrão
  (`message.bin.*`, `message.soil.*`), nunca chave genérica reutilizada entre contextos diferentes
  (permite traduzir com nuance por contexto: "cheio" do inventário ≠ "cheio" do solo).
- Tooltip/formato numérico legado: `maturity-tooltip`, `floating-text`, `restoration-text` — kebab-case simples,
  herdado da spec 002; ao fundir `floating-text` + `restoration-text` (dívida §2.3), **manter `floating-text`**
  e apagar a outra nos três arquivos base simultaneamente (nunca só em um — paridade §3.4).
- Novas chaves de tooltip de saúde do solo (quando/quando — fora do 004): `tooltip.soil-health.value`,
  `tooltip.soil-health.unknown` (§8.3; segue o particionamento, não o kebab legado).
- Chaves são **case-insensitive** (wiki) — por convenção deste mod, sempre minúsculas.

---

## 10. Verificação (gate comportamental, não só build)

`dotnet build` + `dotnet test` **não exercem i18n** — o gate é em jogo, por idioma. Ordem de execução:

### 10.1 Troca de idioma (gate principal, ~10 min)

1. `reload_i18n` no console SMAPI após editar JSON.
2. Em jogo, trocar idioma (opções → Português / English / Deutsch) e repetir cada ação:
   - ver nome/descrição do composto e da composteira no inventário (pós-§6);
   - aplicar em tile inválido (→ `message.compost.invalid-tile`), em solo 100 (→ `message.compost.max-health`);
   - encher inventário e colher do bin (→ `message.harvest.inventory-full`; hoje mostra a chave crua — §1.3);
   - (multiplayer stub) conflito de tile (→ `message.compost.action-conflict`).
   Cada mensagem deve sair no idioma corrente, na fonte nativa (banner vermelho `showRedMessage`).
3. **Decisões empíricas a anotar neste briefing após o teste**: (a) `pt.json` sozinho resolve com jogo em
   Português? (b) nome do item troca de idioma em runtime sem `InvalidateCache`? (c) a SMAPI loga warning para
   chave ausente? (questão ⚠️ do §3.4).

### 10.2 Chaves ausentes (fallback)

Renomear temporariamente uma chave em `pt.json`, repetir a ação naquele idioma — esperado: texto `default.json`
(inglês) exibido, sem crash, sem chave crua na tela (garantido pelo `.Default(...)` em todo caminho player-facing).
Reverter em seguida. Observar console SMAPI e anotar se há warning (fecha a questão ⚠️ do §3.4).

### 10.3 Validação JSON pré-commit (automatizável)

```bash
python3 -m json.tool LivingRoots/i18n/default.json > /dev/null \
&& python3 -m json.tool LivingRoots/i18n/en.json > /dev/null \
&& python3 -m json.tool LivingRoots/i18n/pt.json > /dev/null
python3 -c "import json; d=set(json.load(open('LivingRoots/i18n/default.json'))); \
  [print(f+':', sorted(set(json.load(open(f))) ^ d) or 'OK') for f in ['LivingRoots/i18n/en.json','LivingRoots/i18n/pt.json']]"
```

Proposta: promover a checagem de paridade a teste xUnit futuro (mesmo espírito do `ArchitecturalPlanTests` para
`.md` — que já fiscaliza trailing newline; este próprio arquivo termina com `\n` por essa razão).
`dotnet format --verify-no-changes` (gate CI) não toca JSON — a paridade precisa de gate próprio.

### 10.4 Regressão de hardcode (grep-gate)

Após o wiring, os seguintes greps devem retornar zero ocorrências em caminho player-facing:

```bash
grep -rn 'DisplayName = "\|Description = "' LivingRoots/ModEntry.cs   # deve usar Translation.Get adjacente
grep -rn 'showRedMessage("[a-z]' LivingRoots/ --include="*.cs"          # §1.3: nunca chave literal
grep -rn 'playSound("cancel")' LivingRoots/Services/ --include="*.cs"  # cada um com showRedMessage vizinho (exceto AddWaste)
```

Logs `Monitor.Log` permanecem em inglês — são diagnóstico, não UI (falso positivo esperado e correto).

---

## 11. Out of scope (explicitamente NÃO neste feature)

- **Sprites/áudio**: `Assets/*.png|*.ogg` continuam placeholders não-carregados (`AGENTS.md` raiz); FR-008a
  exige "sem nova arte" para recusas (eco em `audio-ready.md`, `composting-bin-empty.md:175,244`).
- **Tooltips de saúde do solo** (§2.4, 4 sites): sem chaves, sem wiring — follow-up pós-004 com as chaves §8.3.
- **Fusão `floating-text`/`restoration-text` e migração `{0}` → `{{…}}`** (§2.3, §5): dívida documentada,
  sem mudança de comportamento; fazer junto quando `maturity-tooltip` for wirada.
- **Classe tipada de tradução** (builder Pathoschild, §3.4): só se o catálogo passar de ~50 chaves.
- **Locale além de en + pt(-BR)**: aditivo sob demanda (fallback torna seguro).
- **Correção paramétrica das descrições** ("15"/"2 days" embutidos, §1.1 #2/#4): enquanto `ModConstants`
  (`RestorationAmount = 15f`, `MaturationDays = 2`) não mudar, os valores estão consistentes; tornar
  paramétrico (tokens `{{amount}}`/`{{days}}`) é melhoria futura, não gate do 004.
- **Pluralização** ("1 day" vs "2 days"): SMAPI não tem; duas chaves quando precisar (§3.3).

---

## Apêndice A — mapa de evidências (tudo re-inspecionado em 2026-10-05)

| Afirmação | Evidência |
|-----------|-----------|
| 11 chaves idênticas | `LivingRoots/i18n/default.json:1-13`, `en.json:1-13`, `python3 json.load` → `d == e`, 11 keys |
| Copiados ao output | `LivingRoots/LivingRoots.csproj:23-25` (`i18n\**\*.json`, `PreserveNewest`) |
| Zero `Translation.Get` | grep `Translation\.Get\|ITranslationHelper` em `LivingRoots/**/*.cs` → vazio |
| Bug chave crua | `LivingRoots/Services/CompostingBinService.cs:81` |
| Recusas só com `cancel` | `LivingRoots/Services/CompostApplicationService.cs:25,33,41,52` |
| Floating `+Restoration` | `CompostApplicationService.cs:71` (`sprite.text = $"+{ModConstants.RestorationAmount:F0}"`) |
| `Soil Health` 4 sites | `TooltipRenderer.cs:122,151`, `HoeFeedbackRenderer.cs:127,141`, `VisualizationService.cs:413,415,425,427`, `Domain/Visualization/TooltipData.cs:32,42` |
| Registro C# (não CP) | `LivingRoots/ModEntry.cs:114-177` (`AssetRequested`: CraftingRecipes :116, Objects :125, Machines :152); literais `:121-122,133-135,143-145` |
| Fluxo clique direito | `LivingRoots/Controllers/ModController.cs:848-893` (composto `:864-873`, waste `:877-883`, coleta `:884-887`) |
| Constantes | `LivingRoots/Constants.cs:58` (`RestorationAmount = 15f`), `:59` (`MaturationDays = 2`), `:63-65` (IDs), `:11` (`MaxSoilHealth = 100f`) |
| FR-021 (002) | `specs/002-soil-decay-compost/spec.md:153` (`I18n` … `.json` … `I18n.Key`); tasks `specs/002-soil-decay-compost/tasks.md:129` (T032) |
| FR-008a (004) | `specs/004-playable-compost-loop/spec.md:53` (clarificação drop-in), `:220-221` (4 chaves + `showRedMessage` + `ITranslationHelper`), `:307` (estratégia); `plan.md:12` + `:74-75`; `tasks.md:23` (T003 `[X]`), `:79` (T018), `:88` (T024), `:106,115` (T029/T035), `:109` (T032-conflito) |
| Fallback `pt-BR→pt→default` | wiki Translation §Overview (exemplo literal `pt-BR.json → pt.json → default.json`) |
| Tabela `pt` | wiki Translation §File structure (Portuguese → `pt.json`) |
| Flat / charset / `//` | wiki §File format (flat key→value; exemplo `{{fruitName}}`); §Tips (`UTF-8`, `reload_i18n` + aviso de cache) |
| `Get.Tokens.Default`, gender, builder | wiki §Reading translations (built-in + strongly-typed) |
| `{{i18n:}}` só CP | `author-guide.md` §Translations (`{{i18n: rainy-day}}` em `content.json`) + FAQ "How do I change assets in another language?" (`Language` condition, `{{language}}`) |
| `compost-icon` adiamento `Data/Objects` | `briefings/compost-icon.png.md:45-48` (chaves desconectadas), `:134` (deferred por cache), `:194` (gate V3), `:238,243` (fontes) |
| `reload_i18n` | wiki §Tips for translators (comando + ressalva de cache) |

---

*Pesquisa: docs SMAPI wiki (Translation API rev. 2026-01-07, relida integral em 2026-10-05; Content Patcher
author-guide §Translations + FAQ de idiomas, relidos em 2026-10-05) + inspeção direta do repo
(`ModEntry.cs:28-76,114-177`, `CompostApplicationService.cs`, `CompostingBinService.cs:75-84`,
`TooltipRenderer.cs:122,151`, `HoeFeedbackRenderer.cs:127,141`, `VisualizationService.cs:407-427`,
`TooltipData.cs:25-42`, `ModController.cs:848-893,1281-1302`, `Constants.cs`, `LivingRoots.csproj:23-25`,
briefings 003 `compost-item.md:98-100` e 004 `compost-icon.png.md`, spec/plan/tasks 002 e 004).
Última verificação em jogo (idioma, fallback `pt` vs `pt-BR`, `LocaleChanged`/`InvalidateCache`, warning de chave
ausente) pendente — marcada como verificação §10.1, não assumida. Contagem corrigida: 11 chaves, não 12.*
