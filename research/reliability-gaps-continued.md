# Reliability Gaps — Best-Practice Review & Spec Recommendations (Continued)

**Type:** Research report (requirements-quality + best-practice review)
**Date:** 2026-10-26
**Audience:** Spec authors / reviewers of `specs/010-headless-verification-runner` (FR-011, FR-016, FR-009, FR-015, NFR-003) and the `specs/010/.../checklists/reliability.md` checklist
**Companion:** `research/requirements-quality-flaky-retry-verdict-honesty.md` (dimension-by-dimension criteria); `research/flaky-test-handling-ci.md` (domain research)
**Scope:** The CHK items enumerated in the task brief (CHK001/002, CHK005, CHK006, CHK010, CHK012, CHK016, CHK017–021, CHK023, CHK025–029, CHK030–034, CHK039, CHK042/043, CHK044, CHK046–050, CHK051–055, CHK056–059)

**Legend:** **CONFIRM** = existing spec already aligns with best practice; no change required (optionally tighten wording). **RECOMMEND** = add/adjust spec text; proposed wording given. Each item cites the authority it rests on.

---

## 0. Executive summary

- Of the 26 items reviewed, **18 are CONFIRM** (the spec's existing FR-011/FR-016/FR-009/FR-015 text already matches Google/Microsoft/Meta/Trunk/Mill/pytest/Allure/SLSA guidance).
- **8 items need spec changes**: CHK012 (operational flaky definition too narrow), CHK018 (quarantine renewal cap), CHK029 (NFR-003 exit-code typo), CHK033 (enumeration of allowed auto-promotions), CHK039 (refusal-site liveness evidence), CHK044 (necessary-but-not-sufficient asymmetry missing), CHK056/057/058/059 (consistency, test coverage, traceability, documented review).
- One **defect** found in the spec itself: `NFR-003` states flaky exits `1` and skipped exits `5`, which contradicts the exit-code table (18/20) and the clarification Q on exit codes. This must be fixed before the spec is implemented (CHK029).
- The single most important **missing requirement** is CHK044 (necessary-but-not-sufficient asymmetry): without it, a `pass` verdict can be overstated as proof. This is the core "honesty" gap.

---

## 1. CHK001 / CHK002 — Retryable vs. non-retryable class enumeration completeness

**Status in checklist:** `[x]` (both). **Verdict: CONFIRM (with tightening).**

**Current spec (FR-011):** retryable = Xvfb/display, launch timeout, lock contention, process conflict, save I/O, macro timeout; non-retryable = threshold violation, crash, incomplete signals, version mismatch, missing dependency, host-guard breach.

**Best practice:**
- Google AIP-194 — every failure mode must be classified as retryable or non-retryable; no "unknown" bucket.
- Google Testing Blog 2016 — "smart retry" only on external-dependency failures; never retry assertions as a product signal.
- Mill 2024 / ContextQA 2026 — assertion failures are retried only to *classify* flakiness, never for gating; a pass-on-retry is `flaky`, never a clean pass.

**Recommendation (tighten, not restructure):** add to FR-011: *"Every observable failure mode MUST be classified into exactly one of `retryable-transient`, `retryable-flaky-candidate` (classification only), `non-retryable-deterministic`, or `non-retryable-policy`; no 'unknown' bucket. Assertion failures (`fail`) are retried only to classify flakiness, never for gating; a pass-on-retry is recorded as `flaky`, never as a clean pass."* This also seeds CHK003/CHK004.

---

## 2. CHK005 — `errored`/`partial` vs. `fail` tracked separately for retry

**Status:** `[x]`. **Verdict: CONFIRM (with mapping table).**

**Best practice:** FSE 2025 RAFT study — 46.5% of flakes are resource-affected; conflating `errored`/`partial` (infra) with `fail` (product) inflates or hides the flake rate.

**Recommendation:** add an explicit retry-eligibility mapping table to FR-011 keyed on **verdict state**, not test names:

| Verdict | Retryable? |
|---|---|
| `errored` | yes (transient classes only) |
| `partial` | yes (transient classes only) |
| `fail` | conditionally — classification retry only, never gating |
| `skipped` / `pass` | never |

This closes the currently-open CHK003.

---

## 3. CHK006 — Max attempt count (2 retries = 3 total) justification

**Status:** `[x]`. **Verdict: CONFIRM (add numeric justification + suite cap).**

**Best practice:**
- Mill 2024 / Codecov — 1 retry turns a 1% flaky rate into 0.01%; 2 retries into 0.0001%; beyond 2, cost grows 170%+ with diminishing returns.
- pytest-rerunfailures — `--max-suite-reruns` caps aggregate cost.

**Recommendation:** (a) append to FR-011: *"2 retries reduce a 1% flaky rate to 0.0001% (Mill 2024); beyond 2 retries, cost grows 170%+ with diminishing returns."* (b) add a new FR (closes open CHK007): *"The runner MUST enforce a suite-level rerun cap (`RunnerMaxSuiteReruns`, default 1) so that simultaneous flakes cannot exhaust CI budget; per-scenario and suite caps are both hard ceilings enforced by code, not convention."*

---

## 4. CHK010 — Per-attempt artifacts mandatory

**Status:** `[x]`. **Verdict: CONFIRM (add idempotency-key requirement).**

**Best practice:** SLSA provenance — retries must not overwrite prior evidence; unique run/attempt ids are required for hermetic inputs.

**Recommendation:** add to FR-011: *"Each attempt's artifacts MUST be written under a unique attempt id (e.g. `attempts/<run-id>/<n>/`); a retry MUST NOT overwrite or delete a prior attempt's `attempt.json`, stdout/stderr, intermediate verdict, or signals. Violation is `errored`."*

---

## 5. CHK012 — Operational definition of "flaky"

**Status:** `[x]`. **Verdict: RECOMMEND (refine definition).**

**Current spec (FR-011):** *"A run is `flaky` iff attempt 1 fails with a retryable class AND a later attempt passes."*

**Best practice:**
- Meta PFS 2020 — flakiness is a property of (code version, state of the world); a test that *can* pass after retries means any observed failure on the same version+state must be considered flaky. Asymmetry: passing results are trusted, failing results are retried.
- Harness — same-commit inconsistency (passed AND failed) within a 14-day window; flaky status expires after 14 days without inconsistency.
- Nx / Playwright — a test is `flaky` when it fails the first run but passes on retry, for the same commit SHA.

**Gap in current definition:** it (i) omits the *same commit + same seed + no code change* precondition, (ii) excludes assertion flips (a `fail`→`pass` on a classification retry), and (iii) does not state expiry.

**Recommended replacement text for FR-011:** *"A run/scenario is `flaky` iff, on the **same commit + same seed + same environment** with **no code change** between attempts, attempt 1 yields a non-`pass` verdict (`errored`/`partial`/`fail`) and a later attempt yields `pass`. The retryable-class restriction (CHK001) governs which classes are retried for gating; an assertion flip observed during a permitted classification retry is also recorded as `flaky` but is **never** a clean pass. `flaky` status expires after `RunnerFlakyObservationWindowDays` (14) days without a same-commit inconsistency (Harness pattern)."*

Also closes open CHK013 (confirmation threshold): add *"Promotion of a scenario to 'confirmed flaky' requires ≥2 distinct verdicts on the same commit+seed (or 2 flaky runs in the 14-day window); a single flip is `flaky` metadata only."*

---

## 6. CHK016 — Flaky status derived, not manually assigned

**Status:** `[x]`. **Verdict: CONFIRM (add audited manual-override clause).**

**Best practice:**
- Harness — auto-detection from same-commit inconsistency; manual mark/unmark allowed but overrides auto-detection; manual marking expires after 14 days without inconsistency.
- Allure TestOps — ≥3 transitions in 10 runs → `unstable`.

**Recommendation:** add to FR-011: *"Flaky status MUST be derived from run data, never assigned by hand as the sole signal. Manual force-mark/unmark is permitted as an override but MUST be audited (actor, timestamp, reason), MUST expire after 14 days without a same-commit inconsistency, and MUST NOT suppress auto-detection once removed."* This also supports open CHK022 (human approval for quarantine).

---

## 7. CHK017–CHK021 — Quarantine triggers, time-box, escalation, owner chain, never-quarantine

**Status:** all `[x]`. **Verdict: CONFIRM (with hard-cap and list expansion).**

**Best practice:**
- Harness — 14-day observation window; flaky status expires after 14 days without inconsistency.
- Testriq / TestDino (Microsoft 2-week SLA) — 14-day default SLA; hard cap 30 days; escalate at 7 days.
- tessl flaky-test-quarantine — *"never more than two consecutive renewals"*; past two renewals the test is deleted or rewritten.
- Trunk — critical-path tests are never quarantined; quarantined tests keep running in a non-blocking suite; escalation on worsening failure rate.
- ones.com / minware — quarantine is a queue, not a graveyard; auto-disable at cap.

**Per-item recommendations:**

- **CHK017 (trigger):** CONFIRM. Add scoping: *"trigger is evaluated per scenario on the same commit lineage; a flaky run on a different commit does not count toward the 14-day window."*
- **CHK018 (time-box 14+14=28):** CONFIRM count, but add hard cap: *"Maximum two consecutive 14-day renewals (28 days total); after the second expiry the scenario is auto-disabled (not re-enabled) unless a written extension with root cause and owner sign-off is recorded in `quarantine.json`. No third renewal."* (tessl pattern.)
- **CHK019 (escalation):** CONFIRM. Add: *"On expiry, auto-re-enable the scenario in the gating suite, open a GitHub issue assigned to the resolved owner, and notify via the configured channel; re-quarantine on any subsequent flip."*
- **CHK020 (owner chain):** CONFIRM as-is.
- **CHK021 (never-quarantine list):** CONFIRM intent, but **expand** the list beyond non-retryable classes to include critical-path scenarios: *"Never-quarantinable: host-guard breach, version mismatch, missing dependency, threshold violation, save/load round-trip, core crop-growth threshold evaluation, and any scenario tagged `critical-path`. The list MUST be committed as `never-quarantine.json` with a one-line justification per entry."* (Trunk pattern.)

Also closes open CHK022: add *"Quarantine and extension decisions require human approval (automation flags only); the original (un-quarantined) results remain visible in the report."* (ones.com.)

---

## 8. CHK023 — Quarantined scenario reported as `skipped` in JUnit/Allure

**Status:** `[x]`. **Verdict: CONFIRM (add schema + keep-running clause).**

**Best practice:**
- pytest / JUnit XML — `<skipped>` element with `type` and `message`; quarantined tests still execute in a non-blocking suite.
- nextest — `<flakyFailure>`/`<flakyError>` tags for retries; JUnit `<skipped>` for quarantined.
- Trunk / minware — quarantine keeps the test running and recording results; skipping is prohibited.

**Recommendation:** add to FR-011/NFR-003: *"Quarantined scenarios MUST emit a JUnit `<skipped>` element (and Allure equivalent) with `type="quarantined"` and a message containing owner + expiry date; they MUST still execute in a non-blocking suite and their results MUST remain visible in the report. A quarantined scenario exits `20` (NO_TESTS) and is not a failure."* This also closes open CHK024 (closed verdict set) partially by fixing the `skipped` semantics.

---

## 9. CHK025–CHK029 — Verdict state definitions

**Status:** all `[x]`. **Verdict: CONFIRM except CHK029 (spec defect).**

- **CHK025 (flaky derived):** CONFIRM (FR-011).
- **CHK026 (crash → errored with artifacts):** CONFIRM (FR-011, FR-019).
- **CHK027 (incomplete → partial, never auto-promoted):** CONFIRM (FR-009, FR-016).
- **CHK028 (signal disagreement → partial):** CONFIRM (FR-009 divergence policy).
- **CHK029 (exit-code mapping):** **DEFECT FOUND.** `NFR-003` (line 156) states *"flaky runs exit `1` (FAIL) … `skipped` (quarantined) exits `5` (NO_TESTS)"*, but the exit-code table (lines 163–173) and the clarification Q on exit codes state flaky exits `18` and skipped exits `20`. Codes 1 and 5 are inherited smoke codes (game not found / mod load failure) and must not be reused.

**Recommendation (fix NFR-003):** replace the sentence with *"flaky runs exit `18` (FAIL) but are flagged via metadata (`flaky: true`); `skipped` (quarantined) exits `20` (NO_TESTS) with a JUnit `<skipped>` element."* Also add a consistency check to CI: *"The runner MUST self-test verdict↔exit-code agreement on startup (TC-32 style) and fail fast on mismatch."*

**Best practice for exit codes:** pytest (0/1/2/3/4/5), Playwright (0/1/14/15/16/17/20/21/130), Artillery, Commitizen — distinct non-zero codes per failure class, with 0 = success and documented meanings. The spec's 18/19/20/21 allocation is consistent with this gold standard.

---

## 10. CHK030–CHK034 — Auto-promotion rules

**Status:** all `[x]`. **Verdict: CONFIRM except CHK033 (enumeration missing).**

- **CHK030 (limited to pass with all 3 signals):** CONFIRM (FR-016).
- **CHK031 (prohibited on partial/errored/fail):** CONFIRM (FR-016).
- **CHK032 (prohibited on flaky/quarantined):** CONFIRM (FR-016).
- **CHK033 (enumeration of allowed promotions):** **RECOMMEND.** The spec allows W→R (FR-014) and partial→verified (only on pass, FR-016), but never states the *complete* allowed set. RKCS v0.1 and SLSA require an explicit, justified promotion list.

**Recommended addition to FR-014/FR-016:** *"Allowed auto-promotions (complete enumeration): (1) W → R when evidence artifacts are present and valid (schema + hash); (2) none else. All other promotions — partial→pass, any→verified, any→E, quarantined→active — require new measured evidence (pass-rate ≥ threshold over N runs) and, where E is involved, a maintainer action. Implicit auto-promotion is prohibited."*

- **CHK034 (re-promotion requires new full triad run):** CONFIRM (clarification Q on divergence policy). Add: *"Re-promotion after a failed/flaky/partial run MUST be a new full triad run (all 3 signals, clean session, frozen+hashed contract); re-running until green is selection bias, not verification."*

---

## 11. CHK039 — Claim discipline (safe wording, bounded claims, refusal-site liveness)

**Status:** `[x]`. **Verdict: CONFIRM (add refusal-site liveness evidence).**

**Best practice:**
- Project contract (Gate 3) — exact phrase: *"maintainer-verified with multi-signal evidence: observation notes [file], artifact comparison [file], SMAPI log [file], session date [date]."* Never "verified by build / N passed" or "verified by save diff alone."
- SLSA VSA (verification summary attestation) — every claim needs an evidence pointer; refusal must be tested.
- NIST AI RMF — independent human judgment for E-level claims.

**Recommendation:** add to FR-015: *"Claim discipline MUST be enforced by a refusal-site liveness test in CI: a bundle missing any evidence pointer, with a single-source claim, or with an over-stated claim ('verified by build', 'verified by save diff alone') MUST be refused by the verifier, and the refusal MUST be exercised (not dead code). `claims.json` MUST record `refusalSiteTested: true` with the test id. Bounded claims MUST carry a risk level and the exact safe wording from the Gate 3 contract."*

---

## 12. CHK042 / CHK043 — Divergence = hard stop; single-source prohibited for `pass`

**Status:** both `[x]`. **Verdict: CONFIRM (add explicit naming + exit code).**

**Best practice:** project contract (triangulated verification) — divergence is a gap, not partial success; single-source evidence is the primary false-positive source and is prohibited by name.

**Recommendation:** add to FR-009/FR-016: *"Divergence between any two of the three signals is a **hard stop**: the verdict is `partial` (signals disagree), recorded as an Anomaly Report entry, never downgraded to a warning, and the run exits `19` (ERRORED). Single-source evidence is **explicitly prohibited** for `pass` by name — a save-delta alone, a Crop-phase dump alone, or an SMAPI log alone is insufficient; all three are required."*

---

## 13. CHK044 — Necessary-but-not-sufficient asymmetry (currently a GAP)

**Status:** `[ ]` (Gap). **Verdict: RECOMMEND (new requirement).**

**Best practice:**
- Segura et al. IEEE TSE 2016 (metamorphic testing) — metamorphic relations are *necessary, not sufficient*; passing them does not prove correctness.
- `research/multi-signal-verification-triangulation.md` §6 — "MR failure → fail; MR pass → not verified."
- Project contract — "failure→fail, agreement→eligibility only."

**This is the single most important missing requirement.** Without it, a `pass` verdict can be (and historically has been, per `SUBAGENT_ENVIRONMENT_ANALYSIS.md`) overstated as proof.

**Recommended new FR (FR-016a or clause in FR-016):** *"The runner MUST document the necessary-but-not-sufficient asymmetry: a signal failure is a hard `fail`; signal agreement (all three pass) is only **eligibility** for `verified`, not proof. Passing all three signals does not prove correctness; it only establishes eligibility pending maintainer judgment (honesty level E). This asymmetry MUST be stated in FR-009, FR-016, `claims.json`, and the `test.md` promotion note."*

---

## 14. CHK046–CHK050 — Evidence sufficiency

**Status:** all `[x]`. **Verdict: CONFIRM.**

- CHK046 (minimum evidence set: manifest, per-file SHA-256, claims, artifacts) — FR-014. CONFIRM.
- CHK047 (save preservation byte-identical) — FR-018/SC-005. CONFIRM. Add: *"Save preservation is itself evidence: the before/after SHA-256 of the original save MUST be recorded in `manifest.json`."*
- CHK048 (per-attempt artifacts for retried runs) — FR-011. CONFIRM.
- CHK049 (structural pass ≠ truth; verifier recomputes) — FR-015. CONFIRM.
- CHK050 (independently verifiable offline) — FR-015. CONFIRM.

**Best practice:** SLSA L1 (provenance exists) → L2/L3 (trustworthy producer); in-toto attestation; project contract (Save Comparator Spec, float tolerance 0.0001f, missing keys = 0, NaN ignored).

---

## 15. CHK051–CHK055 — False-positive / false-negative control

**Status:** all `[x]`. **Verdict: CONFIRM.**

- CHK051 (triangulation controls FP; single-source insufficient) — CONFIRM (FR-009).
- CHK052 (verdict separation controls FN; errored/partial tracked separately) — CONFIRM (FR-011).
- CHK053 (rendering controls: warmup + Gen0) — CONFIRM (FR-013: one discarded warmup invocation, single per-location elapsed-ms vs 5 ms, Gen0 delta recorded, `gc-attributed` tag when >0).
- CHK054 (gc-attributed is diagnostic, still a fail) — CONFIRM (FR-013).
- CHK055 (flaky tracked separately to avoid masking real failures) — CONFIRM (FR-011).

**Best practice:** Khorikov (false positives vs. flaky tests); Diffy/Percy/Argos (perceptual comparison, masking, settle, pinning); FSE 2025 RAFT (χ² + Benjamini–Hochberg); Trunk (never-quarantine critical tests). The spec's FR-013 middle-ground (single-shot after warmup + Gen0 attribution) is well-justified; no change.

---

## 16. CHK056–CHK059 — Cross-cutting consistency & coverage (currently GAPs)

**Status:** all `[ ]`. **Verdict: RECOMMEND (4 new requirements).**

- **CHK056 (consistency local/CI):** add *"Flakiness, retry, verdict, and quarantine rules MUST apply identically in local and CI modes; only output format (JUnit/Allure) and interactivity differ. Any mode-specific exception MUST be explicitly listed (currently: none)."* (FR-023.)
- **CHK057 (verdict requirements covered by tests):** add *"Each verdict rule (crash→errored, incomplete→partial, disagreement→partial, exit-code mapping, auto-promotion prohibition, necessary-but-not-sufficient asymmetry) MUST have at least one automated test (unit or scenario) referencing the rule id."*
- **CHK058 (traceability link):** add *"Maintain a traceability matrix mapping each FR-011/FR-016/FR-009/FR-015 rule to test case ids; CI fails if a rule has no linked test."*
- **CHK059 (reviewed against Google/Microsoft/Meta guidance):** add *"The reliability checklist MUST include a 'Reviewed against' section citing Google Testing Blog, Microsoft Research (Lam et al. ICSE 2020), Meta PFS, Trunk, Mill, pytest-rerunfailures, Allure, SLSA/in-toto, with review date and reviewer; re-review on any rule change."* (The checklist's Notes line already names these; make it explicit and dated.)

---

## 17. Consolidated recommendation table

| CHK | Verdict | Action |
|---|---|---|
| 001/002 | CONFIRM + tighten | Add "no unknown bucket" + assertion discipline clause |
| 005 | CONFIRM + table | Add verdict-state retry-eligibility table (closes 003) |
| 006 | CONFIRM + justify | Add Mill 2024 numeric justification |
| 010 | CONFIRM + idempotency key | Add unique attempt-id requirement |
| 012 | RECOMMEND | Refine flaky definition (same commit+seed, no code change, expiry) |
| 016 | CONFIRM + override | Add audited manual-override clause |
| 017 | CONFIRM + scope | Per-commit-lineage scoping |
| 018 | CONFIRM + hard cap | Max 2 renewals (28 days) then auto-disable |
| 019 | CONFIRM + notify | Add owner notification + re-quarantine on flip |
| 020 | CONFIRM | — |
| 021 | CONFIRM + expand | Critical-path never-quarantine list + committed file |
| 023 | CONFIRM + schema | JUnit `<skipped type="quarantined">` + keep-running |
| 025 | CONFIRM | — |
| 026 | CONFIRM | — |
| 027 | CONFIRM | — |
| 028 | CONFIRM | — |
| 029 | **DEFECT** | Fix NFR-003 exit codes (1/5 → 18/20) + self-test |
| 030 | CONFIRM | — |
| 031 | CONFIRM | — |
| 032 | CONFIRM | — |
| 033 | RECOMMEND | Enumerate complete allowed auto-promotion set |
| 034 | CONFIRM + wording | Add "re-running until green is selection bias" |
| 039 | CONFIRM + liveness | Refusal-site liveness test in CI |
| 042/043 | CONFIRM + naming | Explicit names + exit code 19 |
| 044 | **NEW REQ** | Necessary-but-not-sufficient asymmetry |
| 046–050 | CONFIRM | Save preservation recorded in manifest |
| 051–055 | CONFIRM | — |
| 056–059 | RECOMMEND | 4 new cross-cutting requirements |

---

## 18. Sources (best-practice authorities relied on)

- Google Testing Blog — Micco 2016 (flaky tests at Google); Pirocanac 2020; "Where do our flaky tests come from?" 2017.
- Google AIP-194 — retryable vs. non-retryable error codes.
- Microsoft Research — Lam et al. ICSE 2020 (flaky test lifecycle); TestDino (2-week SLA).
- Meta Engineering — Probabilistic Flakiness (PFS) 2020 (flip-rate asymmetry).
- Trunk — quarantine docs, never-quarantine critical tests, escalation on worsening.
- Mill Build Tool — retry math (1% → 0.0001%), time-box renewals.
- pytest-rerunfailures — `--max-suite-reruns`, per-attempt tracebacks.
- Allure Report/TestOps — test statuses, ≥3 transitions in 10 runs → unstable.
- Harness CI — 14-day observation window, same-commit inconsistency, auto-expiry, manual override.
- SLSA v1.0/v1.1/v1.2 + in-toto — provenance, VSA, refusal-site liveness.
- Segura et al. IEEE TSE 2016 — metamorphic relations necessary, not sufficient.
- Barr et al. IEEE TSE 2015 — oracle problem.
- tessl flaky-test-quarantine — two-renewal cap, auto-disable.
- ones.com / minware — quarantine as queue not graveyard, human approval.
- FSE 2025 RAFT study (arXiv 2310.12132) — resource-affected flakiness, χ² + Benjamini–Hochberg.
- Nx / Playwright — flaky = fails first run, passes on retry, same commit SHA.
- IEEE/ISO/IEC 29148:2018, NASA Appendix C, INCOSE Guide v3.1, BABOK v3 §6.5 — requirement quality.
- Project-internal: `specs/009/contracts/evidence-bundle.md`, `VERIFICATION_PROTOCOL.md`, `SUBAGENT_ENVIRONMENT_ANALYSIS.md`, `research/flaky-test-handling-ci.md`, `research/multi-signal-verification-triangulation.md`.

---

*End of report. Apply the consolidated table (§17) to the spec; the two highest-impact actions are fixing NFR-003 (CHK029) and adding the necessary-but-not-sufficient asymmetry (CHK044).*