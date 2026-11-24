# Idea Intake: Mulch / Soil Cover (Epic 4)

- **Slug**: mulch-soil-cover
- **Created**: 2026-10-30
- **Source**: repo path `specs/future-roadmap/briefings/mulch-soil-cover.md` (Briefing 1:1, Epic 4 — US-04-01 #30 / US-04-02 #31); triggered by ROADMAP.md 80/20 ordering (item 3 after Soil Health → Crop Growth shipped) and the agronomic evidence in `research/2026-10-roadmap-feasibility-and-agronomy.md` §C.2 (mulch: strongest evidence — runoff −47%, soil loss −76%).
- **Type**: new-capability

## Idea (as captured)

> "Cobertura morta aplicada em tile arado (gesto como fertilizante vanilla, via novo `IMulchApplicationService` espelhado em `ICompostApplicationService`). Efeito: zera ou reduz o `DailyDecayRate = 2/dia` no tile coberto, com sinergia ao `SeasonalDecayMultiplier`. Duração: consome 1 carga a cada N dias (`MulchDurationDays = 7`); chuva não remove. Também previne ervas." — briefing `mulch-soil-cover.md` §1.
>
> Assets necessários (briefing §2): `LivingRoots/Assets/mulch.png` **16×16** (item sprite, base marrom `#92400E` + highlights); tile overlay tint `MulchColor` opacidade 0.5 com pattern `Dots` para daltônicos; partícula de aplicação reutiliza a do compost (sem asset novo).
>
> Dados: `Data/Objects` chave `LivingRoots_Mulch` (Category −26, tags `mulch_item`/`compostable_item`); receita `Data/CraftingRecipes` legada `"771 10/Home/LivingRoots.Mulch 1/false/null/Mulch"` (Fiber ×10); estado `HasMulch + dias restantes` persistido via `ModDataService` (cache esparso).
>
> Loja: receita à venda na Elena shop dia 1 (`Price: 100g`); Pierre não vende.
>
> Sons: `hoeHit`/`sandyStep` (aplicação), `axe` (craft); sem `.ogg` custom Y1.
>
> i18n: `mulch.name`, `mulch.description`, `message.mulch.invalid-tile`, `message.mulch.already`.
>
> Constantes novas: `MulchItemId`, `QualifiedMulchItemId`, `MulchDurationDays = 7`, `MulchDecayMultiplier = 0f`, `MulchTag`.
>
> Multiplayer (Principle VI): `IMulchApplicationService` em `Domain/Interfaces/` + impl em `Services/`; hook em `ModController.OnButtonPressed`; `ExecuteWithConcurrencyGuard`; estado farm-keyed via `SaveIdProvider`; writes host-autoritativos; sem `.Result`/`.Wait()`; logs sem mensagens cruas.
>
> Testes: `Data/Objects` contém `LivingRoots_Mulch` (Category −26); receita parseável; `decay *= 0` em tile com mulch (incl. sinergia Spring 0.5×/Summer 1.5×); duração 7 dias expira; chuva não remove; aplicar em tile não-arado → `message.mulch.invalid-tile`; reaplicar → `message.mulch.already`; overlay usa `MulchColor` via `IVisualizationService`.
>
> Critérios de aceite: craft dia 1 só com fibra; tile com mulch mantém saúde por 7 dias; receita comprável na Elena dia 1; `dotnet format` limpo.
>
> Dependências: nenhuma (só do decay existente). Desbloqueia `welcome-kit.md` e alimenta overlay de `elena-npc.md`.
>
> Pergunta do usuário: "Existe asset a ser criado para essa feature?" — Sim: `LivingRoots/Assets/mulch.png` (16×16) + tile overlay tint (sem asset novo, reutiliza pattern `Dots`).

## Restated

Proposta de adicionar "Mulch" (cobertura morta) como item craftável de early game que zera/reduz a taxa de decay do solo em tiles arados, com duração de 7 dias, prevenção de ervas, e integração com o sistema de soil health existente. É o feature de menor esforço e maior impacto agronômico do roadmap (meta-análise: runoff −47%, perda de solo −76%). Requer 1 asset novo (sprite 16×16) e reutiliza infraestrutura existente (decay, overlay, i18n, persistence).

## Origin & Context

- **Raised by**: ROADMAP.md 80/20 ordering (item 3, após Soil Health → Crop Growth shipped); briefing `mulch-soil-cover.md` (Epic 4); pesquisa agronômica `research/2026-10-roadmap-feasibility-and-agronomy.md` §C.2.
- **Trigger**: necessidade de lado defensivo do soil health — o jogador pode acelerar crescimento em living soil mas não pode ainda *proteger* o solo.

## First-Glance Unknowns

- [NEEDS CLARIFICATION: o sprite `mulch.png` 16×16 será criado por quem e quando? (asset pipeline vs. placeholder)]
- [NEEDS CLARIFICATION: `MulchDecayMultiplier = 0f` (zera decay) vs. `0.25f` (reduz) — decisão de balanceamento pendente na spec.]
- [NEEDS CLARIFICATION: Elena NPC não existe ainda — a receita "vendida na Elena shop dia 1" depende de Elena (não implementada). A receita craftável (fibra) funciona sem Elena.]
- [NEEDS CLARIFICATION: duração 7 dias é balanceada? Ou deve ser configurável?]
- [NEEDS CLARIFICATION: o tile overlay de mulch é compatível com o sistema de overlay existente (spec 001) sem conflito de z-order?]
- [NEEDS CLARIFICATION: `MulchTag = "mulch_item"` deve ser adicionado ao `ContextTags` de outros itens compostáveis?]
