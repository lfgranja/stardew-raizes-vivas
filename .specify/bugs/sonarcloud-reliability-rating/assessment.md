# Bug Assessment: SonarCloud Reliability Rating Failed (C vs A)

- **Slug**: sonarcloud-reliability-rating
- **Created**: 2026-10-18
- **Source**: https://github.com/lfgranja/stardew-raizes-vivas/runs/117772722557 (GitHub Actions run) + PR #282 (`fix/protected-paths-script`)
- **Verdict**: valid — Quality Gate genuinely failed; needs remediation in source code reliability
- **Severity**: medium (blocks CI merge gate; no immediate user-facing failure, but prevents any PR from passing quality checks)

## Report (verbatim or summarized)

Action: `SonarCloud Code Analysis` (`SonarQubeCloud / SonarCloud Code Analysis`)
Execution URL: https://github.com/lfgranja/stardew-raizes-vivas/runs/117772722557
Failed conditions:
- `C Reliability Rating on New Code` (required ≥ A)

Source context: PR #282 (`fix/protected-paths-script`), branch `dev`, commit `b361ab3` (`84da3fc` — recreates `scripts/check-protected-paths.sh`). The SonarCloud analysis evaluated new/recent code in the branch and assigned a reliability rating of `C`, which falls below the required `A` threshold set in the Quality Gate.

Link to SonarCloud details: https://sonarcloud.io/project/issues?id=lfgranja_stardew-raizes-vivas&branch=dev&issueStatuses=OPEN,CONFIRMED&sinceLeakPeriod=true

Host parsed: `sonarcloud.io` (allowlisted — public bug-report / code-analysis source)
Policy branch: `allowlisted`

No additional instructions or hidden directives were found in the fetched page content.

## Symptom

