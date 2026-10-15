# Briefing — GMCM (Generic Mod Config Menu): páginas de configuração e `config.json`

> **Status:** briefing (não implementado). Adiado para `/speckit.specify`. Este briefing enumera **todos** os assets que o GMCM impõe ao mod, não especifica a feature.
> **Citações-âncora:** `specs/future-roadmap/briefings/rotation-journal.md:48` · `specs/future-roadmap/briefings/mobile-coop.md:54` · `specs/future-roadmap/briefings/insect-hotels-beehive-aura.md` · `specs/future-roadmap/briefings/living-pharmacy-tonics-sprays.md` · `specs/future-roadmap/briefings/adobe-shed-green-roof.md` · `specs/future-roadmap/briefings/green-manure.md` · `specs/001-soil-health-visualization/briefings/overlay-procedural.md:436` · `research-report-ui-overlays-tooltips.md:569-594` · `.specify/memory/constitution.md:28-37` · `specs/001/spec.md:119-121,148-154,306` · `checklists/configuration.md:14-16` (CHK001/CHK002/CHK010/CHK011/CHK013/CHK014, GMCM)
> **Por que este briefing existe:** **zero** briefings no repo cobrem `config.json`, mas **seis** briefings exigem páginas GMCM. O briefing também resolve a contradição não documentada: `spec.md` FR-004/005/006 pede config global, `tasks.md:306` grava por save. Documenta o que precisa mudar para o ícone realmente subir — check se `Assets/**` vai para output e se um ícone na raiz seria excluído.
> **Não promover para `specs/` sem `/speckit.specify`.**

---

## 0. Sumário executivo (TL;DR)

O mod hoje **não tem** `config.json` nem página de configuração, mas **seis** briefings do roadmap exigem páginas GMCM. Recomendação: GMCM como dependência **opcional** (`IsRequired: false`, UniqueID `spacechase0.GenericModConfigMenu`, versão-alvo **1.16.0**), registro em `GameLaunched` com fallback silencioso, e config **global `config.json`** via SMAPI Config API — não persistência por save — porque os campos são preferências por jogador, não estado da fazenda (Principle VI). O que trava: a contradição não documentada `spec.md` FR-004/005/006 (arquivo JSON global) vs `tasks.md:306` (chave `visualization_config` por save) precisa de adjudicação em `/speckit.specify` antes de qualquer código; sem ela, nenhum briefing consumidor pode congelar sua lista de campos.

## 1. Escopo e tese

Este briefing mapeia **todos** os artefatos que o GMCM impõe ao mod — `manifest.json`, `config.json`, interface `IGenericModConfigMenuApi`, chaves i18n, comportamento sem GMCM e regras multiplayer — para que os seis briefings-consumidoras citem uma única fonte canônica. Não é especificação de feature: cada campo de gameplay aqui listado só existe após seu próprio `/speckit.specify`. Tese: GMCM é a **única** camada de configuração do mod; não há segunda UI de config (YAGNI, Constitution V).

## 2. Fontes primárias com data de revisão

### Externas

