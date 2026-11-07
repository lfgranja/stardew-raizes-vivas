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

## Phase 8 addendum (2026-11-06/07)

After this review was written, the 016-hardened runner was used to verify the 017 path end-to-end.
A real host-only run passed and is recorded in [`host-only-gate3-run.md`](host-only-gate3-run.md) and
the tracked bundle `evidence-bundle-run-20261107T015936Z-7f35/` (`test.md` `machine-pass`, honesty `R`).

The addendum preserves the original review's boundaries: the machine-pass is host-only machine
evidence; it is not `human-verified`, not `d1-farmhand-convergence`, and not SC-004. The C2
unknown/`NaN` baseline path is exercised by automated tests, not by the Gate 3 scenario. The
three failed attempts that preceded the passing run are disclosed in the run record and are not
cited as passing evidence. No prior verification artifact was rewritten or retroactively promoted;
`host-only-gate3.md` keeps the infeasibility record as historical context.

## Phase 9 addendum (2026-11-06) — convergence re-audit

**Append-only; the review above is unchanged.** The Phase 9 convergence remediation
(T064–T089) was implemented and re-verified; the append-only closure record is
[`convergence-rerun.md`](convergence-rerun.md). What this addendum reaffirms and discloses:

- **No promotions reaffirmed.** No claim in the register was promoted by the Phase 9
  re-run or by this addendum. D1 (`d1-farmhand-convergence`), human `verified`, and SC-004
  remain `pending`. The host-only `machine-pass` claim is unchanged in scope (host, measured
  50 → 40 path only) and was **not** re-run on the post-Phase-9 tree. `planned-run-not-executed`
  remains non-passing everywhere it appears. Acquisition remains descoped/`blocked`; the
  measured-zero persistence row remains `blocked`.
- **V-01 empty-log acknowledgement.** The 2026-11-06 offline Gate 3 bundle-check stdout was
  cited as `/tmp/opencode/bundle-check.log`, which was **0 bytes** — the exit-0 attestation for
  the offline bundle check therefore rested on the session, not on retained output. The bundle
  itself was verified independently in the parent session on 2026-11-06: `manifest.json`
  16/16 SHA-256 digest spot-audit OK, `claims.json` honestyLevel `R`, verdict `pass`. The
  empty-log citation is superseded by the T091 re-run once committed (a fresh offline bundle
  check with its full stdout retained as a committed artifact); until then, the attestation
  gap for that single cited log is acknowledged here rather than papered over.
- **Stale check #9 supersession.** Check #9 above states "no 017 scenario, bundle, or SHA-256
  anchor exists". That statement was true when this review was written (2026-11-06, pre-Phase-8)
  and became stale once the Phase 8 host-only run produced the tracked bundle
  `evidence-bundle-run-20261107T015936Z-7f35/` (`test.md` `machine-pass`, honesty `R`). No
  scenario/bundle/anchor claim that was already false pre-Phase-8 is silently carried forward:
  the infeasibility record in `host-only-gate3.md` is retained as historical provenance, check
  #9's "Pass" verdict applies to the pre-Phase-8 tree only, and the current bundle/anchor facts
  live in `host-only-gate3-run.md`, `test.md`, and `convergence-rerun.md`. This is a
  supersession note, not a retroactive edit of the check.
- **Bundle digest audit.** The tracked bundle's `manifest.json` digest matches
  `manifest.sha256` and the 16/16 spot-audit was reported OK by the parent session on
  2026-11-06. The register's offline-bundle citation remains supported by the bundle's own
  integrity files; the empty cited stdout log (V-01) does not affect the bundle's integrity,
  only the retained-output attestation for the check.
- The Phase 8 addendum's boundaries still hold: machine-pass is not `human-verified`, not D1,
  not SC-004, and not the C2 unknown-baseline path. The C1 claim remains a dependency-boundary
  check plus lexical proxies (`double-counting-risk.md`); the measured-zero outcome does not
  survive save/load (`measured-zero-persistence.md`).

## Phase 10 addendum (2026-11-07) — post-Phase-9/10 tree re-verification

**Append-only; the review above is unchanged.** Phase 10 (T090–T104) was implemented and
re-verified; the append-only evidence lives in [`convergence-rerun.md`](convergence-rerun.md),
[`host-only-gate3-phase10-run.md`](host-only-gate3-phase10-run.md), and
`evidence-bundle-run-20261107T040235Z-589a/`. What this addendum reaffirms and corrects:

- **T090 clean host-only pass @ c9ff596.** The 016-hardened host-only Gate 3 was re-run on the
  post-Phase-9/10 tree and produced a clean host pass: bundle `run-20261107T040235Z-589a`,
  `test.md` promoted by the runner itself to `machine-pass` (honesty `R`), `verdict: pass`,
  `signalsComplete/signalsAgree: true`, **3/3 thresholds passed**, `farmhandBreach: false`,
  measured 50 → 40 (delta −10), `phaseDays` unchanged, host `isMainPlayer` true. The Phase-9
  addendum's line that the host-only `machine-pass` was **"not re-run on the post-Phase-9
  tree"** is superseded by this addendum — a re-run occurred and passed (commit `c9ff596`).
  This supersedes nothing else: the e5882c1-era bundle remains retained, and the C2
  unknown/`NaN` baseline path remains automated-coverage only.
- **T091 landed — V-01 superseded by exit-code attestation.** The Phase 9 addendum above
  recorded that the cited `/tmp/opencode/bundle-check.log` was 0 bytes and carried an outstanding
  expectation that the "empty log citation is superseded by the T091 re-run once committed (a
  fresh offline bundle check with its full stdout retained as a committed artifact)". That
  expectation — **full stdout retained as a committed artifact** — was not met: the
  hardened runner's `--verify-bundle` mode **emits no stdout by design**; its attestation is
  the exit code. Observed exit code **0** on 2026-11-06 (twice) and again on 2026-11-07 after
  Phase 10. This addendum corrects the expectation here rather than leaving it silently pending:
  no stdout artifact was or will be retained for that check, and future citations of
  `--verify-bundle` must attest the exit code, not a captured log. V-01 is superseded
  accordingly; the bundle's own integrity files (16/16 `manifest.json` digest spot-audit OK,
  `manifest.sha256` match, `verdict.json` pass, `claims.json` honestyLevel `R`) remain the
  bundle-level evidence.
- **No promotions.** No claim in the register was promoted by the Phase 10 re-run or by this
  addendum. **D1** (`d1-farmhand-convergence`), **human `verified`**, and **SC-004** remain
  `pending`. The two `blocked` rows (normal-play acquisition; measured-zero save/load) remain
  `blocked`. The host-only `machine-pass` claim is host-only, measured 50 → 40 path only, and
  is not D1, not human `verified`, and not SC-004.
- **Phase-8 check #9 note unchanged.** Check #9 above ("no 017 scenario, bundle, or SHA-256
  anchor exists") remains stale-as-written for the pre-Phase-8 tree; its "Pass" verdict applies
  to the pre-Phase-8 tree only and is mitigated by the Phase 8 addendum (which documents the
  bundle and runner-owned anchor) and now by the Phase 10 row in `claim-records.md`. No edit to
  check #9 is made by this addendum; this note merely re-anchors the mitigation chain.
- The Phase 9 addendum's remaining content stands unchanged apart from the two supersessions
  above (the "not re-run" line and the V-01 stdout expectation). Bundle provenance, no-rewrite
  discipline, and the boundary reaffirmations (machine-pass ≠ human/D1/SC-004; C1 =
  dependency-boundary check plus lexical proxies, no behavioral composition proof) all hold.
