# Briefing — Assets de apresentação e release do mod (ícone, screenshots, metadados)

> Status: briefing (não implementado). Agrupado de origem: `specs/future-roadmap/briefings/` (agrupado de release/apresentação).
> Escopo interno: **nenhum**. Este briefing não pertence a nenhuma spec ativa — `ROADMAP.md` não menciona ícone/screenshot/badge/release.
> Não promover para `specs/` sem `/speckit.specify`.

**Data**: 2026-10-05 · **Idioma**: pt-BR · **Autor do briefing**: subagente (orquestrado)
**Natureza**: briefing de *asset + metadado*. Única família de asset com **zero cobertura** num repo que é explicitamente público.

---

## Fontes primárias (todas revisadas em 2026-10-05)

| # | Fonte | URL | Método | Resultado que mudou este briefing |
|---|-------|-----|--------|-------------------------------------|
| P1 | **Schema oficial do manifest SMAPI** (validador do próprio SMAPI) | `POST https://smapi.io/json` com `SchemaName=manifest` | probe programático, 20+ manifests de teste | **`Icon` NÃO é chave de manifest.** Erro literal: `Property 'Icon' has not been defined and the schema does not allow additional properties.` |
| P2 | Índice de compatibilidade SMAPI (mod-list, dados abertos) | `https://smapi.io/mods` → blob `smapistorage.blob.core.windows.net/smapi-web-temp/uploads/mod-list` | leitura integral do JSON | **4740 mods, zero campo de ícone.** União de chaves = `AlternateAuthors, AlternateNames, Author, Compatibility, DevNote, GitHubRepo, Id, ModPages, Name, Slug, SourceUrl, Warnings`. `LivingRoots` **não está listado**. |
| P3 | `Pathoschild.Stardew.ModBuildConfig` 4.4.0 (o pack que este repo já usa) | `~/.nuget/packages/pathoschild.stardew.modbuildconfig/4.4.0/build/` | leitura de `Pathoschild.Stardew.ModBuildConfig.targets` + `strings` em `SMAPI.ModBuildConfig.dll` | `EnableModZip` default **true**; zip em `$(ModZipPath)`=`$(TargetDir)`; zip já é nomeado por `ModFolderName` + versão do manifest. O zip **já existe** em `LivingRoots/bin/LivingRoots 0.0.1.zip`. |
| P4 | Nexus Mods — *File Submission Guidelines* | `https://help.nexusmods.com/article/28-file-submission-guidelines` (last updated **2026-09-29**) | leitura integral | Regra de IP de terceiros; **obrigação de tagueamento de IA**; exigência de claims verdadeiras; imagens relevantes. |
| P5 | GitHub Docs — *Customizing your repository's social media preview* | `https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/customizing-your-repositorys-social-media-preview` | leitura integral | **Upload só pela UI** (Settings → Social preview → Edit). PNG/JPG/GIF, **< 1 MB**, ≥640×320 recomendado, 1280×640 ideal. **Não há caminho em repo documentado.** |
| P6 | SMAPI home (confirma que a doc de manifest migrou) | `https://smapi.io/` | leitura | O sidebar só linka `https://stardewvalleywiki.com/Modding:Index`. `smapi.io/docs/config/manifest` retorna **404** — o briefing pai apontava para uma URL morta. |

> **Correção ao briefing pai**: `https://smapi.io/docs/config/manifest` **não existe** (404). A fonte primária correta do schema é o endpoint de validação `POST https://smapi.io/json` (`SchemaName=manifest`), que é o mesmo JSON Schema que o editor do usuário pode consumir localmente (o site oferece isso explicitamente: *"Tip: you can validate directly in your text editor if it supports JSON Schema"*).

## Fontes internas

| Arquivo | Linhas | O que se tira |
|---------|--------|--------------|
| `LivingRoots/manifest.json` | 1-9 | 7 chaves; 263 B; **sem newline final** (`b'.0"\n}'`) |
| `LivingRoots/LivingRoots.csproj` | 19-29 | 3 regras `Content … CopyToOutputDirectory=PreserveNewest`: `manifest.json`, `i18n\**\*.json`, `Assets\**\*.*` |
| `LivingRoots.Tests/LivingRoots.Tests.csproj` | 10-12, 19-21, 51 | `EnableModDeploy=false`, `EnableModZip=false`, `EmbeddedResource ..\README.md` |
| `LivingRoots/Constants.cs` | 31-53 | paleta (§3) |
| `LivingRoots/bin/LivingRoots 0.0.1.zip` | listing | conteúdo real do artefato de release (§8) |
| `.github/workflows/build_and_test.yml` | 3-27, 49-128 | ver §8 |
| `.github/workflows/code-style.yml` | 7-38 | ver §8 |
| `.github/workflows/protected-paths.yml` | 1-33 | ver §8 |
| `.gitignore` | 50 | `*.zip` — release zip nunca é commitado |
| `README.md` | 1-47 | ver §7 |
| `CONTRIBUTING.md` | 95 | *"**Pixel Art:** If contributing visual assets, try to maintain the Stardew Valley pixel art style."* |
| `LICENSE` | 1-3 | MIT, `Copyright (c) 2025 Luís Felipe` |
| `specs/004-playable-compost-loop/briefings/compost-icon.png.md` | **193-197** | formulação de licensing/PAL reutilizada **literal** em §4 |
| `specs/003-composting-tests/briefings/README.md` | 18-28 | convenção "asset vs não-asset" — reaplicada em §11 |
| commit `a1d5305` | — | `feat(assets): create authentic Stardew pixel art sprites for compost and composting bin` (2026-10-04) |

---

## 1. Gap table

| # | Gap | Evidência | Severidade | Encerrado por |
|---|-----|-----------|-----------|----------------|
| G1 | **Sem ícone do mod em qualquer lugar** | nenhum `Icon.*`, `logo.*`, `*.ico` no repo (`find` sobre `LivingRoots/Assets` → 4 PNG + 2 ogg) | **Blocking** para distribuição | §2.3, §7, §8 |
| G2 | **Sem screenshots** | nenhum arquivo de imagem além dos 4 sprites de gameplay; README não tem imagem | **Blocking** para SMVCR/Nexus | §2.3, §7 |
| G3 | **`manifest.json` no mínimo do schema** | `manifest.json:1-9` — só as 7 chaves obrigatórias + nenhuma opcional | Non-blocking (é **válido**, §5) | §5 |
| G4 | **Assets wired mas nunca carregados** | 4 PNG + 2 ogg stub (`OggS` de 24 B, **payload zero** — sem stream de áudio) vão para o zip, zero referências em C# | Non-blocking, mas é peso morto no release | fora de escopo (§11) |
| G5 | **Sem badge / preview social** | README sem badge; sem preview social no repo | Non-blocking (cosmético, alta visibilidade) | §7 |
| G6 | **Licença da arte não declarada** | `LICENSE` é MIT e cobre "software and associated documentation files", mas não menciona arte; `CONTRIBUTING.md:95` pede pixel art em estilo SDV sem regra de IP | **Blocking** para Nexus (§4) | §4 |
| G7 | **i18n não cobre texto de apresentação** | `i18n/default.json` tem 12 chaves, **todas** de HUD/tooltip em-jogo; nenhuma de README/promoção | Non-blocking, mas precisa de regra explícita | §6 |
| G8 | **CI não produz nem consome arte de release** | nenhum dos 3 workflows faz upload de release; `build_and_test.yml` só produz coverage | **Blocking** para o ícone "subir" | §8 |
| G9 | **`manifest.json` sem newline final** | `d[-5:] == b'.0"\n}'` | Trivial | §9 |

