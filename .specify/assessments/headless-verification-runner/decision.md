# Decision: Headless Verification Runner for Gate 3

- **Slug**: headless-verification-runner
- **Decided**: 2026-11-02
- **Verdict**: go
- **Artifacts reviewed**: intake.md | research.md | problem.md | concept.md

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | Gate 3 has 0 runs; harvest-quality effects unverified; central promise unproven. (problem.md, ROADMAP.md) |
| Evidence strength | adequate | Prior art strong (JunimoServer, godot-qa, smoke spec 008). Headless Xvfb proven on host. Some assumptions remain (NEEDS CLARIFICATION x5). (research.md) |
| Value vs. inaction | strong | Manual 30-min sessions unsustainable; 19 unimplemented features need a verified baseline. (problem.md, ROADMAP.md) |
| Feasibility / appetite | adequate | Option B (Incremental MVP) is credible: reuses proven smoke infra, 3-signal capture, medium appetite. Option A (full 161 tasks) is large and risky. (concept.md) |
| Strategic fit | strong | Aligns with Principle IV (TDD), Gate 3 requirement, and the "verified" honesty level. Supports multiplayer verification (D1). (constitution.md, spec 009) |
| Risk posture | adequate | Key risks identified (complexity/YAGNI, false-green, headless graphics, game-install CI). Mitigations: MVP scope, human-only `verified` attestation, software-GL baseline in CI. (research.md, concept.md) |

## Verdict & Rationale

**Go** — the problem is real and worth solving, evidence is adequate (not weak/unknown), and a credible concept option (Option B, Incremental MVP) exists within a medium appetite. The runner is the key enabler for automated Gate 3, which unblocks "verified" status for harvest-quality effects and provides a regression safety net for all future features. The full 161-task spec (Option A) is too large for a first pass; Option B delivers the 3 critical signals with ~10x less scope. Killing is not warranted: the cost of inaction (unverified central promise, unsustainable manual verification) exceeds the MVP cost. The 5 open questions must be resolved during specification.

## If go — Handoff to `/speckit.specify`

- **Problem**: Gate 3 has 0 runs; harvest-quality effects unverified; manual verification unsustainable.
- **Chosen approach**: Option A — Full Spec 010 (161 tasks, full harness) per maintainer clarification (2026-11-02). Note: this overrides the assessment's earlier Option B recommendation. The assessment's evidence-against section flagged YAGNI risk for the full scope; the maintainer explicitly chose the full spec.
- **In scope / out of scope**: In: 3-signal capture for harvest-quality loop, scenario JSON, save diff, log capture, `machine-pass` verdict. Out: screenshots, video, CI integration, C# companion projects, public mod API, non-harvest-quality scenarios, deterministic replay.
- **Success metrics**: ≥1 automated run with `machine-pass` + all 3 signals; Gate 3 moves from "partial (smoke)" to "machine-pass"; verification time < 5 min (from 30 min manual); zero false "verified" claims.
- **Carried-forward open questions**:
  1. Whether the runner should reuse `scripts/smoke-validation.sh` infrastructure or be separate.
  2. Communication mechanism between C# companion and running game process (SMAPI mod API vs. file-based IPC).
  3. Whether `verified` attestation can be partially automated or remains human-only.
  4. Acceptance of the 161-task scope vs. a smaller MVP that only captures the 3 Gate 3 signals.
  5. CI runner availability (self-hosted vs. GitHub-hosted with gamepath.zip).