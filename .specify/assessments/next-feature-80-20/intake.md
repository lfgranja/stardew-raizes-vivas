# Idea Intake: Next Feature by Value-per-Effort (80/20)

- **Slug**: next-feature-80-20
- **Created**: 2026-10-03
- **Source**: pasted text + repository path (codebase pointer: whole repo at `/home/luis/development/stardew-raizes-vivas`)
- **Type**: exploration

## Idea (as captured)

> Pesquise longa, completa, profunda, ampla e detalhadamente tanto internamente no repositório, quanto na internet de forma atualizada e utilize toda a sua capacidade, ferramentas, habilidades, agentes e subagentes em background:
> Considerando tudo o que já foi implementado, avalie qual seria a próxima funcionalidade / feature / correção / melhoria / pesquisa que acrescentaria mais valor ao projeto pelo menor esforço - Qual é o nosso próximo 80/20?

No URLs, credentials, or secrets present in the input; no redaction applied. No URL Trust Policy branch taken (nothing to fetch).

## Restated

The maintainer is asking for a ranked recommendation of the single next piece of work — feature, fix, improvement, or research — that would add the most value to the Living Roots mod for the least effort, taking account of everything already implemented. The request explicitly asks for both internal (repository) and external (current web) investigation, using subagents and background work.

## Origin & Context

- **Raised by**: [NEEDS CLARIFICATION: the request was submitted through the `/assess` pipeline without naming an author — repo owner (`lfgranja`, per `manifest.json`) or an agent acting on their behalf is not distinguishable from the input.]
- **Trigger**: A repository status audit completed immediately before this intake established the following, which is the context the request responds to:
  - Release build is clean (0 warnings, 0 errors) and the automated suite is green at **816 passed / 0 failed**.
  - `specs/002-soil-decay-compost` is **100% complete** (38/38 tasks); `specs/001-soil-health-visualization` is 10/86; `specs/003-composting-tests` is 28/117. `.specify/feature.json` currently points at `specs/003-composting-tests`.
  - The four most recent commits (2026-10-03) are fixes to the compost item-ID comparison in `ModController.OnButtonPressed` — a code path that cannot currently execute in-game.
  - The repository's own briefings already classify the gap as blocking: `specs/003-composting-tests/briefings/compost-item.md` records **"Priority: P0 — blocking · Type: Content Patcher data patch · Status: not started"**, and `composting-bin-item.md` records the bin item as **"P3 — deferred · constant is orphaned"**.
  - `manifest.json` (`0.0.1`, UniqueID `lfgranja.LivingRoots`) declares **no Content Patcher dependency**, and no Content Patcher JSON exists in the repository.

### Verified repository state at intake (recorded as context, not as a verdict)

Established by direct execution and code reading in the session preceding this intake. Listed for downstream stages to weigh; **no ranking or recommendation is made here.**

| Observation | Evidence |
|---|---|
| `Data/CraftingRecipes` is patched, registering the composting-bin recipe | `ModEntry.cs:118` |
| The `Data/Machines` branch is present but **empty** (comments only) | `ModEntry.cs:124-129` |
| No `Data/Objects` asset handler exists anywhere in the codebase | grep across `LivingRoots/` |
| `ModConstants.CompostItemId` / `QualifiedCompostItemId` / `CompostingBinItemId` are declared | `Constants.cs:63-66` |
| `SetSoilHealth` has **zero production callers** (interface declaration and implementation only) | `SoilHealthService.cs:544`, `Domain/ISoilHealthService.cs:10` |
| `SetTileHealthData` is called **only from tests** | `VisualizationService.cs:65`; `VisualizationServiceOverlayTests.cs:34` |
| `HoeFeedbackRenderer` is implemented and tested but **never instantiated in production** | `Services/Visualization/HoeFeedbackRenderer.cs`; no `new HoeFeedbackRenderer` in `LivingRoots/` |
| Render viewport and tooltip position are **hardcoded placeholders** | `ModController.cs:792` (`100x100`), `ModController.cs:795` (`Vector2(0,0)`) |
| No `Content.Load` / `I18n.` calls — assets, audio, and `i18n/*.json` are copied to output but never loaded | grep across `LivingRoots/`; `Assets/`, `i18n/` |
| A raw exception message is logged, against the project's own security rule | `ModController.cs:800` (`ex.Message`) vs. root `AGENTS.md` |
| CI runs no build/test gate under current conditions | requires `vars.BUILDDEPS_ENABLED == 'true'` and a private release asset |
| `dotnet format --verify-no-changes` could **not** be verified (exceeded a 5-minute timeout) | no result recorded either way |

## First-Glance Unknowns

- [NEEDS CLARIFICATION: Is "value" here player-facing gameplay value, unblocking value for the spec pipeline, or release-readiness value? The three rank very differently.]
- [NEEDS CLARIFICATION: Is adding a Content Patcher dependency to `manifest.json` acceptable, or must all content registration stay in C# via `AssetRequested`? The two paths differ in effort and in how much of the existing work they unlock.]
- [NEEDS CLARIFICATION: Is in-game manual verification available and affordable right now? PR template requires it; unit tests demonstrably do not cover the integration seam.]
- [NEEDS CLARIFICATION: What is the acceptance bar for "done" here — automated suite green, or demonstrably playable in-game?]
- [NEEDS CLARIFICATION: Does the 80/20 search space include CI/build/test infrastructure and the missing in-game verification loop, or only gameplay features?]
- [NEEDS CLARIFICATION: Is authoring pixel art / audio in scope for the effort accounting, given `compost-icon.md` is "not started" and no asset pipeline loads those files today?]
- [NEEDS CLARIFICATION: Who is the target player, and is there any playtest or playerval feedback to optimize against?]
- [NEEDS CLARIFICATION: Is the intent to ship `0.0.1` publicly soon, or to continue as R&D? This changes whether polish or proof-of-playability wins.]
- [NEEDS CLARIFICATION: Should the recommendation respect the existing `ROADMAP.md` progression, or is a cheaper off-roadmap item acceptable?]
- [NEEDS CLARIFICATION: `specs/001` sits at 10/86 with an active-but-unwired renderer — is that spec abandoned, deferred, or the real next target?]

## Notes for the Next Stage

- `/speckit.assess.research` should weigh external evidence the request explicitly asks for: SMAPI 1.6 conventions for registering items and machines from a code mod without Content Patcher, and comparable mods' cost of the same integration seam.
- The user's request for background subagents and extended investigation is a **process instruction for the research stage**, not a scope change to the idea itself.