**G1+G2 são a maior lacuna estrutural do repo.** Um mod SMAPI público funciona sem ícone — SMAPI não exige um, e o mod-list oficial não tem campo de ícone (P2). Mas os *canais de distribuição* (SMVCR, Nexus, GitHub Releases, página do mod) todos esperam uma imagem, e o briefing pai está certo: é a única família com zero cobertura.

---

## 2. Inventário de assets

### 2.1 O que existe (verificado byte a byte, 2026-10-05)

| Caminho | Dimensões | Formato | Bytes | Cenus de cor | Wired? | Dentro do zip? |
|---------|-----------|---------|-------|--------------|--------|----------------|
| `LivingRoots/Assets/compost.png` | **16×16** | PNG 8-bit RGBA (colortype 6), non-interlaced | 165 | 5 opacos (141/256 = 55% transparente) | ❌ não | ✅ sim |
| `LivingRoots/Assets/composting_bin_empty.png` | **16×32** | PNG 8-bit RGBA, non-interlaced | 170 | 5 opacos (242/512 = 47% transp.) | ❌ não | ✅ sim |
| `LivingRoots/Assets/composting_bin_processing.png` | **16×32** | PNG 8-bit RGBA, non-interlaced | 235 | 9 opacos (239/512 = 46% transp.) | ❌ não | ✅ sim |
| `LivingRoots/Assets/composting_bin_ready.png` | **16×32** | PNG 8-bit RGBA, non-interlaced | 222 | 9 opacos (238/512 = 46% transp.) | ❌ não | ✅ sim |
| `LivingRoots/Assets/Audio/composting_bin_processing.ogg` | — | OggS header | **24** | — (stub, sem stream) | ❌ não | ✅ sim |
| `LivingRoots/Assets/Audio/composting_bin_ready.ogg` | — | OggS header | **24** | — (stub, sem stream) | ❌ não | ✅ sim |
| `LivingRoots/i18n/default.json` | — | JSON, 12 chaves | 719 | — | ✅ (`CopyToOutputDirectory`) | ✅ sim |
| `LivingRoots/i18n/en.json` | — | JSON, idêntico ao default | 719 | — | ✅ | ✅ sim |

**Nenhum PNG de 32×32 ou maior. Nenhum `Assets/Icon.*`. Nenhum screenshot.**

### 2.2 Procedência da arte (commit `a1d5305`)

```
feat(assets): create authentic Stardew pixel art sprites for compost and composting bin
Luis <lfgranjaead@gmail.com>   Sun Oct 4 23:59:51 2026 -0400

 LivingRoots/Assets/compost.png                   | Bin  82 -> 165 bytes
 LivingRoots/Assets/composting_bin_empty.png      | Bin  91 -> 170 bytes
 LivingRoots/Assets/composting_bin_processing.png | Bin  91 -> 235 bytes
 LivingRoots/Assets/composting_bin_ready.png      | Bin  91 -> 222 bytes
```

O commit **substituiu stubs de 82-91 B por arte multi-cor**. O primeiro briefing a mentionar isso (`compost-icon.png.md` §1.1) ainda descreve os arquivos como "6 unique colors / placeholder-grade"; o census de hoje confirma **5 cores opacas** no `compost.png` (o 6º "color" do briefing anterior era a contagem de transparente). OIcão **deve seguir esta proveniência**: pixel art original, authored in-house, escala nativa do jogo, contorno marrom-escuro, sombreado por *hue shifting*. Não é "placeholder que precisa virar asset" — é a **referência de direção de arte**, e o ícone do mod nasce dela.

### 2.3 O que precisa ser criado (nada disto existe)

| # | Arte | Dimensão mínima | Dimensão recomendada | Onde vive | Vai no zip de release? |
|---|------|------------------|-----------------------|-----------|------------------------|
| A1 | **Ícone do mod** (`icon.png`) | **verificar na documentação atual do canal** — nenhuma fonte oficial primária acessível fixa um mínimo por canal. Princípio pragmático:SMAPI/Nexus renderizam em 32-64 px no mod-list, então **32×32 é o piso prático** e 64×64 o alvo confortável. GitHub social preview tem mínimo oficial separado (**640×320**, P5) e **não** é o mesmo arquivo. | 64×64 (ou 128×128 para reamostragem) | `.github/assets/icon.png` **ou** `docs/icon.png` | ❌ **não** — ver §8 |
| A2 | **Social preview** | **640×320** (oficial, P5) | 1280×640 | qualquer caminho de repo; **upload manual pela UI** | ❌ não |
| A3 | **Screenshot 1 — solo com overlay de saúde** | **verificar na documentação atual do canal.** Sem regra oficial encontrada. | 1920×1080 | `docs/screenshots/*.png` | ❌ não |
| A4 | **Screenshot 2 — compostar / aplicar** | idem A3 | 1920×1080 | idem | ❌ não |
| A5 | **Screenshot 3 — a bin no mundo** | idem A3 | 1920×1080 | idem | ❌ não |

⚠️ **Nenhuma figura é artwork do jogo.** Ver §4. Screenshots do jogo rodando **mostram pixels do ConcernedApe** — isso é o caso mais delicado de IP de todo este briefing, tratado em §4.2.

---

## 3. Restrição de direção de arte

### 3.1 A paleta de `Constants.cs` **não é usada por nenhum sprite atual** (achado verificado)

Census de pixel, 2026-10-05, contra `LivingRoots/Constants.cs:31-53`:

| Cor do mod | Constante | Aparece em algum dos 4 sprites? |
|-----------|-----------|-------------------------------|
| `#15803D` Healthy | `Constants.cs:33` | ❌ |
| `#166534` CompostReady | `Constants.cs:50` | ❌ |
| `#A16207` CompostProcessing | `Constants.cs:49` | ❌ |
| `#78716C` CompostEmpty | `Constants.cs:48` | ❌ |
| `#92400E` Mulch | `Constants.cs:53` | ❌ |

