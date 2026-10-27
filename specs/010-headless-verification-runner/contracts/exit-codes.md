# Exit-Code Contract (NFR-003)

**Feature**: 010-headless-verification-runner

The runner's exit code is the sole machine-readable signal (spec 008 FR-013). Infrastructure failure classes are inherited unchanged from the spec 008 smoke taxonomy (codes 1–17; code 8 is retired; code 11 is inherited as the game-store/Steam prerequisite class but is never emitted by this runner because acquisition uses the private repo). The runner's own verdict/failure classes are allocated starting at 18 per the spec 008 allocation rule ("new categories take the next value above the highest published code"; no published code is ever re-assigned). Reserved ranges 64–78, 100–254, 126, 127, ≥128 are never used.

## Table

| Code | Class | Meaning |
|------|-------|---------|
| 0 | PASS | All selected tests ran and passed (warnings and `flaky: true` allowed) |
| 1–17 | (inherited) | Smoke infrastructure codes, unchanged (spec 008 taxonomy); 8 retired; 11 inherited but never emitted by this runner |
| 18 | FAIL | Threshold violation / assertion failure (verdict `fail`), including a matched `expectedVerdict: fail` |
| 19 | ERRORED | Crash, incomplete or divergent signal set, save-preservation breach, save I/O error or macro timeout after retries are exhausted, version mismatch, or `expectedVerdict` mismatch (verdict `errored`/`partial`) |
| 20 | NO_TESTS | No scenario discovered / all skipped (quarantined) (verdict `skipped`) |
| 21 | THRESHOLD | Policy / quality-gate violation: docs drift, minimum-expected-tests, coverage threshold, or claim/honesty overreach |
| 22 | reserved | Future use (flaky-only or partial-success); unused |

## Rules

- `flaky` is reported as metadata (`flaky: true` in JUnit/Allure), never a distinct code; a flaky run exits `0` (PASS) and is never counted as a failure. Flakiness alone never fails the run; quarantine is the mechanism that eventually removes a persistently flaky scenario from the blocking gate.
- `skipped` (quarantined) exits `20` (NO_TESTS) with a JUnit `<skipped>` element and is not a failure.
- Version mismatch classifies as `ERRORED` (19). Inherited code `2` remains SMAPI-not-found and is not used for version mismatch; inherited code `11` remains the game-store/Steam prerequisite class and is never emitted by this runner or reused for another class.
- `ABORTED`/`USAGE`/`CRASH` are covered by inherited smoke codes (4 timeout, 14 usage error, 13 internal error) and are not redefined.
- **Expected verdicts:** a `negative` scenario may declare any non-pass `expectedVerdict` (`fail`, `errored`, `partial`, `skipped`); `positive` and `boundary` scenarios are restricted to `pass`/`fail`. A matched expected verdict is preserved and exits with that verdict's normal code (`fail` → `18`; `errored`/`partial` → `19`; `skipped` → `20`, or `0` in a mixed suite with at least one `pass`). A mismatch — including an unexpected pass — is `errored` and exits `19`, recorded as `expected-verdict-mismatch`. A matched `fail` is never rewritten to `pass`.
- `--ignore-exit-code <list>` accepts a comma-separated list drawn from `{4,6,7,9,10,13,14,18,19,20,21}` and demotes the listed codes to 0 without altering the recorded verdict, evidence, or JUnit/Allure result. `0` and unknown values are usage errors (exit `14`). Expected-failure lanes opt into `--ignore-exit-code 18`; the override is recorded as an audit event. **Integrity/security failures and claim overreach are never demotable:** when the underlying failure is an FR-024/FR-025 integrity class or a `--verify-bundle` honesty overreach (`21`), the runner records `ignoreExitCodeRefused: true` and exits with the original code.
- `--no-acquire` disables network acquisition: a valid cache proceeds without network or token, a cache miss exits `9` (missing dependency), and combining `--no-acquire` with `--force-refresh` exits `14` (usage error).
- `--verify-bundle <dir>` exits `0` for a valid bundle, `19` for a schema/digest/mandatory-evidence failure, and `21` for a claim/honesty-level overreach.
- `--check-docs` exits `0` with no drift, `21` on drift, and `14` when a marker block is missing or malformed.
- Any unknown flag or positional argument exits `14`.
- **Suite aggregation** (`--scenario <directory>`): the aggregate exit code follows the precedence `errored/partial (19) > fail (18) > policy/threshold (21) > no-tests (20) > pass (0)`. A mixed suite with at least one `pass` and some `skipped` exits `0`; `flaky: true` never changes the aggregate exit code. Per-scenario JUnit/Allure entries are preserved and the aggregate verdict is recorded in `manifest.json`.
- The enum is published as a public, semver-governed contract.
