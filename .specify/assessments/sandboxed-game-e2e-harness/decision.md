# Decision: Sandboxed, Visual End-to-End Game Test Harness

- **Slug**: sandboxed-game-e2e-harness
- **Decided**: 2026-10-31
- **Verdict**: kill
- **Artifacts reviewed**: intake.md | research.md | problem.md | concept.md

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | The problem is real and worth solving: Gate 3 evidence is manual, `In-game runs: 0` is the documented default, and the manual protocol does not scale to the 19 roadmap features. [source: `problem.md`; `research.md` §Market & Context] |
| Evidence strength | adequate | Strong internal evidence for the automation gap and the existing spec 010 direction; prior art (StarDojo, JunimoServer, Xvfb) supports headless execution. Evidence for the idea's specific deltas (sandbox, visual oracle, automatic per-feature validation) is weak to absent, and no external demand was found. [source: `research.md`] |
| Value vs. inaction | adequate | Automating Gate 3 has high value; the *marginal* value of the sandbox/visual deltas over the already-specified headless runner is unproven, while their cost (complexity, storage, flakiness, maintenance) is real. [source: `problem.md`; `research.md` §Evidence Against] |
| Feasibility / appetite fit | adequate | Option A (complete spec 010) is credible within its existing large appetite. Option C (the idea as captured) is large and largely additional, with no credible path at sane appetite for a single maintainer. [source: `concept.md`] |
| Strategic fit | adequate | Aligns with the Gate 3 automation goal, but conflicts with the project's YAGNI/Regra dos Três conventions and risks duplicating an already-accepted direction (spec 010). [source: `AGENTS.md`; `concept.md`; `research.md`] |
| Risk posture | weak | The core automation risks are understood and partially mitigated by spec 010. The deltas (sandbox plumbing, visual flakiness, video storage, licence exposure, demand uncertainty) are serious and unmitigated, and no mitigation plan exists beyond "maybe later." [source: `research.md` §Evidence Against; `concept.md` §Rabbit holes] |

## Verdict & Rationale

**Kill — not worth building now, as captured.** The core problem (trustworthy, reproducible end-to-end evidence) is real and is already addressed by `specs/010-headless-verification-runner/`, which is specified but not yet implemented. The idea's distinguishing deltas — a stronger sandbox, a minimized/muted desktop mode, video as a first-class artifact, and automatic validation against per-feature specifications — add cost and complexity that the evidence does not justify: visual evidence is a weak oracle and is already scoped as supporting-only in spec 010; "minimized and muted" is satisfied by Xvfb by construction; and no external demand or concrete diagnostic gap has been demonstrated. Building a second harness now would split a single-maintainer effort across two large surfaces and risk neither shipping. The assessment record is retained so the decision can be revisited; Option B (incremental visual evidence) in `concept.md` remains a sanctioned future delta if a documented, recurring diagnostic gap emerges after spec 010 ships.

## If needs-clarification

Not applicable — verdict is `kill`. The blocking unknowns in `problem.md` remain documented for any future re-assessment, but they do not change this verdict.

## If go — Handoff to `/speckit.specify`

Not applicable — verdict is `kill`. No handoff is made.