| Fonte | Identificação | Lida em |
|---|---|---|
| Stardew Modding Wiki — *Generic Mod Config Menu/Reference* | **oldid 6502**, última edição 2025-09-13 18:46:21 UTC por AtlasVBot | 2026-10-05 — ⚠️ página "under construction" que documenta a API **legada** (pré-1.5.0: `RegisterModConfig`/`RegisterSimpleOption`/`RegisterClampedOption`). Não copiar como fonte da API atual |
| `IGenericModConfigMenuApi.cs` — `spacechase0/StardewValleyMods` (branch `develop`) | interface canônica moderna: `Register`, `AddSectionTitle`, `AddSubHeader`, `AddParagraph`, `AddImage`, `AddBoolOption`, `AddNumberOption` (sobrecargas `int`/`float`), `AddTextOption`, `AddKeybind`, `AddKeybindList`, `AddPage`, `AddPageLink`, `AddComplexOptionWithGamepadSupport`, `SetTitleScreenOnlyForNextOptions`, `OnFieldChanged`, `OpenModMenu`, `OpenModMenuAsChildMenu`, `TryGetCurrentMenu`, `Unregister`. API estável desde o redesenho 1.5.0 | 2026-10-05 |
| *release-notes.md* — `spacechase0/StardewValleyMods` | histórico no repo vai só até 1.9.1; releases 1.14.x/1.16.x são publicados no Nexus sem push ao `master` | 2026-10-05 |
| Nexus Mods — Generic Mod Config Menu (mod 5098) | versão estável mais recente conhecida: **1.16.0** (publicada 2025-12-20, verificada contra SV 1.6.15; corroborada pela tradução japonesa "(1.16.0)" e pelo mirror stardewmods.ru) | 2026-10-05 |
| Stardew Modding Wiki — *Tutorial: How to create a manifest* | semântica de `Dependencies`/`IsRequired`: `true` **impede o carregamento** do mod se a dependência faltar; `false` = opcional | 2026-10-05 |
| SMAPI docs — *Modding:Modder Guide/APIs/Config* | `config.json` é criado automaticamente pelo SMAPI na primeira chamada a `helper.ReadConfig<T>()`; **não há schema obrigatório** — os campos vêm do POCO do mod | 2026-10-05 |
| stardewvalleywiki.com — *Modding:GMCM* | página do wiki principal; **não acessível ao fetcher nesta sessão** (host retorna 404) — oldid não verificado, conferir manualmente antes de citar | 2026-10-05 |
| wiki.gg — *List of Triggers and Trigger Actions added by Mod Frameworks* | GMCM expõe o trigger action `spacechase0.GenericModConfigMenu_OpenModConfig <modID>` (abre o menu de config de um mod via console SMAPI) | 2026-10-05 |

### Internas

| Fonte | O que foi lido |
|---|---|
| `specs/001-soil-health-visualization/checklists/configuration.md` (full) | **CHK001** (localização/API de persistência explicitamente especificadas) e **CHK002** (escopo de "global" = mod-wide, compartilhado entre fazendas de um jogador) — ambos `[x]`; **CHK010** (validação também em save-time; RESOLVED: FR-006 manda substituir inválidos antes de persistir); **CHK011/CHK014** (defaults de todos os campos, `HudAnchorOffset` em coordenadas exatas); **CHK013** (arquivo ausente → defaults completos) |
| `LivingRoots/Domain/Visualization/VisualizationConfiguration.cs` | 8 campos: `OverlaysEnabled`, `TooltipsEnabled`, `HoeFeedbackEnabled` (bools, default `true`), `Opacity` (float 0.0–1.0, default 0.5), `PoorColor`/`ModerateColor`/`HealthyColor`/`UnknownColor` (`ColorDTO`), `ShowPatterns` (bool, default `true`), `AccessibilityDegradation` (string? `auto`/`never`/`notify`, default `auto`) |
| `LivingRoots/Services/Visualization/VisualizationConfigurationService.cs` | persistência **por save** via `IModDataService`, chave `visualization_config_<saveId>` com sanitização; `Validate` substitui só campos inválidos por defaults (FR-006); logs genéricos sem exceção bruta |
| `LivingRoots/ModEntry.cs:28-82` | `Entry(IModHelper helper)` usa `helper` para eventos e `ModDataService`, mas **não retém** o helper em campo — `_controller` é o único estado de instância. Registro GMCM tem de ocorrer dentro de `Entry` (ou o helper precisa virar campo, o que é decisão de design) |
| `specs/001-soil-health-visualization/spec.md:119-121,146-153` | FR-004 (persistir config via arquivo JSON), FR-005 (carregar na inicialização + hot-reload), FR-006 (fallback parcial, forward-compat); entidades `Visualization Configuration` e `ColorDTO` |
| `specs/001-soil-health-visualization/tasks.md:306` | "Configuration key: `visualization_config` via IModDataService" — a meia-verdade de implementação que contradiz FR-004 (ver §5) |
| `.specify/memory/constitution.md:28-37` | Principle VI: estado da fazenda é compartilhado e host-authoritativo; clientes **não** fazem fork; persistência só via save do SV; divergência precisa de regra explícita |
| `LivingRoots/manifest.json` | sem `Dependencies`, `MinimumApiVersion: 4.0.0`, sem `ContentPackFor` |
| `LivingRoots/i18n/default.json` | 12 linhas, só chaves de compost (`compost.name`, `floating-text`, …) — **zero** chaves de configuração |
| `LivingRoots/LivingRoots.csproj:18-29` | copia `manifest.json`, `i18n/**/*.json` e `Assets/**/*.*` para o output (`PreserveNewest`) — **não** copia `config.json` (que não existe no repo) |
| `LivingRoots/Constants.cs:31-45` | defaults canônicos: `PoorColor #B91C1C`, `ModerateColor #D97706`, `HealthyColor #15803D`, `UnknownColor #6B7280`, `DefaultOpacity 0.5f`, `PatternMinOpacity 0.7f`, `*EnabledDefault = true` |

