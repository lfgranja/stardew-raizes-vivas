# Specification Quality Checklist: Composting State Machine Tests

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-09-07
**Feature**: [spec.md](../spec.md)

## Content Quality

- [ ] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [ ] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [ ] No implementation details leak into specification

## Notes

- 13/16 items pass validation
- 3 items unchecked: implementation details present (spec contains C# code snippets, type names, class names, file:line references), written for developers not non-technical stakeholders
- These are inherent to a developer-facing test spec (IEEE 829 Test Case Specification level) but should be clarified
- Clarification 1 resolved: time progression uses existing `ProcessDayStart` + `Game1.timeOfDay` pattern
- Clarification 2 resolved: fix both documented bugs before writing state transition tests
- Prerequisite fixes (cross-day elapsed time, `ProcessDayStart` wiring) are in scope
- Specification is ready for `/speckit-plan`

## Clarifications (Session 2026-09-07, Round 2)

- Q: `ConsecutiveActiveDays` is tracked at runtime but not persisted in `CompostingBinStateData`. Should this be a prerequisite fix? → A: Yes — add as Fix-3
- Q: Fix-1 states InputTimestamp changes from `int?` to `int`. The actual current type is `long?`. What is the correct fix? → A: Change from `long?` to `int?` (not `int`)
- Q: The plan claims "existing tests use real Game1 with snapshot/restore helper" but zero existing tests use Game1. How should this be described? → A: Describe as "proposed pattern" with risk acknowledgment
- Q: Research says `GameLocation(string name, string mapName)` but binary analysis shows the actual signature is `GameLocation(string mapPath, string name)`. What should test code use? → A: Update to correct signature and use `new GameLocation("Maps\\Farm", "Farm")`
- Q: The spec describes `OrganicWasteValidator` as "whitelist approach" but the implementation uses three validation lists. What is the correct description? → A: Three-list approach (Categories, Include List, Exclude List)
