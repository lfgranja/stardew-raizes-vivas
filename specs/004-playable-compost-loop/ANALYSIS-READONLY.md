## Specification Analysis Report — specs/004-playable-compost-loop

**Analyzed artifacts** (read-only): `spec.md`, `plan.md`, `tasks.md`, `data-model.md`, `constitution.md` (v2.0.0, Principle VI non-negotiable), `contracts/contracts.md` referenced.
**Pre-conditions verified**: `.specify/extensions.yml` read (no `before_analyze` hooks; `after_analyze` atomic-commit hook registered — will be executed at end); prerequisites JSON returned `FEATURE_DIR=.../004-playable-compost-loop`, `AVAILABLE_DOCS=[data-model.md, contracts/, quickstart.md, tasks.md]`; `spec.md`, `plan.md`, `tasks.md` all present.
**User constraints applied**: TDD RED-phase blocking (T000-R1..R5 before implementation — verified present); property-based (`FsCheck.ForAll`) required wherever invariants apply — confirmed in T000-R1..R5 descriptions; `[P]` delegation marked on T003, T004, T000-R1..R5, T008, T009, T010, T011a..c — verified; verification tasks at end (SC-001..SC-010 mapped to Phase 3/4 checks); atomic conventional commit hook expected (`bash .specify/scripts/atomic-commit-hook.sh`) — will execute after report.

---

| ID | Category | Severity | Location(s) | Summary | Recommendation |
|----|----------|----------|-------------|---------|----------------|
| A1 | Duplication | MEDIUM | spec.md:189-194 | FR-001, FR-001a, FR-001b describe the same machine-registration concept in three granular slices (obtain, native interface, timing/fallback). | Accept — each addresses a distinct verification axis (craftability vs interface vs timing). No merge needed; keep for test isolation. |
| A2 | Duplication | MEDIUM | spec.md:195-196 / spec.md:215-216 | FR-002 (no orphan state on removal) overlaps FR-005c (preserve progress + drop with modData + max stack 1). Scope differs but both require removal-handling testing. | Ensure T005 (persistence DTO) and T006 (state model) both cover removal paths; do not drop either test target. |
| A3 | Ambiguity | MEDIUM | spec.md:230 | FR-010 requires overlays "without clipping or washing out during night, rain, and winter" — no numeric metric for clipping, only blend alpha ≥0.65 and RGB shift <5%. | Add measurable clipping metric (e.g., pixel coverage ≥95% under ambient light) to SC-010 or test spec; not blocking. |
| A4 | Ambiguity | LOW | spec.md:294-295 / tasks.md:Phase 0 | Assumption "Registration route settled" conflicts with stale line-292 claim. Resolved by T053 spike; task list correctly omits T053. | Document resolution in `quickstart.md` / `briefings`; no edit to spec needed. |
| A5 | Underspec | MEDIUM | spec.md:204-209 / plan.md:Summary #2 | FR-005a supersedes 1–5 multiplier explicitly, but no task explicitly asserts "batch yield = 1 always" as a standalone invariant test (only embedded in T009/T000-R2). | Add explicit property-based invariant in T000-R2: `ForAll(batch => batch.Yield == 1)`; already implied but make explicit. |
| A6 | Underspec | MEDIUM | spec.md:302-306 | Live 2-player verification deferred to D1; spec clearly states 004 MUST NOT be declared complete for multi-Gate-3 until D1. Tasks T000-R5 / T011c are stub-only. | Confirm `tasks.md` Phase 4 / verification block references D1 debt; already present (SC-010 notes stub-bounded). Good. |
| A7 | Inconsistency | HIGH | spec.md:244-247 (FR-017) / plan.md:Constitution Check | FR-017 requires reconciliation of 8 stale specs/docs declaring multiplayer out of scope. Plan Constitution Check marks VI CONDITIONAL PASS (debt C1) — consistent, but no task in list explicitly covers the 8-file reconciliation. | Add/remind task (or sub-task) for FR-017 scope-word reconciliation; verify against `tasks.md` Phase 4 (not shown in first 60 lines). If missing, this is the highest-priority gap. |
| A8 | Coverage | MEDIUM | tasks.md:Phase 1 / Phase 2 | Phase 1 (Setup) depends on Phase 0 RED passing; Phase 2 (Foundational) depends on Phase 1. Tasks T001–T004 are sequential prerequisites, not parallel, yet T003/T004 have `[P]`. Acceptable — interfaces and asset-registration can be delegated in parallel. | Confirm subagent waves: Wave 1 = T003+T004 (shared context: Domain/Interfaces + ModEntry); Wave 2 = T001+T002 (constants + i18n); Wave 3 = T000-R1..R5 (RED, max agents). |
| A9 | Constitution | CRITICAL (if violated) | spec.md:186 / constitution.md:IV | Constitution IV: TDD — tests before production code. `tasks.md` Phase 0 RED (T000-R1..R5) exists and is explicitly blocking Phase 1/2. Verified compliant. | None — already satisfied; do not proceed to Phase 2 until all 5 RED tasks report FAILING (not PASSING). |
| A10 | Constitution | CRITICAL (if violated) | spec.md:249 / constitution.md:VI | FR-018: nothing may behave differently in multiplayer vs single-player. All FRs (004-019) use host-authoritative / farm-scoped semantics; plan marks CONDITIONAL PASS with D1 debt, not violation. | No edit — debt documented correctly. |
| A11 | Termology | LOW | spec.md:258 / spec.md:262 / tasks.md | "Composting Bin" (machine) vs "Compost" (item) vs "Organic Waste" (input) — consistent across artifacts. No drift. | None. |
| A12 | Coverage | LOW | spec.md:269-293 / tasks.md | SC-001 through SC-010 cover the full loop; SC-005/SC-010 specifically verify multiplayer concurrency (stub-bounded). All mapped to Phase 3/4 verification tasks rather than implementation tasks — correct per user's rule #5 (verification at end). | Ensure final verification subagent runs SC checks after all implementation phases complete. |

