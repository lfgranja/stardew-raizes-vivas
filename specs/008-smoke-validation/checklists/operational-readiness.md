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
- [x] 2.2 Exit code 1 is returned for tool/runtime errors (script itself broke)
- [x] 2.3 Exit code 2 is returned for validation/policy failures (script ran correctly but check failed)
- [x] 2.4 No exit codes use reserved values (126, 127, 128+n, 130, 255) for non-standard purposes
- [x] 2.5 All exit codes are documented in the script header and `--help` output
- [x] 2.6 Exit code semantics are consistent with the documented scheme across all failure modes
- [x] 2.7 CI pipeline correctly interprets the script's exit codes (non-zero = failure)
- [x] 2.8 Child command exit codes are propagated (not swallowed or collapsed to 1)
- [x] 2.9 `set -euo pipefail` is used so pipeline failures are caught
- [x] 2.10 sysexits.h convention is followed with distinct codes per failure category (1=game, 2=SMAPI, 3=Xvfb, 4=timeout, 5=mod failure, 6=lock, 7=process conflict)

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

---

## 5. CI Readiness

- [x] 5.1 Script exits non-zero on any failure (CI can rely on exit code alone)
- [x] 5.2 stdout carries data/reports; stderr carries errors and human-readable messages
- [x] 5.3 `--json` flag (or equivalent) produces machine-parseable output for fine-grained CI routing
- [x] 5.4 JSON output includes stable fields: `ok`, `checks[]`, `warnings[]`, `errors[]`, `exit_code`
- [x] 5.5 JSON output does not contain secrets
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
- [x] 6.5 Script is modular — reusable logic is in sourced library files
- [x] 6.6 Configuration is externalized (config file or env vars, not hardcoded)
- [x] 6.7 Structured logging is used (log_info/log_warn/log_error, not bare echo)
- [x] 6.8 Log messages are timestamped
- [x] 6.9 Log messages include run ID / correlation ID for multi-run tracing
- [x] 6.10 Script is version-controlled in Git with semantic version tags
- [x] 6.11 `.gitignore` excludes logs, temp files, and .env files
- [x] 6.12 Cleanup trap removes temp files and releases resources
- [x] 6.13 Script follows a consistent naming convention (functions, variables, files)
- [x] 6.14 No SUID/SGID on the script
- [x] 6.15 `mktemp` is used for temp files (not predictable paths)
- [x] 6.16 Script anchors paths to its own location: `SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"`
- [x] 6.17 No CRLF line endings (checked with `file script.sh`)
- [x] 6.18 `printf` is used instead of `echo -e` for escape sequences (portability)

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
- [x] 8.4 Resumption requirements are defined (what must be true to re-run)
- [x] 8.5 Environmental needs are documented (hardware, software, tools)
- [x] 8.6 Features NOT tested are explicitly listed (scope boundaries — save check deferred)
- [x] 8.7 Test items are identified (what specifically is being validated)
- [x] 8.8 Each test case has: input, execution condition, expected output, actual output, pass/fail

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
| No JSON output mode | Add `--json` for CI parsing |
| Secrets in output | Must redact |
| Script exits 0 on pre-flight failure | Must exit non-zero |
| No concurrency protection | flock required |
| No version tracking in output | Include `$VERSION` in logs |
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