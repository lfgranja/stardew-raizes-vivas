# Briefing 1:1 — Agroecological Welcome Kit (substitui parsnips iniciais)

> Status: briefing (não implementado). Agrupado de origem: `specs/future-roadmap/briefings/early-game-year1.md` Cap. 2 (preservado, não apagar).
> Escopo interno: `ROADMAP.md:35` ("Replaces the initial parsnip seeds with a kit that includes a Composter and heirloom seeds").
> Não promover para `specs/` sem `/speckit.specify`.

## 1. Conceito

Kit entregue pela Elena (ver `elena-npc.md`) via evento de intro (dia 1–2, mail + cutscene curta). Conteúdo:

1. 1 Composter (reutiliza `ModConstants.CompostingBinItemId = "LivingRoots.CompostingBin"` existente);
2. 5 heirloom parsnip seeds (ver `heirloom-seeds.md`);
3. 3 mulch (ver `mulch-soil-cover.md`);
4. 1 carta tutorial.

Tutorial em 3 passos via `Data/Quests`: (1) colocar o Composter → (2) aplicar mulch em 4 tiles → (3) colher 1 cultura heirloom. Elegibilidade com fallback anti-softlock: se o evento for pulado, enviar mail no dia 2 (`Condition` sobre flag `LivingRoots_IntroSeen` ausente).

## 2. Sprite specs

Nenhum sprite novo obrigatório Y1: kit = `Data/Mail` + itens existentes. Opcional: `LivingRoots/Assets/welcome_kit.png` **16×16** como token visual do mail (registrável como `Data/Objects LivingRoots_WelcomeKit`, `Type: Quest`, `Price: 0`, `ExcludeFromRandomSale: true`, `CanBeTrashed: false`, inobtenível fora do evento). Recomendação 80/20: pular o token no Y1, usar só mail+quest.

## 3. Data/* — JSON/C# (registro via `ModEntry.OnAssetRequested`, padrão `ModEntry.cs:114-177`)

```csharp
// Data/Mail — carta da Elena; attachments via trigger actions do evento
data["LivingRoots_WelcomeKit"] = "{{i18n:mail.welcome-kit.text}}";
// Evento dia 1-2: addItem (O)LivingRoots_CompostingBin 1,
//   (O)LivingRoots_HeirloomParsnipSeeds 5, (O)LivingRoots_Mulch 3
// Data/Quests — tutorial em 3 passos:
//   1. "Place your Composter"  2. "Mulch 4 tiles"  3. "Harvest an heirloom crop"
// Data/CraftingRecipes — NADA novo aqui (kit usa itens dos outros capítulos + bin existente)
```

Condição de elegibilidade: enviar mail dia 2 se `PLAYER_HAS_FLAG LivingRoots_IntroSeen` for falso (fallback se o evento for pulado). Nunca sobrescrever o mail vanilla das parsnips — o kit é aditivo via evento, não edição do intro vanilla.

### Loja

Não vendável: sem entrada em `Data/Shops`; se virar item, `Price: 0`, `ExcludeFromRandomSale: true`, `CanBeTrashed: false`.

## 4. Sons

`questStarted` vanilla ao receber; `gift` ao abrir. Sem `.ogg` custom.

## 5. i18n (adicionar a `en.json` + `default.json`; padrão existente: `compost.name`, `message.compost.*`)

```json
{ "mail.welcome-kit.title": "Welcome to the valley — Elena",
  "mail.welcome-kit.text": "Forget Joja hybrids for a season. Try these heirlooms, keep your soil covered, and come talk to me at Pierre's.^ ...",
  "quest.welcome-kit.1": "Place your Composter",
  "quest.welcome-kit.2": "Mulch 4 tiles of tilled soil",
  "quest.welcome-kit.3": "Harvest your first heirloom crop" }
```

## 6. Arquitetura / multiplayer (Principle VI)

- Entrega e quest-flags por fazenda (farm-keyed via `SaveIdProvider`); writes host-autoritativos com guards `Context.IsMainPlayer` onde houver mutação.
- DDD: sem serviço novo; orquestração no `ModController` (`ExecuteWithConcurrencyGuard`); `new` só em `ModEntry.cs`; sem magic numbers (usar `ModConstants` existentes + novos IDs dos outros briefings).

## 7. Testes / gates

- `dotnet format --verify-no-changes` → build com `GamePath=` → `dotnet test` (baseline 816).
- Novos testes: mail `LivingRoots_WelcomeKit` registrado; 3 quests encadeadas (completar passo N libera N+1); fallback dia 2 dispara sem flag; kit não aparece em nenhuma `Data/Shops`; chaves i18n em `en.json` + `default.json`.
- Critérios de aceite: jogador novo recebe kit dia 1–2; pular evento ainda entrega via mail; quest completa ponta a ponta; `dotnet format` limpo.
- Armadilhas: trailing newline nos `.md`; não tocar nos `"(Planned)"` do `README.md`.

## 8. Constantes novas (`LivingRoots/Constants.cs`, classe `ModConstants`)

```csharp
public const string WelcomeKitMailId = "LivingRoots_WelcomeKit";
public const string IntroSeenFlag = "LivingRoots_IntroSeen";
```

(Reutiliza `CompostingBinItemId`, IDs de `heirloom-seeds.md` e `mulch-soil-cover.md` — sem duplicar.)

## 9. Dependências / ordem

Quinto na ordem 80/20 (depende de mulch + heirloom + Composter existente; antecede só Elena). Implementar após `mulch-soil-cover.md`, `heirloom-seeds.md` e `small-cistern.md` estarem jogáveis.
