# Implementation Plan: Smoke Validation Script

**Branch**: `008-smoke-validation` | **Date**: 2026-10-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/008-smoke-validation/spec.md`

## Summary

A bash script that launches Stardew Valley via SMAPI with the Living Roots mod installed under a headless Xvfb display, polls the SMAPI log for the mod's success marker, and exits with a deterministic pass/fail result. The script is the project's first automated end-to-end validation tool — it transforms "does the mod load in the real game?" from a manual, error-prone task into a single command with a machine-readable exit code.

The script follows a strict lifecycle: pre-flight validation → flock acquisition → process detection → Xvfb launch → game launch → log polling → result determination → game termination → cleanup. It uses SMAPI's officially supported automation environment variables and parses the SMAPI log for mod-specific entries only. The save-data persistence check is deferred (the mod is purely reactive and SMAPI has no CLI save selection).

## Technical Context

**Language/Version**: Bash 4+ (Linux), with standard POSIX utilities (grep, pgrep, flock, sed, awk) and a Unicode normalizer (`python3`, or `uconv` as fallback) for NFC path normalization and symlink-safe file opens (`O_NOFOLLOW`)

**Primary Dependencies**: Stardew Valley game installation, SMAPI (Stardew Modding API), Xvfb (virtual framebuffer), `python3`/`uconv` (path normalization — pre-flight verified, fails closed)

**Storage**: N/A (script is stateless; reads SMAPI log, writes nothing persistent)

**Testing**: Manual validation via script execution against real game installation; exit code verification

**Target Platform**: Linux (maintainer's machine; Steam installation at `~/.steam/steam/steamapps/common/Stardew Valley/`)

**Project Type**: CLI tool / maintainer automation script

**Performance Goals**: Script completes within 120 seconds (configurable timeout). Game launch to log check typically 15-30 seconds on modern hardware.

**Constraints**: 
- Headless operation only (Xvfb)
- No save file loading (title screen is expected state)
- Local execution only (no CI integration in current scope)
- No cross-process data locking in the mod (script-level flock + process detection required)
- Not a container entrypoint — PID 1 signal semantics are out of scope
- Configuration via CLI arguments with documented defaults (no config file, no env-var mechanism)

**Scale/Scope**: Single maintainer, single game instance, single mod. No scalability concerns.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The Living Roots constitution (v2.0.0) defines three quality gates. This feature is a **maintainer tool**, not a mod feature, so the gates apply as follows:

| Gate | Application to This Feature | Status |
|------|---------------------------|--------|
| **Gate 1 — Static Checks and Tests Pass** | The script must be syntactically valid (`bash -n`), follow shellcheck conventions, and produce correct exit codes for all defined scenarios. | ✅ Pass — script is bash, validated by execution |
| **Gate 2 — Product-Level Quality Bar** | The script must be usable without documentation (clear `--help`, actionable error messages following "What → Where → How to fix" pattern). Must produce deterministic, idempotent results. | ✅ Pass — spec defines clear UX contracts |
| **Gate 3 — End-to-End Verification** | The script must actually launch the real game with the real mod and detect load success/failure. This is the entire purpose of the feature. | ✅ Pass — spec defines acceptance scenarios for real-game validation |

**Constitutional Principles Compliance**:

- **DDD**: Not applicable — this is a maintainer tool, not domain logic.
- **Security-First**: The script handles filesystem paths (SMAPI log location, mod data folder) and must sanitize/validate all paths. The mod's existing `SanitizePathSegments` pattern is referenced. Exit codes and error messages must not leak sensitive system details.
- **Async-Only**: Not applicable — bash script, not .NET async code.
- **TDD**: Not applicable — bash script, not .NET unit tests. Validation is via execution.
- **YAGNI**: The script implements only what the spec requires. No CI integration, no save check, no parallel execution, no configuration files.
- **Multiplayer**: Not applicable — the script validates single-player mod loading. Multiplayer validation is explicitly deferred (roadmap D1).

**No gate violations. No complexity tracking needed.**

## Project Structure

### Documentation (this feature)

```text
specs/008-smoke-validation/
├── spec.md              # Feature specification
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output — none needed (internal tool, no external interfaces)
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (/speckit-tasks command — NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
scripts/
└── smoke-validation.sh  # Single-file bash script — the entire feature
```

**Structure Decision**: Single-file bash script at `scripts/smoke-validation.sh`. The feature is a standalone maintainer tool with no external interfaces, no API surface, and no data persistence. A single file is the simplest structure that satisfies all requirements. The `scripts/` directory already exists in the repository for similar tooling (e.g., `scripts/check-protected-paths.sh`).

## Complexity Tracking

> No violations. This section is intentionally empty.
