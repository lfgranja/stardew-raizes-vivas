# Flakiness, Retry & Verdict Model Requirements Quality Checklist: Headless Game Verification Runner

**Purpose**: Validate that flaky-test handling, retry policy, verdict model, honesty levels, and multi-signal triangulation requirements are complete, clear, measurable, consistent, and covered.
**Created**: 2026-10-26
**Feature**: [spec.md](../spec.md)

**Review Ownership**: This checklist is a reviewer-owned requirements-quality review artifact. Mark an item `[x]` only when the reviewer determines the requirements-quality criterion is satisfied.

**Note**: Items test whether the *requirements themselves* are well-written — not whether the implementation works.

---

## Retry Classification (Retryable vs. Non-Retryable)

- [ ] CHK001 - Is the retryable-class set explicitly enumerated (display, launch timeout, lock, process conflict, save I/O, macro timeout)? [Completeness, Spec §FR-011]
- [ ] CHK002 - Is the non-retryable set explicitly enumerated (threshold violation, crash, incomplete signals, version mismatch, missing dependency, host-guard breach)? [Completeness, Spec §FR-011]
- [ ] CHK003 - Is retry eligibility keyed on verdict state (not test names or ad-hoc flags)? [Clarity, Spec §FR-011]
- [ ] CHK004 - Is the idempotency gate for retries specified (re-running must be safe)? [Measurability, Gap]
- [ ] CHK005 - Is the distinction between `errored`/`partial` and `fail` tracked separately for retry purposes? [Consistency, Spec §FR-011]

## Bounded Attempts & Cooldowns

- [ ] CHK006 - Is the maximum attempt count specified (2 retries = 3 total)? [Clarity, Spec §FR-011]
- [ ] CHK007 - Is a suite-level retry cap defined (not just per-scenario)? [Completeness, Gap]
- [ ] CHK008 - Is the cooldown duration specified (fixed 5 s) with rationale? [Measurability, Spec §FR-011]
- [ ] CHK009 - Is the cooldown type (fixed vs. exponential backoff) explicitly chosen and justified? [Clarity, Spec §FR-011]
- [ ] CHK010 - Are per-attempt artifacts mandatory (`attempt.json`, stdout/stderr, intermediate verdict, signals)? [Completeness, Spec §FR-011]
- [ ] CHK011 - Is the retry classification required to be stable across SMAPI versions? [Consistency, Gap]

## Flaky Detection Criteria

- [ ] CHK012 - Is the operational definition of "flaky" specified (attempt 1 fails with retryable class AND later attempt passes)? [Clarity, Spec §FR-011]
- [ ] CHK013 - Is a confirmation threshold defined (e.g., N=30 runs) before promoting "flaky" status? [Measurability, Gap]
- [ ] CHK014 - Are pass-rate tiers defined (healthy ≥99%, flaky <95%, etc.)? [Clarity, Gap]
- [ ] CHK015 - Is RAFT (retry-aware flaky test) awareness documented (verdict separation)? [Coverage, Gap]
- [ ] CHK016 - Is flaky status required to be derived (not manually assigned) from run data? [Consistency, Spec §FR-011]

## Quarantine Triggers & Time-Boxes

- [ ] CHK017 - Is the quarantine trigger specified (2 flaky runs in rolling 14-day window or 2 of last 3)? [Measurability, Spec §FR-011]
- [ ] CHK018 - Is the quarantine time-box specified (14 days + one 14-day extension = 28 total)? [Clarity, Spec §FR-011]
- [ ] CHK019 - Is escalation defined (auto-re-enable + GitHub issue) on expiry? [Completeness, Spec §FR-011]
- [ ] CHK020 - Is the quarantine owner resolution chain specified (`--quarantine-owner` → env → git identity → repo owner)? [Clarity, Spec §FR-011]
- [ ] CHK021 - Is the "never quarantine" list defined (host-guard breach, version mismatch, etc.)? [Coverage, Spec §FR-011]
- [ ] CHK022 - Is human approval required for quarantine (not fully automated)? [Governance, Gap]
- [ ] CHK023 - Is quarantined scenario reporting as `skipped` in JUnit/Allure specified? [Completeness, Spec §FR-011]

## Verdict State Definitions

- [ ] CHK024 - Is the verdict set closed (pass/fail/errored/partial/skipped/flaky) with no additional states? [Clarity, Spec §FR-011]
- [ ] CHK025 - Is `flaky` defined as a derived state (not a base verdict)? [Consistency, Spec §FR-011]
- [ ] CHK026 - Is crash → `errored` with attached artifacts specified? [Completeness, Spec §FR-011]
- [ ] CHK027 - Is incomplete signal set → `partial` (never auto-promoted to `pass`) specified? [Measurability, Spec §FR-011]
- [ ] CHK028 - Is signal disagreement → `partial` specified? [Clarity, Spec §FR-009]
- [ ] CHK029 - Are exit codes mapped to verdicts (0=PASS, 18=FAIL, 19=ERRORED, 20=NO_TESTS)? [Consistency, Spec §NFR-003]