## 3. Seis briefings-consumidoras e suas páginas GMCM

| # | Briefing | Página GMCM solicitada | Campos reais (citados no briefing) |
|---|---|---|---|
| 1 | `rotation-journal.md` | Página "Rotation" (config, não conteúdo) | `RotationBonusesEnabled` (bool), `JournalToastsEnabled` (bool), `RotationStrictness` (choice: leniente/estrito), slider de opacidade do selo 0.2–0.8 (float). Registro com fallback silencioso; **nunca** dependência dura no `manifest.json` | `rotation-journal.md:48` |
| 2 | `mobile-coop.md` | Config page (grazing, manure) | `GrazingEnabled` (bool), `ManureExpiryDays` (int, default 2), `ShowGrazingRadius` (bool) | `mobile-coop.md:54` (constants em `:47`) |
| 3 | `insect-hotels-beehive-aura.md` | Aura toggles | `BeneficialsEnabled` (bool), `ShowAuraRings` (choice: hover/always/never), `AuraParticles` (bool, default `true`, desligado automaticamente por `ReduceMotion`) | `insect-hotels-beehive-aura.md:54,56` |
| 4 | `living-pharmacy-tonics-sprays.md` | Spray/tonic toggles | `PharmacyEnabled` (bool), `SprayDays` (int 1–5, default 3), `ShowSpraySeals` (bool) | `living-pharmacy-tonics-sprays.md:56` |
| 5 | `adobe-shed-green-roof.md` | Ripening-bonus toggle | `AdobeBonus` (int 0–50%, default 25), `GreenRoofHerbOnly` (bool) | `adobe-shed-green-roof.md:59` |
| 6 | `green-manure.md` | Cover-crop toggles | `GreenManureEnabled` (bool), `CoverCropWindowDays` (slider int), `ShowCoverOverlay` (bool) | `green-manure.md:60` |

## 4. O `config.json` completo como bloco JSON

**Estado atual:** não existe `config.json` no repo (nenhum arquivo; `ModEntry` nunca chama `ReadConfig`/`WriteConfig`). A configuração existente é `VisualizationConfiguration`, persistida **por save** via `IModDataService` (chave `visualization_config_<saveId>`). Shape JSON real de hoje:

```json
{
  "OverlaysEnabled": true,
  "TooltipsEnabled": true,
  "HoeFeedbackEnabled": true,
  "Opacity": 0.5,
  "PoorColor": { "R": 185, "G": 28, "B": 28, "A": 255 },
  "ModerateColor": { "R": 217, "G": 119, "B": 6, "A": 255 },
  "HealthyColor": { "R": 21, "G": 128, "B": 61, "A": 255 },
  "UnknownColor": { "R": 107, "G": 114, "B": 128, "A": 255 },
  "ShowPatterns": true,
  "AccessibilityDegradation": "auto"
}
```

**Proposta pós-GMCM** (`config.json` global, seção por seção — apenas campos que aparecem nos briefings consumidores):

```jsonc
{
  "General": "TBD — nenhum briefing solicita campos gerais (ex.: reset, idioma)",
  "Visualization": {
    "OverlaysEnabled": true,
    "TooltipsEnabled": true,
    "HoeFeedbackEnabled": true,
    "Opacity": 0.5,
    "PoorColor": { "R": 185, "G": 28, "B": 28, "A": 255 },
    "ModerateColor": { "R": 217, "G": 119, "B": 6, "A": 255 },
    "HealthyColor": { "R": 21, "G": 128, "B": 61, "A": 255 },
    "UnknownColor": { "R": 107, "G": 114, "B": 128, "A": 255 }
  },
  "Accessibility": {
    "ShowPatterns": true,
    "AccessibilityDegradation": "auto"
  },
  "Behavior": {
    "RotationBonusesEnabled": true,
    "JournalToastsEnabled": true,
    "RotationStrictness": "lenient",
    "SealOpacity": 0.5,
    "GrazingEnabled": true,
    "ManureExpiryDays": 2,
    "ShowGrazingRadius": true,
    "BeneficialsEnabled": true,
    "ShowAuraRings": "hover",
    "AuraParticles": true,
    "PharmacyEnabled": true,
    "SprayDays": 3,
    "ShowSpraySeals": true,
    "AdobeBonus": 25,
    "GreenRoofHerbOnly": true,
    "GreenManureEnabled": true,
    "CoverCropWindowDays": "TBD — default não fixado em green-manure.md",
    "ShowCoverOverlay": true
  },
  "Modem": "TBD — chave não existe em nenhum briefing nem no código (provavelmente artefato de template)"
}
```