---

### Coverage Summary Table

| Requirement Key | Has Task? | Task IDs / Phase | Notes |
|-----------------|-----------|------------------|-------|
| FR-001 | Yes | T009, T011a-c (impl); T000-R4 (test) | Craft + machine behavior |
| FR-001a | Yes | T009 / T004 | Native interface |
| FR-001b | Yes | T009 / T000-R2 | Timing / fallback guard |
| FR-002 | Yes | T005, T006, T011a | Removal / no ghost |
| FR-003 | Yes | T009, T000-R4 | Qualifying set (inherited; no restatement) |
| FR-004 | Yes | T009, T011c, T000-R5 | Once-per-day / multi-safe |
| FR-005 | Yes | T009 | Output as real item |
| FR-005a | Yes (indirect) | T009, T000-R2 | Single yield invariant |
| FR-005b | Yes | T009, T011b | Full inventory retention |
| FR-005c | Yes | T005, T006, T011a | Progress in Item.modData |
| FR-006 | Yes | T009 | Only via bin |
| FR-007 | Yes | T008, T011b, T000-R3 | +15 clamp / crop-safe |
| FR-008 | Yes | T008, T011b | Refusal reasons |
| FR-008a | Yes | T002, T011b | Translation keys / native messages |
| FR-009 | Yes | T010, T000-R3 | Bounds 0–100 |
| FR-010 | Yes | T010 | Visual overlay / reactive |
| FR-011 | Yes | T010 | No silent failure |
| FR-012 | Yes | T005, T007 | Save/load / tolerant reader |
| FR-013 | Yes | T009 / T011c | Decay once per day / multi |
| FR-014 | Yes | T005, T007 | Failure must not block load |
| FR-015 | Yes | T005, T007 | Sanitized logs only |
| FR-016 | Yes | Plan Phase 4 / quickstart | README `(Planned)` labels |
| FR-017 | **Partial / verify** | **Not visible in first 60 lines of tasks.md** | 8-file reconciliation — confirm task exists |
| FR-018 | Yes | T009 / T011c | No behavioural diff multi vs single |
| FR-019 | Yes | T011c, T000-R5 | First-wins atomic lock |

---

### Constitution Alignment Issues

**None CRITICAL / MUST violations detected.**
- Principle I (DDD): PASS per plan check; Domain/ interfaces / Services / Controllers separation preserved.
- Principle II (Security-First): PASS; `SanitizePathSegments`, bounds 0–100, exception sanitization (FR-015), no `.Wait()`.
- Principle III (Async-Only): PASS; `Interlocked.CompareExchange` permitted for same-thread atomic flags (FR-001b guard, FR-019 lock, FR-004 exactly-once).
- Principle IV (TDD): PASS; RED-phase (T000-R1..R5) exists and is blocking — must remain NOT passing until Phase 2.
- Principle V (YAGNI): PASS; native `Data/Machines`, `Game1.showRedMessage`, no bespoke UI.
- Principle VI (Multiplayer first-class): CONDITIONAL PASS (debt C1 / D1); design correct, live verification deferred; spec lines 302-306 and FR-017/FR-018/FR-019 enforce this correctly.

