# Concept: Joja / Agrochemical Tradeoff (controlled MVP slice)

- **Slug**: `joja-agrochemical-tradeoff`
- **Created**: 2026-11-06
- **Recommended option**: **Option A — Mod-intercepted Joja input with legible soil cost (MVP)**

## Options

### Option A — Mod-intercepted Joja input with legible soil cost (MVP)
- **Sketch**: The player obtains a clearly identified Joja/agrochemical input (sold through the Joja framing) and can choose to use it on farm soil for a small, immediate, understandable short-term benefit. That choice is presented as a tradeoff, not a hidden trap: before use the player can tell it is an industrial shortcut with soil consequences, and after use they receive clear feedback that soil condition was harmed. The benefit is scoped so it does not silently stack with existing soil-driven quality/growth outcomes; the cost is immediate, bounded, and recoverable through the regenerative systems players already have. In shared farms, the host’s game remains the authority for the soil change in this MVP; if the slice cannot safely support a farmhand path, the experience refuses that path explicitly rather than mutating state unpredictably. Verification focuses on what can be honestly demonstrated now (host-only machine evidence) without claiming human/D1 attestation.
- **Appetite**: **small** (spec slice; implementation later expected to be small-to-medium because it reuses existing soil, application, feedback, and sync patterns)
- **Trade-offs**:
  - **Wins:** Fastest path to close the missing GDD decision loop; lowest technical risk because it avoids a new Harmony hook on vanilla fertilizer commit/probe paths; aligns with existing mod-intercepted application patterns and host-authoritative/FR-009 sync model; supports cozy legibility with textual/procedural feedback and no mandatory new assets; makes Gate 3 claims honest within current runner/D1 constraints.
  - **Sacrifices:** Does not behave as a true vanilla fertilizer-slot interop in MVP; may feel less “native” than occupying the vanilla fertilizer mechanic; farmhand-initiated multiplayer flow may be limited/refused until a follow-up protocol exists; balance/feel will be first-pass and require playtest calibration.
  - **Risks/unknowns:** The exact short-term benefit may still be too weak, confusing, or accidentally compositional with 014/015 if not specified tightly; applying a penalty to previously unmeasured soil needs an explicit baseline rule; i18n/tag/content details can create small integration inconsistencies if not made explicit.
- **Rabbit holes**:
  - Chasing vanilla fertilizer-slot parity (`HoeDirt.fertilizer`, tier heuristics, timing) inside the MVP.
  - Designing a full farmhand intent/ack/idempotency protocol and D1 evidence plan instead of scoping host-authoritative MVP behavior.
  - Over-modeling penalty timing (stacking, cooldowns, delayed state) beyond one immediate tunable rule.
  - Asset/content production (art, audio, full Joja economy/narrative arc) unrelated to the core decision.

### Option B — Vanilla-fertilizer-interop Jojo input (Harmony commit-path hook)
- **Sketch**: The player uses a Joja/agrochemical fertilizer that participates in the vanilla fertilizer application flow, so the experience feels native to Stardew’s fertilizer system. The mod observes or extends the real application outcome and attaches a soil-health cost to that confirmed application, while preserving the short-term payoff players expect from fertilizer use.
- **Appetite**: **medium** (spec) / **medium-to-large** implementation risk due to decompile verification, probe-vs-commit semantics, compatibility, and MP behavior
- **Trade-offs**:
  - **Wins:** Highest mechanical familiarity if it truly interoperates with vanilla fertilizer behavior; potentially cleaner mental model for players who think in vanilla fertilizer terms.
  - **Sacrifices:** Much higher scope/risk for the first destructive slice; depends on exact SDV 1.6.14+/1.6.15 method semantics and safe hook points; can conflict with vanilla fertilizer slot, existing 014/015 tier heuristics/composition, and multiplayer commit paths.
  - **Risks/unknowns:** Decompile-verified signatures/commit points are required; probe/validation paths could trigger false penalties; custom item IDs may not produce desired vanilla effects without deeper patching; Harmony surface area and compatibility burden increase.
