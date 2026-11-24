# Problem Definition: Soil Health → Crop Growth

- **Slug**: soil-health-to-crop-growth
- **Created**: 2026-10-15
- **Inputs used**: intake.md | research.md | user criteria table (impact/effort/technical/validation + minimal scoping)

## Problem Statement

The mod measures, displays, and lets players change per-tile soil health through compost — but that value never affects how the farm plays. A player who improves soil from 0% to 100% sees the overlay shift from red to green, yet harvest quality, growth speed, and extra-yield rolls stay exactly vanilla, with no observable change on the tile. The core concept promises three tiers of gameplay (Poor / Medium / Living) and all 18 dependent mechanisms; only 2 features and 4 of 13 promises exist in code, and zero of them change crop behavior.

## Affected Users & Stakeholders

- **Users (primary)**: The maintainer (only verified user of this mod — builds/tested, never executed in-game; save `MDMV_424795694` from 2026-01-06 contains zero `LivingRoots` state, SMAPI log directory empty). The user's criteria table treats impact/effort/validation specifically for this maintainer-led project.
- **Stakeholders — maintainer / funder**: The assessment pipeline (`.specify/assessments/next-feature-80-20/`) produced a `go` verdict (2026-10-03) with Option A (Make It Playable First), meaning this problem is the approved next step — not a speculative request.
- **Stakeholders — future players / end users**: [NEEDS CLARIFICATION] No external user signal exists (no Nexus comments, no support tickets, no forum posts specifically asking for "soil health → crop growth"). The competitive product `ReGen Valley` (Nexus 39740, 2025) proves market appetite for per-tile soil health affecting growth/quality — but who wants *this* mod's version specifically is unknown.
- **Stakeholders — downstream specs**: Spec 004 (playable compost loop, 65/67 complete, 2 open as verification-only) and Spec 001 (visualization, 10/86 marked, functionality complete but checklist stale) depend on this feature for their own success criteria (`SC-001` through `SC-010` of spec 004 reference the full loop including visible soil change after compost).

## Goals

- **Primary**: The soil-health metric must meaningfully change crop outcomes, so the player experiences the loop (apply compost → soil improves → crop responds) rather than just observing a number.
- **Secondary**: The change must be achievable with zero new assets / items / recipes, using the existing `DayStarted` scan + persistence, so effort stays at the user's stated "low-moderate" level.
- **Tertiary (constraint)**: The change must compose with vanilla modifiers (Speed-Gro, Agriculturist, `IsPaddyCrop`, `RegrowDays`) without double-dipping, and must be verifiable without asserting on pixels/colors (pure logic + vanilla-parity test).
- **Quaternary (constraint)**: The change must not break the existing compost loop (FR-005 to FR-019) — the rollback bug in `CollectCompost` must be fixed first.

## Non-Goals

- **Not** a full agroecological simulation — long-term rotation benefits (21–52 years) cannot be simulated in 28-day seasons; model as abstraction, not physics.
- **Not** a redesign of the visualization layer — overlay/tooltip/hoe feedback (spec 001) remain as-is; this problem only connects existing state to gameplay.
- **Not** multiplayer synchronization itself — Principle VI requires host-authoritative rolls, but the sync transport (roadmap D1) is separate debt.
- **Not** teaching material (Three Sisters' overstatement, insect hotels' negative evidence) — those belong to specific future specs and must be handled with documented caveats, not as hidden bonuses.
- **Not** fixing the 7 proprietary DLLs in `.gamepath/` — licensing/blocker, not a feature gap.
- **Not** a redesign of soil-health bands — the discrepancy between `ROADMAP.md` (0-30/31-70/71-100) and `spec-001` ([0,34)/[34,67)/[67,100]) must be resolved, but the choice of which scale is the user's/decision-maker's, not the problem's.

## Success Metrics

