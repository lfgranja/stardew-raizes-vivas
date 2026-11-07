# Spec 017 — Claim Records (Joja / Agrochemical MVP)

**Purpose:** Auditable register of 017 MVP verification claims. Schema mirrors the canonical
source in [`../contracts/multiplayer-gate3.md`](../contracts/multiplayer-gate3.md) (FR-015,
SC-005).

**Current state:** implementation has **not** started and no verification run has been recorded.
Every row below is `pending`. No claim is complete.

## Status vocabulary

| Status | Meaning |
|---|---|
| `pending` | Not yet evidenced, or evidence not yet recorded. Default state. |
| `complete` | A source artifact exists and actually supports the claim, at its stated evidence class. |
| `infeasible` | The planned check cannot be performed in the current environment; reason recorded. Not a pass. |
| `blocked` | A prerequisite is missing; reason recorded. Not a pass. |

## Claim register

| Claim | Evidence class | Source artifact/path | Status | Scope note |
|---|---|---|---|---|
| Automated service/controller contract tests pass for 017 MVP behavior and invariants (FR-004–FR-010, SC-001–SC-003, SC-006). | `automated` (unit/property) | `LivingRoots.Tests/Services/AgrochemicalApplicationServiceTests.cs`; `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs`; `LivingRoots.Tests/AgrochemicalContentParityTests.cs`; run record in `verification/red-phase.md` (or a later verification record) | `pending` | Mocked dependencies; no real game process. Does not by itself cover real-game behavior, human attestation, D1, or SC-004. Fallback-only tests cannot promote FR-007/FR-013/SC-002. |
| Persistence round-trip through the existing `ISoilHealthService.SaveData`/`LoadData` seam with a mocked `IModDataService` capture preserves the measured C2 post-penalty value. | `automated` (integration-style, mocked seam) | `LivingRoots.Tests/Services/AgrochemicalPersistenceTests.cs` | `pending` | Proves the save/load seam only, not real disk persistence and not Gate 3. Must not be described as a real save/load or end-to-end result. |
| Host-only real-game Gate 3 machine-pass (3 required signals) for the 017 MVP path. | `host-only-machine-pass` (host-only machine evidence) | Runner/evidence-bundle path to be recorded when run (for example under `specs/017-joja-agrochemical-tradeoff/verification/`) | `pending` | Host-only machine evidence only. Not human `verified` and not D1. If the run is infeasible, record `planned-run-not-executed` with status `infeasible` and a reason; never fabricate logs, bundles, or a pass. |
| D1 farmhand convergence on a shared farm. | `d1-farmhand-convergence` (multiplayer runtime) | To be recorded from a real farmhand session | `pending` | Requires real farmhand evidence. Controller mocks, host-only machine runs, and automated tests do not satisfy D1. |
| Human `verified` attestation for the 017 MVP. | `human-verified` (human review/playtest) | To be recorded from the human attestation | `pending` | Requires an explicit recorded human attestation. Automated and host-only machine evidence do not satisfy it. |
| SC-004 qualitative comprehension follow-up: participants identify both the short-term shortcut and the soil-health cost at first supported use (target ≥90%). | `qualitative-follow-up` (human protocol run) | `specs/017-joja-agrochemical-tradeoff/verification/sc004-comprehension-protocol.md` (protocol only; no run recorded) | `pending` | Protocol exists, no run recorded. Protocol text alone is not evidence; SC-004 must not be claimed complete until a run and its evidence note exist. |

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
