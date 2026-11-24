# Problem Definition: Mulch / Soil Cover

- **Slug**: mulch-soil-cover
- **Created**: 2026-10-30
- **Inputs used**: intake.md | research.md

## Problem Statement

Players of Living Roots have no way to *prevent* soil health decay. Soil health decays at 2/day on bare tilled tiles, and although compost can *recover* it and crop growth now *benefits* from it, nothing in the game lets a player protect a tile from decay between plantings. The only defensive action available is reactive (apply compost after decay), which is costly and unsatisfying. Without a preventive tool, soil health becomes a treadmill rather than a managed resource, and the agroecological lesson ("cover the soil") cannot be taught.

## Affected Users & Stakeholders

- **Users**: Stardew Valley players using Living Roots who leave tiles fallow between crops (e.g., early-game players with limited seeds, or players rotating beds). They watch soil health drop with no recourse beyond compost.
- **Stakeholders**: mod maintainer (Luís) — needs a low-cost, high-evidence feature to complete the "build → protect → harvest" loop and differentiate from ReGen Valley; agroecology educators — mulch is the strongest-evidence mechanic (meta-analysis: runoff −47%, soil loss −76%).

## Goals

- Give players a preventive tool (mulch) that stops or slows soil decay on tilled tiles.
- Complete the "build → protect → harvest" loop so soil health management has both offensive (compost) and defensive (mulch) actions.
- Teach the real-world agroecological practice of soil cover (Primavesi) with the strongest scientific backing available in the roadmap.
- Keep implementation cost minimal (1 object + 1 recipe + 1 hook) and fully testable without a game install.

## Non-Goals

- No new NPC, dialogue, or shop (Elena is not implemented; the mulch recipe is craftable with fiber and does not depend on Elena).
- No new crop or plantable mulch (mulch is an *item applied to soil*, not a crop — unlike the existing Nexus "Mulch" mod).
- No weed-prevention gameplay beyond the decay hook in Y1 (weed suppression is a visual/secondary effect, not a core mechanic).
- No mutation testing (Stryker.NET) — out of scope per spec 011 non-goals.
- No multiplayer live verification (roadmap D1) — mechanism only, via stubs.
- No new `.ogg` audio assets Y1 (reuse vanilla sounds).
- No SonarCloud or CI changes (handled by spec 011).

## Success Metrics

- `dotnet build` → 0 warnings, 0 errors (baseline: current).
- `dotnet test` → all existing tests pass + new mulch tests pass (baseline: 1436 passed).
- `dotnet format --verify-no-changes` → clean.
- New tests: `Data/Objects` contains `LivingRoots_Mulch` (Category −26, tags); recipe parseable; `decay *= MulchDecayMultiplier` on mulched tile (incl. Spring 0.5×/Summer 1.5× synergy); 7-day duration expires; rain does not remove; applying to non-tilled tile → `message.mulch.invalid-tile`; re-applying → `message.mulch.already`; overlay uses `MulchColor` via `IVisualizationService`.
- Acceptance: craft day 1 with fiber only; mulched tile maintains health for 7 days; `dotnet format` clean.
- `MulchDecayMultiplier` resolved to `0f` (full protection) for Y1 — simpler, more satisfying, and the balance risk is mitigated by the 7-day duration and fiber cost (10 fiber per mulch). Partial protection (`0.25f`) deferred to a balance pass if playtesting shows `0f` is overpowered.

## Cost of Inaction

If mulch is not built, soil health remains a one-sided system: the player can *lose* health (decay) and *recover* it (compost) but cannot *prevent* loss. The agroecological lesson "cover the soil" — the strongest-evidence mechanic in the roadmap — cannot be taught. The "build → protect → harvest" loop stays incomplete, and the mod's differentiation from ReGen Valley (which ships cover crops) is limited to compost alone. The 80/20 ordering stalls at step 2, and the next cheapest complete feature is never shipped.

## Open Questions

- [NEEDS CLARIFICATION: `MulchDecayMultiplier` final value — resolved to `0f` for Y1, subject to playtesting (no in-game runs exist yet).]
- [NEEDS CLARIFICATION: Elena dependency — shop sale criterion deferred; craftable recipe (fiber ×10) works without Elena.]
- [NEEDS CLARIFICATION: who creates the `mulch.png` sprite 16×16 and when? (placeholder viable with specified colors)]
- [NEEDS CLARIFICATION: 7-day duration — balanced? Configurable in a later pass?]
- [NEEDS CLARIFICATION: overlay z-order compatibility with spec 001 health overlays — to be verified during implementation.]
- [NEEDS CLARIFICATION: weed prevention scope — visual/secondary in Y1 or deferred?]
EOF
echo "written"