| Metric | Baseline (today) | Target (if solved) | Type | Source / note |
|---|---|---|---|---|
| Soil health has effect on growth speed on Poor/Live tiles | 0 (no code reads it at growth time) | Multiplicative adjustment via `Crop.newDay` or `HoeDirt.GetFertilizerSpeedBoost()` | Measurable | Must not change Medium tier (parity with vanilla) |
| Soil health affects harvest quality | 0 | Roll adjusted by tier (Poor: no stars → quality 0; Living: enhanced roll) | Measurable | Requires `Crop.harvest` hook; must compose with vanilla roll |
| Soil health affects extra-yield chance | 0 | Per-tile roll in harvest or data entry, depending on spec | Measurable | `Data/Crops.ExtraHarvestChance` is global — per-tile requires runtime |
| Player observes loop end-to-end (apply → see change) | Unverified — 0 runs in game | Confirmed by manual gate (T034 / SC-008) | Qualitative | The only verification path; requires launch |
| Band definition is single and locked | Two conflicting definitions | One scale adopted, referenced by spec, constant, and test | Measurable | `HealthCategory.cs` + `ROADMAP.md` + spec must agree |
| `CollectCompost` rollback correct | Hidden (output=1 only) | Verified by full-inventory multi-item test | Measurable | Must come before any yield-increase |

All metrics except the last manual one are pure-logic and verifiable by `dotnet test`; the manual gate (launch + observe) is unavoidable because the hook touches live `Crop.phaseDays`.

## Cost of Inaction

If this is never built:
- The 2 of 21 roadmap features remain the only ones ever delivered; the 19 unimplemented features (including every progression mechanic) have no reason to exist, because the one mechanism that connects soil to play is missing.
- The assessment stream (`.specify/assessments/next-feature-80-20/` → this intake/research/define) produces a decision (`go`) that never converts to gameplay — the pipeline works, the product doesn't.
- The competitive product (`ReGen Valley`) demonstrates that the premise is viable; not building it is a deliberate choice to stay at a lower level of coherence than what's possible with the same platform.
- The specific feature is not lost — all 31 briefings document the design. The cost is not design debt; it is **the gap between measurement and experience** that makes the rest of the roadmap feel like decoration.
- The 7 proprietary DLLs in `.gamepath/` and the broken `.github/workflows/protected-paths.yml` are pre-existing; they are not caused by this feature, but they must be repaired before any live-state change, because the build/test evidence must be trustworthy before touch.

## Open Questions (carried from intake/research; must resolve before `/speckit.assess.define` can finalize)

- [NEEDS CLARIFICATION] **Hook path choice** (spike): `Crop.newDay` public fields (no new dependency, needs ordering verification with vanilla growth) vs. `HoeDirt.GetFertilizerSpeedBoost()` patch (durable, requires Harmony / new package)? The user's criteria prefer the lower-effort path; the briefing `soil-tier-outcomes-harvest-feedback.md:5` flags both.
- [NEEDS CLARIFICATION] **Band reconciliation**: Adopt `ROADMAP.md` 0-30/31-70/71-100, or `spec-001` half-open `[0,34)/[34,67)/[67,100]`? Both are documented; the user's table references the ROADMAP version explicitly (`0-30 / 31-70 / 71-100`); the briefings (`soil-tier-outcomes-harvest-feedback.md:18`) flag the discrepancy.
- [NEEDS CLARIFICATION] **Extra harvest mechanism**: Per-tile roll in `Crop.harvest` (requires hook at harvest time, host-authoritative) or global `Data/Crops.ExtraHarvestChance` edit (simpler, not per-tile)? The briefing explicitly says global data cannot give per-tile behavior.
- [NEEDS CLARIFICATION] **Play-test gate for live state change**: How is manual verification of `Crop.phaseDays` change recorded (T034 / SC-008)? With no SMAPI log and no previous in-game execution, the baseline is "it didn't run" — the first run is the gate.
- [NEEDS CLARIFICATION] **`ReGen Valley` interaction / multi-mod**: If both mods apply multiplicative growth adjustments, what is the composition rule? The user's criteria do not mention multi-mod; Constitution Principle VI requires it for multiplayer, not for single-mod interaction — but the interaction is real.
- [NEEDS CLARIFICATION] **File-system / git / CI repair**: `git add` blocked by `btrfs` inode exhaustion; 7 proprietary DLLs committed to `origin/dev`; `.github/workflows/protected-paths.yml` calls missing script; `COMPLETION_SUMMARY.md` false (26 failing → 0). Should feature development wait until environment repaired? (Recommendation: yes — plumbing commits work, but feature branch needs reliable index.)