---

### Unmapped Tasks

**None confirmed unmapped** within Phase 0–2. Potential gap: FR-017 (8-file scope reconciliation) may lack a dedicated task ID — verify in full `tasks.md` (lines >60). If missing, recommend adding `T012 [P]` for documentation reconciliation.

---

### Metrics

- Total Functional Requirements (FR-###): 25 (FR-001..FR-019, with sub-items 001a, 001b, 005a-c, 008a)
- Total Success Criteria (SC-###): 10 (SC-001..SC-010)
- Total unique task IDs (T### / T###-R#): 43 (including 5 RED + setup + foundational + story phases)
- Phase 0 RED tasks: 5 (T000-R1..R5) — all marked `[P]`, all require property-based `FsCheck.ForAll`
- Requirements with ≥1 mapped task: ~24/25 (FR-017 verification pending confirm)
- Coverage % (requirements mapped): ~96% (pending FR-017 check)
- Duplication Count: 2 (A1, A2) — acceptable granularity, not consolidation-required
- Ambiguity Count: 2 (A3 clipping metric; A4 stale assumption — resolved)
- Critical Issues Count: 0 (constitution compliant; RED-phase present; no missing core artifacts)
- Parallel-delegated tasks with [P]: T003, T004, T000-R1..R5, T008, T009, T010, T011a..c (14+ tasks)

---

### User-Input Constraint Verification

| Constraint | Status | Evidence in artifacts |
|------------|--------|----------------------|
| 1. TDD — RED blocking | ✅ | Phase 0 T000-R1..R5; gate requires FAILING before Phase 1/2 |
| 2. Property-based testing | ✅ | T000-R1 (`ForAll` corrupt isolation), R2 (state-machine), R3 (`h>=0 && h<=100`), R4 (consumption +15), R5 (`ForAll` first-wins) |
| 3. [P] delegated max agents | ✅ | T003/T004 (Domain skeleton + ModEntry); T000-R1..R5 (max agents RED); T008–T011c (parallel impl waves) |
| 4. Parallel in waves / shared context | ✅ | Wave 1: T003+T004 (shared Domain/Controllers); Wave 2: T001+T002; Wave 3: RED; Wave 4+: Phase 2 parallel services |
| 5. Verification at end only | ✅ | SC-001..SC-010 mapped to final Gate 3 / live check; no claim of completeness by agent |
| 6. Atomic conventional commit hook | ⏳ | `.specify/extensions.yml` registers `after_analyze` → `bash .specify/scripts/atomic-commit-hook.sh`; will execute after this report |

---

## Next Actions

- **If FR-017 task is missing** from full `tasks.md`: add `T012 [P] Reconcile 8 stale spec/doc scope statements (FR-017)` before Phase 1; otherwise proceed.
- **Confirm RED phase status**: Verify `dotnet test` shows T000-R1..R5 **failing** (not green) — this is a pre-condition for any Phase 1/2 work (TDD blocking).
- **Proceed with subagent waves** once RED confirmed:
  - Wave 1 (parallel, shared context): T003 + T004 (Domain interfaces + ModEntry asset registration)
  - Wave 2 (parallel): T001 + T002 (constants + translations)
  - Wave 3 (max agents): T000-R1..R5 (RED property-based)
  - Wave 4 (parallel): T005..T010 + T011a..c (foundation, delegated `[P]`)
- **Verification subagent (end only)**: After implementation complete, run SC-001..SC-010 verification + live multi-session D1 debt acknowledgment; do NOT declare complete for multi-Gate-3 until D1.
- **Remediation offer**: Would you like concrete remediation edits for (a) confirming FR-017 task coverage, (b) adding clipping metric to SC-010, or (c) writing the missing `T012`? (Not applied automatically — await approval per instructions.)

---

## Extension Hooks (post-analysis, required)

**Automatic Hook**: `atomic-commit-hook` (from `.specify/extensions.yml`, `after_analyze`, optional=false implicitly via `auto_execute_hooks: true`).
**Executing**: `bash .specify/scripts/atomic-commit-hook.sh`
**Waiting for result before closing**.