Os verdes que **existem** nos sprites são de outra família, não mapeados em nenhuma constante:

| Cor encontrada | Onde | Família |
|----------------|------|---------|
| `#2E7D32` | `compost.png` (6 px) | Material Green 800 |
| `#558B2F`, `#7CB342` | `composting_bin_processing.png` (17 + 7 px) | Material Light Green / Green 600/400 |
| `#4CAF50` | `composting_bin_ready.png` (2 px) | Material Green 500 |

**Consequência de direção de arte:** o ícone do mod **não deve** ser colorido com `#15803D` esperando coerência com os sprites — não há coerência para ter. Duas saídas, ambas defensáveis, e a spec precisa escolher **uma**:

- **Opção A (recomendada, cheaper):** derivar o ícone da **família já presente nos sprites** — madeira `#85572C`/`#2B1D0E`/`#A8723E`/`#5C3A1E`, contorno `#19110A`, com um único acento verde **normalizado a `#15803D`**. Barato (o acento aparece uma vez), e casa com o que o jogador vê no inventário. Custo: normalizar só o acento do ícone, não dos sprites (esses ficam para os briefings de wiring).
- **Opção B (cara):** empurrar `#15803D`/`#166534` para **todos** os 4 sprites também. Isso é retrabalho de arte em escopo de outro briefing (wiring dos sprites da bin) e **não deve** ser escondido dentro de um briefing de apresentação.

### 3.2 Regras de pixel (herdadas literalmente de `compost-icon.png.md` §3.2-3.3)

- **Shape first, color second.** A silhueta é o canal primário — para jogadoras daltônicas e para a leitura em 1×. Hue nunca pode ser o único diferenciador (`research-wcag-gaming.md` na raiz do repo é a baseline de a11y do projeto).
- **Light source top / top-left** para itens.
- **3–4 shades por hue máximo** (convenção de sprite de item SDV). O `compost.png` atual já cumpre: 5 cores opacas no total.
- **1 px outline** em marrom-escuro, **nunca preto puro** — `#19110A` / `#26180F` são a referência do repo.
- **Alpha**: interior opaco (`A=255`), exterior totalmente transparente (`A=0`), no máximo 1 px de fringe semi-transparente. Half-alpha "bloomeia" no fundo escuro do inventory e do tooltip.
- **Duas escalas de aceitação**: 1× (16 px, drop no mundo) e 4× (64×64, inventory). Para o **ícone do mod**, a escala análoga é 32 px (mod-list) e 64 px (página do canal).

### 3.3 Restrição de ícone: legível a 32 px

O ícone do mod é lido a **~32×32** numa grade de mod-list e a **64×64** numa página de canal. Isso é uma restrição *mais dura* que o sprite de 16×16 do inventário:

- Máximo **4-6 cores** totais. Menos que isso fica chapado e indistinguível de emoji na grade.
- **Silhueta única e recognition-first**. Nada de detalhe fino, nada de texto, nada de gradiente.
- O objeto reconhecível mais forte que já existe no projeto é a **pilha de composto** (`compost.png`) — montinho organulado com broto. Ícone = pilha de composto ampliada e simplificada, com a mesma direção de luz. Isso também evita ter de desenhar uma cena.
- **Teste de cego obrigatório**: reductions a 32×32 e 16×16 (nearest-neighbor), lado a lado com 10 mods vizinhos. Se não sobrevive a 16 px, é detalhe demais.

---

## 4. Licensing

### 4.1 Regra dura —pixels vanilla não entram no repositório

Reutilizada **literal** de `specs/004-playable-compost-loop/briefings/compost-icon.png.md` §3.4, para não contradizer:

> - Stardew Valley art and `springobjects.png` are **proprietary (ConcernedApe LLC)**. This repo's `AGENTS.md` already quarantines `GamePath` assemblies out of the tree for exactly this reason.
> - The new icon MUST be **original pixel art**. Reference vanilla sprites for style (pose, outline weight, shading depth) but **do not copy, trace, recolor, or vendor any vanilla pixel** into `LivingRoots/Assets/`. Do not commit extracted `springobjects` crops, `i.png`-style vanilla dumps, or "edited vanilla fertilizer" derivatives.
> - Recommended tooling: Aseprite, GIMP, Krita, or Paint.NET. Optional: keep the layered source (`.aseprite`/`.xcf`) next to the export outside the shipped `Assets/` tree or in `docs/` — never ship sources inside `Assets/` (everything under it is copied to build output).

E o Nexus concorda, textualmente (P4, rev. 2026-09-29):

> User submissions which include copyrighted content, be it game files or images, are prohibited unless proper permission has been granted by the property owner or appropriate representation. Such content may be removed from Nexus Mods websites and services and the infringing user might be subject to administrative action.

E, mais abaixo, na seção de claims:

> Content that enables or promotes illegal activity, including but not limited to software piracy and copyright infringement, may be subject to moderation. Submissions must adhere to the licences and agreements associated with the modified or featured title(s).

**Regra declarada para este repo:** nenhum asset vetorial, rasterizado ou derivado de arquivo do ConcernedApe é commitado, nem em `LivingRoots/Assets/`, nem em `docs/`, nem em `.github/`. Isto vale também para o `GamePath` (`.gitignore:24-29` + `scripts/check-protected-paths.sh` já Apply a regra para assemblies).

### 4.2 Screenshots: o caso mais delicado, e é permitido

Um screenshot **é** uma imagem do jogo, então o cláusula de IP acima precisa ser lida com precisão. A distinção que importa:

- **Proibido**: *reempacotar* conteúdo protegido como se fosse seu asset — derivar sprite, extrair crop, recolorir, "editar sprite vanilla", vendorizar `i.png`. Isso é o que §4.1 proíbe.
- **Permitido e esperado**: *captura de tela* do jogo rodando com o mod instalado, para demonstrar funcionalidade. É uso documental/nominativo de tela — o mesmo que qualquer review faz, e o que a página de qualquer mod no Nexus contém.

Contudo, três restrições mantêm isso no lado certo:

1. **Screenshot é ilustrativo, nunca distributivo.** Vai no README/canais, **nunca** dentro do zip do mod. Um arquivo de imagem do ConcernedApe dentro do pacote distribuído seria "vendoring" em sentido literal.
2. **A captura não é do autor.** Tirar na resolução nativa (1920×1080) sem upscale.
3. **Sem composição enganosa.** P4: *"File names, descriptions, images, tags, and other associated content must be legible, informative, and accurately reflect the file's nature and functionality. Wholly misleading, incoherent, or deceptive descriptions are prohibited."* Um screenshot não pode mostrar feature que não existe — ver §7.3, isso amarra diretamente no `FeatureStatusTests`.

### 4.3 Obrigação de tagueamento de IA (P4) — não é opcional

O Nexus exige tagueamento explícito do uso de IA generativa, com **três** categorias distintas. Trecho literal:

