# Decision: Runner quality gates that keep Gate 3 trustworthy

- **Slug**: runner-repair-gate3
- **Decided**: 2026-11-05
- **Verdict**: go
- **Artifacts reviewed**: intake.md, research.md, problem.md, concept.md

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | Gate 3 was genuinely blocked: the real-host run reported `partial` with `signalsComplete=false`, and specs 014/015 were gated behind the machine-pass-first rule. The problem is real and measurable. — [research: Users & Demand; problem.md] |
| Evidence strength | strong | The verdict rests on the full SMAPI log, the save directory listing, the pre-fix bundles failing schema with 13 missing fields, and the successful 2026-11-05 run (exit 0, verifier rc 0). Not assumption-driven. — [research: Prior Art, Data & Constraints] |
| Value vs. inaction | strong | The repair already achieved `machine-pass` on a real host (~120 s) and unblocked the verification chain; doing nothing leaves the residual gaps (incomplete `subject[]`, non-genuine red-gate logs, T138/T145 blockers) to cause the next misdiagnosis. — [research: Market & Context, Cost of Inaction] |
| Feasibility / appetite | strong | Option A (harden the existing runner) is a small appetite (days): regression guards + closing the `subject[]`/retention-receipt gap, no new capabilities, no new dependencies. — [concept.md, Option A] |
| Strategic fit | strong | Aligns with the constitution's quality bar, the reproducibility goal, and the honesty ladder (`verified` stays human-only). Does not contradict FR-006 or the no-input-simulation contract. — [research: agent-desktop-usefulness decision.md, "Strategic fit"] |
| Risk posture | adequate | Key risks are identified (second blocker behind save fix, log noise, schema version bump for `subject[]` completeness) and partially mitigated by the full-log capture and the existing test suite. Residual risk: the repair is reactive and may not catch the next defect class. — [research: Evidence Against the Idea] |

## Verdict & Rationale

**Go.** The problem is real and worth solving, the evidence is strong and reproducible (internal, high confidence), and Option A is a credible, small-appetite path that does not add capabilities or dependencies. The core repair is already applied and committed (commits 4b8452d..44571eb) and produced `machine-pass` on 2026-11-05; the remaining value is hardening (regression guards, `subject[]` completeness, genuine red-gate evidence) so the defect class does not recur. This is a `go` for the hardening scope, not for re-doing the repair.

## If go — Handoff to `/speckit.specify`

- **Problem**: Runner evidence path carried three defects (save-name mismatch, two-line SMAPI excerpt, schema-invalid manifest) that blocked trustworthy `machine-pass` and caused multi-day misdiagnosis; residual gaps remain (incomplete `subject[]` coverage, non-genuine red-gate logs, T138/T145 blockers).
- **Chosen approach**: Option A — Harden the existing runner (no new capabilities): regression guards for save-load alignment, full SMAPI log capture, manifest schema + digest enforcement, and closing the `subject[]`/retention-receipt gap.
- **In scope / out of scope**: In scope: regression tests asserting schema validity, subject completeness, full-log presence, and save-name alignment; `subject[]` completeness change (may require schema version bump); genuine red-gate evidence for T130. Out of scope: new capabilities, new dependencies, agent-desktop or any GUI driver, changing the honesty ladder, implementing specs 014/015, generalizing to other mods before a second consumer exists (rule of three).
- **Success metrics**: `scripts/verify-evidence-bundle.sh` returns 0 on a valid bundle and 19 on any schema/digest/mandatory-evidence failure; `subject[]` covers every file in the bundle; red-gate logs show the intended failing assertion; T138/T145 unblocked or explicitly recorded as blocked.
- **Carried-forward open questions**: Should save-name alignment live in `save_prepare` or milestone packaging? Is full-log capture a permanent contract or configurable? Should schema validation be a CI hard gate? Is this repair scoped to spec 010 or generalizable? Which parts are domain-specific? Should `subject[]` cover every bundle file?