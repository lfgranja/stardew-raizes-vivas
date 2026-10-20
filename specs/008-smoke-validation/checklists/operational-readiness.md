# Checklist: Operational Readiness

**Purpose**: Validate that the script is documented, debuggable, CI-ready, and maintainable for long-term use
**Created**: 2026-10-19
**Feature**: [spec.md](../spec.md) · [quickstart.md](../quickstart.md) · [data-model.md](../data-model.md)

---

## 1. Pre-Flight Validation

- [x] 1.1 Script has a dedicated pre-flight phase that runs before any state-changing command — FR-012 mandates a pre-flight phase "before launching" and item 0 pins its order (tool sweep → game/SMAPI paths → Steam prerequisite → marker removal → display start + graphics probe); the build-and-verify (FR-016), the flock, the process check and the launch all follow it. The two state-changing steps inside the phase (removing the stale markers, starting Xvfb) come after every read-only check, so no expensive or irreversible mutation precedes a validation
- [x] 1.2 No user-supplied environment variables are required, so there is nothing to validate up front — FR-015 states that the automation env vars (`SMAPI_NO_TERMINAL`, `SMAPI_DEVELOPER_MODE`, `LIBGL_ALWAYS_SOFTWARE`, `GALLIUM_DRIVER`, `SDL_VIDEODRIVER`, `SDL_AUDIODRIVER`, `ALSOFT_DRIVERS`, `DISPLAY`) "are managed by the script and are not user-configurable", and `plan.md`'s Constraints record "Configuration via CLI arguments with documented defaults (no config file, no env-var mechanism)". The only environment variables the script *honours* are the standard location overrides — `XDG_STATE_HOME` (the run log, with a `$HOME/.local/state` fallback), `XDG_RUNTIME_DIR` (the lock file, per FR-002, with an `XDG_STATE_HOME` fallback) and `TMPDIR` (the `0700` temp directory, per the Scope) — all optional, all with documented fallbacks
- [x] 1.3 All required commands are verified with `command -v` (never `which`)
- [x] 1.4 Required input files/directories are checked for existence and readability
- [x] 1.5 Checks are ordered cheapest-first — FR-012 item 0 now pins the order: required-tool presence (item 8) → game and SMAPI path checks (items 1–2) → Steam prerequisite (item 9) → crash-marker removal (item 6) → start the virtual display and probe software OpenGL (items 4–5). No check runs before the tools it needs are verified present.
- [x] 1.6 Script uses a fail-fast pattern consistently: the first failing check aborts the phase with that check's category code — FR-012 item 8 names the *first* missing tool and exits `9`; the ordered phases each stop at their first failure. (Collect-all is not used; the normative text has always said "name the tool", singular.)
- [x] 1.7 Every **failing** check emits a context-rich message (which path, which command, which threshold) following the "What → Where → How to fix" pattern — FR-007 mandates it for failures. **Passing** checks are not required to be printed: FR-014 requires the run log to contain every line the script emits at any level, so whatever a check emits is recorded there; stderr carries the level-prefixed diagnostics and stdout carries the verdict **and** the summary (run ID, `VERSION`, per-phase durations and total wall time — FR-014), so the verdict stays greppable. There is no `--verbose` option anywhere in the spec (the CLI surface is spread over FR-002, FR-005, FR-009, FR-011, FR-012, FR-016 and FR-017). No external convention requires a PASS line per check (GNU `rsync` "works silently" by default; POSIX reserves stderr for error-time diagnostics)
- [x] 1.8 Non-zero exit code is guaranteed on any pre-flight failure
- [ ] 1.9 No hardcoded assumptions about environment shape; uses config values/arguments with documented defaults — **undecided (D1)**: every path *is* configurable with a documented default (`--game-path`, `--mods-path`, `--smapi-log`, `--mod-project`), but FR-012 item 7's ASCII path allowlist `[A-Za-z0-9._/-]+` is stated without a scope, and the **default** `--game-path` (`…/Stardew Valley/`) and the game assembly `Stardew Valley.dll` both contain a space the allowlist rejects — so whether the spec still imposes a hardcoded assumption about the shape of the environment's paths is not decided
- [x] 1.10 A skipped check is clearly labeled `SKIPPED:` and counted separately from passes
- [x] 1.11 Lock/concurrency mechanism prevents overlapping runs

