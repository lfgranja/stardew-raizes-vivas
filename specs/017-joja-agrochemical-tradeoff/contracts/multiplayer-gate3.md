# Contract: Multiplayer Authority & Gate 3 Verification (017 MVP)

## Multiplayer authority (Constitution VI)

017 is **host-authoritative MVP** for shared soil mutation.

### Required behavior
| Scenario | Contract |
|---|---|
| Host, eligible target, no conflict | Acquire per-tile lock (first-wins), apply service, commit explicit measured soil change, consume item on success, emit confirmation, broadcast FR-009 snapshot after success, release lock. |
| Host, contention/conflict | Fail first-wins with explicit refusal; no soil mutation/item consumption/false success for losing attempt. |
| Farmhand / unsupported MVP path | **Refuse before any soil mutation, item consumption, or false-success feedback** (C4:A). Clear localized message. No local authoritative soil change. |
| Missing authority/lock safety | Fail closed for authoritative operations where required; never perform unsafe uncoordinated shared mutation. |

### Boundaries
- `HostAtomicLockService` is in-process; it protects process-local contention and supports controller pattern but is **not** cross-client arbitration. 017 must not claim D1/cross-client protocol completeness.
- FR-009 uses existing `SoilHealthSync` snapshot broadcast/apply pattern. Farmhands apply only matching-save messages and hosts ignore received sync.
- No second source of truth (no external DB/network state beyond Stardew save/SMAPI mod messaging patterns used by project).

## Gate 3 / verification honesty

### Claim levels (must be explicit)
| Level | Allowed when |
|---|---|
| Automated evidence | Unit/controller/property/invariant tests pass for defined behavior. |
| Host-only `machine-pass` | Host-only end-to-end run with required signals/evidence passes; **not** human `verified`, **not** D1. |
| Human `verified` | Separate human attestation completed. |
| D1 farmhand convergence | Separate real-farmhand MP evidence completed. |

### 017 MVP verification targets
- **Automated:** C1 no-double-counting boundary (no 014/015/vanilla-tier mechanical buff hooks added), C2 `NaN`/unmeasured baseline uses `MaxSoilHealth` and absolute `SetSoilHealth` measured write (never negative `UpdateHealth` from unknown), bounded `P=10` provisional clamp, refusal classes, host lock/broadcast behavior, farmhand refusal before mutation/consumption/feedback, no broadcast on refusal.
- **Host-only machine evidence (if feasible):** 3-signal style end-to-end where applicable under project Gate 3/runner rules; record as host-only only.
- **Follow-up:** D1 real-farmhand convergence; playtest calibration of `P=10` and temptation/legibility.

### Prohibitions
- Do not claim `verified` without human attestation.
- Do not claim D1/farmhand convergence without real-farmhand evidence.
- Do not claim vanilla fertilizer-hook behavior or Harmony fertilizer MVP support.
- Do not claim final balance.