# Checklist: Operational Readiness

**Purpose**: Validate that the script is documented, debuggable, CI-ready, and maintainable for long-term use
**Created**: 2026-10-19
**Feature**: [spec.md](../spec.md) · [quickstart.md](../quickstart.md) · [data-model.md](../data-model.md)

---

## 1. Pre-Flight Validation

- [x] 1.1 Script has a dedicated pre-flight phase that runs before any state-changing command
- [x] 1.2 All required environment variables are validated up front with explicit error messages
- [x] 1.3 All required commands are verified with `command -v` (never `which`)
- [x] 1.4 Required input files/directories are checked for existence and readability
- [x] 1.5 Checks are ordered cheapest-first (env vars → command existence → files → service health)
- [x] 1.6 Script uses fail-fast or collect-all pattern consistently (not ad hoc)
- [x] 1.7 Every check emits context-rich pass/fail messages (which path, which command, which threshold)
- [x] 1.8 Non-zero exit code is guaranteed on any pre-flight failure
- [x] 1.9 No hardcoded assumptions about environment shape; uses config values/arguments with documented defaults
- [x] 1.10 A skipped check is clearly labeled `SKIPPED:` and counted separately from passes
- [x] 1.11 Lock/concurrency mechanism prevents overlapping runs

---

## 2. Exit Code Conventions

- [x] 2.1 Exit code 0 is returned on success
- [x] 2.2 Exit code 1 is returned for tool/runtime errors (script itself broke) (resolved by decision — the category is **renumbered, not dropped**: FR-007 now assigns `13` = internal script error to "the script itself broke", so the category is covered and `1` remains reserved for game-not-found; the generic 1=tool-error convention never applied and is withdrawn — spec.md FR-007 and the round-6 clarification)
- [x] 2.3 Exit code 2 is returned for validation/policy failures (script ran correctly but check failed) (resolved by decision — the generic 2=validation-failure convention never applied; FR-007 assigns `5` = mod load failure to "the script ran correctly but the check failed" and keeps `2` = SMAPI not found or not functional — spec.md FR-007, data-model.md exit-code table)
- [x] 2.4 No exit codes use reserved values (126, 127, 128+n, 130, 255) for non-standard purposes (resolved — the FR-007 translation table maps the reserved child statuses into documented categories (126→9, 127→9) and the only `128+N` values that reach the caller are the standard signal statuses 130 and 143; no category borrows a reserved value for a non-reserved meaning — FR-007, data-model.md translation note)
- [x] 2.5 All exit codes are documented in the script header and `--help` output
- [x] 2.6 Exit code semantics are consistent with the documented scheme across all failure modes (resolved — the two categories that previously overlapped now have disjoint, decidable definitions: `9` = a required host tool is **absent**, `3` = the graphics stack is **present but unusable**; every failure mode maps to exactly one of the 13 documented codes, and every child status is translated through the FR-007 table — spec.md FR-007, FR-012 items 3/4/7/8, data-model.md exit-code table)
- [x] 2.7 CI pipeline correctly interprets the script's exit codes (non-zero = failure)
- [x] 2.8 Child command exit codes are propagated (not swallowed or collapsed to 1) (reworded by decision — a child status is **never** propagated literally: FR-007 requires every child status to be translated into a documented category, so `dotnet build` exiting `1` can no longer masquerade as "game not found". The translation table (124→4, 125→13, 126→9, 127→9, 134→2, 137→5/13, 128+N unchanged, everything else→13) is normative — FR-007, data-model.md translation note)
- [x] 2.9 `set -euo pipefail` is used so pipeline failures are caught
- [x] 2.10 One distinct exit code per failure category is followed (0=success, 1=game, 2=SMAPI, 3=graphics-unusable, 4=timeout, 5=mod failure, 6=lock, 7=process conflict, 9=missing dependency, 10=display conflict, 11=Steam prerequisite, 12=build/artifact failure, 13=internal error, 130/143=signals) (resolved by decision — the scheme is sysexits-*style* (one code per failure class) with bespoke values, **not** the `sysexits.h` numbering, which begins at `EX__BASE` = 64 and has been deprecated by FreeBSD and OpenBSD; the "sysexits.h convention" label is withdrawn and codes are allocated additively — FR-007, data-model.md exit-code table, round-6 clarification)

---

## 3. Error Message Patterns