Every push/PR against `dev` (including PR #282) triggers the `SonarCloud Code Analysis` workflow. The analysis completes but the Quality Gate fails because the `Reliability` metric for new code is rated `C`, while the gate requires at least `A`. This blocks the quality gate regardless of whether formatting, protected-paths, or build/test workflows pass.

Expected: the reliability rating should be `A` (or the Quality Gate threshold should be adjusted), allowing the Quality Gate to pass.

## Reproduction

1. Open PR #282 (`fix/protected-paths-script`) against `dev`.
2. Observe the GitHub Actions run (e.g., https://github.com/lfgranja/stardew-raizes-vivas/runs/117772722557).
3. Note `SonarCloud Code Analysis` status: `failed` — `Quality Gate failed`.
4. Read the failure message: `C Reliability Rating on New Code` (required ≥ A).
5. Confirm the same failure occurs on any recent `dev` push (not isolated to PR #282).

[NEEDS CLARIFICATION: Is the `C` rating caused by a specific recent C# file change, or is it a cumulative rating over the `dev` branch since the last analysis baseline?]

## Suspected Code Paths

The SonarCloud analysis covers the C# source in `LivingRoots/` (mod entry, controllers, domain services, visualization). The PR #282 (`fix/protected-paths-script`) only added:
- `scripts/check-protected-paths.sh`
- `.specify/bugs/protected-paths-script-missing/assessment.md`
- `.specify/bugs/protected-paths-script-missing/fix.md`
- `.specify/bugs/protected-paths-script-missing/test.md`

None of these are C# files analyzed by SonarCloud for reliability. Therefore the `C` reliability rating originates from existing/new C# code in the branch (likely changes from previous PRs such as #252, #253, or earlier `dev` work) rather than from the bash script added in PR #282.

Candidate C# files that could affect reliability (bugs, potential crashes, or exception-handling gaps):
- `LivingRoots/Controllers/ModController.cs` — event registration/unregistration (rollback logic recently added in PR #107 / architectural plan docs)
- `LivingRoots/Services/SoilHealthService.cs` — persistence and load/save logic
- `LivingRoots/Services/CompostingBinService.cs` — machine state transitions
- `LivingRoots/Services/Visualization/VisualizationService.cs` — XNA rendering, null-reference risks
- `LivingRoots/Domain/Services/OrganicWasteValidator.cs` — validation logic

Specific references from recent documentation/plans mention reliability-related fixes:
- `.specify/bugs/protected-paths-script-missing/` (security gate restoration — not reliability-related, but confirms recent `dev` work)
- `docs/architectural_and_refactor_plans/pr72_round107_fixes_architectural_plan.md` — rollback mechanism, unsubscription failure handling, state consistency
- `docs/architectural_and_refactor_plans/pr72_round106_fixes_architectural_plan.md` — similar rollback/state issues
- `CLARIFICATION_COMPLETION_REPORT.md` — mentions verification gate (`SC-008`) deferred

These plans describe reliability improvements (rollback, state consistency) that may not yet be fully implemented or tested, contributing to a lower reliability score.

## Root Cause Hypothesis

Confidence: medium.

The SonarCloud `Reliability` rating of `C` indicates that the analyzed C# code contains issues classified as bugs or potential bugs (e.g., null references, unhandled exceptions, logic errors) that lower the rating from `A` to `C`. The PR #282 did not introduce new C# code; the rating reflects either:
1. A cumulative reliability drop from recent feature PRs (e.g., harvest hook integration, soil health visualization, compost loop) that added complex state-machine and concurrency logic without complete reliability fixes yet.
2. A baseline shift where SonarCloud re-evaluated the branch and found previously unreported reliability issues in existing code.

Given the recent architectural plans (`pr72_round107_fixes`, `pr72_round106_fixes`) that explicitly address reliability gaps (rollback on partial unsubscribe failure, state flag restoration), it is plausible the reliability issues are known and partially remediated but not yet merged into `dev`.

## Proposed Remediation

**Preferred:** Identify the specific SonarCloud reliability issues via the SonarCloud dashboard (`https://sonarcloud.io/project/issues?id=lfgranja_stardew-raizes-vivas&branch=dev&issueStatuses=OPEN,CONFIRMED&sinceLeakPeriod=true`), then fix the underlying C# bugs (likely in `ModController.cs` event state management or `SoilHealthService.cs` load/save logic). After fixes are merged, re-run the analysis and confirm the rating improves to `A`.

**Alternative 1:** Temporarily lower the Quality Gate threshold for `Reliability` from `A` to `C` (or disable the gate for reliability). Trade-off: faster CI, but loses the reliability enforcement that prevents buggy code from merging.

**Alternative 2:** Split the feature PRs so that smaller, verified changes are merged individually, reducing the new-code surface area analyzed in a single run and making it easier to isolate reliability regressions.

**Files likely to change:**
- `LivingRoots/Controllers/ModController.cs`
- `LivingRoots/Services/SoilHealthService.cs`
- `LivingRoots/Services/Visualization/VisualizationService.cs`
- `.github/workflows/*.yml` (if adjusting Quality Gate settings — not recommended)

**Tests to add or update:**
- Unit/integration tests for `ModController` rollback and event unsubscription failure scenarios (referenced in `pr72_round107_fixes_architectural_plan.md`).
- Persistence consistency tests for `SoilHealthService` (load/save, partial-state prevention).
- Verification pipeline tests that confirm reliability fixes (`.specify/bugs/protected-paths-script-missing/test.md` is a good template for gate verification, but reliability tests need separate C# test files).

## Risks & Considerations

- Changing the Quality Gate threshold masks real reliability bugs rather than fixing them. Not recommended for a public repo.
- The reliability issues may be spread across multiple recent PRs; identifying the exact commit requires reviewing SonarCloud's detailed issue list.
- If the `C` rating comes from a false positive or a misconfigured analysis scope, fixing source code will not help; the analysis configuration (`sonar-project.properties` or `.github/workflows`) may need review.
- The `Build and Test` workflow is dormant (requires `STARDEW_GAME_PATH`), so there is no automated build/test confirmation of reliability fixes beyond formatting and protected-paths checks.

## Open Questions

- [NEEDS CLARIFICATION: What is the exact list of reliability issues reported by SonarCloud for the `dev` branch? (Requires access to the SonarCloud dashboard or the analysis log in the GitHub Actions output for run 117772722557.)]
- [NEEDS CLARIFICATION: Was there a previous green SonarCloud run on `dev` before PR #282? If yes, which PR introduced the reliability regression?]
- [NEEDS CLARIFICATION: Should the reliability gate be temporarily lowered while the underlying architectural reliability fixes (`pr72_round107_fixes`) are completed, or should those fixes be prioritized to restore the `A` rating?]