Notas: defaults de `Visualization`/`Accessibility` vêm de `Constants.cs:31-45`. Defaults de `Behavior` são fixados pelos briefings apenas onde citados (`ManureExpiryDays=2`, `SprayDays=3`, `AdobeBonus=25`, `AuraParticles=true`); os demais são `TBD` até cada `/speckit.specify`. **Drift a resolver na spec:** CHK011/CHK014 (e FR-009) referenciam `HudAnchorOffset`, que **não existe** em `VisualizationConfiguration.cs` — checklist e código divergem.

## 5. Decisão: persitência por save vs mod-wide `config.json`

| Critério | `config.json` global | Persistência por save (status quo) |
|---|---|---|
| **Principle VI** (`constitution.md:28-37`) | ✅ preferências por jogador/máquina; nenhum estado de fazenda é tocado | ⚠️ escrever preferências no save do host via `IModDataService` cria risco de fork se um farm hand editar (ver §10) |
| **FR-004/005/006** (`spec.md:119-121`) | ✅ "via JSON file" + "load on mod initialization" + hot-reload = semântica de arquivo global lido na inicialização | ⚠️ `tasks.md:306` registrou a chave por saveId **antes** de o GMCM entrar no radar; FR-005 fala em inicialização do mod, não em load de save |
| **CHK001/CHK002** | ✅ localização e escopo ("global" = mod-wide, compartilhado entre as fazendas de um jogador) ficam inequívocos | ❌ escopo ambíguo: a config não segue o jogador entre saves |
| **CHK013** (arquivo ausente → defaults completos) | ✅ `ReadConfig<T>()` cria o arquivo com defaults na primeira execução | ⚠️ `IModDataService` não tem equivalente natural (`LoadData` retorna nulo → defaults em memória) |
| **CHK010** (validação em save-time) | ✅ `WriteConfig` só persiste o que o callback `save` do GMCM entrega; `Validate` continua aplicável | ✅ já implementado em `VisualizationConfigurationService.Validate` |
| **`tasks.md:306`** | ⚠️ a chave `visualization_config_<saveId>` **existe e é usada hoje** por `VisualizationConfigurationService` — sua deprecação é decisão de `/speckit.specify` (migração aditiva e lossless, Principle VI) | ✅ mecanismo já implementado e testado |

**Recomendação:** **`config.json` global** editado via GMCM. Razão que resolve a contradição: FR-004/005/006 descrevem um contrato de requisitos (arquivo JSON global, carregado na inicialização, hot-reload, fallback parcial); `tasks.md:306` registra a decisão de implementação do 001, tomada antes do GMCM e **nunca ratificada contra FR-004**. Todos os campos dos seis briefings são preferências por jogador (apresentação) ou modificadores por jogador — não são estado da fazenda, que é o domínio exclusivo do save host-authoritativo (Principle VI). A migração per-save → global é **aditiva e lossless** (saves existentes permanecem válidos) e pertence a `/speckit.specify` — este briefing não a implementa.

## 6. Registro GMCM em C#

Versão-alvo: **GMCM 1.16.0** (Nexus mod 5098). A API `IGenericModConfigMenuApi` é estável desde o redesenho 1.5.0. **Não compilado nesta sessão** (sem `GamePath` nesta máquina) — **verificar no build com `GamePath`**. Não há pacote NuGet oficial; o padrão documentado (wiki.gg) é o mod trazer a interface em seu próprio código.