> **AI-Generated Content** — AI-Generated code, user interface elements, voices, dialogue, translations, music, in-game AI-generated assets.
> **AI Media** — AI promotional art, thumbnails, videos, mod page descriptions or media outside of the mod content in-game.
> **AI Assisted** — Developer-led code, content, or mod assets, with limited AI involvement, AI texture upscaling, AI audio upscaling/cleaning, AI concept art not in final mod content.

E: *"Failure to correctly tag mods, including undisclosed generative AI usage, may be subject to moderation action."*

O caso deste projeto é `AI Media` + `AI Assisted`: o ícone do mod e os screenshots são mídia promocional fora do conteúdo in-game, produzidos com assistência de IA. **`AI Media` é a categoria que se aplica literalmente a este briefing.** Note que "AI concept art not in final mod content" está explicitamente sob `AI Assisted` — ou seja, o conceito produzido por IA que depois é **redesenhado à mão** em pixel art é `AI Assisted`, não `AI Generated`. Isso é exatamente o caminho recomendado em §2.3/§3.

O tag `AI Assisted` exige evidência substancial (P4):

> Development history (project file history, version control commit history, other evidence of human-led design. Genuine expertise and understanding of the functionality of your code…

👉 **Ação concreta:** os commits `feat(assets)` da série a1d5305+ já são evidência de desenvolvimento human-led, mas a página do canal precisa de uma linha de changelog por arte. **Este briefing não implementa tagging em canal nenhum** — só registra a obrigação, para que a publicação não a descarte.

### 4.4 MIT cobre a arte?

`LICENSE:7` — *"software and associated documentation files"* — é ambíguo o bastante para ser um risco real: a licença MIT é a única que o repositório tem, e a página de qualquer canal vai exibir um único campo de licença. Duas saídas:

- **Recomendado:** manter **uma** licença e declarar explicitamente no `LICENSE` (ou num `LICENSE-ASSETS`) que a arte em `LivingRoots/Assets/**` e `docs/screenshots/**` está sob a mesma MIT, com a nota de que os pixels são originais. Uma arte 100% original derivada do trabalho do próprio autor **cabe** na MIT sem ambigüedad — o problema é só a *ausência* de declaração, não uma incompatibilidade.
- **Alternativa:** `CC-BY-4.0` só para a arte, para permitir reuso com crédito. Isso é **um segundo regime de licença num repo MIT** e cria trabalho de política sem benefício claro num mod solo. **YAGNI** (`CONTRIBUTING.md:10`).

**Decisão recomendada:** MIT única, com um parágrafo de escopo em `LICENSE` nomeando a arte. Barato, remove a ambiguidade, não divide a política.

### 4.5 `CONTRIBUTING.md:95` precisa de uma linha

O guia de contribuição hoje diz apenas *"try to maintain the Stardew Valley pixel art style"*. Isso é ambíguo o suficiente para convidar um contribuidor a extrair vanilla. Reescrever para: pixel art **original** no estilo do jogo; vanilla apenas como referência de estilo; nunca commitar pixels do ConcernedApe (com link para §4.1 desta seção).

---

## 5. `manifest.json` — o que o schema aceita, e a proposta final

### 5.1 Conjunto completo de chaves, verificado contra o schema oficial (P1, 2026-10-05)

O schema é **`additionalProperties: false`** — chave desconhecida é **rejeitada**. Testei cada uma:

| Chave | Schema | Notas verificadas |
|-------|--------|-------------------|
| `Name` | ✅ **obrigatória** | `Missing required fields: Name` |
| `Author` | ✅ **obrigatória** | Deve ser **String**. Array é rejeitado: `Invalid type. Expected String but got Array` |
| `Version` | ✅ **obrigatória** | Semver. Token `%ProjectVersion%` é aceito. `$version$` é **rejeitado** |
| `Description` | ✅ **obrigatória** | Sem limite de comprimento no schema (testei 200/500/1000/5000 chars — todos válidos) |
| `UniqueID` | ✅ **obrigatória** | ⚠️ **formato NÃO é validado pelo schema** — `No_Dots_Here` passou sem erro. A regra `author.modId` é em runtime |
| `EntryDll` | ✅ **obrigatória OU** `ContentPackFor` | `Missing required fields: EntryDll or ContentPackFor` |
| `ContentPackFor` | alternativa | **Objeto único, não array** (`Invalid type. Expected Object but got Array`) e **mutuamente exclusivo** com `EntryDll`: `Can't specify both EntryDll and ContentPackFor, they're mutually exclusive` |
| `MinimumApiVersion` | ✅ **opcional no schema** | Semver validado quando presente |
| `MinimumGameVersion` | ✅ **opcional** | Semver validado quando presente |
| `Dependencies` | ✅ **opcional** | Array de objetos; `UniqueID` **obrigatório** dentro, `Version` e `IsRequired` opcionais |
| `UpdateKeys` | ✅ **opcional** | **Array de strings** (`["Nexus:1234"]`). Forma de objeto é rejeitada: `Expected String but got Object` |

**Chaves que eu testei e o schema REJEITA explicitamente** — ninguém deve caçar nenhuma delas:

| Chave rejeitada | Erro |
|------------------|-------|
| **`Icon`** | `Property 'Icon' has not been defined and the schema does not allow additional properties.` |
| `Website` | idem |
| `Authors` (array) | idem |
| `GameVersion` | idem |
| `LoadPriority` | idem |
| `Screenshots` | idem |
| `Content Patches` / `ContentPatches` | idem |

> **`Icon` não é chave de manifest. Ponto encerrado.** A busca foi feita pelo endpoint oficial do SMAPI, não por leitura de docs. O briefing pai estava certo em desconfiar.

### 5.2 E onde o ícone vai, então?

O manifest **não tem** slot de ícone, e o mod-list oficial do SMAPI (P2, 4740 mods) **não tem** campo de ícone. Logo o ícone é arte de **canal**, não de **runtime**: vive no repositório ou no painel do canal, e é referenciado por URL relativa no README e nas release notes.

### 5.3 Comportamento em falta/inválido

- Chave faltando → o schema reporta `Missing required fields: X`. **Na prática o SMAPI não faz hard-fail de carregamento por isso** — ele registra um erro de parse no console e **ignora o mod**. `Verify` no log do SMAPI na máquina de teste antes de afirmar esse comportamento.
- Chave inválida (semver malformada) → `Invalid semantic version; must be formatted like 1.2.0 or 1.2.0-prerelease.tags`.
- Chave desconhecida → rejeitada pelo schema, mas **ignorada em runtime** pelo loader. Ou seja: um `Icon` no manifest é inerte, não um crash. Ninguém deve "consertar" adicionando essa chave.

### 5.4 `manifest.json` final proposto

Mudanças mínimas e todas **verificadas válidas** contra o schema (P1):

```json
{
  "Name": "Stardew Valley - Living Roots",
  "Author": "lfgranja",
  "Version": "0.0.1",
  "Description": "Agroecological Expansion Pack for Stardew Valley",
  "UniqueID": "lfgranja.LivingRoots",
  "EntryDll": "LivingRoots.dll",
  "MinimumApiVersion": "4.0.0",
  "MinimumGameVersion": "1.6.0",
  "UpdateKeys": [],
  "Dependencies": [
    {
      "UniqueID": "spacechase0.SMAPI",
      "MinimumApiVersion": "4.0.0"
    }
  ]
}
```

⚠️ **`Dependencies[].MinimumApiVersion` NÃO foi verificado.** Testei `Version` (válido) e omitir tudo menos `UniqueID` (válido); **não testei `MinimumApiVersion` dentro de um dependency**. O token seguro, comprovado, é `Version`. A spec deve validar a forma final contra o schema antes de commitar:

- `MinimumGameVersion: "1.6.0"` — ✅ validado. Justificativa: o mod usa `Data/Machines` com chaves `(BC)` e `Data/Objects` com `Texture`/`SpriteIndex`, ambos 1.6-only (§1.6 do `compost-icon.png.md`). Declarar isso é honesto e evita relatório de bug confuso.
- `Dependencies` com `spacechase0.SMAPI` — ✅ validado. `IsRequired` opcional dentro (validado).
- `UpdateKeys: []` — **array vazio é válido mas inútil** até existir página no canal. Deixar de fora é mais limpo; foi incluido acima só para mostrar a forma correta (`["Nexus:<id>"]`, string, quando o ID existir). **Recomendação: omitir.**
- **Nenhuma chave `Dependencies` para Content Patcher.** O mod é C# puro; CP é fallback do spike e não é dependência (`compost-icon.png.md` §1.5).
- **Nenhum `Website`** — o schema não aceita. O link para o repositório vai no README e nas release notes do GitHub, que é onde o mod-list já busca (`GitHubRepo` existe no mod-list, P2).

### 5.5 `MinimumApiVersion` vs `GameVersion`

| Chave | Schema | Significado |
|-------|--------|-------------|
| `MinimumApiVersion` | opcional | Versão mínima do **SMAPI** necessária. Este é o que o mod controla. |
| `MinimumGameVersion` | opcional | Versão mínima do **jogo**. |
| `GameVersion` | ❌ **rejeitada** | Não existe. Quem escreve `GameVersion` está confundindo. |

`MinimumApiVersion` ausente é *aceito pelo schema*, mas **não se deve remover**: SMAPI usa esse valor para decidir o modelo de compatibilidade de API e para falhar cedo com mensagem clara. `4.0.0` está correto e casa com o `MinimumApiVersion` do build (`Pathoschild.Stardew.ModBuildConfig` 4.4.0 exige SMAPI 4.1.0+; a versão do assembly do toolkit local pode ser verificada com `strings`). **Verificar na documentação atual do SMAPI** qual é o piso real para a API usada antes de subir esse número.

---

## 6. i18n de metadados user-facing

### 6.1 O que já existe

`LivingRoots/i18n/default.json` (12 chaves) e `en.json` (idêntico). **Todas** são HUD/tooltip em-jogo:

```
compost.name · compost.description · composting-bin.name · composting-bin.description
floating-text · restoration-text · maturity-tooltip
message.compost.{invalid-tile,max-health,action-conflict} · message.harvest.inventory-full
```

O schema de i18n do SMAPI está disponível no mesmo endpoint (`SchemaName=i18n`) e pode ser usado como gate.

### 6.2 A regra que este briefing propõe

**Metadados de apresentação NÃO são i18n do SMAPI.** `manifest.json` é lido pelo SMAPI **antes** de qualquer infraestrutura de tradução — é parse de boot, não asset de conteúdo. Logo:

| Texto | Onde vive | i18n? |
|-------|-----------|--------|
| `Name` / `Description` do manifest | `manifest.json` | ❌ **não** — um valor, boot-time, sem locale |
| Título/descrição de release | Release notes do GitHub | ❌ não — texto de canal |
| README | `README.md` | ❌ não |
| Texto de descrição de página (Nexus/SMVCR) | página do canal | ❌ não — **cada canal tem seu próprio campo** |
| Nome e descrição de item/bin | `i18n/*.json` | ✅ já feito |
| Tooltip e mensagens de HUD | `i18n/*.json` | ✅ já feito |

A implicação prática: **cada canal carrega o mesmo texto uma vez**, em inglês, duplicado em README + release notes + página do canal. Isso é uma divergência que vai mofar. Duas mitigações:

1. **Fonte única declarada**: `docs/presentation/marketing.md` guarda o texto canônico de nome/tagline/descrição curta; README e release notes referenciam ("texto de release copiado de `docs/presentation/marketing.md`"). Um arquivo, três destinos.
2. **`manifest.json` gerado** do arquivo canônico por um passo de build. **Não fazer** — YAGNI, e SMAPI lê o manifest literalmente; introduces um token de versão no meio.

### 6.3 Sem tradução do texto promocional

`i18n/` do SMAPI é para conteúdo in-jogo. O texto de README e de canal é localizado por **fork do README** ou pela página traduzida do canal — nunca por `i18n/*.json`. Fora de escopo (§11).

---

## 7. Plano de imagens no README

### 7.1 O que o README tem hoje

47 linhas, zero imagem, zero badge. `README.md:27` linka para GitHub Releases. `README.md:45-47` declara MIT.

### 7.2 Layout proposto

```markdown
# Living Roots
![Living Roots icon](docs/icon.png)          ← novo: ícone, ~64px, lado do H1

<p align="center">
  <img src="docs/screenshots/soil-health.png" width="49%">
  <img src="docs/screenshots/composting.png"  width="49%">
</p>

> Short description

## Features
## Screenshots                    ← novo, opcional: legendas por imagem
## Installation
```

Regras de imagem no README:

- **Caminho relativo**, nunca URL absoluta nem badge de imagem — o README é renderizado no GitHub, em mirrors de terceiros e no feed Atom.
- **`<img>` lado a lado com `width="49%"`** para screenshots: o grid do GitHub é fluido e dois PNGs de 1920 px empurram a coluna.
- **HTML inline** (`<p align="center">`) para o par; **Markdown** para o ícone, porque é o único que precisa aparecer no Atom/RSS.
- **Alt text descritivo**, nunca o nome do arquivo. Acessibilidade: `README.md` renderizado no GitHub tem leitor de tela, e "screenshot1.png" não diz nada.
- **Badge** (opcional, §7.4): build/SMAPI version badges vêm de `shields.io`. ⚠️ isso adiciona um terceiro party e um request externo em cada render — decidir explicitamente, não por omissão.

### 7.3 Marketing que o README não pode fazer

P4 é explícito: *"Claims about a mod's effects or capabilities … must be truthful and supported by reasonable evidence or clear technical explanation. Unsubstantiated or exaggerated claims are not permitted."*

`README.md:10-12` já marca três features como **`(Planned)`** — e essas três strings são literalmente assertadas por `FeatureStatusTests.cs:19-21`. **Um screenshot só pode mostrar o que já funciona.** Nada de "screenshot showing soil health decay" enquanto `README.md:11` diz `(Planned)`. Isto amarra §4.2 (o screenshot não pode enganar) a §9 (o teste que garante as claims). Se um screenshot depura uma feature `(Planned)`, é necessário inverter o `(Planned)` **e** o teste — nessa ordem, com o teste vermelho primeiro.

Features que são verdadeiras hoje (ver `LivingRoots/i18n/default.json` + `ModEntry.cs`): item de compost com ícone, receita da bin, ciclo de 2 dias com 3 estados, aplicação de +15, refusal messages. **Esse é o conjunto de screenshot honesto.**

### 7.4 Badge — recomendação

**Não adicionar badge agora.** Motivo: os três workflows do repo não produzem status, arte de release nem nada que um badge comunicaria (ver §8). Um badge verde de "build" seria verde-falso — exatamente o problema que `.specify/bugs/ci-skipped-job-false-green/` já registra. Sejam considerados **depois** que `Build and Test` tenha uma run genuinamente verde.

---

## 8. Notas de wiring de CI / release — o que precisa mudar para o ícone realmente subir

### 8.1 O que acontece hoje

`Pathoschild.Stardew.ModBuildConfig` **já** faz o empacotamento. `Pathoschild.Stardew.ModBuildConfig.targets:23,26` define `ModZipPath` = `$(TargetDir)` e `EnableModZip` default **true**, e o `DeployModTask` roda em `AfterBuild` (`targets:86-105`). O artefato existe:

```
LivingRoots/bin/LivingRoots 0.0.1.zip     (91775 B, 10 arquivos)
```

Conteúdo real (verificado):

```
LivingRoots/manifest.json
LivingRoots/i18n/default.json
LivingRoots/i18n/en.json
LivingRoots/LivingRoots.dll
LivingRoots/Assets/compost.png
LivingRoots/Assets/composting_bin_{empty,processing,ready}.png
LivingRoots/Assets/Audio/composting_bin_{processing,ready}.ogg
```

O nome do zip (`LivingRoots 0.0.1.zip`) já sai de `ModFolderName` + a versão do manifest — **então subir a versão no manifest já renomeia o zip**. Nenhuma config extra é necessária para isso. `bin/manifest.json` é byte-idêntico ao da fonte (diff limpo, 263 B).

### 8.2 Um ícone na raiz seria excluído — e essa é a decisão certa

`LivingRoots.csproj:19-28` tem exatamente três regras `Content`, todas com `PreserveNewest`, e **só três**:

| Regra | Cobre |
|-------|-------|
| `manifest.json` | o manifest |
| `i18n\**\*.json` | traduções |
| `Assets\**\*.*` | sprites + áudio |

Um `LivingRoots/icon.png` **não** seria copiado para `bin/` e **não** entraria no zip — nem por acidente, nem por omissão. E isso é exatamente o que se quer:

1. **O zip é o mod em runtime.** Ele não precisa de ícone — SMAPI não lê ícone de manifest (§5.2) e o mod-list não tem campo de ícone (P2). Encheria o pacote com bytes que o jogo nunca lê.
2. **O ícone é arte de canal**, então pertence fora da pasta do mod.

➡️ **O ícone vive em `.github/assets/icon.png` (ou `docs/icon.png`), e é referenciado por caminho relativo no README.** Nenhuma mudança no `.csproj`. Isso é uma não-ação deliberada e deve ser registrada como tal na spec, senão alguém "conserta" o csproj depois.

### 8.3 O que CI não faz hoje (e precisa)

| # | Lacuna | Arquivo | Correção |
|---|--------|---------|----------|
| W1 | Nenhum workflow cria release | os 3 workflows só: format, build+test, protected paths | workflow `release.yml` em `on: push: tags: v*` |
| W2 | Nenhum workflow faz upload de artefacto | `build_and_test.yml:123-128` sobe **só** `coverage-opencover` | sobe `LivingRoots/bin/*.zip` como `actions/upload-artifact` |
| W3 | O zip **não** é commitável | `.gitignore:50` → `*.zip` | ✅ **correto, não mexer** — o zip é artefato de build |
| W4 | Build só roda se `BUILDDEPS_ENABLED` | `build_and_test.yml:53` | ⚠️ o release **herda essa dormência**. Um `release.yml` próprio tem as mesmas restrições de `GamePath`. Ver §8.4 |
| W5 | `paths-ignore` bloqueia doc-only | `build_and_test.yml:8-16` | ok como está; mas se algum check virar required, apagar o filtro primeiro (o próprio header do arquivo avisa, linhas 46-48) |
| W6 | Social preview não é ficheiro | GitHub Docs (P5) | upload manual via Settings. **Nenhum workflow faz isso.** Registrar na checklist de release, não no CI |

### 8.4 O problema real do release: `GamePath` é obrigatório

Este é o bloqueio duro, não uma nota de rodapé. `Pathoschild.Stardew.ModBuildConfig.targets:74-76` emite `<Error>` (não warning) sem `Stardew Valley.dll` e `StardewModdingAPI.dll` no `GamePath`. Então:

- **Um `release.yml` não pode simplesmente rodar `dotnet build`.** Precisa do mesmo preflight de 4 passos de `build_and_test.yml:71-109` (fetch do `gamepath.zip` do repo privado + preflight de 6 assemblies + a relocação de `SMAPI.Toolkit.CoreInterfaces.dll` para `smapi-internal/`, que o `.gitignore:24-29` e `AGENTS.md` já documentam como o passo fácil de errar).
- **Isso significa que o release herda o mesmo segredo e a mesma flag `vars.BUILDDEPS_ENABLED`.** Ou o `release.yml` duplica o preflight (código duplicado, rejeitável), ou **se casa com `build_and_test.yml` num workflow só** e adiciona um job de release sob a mesma `if`. **Recomendação:** a segunda. Um workflow, um preflight, sem duplicação — e consistente com o princípio do próprio repo de que um nome de workflow deve declarar o seu âmbito inteiro (`code-style.yml:3-6`).
- **Release manual é aceitável e talvez preferível** para um mod solo. Um `dotnet build && gh release create` na máquina do maintainer elimina 40 linhas de workflow e todo o risco de vazar `GamePath`. Registrar essa como a opção A/YAGNI e o workflow completo como a opção B/escala.

### 8.5 `scripts/check-protected-paths.sh` e arte nova

O script verifica duas listas: `PROTECTED` (caminhos que **devem** estar ignorados, linhas 25-36) e `BANNED_TRACKED` (nomes que **não** podem estar rastreados, linhas 41-48). Arte nova **não precisa** ser adicionada a nenhuma das duas — não há nada sensível em um PNG original.

⚠️ **Mas:** o próprio header do script é honesto sobre o limite — *"Neither check can catch a sensitive file type nobody thought to list."* Um PNG com um crop de vanilla dentro **passaria** ambos os checks. É por isso que §4.1 é uma regra de revisão humana, não um gate automatizado. **Não vender `protected-paths.yml` como se cobrisse arte.**

---

## 9. Testes / gates

### 9.1 O que já quebra e o que não

| Gate | Quebra com imagem nova? | Quebra com **texto** novo? |
|------|--------------------------|-----------------------------|
| `FeatureStatusTests.cs` (README como embedded resource) | ❌ **não** — só faz `Assert.Contains` em 3 strings `"(Planned)"` (linhas 17-21, 39-42) | ⚠️ **sim, se apagar/reescrever** `"Visual indicators show soil health status (Planned)"`, `"Health degrades over time when soil is left bare (Planned)"`, `"Health improves with compost application (Planned)"` |
| `ArchitecturalPlanTests.cs` | ❌ não | ⚠️ só para `.md` sob `LivingRoots/docs/architectural_and_refactor_plans/**` — exige newline final |
| `dotnet format --verify-no-changes` | ❌ não | ❌ não (é C#/whitespace) |

⚠️ **Pegadinha que já mordeu antes:** `--no-build` lê a cópia **embedded obsoleta** do README. Mudar o README exige **rebuild**, senão o teste valida o texto antigo e dá um falso-verde. (`compost-icon.png.md` §4.2 registra isso.)

### 9.2 Novos testes que este briefing exige

1. **Schema do manifest** (o mais valuable). Postar `LivingRoots/manifest.json` ao endpoint público `https://smapi.io/json` com `SchemaName=manifest` e afirmar que volta `"No errors found!"`. Vira um gate de release e **torna impossível** alguém reintroduzir `Icon`. ⚠️ É rede — decidir se isso é aceitável no gate (ver 9.3).
2. **Gate offline equivalente** (preferido para `dotnet test`): `SMAPI.Toolkit.dll` está **no disco**, em `$(NuGetPackageRoot)pathoschild.stardew.modbuildconfig/4.4.0/build/`, e `LivingRoots.Tests.csproj:23-34` **já copia** os assemblies do SMAPI para o output de teste. Se `SMAPI.Toolkit` expõe um leitor de manifest utilizável (tem `ManifestHelper`, `ManifestValidator`, `TryLoadManifest`, `IsModManifest` nos metadados), o teste pode validar `manifest.json` **sem rede**, no mesmo padrão que `LivingRoots.Tests.csproj` já usa. **Verificar a API pública do toolkit antes de prometer isto.**
3. **Guard de imagem nova** (se os assets forem versionados no repo): assertar que cada PNG sob `docs/` e `.github/` tem dimensões e formato válidos — mesma técnica de parse do chunk IHDR descrita em `compost-icon.png.md` §4.2 (item 3), que é dependency-free e não usa `System.Drawing.Common`. Aplicar os critérios de §3.3: ícone ≥32×32, RGBA colortype 6, non-interlaced.
4. **Trava de anti-vendorização**: um teste que falhe se qualquer PNG commitado no repo tiver um hash conhecido de arquivo vanilla do jogo. ✅ **Não implementar.** Não temos os hashes (não podemos ter os arquivos!), a checagem seria teatro, e daria falsa confiança. A regra é humana e de revisão (§4.1). Registrado para que ninguém tente.
5. **README** — nada novo a adicionar. As três strings `(Planned)` são o contrato existente e continua válido.

### 9.3 Ordem de gate

```
dotnet format Stardew-LivingRoots.sln --verify-no-changes   # sempre
GamePath=$GP dotnet build Stardew-LivingRoots.sln -c Release
GamePath=$GP dotnet test --no-build -c Release              # baseline 816
```

Schema do manifest: **antes** do build (falha mais cedo e mais barata). Se ficar offline (9.2 item 2), roda como parte de `dotnet test` sem passo novo.

---

## 10. Riscos

| # | Risco | Prob. | Impacto | Mitigação |
|---|-------|-------|---------|-----------|
| R1 | **Screenshot mostra feature `(Planned)`** → viola o Nexus ("must accurately reflect the file's nature"), e o `FeatureStatusTests` fica paradoxalmente correto | alta (é tentador) | **reputacional** | §7.3 — o conjunto de screenshot honesto é fixado aqui; screenshot novo exige feature verificada primeiro |
| R2 | **Ícone em pixel art 32×32 ilegível** | média | baixa | §3.3 — teste de cego a 32/16 px antes de commit |
| R3 | **Alguém "conserta" o csproj para copiar o ícone para o zip** | média | baixa (peso morto) | §8.2 é uma não-ação **documentada** — registrar na spec como decisão, não como omissão |
| R4 | **Ninguém tagueia IA no canal** → mod removido ou moderado | média | **alto** | §4.3 — obrigação declarada; tag `AI Media` para ícone/screenshots |
| R5 | **`Icon` volta ao manifest** porque alguém "sabe que mod usa ícone" | baixa | **médio** (inerte em runtime, mas quebra o gate de schema e taughta) | §5.1 + teste 9.2 item 1 |
| R6 | **Release não roda** porque `GamePath` é obrigatório e o preflight não está lá | **quase certa** se o `release.yml` for ingenuo | bloqueante | §8.4 — ou release manual, ou um workflow com o preflight |
| R7 | **Screenshot com pixel do ConcernedApe é commitado** em `docs/` | média | **alto** (repo público) | §4.2 — mas note: `protected-paths.yml` **não** pega isso (§8.5). Revisão humana obrigatória |
| R8 | **Deriva de paleta**: ícone em `#15803D` enquanto sprites estão em `#558B2F` | média | baixa | §3.1 — escolher Opção A ou B **na spec**, não deixar implícito |
| R9 | **Texto de marketing diverge** entre README, release notes e página do canal | **alta** (3 destinos, zero sync) | média | §6.2 — `docs/presentation/marketing.md` como fonte única |
| R10 | Ícone **só 16×16** (reaproveitando `compost.png` direto) | média | baixa | §3.3 — 32 px é o piso prático; a 16 px some na grade |
| R11 | `manifest.json` sem newline final | **já está assim** | cosmético | §9 — adicionar no mesmo toque |
| R12 | Adicionar badge de "build" que dá **verde falso** (build adormecido) | média | **médio** — recorre a `.specify/bugs/ci-skipped-job-false-green/` | §7.4 — sem badge até `Build and Test` ter run verde genuína |

---

## 11. Fora de escopo (explorados e descartados — não briefar)

Seguindo a convenção de `specs/003-composting-tests/briefings/README.md:18-28` ("Not assets — do not brief these"):

| Coisa | Por que **não** é um asset |
|-------|---------------------------|
| Wiring dos 4 PNGs existentes (`compost.png`, `composting_bin_*.png`) | Já tem briefing: `specs/004-playable-compost-loop/briefings/compost-icon.png.md` (T022). Este briefing não toca `ModEntry.cs` |
| Os 2 `.ogg` stub (24 B, payload zero) | Pertencem ao briefing de wiring de áudio/máquina. Não têm relação com apresentação |
| `Data/Objects` `Texture`/`SpriteIndex` | Idem — escopo do 004 |
| `Category = -26` vs `-19` | Decisão de item-registration, não de apresentação (`compost-icon.png.md` §1.4) |
| Overlay de solo, padrão de stripes/dots, tooltips | Desenhados em runtime (`SpriteBatch.Draw`); sem arquivo. Mesmo critério do briefing 003 |
| `i18n` de HUD/tooltip | 12 chaves já existem e já estão wired |
| **Trailer animado / vídeo** | Fora de escopo. Além disso, cai na categoria `AI Media` do Nexus e multiplica o custo de produção |
| **Voice-over / áudio de apresentação** | Fora de escopo; `AI-Generated Content` no Nexus se fosse gerado |
| **Tradução do texto promocional** | §6.3 — o i18n do SMAPI é para conteúdo in-jogo. Fork de README ou página traduzida do canal |
| **`content.json` / Content Patcher** | O mod é C# puro; CP é fallback do spike (`compost-icon.png.md` §1.5, §5 item 3) |
| **Sprite da `Assets/Icon.png` dentro da pasta do mod** | §8.2 — não seria lido por nada, só peso no zip |
| **Trocar `LICENSE` por CC-BY para arte** | §4.4 — cria dois regimes de licencia num repo MIT por ganho zero |
| **Badge de build** | §7.4 — verde falso |
| **Automação de upload de release** | §8.4 — alternativas registry; decidir na spec com a trade-off explícita |

---

## 12. Ordem de execução sugerida (derivada, não normativa)

1. Escolher Opção A ou B de §3.1 e registrar na spec.
2. Criar `docs/icon.png` (64×64, §3.2/§3.3) + teste de legibilidade a 32/16 px.
3. Declarar arte no `LICENSE` (§4.4) e reescrever `CONTRIBUTING.md:95` (§4.5).
4. Aplicar o `manifest.json` proposto (§5.4) **após** validar a forma de `Dependencies[].MinimumApiVersion` contra o schema.
5. Adicionar newline final no `manifest.json` (§9, G9).
6. Tirar screenshots **só** de feature verificada (§7.3).
7. Reorganizar o README (§7.2).
8. Guard de schema do manifest como gate (§9.2, item 1 ou 2).
9. Só então: mechanics de release — **release manual** (YAGNI) ou workflow com preflight (§8.4).
10. Subir social preview pela UI (§8.5 W6).
11. Publicar no canal com o tagueamento `AI Media` correto (§4.3).

---

## Fontes

**Primárias (revisadas 2026-10-05):**

- SMAPI — validador JSON com schema oficial do manifest: `POST https://smapi.io/json` (`SchemaName=manifest`). Verificado por 20+ submissões de teste. URL `https://smapi.io/docs/config/manifest` do briefing pai está **404** (P6).
- SMAPI — mod-list de compatibilidade (dados abertos): `https://smapi.io/mods` → blob `smapistorage.blob.core.windows.net/smapi-web-temp/uploads/mod-list`. 4740 mods lidos; união de chaves sem campo de ícone.
- SMAPI — home, confirma que a doc de manifest migrou para o wiki: `https://smapi.io/`.
- `Pathoschild.Stardew.ModBuildConfig` 4.4.0 — `build/Pathoschild.Stardew.ModBuildConfig.targets` e `build/SMAPI.ModBuildConfig.dll` (nuspec pin: commit SMAPI `8bbbf08b95b4287799ad0285166f3b1c93d2c808`).
- Nexus Mods — [File Submission Guidelines](https://help.nexusmods.com/article/28-file-submission-guidelines) (rev. **2026-09-29**): IP de terceiros, tagueamento de IA, claims verdadeiras, imagens relevantes.
- GitHub Docs — [Customizing your repository's social media preview](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/customizing-your-repositorys-social-media-preview) (rev. 2026-10-05): upload por UI; PNG/JPG/GIF < 1 MB; ≥640×320, 1280×640 ideal.

**Internas:**

- `LivingRoots/manifest.json:1-9` · `LivingRoots/LivingRoots.csproj:19-29` · `LivingRoots/Constants.cs:31-53`
- `LivingRoots.Tests/LivingRoots.Tests.csproj:10-12,19-21,23-34,51` · `LivingRoots.Tests/FeatureStatusTests.cs:17-21,39-42`
- `LivingRoots/i18n/default.json:1-13` · `LivingRoots/Assets/**` (census de pixel por inflate puro de IDAT, 2026-10-05) · `LivingRoots/bin/LivingRoots 0.0.1.zip` (listing)
- `.github/workflows/{build_and_test,code-style,protected-paths}.yml` · `.gitignore:24-29,50` · `scripts/check-protected-paths.sh:25-48`
- `README.md:1-47` · `CONTRIBUTING.md:95` · `LICENSE:1-21`
- `specs/004-playable-compost-loop/briefings/compost-icon.png.md:193-197` (licensing/PAL, reutilizado literal), `:1-6`, `:155-198`, `:201-232`
- `specs/003-composting-tests/briefings/README.md:18-28` (convenção asset vs não-asset)
- commit `a1d5305` — `feat(assets): create authentic Stardew pixel art sprites for compost and composting bin`

---

Última verificação: **2026-10-05** — schema do manifest, conjunto completo de chaves e rejeição de `Icon`/`Website`/`Authors`/`GameVersion`/`LoadPriority`/`Screenshots`/`ContentPatches` confirmados por probe no endpoint oficial do SMAPI; mod-list lido integralmente (4740 mods, sem campo de ícone, LivingRoots ausente); guidelines do Nexus lidas (rev. 2026-09-29); docs do GitHub lidos; arte e packaging verificados byte a byte no working tree. **Não confirmado, marcado como "verificar na documentação atual do canal":** dimensões mínimas de ícone e de screenshot exigidas por SMVCR e por Nexus Mods — nenhuma fonte oficial acessível fixa um número por canal. **SMVCR:** requisitos de submissão não verificados (site inacessível a partir deste ambiente) — tratar como pendente antes de publicar.
