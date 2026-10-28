# `test.md` GATE3 Block Template

**Feature**: 010-headless-verification-runner
**Contract**: FR-007, FR-008, FR-016, FR-017

Every feature's `specs/<NNN-slug>/test.md` MUST contain exactly one GATE3 marker block. The runner validates the block before launch and replaces only the bytes between the markers; everything outside the block is preserved byte-for-byte.

## Status vocabulary (`testMdStatus`)

The block's `status` field is a **closed three-value enum**. It is not the run verdict, not `gate3Status`, and not `scenarioStatus`.

| Status | Writer | Meaning |
|--------|--------|---------|
| `partial` | Runner (initial state and every non-promoting run) | Not verified. Also the permanent state for a flaky pass, a `fail`/`errored`/`partial`/`skipped` run, and **every farmhand/split-screen run**. |
| `machine-pass` | Runner only | Clean, non-flaky **host** `pass`; all three mandatory signals present and agreeing; honesty level ≥ R; `expectedVerdict` matches. This is machine evidence, not human verification. |
| `verified` | Human maintainer only; never the runner | Human `E` attestation layered on an existing `machine-pass`: the block records `reviewer`, `reviewDate`, and `sessionId`. |

`machine-pass` is **not** `verified`. A run verdict `pass` is **not** `machine-pass`. The runner writes only `partial` or `machine-pass`; it must refuse to write `verified`, and no automation may promote a block to `verified`.

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

Required fields (all present in every block; use `—` where a field does not yet apply):

