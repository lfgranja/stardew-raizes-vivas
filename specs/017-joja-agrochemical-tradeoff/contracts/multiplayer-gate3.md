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
| Infeasible/unavailable (planned) | A planned verification run cannot be executed under current constraints; record infeasibility + reason + what remains claimable at lower evidence levels. This is **not** a pass/fail evidence level. |
| Qualitative follow-up evidence | Structured playtest/legibility evidence collected outside the automated/runner path (e.g., SC-004 protocol when run); useful for calibration but does not substitute for required automated/Gate 3 claim levels unless explicitly stated. |

### Claim record convention (auditability)
Each MVP verification claim MUST be auditable by a lightweight record (in spec/plan/tasks/verification notes as appropriate) containing:
- **claim** (what is being claimed),
- **evidence class** (from the table above),
- **source artifact/path** (test result, runner bundle, scenario/log, playtest note, or infeasibility record),
- **status** (`pending` | `complete` | `infeasible`/`blocked` for planned runs, as applicable),
- **scope note** when needed (host-only, no D1, provisional tuning, etc.).

Claims MUST NOT be promoted above their evidence class.

### Provenance / historical evidence
017 work MUST preserve project verification provenance: do not rewrite, overwrite, or retroactively upgrade prior verification artifacts/evidence. New runs produce new evidence records; prior `machine-pass`/`verified`/D1 artifacts remain historically attributable. This applies to 017 evidence and to any referenced prior feature evidence.

### 017 MVP verification targets
- **Automated:** C1 no-double-counting boundary (no 014/015/vanilla-tier mechanical buff hooks added), C2 `NaN`/unmeasured baseline uses `MaxSoilHealth` and absolute `SetSoilHealth` measured write (never negative `UpdateHealth` from unknown), bounded `P=10` provisional clamp, refusal classes, host lock/broadcast behavior, farmhand refusal before mutation/consumption/feedback, no broadcast on refusal, item consumption only after authoritative measured commit.
- **Host-only machine evidence (if feasible):** 3-signal style end-to-end where applicable under project Gate 3/runner rules; record as host-only only. **If infeasible/unavailable:** do not fabricate a run; claim only completed lower-level evidence (e.g., automated tests) plus an infeasibility record; human `verified` and D1 remain pending.
- **Follow-up:** D1 real-farmhand convergence; playtest calibration of `P=10` and temptation/legibility. SC-004 comprehension may be evaluated by a defined minimal protocol when run, or recorded as qualitative follow-up evidence when not run (see spec).

### Prohibitions
- Do not claim `verified` without human attestation.
- Do not claim D1/farmhand convergence without real-farmhand evidence.
- Do not claim vanilla fertilizer-hook behavior or Harmony fertilizer MVP support.
- Do not claim final balance.
- Do not rewrite/retroactively promote historical verification evidence.