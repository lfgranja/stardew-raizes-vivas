# `test.md` GATE3 Block Template

**Feature**: 010-headless-verification-runner
**Contract**: FR-007, FR-008, FR-016, FR-017

Every feature's `specs/<NNN-slug>/test.md` MUST contain exactly one GATE3 marker block. The runner validates the block before launch and replaces only the bytes between the markers on a clean, non-flaky `pass`; everything outside the block is preserved byte-for-byte.

## Required markers

```markdown
<!-- GATE3:BEGIN -->
<!-- GATE3:END -->
```

- Exactly one begin marker and exactly one end marker.
- The begin marker MUST precede the end marker.
- No nested or duplicate blocks.
- The markers themselves are preserved (only the content between them changes).
- A missing, duplicated, or inverted marker pair fails validation with exit `14` before any game launch.

## Block content

The runner writes a short, machine- and human-readable status block. Required fields:

| Field | Meaning |
|-------|---------|
| `status` | `partial` (initial / not verified / flaky pass) or `verified` (all three signals agree on a clean, non-flaky `pass`) |
| `verdict` | Base verdict from the latest run (`pass`, `fail`, `errored`, `partial`, `skipped`) |
| `flaky` | `true` when attempt 1 failed with a retryable class and a later attempt passed; `false` otherwise. A flaky pass keeps `status: partial`. |
| `runId` | Evidence-bundle run ID |
| `commit` | Commit SHA the run executed against |
| `date` | ISO 8601 UTC timestamp of the run |
| `signals` | Paths to the three mandatory signals: SoilHealthState delta, Crop-phase delta, SMAPI log |
| `bundle` | Repo-relative path to the evidence bundle |
| `honestyLevel` | `W`, `R`, or `E` (E is human-assigned only) |
| `claim` | Safe wording copied from `claims.json` |

`status: verified` is written only when the verdict is `pass`, all three signals agree, the honesty level is at least `R`, and `flaky` is `false`. A flaky pass MUST leave `status: partial` and record `flaky: true`; the block is promoted only by a later clean, non-flaky `pass`. This prevents a retry from laundering an unstable scenario into a verified claim.

## Template (initial state)

```markdown
# Feature Test Record: <NNN-slug>

<!-- GATE3:BEGIN -->
- **Status**: partial
- **Verdict**: —
- **Flaky**: false
- **Run ID**: —
- **Commit**: —
- **Date**: —
- **Signals**:
  - SoilHealthState delta: —
  - Crop-phase delta: —
  - SMAPI log: —
- **Bundle**: —
- **Honesty level**: —
- **Claim**: Not yet verified. Gate 3 evidence is incomplete.
<!-- GATE3:END -->
```

## Template (verified)

```markdown
# Feature Test Record: <NNN-slug>

<!-- GATE3:BEGIN -->
- **Status**: verified
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261027T120000Z-a1b2
- **Commit**: 0123456789abcdef0123456789abcdef01234567
- **Date**: 2026-10-27T12:04:31Z
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261027T120000Z-a1b2/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261027T120000Z-a1b2/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261027T120000Z-a1b2/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261027T120000Z-a1b2/`
- **Honesty level**: R
- **Claim**: verified by automated headless run + multi-signal evidence: observation notes [artifacts/observation-notes.md], artifact comparison [artifacts/soil-health-before-after.json], SMAPI log [artifacts/smapi-log.txt], session date 2026-10-27.
<!-- GATE3:END -->
```

## Promotion set

Only the following transitions are legal. Anything not listed is rejected (exit `14` for a malformed block, `21` for a claim that overstates evidence).

| From | To | Condition |
|------|----|-----------|
| `partial` | `verified` | Clean, non-flaky `pass`; three signals agree; honesty level ≥ R; expectedVerdict matches |
| `partial` (flaky pass) | `partial` (flaky: true) | Retry passed but the run is flaky; no promotion |
| `verified` | `partial` | A later run is `fail`, `errored`, `partial`, `skipped`, or a flaky `pass` |
| `verified` | `verified` | A later clean, non-flaky `pass` refreshes `runId`/`date`/evidence pointers |
| any | `partial` | Manual demotion after evidence review; never silently rewritten to `verified` |

A `fail`, `errored`, `partial`, or `skipped` run never promotes. `E` is never a promotion target for the runner; it is a separate human attestation layered on top of `R`.

## Claim discipline

- Never write "verified by build" or "verified by N passing tests".
- `verified` means the multi-signal evidence is present and agrees; it does not mean the feature is bug-free.
- `partial`, `errored`, `fail`, and `skipped` MUST NOT be rewritten to `verified` by hand. A new full triad run is required.
- `E` is never auto-assigned; a maintainer may raise the level after reviewing W + R evidence.

## Validation checklist (runner, fail-fast)

1. `testMdPath` exists, is a regular file, is repo-relative, and ends with `test.md`.
2. Exactly one `<!-- GATE3:BEGIN -->` and one `<!-- GATE3:END -->` exist.
3. Begin precedes end; no other GATE3 markers exist in the file.
4. The block is writable; a failed write leaves the original file untouched (atomic temp + rename).
5. Root docs (`ROADMAP.md`, `COMPLETION_SUMMARY.md`) contain the same marker pair before the runner updates them (FR-017).
