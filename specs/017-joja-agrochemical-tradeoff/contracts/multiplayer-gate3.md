# Contract: Multiplayer Authority & Gate 3 Verification (017 MVP)

## Multiplayer authority (Constitution VI)

017 is **host-authoritative MVP** for shared soil mutation. Ordering/authority rules here and in `agrochemical-application-service.md` are coordinated behavioral contracts (duplication intentional — avoid label drift).

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

### Claim schema (canonical for 017)
This contract is the **canonical source** for the 017 verification claim schema; other 017 artifacts mirror it (duplication intentional — avoid label drift).

Each MVP verification claim MUST be auditable by a lightweight record containing:
- **`claim`** — what is being claimed.
- **`evidence_class`** — one of: `automated`, `host-only-machine-pass`, `human-verified`, `d1-farmhand-convergence`, `qualitative-follow-up`, `planned-run-not-executed`.
- **`source_artifact_path`** — test result, runner bundle, scenario/log, playtest note, or infeasibility record path/reference.
- **`status`** — one of: `pending`, `complete`, `blocked`, `infeasible`.
- **`scope_note`** — required when needed for honesty (e.g., host-only, no D1, provisional tuning, infeasibility reason).

### Evidence classes and status meanings
| `evidence_class` | Meaning | Typical `status` notes |
|---|---|---|
| `automated` | Unit/controller/property/invariant tests for defined behavior. | `complete` when passing evidence is recorded; otherwise `pending`/`blocked`. |
| `host-only-machine-pass` | Host-only end-to-end run with required signals/evidence passed. **Not** `human-verified`, **not** `d1-farmhand-convergence`. | `complete` only for host-only machine evidence; never promoted above class. |
| `human-verified` | Separate human attestation. | `pending` until human attestation is recorded. |
| `d1-farmhand-convergence` | Separate real-farmhand MP convergence evidence. | `pending` until real D1 evidence is recorded. |
| `qualitative-follow-up` | Structured playtest/legibility evidence outside automated/runner path (e.g., SC-004 protocol when run). | `complete` only when that qualitative evidence is recorded; does not substitute for required automated/Gate 3 claim levels unless explicitly stated. |
| `planned-run-not-executed` | A planned verification run was not executed. **Not a pass** and not pass/fail evidence. | `infeasible` or `blocked` with reason in `source_artifact_path`/`scope_note`; lower-level completed evidence may still be claimed separately. |

Claims MUST NOT be promoted above their `evidence_class`. A `planned-run-not-executed`/`infeasible` claim is never treated as `host-only-machine-pass`, `human-verified`, or `d1-farmhand-convergence`.

### Provenance / historical evidence
017 work MUST preserve project verification provenance: do not rewrite, overwrite, or retroactively upgrade prior verification artifacts/evidence. New runs produce new evidence records; prior `machine-pass`/`verified`/D1 artifacts remain historically attributable. This applies to 017 evidence and to any referenced prior feature evidence.

### 017 MVP verification targets
- **Automated (`automated`):** C1 no-double-counting boundary (no 014/015/vanilla-tier mechanical buff hooks added), C2 `NaN`/unmeasured baseline uses `MaxSoilHealth` and absolute `SetSoilHealth` measured write (never negative `UpdateHealth` from unknown), bounded `P=10` provisional clamp, refusal classes, host lock/broadcast behavior, farmhand refusal before mutation/consumption/feedback, no broadcast on refusal, item consumption only after authoritative measured commit.
- **Host-only end-to-end (if feasible):** 3-signal style end-to-end where applicable under project Gate 3/runner rules; claim as `host-only-machine-pass` only. **If not executed:** record a `planned-run-not-executed` claim with `status` `infeasible`/`blocked` and reason; claim only completed lower-level evidence (e.g., `automated`) separately; `human-verified` and `d1-farmhand-convergence` remain pending.
- **Follow-up:** `d1-farmhand-convergence`; playtest calibration of `P=10` and temptation/legibility. SC-004 comprehension may be evaluated by a defined minimal protocol when run, or recorded as `qualitative-follow-up` when not run (see spec).

### Prohibitions
- Do not claim `human-verified` without human attestation.
- Do not claim `d1-farmhand-convergence` without real-farmhand evidence.
- Do not treat `host-only-machine-pass` as `human-verified` or `d1-farmhand-convergence`.
- Do not treat `planned-run-not-executed`/`infeasible` as a pass or as any completed machine/human/D1 evidence.
- Do not claim vanilla fertilizer-hook behavior or Harmony fertilizer MVP support.
- Do not claim final balance.
- Do not rewrite/retroactively promote historical verification evidence.