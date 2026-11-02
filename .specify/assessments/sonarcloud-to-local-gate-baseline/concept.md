# Concept: SonarCloud to local gate baseline readiness

- **Slug**: sonarcloud-to-local-gate-baseline
- **Created**: 2026-11-01T07:50:00Z
- **Recommended option**: Option A — Capture baselines and follow spec 011 plan as-is

## Options

### Option A — Capture baselines and follow spec 011 plan as-is
- **Sketch**: Measure fresh coverage (ReportGenerator) and duplication (jscpd) on current dev, confirm Phase 2 tests state, then proceed with TDD RED → analyzers → implementation per tasks.md. Treat new analyzer warnings as part of feature 011.
- **Appetite**: small (1-2 days)
- **Trade-offs**: Wins - fair thresholds, clean starting point, follows established plan. Sacrifices - may uncover new warnings requiring remediation.
- **Rabbit holes**: Analyzer rule differences causing more remediation than expected; coverage/duplication measurement environment issues.

### Option B — Re-run full SonarCloud scan first
- **Sketch**: Wait for SonarCloud to re-analyze and confirm closure, then proceed.
- **Appetite**: small (hours wait)
- **Trade-offs**: Wins - extra confirmation. Sacrifices - delays start, doesn't help with local gate rules/thresholds.
- **Rabbit holes**: SonarCloud may not re-analyze immediately; doesn't address local gate requirements.

### Option C — Adjust thresholds from old baselines
- **Sketch**: Use existing .coverage-baseline/.jscpd-baseline.json without re-measuring.
- **Appetite**: small
- **Trade-offs**: Wins - faster. Sacrifices - baselines may be stale after refactorings; thresholds could be unfair.
- **Rabbit holes**: Gate failures due to stale baselines, ratchet issues.

## Recommendation

**Option A** is recommended. The codebase is clean from SonarCloud perspective, but the local gate has different rules. Fresh baselines are required by FR-009/FR-017 and will ensure fair thresholds. Following the spec 011 plan is the correct path.

## Out of Scope (for the recommended option)

- Modifying SonarCloud configuration
- Creating dashboards/PR decorations
- Changing gate architecture
- Removing SonarCloud now

## Assumptions to Validate

- Fresh measurement tools (ReportGenerator, jscpd) are available/configured
- Phase 2 tests exist on 011 branch or can be created per plan
- New analyzer warnings will be manageable within scope of feature 011
