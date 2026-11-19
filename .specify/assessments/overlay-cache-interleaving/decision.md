# Decision: Stabilize overlay cache invalidation under interleaved events

- **Slug**: overlay-cache-interleaving
- **Decided**: 2026-11-18
- **Verdict**: go
- **Artifacts reviewed**: intake.md | research.md | problem.md | concept.md

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | Required `Build and Test` check is RED on `dev`; live CI log + 4/5 local repro. |
| Evidence strength | strong | Instrumented `DIAG:budget` isolates the failing assertion; counterexamples and seeds recorded. |
| Value vs. inaction | strong | Inaction blocks every merge; fix is small and test-only. |
| Feasibility / appetite | strong | Option A is small; one test file, no production C#. |
| Strategic fit | adequate | Aligns with Gate 1 determinism; no constitution principle implicated. |
| Risk posture | strong | No runnable-code change → Gate 3 N/A; FR-009 preserved via deterministic work bound. |

## Verdict & Rationale

Go. The problem is real and measured, the diagnosis is evidence-backed rather than assumed, and the chosen option is test-only with a bounded blast radius. No owner-gated action (tag, release, protection, secrets, D1, human attestation) is required.

## If go — Handoff to `/speckit-specify`

- **Problem**: a wall-clock assertion inside a property test makes a correct cache-invalidation path fail under runner load, turning a required CI check red.
- **Chosen approach**: Option A — deterministic per-frame work bound (≤ one color-service consultation per visible tile) replacing wall-clock elapsed time in the failing property; wall-clock budget stays Gate 3 evidence.
- **In scope / out of scope**: in scope = one property test and its documentation; out of scope = production rendering code, other properties, CI/branch protection, owner-gated items.
- **Success metrics**: property 5/5 local runs; full Release test suite green; PR and `dev` push required check green.
- **Carried-forward open questions**: whether to add a separate wall-clock benchmark (debt, not blocking).
