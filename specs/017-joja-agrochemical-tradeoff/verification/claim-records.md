# Spec 017 — Claim Records (Joja / Agrochemical MVP)

**Purpose:** Auditable register of 017 MVP verification claims. Schema mirrors the canonical
source in [`../contracts/multiplayer-gate3.md`](../contracts/multiplayer-gate3.md) (FR-015,
SC-005).

**Current state:** implementation is in place and the targeted 017 test suite is green, but this
register only records claims supported by **linked artifacts**. The Phase 7 verification tasks
(T048–T052) produce those artifacts; T053 links them and changes statuses. Until then every row
below stays `pending`. The RED gate record at `verification/red-phase.md` is historical gate
evidence (a failing run), not passing behavior evidence.

## Status vocabulary

| Status | Meaning |
|---|---|
| `pending` | Not yet evidenced, or evidence not yet recorded. Default state. |
| `complete` | A source artifact exists and actually supports the claim, at its stated evidence class. |
| `infeasible` | The planned check cannot be performed in the current environment; reason recorded. Not a pass. |
| `blocked` | A prerequisite is missing; reason recorded. Not a pass. |

## Verification artifacts

These are the artifacts this register may cite once they exist. A missing artifact means the
related row stays `pending` (or `infeasible`/`blocked` with a reason).

| Artifact | Purpose | Produced by |
|---|---|---|
| `verification/red-phase.md` + `verification/red-phase-run.log` | Historical RED gate: 60 targeted tests failed for expected missing-behavior reasons before implementation. Not passing evidence. | T025 |
| `verification/automated-tests.md` | Targeted 017 suite results and full-regression results with exact counts. | T048, T050 |
| `verification/persistence-roundtrip.md` | Mocked `ISoilHealthService.SaveData`/`LoadData` seam evidence for the C2 measured result and sparse-zero scope. | T049 |
| `verification/static-checks.md` | Formatting/build gate result. | T051 |
| `verification/manual-review.md` | Manual FR-002/FR-012 semantic tone/meaning/legibility checklist for touched keys. | T052 |
| `verification/provisioning-validation.md` | Validation-only item provisioning decision (test fixture + scoped manual/Gate 3 decision); no `Data/Shops`. | T046 |
| `verification/host-only-gate3.md` | Host-only Gate 3 feasibility or `planned-run-not-executed` record. | T047 |
| `verification/sc004-comprehension-protocol.md` | SC-004 protocol; execution evidence only if a real run is appended. | T045 |

## Claim register

| Claim | Evidence class | Source artifact/path | Status | Scope note |
|---|---|---|---|---|
| Automated service/controller contract and invariant tests pass for 017 MVP behavior (FR-004–FR-010, SC-001–SC-003, SC-006). | `automated` (unit/property) | `LivingRoots.Tests/Services/AgrochemicalApplicationServiceTests.cs`; `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs`; run record in `verification/automated-tests.md` | `pending` | Mocked dependencies; no real game process. Does not by itself cover real-game behavior, human attestation, D1, or SC-004. Fallback-only tests cannot promote FR-007/FR-013/SC-002. |
| Automated content key/parity and tone-proxy tests pass for touched 017 keys (FR-002, FR-012). | `automated` (unit) | `LivingRoots.Tests/AgrochemicalContentParityTests.cs`; run record in `verification/automated-tests.md` | `pending` | Lexical proxy only. Cannot prove semantic meaning, nuance, or tone; T052 manual review remains separate. |
| Persistence round-trip through the existing `ISoilHealthService.SaveData`/`LoadData` seam with a mocked `IModDataService` capture preserves the measured C2 post-penalty value. | `automated` (integration-style, mocked seam) | `LivingRoots.Tests/Services/AgrochemicalPersistenceTests.cs`; run record in `verification/persistence-roundtrip.md` | `pending` | Proves the save/load seam only, not real disk persistence and not Gate 3. Must not be described as a real save/load or end-to-end result. |
| Manual FR-002/FR-012 semantic review of touched 017 keys is legible, meaning-equivalent across `default`/`en`/`pt`, and cozy/non-punitive. | `qualitative-follow-up` (manual copy review) | `verification/manual-review.md` | `pending` | Copy review only. Not human `verified` attestation of the feature, not automated coverage, and not SC-004. |
| Host-only real-game Gate 3 machine-pass (3 required signals) for the 017 MVP path. | `host-only-machine-pass` (host-only machine evidence) | `verification/host-only-gate3.md` (feasibility/infeasibility record; no bundle exists) | `pending` | Host-only machine evidence only. Not human `verified` and not D1. If the run is infeasible, record `planned-run-not-executed` with status `infeasible` and a reason; never fabricate logs, bundles, or a pass. |
| D1 farmhand convergence on a shared farm. | `d1-farmhand-convergence` (multiplayer runtime) | To be recorded from a real farmhand session | `pending` | Requires real farmhand evidence. Controller mocks, host-only machine runs, and automated tests do not satisfy D1. |
| Human `verified` attestation for the 017 MVP. | `human-verified` (human review/playtest) | To be recorded from the human attestation | `pending` | Requires an explicit recorded human attestation. Automated and host-only machine evidence do not satisfy it. |
| SC-004 qualitative comprehension follow-up: participants identify both the short-term shortcut and the soil-health cost at first supported use (target ≥90%). | `qualitative-follow-up` (human protocol run) | `verification/sc004-comprehension-protocol.md` (protocol only; no run recorded) | `pending` | Protocol exists, no run recorded. Protocol text alone is not evidence; SC-004 must not be claimed complete until a run and its evidence note exist. |

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
