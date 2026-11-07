# Honesty & Provenance Review — Spec 017 (Joja / Agrochemical MVP)

**Date:** 2026-11-06  
**Reviewer:** implementation agent (evidence/provenance review)  
**Inputs:** `verification/claim-records.md`, `verification/automated-tests.md`,
`verification/persistence-roundtrip.md`, `verification/static-checks.md`,
`verification/manual-review.md`, `verification/provisioning-validation.md`,
`verification/host-only-gate3.md`, `verification/sc004-comprehension-protocol.md`, and the 017
source diff.

## Verdict

**No honesty/provenance violation found in the recorded 017 artifacts.** The record is bounded:
automated and mocked-persistence claims are supported; host-only end-to-end is `infeasible`;
D1, human `verified`, and SC-004 remain pending. The only correction during this verification pass
was a real 017 defect (unused constants), disclosed below and fixed before the final evidence runs.

## Checks

| # | Check | Evidence reviewed | Verdict |
|---:|---|---|---|
| 1 | No `human-verified` claim without a recorded human attestation. | Claim register keeps `human-verified` `pending`; the copy review is explicitly AI-assisted and says it is not human attestation; no human attestation artifact exists. | Pass — no overclaim. |
| 2 | No `d1-farmhand-convergence` claim without real farmhand evidence. | Claim register keeps D1 `pending`; host-only record says it is not D1; controller tests are mocked and are not presented as D1. | Pass — no overclaim. |
| 3 | No Harmony fertilizer-hook or vanilla-fertilizer-slot MVP claim. | 017 code adds no Harmony patch; the item registration is `Type = "Basic"`, Category 0, no context tags, no `Texture`; the controller comment says it never writes `HoeDirt.fertilizer`. The existing Harmony patch is the pre-existing spec 015 harvest patch, not 017. | Pass — 017 is mod-intercepted only. |
| 4 | No final balance claim. | Spec/plan/quickstart/manual review all call `P=10` provisional/tunable; no artifact claims balance or final tuning. | Pass — provisional wording preserved. |
| 5 | No double-counting claim beyond evidence. | Claim register limits the automated C1 claim to a service-dependency boundary test and explicitly says it is not a full harvest/growth composition proof. No artifact claims SC-006 is fully proven by tests. | Pass — narrow claim. |
| 6 | No persistence overclaim beyond the tested seam. | `persistence-roundtrip.md` states the mocked `ISoilHealthService.SaveData`/`LoadData` seam only; documents the sparse-zero reload-as-unknown behavior; explicitly excludes real disk/save and Gate 3. | Pass — seam scope is explicit. |
| 7 | No false-success/SC-002/FR-013 claim promoted from fallback-only tests. | Claim register says fallback-only tests cannot promote FR-007/FR-013/SC-002; controller tests cover fallback no-escape behavior, not a positive SC-002/FR-013 completion claim. | Pass — no promotion. |
| 8 | No SC-004 completion without a recorded run. | SC-004 protocol status is `pending`; claim register status is `pending`; no participant run or evidence note exists. | Pass — SC-004 not claimed complete. |
| 9 | No fabricated host-only run, log, bundle, or machine-pass. | `host-only-gate3.md` records `planned-run-not-executed` / `infeasible` with exact missing runner capabilities; no 017 scenario, bundle, or SHA-256 anchor exists. | Pass — not a pass, no fabrication. |
| 10 | No rewrite/retroactive promotion of prior verification artifacts. | The RED gate record and log are unchanged historical evidence; prior feature artifacts were not modified. Phase 7 produced new files. | Pass — provenance preserved. |
| 11 | No `Data/Shops`, Joja economy, or shipped item-acquisition claim. | `provisioning-validation.md` explicitly excludes `Data/Shops`, economy/quest sources, and external item spawners; the manual grant is scoped to `LivingRoots.Verification` and is not implemented. | Pass — no shipped acquisition path. |
| 12 | The full-suite correction is disclosed, not hidden. | `automated-tests.md` records the interim `ProductionSources_NoUnreferencedConstantsSurvive` failure and the `5dd838d` fix; the final-tree run is the evidence. | Pass — correction disclosed. |

## Residual gaps (not violations)

These are expected pending/follow-up items, not overclaims:

- **Host-only Gate 3:** infeasible with the current runner (no agrochemical action, no item-grant
  path, no right-click simulation). No machine-pass is claimed.
- **D1 farmhand convergence:** no real farmhand session. Pending.
- **Human `verified`:** no human attestation. Pending.
- **SC-004 comprehension:** protocol finalized but not executed. Pending.
- **Manual copy review:** performed by the implementation agent, not an independent human. The
  artifact says so; human review would be separate.
- **Real disk persistence:** not tested; only the mocked seam was tested.
- **Final balance:** not claimed; `P=10` remains provisional.

## Provenance notes

- The RED gate (`red-phase.md` + `red-phase-run.log`) remains a failing-run record and is not
  presented as passing evidence.
- The final targeted/persistence/full-suite logs are new Phase 7 artifacts. The interim failed
  full-suite run was not committed as a passing record; its correction is documented in
  `automated-tests.md`.
- No prior verification artifact was edited, overwritten, or retroactively promoted by this
  review.
