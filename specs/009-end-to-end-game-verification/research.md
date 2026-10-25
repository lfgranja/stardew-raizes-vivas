# Research: End-to-End In-Game Verification (Gate 3)

**Feature**: 009-end-to-end-game-verification
**Date**: 2026-10-25
**Consolidated from**: `.specify/assessments/end-to-end-game-verification/` (intake.md, problem.md, research.md, decision.md, research-completa.md) + 6 clarification rounds

## Research Questions & Findings

### R1: Which SMAPI event fires the growth modifier?

**Decision**: `DayEnding` (not `DayStarted`).

**Evidence**: SMAPI `SCore.cs:1405` calls `EventManager.DayEnding.RaiseEmpty()` before `action()` (vanilla `newDayAfterFade`/`Crop.newDay`). `DayStarted` fires after save/load, post-growth. Using `DayStarted` causes composition with vanilla growth instead of replacement.

**Alternatives**: `DayStarted` (current, stale) — rejected. `DayEnding` — accepted.

**Source**: SMAPI source code analysis (Round 4 Clarify, autonomous).

### R2: Growth modifier math — ±1 vs multiplicative?

**Decision**: Multiplicative on cached base `phaseDays` (Pobre 1.15×, Moderado 1.00×, Saudável 0.85×).

**Evidence**: Current ±1 is semantically inverted (poor +1 accelerates, healthy −1 delays). Multiplicative is the vanilla-blessed path (Speed-Gro uses `phaseDays` multipliers). Red-phase tests (`GrowthModifierIntegrationTests`) already encode multiplicative expectations.

**Alternatives**: ±1 additive (current) — rejected (semantically inverted, indistinguishable from vanilla). Multiplicative — accepted.

**Source**: Codebase analysis + SMAPI vanilla behavior research (Round 3/4 Clarify).

### R3: Host guard & idempotency?

**Decision**: Add `if (!Context.IsMainPlayer) return;` at entry; track processed day key (`season:dayOfMonth:year`) per location.

**Evidence**: `DayStarted`/`DayEnding` fires on farmhands too. Without host guard, farmhands execute the modifier (violates Constitution VI). Without idempotency, save/load re-fires events, double-applying the modifier.

**Alternatives**: No guard (current) — rejected (multiplayer violation). Host-only — accepted.

**Source**: Constitution VI + SMAPI event behavior (Round 3 Clarify, autonomous).

### R4: Location scope — currentLocation only vs all farm locations?

**Decision**: Refactor to `ApplySoilGrowthModifier(string locationName)`; call within `foreach (var locationName in farmLocationNames)` loop.

**Evidence**: Current code only scans `Game1.currentLocation`, so crops in `Farm`/`Greenhouse` (where host is not standing) are never processed. Day-start loop already iterates farm locations for composting bins and soil decay.

**Alternatives**: Current location only (current) — rejected (silently starves non-occupied locations). Full iteration — accepted.

**Source**: Existing `OnDayStarted` pattern (Round 3 Clarify, autonomous).

### R5: SC-009 formalization?

**Decision**: SC-009 is the per-criterion manual verification protocol (`VERIFICATION_PROTOCOL.md` §3). Formally defined in spec 009 Success Criteria.

**Evidence**: SC-009 referenced 70+ times but never formally defined. Not an IEEE/ISO/SMAPI standard. Overloaded across 4 specs. Research confirms no external standard uses this label.

**Alternatives**: Keep as informal shorthand — rejected (implementation ambiguity). Formalize as success criterion — accepted.

**Source**: Codebase analysis + research on verification protocols (Round 6 Clarify, autonomous).

### R6: 3-signal triangulation retention?

**Decision**: Retain 3 signals (observation + artifact + log). No reduction or extension.

**Evidence**: Triangulation methodology (BetterEvaluation) — sources with opposite weaknesses converge → robust conclusion. Removing any signal opens false-positive gap. Adding 4th is YAGNI until automation framework.

**Alternatives**: 2-signal — rejected (can't confirm gameplay responded). 4-signal — deferred (YAGNI).

**Source**: Research-scientific-subagent-response.md + triangulated-verification-gate3.md.

### R7: Automation scope?

**Decision**: Only save comparator in scope. Reflector, structured logger, scripted runner deferred until 2+ features reuse (Regra dos Três).

**Evidence**: YAGNI. Full automation cost not justified for single feature. Manual protocol (SC-009) is primary path.

**Alternatives**: Full automation (COMPLETA) — rejected (premature). Manual-only — rejected (save comparator is low-cost structural evidence). Save comparator only — accepted.

**Source**: decision.md + research.md (assessment artifacts).

### R8: "verified" terminology?

**Decision**: "verified" = "maintainer-verified with multi-signal evidence" (1 session + 3 signals). "independently verified" = future goal.

**Evidence**: Honest scoping. Evidence multi-signal with documented protocol exceeds community mod practice. IEEE/ACM/NIST require replication, but requiring it now blocks Phase 3/4/5 without proportional gain.

**Alternatives**: Require replication (IEEE/ACM/NIST) — rejected (blocks progress). Current definition — accepted.

**Source**: Round 4/5 Clarify (maintainer decision).

## External Research Sources

| Source | Classification | Contribution |
|--------|---------------|--------------|
| SMAPI Wiki (stardewvalleywiki.com) | Community docs | Standard SMAPI workflow: build → install → launch → test → review console/log |
| BetterEvaluation.org / Monitoring & Evaluation Studio | Research methodology | Triangulation: cross-verify with >1 source; unrelated weaknesses → robust conclusion |
| Huang, Meyer, Oriol (CACM 2026) | Peer-reviewed | Tests + proofs + contracts complementary; no single method sufficient |
| ITEA Journal / NIST AI RMF | Standards-aligned | Independent judgment; evidence adequacy > quantity |
| Astaqc (2026) | Industry analysis | False confidence from passing tests; assertion gap allows wrong values to ship |
| Tricentis (BVT) | Industry best practice | Build verification is litmus test, not exhaustive correctness |
| ReGen Valley (Nexus 39740) | Competitor | Manual verification only; no E2E automation |
| IEEE 1012-2024, ISO/IEC 29119, NIST AI 100-1 | Standards | V&V activities, test case specification, testing framework |

## Key Conclusions

1. **No automated E2E framework exists for SMAPI mods** — manual verification is the ecosystem consensus.
2. **3-signal triangulation is scientifically defensible** — each signal covers the others' blind spots.
3. **SC-009 is a project-specific label** — needs formalization (done in Round 6).
4. **Growth modifier must be corrected before verification** — current ±1 is inverted; must be multiplicative on `phaseDays`.
5. **Host guard + idempotency are mandatory** — Constitution VI requires host-authoritative execution.
6. **All farm locations must be scanned** — current `currentLocation`-only misses Farm/Greenhouse crops.
7. **Save comparator is the sole automation component** — YAGNI for the rest.