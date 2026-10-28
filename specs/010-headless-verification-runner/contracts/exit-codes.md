# Exit-Code Contract (NFR-003)

**Feature**: 010-headless-verification-runner

The runner's exit code is the sole machine-readable signal (spec 008 FR-013). Infrastructure failure classes are inherited unchanged from the spec 008 smoke taxonomy (codes 1–17; code 8 is retired; code 11 is inherited as the game-store/Steam prerequisite class but is **never emitted** by this runner because acquisition uses the private repo). The runner's own verdict/failure classes are allocated starting at 18 per the spec 008 allocation rule ("new categories take the next value above the highest published code"; no published code is ever re-assigned). Reserved ranges 64–78, 100–254, 126, 127, ≥128 are never used.

## Table

| Code | Class | Meaning |
|------|-------|---------|
| 0 | PASS | All selected tests ran and passed (warnings and `flaky: true` allowed) |
| 1–17 | (inherited) | Smoke infrastructure codes, unchanged (spec 008 taxonomy); 8 retired; 11 inherited but **never emitted** by this runner |
| 18 | FAIL | Threshold violation / assertion failure (verdict `fail`), including a matched `expectedVerdict: fail` |
| 19 | ERRORED | Crash, capture-path failure, **missing bundle or contract artifact** (`manifest.json`, `claims.json`, `manifest.sha256`, or a per-attempt record named in `attempts[]`), a `partial` verdict from an incomplete or divergent signal set, save-preservation/working-copy breach, save I/O error after retries, version mismatch, or `expectedVerdict` mismatch (verdict `errored`/`partial`) |
| 20 | NO_TESTS | No scenario discovered, or **every** selected scenario was skipped/quarantined (verdict `skipped`). A mixed suite with at least one non-skipped result does not use 20. |
| 21 | POLICY | Policy / quality-gate violation: docs drift, minimum-expected-tests, coverage threshold, claim/honesty overreach, or another non-gameplay policy check |
| 22 | reserved | Reserved for a future flaky-only / partial-success code. Never emitted; no other class may be assigned to it. |

## Rules

- `flaky` is reported as metadata (`flaky: true` in JUnit/Allure), never a distinct code; a flaky run exits `0` (PASS) and is never counted as a failure. Flakiness alone never fails the run; quarantine is the mechanism that eventually removes a persistently flaky scenario from the blocking gate.
- **Code 11 stays inherited and is never emitted** by this runner. It is not reused for version mismatch, missing dependency, or any spec-010 class.
- **Code 22 is reserved, not assigned.** It exists only to hold the next future class (flaky-only or partial-success); it must not be emitted, listed in `--ignore-exit-code`, or included in suite aggregation until a new contract version assigns it.
- **Missing artifact split:** a missing mandatory **signal** artifact (SoilHealthState delta, Crop-phase dump, SMAPI log excerpt) yields `partial` and exits `19`. A missing **bundle/contract** artifact (`manifest.json`, `claims.json`, `manifest.sha256`, or a named per-attempt record) yields `errored` and exits `19`.
- **Save I/O error** is a classified transient: it is retried (3 attempts total, 5 s cooldown) and, after retries are exhausted, exits `19` (ERRORED). It is never mapped to inherited code `4`.
- **Macro timeout** inherits the smoke TIMEOUT class: it is retryable and exits `4` (inherited) when retries are exhausted. It does not become `19`. This keeps the timeout meaning stable with the spec 008 taxonomy.
- `skipped`: a run in which **all** selected scenarios are skipped/quarantined exits `20` (NO_TESTS) with a JUnit `<skipped>` element and is not a failure. A mixed suite with at least one `pass` exits `0`; the aggregate still follows the precedence below.
- Version mismatch classifies as `ERRORED` (19). Inherited code `2` remains SMAPI-not-found and is not used for version mismatch; inherited code `11` remains the game-store/Steam prerequisite class and is never emitted by this runner or reused for another class.
- `ABORTED`/`USAGE`/`CRASH` are covered by inherited smoke codes (4 timeout, 14 usage error, 13 internal error) and are not redefined.
- **Expected verdicts:** a `negative` scenario MUST declare a non-pass `expectedVerdict` (`fail`, `errored`, `partial`, `skipped`); a `positive` scenario may omit it (default `pass`); a `boundary` scenario MUST declare `pass` or `fail`. A matched expected verdict is preserved and exits with that verdict's normal code (`fail` → `18`; `errored`/`partial` → `19`; `skipped` → `20` when the whole selection is skipped, or `0` in a mixed suite with at least one `pass`). A mismatch — including an unexpected pass — is `errored` and exits `19`, recorded as `expected-verdict-mismatch`. A matched `fail` is never rewritten to `pass`.
- `--ignore-exit-code <list>` accepts a comma-separated list drawn from `{4,6,7,9,10,13,14,18,19,20,21}` and demotes the listed codes to 0 without altering the recorded verdict, evidence, or JUnit/Allure result. `0`, `22`, and unknown values are usage errors (exit `14`). Expected-failure lanes opt into `--ignore-exit-code 18`; the override is recorded as an audit event. **Integrity/security failures and claim overreach are never demotable:** when the underlying failure is an FR-024/FR-025 integrity class or a `--verify-bundle` honesty/claim overreach (`21`), the runner records `ignoreExitCodeRefused: true` (with the requested list) and exits with the original code. It never converts a failed integrity check into `0`.
- `--no-acquire` disables network acquisition: a valid cache proceeds without network or token, a cache miss exits `9` (missing dependency), and combining `--no-acquire` with `--force-refresh` exits `14` (usage error).
- `--verify-bundle <dir>` exits `0` for a valid bundle, `19` for a schema/digest/mandatory-evidence failure (including a missing `manifest.sha256`), and `21` (POLICY) for a claim/honesty-level overreach.
- `--check-docs` exits `0` with no drift, `21` (POLICY) on drift, and `14` when a marker block is missing or malformed.
- Any unknown flag or positional argument exits `14`.
- **Suite aggregation** (`--scenario <directory>`): the aggregate exit code follows the precedence `errored/partial (19) > fail (18) > policy (21) > no-tests (20) > pass (0)`. A mixed suite with at least one `pass` and some `skipped` exits `0`; a suite in which every selected scenario is skipped exits `20`; `flaky: true` never changes the aggregate exit code; code `22` never participates. Per-scenario JUnit/Allure entries are preserved and the aggregate verdict is recorded in `manifest.json`.
- The enum is published as a public, semver-governed contract.
