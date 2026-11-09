# Cross-Artifact Analysis Report — 020-heirloom-seeds-pipeline (2026-11-09)

Scope: spec.md, plan.md, research.md, data-model.md, contracts/*.md, tasks.md, quickstart.md, checklists/, constitution v2.0.0. Read-only analysis; findings remediated in-cycle by append-only edits (commits 3f85cc4..2b621cf).

## Findings (all resolved or accepted)

| ID | Category | Severity | Location | Summary / Resolution |
|----|----------|----------|----------|----------------------|
| A1 | Consistency | MEDIUM | spec.md FR-011 vs data-model/tasks | i18n key drift `plant-bonus-refused-authority` → unified to `message.heirloom.plant-bonus-authority-unsupported`. |
| A2 | Underspecification | HIGH | tasks T016, research D8, spec Clarif A3 | Gate 3 could not observe the plant bonus: `lr_plant_crop` constructs `Crop` directly, bypassing `HoeDirt.plant` (the Harmony postfix would never fire). Resolution: new verification-only `lr_sow_heirloom` command planting via real `HoeDirt.plant`; scenario = multiplier `1.04` on measured 50 (→52); before/after captures bracket the steps (runner-injected). |
| A3 | Edge Case | LOW | quickstart | Cart acquisition honesty (cart odds ≠ correctness; use `lr_give_heirloom_seed`); wording updated. |
| A4 | Ambiguity | MEDIUM | spec FR-003/FR-013 | SeedShop/Joja untouched is now asserted by an absence-style contract assertion (no registration branch; tasks T007), not just intent. |
| A5 | Ambiguity | LOW | plan.md structure | Optional `Domain/IHeirloomSeedService.cs` removed from scope; identity logic centralized in `Domain/HeirloomIdentity.cs` (pure, testable). |
| A6 | Metrics | LOW | across artifacts | Suite baseline 1937/0/4 cited consistently; no regressions allowed. |
| A7 | Coverage | — | tasks.md | Every FR maps to ≥1 task (see tasks.md traceability); every contract item C-x → test file tasks; coverage ≈100% of buildable requirements. |
| A8 | Constitution | — | — | No violations: DDD layering preserved; async-only untouched; TDD ordering enforced (red files first: T005/T006/T010/T011); Principle VI (host-authoritative + refusal grammar) preserved. |
| A9 | Duplication | LOW | research/concept/decision | Option A scope (3 crops/Elena) consistently rejected across artifacts; no conflicting scope statement. |
| A10 | Consistency | LOW | plan/structure vs implementation | `Patches/HeirloomPatches.cs` new file vs fold into `HarvestPatches` — resolved as task-time decision (minimal-surface preference: separate postfix class, same hygiene). |

## Coverage Summary

| Requirement | Has Task? | Tasks | Notes |
|---|---|---|---|
| FR-001/FR-002/FR-005/FR-017 | Yes | T001, T002, T003, T005, T007, T012 | registration + constants + art |
| FR-003/FR-004/FR-013 | Yes | T007 | Traveler edit; SeedShop absence assertion; additive-only persistence |
| FR-006/FR-007/FR-008/FR-012/FR-018 | Yes | T010, T012, T013, T014, T018, T021 | host-authoritative seams; 014 unchanged |
| FR-009/FR-010/FR-011 | Yes | T006, T011, T015, T019, T020, T023, T024 | saved-seed yield, validator, i18n |
| FR-014/FR-015/FR-016 | Yes | T026..T032 | verify, honesty, append-only docs; no release/tag |

## Metrics

- Requirements: 18 FR + 7 SC — coverage % = 100% (buildable items).
- Tasks: 35; critical-path = Phase 1→2→3→4; parallel opportunities logged.
- Ambiguity/Duplication counts after remediation: 0 open blocking; 1 open non-blocking (A10 task-time decision).
- Critical issues: 0.

## Next actions

Proceed to `/speckit-implement`. No blocking findings remain. `after_analyze` hook checked (`.specify/extensions.yml` registers `atomic-commit-hook`, `optional: false`): no spec/plan/tasks edits were pending at analysis start — hook invocations happened on each remediated artifact instead. Advisory: re-run `/speckit-analyze` if plan/tasks drift again before implement closes.
