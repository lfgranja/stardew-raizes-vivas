# Concept: What to Build Next

- **Slug**: next-feature-80-20
- **Created**: 2026-10-03
- **Recommended option**: Option A — Make It Playable First

## Options

### Option A — Make It Playable First

- **Sketch**: Close the content-registration gap so a human can launch the mod and exercise a real
  loop: craft the composting bin, add organic waste, wait for maturation, collect compost, apply it to
  a tilled tile, and *see* the soil health change. This means registering the compost item and the bin
  as actual game objects, giving the bin real machine behaviour instead of the currently empty branch,
  and connecting soil health data to the visualization layer so the overlay actually renders. The
  player's experience changes from "nothing happens" to "a complete, if plain, gameplay loop exists".
- **Appetite**: **small–medium** (days to a couple of weeks). *Confidence: low* — see Assumptions. The
  internal audit produced LOC estimates (30–50 for content registration, 40–80 for the visualization
  data pipeline, 40–80 for texture initialization) but explicitly graded them ASSUMPTION-grade, and no
  part of this has been touched in a real game.
- **Trade-offs**:
  - **Wins**: the only option that moves the "playable surface" metric off zero; produces the first
    artifact that can be verified by a human rather than a test runner; satisfies the premise of
    Principle VI's own Gate 3, which requires a second player observing consistent values — impossible
    to do against nothing; converts 816 misleading green tests into tests that mean something; makes
    every later prioritization decision cheaper because the code path finally executes.
  - **Sacrifices**: makes no progress toward multiplayer, which is the capability Principle VI actually
    mandates; does not address the two latent multiplayer correctness bugs (N-times daily decay, silent
    state divergence); leaves the eight stale specs unaddressed; risks looking like the mod is "not
    prioritizing the constitution".
  - **Key risks**: the real `Data/Objects` field format for Stardew 1.6 is unverified; Content Patcher
    might turn out to be materially simpler than hand-rolling asset edits; the visualization pipeline
    may surface deeper breakage once it executes instead of silently swallowing an exception.
- **Rabbit holes**: the visualization layer has never run in production — `Initialize()` is never
  called, so its texture accessor throws and the exception is swallowed at `ModController.cs:800`.
  Turning it on may reveal a cascade of latent defects. `ModController.cs` is 1,178 lines and touches
  everything; scope discipline there will be hard. Asset loading, if pursued, drags in a whole
  content-pipeline that does not exist yet.

### Option B — Multiplayer First

- **Sketch**: Make the domain state host-authoritative before anything is built on top of it. Gate
  persistence on host context so farmhands stop silently forking their own copies, fix daily processing
  so it does not run once per connected player, and decide the read/write model for farmhands. The
  player experience is unchanged today — two people can finally play the same farm consistently instead
  of silently disagreeing about soil health.
- **Appetite**: **large** (weeks to months). *Confidence: medium* — the platform work is well understood
  (SMAPI already throws on non-host save writes), but the test infrastructure does not exist yet and
  the farmhand-state question is unresolved.
- **Trade-offs**:
  - **Wins**: directly serves the constitution's non-negotiable mandate; addresses the game-breaking
    correctness bugs directly; the state model is already farm-keyed, which is genuinely favourable
    groundwork that most codebases would lack.
  - **Sacrifices**: builds on a mod nobody can play, so none of it can be end-to-end verified — Gate 3
    requires two connected players observing real values; leaves the playable surface at zero for
    longer; the highest risk of the three to produce something that must be substantially reworked.
  - **Key risks**: the `WriteJsonFile` versus `WriteSaveData` host-authority semantics are unconfirmed
    for the pinned SMAPI version, so the size of the correctness problem is not yet known; multiplayer
    test doubles do not exist and would need an estimated 200–400 LOC of new test scaffolding before
    TDD is even possible; the read/write model question (may a farmhand write at all?) is unanswered
    and materially changes scope.
- **Rabbit holes**: live state sync versus save-time sync is a fork in the road that roughly doubles
  the work if live turns out to be required; farmhands can be in different locations than the host,
  which touches the daily-processing design; peer connection lifecycle adds a state machine that the
  current design has no concept of.

### Option C — Repair the Signals, Build Nothing

- **Sketch**: Change no gameplay behaviour at all. Instead produce a verified in-game capability
  inventory, correct the eight spec/plan documents that still declare single-player scope, fix the
  README's inaccurate feature claims, and add a check that fails when the suite is green but no content
  is registered. The "product" is trustworthy reporting: a maintainer who can finally tell what works.
