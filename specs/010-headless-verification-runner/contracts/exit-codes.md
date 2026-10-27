# Exit-Code Contract (NFR-003)

**Feature**: 010-headless-verification-runner

The runner's exit code is the sole machine-readable signal (spec 008 FR-013). Infrastructure failure classes are inherited unchanged from the spec 008 smoke taxonomy (codes 1–17, 8 retired). The runner's own verdict/failure classes are allocated starting at 18 per the spec 008 allocation rule ("new categories take the next value above the highest published code"; no published code is ever re-assigned). Reserved ranges 64–78, 100–254, 126, 127, ≥128 are never used.

## Table

| Code | Class | Meaning |
|------|-------|---------|
| 0 | PASS | All selected tests ran and passed (warnings allowed) |
| 1–17 | (inherited) | Smoke infrastructure codes, unchanged (spec 008 taxonomy) |
| 18 | FAIL | Threshold violation / assertion failure (verdict `fail`) |
| 19 | ERRORED | Crash or incomplete signal set (verdict `errored`/`partial`), incl. version mismatch |
| 20 | NO_TESTS | No scenario discovered / all skipped (quarantined) (verdict `skipped`) |
| 21 | THRESHOLD | Policy / quality-gate violation |
| 22 | reserved | Future use (flaky-only or partial-success); unused |

## Rules

- `flaky` is reported as metadata (`flaky: true` in JUnit/Allure), never a distinct code; a flaky run exits `18` (FAIL) but is flagged.
- `skipped` (quarantined) exits `20` (NO_TESTS) with a JUnit `<skipped>` element and is not a failure.
- `ABORTED`/`USAGE`/`CRASH` are covered by inherited smoke codes (4 timeout, 14 usage error, 13 internal error) and are not redefined.
- `--ignore-exit-code <list>` demotes the listed codes to 0 without altering runner behavior.
- The enum is published as a public, semver-governed contract.