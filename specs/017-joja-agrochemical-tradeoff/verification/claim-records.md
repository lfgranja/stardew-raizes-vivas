# Spec 017 — Claim Records (Joja / Agrochemical MVP)

**Purpose:** Auditable register of 017 MVP verification claims. Schema mirrors the canonical
source in [`../contracts/multiplayer-gate3.md`](../contracts/multiplayer-gate3.md) (FR-015,
SC-005).

**Current state:** implementation is in place and the Phase 7/8 artifacts are recorded. The
`automated` and mocked-persistence claims are `complete` for their stated scope. The host-only
Gate 3 run has been performed against the 016-hardened runner and is recorded as
`host-only-machine-pass` (`machine-pass`, honesty `R`). D1, human `verified`, and SC-004 remain
`pending` because no real farmhand, human-attestation, or playtest run exists. The RED gate
record at `verification/red-phase.md` is historical gate evidence (a failing run), not passing
behavior evidence.

## Status vocabulary

| Status | Meaning |
|---|---|
| `pending` | Not yet evidenced, or evidence not yet recorded. Default state. |
| `complete` | A source artifact exists and actually supports the claim, at its stated evidence class. |
| `infeasible` | The planned check cannot be performed in the current environment; reason recorded. Not a pass. |
| `blocked` | A prerequisite is missing; reason recorded. Not a pass. |

## Verification artifacts

These are the artifacts this register cites (or will cite once produced). A missing artifact means
the related row stays `pending` (or `infeasible`/`blocked` with a reason).

| Artifact | Purpose | Produced by |
|---|---|---|
| `verification/red-phase.md` + `verification/red-phase-run.log` | Historical RED gate: 60 targeted tests failed for expected missing-behavior reasons before implementation. Not passing evidence. | T025 |
| `verification/automated-tests.md` | Targeted 017 suite results and full-regression results with exact counts. | T048, T050 |
| `verification/persistence-roundtrip.md` | Mocked `ISoilHealthService.SaveData`/`LoadData` seam evidence for the C2 measured result and sparse-zero scope. | T049 |
| `verification/static-checks.md` | Formatting/build gate result. | T051 |
| `verification/manual-review.md` | Structured (AI-assisted) FR-002/FR-012 semantic tone/meaning/legibility review for touched keys. | T052 |
| `verification/provisioning-validation.md` | Validation-only item provisioning decision (test fixture + scoped manual/Gate 3 decision); no `Data/Shops`. | T046 |
| `verification/host-only-gate3.md` | Historical 2026-11-06 infeasibility/planned-run-not-executed record; superseded for the current run status but retained as provenance. | T047 |
| `verification/host-only-gate3-run.md` | Real host-only Gate 3 run record (clean host pass, 3/3 thresholds, signals agree); links to the evidence bundle. | T062 |
| `evidence-bundle-run-20261107T015936Z-7f35/` | Tracked evidence bundle for the host-only machine-pass; contains full SMAPI log, three signals, and offline-verifier exit 0. | T062 |
| `verification/sc004-comprehension-protocol.md` | SC-004 protocol; execution evidence only if a real run is appended. | T045 |
| `verification/honesty-review.md` | Gate 3 honesty/provenance review of the claim set and 017 source boundaries. | T054 |
| `verification/summary.md` | Evidence-derived final implementation/verification summary. | T055 |

## Claim register

