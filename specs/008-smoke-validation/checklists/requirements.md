# Specification Quality Checklist: Smoke Validation Script

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-10-19
**Updated**: 2026-10-19
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs) — waived: this feature's spec is intentionally a technical contract (see Notes)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders — waived: audience is the maintainer of a CLI tool
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details) — waived: technical contract spec (see Notes)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria (SC-004/SC-005 aligned with the deferred save check)
- [x] No implementation details leak into specification — waived: technical contract spec (see Notes)

## Notes

- All items pass. The spec is grounded in the assessment pipeline (intake → research → define → shape → decision, verdict: go, Option A).
- 11 clarifications total: 6 resolved autonomously via 10 background agents (5 internal codebase + 5 external research), 3 resolved via user batch recommendation, 2 resolved via user batch recommendation in session 2.
- Checklist review round 3 (2026-10-19): 5 checklists reviewed by 5 parallel review agents; 6 research agents gathered evidence (shell exit codes/signals, atomic writes/idempotency, flock/NFS/Xvfb locking, path traversal/TOCTOU security, SMAPI log/crash handling, spec technical depth). 10 open questions were presented as a batch and all 10 recommendations were accepted by the user, producing FR-014, FR-015, the FR-007/FR-012 amendments, the SC-004/SC-005 deferral alignment, the quickstart test-case table and Resumption Requirements section, and the technical-contract waiver recorded in the Notes above.
- Environment-verified round 4 (2026-10-19): the spec was re-validated against the **real** SMAPI 4.5.2 installation on the maintainer's machine (10 background agents — 5 internal codebase deep-dives + 5 external research). Five decisions were presented as a batch and all five recommendations were accepted by the user: (Q1) the success gate is SMAPI's `Loaded N mods:` / `Skipped mods` output rather than the mod's self-reported marker; (Q2) the script rebuilds the mod and asserts artifact identity by SHA-256 digest; (Q3) the mods directory is resolved explicitly via `--mods-path`, defaulting to `<game-path>/Mods`; (Q4) log signals are evaluated only after SMAPI's `Log started at {UTC} UTC` sentinel; (Q5) the script writes a per-run XDG log with a run ID. Nine further corrections were resolved directly from the evidence. This produced FR-016, FR-017, SC-011…SC-014, six new acceptance scenarios, six new edge cases, and TC-9…TC-14 in quickstart.md. **Re-validation result: 16/16 → 16/16 items passing — no marker changed.**
- Checklist review round 5 (2026-10-19): 4 checklists (the ones with unchecked items) reviewed by 4 parallel review agents against the current spec/plan/quickstart/data-model; 6 background research agents gathered external evidence (exit-code taxonomy, exit-status translation, bash trap & process-group semantics, X11 lock lifecycle, PID identity verification). **Result: 344/344 → 360/360 checkbox items passing across all 5 checklists** (operational-readiness 92/92, requirements 16/16, script-reliability 88/88, security-safety 82/82, smapi-integration 82/82). One item (script-reliability 1.9) was re-grounded on empirically verified bash semantics rather than the unsupported "no subshells" claim; four stale/inconsistent statements were corrected (research.md exit-code table, checklist `smapi-integration.md` 1.3, checklist `operational-readiness.md` 2.8, checklist `security-safety.md` Anti-Patterns row 9 and item 9.4); FR-005 gained the single-`EXIT`-trap rule and the X11 residual risk was recorded in Assumptions. Five open questions (exit-code overlap `3`/`9`, uncovered internal-error and build-failure codes, process-group launch mechanism, X11 lock-removal scope, `--force` PID identity verification) were raised as a batch for the user.
- Critical correction: mod data is NOT in the save file — it uses JSON sidecar files under `~/.config/StardewValley/.smapi/mod-data/lfgranja.LivingRoots/` (SMAPI's `IModHelper.Data` resolves relative names against its mod-data directory; it never writes to a `data/` folder inside the mod folder).
- Critical correction: the success marker is emitted **by the mod** on SMAPI's `GameLaunched` event (`ModController.cs:959`), not by SMAPI after `Entry()` — so it only appears once the game reaches the title screen, which requires a working graphics device.
- Critical correction: no SMAPI release ships `smapi-internal/SMAPI.dll`; the correct markers are `StardewModdingAPI`, `StardewModdingAPI.dll`, and `smapi-internal/SMAPI.Toolkit{,.CoreInterfaces}.dll`.
- Save check is deferred — the automated script cannot load a save (no SMAPI CLI save selection, mod is purely reactive). Title screen is the expected state.
- Termination happens immediately after the log check passes — the script does NOT wait for the `Saved` event (no observable log marker exists).
- Timeout is configurable via CLI arg with 120s default.
- Local-first design with CI-ready exit codes.
- Technical details (bash, Xvfb, SMAPI log parsing) are **intentionally included** in the spec. For a maintainer-facing CLI tool, the spec acts as a technical contract; implementation-level specifics (bash, Xvfb, SMAPI log parsing, signal handling, exit codes) live in the spec and plan rather than only in the tasks phase. The "no implementation details" / "non-technical stakeholders" / "technology-agnostic SC" criteria are waived accordingly (decided 2026-10-19, checklist review round 3).