```csharp
// LivingRoots/Integrations/IGenericModConfigMenuApi.cs
// Cópia local da interface — assinaturas copiadas de
// spacechase0/StardewValleyMods/IGenericModConfigMenuApi.cs (branch develop).
// NÃO inventar membros: Register, AddSectionTitle, AddSubHeader, AddParagraph,
// AddImage, AddBoolOption, AddNumberOption (int/float), AddTextOption, AddKeybind,
// AddKeybindList, AddPage, AddPageLink, AddComplexOptionWithGamepadSupport,
// SetTitleScreenOnlyForNextOptions, OnFieldChanged, OpenModMenu,
// OpenModMenuAsChildMenu, TryGetCurrentMenu, Unregister.
// NÃO existe AddColorOption em nenhuma versão do GMCM — cores exigiriam
// AddComplexOptionWithGamepadSupport (fora de escopo, ver §14).

// Registro — dentro de ModEntry.Entry(IModHelper helper), pois o helper não é
// retido em campo hoje (ModEntry.cs:28-82). ModController já recebe helper e
// ModManifest (ModEntry.cs:78) e já trata GameLaunched (Controllers/AGENTS.md).
// Timing em GameLaunched = CHK003.
helper.Events.GameLoop.GameLaunched += OnGameLaunched;

private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
{
    var gmcm = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>(
        "spacechase0.GenericModConfigMenu");
    if (gmcm is null)
    {
        this.Monitor.Log("GMCM not installed; config menu registration skipped.", LogLevel.Trace);
        return;
    }

    gmcm.Register(
        this.ModManifest,
        reset: () => this._controller?.ResetConfigurationToDefaults(),
        save: () => this._controller?.SaveConfiguration());

    gmcm.AddSectionTitle(this.ModManifest,
        () => this.Helper.Translation.Get("config.section.visualization"));

    gmcm.AddBoolOption(this.ModManifest,
        getValue: () => this._controller.GetConfiguration().OverlaysEnabled,
        setValue: value => this._controller.SetOverlaysEnabled(value),
        name: () => this.Helper.Translation.Get("config.overlays.name"),
        tooltip: () => this.Helper.Translation.Get("config.overlays.tooltip"),
        fieldId: "OverlaysEnabled");

    gmcm.AddNumberOption(this.ModManifest,
        getValue: () => this._controller.GetConfiguration().Opacity,
        setValue: value => this._controller.SetOpacity(value),
        name: () => this.Helper.Translation.Get("config.opacity.name"),
        tooltip: () => this.Helper.Translation.Get("config.opacity.tooltip"),
        min: 0f, max: 1f, interval: 0.05f,
        formatValue: value => $"{value:P0}",
        fieldId: "Opacity");
}
```

`ResetConfigurationToDefaults`, `SaveConfiguration`, `SetOverlaysEnabled` e `SetOpacity` **não existem** no `ModController` hoje — são métodos a criar na spec (TDD, Constitution IV). `GetConfiguration` existe em `IVisualizationConfigurationService`. Um `fieldId` estável é obrigatório para `OnFieldChanged`; sem ele o GMCM gera IDs aleatórios.

## 7. `manifest.json` diff (declaração de dependência opcional)

`config.json` **não existe** no projeto hoje — o SMAPI o cria automaticamente na primeira chamada a `helper.ReadConfig<T>()` (wiki: *Modding:Modder Guide/APIs/Config*). O `.csproj` **não** copia `config.json` para o output; ele é gerado em runtime na pasta do mod do jogador (`<Stardew Valley>/Mods/lfgranja.LivingRoots/`).

Diff em `LivingRoots/manifest.json`:

```diff
   "UniqueID": "lfgranja.LivingRoots",
   "EntryDll": "LivingRoots.dll",
-  "MinimumApiVersion": "4.0.0"
+  "MinimumApiVersion": "4.0.0",
+  "Dependencies": [
+    {
+      "UniqueID": "spacechase0.GenericModConfigMenu",
+      "IsRequired": false
+    }
+  ]
 }
```

`IsRequired: false` é o ponto crítico: com `true` o mod **não carrega** sem GMCM (wiki.gg, *Tutorial: How to create a manifest*) — contrário ao mandato de `rotation-journal.md:48` ("nunca dependência dura"). Sem a declaração o GMCM ainda funciona (o registro é por `GetApi` em runtime), mas a declaração documenta a integração para o jogador e para o SMAPI. **Não inventar `UpdateKeys`** — o mod não tem Nexus ID verificado; deixar fora até o release.

## 8. Chaves i18n para cada rótulo

