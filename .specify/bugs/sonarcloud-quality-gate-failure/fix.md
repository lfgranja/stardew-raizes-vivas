# Bug Fix: SonarCloud Quality Gate failure

- **Slug**: sonarcloud-quality-gate-failure
- **Fixed**: 2026-11-01T04:00:00Z
- **Assessment**: ./assessment.md
- **Status**: partial

## Summary

No automated code change was applied. The SonarCloud Quality Gate failure is driven by security hotspots in workflow files (npx/pip/curl rules) and code-smell findings whose remediation requires SonarCloud UI triage and human judgement on false positives. The assessment's open questions remain unresolved.

## Changes

| File | Change | Notes |
|------|--------|-------|
| (none) | not-applied | See deviations |

## Deviations from Assessment

1. The assessment verdict was "likely valid, needs reproduction" with two `[NEEDS CLARIFICATION]` items. Per the fix guardrails, automated mode stops rather than guessing.
2. The preferred remediation (triage hotspots, fix genuine code smells) cannot be executed without SonarCloud UI access to mark security hotspots as safe and without deciding which findings are genuine.
3. No source or workflow files were modified.

## Local Verification

- Not applicable (no changes).

## Follow-ups

- A human should review the 123 SonarCloud new-code issues and mark workflow security hotspots as safe where intentional.
- Refactor `MulchApplicationService` (cognitive complexity 16 → ≤15, extract `'cancel'` constant) and the flagged Python functions if their findings are confirmed genuine.
- Re-run `/speckit.bug.assess` after triage to capture a concrete, actionable remediation.