- **Appetite**: **small** (a day or two).
- **Trade-offs**:
  - **Wins**: cheapest option by a wide margin; directly serves the stated problem, which is about
    *untrustworthy signals* rather than missing features; removes the eight-document governance
    contradiction; leaves the codebase honestly labelled instead of falsely green.
  - **Sacrifices**: the mod remains unplayable; the maintainer and his spouse get nothing usable;
  - leaves Principle VI entirely unserved; risks being experienced as paperwork.
  - **Key risks**: documentation corrections decay if nothing changes underneath them; an inventory is
    a snapshot, not a guarantee.
- **Rabbit holes**: reconciling eight spec documents invites rewriting their task lists, which drags
  the whole speckit history with it.

## Recommendation

**Option A — Make It Playable First**, with the documentation half of Option C folded in as a
cheap rider (correct the stale specs and README claims; it costs almost nothing once someone has
actually played the thing and can write down what they saw).

Rationale against the problem's stated goals and metrics:

1. It is the only option that moves the **playable surface** metric from 0 to non-zero, which is the
   problem's central condition — every other success metric is unmeasurable while it stays at zero.
2. It is a **precondition for satisfying the constitution**, not an alternative to it. Principle VI's
   Gate 3 demands verification with a second player observing consistent values. You cannot verify
   multiplayer correctness against a mod that cannot be launched. Choosing B first means shipping the
   mandate's prerequisite backwards.
3. It is the cheapest path to a **trustworthy signal**, which is the actual problem. One human playing
   the loop produces more reliable prioritization input than any amount of further test-count
   analysis.
4. It is the smallest appetite of the two build options, and Option C's rider removes the "ignored
   governance" objection at near-zero marginal cost.

**Honest counterweight**: this does not advance the multiplayer mandate by one line of code, and if
the maintainer's intent is "start multiplayer now", A is the wrong recommendation. That is exactly
open question 5 in `problem.md` (does Principle VI require multiplayer *now*, or only that nothing new
be single-player-only?) and it is a judgement call, not an evidence question. It should be settled
before `/speckit.assess.decide` commits.

## Out of Scope (for the recommended option)

- **Multiplayer synchronization itself.** Stays deferred, to be specified separately under Principle VI.
- **Custom sprites, audio, and i18n wiring.** The placeholder assets stay unloaded; the gameplay loop
  uses vanilla visuals and sounds.
- **The `HoeFeedbackRenderer` refactor.** It is dead code, but deleting or wiring it is cleanup, not
  value. Record as deferred rather than absorb it.
- **The visual overlay's polish.** Making it *render* is in scope; making it look good is not.
- **Refactoring `ModController.cs` beyond what the change strictly requires.** Its size is a known
  hazard, not this work item.
- **Everything in `ROADMAP.md`** — Elena, heirloom seeds, crop rotation, polyculture, and the entire
  mid and endgame remain design documents with no code.
- **The remaining seven stale specs beyond the scope corrections** — reconciliation is a rider here,
  not the project.
- **CI gate enablement.** A real problem (no automated test gate currently runs) but orthogonal.

## Assumptions to Validate

- **Content Patcher is genuinely unnecessary for this.** The research found `AssetRequested` +
  `AsDictionary<string, string>()` is the idiomatic in-code path and is already used for
  `Data/CraftingRecipes` at `ModEntry.cs:118`. *Confidence: medium* — the supporting citations came
  from a host not on the safe list and were marked `[UNVERIFIED]`. **Validate first**: if a small
  Content Patcher JSON is materially cheaper than hand-rolled asset edits, the appetite and the design
  both change.
- **The Stardew 1.6 `Data/Objects` schema for a placeable/machine object is understood well enough to
  write correctly without iterating in-game.** Unverified. *Confidence: low.*
- **Turning on the visualization layer surfaces a bounded set of defects, not a cascade.**
  *Confidence: low* — it has never executed in production and currently throws a swallowed exception.
- **The "small–medium" appetite is real.** All supporting LOC figures are ASSUMPTION-grade from a
  read-only audit with no implementation trial. Treat the first task as a spike, not a commitment.
- **Single-player play is unaffected by the host-local persistence model.** Trivially true today, but
  becomes load-bearing the moment multiplayer starts — should not be quietly "fixed" as part of this.
- **A second machine or local second client is not required for this option**, only for Option B's
  verification. If unavailable, that further favours A.