Contraste: `LivingRoots/i18n/default.json` tem **apenas** 12 linhas de chaves de compost — **zero** chaves de configuração. Convenção existente: minúsculas com pontos (`rotation-journal.md:51`). O GMCM recebe `Func<string>` para `name`/`tooltip` → `Helper.Translation.Get(...)`; nunca hardcodar literais (lição de `living-pharmacy-tonics-sprays.md:77`). `default.json` = EN; `pt.json` futuro.

| Briefing | Chaves a criar (name + tooltip por campo) |
|---|---|
| Visualização (001, existente) | `config.section.visualization`, `config.overlays.name`/`.tooltip`, `config.tooltips.*`, `config.hoe-feedback.*`, `config.opacity.*`, `config.colors.poor.*`/`.moderate.*`/`.healthy.*`/`.unknown.*`, `config.patterns.*`, `config.degradation.*` |
| `rotation-journal` | `config.section.rotation`, `config.rotation.bonuses.*`, `config.rotation.toasts.*`, `config.rotation.strictness.*` (+ `formatAllowedValue` para leniente/estrito), `config.rotation.seal-opacity.*` |
| `mobile-coop` | `config.section.coop`, `config.coop.grazing.*`, `config.coop.manure-expiry.*`, `config.coop.show-radius.*` |
| `insect-hotels` | `config.section.insects`, `config.insects.beneficials.*`, `config.insects.aura-rings.*` (hover/always/never), `config.insects.particles.*` |
| `living-pharmacy` | `config.section.pharmacy`, `config.pharmacy.enabled.*`, `config.pharmacy.spray-days.*`, `config.pharmacy.show-seals.*` |
| `adobe-shed` | `config.section.adobe`, `config.adobe.bonus.*`, `config.adobe.herb-only.*` |
| `green-manure` | `config.section.green-manure`, `config.green-manure.enabled.*`, `config.green-manure.window.*`, `config.green-manure.show-overlay.*` |

## 9. Se o player **não** tem GMCM instalado

O mod **carrega e funciona normalmente**: o registrar detecta `GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu") == null` e retorna silenciosamente (log `LogLevel.Trace`, sem mensagem de erro ao jogador — regra de segurança de logs do repo). Nada é desabilitado: os campos mantêm os defaults de `ModConstants`, e o `config.json` (uma vez criado) continua legível e editável à mão via SMAPI Config API, sem GMCM. Justificativa: `rotation-journal.md:48` exige explicitamente "registrar via API GMCM com fallback silencioso se GMCM ausente (nunca dependência dura no `manifest.json`)"; Constitution V (YAGNI) proíbe construir UI própria quando o padrão do framework basta; os seis briefings confirmam "IClickableMenu custom: **não**". O único custo ao jogador sem GMCM é editar o JSON manualmente.

## 10. Risco de fork de config em multiplayer (Principle VI)

`config.json` é **por máquina/por jogador**: cada cliente (host e farm hands) lê a própria cópia na própria instalação; edições GMCM de um farm hand **nunca** tocam o save do host. Para campos de **apresentação** (opacidade, anéis, tooltips, partículas, selos, overlay de cobertura) isso é correto e não há fork: cada jogador vê sua própria config.

Para campos de **gameplay** (`RotationBonusesEnabled`, `GrazingEnabled`, `ManureExpiryDays`, `BeneficialsEnabled`, `SprayDays`, `AdobeBonus`, `GreenManureEnabled`, `CoverCropWindowDays`, `GreenRoofHerbOnly`) o risco é real: se host e farm hand discordam, os cálculos sobre a **mesma fazenda** divergem. Principle VI (`constitution.md:28-37`) exige: *"Farm state is shared, host-authoritative state… clients MUST NOT fork divergent copies of farm state"* e *"Divergence handling is explicit… Silent last-write-wins on farm tiles is not acceptable without a documented rationale."* Regra recomendada para cada spec de feature: **a config do host é autoritativa para cálculos da fazenda**; a do farm hand rege apenas sua própria apresentação. Nunca gravar edits GMCM de farm hand em dados por save (`IModDataService`) — isso escreveria no save do host e criaria exatamente o fork que Principle VI proíbe. A regra final é de cada `/speckit.specify`, não deste briefing.

## 11. Testes

Arquivos existentes que mudam quando o registrar GMCM for implementado (TDD — Constitution IV):