## Auto-Promotion Rules

- [ ] CHK030 - Is auto-promotion to `verified` limited to `pass` with all three signals agreeing? [Measurability, Spec §FR-016]
- [ ] CHK031 - Is auto-promotion on `partial`/`errored`/`fail` explicitly prohibited? [Clarity, Spec §FR-016]
- [ ] CHK032 - Is auto-promotion on flaky/quarantined scenarios explicitly prohibited? [Completeness, Spec §FR-016]
- [ ] CHK033 - Is the enumeration of allowed promotions specified (no `partial`→`pass`, no auto-`E`)? [Consistency, Spec §FR-016]
- [ ] CHK034 - Is re-promotion after a failed run required to be a new full triad run (not re-run until green)? [Coverage, Spec §FR-009]

## Honesty Levels (W/R/E)

- [ ] CHK035 - Is W (launch/connect) defined as tooling/launch verified with no runtime evidence? [Clarity, Spec §FR-014]
- [ ] CHK036 - Is R (runtime evidence collected) defined as evidence artifacts present and valid? [Measurability, Spec §FR-014]
- [ ] CHK037 - Is E (human judgment) defined as never auto-assigned, requiring W+R prerequisites? [Completeness, Spec §FR-014]
- [ ] CHK038 - Is the sequential assignment rule (W → R → E) specified? [Consistency, Spec §FR-014]
- [ ] CHK039 - Is claim discipline specified (safe wording, bounded claims, refusal-site liveness)? [Clarity, Spec §FR-015]
- [ ] CHK040 - Are honesty levels required in `claims.json` with risk and safe wording? [Coverage, Spec §FR-014]

## Multi-Signal Triangulation

- [ ] CHK041 - Is the requirement for ≥3 independent signals specified? [Completeness, Spec §FR-009]
- [ ] CHK042 - Is divergence between signals defined as a hard stop (not noise)? [Clarity, Spec §FR-009]
- [ ] CHK043 - Is single-source evidence explicitly prohibited for `pass`? [Measurability, Spec §FR-009]
- [ ] CHK044 - Is the necessary-but-not-sufficient asymmetry documented (failure→fail, agreement→eligibility only)? [Consistency, Gap]
- [ ] CHK045 - Are signal independence requirements specified (no shared failure mode)? [Coverage, Gap]

## Evidence Sufficiency

- [ ] CHK046 - Is the minimum evidence set enumerated (manifest, per-file SHA-256, claims, artifacts)? [Completeness, Spec §FR-014]
- [ ] CHK047 - Is save preservation (byte-identical) required as evidence? [Measurability, Spec §SC-005]
- [ ] CHK048 - Are per-attempt artifacts required for retried runs? [Coverage, Spec §FR-011]
- [ ] CHK049 - Is structural pass distinguished from truth (verifier recomputes digests)? [Clarity, Spec §FR-015]
- [ ] CHK050 - Is evidence sufficiency required to be independently verifiable offline? [Consistency, Spec §FR-015]

## False-Positive / False-Negative Control

- [ ] CHK051 - Is triangulation required to control false positives (single-source agreement insufficient)? [Measurability, Spec §FR-009]
- [ ] CHK052 - Is verdict separation required to control false negatives (track `errored`/`partial` separately from `fail`)? [Clarity, Spec §FR-011]
- [ ] CHK053 - Are rendering controls (warmup, Gen0 attribution) specified to avoid cold-JIT false failures? [Completeness, Spec §FR-013]
- [ ] CHK054 - Is the `gc-attributed` tagging required as diagnostic (still a fail)? [Consistency, Spec §FR-013]
- [ ] CHK055 - Are flaky tests required to be tracked separately to avoid masking real failures? [Coverage, Spec §FR-011]

## Cross-Cutting Consistency & Coverage

- [ ] CHK056 - Are flakiness requirements consistent across local and CI modes? [Consistency, Spec §FR-023]
- [ ] CHK057 - Are verdict requirements covered by tests? [Coverage, Gap]
- [ ] CHK058 - Is there a traceability link from verdict requirement → test case? [Traceability, Spec §SC-001..008]
- [ ] CHK059 - Are reliability requirements reviewed against Google/Microsoft/Meta flaky-test guidance? [Governance, Gap]

## Notes

- Items marked `[Gap]` indicate requirements-quality gaps where the spec does not currently define the criterion.
- Based on Google Testing Blog, Microsoft Research, Meta PFS, Trunk, Mill, pytest-rerunfailures, Allure, SLSA/in-toto, Barr et al. (IEEE TSE), IEEE 29148, NASA, INCOSE, BABOK, NIST AI RMF.