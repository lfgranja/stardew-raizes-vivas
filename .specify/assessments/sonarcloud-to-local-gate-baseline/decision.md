# Decision: SonarCloud to local gate baseline readiness

- **Slug**: sonarcloud-to-local-gate-baseline
- **Decided**: 2026-11-01T07:55:00Z
- **Verdict**: go
- **Artifacts reviewed**: intake.md | research.md | problem.md | concept.md

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | Clear need to establish fair baselines for local quality gate after cleaning SonarCloud issues |
| Evidence strength | adequate | Grounded in spec 011 requirements, current baselines, and git history |
| Value vs. inaction | strong | Avoids mis-set thresholds and unnecessary remediation |
| Feasibility / appetite | strong | Small appetite, follows existing plan |
| Strategic fit | strong | Aligns with spec 011 goal of local OSS quality gate |
| Risk posture | adequate | Risks identified (analyzer differences) and manageable as part of feature 011 |

## Verdict & Rationale

**go** — This is necessary preparatory work for feature 011. Zeroing SonarCloud gives a clean slate, but fresh baselines are required by FR-009/FR-017 and the gate has different rules. Following the plan with fresh measurements is the right approach.

## If go — Handoff to `/speckit.specify`

- **Problem**: Need fair coverage/duplication baselines and awareness of analyzer differences to implement local quality gate (spec 011) correctly
- **Chosen approach**: Option A — Capture baselines and follow spec 011 plan as-is
- **In scope / out of scope**: In scope - measure baselines, follow TDD sequence, handle analyzer warnings. Out of scope - SonarCloud removal, dashboards, gate architecture changes
- **Success metrics**: Fresh baselines recorded, Phase 2 RED tests committed, analyzer build passes with TreatWarningsAsErrors
- **Carried-forward open questions**: Will analyzers introduce new warnings? Are Phase 2 tests ready on 011 branch? Should we ratchet or set as-is?
