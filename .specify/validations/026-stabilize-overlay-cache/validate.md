# Validation Report: Stabilize overlay cache invalidation property

- **Feature**: 026-stabilize-overlay-cache
- **Date**: 2026-11-18
- **Stage**: R4 (validate) — must be approved before R7
- **Verdict**: **APPROVED**

## Scope validated

Single test-file change in `LivingRoots.Tests/Invariants/VisualizationCacheInvalidationInvariantsTests.cs`: the wall-clock frame-budget assertion was replaced by a deterministic per-frame work bound (at most one `GetCategoryForHealth` consultation per visible tile). No production code changed.

## Evidence (commands executed + outputs)

| Check | Result |
|-------|--------|
| RED reproduction before edit | 4 of 5 local runs failed; counterexample + StdGens recorded in assessment `research.md`; instrumented label `DIAG:budget` |
| `dotnet build -c Release -m:1` | 0 errors, 14 pre-existing warnings, exit 0 |
| Property, 5 consecutive runs | **5 passed / 5** (SC-001; baseline 1/5) |
| Full `dotnet test -c Release --no-build` | **1999 passed / 0 failed / 4 skipped / 2003 total** (SC-002) |
| `dotnet format --verify-no-changes` | exit 0 |
| `git diff --name-only origin/dev \| grep '^LivingRoots/'` | no output (SC-004) |
| Pre-flight `bash scripts/ci/aggregate-gate.sh` | RED **only** for missing R4/R7 artifacts (expected at that stage); no code failure |
| `speckit-analyze` (read-only) | 0 constitution violations; 1 MEDIUM coverage gap fixed at source (T011) and re-analyzed to 100% coverage |
| `speckit-converge` | **Converged** — 0 findings; `tasks.md` left byte-for-byte unchanged |

## Requirements disposition

FR-001 ✅ | FR-002 ✅ | FR-003 ✅ | FR-004 ✅ | FR-005 ✅ | FR-006 ✅
SC-001 ✅ | SC-002 ✅ | SC-003 **[NV]** (live PR CI and post-merge `dev` push not yet observed) | SC-004 ✅

## Risks and residual debt

- Removing a timing assertion could be misread as weakening FR-009; the test comment explicitly assigns wall-clock budget to Gate 3 verification-runner evidence.
- A separate wall-clock benchmark remains a deferred debt (recorded in assessment `concept.md` / `research.md`), not a blocker.
- Gate 3 real-SMAPI evidence is **N/A**: no runnable code changed. No `machine-pass` claim is made.

## R4 decision

APPROVED. Proceed to R7. No owner-gated action (tag, release, protection, secrets, auto-merge, D1, human attestation) is requested or simulated.
