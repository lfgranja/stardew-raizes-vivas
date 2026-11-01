# Bug Assessment: SonarCloud Quality Gate fails on new code (C Security and Reliability)

- **Slug**: sonarcloud-quality-gate-failure
- **Created**: 2026-11-01T03:50:00Z
- **Source**: https://github.com/lfgranja/stardew-raizes-vivas/runs/124740050890
- **Verdict**: likely valid, needs reproduction
- **Severity**: medium

## Report

SonarCloud Code Analysis check (run `124740050890`) failed on `dev` with Quality Gate conditions:
- C Security Rating on New Code (required ≥ A)
- C Reliability Rating on New Code (required ≥ A)

SonarCloud API lists 123 open issues on the new-code leak period, including VULNERABILITY findings in `.github/workflows/quality-gate.yml` (`npx` on-demand install, non-HTTPS redirection, `pip` without `--only-binary :all:`, unlocked dependency versions) and CODE_SMELL findings in `MulchApplicationService.cs`, `duplication.py`, `security.py`, `coverage.py`, `baseline.py`, `analyzer-probe.sh`, and several test files.

## Suspected Code Paths

- `.github/workflows/quality-gate.yml` — SonarCloud flags `npx`, `curl`, `pip` invocations.
- `LivingRoots/Services/MulchApplicationService.cs` — cognitive complexity 16, repeated `'cancel'` literal.
- `scripts/quality-gate/quality_gate/*.py` — cognitive complexity and duplicated literals.
- `scripts/quality-gate/tests/*.py` — composite assertions.

## Root Cause Hypothesis

The Quality Gate's "new code" leak period covers recent workflow and code changes, and SonarCloud rates the new code as C for Security and Reliability because of (a) shell commands in workflow files that SonarCloud's security rules flag by default (npx, pip, curl) and (b) code-smell findings in recently touched Python and C# files. Many of these are likely false positives or pre-existing patterns newly attributed to the leak period. Confidence: medium.

## Proposed Remediation

**Preferred**: Triage the SonarCloud findings — fix the genuine code smells (reduce cognitive complexity, extract constants) and mark the workflow security hotspots as "safe" in SonarCloud where they are intentional (pinned versions, HTTPS URLs). Re-run the Quality Gate.

**Alternatives**: Adjust the Quality Gate to not fail on new code for these categories (weakens the gate).

**Files likely to change**:
- `.github/workflows/quality-gate.yml`
- `LivingRoots/Services/MulchApplicationService.cs`
- `scripts/quality-gate/quality_gate/*.py`

## Risks

- Marking security hotspots as safe requires SonarCloud UI access and is not code-reviewable.
- Fixing cognitive complexity may require refactoring that risks regressions.

## Open Questions

- [NEEDS CLARIFICATION: which findings are genuine vs. false positives?]
- [NEEDS CLARIFICATION: is the SonarCloud gate intended to be blocking?]