- **Rabbit holes**:
  - Decompiling/reverse-engineering multiple vanilla call paths before proving the MVP value.
  - Expanding 015 harvest-seam style work into a second Harmony seam without a narrow contract.
  - Solving multi-harvest/vanilla quality/timing edge cases that are not needed to validate the core tradeoff.

### Option C — Do nothing / defer the destructive slice
- **Sketch**: Keep the current state: soil stewardship remains positive/rewarding through 014/015, and the Joja/agrochemical tradeoff is deferred while the project focuses on operational follow-ups (D1, human `verified` evidence, 015 T110/deferred items) or other roadmap work.
- **Appetite**: **small** (decision/spec deferral) but **high opportunity cost** for product direction
- **Trade-offs**:
  - **Wins:** Avoids design and verification risk now; preserves focus on evidence/promotion work and avoids adding new mechanics before D1/human verification.
  - **Sacrifices:** Leaves the core GDD decision loop incomplete; positive-only soil effects continue to undersell the mod’s differentiating tradeoff; the 80/20 opportunity identified by intake/research is missed.
  - **Risks/unknowns:** Deferred design may become harder after additional systems accumulate; team may keep paying operational cost on 014/015 while product-level strategic depth remains unchanged.
- **Rabbit holes**: Repeated re-analysis of 014/015 status with diminishing returns instead of specifying the next bounded product decision.

## Recommendation

**Recommend Option A** and **do not recommend Option B or C** for this slice.

Option A best serves the stated goals with the smallest defensible appetite: it creates a bounded path to specify the missing destructive choice (Goal 1), makes legible/non-punitive feedback feasible without new assets (Goal 2), preserves host-authoritative/validated/honest verification constraints (Goal 3), and is small enough to specify now by reusing existing soil/application/feedback/sync concepts (Goal 4). It also respects the accepted non-goals: mod-intercepted MVP, no Harmony fertilizer hook, no monoculture, no D1/`verified` claims from host-only evidence, and no history rewrites.

Option B may be valuable later if native fertilizer interop becomes essential, but only after the decompile gate and a proven core tradeoff. Option C is safe operationally but does not solve the problem defined in `problem.md`.

## Out of Scope (for the recommended option)

- Monoculture and any other destructive/rotation/history-based mechanics.
- Harmony hooks on vanilla fertilizer application/commit/probe paths in the MVP.
- True vanilla `HoeDirt.fertilizer` slot interop as a required MVP behavior.
- Full farmhand intent/ack/idempotency protocol, real-farmhand D1 convergence, and any `verified`/production-ready multiplayer claim without evidence.
- Full Joja economy/content arc, shop/narrative expansion beyond what is needed to frame the single controlled input.
- Mandatory new art/audio assets; large UI/tutorial systems.
- Rewriting historical 014/015/010 specs, bundles, task history, or promoting evidence beyond attestation.
- Final balance tuning claims; only a first-pass tunable starting point is in scope for later spec/implementation.

## Assumptions to Validate

- A **single, small, clearly explained short-term benefit** can feel tempting enough to create a real decision without requiring vanilla fertilizer-slot interop or double-counting 014/015 outcomes.
- An **immediate, bounded, tunable soil penalty** (with explicit handling for previously unmeasured/`NaN` soil) will be legible and recoverable enough to fit the mod’s cozy/non-punitive intent.
- The existing **mod-intercepted application + host-authoritative mutation + FR-009 snapshot sync** concepts are sufficient to specify a safe MVP boundary; any farmhand path that cannot preserve authority/inventory/state safety can be explicitly refused.
- **Text/procedural/localized feedback** (item description plus before/after cues) is sufficient for MVP telegraphing without new assets.
- A future **decompile gate** is an acceptable constraint: deferring vanilla fertilizer-hook work does not block the core tradeoff from being specified and later validated at host-only scope.
- Playtest can calibrate the first-pass penalty/tier-boundary impact; the assessment should not pretend the initial number is balanced.
