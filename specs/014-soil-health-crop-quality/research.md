# Phase 0 Research: Soil Health → Crop Quality

**Feature**: `014-soil-health-crop-quality` | **Branch**: `014-soil-health-crop-quality`
**Based on**: `spec.md` (clarify session 2026-11-01, 7 questions resolved)

---

## Summary

All `NEEDS CLARIFICATION` markers are resolved. Evidence was gathered via paired internal codebase deep-dives + online research for each candidate, documented in `/tmp/opencode/research-014/`.

## Resolved Decisions

| # | Topic | Decision | Rationale | Evidence |
|---|-------|----------|-----------|----------|
| A1 | Inventory full at harvest | Extra-harvest item dropped as ground debris (vanilla); main item vanilla-blocked (red message, crop stays) | Vanilla `Crop.harvest` blocks main item on full inventory but always spawns extras as debris; "Nada se perde" (GDD); dislodgement/cooking-waste debris pattern | research-c1.md (decompiled `Crop::harvest`), internal-c1.md |
| A2 | Extra-harvest item quality | Normal quality (0); only primary item gets boosted roll | Decompiled `Crop.harvest` (1.6.15) creates extras as fresh `Object` with default `Quality = 0`; no per-extra re-roll | research-c2.md, internal-c2.md |
| A3 | NaN/Infinity (Unknown) harvest | No modification (vanilla roll, no forced 0, no +0.08, no feedback) | NaN is absence-of-measurement sentinel; growth modifier skips non-finite (`ModController` `continue`); overlay treats Unknown as gray | internal-c3.md |
| A4 | Feedback text localization | Literal text (existing floating-text pattern; no new i18n keys) | Compost/mulch use literal `sprite.text`; spec 005 A4 zero new i18n keys; refusal messages keep existing i18n keys | internal-c2.md §2.1/§2.5 |
| A5 | Pest clamp integration | Optional null-safe `IPestStateProvider` (null → no clamp); contract finalized in pest spec | Pest system is separate spec (Q7); avoids hard dependency; no new constant beyond `PestQualityPenalty` | spec.md Assumptions |
| B1 | Feedback conditionality | "Living Soil" shows only when outcome is enhanced (silver/gold/iridium and/or extra harvest); normal-quality no-extra → no feedback (silence = vanilla) | Matches spec 005 Q3 "when tier modifies harvest outcome"; keeps "silence = vanilla" contract; makes tests deterministic | internal-c2.md §2.4, §6 |
| B2 | Feedback co-occurrence | Both messages as two separate sprites broadcast together via `Game1.multiplayer.broadcastSprites(location, sprite1, sprite2)` | Vanilla harvest broadcasts two sparkle sprites; MP-safe; keeps two i18n keys distinct and testable; no new i18n key | internal-c2.md §2.4, §3 |

## Alternatives Considered

- **Inventory full → "not produced" (skip)**: rejected in favor of debris fallback because vanilla spawns extras as debris and the mod's "Nada se perde" principle forbids silent destruction; compost collection's retain-state contract is a different (collection) path and remains unchanged.
- **Extra-harvest quality → boosted**: rejected because it would require intercepting item creation/debris and setting `Quality` on each extra, increasing invasiveness and partial-delivery duplication risk (cf. CollectCompost hazard); vanilla extras are normal quality.
- **NaN → Poor (force 0)**: rejected because NaN is the absence-of-measurement sentinel (distinct from measured 0); would penalize unmeasured tiles and diverge from growth modifier's skip-non-finite behavior.
- **Feedback shows on every Living harvest**: rejected (B1 = A) because it decouples feedback from actual enhancement and weakens "silence = vanilla."
- **Single sprite with precedence**: rejected (B2 = A) in favor of both sprites because it loses attribution of the non-precedent outcome; vanilla already broadcasts two sprites.

## Deferred Prerequisites (not ambiguities)

1. `SC-008` rollback verification — absolute blocker for yield activation.
2. `GamePath` spike — verify `Crop.harvest`/`Object.Quality` member names.
3. Gate 3 in-game SMAPI session — capture 3 signals.

These do not block plan creation or design; they gate yield *activation*.
