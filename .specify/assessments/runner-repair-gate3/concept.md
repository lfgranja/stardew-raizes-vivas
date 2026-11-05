# Concept: Runner quality gates that keep Gate 3 trustworthy

- **Slug**: runner-repair-gate3
- **Created**: 2026-11-05
- **Recommended option**: Option A — Harden the existing runner (no new capabilities)

## Options

### Option A — Harden the existing runner (no new capabilities)

- **Sketch**: Keep the current architecture (SMAPI under Xvfb, file-queue injection, three signals, honesty ladder, promotion owner). Make the three defects that caused the misdiagnosis impossible to recur: align save names at packaging time so `SaveGame.Load` cannot silently return to title; capture the full SMAPI log (or a configurable, documented excerpt) instead of a three-pattern grep; and make the manifest a schema-validated, single-producer artifact with a detached SHA-256 anchor that the offline verifier enforces before any promotion. Add regression tests that assert these properties (subject completeness, schema validity, full-log presence) so a future change that reintroduces the defect fails CI.
- **Appetite**: small (days)
- **Trade-offs**: Wins: targets the actual failure, keeps determinism and the no-input-simulation contract, adds no dependency, and unblocks the success metric that matters (`machine-pass` on a real host, now achieved). Sacrifices: no new capability — it only makes the existing one trustworthy. Risk: the save-name fix is an inferred root cause; if a second blocker sits behind it, the fix is iterative (mitigated by the full-log capture).
- **Rabbit holes**: the save-copy path (`save_prepare` / `save_align_inner_name`) may have other assumptions about save naming; widening the log excerpt may expose noise the thresholds were tuned against; requiring `subject[]` to cover every bundle file (closing the retention-receipt gap) changes the manifest contract and needs a schema version bump.

### Option B — Add a general pre-flight quality gate for every spec

- **Sketch**: Extract the three repairs into a reusable pre-flight stage that runs before any spec's Gate 3: save-load smoke, full-log capture, manifest schema + digest check. Apply it to spec 010 and leave hooks for specs 014/015 and other mods.
- **Appetite**: medium (weeks)
- **Trade-offs**: Wins: prevents recurrence across specs and mods, and turns the repair into a shared asset. Sacrifices: scope creep into specs that have not yet reached Gate 3; risk of over-engineering a gate that only one mod uses today. Risk: the "general" parts may be Stardew/SMAPI-specific in disguise.
- **Rabbit holes**: defining what "general" means without a second consumer (rule of three); coupling the gate to the verification mod's internals; maintaining two code paths.

### Option C — Do nothing beyond the inline fix already applied

- **Sketch**: Keep the repairs as committed (commits 4b8452d..44571eb) and move on to specs 014/015. No further hardening.
- **Appetite**: none
- **Trade-offs**: Wins: zero additional cost; the immediate blocker is resolved and `machine-pass` was achieved. Sacrifices: the residual gaps (incomplete `subject[]` coverage, post-implementation red-gate logs, T138/T145 blockers) remain unaddressed, so the next failure may cost another diagnosis cycle. Risk: the repair's lessons are not captured as reusable guards.
- **Rabbit holes**: none — but the misdiagnosis risk persists for the next defect class.

## Recommendation

**Option A.** The repair already achieved its success metric (`machine-pass` on a real host, 2026-11-05, bundle `evidence-bundle-run-20261105T024756Z-07ba`, verifier rc 0). Option A is the smallest scope that makes the defect class non-recurring: it adds regression guards and closes the `subject[]`/retention-receipt gap without inventing new capabilities or a second consumer. Option B should be considered only after a second spec or mod actually needs the gate (rule of three). Option C is acceptable as a fallback if appetite is zero, but it leaves the residual gaps open and forfeits the main value of the repair (preventing the next misdiagnosis).

## Out of Scope (for the recommended option)

- Any agent-desktop integration, MCP server, or C FFI usage (decided: kill, `.specify/assessments/agent-desktop-usefulness/decision.md`).
- AT-SPI, window managers, or accessibility-tree work in the runner.
- Screenshot/OCR as a correctness oracle (screenshots remain supporting evidence only).
- Changing the honesty ladder: `verified` stays a human attestation (T148).
- Implementing specs 014/015 — those follow after machine-pass.
- General desktop computer use (installers, mod managers, Steam UI).

## Assumptions to Validate

- The save folder / inner-file name mismatch was the root cause of `DebugOutput: File does not exist`; the successful 2026-11-05 run confirms it for this milestone, but other saves may expose a second blocker.
- Widening the SMAPI excerpt to the full log does not break existing threshold checks or bundle size limits.
- The screen-space render exceptions at the title screen are unrelated noise.
- No separate desktop-automation need exists outside spec 010 (carried from problem.md).
- `subject[]` completeness (every bundle file, not just artifacts) is worth a schema version bump; to be validated during specification.