- `LivingRoots.Tests/ModEntryTests.cs` — `Entry` passa a assinar `GameLaunched`; os testes de dispose/rollback (`UnregisterEventsRollbackTests.cs`, `ConcurrentRegisterUnregisterRaceConditionTest.cs`) precisam cobrir o novo handler.
- `LivingRoots.Tests/Visualization/VisualizationConfigurationServiceTests.cs` e `VisualizationConfigurationTests.cs` — só mudam **se** a §5 migrar a persistência (não decidido aqui).

Arquivo **novo** (não existe hoje): `LivingRoots.Tests/GmcConfigRegistrarTests.cs` — registrar com API nula (fallback silencioso), `Register` duplo (o GMCM lança `ArgumentException` se o mod já estiver registrado), `fieldId`s estáveis.

Landmines que **não** se deve tocar sem saber:

- `FeatureStatusTests.cs` lê `README.md` como recurso embutido e exige 3× `"(Planned)"` — editar o README quebra o teste (e `--no-build` lê a cópia stale).
- `ArchitecturalPlanTests.cs` exige newline final em todo `.md` de `docs/architectural_and_refactor_plans/**`.

Não há testes de GMCM in-game neste repo (sem jogo nesta máquina) — **verificar no build com `GamePath`**.

## 12. Verificação em jogo

Manual (com GMCM 1.16.0 instalado):

1. Tela de título → **Mod Options** (ícone de engrenagem) → "Stardew Valley - Living Roots".
2. In-game: **Options → Mod Options** (base do menu) — opções editáveis em runtime por padrão (`titleScreenOnly: false`).
3. Toggle `OverlaysEnabled` → **Save** → overlays somem/aparecem **sem reiniciar** (FR-005 hot-reload; FR-019: um frame atômico, sem frames mistos).
4. **Reset** → defaults de `Constants.cs:31-45`.
5. Confirmar `config.json` escrito em `<Stardew Valley>/Mods/lfgranja.LivingRoots/config.json`.

Console SMAPI:

- `spacechase0.GenericModConfigMenu_OpenModConfig lfgranja.LivingRoots` (trigger action único do GMCM, documentado em wiki.gg, *List of Triggers and Trigger Actions added by Mod Frameworks*).
- Sem GMCM: o mod carrega; log de Trace confirma o skip; features funcionam com defaults.

## 13. Riscos

1. **Drift da interface local**: sem referência em tempo de compilação, a cópia de `IGenericModConfigMenuApi` pode divergir da API real; `GetApi` retorna nulo em incompatibilidade → skip silencioso. Mitigação: pin à forma 1.5.0+ e testar contra GMCM 1.16.0.
2. **Fonte wiki desatualizada**: wiki.gg *Generic Mod Config Menu/Reference* (oldid 6502) ainda documenta a API legada pré-1.5.0 — copiá-la gera código que não compila contra GMCM moderno.
3. **Divergência em co-op** (§10): toggles de gameplay sem regra host-authoritativa violam Principle VI; cada feature spec deve declarar a regra.
4. **Atomicidade do hot-reload**: o callback `save` do GMCM muta config; FR-019 exige swap atômico no frame seguinte — o registrar deve trocar o snapshot, não mutar objetos vivos mid-render.
5. **Migração per-save → global**: mudar a persistência de `VisualizationConfiguration` quebra round-trip de saves existentes se não for aditiva (Principle VI) e invalida testes de persistência.

## 14. Fora de escopo

- As specs dos seis features (`/speckit.specify` de cada briefing consumidor) — este briefing só enumera campos.
- `IClickableMenu` custom — só o Rotation Journal justifica um (`rotation-journal.md:45`).
- Widget de cor (GMCM **não tem** `AddColorOption` em nenhuma versão; exigiria `AddComplexOptionWithGamepadSupport`).
- Keybind do Journal (`J` default) — padrão `AddKeybind`, mas o journal é feature própria.
- Ícone de apresentação/release assets do mod (`mod-presentation-release-assets.md`) — o "check de `Assets/**`" do cabeçalho deste briefing.
- `ConfigSchema` do Content Patcher (o mod edita assets via `OnAssetRequested` em C#, não usa CP).
- Harmony (nenhum briefing deste lote exige).
- Traduzir `default.json` — este briefing enumera chaves, não traduções.

---

Última verificação: 2026-10-05 — subagente de pesquisa (sessão de preenchimento do briefing). Fontes externas fetchadas e internas lidas em full nesta data; código não compilado (sem `GamePath` nesta máquina) — marcadores "verificar no build com `GamePath`" onde aplicável.
