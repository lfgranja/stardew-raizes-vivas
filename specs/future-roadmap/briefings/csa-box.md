# Briefing — CSA Box / Community Supported Agriculture (Epic 17 · US-17-01 #58 / US-17-02 #59)

> **Escopo:** `ROADMAP.md:62` — "CSA Box buildable on the farm. Season start: residents (Evelyn, Gus, Clint…) subscribe, pay upfront. Obligation: varied basket weekly. Fail = friendship loss. Success = big friendship (+ metaphorical soil health)".
> **Epics:** Epic 17 (Issue #19): US-17-01 (#58) caixa, US-17-02 (#59) assinatura/entrega.
> **Status:** sem spec/plan/tasks. Insumo para `speckit.specify`.
> **Leitura interna:** agrupado §6, `AGROECOLOGY_CONCEPTS.md:55` (CSA = economia solidária), Epic 16 Fair (cotas/variedade reutilizadas aqui). Meta soberania: ≥5 famílias assinantes (`food-sovereignty-guardian-trophy.md`).
> **Ordem:** Fair antes de CSA (cotas primeiro, cesta depois). Entrega CSA **não** gera +25% da Fair (anti-double-dip).

---

## 1. Conceito de jogo

Caixa construível na fazenda. **Início de cada estação**: moradores assinam e pagam **adiantado** (alívio do early-season). **Toda semana**: cesta variada na caixa. **Falhar** = perde amizade (carta de decepção). **Sucesso** = grande amizade + pequeno buff real de soil health ("solo metafórico" operationalizado: +1 global semanal em sucesso — auditável, anti-inflação).

Assinantes canônicos: Evelyn, Gus, Clint + rotativos (Jodi, Leah, Demetrius…). Nº de candidatos > meta 5 (escolha e rotatividade importam).

---

## 2. Sprites

| Asset | Spec | Arquivo sugerido |
|---|---|---|
| CSA Box (building pequeno) | building `Size` 2×2, `DisplayTile` bubble "item ready" | `Assets/csa_box.png` (tiles building) — caixa madeira verde com lousa |
| Cesta semanal (item token) | **16×16** (representa cesta entregue; opcional — pode ser bundle invisível) | `Assets/csa_basket.png` — cesta vime com vegetais |
| Dropbox marker | bubble vanilla | sem asset novo |
| Carta convite sazonal | mail icon vanilla | sem asset novo |

Building vs. big-craftable: ROADMAP diz "can be built" — **recomendado building** (`Data/Buildings`: `Size`, `CollisionMap`, `Chests[]`/`ItemConversions[]`, `ActionTiles` abre UI) por expressividade (dropbox + indoor? não precisa indoor). Big-craftable + máquina é fallback barato se building estourar escopo.

---

## 3. Data JSON

```csharp
// Data/Buildings — CSA Box (recomendado)
data["LivingRoots.CSABox"] = new BuildingData {
    // Builder = "Robin", BuildCost = 2500, BuildMaterials = "388 100 390 50",
    // Size = (2,2), CollisionMap = "XX\nXX" com frente passável,
    // Chests = [{ Id="CsaInput", Type="Load" }, { Id="CsaOutput", Type="Collect" }],
    // ActionTiles = [{ Tile=(1,0), Action="OpenCSA" }], // C# abre board
    // DisplayTile + bubble quando cesta pronta
};

// Data/SpecialOrders — MOLDE IDEAL da cesta semanal (board-based, repetível, sazonal)
data["LivingRoots.CSA_Evelyn_Spring"] = new SpecialOrderData {
    Name = "{{i18n:csa.evelyn.name}}",
    Requester = "Evelyn",
    Duration = "Week", Repeatable = true,
    RequiredTags = "season_spring, mail_livingroots_csa_open",
    OrderType = "LivingRoots_CSA", // board custom na caixa filtra por tipo próprio!
    Text = "{{i18n:csa.evelyn.text}}",
    RandomizedElements = new[] {
        new { Name = "Basket", Values = new[] {
            new { RequiredTags = "season_spring",
                  Value = "PICK_ITEM Potato, Green Bean, Garlic, Cauliflower" },
            // verão/outono/inverno variam (Lewis-tabela como precedente)
        }},
    },
    Objectives = new[] {
        new ObjectiveData { Type = "Collect",
            Text = "{{i18n:csa.objective.collect}}",
            RequiredCount = 8,
            Data = new { AcceptedContextTags = "{Basket:Tags}" } },
        new ObjectiveData { Type = "Donate",
            Text = "{{i18n:csa.objective.donate}}",
            RequiredCount = 8,
            Data = new { DropBox = "CsaBox", DropBoxGameLocation = "Farm",
                         DropBoxIndicatorLocation = "<x> <y>",
                         AcceptedContextTags = "{Basket:Tags}" } },
    },
    Rewards = new[] {
        new RewardData { Type = "Money", Data = new { Amount = "800" } }, // saldo semanal (adiantado é separado)
        new RewardData { Type = "Friendship", Data = new { Amount = "250", TargetName = "Evelyn" } },
        new RewardData { Type = "Mail", Data = new { MailReceived = "livingroots_csa_success", NoLetter = "true" } },
    },
};

// Pagamento ADIANTADO: evento de assinatura início-estação (Data/Events/Farm ou mailbox):
//   action AddMoney <upfront> + mail flag livingroots_csa_<npc>_<season>_<year>
//   upfront ex.: 1500g/assinante/estação (escala com nº assinantes? fixo — spec calibra)
// Data/Mail — convite sazonal:
//   Condition: "SEASON Spring, DAY_OF_MONTH 1 2 3 4 5 6 7, YEAR 4" + HostMail
// Variedade: AcceptedContextTags "!forage_item, category_vegetable/category_fruit, quality_gold"-style
//   + sintaxe , (E) / / (OU) / ! (NÃO) — ex. Pierre QoS
```

Falha (vanilla não tem "punishment reward" — **requer C#**): `DayEnded` checa ordens `LivingRoots_CSA` expiradas → `changeFriendship(npc, -100)` + carta decepção, cap de perda (ex.: -250/semana) anti-softlock corações. Sucesso: `+250` (1 coração) + `+1` soil health global (ato simbólico real — `ISoilHealthService` batch com cap).

---

## 4. Mapa / Evento

- **Mapa:** CSA Box building na fazenda (posição player-driven, `Buildable T` tiles). `EditMap` n/a (building system posiciona). `DropBox` Action no mapa: `DropBox CsaBox` (tile da caixa) — `Modding:Maps#Action` + `MinimumCapacity` (default 9; cesta 8 cabe).
- **Board custom:** board "CSA" na caixa filtra `OrderType = LivingRoots_CSA` (precedente: boards custom por tipo — Qi/DesertFestivalMarlon). Sem board físico novo em Town (evita conflito Lewis board).
- **Eventos:**
  1. **Assinatura (início estação, dias 1–7):** `Data/Events/Farm` ou carta+visita: morador bate à porta (`Time 800 1200`, `Season <x>`, `DayOfMonth 1 7`, `!FestivalDay`, `SawEvent` once/season). Script: `speak/question fork (aceitar N assinantes?)/AddMoney upfront/mail flag/skippable/setSkipActions (aceita automaticamente com valor menor? ou re-oferta por mail — anti-softlock)/broadcastEvent`.
  2. **Entrega semanal:** sem cutscene (dropbox silencioso + bubble + carta agradecimento).
  3. **Falha:** carta decepção (mail, sem cutscene — evita punir com tempo).
- **ActionTiles:** `OpenCSA` (abre board), `DropBox CsaBox` (deposita cesta).

---

## 5. UI / Interação / Sons

- **UI:** board CSA (lista ordens ativas: morador, cesta exigida com tags, progresso Collect/Donate, dias restantes). Reutilizar Special Orders board UI vanilla se possível (menor custo); menu custom só se board vanilla não filtrar `OrderType` custom (verificar na spec — Qi board prova que filtra).
- **Variedade visível:** cada ordem mostra tags aceitas + exemplos (`Potato/Green Bean/Garlic…`), progresso `5/8`.
- **Sons:** `questStarted` (aceite), `questComplete`/`gift` (entrega), `trashcan`? (falha — confirmar cue), `purchaseClick` (adiantado). Sem `.ogg` custom.
- **Overlay:** bubble na caixa + marca no calendário (dia de entrega = domingo? sugerido **sexta** para não colidir com Fair sábado e Kitchen domingo — triângulo semanal Fair→CSA→Kitchen distribuído).

---

## 6. i18n

```json
{
  "csa.title": "CSA Box",
  "csa.evelyn.name": "Evelyn's Spring Basket",
  "csa.evelyn.text": "Fresh vegetables every week, dearie? I'll pay upfront.",
  "csa.objective.collect": "Gather {0}x {1}",
  "csa.objective.donate": "Leave them in the CSA Box",
  "csa.success": "Evelyn loved your basket! (+1 heart)",
  "csa.fail": "Evelyn missed your basket... (-1 heart)",
  "csa.invite.title": "CSA subscription — {0}",
  "csa.invite.text": "Pay {0}g now, deliver a varied basket every Friday. Fail and I go hungry...",
  "mail.csa.thanks": "That basket fed us all week. Thank you! — {0}"
}
```

---

## 7. Gates / desbloqueio / balanceamento

| Gate | Proposta |
|---|---|
| Desbloqueio | Fair aberta + Year 4 + construir CSA Box (Robin 2500g). Mail convite dias 1–7 cada estação |
| Assinantes | 3–7 candidatos/estação, meta soberania 5 simultâneos; rotativo por estação (Evelyn primavera, Gus verão…); cancela se 2 falhas seguidas (janela: simultâneos contam) |
| Adiantado | 1500g/assinante/estação (fixo; escala? não — simplicidade); útil early-season |
| Cesta | 8 itens/semana, tags sazonais variadas (anti-monocultura: exige ≥3 categorias distintas — regra C# sobre `RandomizedElements`) |
| Recompensa | 800g/semana + 250 amizade (1 coração); falha -100 (cap -250/semana) |
| Solo metafórico | +1 soil health global em semana perfeita (todas cestas OK), cap 4/estação |
| Anti-double-dip | Entrega CSA não conta p/ cotas Fair nem +25% |
| Multiplayer | Assinatura por time (host aceita); entrega qualquer jogador; amizade por time? (vanilla friendship é por jogador — aplicar a quem depositou? spec decide; sugerido: full ao depositante, half aos outros) |

---

## 8. Constantes novas

```csharp
public const string CsaBoxId = "LivingRoots.CSABox";
public const string CsaOrderType = "LivingRoots_CSA";
public const string CsaDropBoxId = "CsaBox";
public const int CsaUpfrontPerSubscriber = 1500;
public const int CsaWeeklyReward = 800;
public const int CsaSuccessFriendship = 250;
public const int CsaFailFriendship = -100;
public const int CsaFailWeeklyCap = -250;
public const int CsaBasketCount = 8;
public const float CsaSoilBonusPerfectWeek = 1f;
```

## 9. Riscos e teste

- **Riscos:** `OrderType` custom exige board custom (se board vanilla não filtrar, custo UI sobe); `RandomizedElements PICK_ITEM` com IDs custom (verificar resolução `LivingRoots.*`); punição de amizade sem suporte vanilla (C# `DayEnded` + expiração `Duration Week` — corrida com coleta tardia); dropbox `MinimumCapacity` vs. cesta 8.
- **Teste:** unit (cesta ≥3 categorias, upfront math, caps amizade, reset semanal); integração (orders aparecem por estação, dropbox aceita/recusa tags, expiração pune, sucesso paga); manual (assinar 2, entregar 1 e falhar outra, checar corações + solo + sem +25% Fair).

## Fontes

- Internas: `ROADMAP.md:62`, Epic 17, agrupado §6.
- Externas: `Modding:Special_orders` (Duration Week/Repeatable/RequiredTags season/mail/OrderType custom/RandomizedElements PICK_ITEM/Objectives Collect-Donate-Deliver/DropBox+DropBoxGameLocation+DropBoxIndicatorLocation+MinimumCapacity/Rewards Money-Friendship-Mail-Object/ContextTags `,!/`); `Modding:Buildings` (Chests/ItemConversions/ActionTiles/DisplayTile); `Modding:Maps#Action` (DropBox); `Modding:Mail_data`; `Modding:Event_data`.
