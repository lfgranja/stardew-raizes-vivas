# Problem Definition: Prioritizing Work Without Trustworthy Signals

- **Slug**: next-feature-80-20
- **Created**: 2026-10-03
- **Inputs used**: intake.md yes | research.md yes | user input yes

## Problem Statement

The project cannot currently identify its highest-value next step because none of its decision
signals are trustworthy: the automated suite reports 816 passing tests over a mod that has no
playable surface, the README and specs describe capabilities that are unreachable in-game, and the
binding governance changed mid-assessment, invalidating the premise the assessment was opened with.
The maintainer is therefore being asked to make a prioritization decision using false signals.

## Affected Users & Stakeholders

- **Maintainer** (sole decision-maker; also the primary user) — must choose what to build next with no
  reliable way to tell working code from non-working code. Every hour invested risks going into a
  feature whose absence is invisible in the test suite.
- **The maintainer's spouse** — the stated reason the project exists (co-op play). Currently served by
  nothing: there is no playable content at all, and no multiplayer code. Source: user input; Principle
  VI rationale.
- **Future contributors and readers** — `README.md:10-12` advertises soil health visualization and
  compost benefit as features; specs `002` is marked 38/38 complete. A reader would reasonably believe
  a playable mod exists.
- **The constitution / governance process** — Principle VI (v2.0.0) mandates a capability the
  repository contains zero code for, and eight spec/plan documents still declare the opposite scope.
  Governance and artifacts disagree, so neither can be trusted as the source of truth.

## Goals

- Establish a prioritization ordering whose every claim is backed by verified behavior rather than by
  documentation, test counts, or task checkboxes.
- Make the project's actual playable surface knowable and explicit, including the case where that
  surface is empty.
- Ensure the next prioritization decision accounts for the multiplayer mandate rather than the
  superseded single-player scope it was originally framed under.
- Restore agreement between governance, specs, and code so that any single artifact can be trusted
  about the state of the project.

## Non-Goals

- Selecting which feature gets built. This document defines the problem; `/speckit.assess.shape` and
  `/speckit.assess.decide` own that choice.
- Designing any architecture, API, data model, sync mechanism, or item-registration approach.
- Implementing, fixing, or refactoring anything.
- Restoring multiplayer capability, or completing the composting feature, or any other specific work.
- Judging whether the maintainer's personal goal (playing with his spouse) justifies the cost. That is
  a decision, not a problem statement.
- Rewriting the stale specs. That is consequence, not problem.

## Success Metrics

- **Playable surface is non-empty and enumerated.** Baseline: **0** user-facing capabilities
  exercisable in-game. Verified this session: no `Data/Objects` handler exists; `Data/Machines` branch
  is empty (`ModEntry.cs:124-129`); the recipe registered at `ModEntry.cs:118` targets an item that
  does not exist.
- **No spec marked complete describes an unreachable capability.** Baseline: **spec `002` is 38/38
  complete** while its feature cannot be exercised in-game.
- **Every capability named in `README.md` is either working or labelled as not working.** Baseline:
  **3 of 3** soil-health bullet points describe non-functional behavior (two marked "(Planned)", one
  unconditional).
- **Green test suite does not overstate working software.** Baseline: **816 passed / 0 failed** over a
  mod with no playable surface. Target state: suite green *and* accompanied by a verified in-game
  inventory.
- **Governance, specs, and code agree on multiplayer scope.** Baseline: **8 spec/plan files** assert
  single-player-only; **0** production files reference any multiplayer primitive.
- **Ranking inputs are cited.** Baseline: not measured — no existing prioritization artifact cites
  verified evidence.
- **Gate 1 formatting gate is verified.** Baseline: **unknown** — `dotnet format --verify-no-changes`
  exceeded a 5-minute timeout this session and produced no result either way.

## Cost of Inaction

- The mod stays unplayable indefinitely while reporting a fully green test suite, so the maintainer's
  natural signal for progress is permanently wrong.
- Principle VI stays violated with no code, no spec, and no plan, and the eight contradicting spec
  documents keep asserting a scope that governance has revoked.
- Continued investment stays misallocated: features can be built, marked done, and still be invisible
  in-game — exactly as `specs/002` already is.
- The gap between what the README promises and what the mod does grows with each feature added,
  raising the cost of an eventual first public release.
- The spouse — the project's originating reason — keeps waiting on a capability that has not been
  started and, per the current artifacts, was previously declared out of scope.
- Personal risk: a solo maintainer repeatedly unable to make a confident prioritization decision is
  likely to stall or lose confidence in the project.

## Open Questions

- [NEEDS CLARIFICATION: `WriteJsonFile` versus `WriteSaveData` host-authority semantics on the exact
  SMAPI version this project pins. Confirmed: `WriteSaveData` throws when not on the host computer.
  Unconfirmed: whether `WriteJsonFile` does the same. This determines whether a farmhand sees a hard
  error or silent divergence, and therefore how much of the multiplayer problem is correctness versus
  usability.]
- [NEEDS CLARIFICATION: must a second player observe shared state live, or is reading host state at
  save/load sufficient? Materially changes the size of the multiplayer problem.]
- [NEEDS CLARIFICATION: may a farmhand write soil health at all, or is the host the sole writer?]
- [NEEDS CLARIFICATION: should farmhands see the soil visualization overlay, or is rendering
  host-only?]
- [NEEDS CLARIFICATION: does Principle VI require multiplayer capability *now*, or only that nothing
  new be designed single-player-only while multiplayer arrives later? The two readings produce very
  different orderings.]
- [NEEDS CLARIFICATION: is a second physical machine available for the Gate 3 verification the
  constitution now mandates, or is a local split-screen/second client sufficient?]
- [NEEDS CLARIFICATION: which is ranked first — the constitutional mandate (build multiplayer) or the
  playable-surface prerequisite (make anything work at all)? A judgement call for `shape`/`decide`,
  recorded here because it determines what "success" even means.]
- [NEEDS CLARIFICATION: should the empty `Data/Machines` branch at `ModEntry.cs:124-129` and the dead
  `HoeFeedbackRenderer` be treated as defects to correct, or as intentional deferrals to be recorded
  as such?]
- [NEEDS CLARIFICATION: no third-party demand evidence for co-op Stardew mods was found this session.
  Is first-party intent sufficient to justify the prioritization, or is external signal required?]