| Field | Values / meaning |
|-------|------------------|
| `status` | `partial`, `machine-pass`, or `verified` (closed enum above). |
| `verdict` | Base run verdict from the latest run: `pass`, `fail`, `errored`, `partial`, or `skipped`; `—` before the first run. |
| `flaky` | `true` when attempt 1 failed with a retryable class and a later attempt passed; `false` otherwise. A flaky pass keeps `status: partial`. |
| `runId` | Evidence-bundle run ID, or `—`. |
| `commit` | Commit SHA the run executed against, or `—`. |
| `date` | ISO 8601 UTC timestamp of the run, or `—`. |
| `playerRole` | `host` or `farmhand` (the scenario schema's enum). A `farmhand` run may be produced by `debug split`; it is never eligible for `machine-pass` and stays `partial`. |
| `signals` | Paths to the three mandatory signals: SoilHealthState delta, Crop-phase delta, SMAPI log. |
| `bundle` | Repo-relative path to the evidence bundle, or `—`. |
| `honestyLevel` | `W`, `R`, or `E`. `E` is human-assigned only and requires `status: verified`. |
| `claim` | Safe wording copied from `claims.json` (or the initial refusal wording). |
| `reviewer` | Maintainer handle for the human `E` attestation. Required (non-`—`) iff `status: verified`. |
| `reviewDate` | ISO 8601 UTC date of the human review. Required iff `status: verified`. |
| `sessionId` | Human verification session identifier (date + save/seed context). Required iff `status: verified`. |

Promotion conditions are cumulative: `machine-pass` requires a **host** run, `verdict: pass`, `flaky: false`, three present and agreeing signals, `honestyLevel` at least `R`, and a matching `expectedVerdict`. `verified` requires an existing `machine-pass` plus the human `E` attestation fields. A farmhand or split-screen run can never satisfy the `machine-pass` condition, even on a clean pass, because its Crop-phase channel asserts absence of mutation rather than a phase delta.

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
- **Player role**: —
- **Signals**:
  - SoilHealthState delta: —
  - Crop-phase delta: —
  - SMAPI log: —
- **Bundle**: —
- **Honesty level**: —
- **Claim**: Not yet verified. Gate 3 evidence is incomplete.
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->
```

## Template (machine-pass)

```markdown
# Feature Test Record: <NNN-slug>

<!-- GATE3:BEGIN -->
- **Status**: machine-pass
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261027T120000Z-a1b2
- **Commit**: 0123456789abcdef0123456789abcdef01234567
- **Date**: 2026-10-27T12:04:31Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261027T120000Z-a1b2/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261027T120000Z-a1b2/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261027T120000Z-a1b2/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261027T120000Z-a1b2/`
- **Honesty level**: R
- **Claim**: machine-pass by automated headless run + multi-signal evidence: observation notes [artifacts/observation-notes.md], artifact comparison [artifacts/soil-health-before-after.json], SMAPI log [artifacts/smapi-log.txt], session date 2026-10-27.
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->
```

## Template (verified — human `E` attestation only)

```markdown
# Feature Test Record: <NNN-slug>

<!-- GATE3:BEGIN -->
- **Status**: verified
- **Verdict**: pass
- **Flaky**: false
- **Run ID**: run-20261027T120000Z-a1b2
- **Commit**: 0123456789abcdef0123456789abcdef01234567
- **Date**: 2026-10-27T12:04:31Z
- **Player role**: host
- **Signals**:
  - SoilHealthState delta: `evidence-bundle-run-20261027T120000Z-a1b2/artifacts/soil-health-before-after.json`
  - Crop-phase delta: `evidence-bundle-run-20261027T120000Z-a1b2/artifacts/crop-phase-dump.json`
  - SMAPI log: `evidence-bundle-run-20261027T120000Z-a1b2/artifacts/smapi-log.txt`
- **Bundle**: `evidence-bundle-run-20261027T120000Z-a1b2/`
- **Honesty level**: E
- **Claim**: verified by manual end-to-end session + multi-signal evidence: observation notes [artifacts/observation-notes.md], artifact comparison [artifacts/soil-health-before-after.json], SMAPI log [artifacts/smapi-log.txt], session date 2026-10-27.
- **Reviewer**: <maintainer-handle>
- **Review date**: 2026-10-28
- **Session ID**: 2026-10-28T09-30-00Z-farm-year1-day5
<!-- GATE3:END -->
```

## Promotion set

Only the following transitions are legal. Anything not listed is rejected (exit `14` for a malformed block, `21` for a claim that overstates evidence). **The runner writes only `partial` and `machine-pass`.**

| From | To | Condition |
|------|----|-----------|
| `partial` | `machine-pass` | Clean, non-flaky **host** `pass`; three signals present and agreeing; honesty level ≥ R; `expectedVerdict` matches |
| `partial` (flaky pass) | `partial` (flaky: true) | Retry passed but the run is flaky; no promotion |
| `machine-pass` | `verified` | Human `E` attestation with `reviewer`, `reviewDate`, and `sessionId` recorded; an existing `machine-pass` block is the prerequisite; never written by the runner |
| `machine-pass` | `partial` | A later run is `fail`, `errored`, `partial`, `skipped`, or a flaky `pass` |
| `verified` | `machine-pass` | A later clean, non-flaky `pass` replaces the evidence; the prior human `E` attestation is not carried forward |
| `verified` | `partial` | A later run is `fail`, `errored`, `partial`, `skipped`, or a flaky `pass` |
| any | `partial` | Manual demotion after evidence review; never silently rewritten to `machine-pass` or `verified` |
| `partial` (farmhand / split-screen) | `partial` | Always stays `partial`; clean passes are not promotable |

A `fail`, `errored`, `partial`, or `skipped` run never promotes. The runner never writes `verified`; only a human may do so, and only on top of `machine-pass`.

## Claim discipline

- Never write "verified by build" or "verified by N passing tests".
- `machine-pass` means the machine evidence is present and agrees; it does not mean the feature is bug-free.
- `verified` means a maintainer applied the `E` judgment to that machine evidence; it does not mean the feature is bug-free.
- `partial`, `errored`, `fail`, and `skipped` MUST NOT be rewritten to `machine-pass` or `verified` by hand. A new full triad run is required.
- `E` is never auto-assigned; a maintainer may raise the level after reviewing W + R evidence, and must record `reviewer`, `reviewDate`, and `sessionId`.
- A flaky pass, a farmhand run, or a split-screen run MUST NOT be presented as `machine-pass` or `verified`.

## Validation checklist (runner, fail-fast)

1. `testMdPath` exists, is a regular file, is repo-relative, and ends with `test.md`.
2. Exactly one `<!-- GATE3:BEGIN -->` and one `<!-- GATE3:END -->` exist.
3. Begin precedes end; no other GATE3 markers exist in the file.
4. `status` is one of `partial`, `machine-pass`, `verified`; an unknown value is a malformed block (exit `14`).
5. The runner refuses to write `verified`; it writes only `partial` or `machine-pass`.
6. `machine-pass` requires `verdict: pass`, `flaky: false`, `playerRole: host`, three non-`—` signal paths, and `honestyLevel` ≥ R.
7. `verified` requires `playerRole: host`, `honestyLevel: E`, and non-`—` `reviewer`, `reviewDate`, and `sessionId`.
8. The block is writable; a failed write leaves the original file untouched (atomic temp + rename).
9. Root docs (`ROADMAP.md`, `COMPLETION_SUMMARY.md`) contain the same marker pair before the runner updates them (FR-017).
