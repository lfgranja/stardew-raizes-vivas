# Requirements Quality Checklist: Stabilize reactive invalidation tests against wall-clock flakiness

**Purpose**: Verify completeness, clarity, consistency, and measurability of specification requirements before task generation (`specs/027-stabilize-reactive-invalidation-tests/spec.md`).
**Created**: 2026-11-19
**Feature**: [spec.md](../spec.md)

**Review Ownership**: Reviewer-owned requirements-quality review artifact.
**Marker Semantics**: `[x]` means the requirements-quality criterion was reviewed and satisfied — **it does NOT mean implementation is complete**.

## Requirement Completeness

- [x] CHK001 Are all four reactive-invalidation `Stopwatch <= ModConstants.MaxRenderTimeMs` occurrences explicitly named by file and method in `FR-001` and `FR-002`? (Verified in `spec.md` FR-001/FR-002)
- [x] CHK002 Is unused `using System.Diagnostics;` removal explicitly specified in `FR-005`? (Verified in `spec.md` FR-005)
- [x] CHK003 Are out-of-scope `Stopwatch` benchmarks (`VisualizationPerformanceTests`, `GrowthPerformanceTests`) and `LivingRoots/` explicitly excluded in `FR-006` and Assumptions? (Verified in `spec.md` US3/FR-006/Assumptions)

## Requirement Clarity & Testability

- [x] CHK004 Does `FR-003` define exact, verifiable deterministic invariants (synchronous `IsCacheEmpty == true`, `0` consultations during invalidation, immediate next-frame rebuild reflecting updated values, `<= 1` consultation per visible tile on rebuild)? (Verified in `spec.md` FR-003 & `data-model.md`)
- [x] CHK005 Are XML documentation requirements (`FR-004`) concrete enough to unambiguously state Gate 3 ownership of wall-clock 16.67ms frame timing? (Verified in `spec.md` FR-004)
- [x] CHK006 Are `.editorconfig` and build constraints (block-bodied methods, `TreatWarningsAsErrors=true`, `dotnet format`) explicitly captured in `FR-005` and `plan.md`? (Verified in `spec.md` FR-005 & `plan.md`)

## Consistency & Traceability

- [x] CHK007 Do `User Stories 1–3`, `FR-001..FR-006`, and `SC-001..SC-004` trace cleanly back to `.specify/assessments/reactive-invalidation-determinism/decision.md` (`verdict: go`)? (Verified)
- [x] CHK008 Are all edge cases (disconnected `ModEntry` wiring, `SafeOverlayHealth` handling of `NaN`/0/Inf, IDE0005 unused `using`) consistent across `spec.md` and `plan.md`? (Verified)
- [x] CHK009 Does `spec.md` contain zero `[NEEDS CLARIFICATION]` placeholders (confirmed in `clarify.md`)? (Verified)

## Measurability & Gate Alignment

- [x] CHK010 Does `SC-001` define a binary, grep-verifiable check for removal of `Stopwatch` / `MaxRenderTimeMs` timing comparisons? (Verified)
- [x] CHK011 Does `SC-002` require 5/5 consecutive runs (`dotnet test`) rather than a single lucky pass? (Verified)
- [x] CHK012 Does `SC-003` bind final acceptance to both target authoritative gates (`scripts/release-gate.sh` and `scripts/ci/aggregate-gate.sh`)? (Verified)
- [x] CHK013 Does `SC-004` verify zero production blast radius (`git diff --name-only origin/dev -- LivingRoots/` empty, keeping Gate 3 N/A)? (Verified)
- [x] CHK014 Is Gate 3 `N/A` rationale consistent with `VERIFICATION_PROTOCOL.md` and `AGENTS.md` (applies to runnable code changes only)? (Verified)

## Notes

- Reviewed 2026-11-19: **14/14 requirements-quality criteria satisfied**. `[x]` means the requirement quality review passed — **it does NOT mean implementation is done** (`tasks.md` governs implementation).
