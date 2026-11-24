# Problem Definition: Sandboxed, Visual End-to-End Game Test Harness

- **Slug**: sandboxed-game-e2e-harness
- **Created**: 2026-10-31
- **Inputs used**: intake.md | research.md | user input (idea text)

## Problem Statement

Maintainers and contributors of LivingRoots cannot reliably obtain trustworthy, reproducible end-to-end evidence that gameplay features actually work: verification today is manual or partial, `In-game runs: 0` is the documented default, and the only fully automated runner that exists is specified (spec 010) but not yet implemented. A further question — whether adding a stronger sandbox, visual evidence, and per-feature automatic validation would materially improve that situation, or merely duplicate the existing direction — is itself unresolved and is part of the problem this definition frames.

## Affected Users & Stakeholders

- **Users — Maintainer**: needs Gate 3 evidence for roughly 19 roadmap features and cannot scale manual play sessions to that volume; carries the cost of unreproducible verification and of over-claiming partial evidence. [source: `research.md` §Users & Demand; `.specify/assessments/end-to-end-game-verification/decision.md`]
- **Users — Contributors**: need a reusable way to verify their feature without inventing steps or maintaining private test rigs; today the documented path is build, launch, play by hand. [source: `research.md` §Users & Demand; SMAPI wiki "Test and Troubleshoot" excerpt]
- **Users — CI operators**: need a machine-readable pass/fail verdict and an auditable evidence bundle; public CI cannot run game files, so local/self-hosted execution is the only path. [source: `specs/010-headless-verification-runner/spec.md` US3; `research.md`]
- **Stakeholders — Other SMAPI mod authors**: would benefit if any harness is reusable across mods, but no external consumer exists yet; reusability is an aspiration, not a demonstrated demand. [source: `research.md` §Users & Demand; `specs/010-headless-verification-runner/spec.md` FR-022]
- **Stakeholders — End players**: affected indirectly through shipped regressions, especially rendering/UI, that unit tests do not catch. [source: `research.md` §Market & Context]
- **Stakeholders — Maintainers of spec 010**: any new harness must not silently duplicate or contradict the existing headless-runner direction. [source: `research.md` §Evidence Against, item 1]
- **Stakeholders — Legal/licensing**: proprietary game assets, recorded footage, and fixture saves raise distribution questions that any harness must respect. [source: `research.md` §Data & Constraints; spec 010 FR-002]

## Goals

- Make Gate 3 end-to-end evidence reproducible and obtainable without manual gameplay.
- Increase the amount of `machine-pass` evidence available, without weakening the honesty discipline (`verified` remains a human attestation).
- Determine whether sandboxed execution and visual evidence add value beyond the already-specified headless runner, or whether they are redundant with it.
- Keep the cost of verification (maintenance, storage, flakiness, runtime) within a sustainable budget for a single-maintainer project.
- Preserve the project's evidence vocabulary (partial / machine-pass / verified) and its refusal to auto-promote on incomplete or disagreeing signals.

## Non-Goals

- This definition does not decide whether to build the harness, nor does it choose an architecture, API, data model, or implementation plan.
- It does not propose replacing the manual `verified` attestation with automation.
- It does not assert that visual evidence can substitute for the three-signal rule.
- It does not commit to containers, microVMs, video recording, or a desktop (non-headless) mode as requirements.
- It does not expand the scope beyond LivingRoots/Stardew Valley unless a later stage justifies it.
- It does not resolve the relationship between this idea and spec 010; that relationship is an open question carried forward.

## Success Metrics

- Share of roadmap features with at least one non-flaky `machine-pass` run. Baseline: 0 (as documented). [source: `research.md` §Market & Context; decision.md]
- Time from feature completion to obtainable Gate 3 evidence. Baseline: unbounded/manual (unknown).
- False-positive rate of automated verdicts (a run reported pass that a human would not attest). Baseline: unknown; must be measured or bounded before any promotion rule changes.
- Flakiness rate and quarantine count for automated scenarios. Baseline: unknown.
- Maintenance cost per game or SMAPI version bump (hours and breakage). Baseline: unknown.
- Storage cost per run and total retained evidence. Baseline: unknown; video in particular scales linearly with scenario count and run frequency. [source: `research.md` §Data & Constraints]
- Honesty integrity: zero auto-promotions to `verified`, and no auto-promotion to `machine-pass` on flaky, signal-incomplete, or signal-disagreeing runs. Baseline: enforced by spec 010; must remain enforced.

## Cost of Inaction

If nothing is built beyond the current state:

- Gate 3 evidence stays manual and does not scale to the 19 roadmap features; `In-game runs: 0` remains the default for shipped features.
- Rendering and UI regressions can pass unit tests unnoticed because no automated end-to-end path exists.
- Evidence remains unreproducible, so `verified` attestations depend on one-off manual sessions.
- The manual protocol stays the bottleneck for every feature and for any contributor who wants to verify their own change.
- There is also a risk of wasted effort: if a second harness is built alongside the already-specified (but unimplemented) spec 010 runner, the project inherits two competing maintenance surfaces. [source: `research.md` §Evidence Against, items 1 and 9; `.specify/assessments/end-to-end-game-verification/decision.md`]

## Open Questions

- [NEEDS CLARIFICATION: what does "sandbox" mean in this idea — container/namespace, VM/microVM, separate OS user, disposable XDG/HOME and mods folder, or only a virtual display?]
- [NEEDS CLARIFICATION: what is being isolated from what — host from untrusted mods, test run from the developer's own configs/mods, concurrent runs from each other, or performance measurement from ambient load?]
- [NEEDS CLARIFICATION: which host OS is targeted — Linux desktop, Windows, macOS, or Linux CI only?]
- [NEEDS CLARIFICATION: does "minimized and muted" require a real desktop window, or is a virtual display (Xvfb/Weston/Gamescope) acceptable?]
- [NEEDS CLARIFICATION: what counts as "own test save" — bundled fixture, generated save, player-save copy, or the milestone-save + seeding approach already in spec 010?]
- [NEEDS CLARIFICATION: is visual evidence a human-review artifact, a machine oracle, or both; and which comparison method/threshold is acceptable?]
- [NEEDS CLARIFICATION: is the per-feature specification a new format or the existing scenario JSON from spec 010?]
- [NEEDS CLARIFICATION: what does "validates automatically against each test/feature specification" assert — exact expected values, thresholds/ranges, metamorphic relations, or a vision/LLM judgment?]
- [NEEDS CLARIFICATION: what performance is measured and what noise is acceptable; is frame-time measurement on Xvfb meaningful at all?]
- [NEEDS CLARIFICATION: is this idea an extension of spec 010, a replacement for it, or a coexisting developer-facing tool?]
- [NEEDS CLARIFICATION: what are the success criteria — features covered, runtime per scenario, false-positive/false-negative tolerance, flakiness budget, maintenance budget?]
- [NEEDS CLARIFICATION: what are the retention, integrity, and privacy requirements for screenshots/video and fixture saves?]
- [NEEDS CLARIFICATION: which input-automation mechanism is acceptable — SMAPI macros/console commands, socket-driven verification mod, raw X11 input, or a mix?]
- [NEEDS CLARIFICATION: is distribution of game files, saves, or recorded footage permitted under the game's licence/EULA, and where may they be stored?]
- [NEEDS CLARIFICATION: the verbatim original idea was reconstructed from the session; any nuance lost in paraphrase must be recovered before this definition is treated as settled.]