| Claim | Evidence class | Source artifact/path | Status | Scope note |
|---|---|---|---|---|
| Automated service/controller contract and invariant tests pass for 017 MVP behavior (FR-004–FR-010, SC-001–SC-003; C1 boundary only). | `automated` (unit/property) | `LivingRoots.Tests/Services/AgrochemicalApplicationServiceTests.cs`; `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs`; `verification/automated-tests.md`; `verification/automated-tests-targeted.log` | `complete` | 60/60 targeted cases passed on the final tree. Mocked dependencies; no real game process. The C1 test is a service-dependency boundary check; it is not a full harvest/growth composition proof. Does not cover real-game behavior, human attestation, D1, or SC-004. Fallback-only tests cannot promote FR-007/FR-013/SC-002. |
| Automated content key/parity and tone-proxy tests pass for touched 017 keys (FR-002, FR-012). | `automated` (unit) | `LivingRoots.Tests/AgrochemicalContentParityTests.cs`; `verification/automated-tests.md`; `verification/automated-tests-targeted.log` | `complete` | 25/25 targeted cases passed. Lexical proxy only; cannot prove semantic meaning, nuance, or tone. The T052 copy review is separate and is not human attestation. |
| Full solution automated regression suite passes with 017 included. | `automated` (full suite) | `verification/automated-tests.md`; `verification/automated-tests-full.log` | `complete` | 1816 total: 1812 passed, 0 failed, 4 skipped. The 4 skips are pre-existing spec 009 deferred harvest-hook tests. A Phase 8 rerun on the pos-wiring tree passed with the same counts. Does not prove real-game behavior or final balance. |
| Persistence round-trip through the existing `ISoilHealthService.SaveData`/`LoadData` seam with a mocked `IModDataService` capture preserves the measured C2 post-penalty value. | `automated` (integration-style, mocked seam) | `LivingRoots.Tests/Services/AgrochemicalPersistenceTests.cs`; `verification/persistence-roundtrip.md`; `verification/persistence-roundtrip.log` | `complete` | 3/3 cases passed. C2 unknown-baseline applies and reloads as measured `90` for `P=10`; measured `50` reloads as `40`. A clamped-to-zero result is omitted by the existing sparse cache and reloads as unknown. Proves the seam only, not real disk persistence or Gate 3. |
| Structured semantic copy review of touched 017 keys is legible, meaning-equivalent across `default`/`en`/`pt`, and cozy/non-punitive. | `qualitative-follow-up` (AI-assisted copy review) | `verification/manual-review.md` | `complete` | Review performed by the implementation agent, not an independent human. Supports a narrow copy-review claim only; does not satisfy `human-verified`, does not prove in-game rendering, and is not SC-004. |
| Host-only real-game Gate 3 machine-pass (3 required signals) for the 017 MVP path. | `host-only-machine-pass` | `verification/host-only-gate3-run.md`; `evidence-bundle-run-20261107T015936Z-7f35/`; `test.md` (`machine-pass`, honesty `R`) | `complete` | A clean, non-flaky **host** run passed the 016-hardened runner: measured 50 → 40 after one provisional `P=10`, `phaseDays` unchanged, host `isMainPlayer` true; three signals present and agreeing; 3/3 thresholds pass; offline verifier exit 0. Supersedes the 2026-11-06 `planned-run-not-executed`/`infeasible` record (kept as historical context in `verification/host-only-gate3.md`). **Not** D1, **not** human `verified`, **not** SC-004, and not proof of final balance or the C2 unknown-baseline path. |
| D1 farmhand convergence on a shared farm. | `d1-farmhand-convergence` (multiplayer runtime) | To be recorded from a real farmhand session | `pending` | Requires real farmhand evidence. Controller mocks, host-only machine runs, and automated tests do not satisfy D1. |
| Human `verified` attestation for the 017 MVP. | `human-verified` (human review/playtest) | To be recorded from the human attestation | `pending` | Requires an explicit recorded human attestation. Automated, AI-assisted copy review, and host-only machine evidence do not satisfy it. |
| SC-004 qualitative comprehension follow-up: participants identify both the short-term shortcut and the soil-health cost at first supported use (target ≥90%). | `qualitative-follow-up` (human protocol run) | `verification/sc004-comprehension-protocol.md` (protocol only; no run recorded) | `pending` | Protocol finalized but no participant run recorded. Protocol text alone is not evidence; SC-004 must not be claimed complete until a run and its evidence note exist. |

## Rules

- A claim is only `complete` when a concrete source artifact exists and supports that claim at
  its stated evidence class. Otherwise it stays `pending`, `infeasible`, or `blocked`.
- Host-only machine-pass is **not** human `verified` and **not** D1. Each class is separate and
  is never promoted into another.
- Fallback-only tests (tests that exercise only the unavailable-feedback/fallback branch) cannot
  promote FR-007, FR-013, or SC-002 to satisfied. They may support a narrower claim about
  non-misleading fallback behavior only.
- No retroactive promotion: do not rewrite, overwrite, or upgrade prior evidence or prior claim
  records. New runs produce new records.
- Non-executed checks stay `pending`; if a planned run cannot happen, record `infeasible` or
  `blocked` with a reason. A skipped or infeasible check is never a pass.
- Implementation status does not imply verification status. A merged change or a green test run
  does not mark a row complete unless the artifact is linked in this register.

## Update procedure

1. Add the new artifact path and the recorded run date to the relevant row.
2. Change status only after the artifact exists; cite the artifact in `Source artifact/path`.
3. Keep scope notes honest about what the artifact does **not** prove.
4. Append new rows for new claims instead of broadening an existing claim's scope.
