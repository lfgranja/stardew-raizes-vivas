# Implementation Plan: Smoke Validation Script

**Branch**: `008-smoke-validation` | **Date**: 2026-10-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/008-smoke-validation/spec.md`

## Summary

A bash script that launches Stardew Valley via SMAPI with the Living Roots mod installed under a headless Xvfb display, polls the SMAPI log for SMAPI's own mod-resolution output, and exits with a deterministic pass/fail result. The script is the project's first automated end-to-end validation tool — it transforms "does the mod load in the real game?" from a manual, error-prone task into a single command with a machine-readable exit code.

The script follows a strict lifecycle: pre-flight validation → build & verify artifact → flock acquisition → process detection → log baseline capture → Xvfb launch → game launch → log polling → result determination → game termination → cleanup. It uses SMAPI's officially supported automation environment variables and parses the SMAPI log for mod-specific entries only. The save-data persistence check is deferred (the mod is purely reactive and SMAPI has no CLI save selection).

## Technical Context

**Language/Version**: Bash 4+ (Linux), with standard POSIX utilities (grep, sed, awk) and a Unicode normalizer (`python3`, or `uconv` as fallback) for NFC path normalization and symlink-safe file opens (`O_NOFOLLOW`)

**Primary Dependencies**: Stardew Valley game installation, SMAPI 4.5.2 (Stardew Modding API), Xvfb (virtual framebuffer), `python3`/`uconv` (path normalization — pre-flight verified, fails closed), `mesa-utils` (`glxinfo`, `xdpyinfo`) for the graphics probe, and the **.NET SDK** (`dotnet`) for the pre-run rebuild (FR-016) — the *game* needs no host .NET runtime. The full host-tool set verified by the pre-flight is enumerated in FR-012 item 8: `bash` 4+, `Xvfb`, `xdpyinfo`, `glxinfo`, `python3`/`uconv`, `flock`, `timeout`, `mktemp`, `stat`, `realpath`, `pgrep`/`pidof`, `sha256sum`, `dotnet`.

**Process-group mechanism**: the game is launched with bash job control (`set -m`) enabled for the launch only, so the game's process-group ID equals its PID and `kill -- -$!` signals exactly that group. No extra dependency (`setsid`) is introduced. Every group signal is preceded by an assertion that the group ID differs from the script's own group ID and from `1` (FR-005).

**Launch Invocation**: `./StardewModdingAPI --no-terminal`, executed with the game installation directory as the working directory. SMAPI 4.x is a **self-contained .NET 6 `linux-x64`** deployment (`libcoreclr.so`, `libhostfxr.so`, `libhostpolicy.so` ship in the game folder), so **no system .NET runtime and no Mono runtime** is required to *launch* the game or probed by the pre-flight. (The .NET **SDK** is a separate requirement: FR-016 rebuilds the mod before each run, so `dotnet` must be on `PATH` — a missing SDK exits `9`.) The launcher must be run in place — a relocated copy aborts with SIGABRT (exit 134), which the FR-007 translation table maps to `2` (SMAPI not functional).

**Storage**: No persistent data model. The script writes exactly one thing: a per-run diagnostic log at `${XDG_STATE_HOME:-$HOME/.local/state}/living-roots-smoke/runs/<run-id>.log` (FR-014).

**Testing**: Manual validation via script execution against real game installation; exit code verification

**Target Platform**: Linux (maintainer's machine; Steam installation at `~/.steam/steam/steamapps/common/Stardew Valley/`)

**Project Type**: CLI tool / maintainer automation script

**Performance Goals**: Script completes within 120 seconds (configurable timeout) for the game launch and log check; the rebuild-and-verify step adds roughly 10–20 seconds.

**Constraints**: 
- Headless operation only (Xvfb + Mesa software rendering via `LIBGL_ALWAYS_SOFTWARE=1` / `GALLIUM_DRIVER=llvmpipe`)
- No save file loading (title screen is expected state)
- Local execution only (no CI integration in current scope)
- No cross-process data locking in the mod (script-level flock + process detection required)
- Not a container entrypoint — PID 1 signal semantics are out of scope
- Configuration via CLI arguments with documented defaults (no config file, no env-var mechanism)
- No running Steam client required (`steam_appid.txt` ships with the game)

**Scale/Scope**: Single maintainer, single game instance, single mod. No scalability concerns.

**Design Decisions (from clarification)**:

| Decision | Resolution |
|----------|------------|
| Success signal | SMAPI's `Loaded N mods:` / `Skipped mods` output — the mod's own marker is reported but does not gate (FR-003) |
| Build provenance | Rebuild, remove deploy target first, compare SHA-256 digests (FR-016) |
| Mods directory | Resolved explicitly via `--mods-path`, default `<game-path>/Mods` (FR-017) |
| Log freshness | Evaluate only lines after SMAPI's `Log started at {UTC} UTC` sentinel, plus an inode-change assertion (FR-002) |
| Diagnostic output | Per-run XDG log file with a run ID; verdict on stdout, diagnostics on stderr (FR-014) |
| Exit-code taxonomy | One distinct code per failure category — sysexits-*style* with bespoke values (1–7, 9–13, 130/143), **not** the `sysexits.h` numbering (which starts at 64); every child status is translated before the script exits (FR-007) |
| Dependency failures | `9` = a required host tool is **absent**; `3` = the graphics stack is **present but unusable** (FR-012) |
| Process-group launch | bash job control (`set -m`) for the launch only; mandatory PGID assertion before every group signal (FR-005) |
| Process identification | `/proc/$PID/exe` restricted to the resolved game path; `pidof` for discovery — never `pgrep -x` on the 17-character apphost (FR-011) |
| X11 lock cleanup | The script's own display only, and only when the owning PID is dead (FR-010) |

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The Living Roots constitution (v2.0.0) defines three quality gates. This feature is a **maintainer tool**, not a mod feature, so the gates apply as follows:

| Gate | Application to This Feature | Status |
|------|---------------------------|--------|
| **Gate 1 — Static Checks and Tests Pass** | The script must be syntactically valid (`bash -n`), pass `shellcheck`, and produce correct exit codes for all defined scenarios. | ⏳ Pending implementation — the script does not exist yet |
| **Gate 2 — Product-Level Quality Bar** | The script must be usable without documentation (clear `--help`, actionable error messages following "What → Where → How to fix" pattern). Must produce deterministic, idempotent results. | ✅ Pass — spec defines clear UX contracts |
| **Gate 3 — End-to-End Verification** | The script must actually launch the real game with the real mod and detect load success/failure. This is the entire purpose of the feature. | ⏳ **Not satisfied — awaiting execution.** The gate is a runtime gate; it was previously marked passed on the strength of the *specification* alone, which does not satisfy it. It can only be marked passed after a real run against the installed game. Note also that `Xvfb` is not installed on the maintainer's machine, so the first run requires `xorg-x11-server-Xvfb` (or `xvfb`) to be installed first — until it is, the pre-flight fails closed with exit code `9` (missing host tool) |

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
