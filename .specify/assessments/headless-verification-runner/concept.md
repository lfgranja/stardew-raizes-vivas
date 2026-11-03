# Concept: Headless Verification Runner for Gate 3

- **Slug**: headless-verification-runner
- **Created**: 2026-11-02
- **Recommended option**: Option B — Incremental MVP (3-signal capture first)

## Options

### Option A — Full Spec 010 (161 tasks, full harness)
- **Sketch**: Implement the entire spec 010 design: bash orchestrator (`verify-scenario.sh`), two C# companion projects (`LivingRoots.Verification`, `LivingRoots.Verification.Runner`), public mod API (`ILivingRootsModApi`), scenario JSON specs, screenshot/video capture, CI integration, and full attestation pipeline. Delivers a complete, self-hosted verification platform.
- **Appetite**: large (months)
- **Trade-offs**: Wins: comprehensive automation, reusable for all future features, CI-integrated. Sacrifices: massive scope (161 tasks, 50+ bash libs, 2 C# projects) vs. Principle V (YAGNI); high risk of building infrastructure before validating the core loop; depends on game install in CI (self-hosted or gamepath.zip).
- **Rabbit holes**: C# companion mod API design; scenario schema and fixture authoring; CI runner provisioning; screenshot baseline management under software GL; determinism claims.

### Option B — Incremental MVP (3-signal capture first)
- **Sketch**: Build the smallest thing that captures the 3 Gate 3 signals (SoilHealthState JSON delta + Crop.phase/phaseDays delta + SMAPI log) for the harvest-quality loop only. Reuse the existing `scripts/smoke-validation.sh` infrastructure (Xvfb + SMAPI + log gate). Add a scenario JSON spec, a minimal driver that plays the scenario (till → compost → harvest → save → reload), and a capture script that diffs save files and SMAPI log. Defer screenshots, video, CI, and the C# companion to later.
- **Appetite**: medium (weeks)
- **Trade-offs**: Wins: delivers "verified" evidence for the most critical feature (harvest-quality) with ~10x less scope; reuses proven smoke infrastructure; low risk; incremental value. Sacrifices: no screenshot/video capture; no CI integration; no reusable platform for other scenarios; manual scenario authoring.
- **Rabbit holes**: save-file diffing reliability; timing of crop growth (needs day advancement); Xvfb + MonoGame rendering quirks; scenario determinism.

### Option C — Do nothing / manual Gate 3
- **Sketch**: Continue manual in-game sessions (30 min each) to capture the 3 signals. No new code. Document evidence in `verification-evidence/`.
- **Appetite**: small (days, recurring)
- **Trade-offs**: Wins: zero implementation cost; immediate. Sacrifices: not reproducible; unsustainable per change; blocks "verified" automation; does not scale to 19 unimplemented features + 12 mid/end-game items.
- **Rabbit holes**: human availability; evidence quality varies; no regression safety net.

## Recommendation

**Option B — Incremental MVP.** The problem is that Gate 3 has 0 runs and the central promise is unproven. Option B delivers verifiable evidence for the most critical feature (harvest-quality) with ~10x less scope than Option A, reuses the proven smoke infrastructure (spec 008), and avoids the YAGNI risk of building a 161-task platform before validating the core loop. Option A is the long-term vision but should follow only after Option B proves the value. Option C is unacceptable as a permanent strategy (unsustainable, no regression safety).

## Out of Scope (for the recommended option)

- Screenshot/video capture (deferred to Option A or a separate spec).
- CI integration (GitHub Actions) — deferred.
- C# companion projects (`LivingRoots.Verification`, `LivingRoots.Verification.Runner`) — deferred.
- Public mod API (`ILivingRootsModApi`) — deferred.
- Scenario authoring for non-harvest-quality features — deferred.
- Deterministic replay (Stardew Valley is not deterministic).

## Assumptions to Validate

- The existing smoke infrastructure (`scripts/smoke-validation.sh`) can be extended to drive scenario steps (till, compost, harvest, save, reload) without a full rewrite.
- Save files can be diffed reliably with the existing comparator (float tolerance 0.0001f, NaN ignored, missing keys = 0).
- The game can advance days under Xvfb without human input (via SMAPI console commands or automated bot).
- Software GL (`llvmpipe`) rendering is sufficient for the 3 signals (save diff + phase delta + log) — screenshots are out of scope for MVP.
- The mod's `RegisterEvents` NullReferenceException in headless mode (pre-existing) does not block the scenario execution (the mod still loads and the patch is registered).