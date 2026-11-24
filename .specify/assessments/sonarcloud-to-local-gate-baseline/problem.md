# Problem Definition: SonarCloud to local gate baseline readiness

- **Slug**: sonarcloud-to-local-gate-baseline
- **Created**: 2026-11-01T07:45:00Z
- **Inputs used**: intake.md | research.md | user input only

## Problem Statement

The maintainer needs a clean, fair baseline to implement the local OSS quality gate (spec 011) now that all OPEN/CONFIRMED SonarCloud issues have been cleared on dev. Without fresh coverage/duplication baselines and awareness of analyzer rule differences, thresholds may be mis-set and the TDD/analyzer rollout could be derailed or require unnecessary remediation.

## Affected Users & Stakeholders

- **Users**: Maintainer/contributor — need deterministic, actionable quality feedback locally/CI without relying solely on opaque SonarCloud ratings
- **Stakeholders**: CI/Release process — needs enforceable quality gates; Project maintainers — need sustainable quality enforcement

## Goals

- Capture fresh coverage and duplication baselines (ReportGenerator/jscpd) at current post-fix state
- Proceed with spec 011 plan (Phase 2 RED tests → Phase 3 analyzers with TreatWarningsAsErrors)
- Treat any new analyzer warnings as part of feature 011 work, not regressions
- Set fair ratchet thresholds based on current state

## Non-Goals

- Re-implementing SonarCloud
- Modifying runtime/gameplay code beyond what's needed for quality fixes
- Creating trend dashboards or PR decorations
- Removing SonarCloud now (separate follow-up per spec 011)

## Success Metrics

- Fresh baselines recorded (.coverage-baseline, .jscpd-baseline.json) reflect post-fix state (baseline: unknown → measured)
- Phase 2 TDD tests are RED and committed before implementation
- Analyzer build passes with TreatWarningsAsErrors after remediation
- Quality gate passes on clean baseline

## Cost of Inaction

Without fresh baselines and following the correct sequence, we risk setting incorrect thresholds, hitting unexpected analyzer failures, or doing unnecessary work. The opportunity to start from a clean slate is lost if we proceed without capturing current state.

## Open Questions

- [NEEDS CLARIFICATION: Will enabling AnalysisLevel=6.0-recommended + TreatWarningsAsErrors introduce new warnings not caught by SonarCloud?]
- [NEEDS CLARIFICATION: Are Phase 2 tests committed and RED on the feature branch 011-local-oss-quality-gate?]
- [NEEDS CLARIFICATION: Should we ratchet baselines upward from current values or set them as-is?]