---

## 2. Exit Code Conventions

- [x] 2.1 Exit code 0 is returned on success
- [x] 2.2 Exit code 1 is returned for tool/runtime errors (script itself broke) (resolved by decision — the category is **renumbered, not dropped**: FR-007 now assigns `13` = internal script error to "the script itself broke", so the category is covered and `1` remains reserved for game-not-found; the generic 1=tool-error convention never applied and is withdrawn — spec.md FR-007 and the round-6 clarification)
- [x] 2.3 Exit code 2 is returned for validation/policy failures (script ran correctly but check failed) (resolved by decision — the generic 2=validation-failure convention never applied; FR-007 assigns `5` = mod load failure to "the script ran correctly but the check failed" and keeps `2` = SMAPI not found or not functional — spec.md FR-007, data-model.md exit-code table)
- [x] 2.4 No exit codes use reserved values (126, 127, 128+n, 130, 255) for non-standard purposes (resolved — the FR-007 translation table maps the reserved child statuses into documented categories (126→9, 127→9), and the only `128+N` values that reach the caller are the genuine signal deaths `129` (SIGHUP), `130` (SIGINT) and `143` (SIGTERM); no category borrows a reserved value for a non-reserved meaning, and the retired `8` is never reused — FR-007, data-model.md translation note and reserved-ranges table)
- [x] 2.5 All exit codes are documented in the script header and `--help` output
- [ ] 2.6 Exit code semantics are consistent with the documented scheme across all failure modes — **undecided (D3)**: the documented set is `0`, the **15** failure codes `1`–`7` and `9`–`16`, and the three signal statuses `129`/`130`/`143` (plus the retired `8`, never reused), but the taxonomy has open gaps — exit `16` is unreachable as specified (FR-017's manifest check runs *after* FR-016, which always deploys a valid manifest), FR-012 item 7 assigns **no** exit code, and "SMAPI aborted before mod resolution" has no code of its own. Until those are decided the scheme cannot be called complete (FR-007, data-model.md exit-code table)
- [x] 2.7 The exit-code contract is **CI-interpretable by design** (0 = success, non-zero = failure) and documented in `--help` — CI *integration* remains out of scope (FR-013), and no pipeline consumes this script today; the property claimed is the contract's design, not an integration that does not exist.
- [x] 2.8 Child command exit codes are propagated (not swallowed or collapsed to 1) (reworded by decision — a child status is **never** propagated literally: FR-007 requires every child status to be translated into a documented category, so `dotnet build` exiting `1` can no longer masquerade as "game not found". The translation table (`124`→`4`, `125`→`13`, `126`→`9`, `127`→`9`, `131`→`2`, `134`→`2`, `135`→`13`, `137`→`5`/`2`/`13`, `154`→`2`, `128+N` unchanged, everything else→`13`) is normative — FR-007, data-model.md translation note)
- [x] 2.9 `set -euo pipefail` is used so pipeline failures are caught
- [x] 2.10 One distinct exit code per failure category is followed (0=success, 1=game, 2=SMAPI, 3=graphics-unusable, 4=timeout, 5=mod failure, 6=lock, 7=process conflict, 9=missing dependency, 10=display-allocation failure, 11=Steam prerequisite, 12=build/artifact failure, 13=internal error, 14=usage, 15=no log produced, 16=mods path/manifest invalid, 129/130/143=signals) (resolved by decision — the scheme is a bespoke **one code per failure class** taxonomy with values of its own: it is *not* the `sysexits.h` numbering (which begins at `EX__BASE` = 64 and has been deprecated by FreeBSD and OpenBSD), and "sysexits-*style*" was withdrawn too because it still implies the 64–78 range. Codes are allocated additively and the retired `8` is never reused — FR-007, data-model.md exit-code table, round-6 clarification)

---

## 3. Error Message Patterns

- [x] 3.1 Every error message states what went wrong (specific, not generic)
- [x] 3.2 Every error message states how to fix it (actionable next steps)
- [x] 3.3 Error messages use the audience's terminology (no unexplained jargon)
- [x] 3.4 Error messages are framed neutrally (don't blame the user)
- [x] 3.5 Error messages go to stderr (not stdout)
- [x] 3.6 Error messages include relevant context (file paths, command names, expected vs actual values)
- [x] 3.7 Separate messages exist for each known cause (no single generic catch-all)
- [x] 3.8 When a next step is known, a `Fix:` line follows the error — the key is **`Fix:`**, not `hint:`: FR-007 makes the remediation **mandatory** ("an actionable remediation, emitted as a trailing `Fix:` line"), and all six worked examples in `quickstart.md` end with a `Fix:` line (five use `What:` / `Where:` / `Fix:`, Scenario 2 adds an `Error:` line). The token `hint:` appears nowhere in the artifacts — only the *word* "hint" survives, in quickstart's TC-6 prose ("the `--timeout` hint")
- [x] 3.9 Error messages do not expose secrets (tokens, passwords, API keys)
- [x] 3.10 The "What → Where → How to fix" pattern is used consistently across all error paths
- [x] 3.11 Failure reports include last 20-30 lines of SMAPI log with Trace-level entries following any Error — FR-007 makes the context the **matched failure record plus its continuation lines** and requires every mod Trace record that follows it (they carry the exception type and HResult); a fixed `tail -N` window is explicitly **forbidden** (a .NET stack trace runs 20–40 lines, so such a window can consist entirely of stack frames and omit the failing record), and when no record matched the context is the trailing records of the normalised stream, capped at 30

---

## 4. Script Documentation

- [x] 4.1 Script has a header block with name, purpose, version and a usage pointer — the **authors** and **last updated** fields are dropped: the project is single-maintainer (`plan.md` → Target Platform), "authors" is not knowable, and "last updated" has no format or source of truth. The normative content is the `VERSION` constant, which FR-014 requires to be reported in the run-log header, the stdout summary and `--help`
- [x] 4.2 `--help` prints usage, options, and examples to stdout, exits 0 — GNU Coding Standards §4.8: "All programs should support two standard options: `--version` and `--help`", both "on standard output, then exit successfully". `-h` is accepted as an **alias** for `--help` but is never the canonical spelling (it is not universal — `ls -h` is `--human-readable`). `--version` prints `<name> <VERSION>` to stdout and exits `0`; it is listed in FR-015's option set
- [x] 4.3 `usage()` function prints to stderr and is called on argument errors
- [x] 4.4 All exit codes are documented in `--help` output
- [x] 4.5 Prerequisites section lists required commands, env vars, and file paths
- [x] 4.6 At least one concrete usage example is provided
- [x] 4.7 A troubleshooting section exists (or links to quickstart.md)
- [x] 4.8 Comments explain tricky/non-obvious parts (not line-by-line noise)
- [x] 4.9 Script filename uses lowercase with `.sh` extension
- [x] 4.10 A `quickstart.md` guide exists with runnable validation scenarios
- [x] 4.11 `--help` documents the `--mods-path` option, the run-log location, the run ID format, the script `VERSION`, and every exit code (FR-015)

---

## 5. CI Readiness

- [x] 5.1 Script exits non-zero on any failure (CI can rely on exit code alone)
- [x] 5.2 stdout carries data/reports; stderr carries errors and human-readable messages
- [x] 5.3 `--json` flag (or equivalent) produces machine-parseable output for fine-grained CI routing (deferred by FR-013 — FR-013 excludes any machine-parseable (`--json`) mode from the current scope; the deferral is recorded in spec.md's Clarifications ("Session 2026-10-19 (checklist review)": "Support `--json`/CI mode? → A: No — defer; local-first") and ("round 3": "Should `--json`/CI output be added? → A: No — defer (local-first, YAGNI)"), with `plan.md`'s Constraints and YAGNI note; FR-014 makes the exit code the sole machine-readable signal)
- [x] 5.4 JSON output includes stable fields: `ok`, `checks[]`, `warnings[]`, `errors[]`, `exit_code` (deferred with the `--json` mode by FR-013 — FR-013 excludes any machine-parseable output mode from the current scope, so the stable-field contract is undefined; the exit code remains the only machine-readable signal, and FR-014's "Streams (MUST)" keeps the stdout verdict and the stderr diagnostics separate)
- [x] 5.5 JSON output does not contain secrets (deferred with the `--json` mode by FR-013 — no JSON output exists in the current scope, and the no-secrets rule for the human-readable output is already covered by checklist 3.9 and `plan.md`'s Constitution Compliance ("Exit codes and error messages must not leak sensitive system details"))
- [x] 5.6 The script is callable from a CI pipeline step by design: it needs no TTY (`SMAPI_NO_TERMINAL=true`, `--no-terminal` — FR-001), signals its outcome through the exit code, and keeps stdout (the verdict **and** the summary — run ID, `VERSION`, per-phase durations and total wall time, FR-014) separate from stderr (diagnostics) (FR-014 "Streams (MUST)"). CI *integration* itself remains out of scope (FR-013).
- [x] 5.7 Paths containing spaces or colons are safe — every expansion is quoted and every path argument is preceded by `--` (`security-safety.md` 5.2/5.3), so no value is re-parsed by a shell. The YAML-quoting question is N/A: CI is out of scope (FR-013).
- [x] 5.8 The script's own command chaining is `set -euo pipefail`-safe and never relies on `&&` short-circuiting to detect failure — every child status is translated through FR-007's table before the script exits, so no raw status escapes (FR-007). The YAML/CI framing is N/A: CI is out of scope (FR-013).
- [x] 5.9 Script does not depend on interactive terminal features when run in CI
- [x] 5.10 Environment variables needed by the script are documented and settable via CI variables — **reworded**: the script needs **no** user-supplied environment variable (FR-015: the automation env vars "are managed by the script and are not user-configurable"; `plan.md`: "no config file, no env-var mechanism"), so "settable via CI variables" is vacuous for them. The only honoured overrides are the standard location variables — `XDG_STATE_HOME` (the run log), `XDG_RUNTIME_DIR` (the lock file, FR-002) and `TMPDIR` (the temp directory) — which a CI job may set to redirect them
- [x] 5.11 Script uses proper exit codes (0/non-zero) so CI integration is straightforward

---

## 6. Maintainability

- [x] 6.1 ShellCheck runs clean (or all violations are explicitly suppressed with rationale) — `plan.md`'s Constitution Check (Gate 1) requires the script to pass `shellcheck`, and `lefthook.yml`'s pre-commit hook runs `bash -n` **per staged file** (the previous `bash -n {staged_files}` parsed only the first) and `shellcheck` over `scripts/*.sh`, skipping the static gate with a warning when the binary is absent — it is not installed on this host. The script does not exist yet, so neither gate has anything to lint
- [x] 6.2 `set -euo pipefail` is at the top of the script
- [x] 6.3 All variable references are quoted (double quotes for substitution, single quotes for literals)
- [x] 6.4 Functions use `local` variables (no accidental globals)
- [x] 6.5 Script is modular — reusable logic is in sourced library files (out of scope by design — `plan.md`'s "Structure Decision" records "Single-file bash script at `scripts/smoke-validation.sh`", the Clarifications ("round 2") keep sourced modules out of scope, and `plan.md`'s YAGNI note and `security-safety.md` item 1.10 agree)
- [ ] 6.6 Configuration is externalized (CLI args with documented defaults) — **undecided (D2)**: no single normative option list exists; the options are spread over eight requirements — `--smapi-log` (FR-002), `--grace-period` (FR-005), `--timeout` (FR-009), `--force` (FR-011), `--game-path` (FR-012), `--help`/`-h`/`--version` (FR-015), `--mod-project` and the round-8 `--build-timeout` (FR-016), `--mods-path` (FR-017). The enumeration this item carried was itself stale: it omitted `--build-timeout`
- [x] 6.7 Structured logging is used (log_info/log_warn/log_error, not bare echo)
- [x] 6.8 Log messages are timestamped
- [x] 6.9 Log messages include run ID / correlation ID for multi-run tracing (FR-014 — the run ID `<UTC>-<PID>` is stamped on every run-log line and reported in the summary and in every error message)
- [x] 6.10 Script is version-controlled in Git, and the script's `VERSION` constant is reported in the run-log header, the stdout summary, and `--help` output (semantic version tags are not yet used by this repo)
- [x] 6.11 `.gitignore` excludes logs, temp files, and .env files — the run log lives under `$XDG_STATE_HOME` (outside the repository), so it is never tracked
- [x] 6.12 Cleanup trap removes temp files and releases resources — the run log is written outside the temp directory and survives the cleanup
- [x] 6.13 Script follows a consistent naming convention (functions, variables, files)
- [x] 6.14 No SUID/SGID on the script
- [x] 6.15 `mktemp` is used for temp files (not predictable paths)
- [x] 6.16 Script anchors paths to its own location: `SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"` — the gap this item used to record is **closed**: FR-016 (a) makes the mod project configurable via `--mod-project`, defaulting to `<repo-root>/LivingRoots/LivingRoots.csproj`, where `<repo-root>` is derived from the script's own location (`SCRIPT_DIR/..`); the path is resolved with `realpath` and prefix-tested against the repository root (FR-012 item 7)
- [x] 6.17 No CRLF line endings (checked with `file script.sh`) — the repository's `.editorconfig` pins `end_of_line = lf`, so the script inherits the rule; the file itself does not exist yet
- [x] 6.18 `printf` is used instead of `echo -e` for escape sequences (portability)
- [x] 6.19 The verdict goes to stdout and all level-prefixed diagnostics go to stderr; the exit code remains the sole machine-readable signal (FR-013, FR-014)
- [x] 6.20 The run log directory is created with mode `0700` and one file is written per run, so concurrent runs cannot interleave — FR-014 "Run log permissions (MUST)" is stricter: **every** directory level below the XDG base (`living-roots-smoke/` **and** `runs/`, not only the leaf) must be `0700` and chmod-enforced (`mkdir -p -m 0700` applies the mode to the final component only), the log file itself must be `0600`, and each run writes its own file

---

## 7. Troubleshooting & Support

- [x] 7.1 A troubleshooting guide exists (quickstart.md contains one)
- [x] 7.2 The guide starts with symptom descriptions (not internal component names) — the symptom-keyed entries come first: the Validation Scenarios are named by symptom ("Scenario 3: Game Not Installed", "Scenario 5: Concurrent Execution Prevention", "Scenario 6: Timeout"), and the Troubleshooting section opens with the ordered ladder (D-1…D-12) — each step naming the command, the expected result, the branch and the remediation — with the exit-code index mapping symptom → cause → fix
- [x] 7.3 A quick-checks table maps symptoms to most common causes and fixes
- [x] 7.4 Diagnostic steps are ordered cheapest-first — `quickstart.md` now has an ordered **diagnostic ladder** (D-1…D-12) ordered by decreasing likelihood with cost as the tiebreak, each step stating the exact command, its expected result, the branch when it does not hold, the remediation and the post-fix expectation; the exit-code table is retained as a quick-reference index.
- [x] 7.5 Each diagnostic step has an explicit expected result — every row of the ladder has an "Expected result if OK" column and an "Expected result once fixed" column.
- [x] 7.6 Each step branches clearly — the ladder's "If NOT ok →" column names the next step for D-1…D-8; the four terminal rows (D-9…D-12, one per exit code) branch to their own remediation ("see the remediation in this row") rather than to a further step
- [x] 7.7 Each resolution includes the expected result once fixed — the ladder's "Expected result once fixed" column states it for every step.
- [x] 7.8 An escalation path names the contact, channel, and required evidence
- [x] 7.9 Escalation evidence requirements are specified (timestamps, logs, error text, repro steps)

---

## 8. ISO/IEC/IEEE 29119-Inspired Test Documentation

- [x] 8.1 Pass/fail criteria are defined before the script runs (not after)
- [x] 8.2 A test log is produced chronologically (IEEE 829 test log concept)
- [x] 8.3 Suspension criteria are defined (when to stop the script early) — `quickstart.md` "Suspension Criteria" (S-1…S-3): campaign-level criteria, the authority that may suspend/resume (the L1 maintainer), and what is recorded on suspension. Suspension is deliberately a **campaign-level** concept, not a per-run state: an individual run always terminates with an exit code from the FR-007 taxonomy, so no new exit code is needed (ISO/IEC/IEEE 29119-3 §6.2.7.9 → ISO/IEC/IEEE 29119-2 §8.4.4 NOTE 1)
- [x] 8.4 Resumption requirements are defined (what must be true to re-run) — quickstart.md "Resumption Requirements" (R-1…R-8 — the list ends at R-8; there is no R-9) **and** "Activities to Be Repeated on Resumption" (which testing activities must be repeated — the second half of ISO/IEC/IEEE 29119-3 §6.2.7.9 / IEEE 829-2008 §9.2.7)
- [x] 8.5 Environmental needs are documented (hardware, software, tools)
- [x] 8.6 Features NOT tested are explicitly listed (scope boundaries — save check deferred)
- [x] 8.7 Test items are identified (what specifically is being validated)
- [x] 8.8 Each test case has: input, execution condition, expected output, actual output, pass/fail — quickstart.md "Test Case Specification" (TC-1…TC-17), now carrying the ISO/IEC/IEEE 29119-3 Annex A fields: unique identifier, **Objective**, **Priority**, **Traceability** (FR-/SC-), **Preconditions**, Inputs, Expected results, Actual results, Test result — plus a stated **execution order** and a named **oracle** (the FR-007 exit-code contract plus the `Log started at` sentinel predicate)
- [x] 8.9 Resumption requirements cover the stale-log and stale-artifact preconditions (quickstart.md R-1…R-8 — R-7 covers the artifact identity, R-8 the stale log, R-5 the leftover crash marker)

---

## Common Gaps Verified Absent

| Gap | Status |
|-----|--------|
| Silent failures from `&&` chaining in CI | Must use `set -e` or wrap in `( ... )` |
| Unset variables passing as empty strings | `set -u` required |
| `which` instead of `command -v` | Use `command -v` |
| Error messages going to stdout | Must go to stderr |
| No `--help` flag | Required |
| Hardcoded paths or values | Must be configurable |
| Missing cleanup trap | Must have `trap ... EXIT` |
| No structured logging | Use level-prefixed, timestamped logs |
| ShellCheck violations | Must run clean |
| No JSON output mode | Deferred per FR-013 (local-first); exit codes suffice for future CI |
| Secrets in output | Must redact |
| Script exits 0 on pre-flight failure | Must exit non-zero |
| No concurrency protection | flock required |
| No version tracking in output | Include `$VERSION` in logs — now stamped on every run-log line and reported in the summary and `--help` (FR-014) |
| No run ID for correlation | Generate a run ID (`<UTC>-<PID>`) and stamp it on every log line and error message (FR-014) |
| Missing escalation path | Troubleshooting guide must have one |
| `echo -e` for escape sequences | Use `printf` |
| Relative paths breaking from different CWDs | Anchor to `SCRIPT_DIR` |
| CRLF line endings | Check and normalize |

---

## Sources

- Google Testing Blog: https://testing.googleblog.com/
- IEEE 829-2008: https://ieeexplore.ieee.org/document/4578383
- sysexits.h: https://man7.org/linux/man-pages/man3/sysexits.h.3head.html
- Google Shell Style Guide: https://google.github.io/styleguide/shellguide.html
- ShellCheck: https://www.shellcheck.net/
- GitLab CI troubleshooting: https://docs.gitlab.com/ci/yaml/script_troubleshooting
- Microsoft Error Message Guidelines: https://learn.microsoft.com/en-us/windows/win32/debug/error-message-guidelines
- Atlassian Continuous Delivery: https://www.atlassian.com/continuous-delivery/principles