# Briefing — Food Sovereignty / Guardian Trophy (Epic 19 · US-19-01 #63 / US-19-02 #64)

> **Escopo:** `ROADMAP.md:64-69` — "New metric in your journal. Victory when: farm >80% avg soil health + ≥10 heirloom seed types saved + Collective Kitchen operational + CSA ≥5 families. Reward: Pierre stops selling Joja Hybrid, only heirloom; Morris leaves town permanently; Elena gives 'Guardian of Living Roots' trophy. Valley self-sufficient".
> **Epics:** Epic 19 (Issue #21): US-19-01 (#63) métrica/critérios, US-19-02 (#64) recompensas.
> **Status:** sem spec/plan/tasks. Por último (consome flags de todos os anteriores).
> **Leitura interna:** agrupado §8, `AGROECOLOGY_CONCEPTS.md:50` (soberania: "right of peoples to define their own food policies"), `EPICS_USER_STORIES.md:124-128`. Métrica final não é ouro — é autonomia.
> **Regra de ouro:** vitória = **latch permanente** (one-way) com epílogo — nunca punir experimentação pós-vitória revertendo o troféu se o solo cair.

---

## 1. Conceito de jogo

Nova **métrica no journal** com 5 critérios (todos). Ao completar: cerimônia Elena → troféu + Pierre só-heirloom + Morris sai + epílogo cidade (diálogos town-wide, preços, carta final). Sem isso a vitória é só um popup — especificar o que muda de fato.

Critérios (definições travadas aqui, spec implementa):

1. **Solo médio >80%** — snapshot `DayStarted`, sobre tiles **aráveis da fazenda** (não estufa/ilhas; spec confirma universo). Agregação do sparse cache (zeros omitidos = ignora? ou conta 0? — **conta 0** via denominador = total aráveis, senão média infla; decidir e documentar).
2. **≥10 tipos Heirloom Seeds** — IDs distintos em inventário + baús da fazenda (plantadas não contam; stacks não multiplicam).
3. **Cozinha operacional** — flag de **inauguração** (evento visto, não só construída).
4. **CSA ≥5 famílias** — households simultâneos ativos (não indivíduos; cancelado não conta).
5. *(Implícito)* ICLF + Fair desbloqueadas (pré-requisitos encadeados — checar flags).

---

## 2. Sprites

| Asset | Spec | Arquivo sugerido |
|---|---|---|
| Guardian Trophy (item) | **16×16** `Data/Objects` Type `Basic` (exibição; `Quest` não-giftable/trashed por padrão — troféu de exibição sugere `Basic` não-vendável Price 0, `CanBeTrashed false`) | `Assets/guardian_trophy.png` — raiz dourada segurando broto |
| Medalha cerimônia (prop evento) | temporarySprite | reutilizar vanilla |
| Placa praça "Vale Soberano" | tile `.tmx` | tileset `z_sovereignty.png` (1 tile) |

Sem spritesheet sazonal; menor custo de arte do End Game (1 item + 1 tile).

---

## 3. Data JSON

```csharp
// Data/Objects — troféu
data["LivingRoots.GuardianTrophy"] = new ObjectData {
    Name = "Guardian of Living Roots", DisplayName = "{{i18n:trophy.name}}",
    Description = "{{i18n:trophy.description}}",
    Type = "Basic", Category = -26, Price = 0,
    Texture = "Mods/LivingRoots/Trophy", SpriteIndex = 0,
    CanBeTrashed = false, // troféu permanente
    ContextTags = new[] { "livingroots_trophy", "not_giftable" },
};

// Data/Shops — Pierre pós-soberania (híbridas somem, heirlooms aparecem):
{ "Id": "LivingRoots.JojaHybrid_Tomato", "ItemId": "(O)LivingRoots.JojaHybridTomato",
  "Price": 60, "Condition": "!PLAYER_HAS_MAIL livingroots_sovereignty" }, // some após vitória
{ "Id": "LivingRoots.Heirloom_Tomato", "ItemId": "(O)LivingRoots.HeirloomTomatoSeeds",
  "Price": 75, "Condition": "PLAYER_HAS_MAIL livingroots_sovereignty" }, // aparece após vitória
// Alternativa C# (ShopMenu patch) se lógica além de queries — YAGNI primeiro.

// Journal/UI — 3 rotas (recomendado (a) agora, (b) evolução):
//   (a) Quest persistente (addQuest/removeQuest, Data/Quests) com texto progresso atualizado por C# — nativa, só texto.
//   (b) Menu custom SMAPI (StardewUI/IClickableMenu) com barra + 5 critérios — rica, custo UI+i18n+controller.
//   (c) Special Order permanente como carrier — hack, semântica errada (descartada).
data["LivingRoots.FoodSovereignty"] = new QuestData {
    // Journal quest "Food Sovereignty" com contadores: "Soil 82% / Heirlooms 11 / Kitchen ✓ / CSA 5"
    // Nenhuma rota é Data-only — recálculo exige C# (média sparse, scan baús, flags).
};

// Morris sair: SEM data-only ("NPC sai para sempre" não existe) — C#:
//   remover schedule, trocar diálogos (addConversationTopic town-wide), evento despedida,
//   mail flag irreversível livingroots_morris_gone. Edge: não-casável — ok. Multiplayer: flag host.
// Cerimônia troféu: Data/Events/Town — Elena entrega addItem (O)LivingRoots.GuardianTrophy.
```

Agregações C# (contratos `Domain/Interfaces → Services/`, `ModEntry` composition root, `ExecuteWithConcurrencyGuard` se em controller):

- `ISovereigntyService.Recalculate()` em `DayStarted` (nunca tick): soil avg + heirloom scan + kitchen flag + CSA count → atualiza quest texto + dispara cerimônia quando 5/5 (once, `SawEvent` + mail latch).
- GSQs gating: `Season/Year/Weather/PLAYER_SPECIAL_ORDER_RULE_ACTIVE`/mail/amizade. Trigger actions recompensas: `AddMoney/AddItem/AddCraftingRecipe/AddCookingRecipe/ResetEvent/addConversationTopic`.

---

## 4. Mapa / Evento

- **Mapa:** placa praça (`EditMap Maps/Town`, 1 tile Buildings + `Action` ler epílogo). Sem location nova.
- **Cerimônia (one-shot):** `Data/Events/Town` — Elena + Lewis + Pierre + Morris. Preconditions: `SawEvent`? não; `HostMail livingroots_sovereignty_ready` + `!SawEvent <ceremonyId>` + `Time 1000 1600` + `!FestivalDay` + `Season` qualquer. Script: `music/speak (cada NPC)/move/emote/addItem (O)LivingRoots.GuardianTrophy/friendship Elena +500/mail livingroots_sovereignty (latch!)/addConversationTopic livingroots_sovereign//skippable/setSkipActions AddItem trophy + mail latch (nunca perder troféu por pular)/broadcastEvent`.
- **Despedida Morris:** evento curto encadeado (`SawEvent ceremony` + `Time` seguinte) ou mesma cerimônia ato 2 — spec decide; `mail livingroots_morris_gone` irreversível.
- **Epílogo persistente:** `addConversationTopic` town-wide (diálogos pós-soberania), carta final Lewis, preços Pierre, estoque Fair (heirlooms com desconto?), schedules inalterados (Morris some dos schedules via C#).

---

## 5. UI / Interação / Sons

- **UI (MVP):** quest journal persistente com 4 linhas + % (atualizada `DayStarted`). Sem barra custom (evolução (b) se playtest pedir visual). Special Orders board inalterado.
- **Notificação progresso:** `floating-text`/`message` ao bater cada critério ("Soil 80% ✓ — 3 to go!"), reutilizar `maturity-tooltip`/`restoration-text` patterns. Sem spam diário (só transição 0→1 por critério).
- **Sons:** `questComplete`/`achievement` (vitória), `gift` (troféu), `doorClose` (Morris sai — poético). Sem `.ogg` custom.
- **Acessibilidade:** progresso textual (não só cor); contraste journal vanilla preservado.

---

## 6. i18n

```json
{
  "sovereignty.quest.name": "Food Sovereignty",
  "sovereignty.quest.soil": "Average soil health: {0}% (need 80%)",
  "sovereignty.quest.heirloom": "Heirloom seed types stored: {0}/10",
  "sovereignty.quest.kitchen": "Collective Kitchen: {0}",
  "sovereignty.quest.csa": "CSA families: {0}/5",
  "sovereignty.done": "The valley feeds itself. Thank you, Guardian.",
  "trophy.name": "Guardian of Living Roots",
  "trophy.description": "For proving abundance grows from living soil, not poison.",
  "message.sovereignty.criterion": "{0} achieved! ({1}/4)",
  "mail.sovereignty.final": "You did what Joja said was impossible... — Lewis",
  "morris.goodbye": "Morris: This town doesn't want what I'm selling anymore..."
}
```

---

## 7. Gates / desbloqueio / balanceamento

| Gate | Definição travada |
|---|---|
| Solo | Snapshot `DayStarted`, tiles aráveis fazenda (exclui estufa/ilhas); denominador = total aráveis (zeros contam); >80% estrito |
| Heirloom | 10 IDs distintos, inventário + baús fazenda; plantadas não contam |
| Cozinha | Flag inauguração (`SawEvent kitchen_opening`), não só construção |
| CSA | 5 households simultâneos ativos |
| Reversibilidade | **Latch permanente**: `livingroots_sovereignty` once; solo pode cair depois sem perder troféu/Pierre/Morris |
| Pierre | Híbridas `Condition: !mail` somem; heirlooms condição inversa aparecem; estoque Fair acompanha |
| Morris | Flag `livingroots_morris_gone` irreversível, host-side; diálogos restantes viram passado (`addConversationTopic`) |
| Multiplayer | Métrica por time (host calcula); troféu para cada jogador presente? (sugerido: todos online ganham cópia; spec confirma); amizade Elena por jogador |
| Segurança | Agregação com cap `MaxTilesPerSave`; scan baús com limite nomes (`MaxLocationNameLength`); `checked` contadores |

---

## 8. Constantes novas

```csharp
public const string SovereigntyMail = "livingroots_sovereignty";
public const string SovereigntyReadyMail = "livingroots_sovereignty_ready";
public const string MorrisGoneMail = "livingroots_morris_gone";
public const float SovereigntySoilThreshold = 80f;
public const int SovereigntyHeirloomTypes = 10;
public const int SovereigntyCsaFamilies = 5;
public const string GuardianTrophyId = "LivingRoots.GuardianTrophy";
public const string QualifiedGuardianTrophyId = "(O)LivingRoots.GuardianTrophy";
public const string SovereigntyQuestId = "LivingRoots.FoodSovereignty";
```

## 9. Riscos e teste

- **Riscos:** média inflada por sparse (zeros omitidos) — definir denominador; heirloom "guardada" ambígua (documentar universo); cozinha "operacional" ambígua (flag evento); CSA rotatividade (janela simultâneos); vitória reversível puniria experimentação (latch!); Morris sem data-only (C# obrigatório); epílogo vazio = popup (exigir `addConversationTopic` + Pierre + carta).
- **Teste:** unit (média com zeros, contagem IDs distintos, households simultâneos, latch irreversível); integração (quest atualiza `DayStarted`, cerimônia dispara 1×, skip dá troféu+flag, Pierre stock troca por mail, Morris some); manual (forçar 4/4 + último critério ao vivo, cerimônia completa + skip, pós-vitória derrubar solo e confirmar latch).

## Fontes

- Internas: `ROADMAP.md:64-69`, Epic 19, agrupado §8, `AGROECOLOGY_CONCEPTS.md:50-56`.
- Externas: `Modding:Quest_data` (addQuest/journal) + `Modding:Special_orders` (carrier descartado); `Modding:Shops` (Condition por item `!mail`/`mail`); `Modding:Event_data` (Time/DayOfWeek/Friendship/SawEvent/HostMail/Year/!/music/speak/move/question fork/farmerEat/addItem/friendship/mail/setSkipActions/skippable/broadcastEvent); `Modding:Objects` (Type Basic/Quest, CanBeTrashed); `Modding:Game_state_queries` + `Modding:Trigger_actions` (cola); `Modding:Maps` (placa 1 tile).
