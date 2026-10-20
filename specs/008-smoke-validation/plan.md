# Implementation Plan: Smoke Validation Script

**Branch**: `008-smoke-validation` | **Date**: 2026-10-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/008-smoke-validation/spec.md`

## Summary

A bash script that launches Stardew Valley via SMAPI with the Living Roots mod installed under a headless Xvfb display, polls the SMAPI log for SMAPI's own mod-resolution output, and exits with a deterministic pass/fail result. The script is the project's first automated end-to-end validation tool — it transforms "does the mod load in the real game?" from a manual, error-prone task into a single command with a machine-readable exit code.

The script follows a strict lifecycle: pre-flight validation → build & verify artifact → flock acquisition → process detection → log baseline capture → Xvfb launch → game launch → log polling → result determination → game termination → cleanup. It uses SMAPI's officially supported automation environment variables and parses the SMAPI log for mod-specific entries only. The save-data persistence check is deferred (the mod is purely reactive and SMAPI has no CLI save selection).

## Technical Context

**Language/Version**: Bash 4+ (Linux), with standard POSIX utilities (grep, sed, awk, date) and no external language runtime

**Primary Dependencies**: Stardew Valley game installation, SMAPI 4.5.2 (Stardew Modding API), Xvfb (virtual framebuffer), `mesa-utils` (`glxinfo`, `xdpyinfo`) for the graphics probe, and the **.NET SDK** (`dotnet`) for the pre-run rebuild (FR-016) — the *game* needs no host .NET runtime. The full host-tool set verified by the pre-flight is enumerated in FR-012 item 8: `bash` 4+, `Xvfb`, `xdpyinfo`, `glxinfo`, `flock`, `timeout`, `mktemp`, `stat`, `realpath`, `pgrep`/`pidof`, `sha256sum`, `date`, `grep` and `dotnet`. **No Unicode normalizer is required** — the round-7 decision replaced NFC normalization with an explicit ASCII path allowlist, because NFC is a provable no-op for ASCII paths (UAX #15) and is not a homoglyph defence (UTS #39).

**Process-group mechanism**: **each** child — the game **and** Xvfb — is launched with bash job control (`set -m`) enabled for that launch only, so each child's process-group ID equals its PID and `kill -- -$!` signals exactly that group. No extra dependency (`setsid`) is introduced: `setsid` is excluded because it would add a dependency, not because it forks — measured on this host, `setsid sleep 30 &` does **not** fork (the background child of a non-interactive script is not already a process-group leader), so the earlier rationale was wrong. Every group signal is preceded by an assertion that the group ID differs from the script's own group ID and from `1` (FR-005), and the assertion applies to **both** children: a child launched without `set -m` shares the script's group, so a group signal to it would either abort the run with `13` before the child is killed — leaving it orphaned — or terminate the script itself. `set -m` is deliberately **not** left enabled for the whole script: that would move the script out of the terminal's foreground process group, so a terminal-generated SIGINT would reach the poll loop's foreground `sleep` instead of the script, and the `INT` handler would never run (measured under a pty).

**Launch Invocation**: `./StardewModdingAPI --no-terminal`, executed with the game installation directory as the working directory. SMAPI 4.x is a **self-contained .NET 6 `linux-x64`** deployment (`libcoreclr.so`, `libhostfxr.so`, `libhostpolicy.so` ship in the game folder), so **no system .NET runtime and no Mono runtime** is required to *launch* the game or probed by the pre-flight. (The .NET **SDK** is a separate requirement: FR-016 rebuilds the mod before each run, so `dotnet` must be on `PATH` — a missing SDK exits `9`.) The launcher must be run in place: a relocated copy fails in one of four measured ways — **154** (assembly missing), **131** (.NET runtime missing), **135** (partial runtime — "Could not resolve CoreCLR path") or **134** (SIGABRT: the runtime is present but the game files are not, so SMAPI's `AssertGamePresent()` calls `PressAnyKeyToExit()`, whose `Console.ReadKey()` throws on the `/dev/null` stdin FR-005 mandates). FR-007's translation table maps the first, second and fourth to `2` (SMAPI not functional) and the third to `13`.

**Display allocation**: Xvfb is started with `-displayfd`, so the **X server itself** binds the first free display and writes the number back; the script never chooses a display number and never touches `/tmp/.X{n}-lock` files (`-displayfd` disables the server lock entirely). The script records the reported number for the cleanup phase and exports `DISPLAY` from it.

**Artifact identity**: the build's deploy target is pinned to the resolved `--mods-path` via `-p:GameModsPath`, and the deployed assembly's SHA-256 digest is compared with the freshly built one **within the same run** — the standard deploy-effectiveness assertion (file-integrity monitoring / SLSA subject digest). The digest is never compared across runs, because the .NET compiler's deterministic content hash is a function of the absolute source path.

**Storage**: No persistent data model of the script's own — it reads external artifacts (the SMAPI log, the mod build artifact, the mod data files) and produces a single exit code. What it **writes** is more than one thing, and the exhaustive list is in `spec.md` → Scope → **Declared side effects**: the per-run diagnostic log at `${XDG_STATE_HOME:-$HOME/.local/state}/living-roots-smoke/runs/<run-id>.log` (FR-014), the build output in `LivingRoots/bin/` and `LivingRoots/obj/` plus the NuGet cache, the deployed mod tree under the resolved mods path, the lock file it leaves in place, the X11 socket it creates and removes, and the temp directory it creates and removes.

**Testing**: Manual validation via script execution against real game installation; exit code verification

**Target Platform**: Linux (maintainer's machine; Steam installation at `~/.steam/steam/steamapps/common/Stardew Valley/`)

**Project Type**: CLI tool / maintainer automation script

**Performance Goals**: The script's wall time is the sum of three **independently bounded** phases, each of which reports its measured duration in the run log (FR-014). Measured on the maintainer's machine (8 cores, .NET SDK 10.0.400):

| Phase | Measured | Bound |
|---|---|---|
| Pre-flight (tool sweep, path probes, display allocation, graphics probe) | ~1–3 s | not separately bounded; fails closed |
| Rebuild-and-verify (FR-016) | 6.6 s warm · 8.8–10.6 s from a clean tree (restore ~2 s) · **5.0–26.2 s under host load** | `--build-timeout`, default **300 s** |
| Launch-to-verdict (exec → SMAPI's loaded-mods block) | **~15 s unloaded · 29.6–41.8 s under host load** | `--timeout`, default **120 s** |
| Termination (SIGTERM → grace) | 0.3–1 s typical, 15 s worst | `--grace-period`, default 15 s |

Typical total **~25 s**; worst observed **~85 s**. **The 120 s figure is a per-phase bound on the launch-to-verdict window, not a whole-script bound** — the whole script's duration is the sum of the phases and is *reported*, not enforced. A whole-script cap of 120 s would leave only ~1.4× headroom in the worst measured state and would manufacture the very flakiness the test is meant to detect (absolute-timing flakiness rises with test size).

**Constraints**: 
- Headless operation only (Xvfb + Mesa software rendering via `LIBGL_ALWAYS_SOFTWARE=1` / `GALLIUM_DRIVER=llvmpipe`)
- No save file loading (title screen is expected state)
- Local execution only (no CI integration in current scope) — but the exit-code contract is CI-interpretable by design
- No cross-process data locking in the mod (script-level flock + process detection required)
- Not a container entrypoint — PID 1 signal semantics are out of scope
- Configuration via CLI arguments with documented defaults (no config file, no env-var mechanism)
- No running Steam client required (`steam_appid.txt` ships with the game)
- **Environment-derived paths are unrestricted**: the game path, the mods path, the SMAPI log path and the mod project path may contain spaces and non-ASCII characters; no character allowlist is applied to them. Path safety rests on `realpath` canonicalization, a component-boundary prefix test, correct quoting (every path expanded as `"$var"` and passed after `--`), and the open-without-gating rule. The script's own authored path literals are constrained to `[A-Za-z0-9._-]+`. No Unicode normalization
- The script **mutates** the workspace and the game installation — it deletes and recreates `<mods-path>/LivingRoots/`, writes `LivingRoots/bin/`, removes stale SMAPI log artifacts, and creates a lock file it leaves in place (see spec.md → Scope → Declared side effects)

**Scale/Scope**: Single maintainer, single game instance, single mod. No scalability concerns.

**Design Decisions (from clarification)**:

| Decision | Resolution |
|----------|------------|
| Success signal | SMAPI's loaded-mods block / `   Skipped mods` section — the skipped-mods header carries **three** leading spaces and the loaded-mods header is `Loaded N mods:` or `Loaded 0 mods.`; the mod's own marker is reported but does not gate (FR-003) |
| Build provenance | Rebuild, remove deploy target first, compare SHA-256 digests (FR-016) |
| Mods directory | Resolved explicitly via `--mods-path`, default `<game-path>/Mods` (FR-017) |
| Log freshness | Evaluate only lines after SMAPI's `Log started at {UTC} UTC` sentinel, plus an inode-change assertion (FR-002) |
| Diagnostic output | Per-run XDG log file with a run ID; verdict on stdout, diagnostics on stderr (FR-014) |
| Exit-code taxonomy | One distinct code per failure category — a bespoke one-code-per-class scheme (**not** the `sysexits.h` numbering, which starts at 64 and is deprecated by FreeBSD and OpenBSD; the "sysexits-*style*" label is withdrawn too). Every child status is translated before the script exits. Round 7 added `14` (usage), `15` (no log produced) and `16` (mods path/manifest invalid), made `11` reachable via a `steam_appid.txt` check, and added `129` for SIGHUP. Round 8 added `17` (SMAPI aborted before mod resolution), allocated above the highest published code |
| Dependency failures | `9` = a required host tool is **absent**; `3` = the graphics stack is **present but unusable** |
| Process-group launch | bash job control (`set -m`) around **each** child launch (the game **and** Xvfb); mandatory PGID assertion before every group signal (a failed assertion exits `13`). `set -m` is scoped to the launches — leaving it on for the whole script would break the `INT` handler (FR-005) |
| Process identification | `/proc/$PID/exe` restricted to the resolved game path; `pidof` for discovery — never `pgrep -x` on the 17-character apphost |
| Display allocation | `Xvfb -displayfd` — the X server binds the first free display atomically; the script neither chooses a number nor manages `/tmp/.X{n}-lock` files. Exit `10` = the display could not be allocated or started |
| Artifact identity | **Deploy effectiveness**: the build's deploy target is pinned to the resolved `--mods-path` (`-p:GameModsPath`) and the deployed assembly's digest is compared with the fresh build's **within the run**. No cross-build digest comparison, and no source-correspondence claim (the assembly carries no build identity) |
| Path validation | Canonicalize with `realpath` (`-e` where the path must exist, `-m` where it need not), then a component-boundary prefix test against the **canonicalized** base, on paths the script *constructs*; user-supplied roots are their own bases. **No character allowlist on environment-derived names** (the game path legitimately contains a space); the script's own authored literals are constrained to `[A-Za-z0-9._-]+`, and every path expansion is quoted and passed after `--`. No Unicode normalization |
| Log freshness | Evaluate every gating signal only against lines after the `Log started at {UTC} UTC` sentinel; the **header region** (version banner, `Mods go here:`) precedes the sentinel and is context only. The `Mods go here:` path is `~`-anonymised |
| Trap design | One `EXIT` trap (registered exactly once) plus `INT`/`TERM`/`HUP` handlers that reset-and-re-raise; `trap '' INT TERM` is prohibited; cleanup is idempotent and time-bounded. The script runs under `set -Eeuo pipefail` — the `-E` is required so the `ERR` trap is inherited by shell functions and can report the enclosing function name (FR-014) |
| CLI surface | A single normative **Command-Line Options** table in `spec.md` owns the spelling, argument placeholder and default of every option; the requirements own the behaviour; `--help` and the quickstart are **renderings** of the table (the `help2man` relation). Long form only except `-h`, no abbreviation, `--opt=VALUE` accepted, `--` ends option processing, no positional arguments |

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

The Living Roots constitution (v2.0.0) defines three quality gates. This feature is a **maintainer tool**, not a mod feature, so the gates apply as follows:

| Gate | Application to This Feature | Status |
|------|---------------------------|--------|
| **Gate 1 — Static Checks and Tests Pass** | The script must be syntactically valid (`bash -n`), pass `shellcheck`, and produce correct exit codes for all defined scenarios. Round 7 added a `bash -n` + `shellcheck` step to `lefthook.yml`'s pre-commit hook (no CI job, no `required_status_checks` entry, so the repo's dormant-job warning is respected). Round 8 fixed the hook: `bash -n` is now run **per file** (the previous `bash -n {staged_files}` parsed only the first file and silently passed a broken second one — measured), and the `shellcheck` step is skipped when the binary is absent, because `shellcheck` is **not installed** on the maintainer's machine and the hook would otherwise abort every commit that touches `scripts/*.sh` | ⏳ Pending implementation — the script does not exist yet; the lint hook is wired but has nothing to lint |
| **Gate 2 — Product-Level Quality Bar** | The script must be usable without documentation (clear `--help`, actionable error messages following "What → Where → How to fix" pattern). Must produce deterministic, idempotent results. | ✅ Pass — spec defines clear UX contracts |
| **Gate 3 — End-to-End Verification** | The script must actually launch the real game with the real mod and detect load success/failure. This is the entire purpose of the feature. | ⏳ **Not satisfied — awaiting execution.** The gate is a runtime gate; it was previously marked passed on the strength of the *specification* alone, which does not satisfy it. It can only be marked passed after a real run against the installed game. `Xvfb` **is now installed** on the maintainer's machine (`xorg-x11-server-Xvfb` 21.1.24, installed 2026-10-20), so the pre-flight's tool sweep now passes and the end-to-end run is unblocked; `shellcheck` is still absent, so the static lint gate skips itself |

**Constitutional Principles Compliance**:

- **DDD**: Not applicable — this is a maintainer tool, not domain logic.
- **Security-First**: The script handles filesystem paths (SMAPI log location, mod data folder) and must sanitize/validate all paths. The surviving control is FR-012 item 7 — canonicalization with `realpath` plus a component-boundary prefix test — and **not** a port of the mod's `SanitizePathSegments`, which round 7 dropped as vacuous for this CLI surface. Exit codes and error messages must not leak sensitive system details.
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