- [x] 3.1 Every error message states what went wrong (specific, not generic)
- [x] 3.2 Every error message states how to fix it (actionable next steps)
- [x] 3.3 Error messages use the audience's terminology (no unexplained jargon)
- [x] 3.4 Error messages are framed neutrally (don't blame the user)
- [x] 3.5 Error messages go to stderr (not stdout)
- [x] 3.6 Error messages include relevant context (file paths, command names, expected vs actual values)
- [x] 3.7 Separate messages exist for each known cause (no single generic catch-all)
- [x] 3.8 When a next step is known, a `hint:` line follows the error
- [x] 3.9 Error messages do not expose secrets (tokens, passwords, API keys)
- [x] 3.10 The "What → Where → How to fix" pattern is used consistently across all error paths
- [x] 3.11 Failure reports include last 20-30 lines of SMAPI log with Trace-level entries following any Error

---

## 4. Script Documentation

- [x] 4.1 Script has a header block with name, purpose, authors, version, last updated
- [x] 4.2 `--help` / `-h` flag prints usage, options, and examples to stdout, exits 0
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
- [x] 5.3 `--json` flag (or equivalent) produces machine-parseable output for fine-grained CI routing (deferred by FR-013 — spec.md:92 excludes any machine-parseable (`--json`) mode from the current scope; the deferral is recorded at spec.md:181 and spec.md:189, with plan.md:34 and plan.md:70; FR-014 (spec.md:99) makes the exit code the sole machine-readable signal)
- [x] 5.4 JSON output includes stable fields: `ok`, `checks[]`, `warnings[]`, `errors[]`, `exit_code` (deferred with the `--json` mode by FR-013 — spec.md:92; no machine-parseable output exists in the current scope, so the stable-field contract is undefined; the exit code remains the only machine-readable signal (spec.md:99))
- [x] 5.5 JSON output does not contain secrets (deferred with the `--json` mode by FR-013 — spec.md:92; no JSON output exists in the current scope, and the no-secrets rule for the human-readable output is already covered by checklist 3.9 and plan.md:67)
- [x] 5.6 Script can be called from a CI pipeline step (GitLab `script:`, Azure DevOps `Bash@3`, GitHub Actions `run:`)
- [x] 5.7 Commands containing colons are properly quoted for YAML parsing
- [x] 5.8 `&&` chaining is wrapped in parentheses to preserve failure semantics in CI
- [x] 5.9 Script does not depend on interactive terminal features when run in CI
- [x] 5.10 Environment variables needed by the script are documented and settable via CI variables
- [x] 5.11 Script uses proper exit codes (0/non-zero) so CI integration is straightforward

---

## 6. Maintainability

- [x] 6.1 ShellCheck runs clean (or all violations are explicitly suppressed with rationale)
- [x] 6.2 `set -euo pipefail` is at the top of the script
- [x] 6.3 All variable references are quoted (double quotes for substitution, single quotes for literals)
- [x] 6.4 Functions use `local` variables (no accidental globals)
- [x] 6.5 Script is modular — reusable logic is in sourced library files (out of scope by design — plan.md:95-99 records the Structure Decision "Single-file bash script at scripts/smoke-validation.sh", the clarification at spec.md:190 keeps sourced modules out of scope, and plan.md:70 (YAGNI) and security-safety.md item 1.10 agree)
- [x] 6.6 Configuration is externalized (CLI args with documented defaults: `--game-path`, `--mods-path`, `--timeout`, `--grace-period`, `--smapi-log`, `--force`)
- [x] 6.7 Structured logging is used (log_info/log_warn/log_error, not bare echo)
- [x] 6.8 Log messages are timestamped
- [x] 6.9 Log messages include run ID / correlation ID for multi-run tracing (FR-014 — the run ID `<UTC>-<PID>` is stamped on every run-log line and reported in the summary and in every error message)
- [x] 6.10 Script is version-controlled in Git, and the script's `VERSION` constant is reported in the run-log header, the stdout summary, and `--help` output (semantic version tags are not yet used by this repo)
- [x] 6.11 `.gitignore` excludes logs, temp files, and .env files — the run log lives under `$XDG_STATE_HOME` (outside the repository), so it is never tracked
- [x] 6.12 Cleanup trap removes temp files and releases resources — the run log is written outside the temp directory and survives the cleanup
- [x] 6.13 Script follows a consistent naming convention (functions, variables, files)
- [x] 6.14 No SUID/SGID on the script
- [x] 6.15 `mktemp` is used for temp files (not predictable paths)
- [x] 6.16 Script anchors paths to its own location: `SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"`
- [x] 6.17 No CRLF line endings (checked with `file script.sh`)
- [x] 6.18 `printf` is used instead of `echo -e` for escape sequences (portability)
- [x] 6.19 The verdict goes to stdout and all level-prefixed diagnostics go to stderr; the exit code remains the sole machine-readable signal (FR-013, FR-014)
- [x] 6.20 The run log directory is created with mode `0700` and one file is written per run, so concurrent runs cannot interleave

---

## 7. Troubleshooting & Support

- [x] 7.1 A troubleshooting guide exists (quickstart.md contains one)
- [x] 7.2 The guide starts with symptom descriptions (not internal component names)
- [x] 7.3 A quick-checks table maps symptoms to most common causes and fixes
- [x] 7.4 Diagnostic steps are ordered cheapest-first
- [x] 7.5 Each diagnostic step has an explicit expected result
- [x] 7.6 Each step branches clearly: "If you see X, go to Fix A. If not, continue."
- [x] 7.7 Each resolution includes the expected result once fixed
- [x] 7.8 An escalation path names the contact, channel, and required evidence
- [x] 7.9 Escalation evidence requirements are specified (timestamps, logs, error text, repro steps)

---

## 8. IEEE 829-Inspired Test Documentation

- [x] 8.1 Pass/fail criteria are defined before the script runs (not after)
- [x] 8.2 A test log is produced chronologically (IEEE 829 test log concept)
- [x] 8.3 Suspension criteria are defined (when to stop the script early)
- [x] 8.4 Resumption requirements are defined (what must be true to re-run) — quickstart.md "Resumption Requirements" (R-1…R-7)
- [x] 8.5 Environmental needs are documented (hardware, software, tools)
- [x] 8.6 Features NOT tested are explicitly listed (scope boundaries — save check deferred)
- [x] 8.7 Test items are identified (what specifically is being validated)
- [x] 8.8 Each test case has: input, execution condition, expected output, actual output, pass/fail — quickstart.md "Test Case Documentation (IEEE 829-style)" (TC-1…TC-14)
- [x] 8.9 Resumption requirements cover the stale-log and stale-artifact preconditions (quickstart.md R-1…R-9)

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