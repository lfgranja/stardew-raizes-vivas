# Deriving (Not Manually Assigning) Flaky Test Status from Run Data in CI — Best Practices and Normative Requirements

**Topic:** How a CI system should *derive* a test's flaky status from observed run data rather than let humans *assign* it by label/annotation.

**Context:** Living Roots headless verification runner (specs 009/010). 4-state verdict model `{pass, fail, errored, partial, skipped}`; `flaky` is a derived mark, never a primary verdict. This report isolates the derivation-vs-assignment question and supplies normative requirement wording.

**Date:** 2026-10-27

**Companion reports:** `research/flaky-test-handling-ci.md` (domain research, 41 refs); `research/requirements-quality-flaky-retry-verdict-honesty.md` (requirement-quality criteria, 47 checks).

---

## 1. Executive Summary

1. **Flaky status must be a function of run data, never a label.** Google, Meta, Trunk, and pytest-rerunfailures all treat "flaky" as a *derived property of a history of outcomes on the same code version + environment*, not an attribute a developer writes next to the test. Manual marking (`@pytest.mark.flaky`, `// FLAKY`) is a *quarantine/override input*, not a detection mechanism (Micco 2016; PFS 2020; Trunk detection docs).
2. **The core asymmetry (Meta PFS):** a passing result is trusted; a failing result is only a *hint to retry*. If a test can pass after retries on the same version + state, then *every* observed failure on that version + state is flaky. This is the single most important principle for derivation (Meta Engineering, "Probabilistic flakiness", 2020).
3. **Statistical derivation uses a binomial proportion CI over N runs on the same commit.** Recommended N = 30 (3-in-30 measurable rate; range 20–50). Flag flakiness when the CI for the pass rate excludes 100% (Testriq 2025; project CHK013).
4. **Automation proposes; humans dispose.** Automation computes the score and opens a *pending* quarantine proposal; the `quarantine.json` entry is written only after maintainer approval (`--quarantine-approve` or CODEOWNERS review). Original results stay visible regardless (ones.com; project §6.6).
5. **False-positive control** comes from (a) requiring the same commit+seed, (b) distinguishing `errored`/`partial` (infra) from `fail` (product) per RAFT (46.5% resource-affected), (c) perceptual (not pixel-exact) comparison for rendering, (d) significance + multiple-testing correction where rates are compared (Benjamini–Hochberg).
6. **Governance:** never-quarantine list for critical paths; hard 14-day SLA / 30-day cap; auto-unquarantine only on measured pass-rate evidence (≥99% over 30 runs); suite-level alarm if >2–3% of tests are known flaky.

---

## 2. Authoritative Evidence

### 2.1 Google Testing Blog — Micco 2016 ("Flaky Tests at Google and How We Mitigate Them")

- **~1.5% of test runs flaky**, affecting **~16% of the test inventory**; 84% of pass→fail transitions involve a flaky test.
- **Rerun mechanism is used only for tests marked flaky *or* on explicit user request** — never blanket retries. This is the Google stance: flakiness is a *property discovered from history*, and retries are a *response* to a discovered flake, not a detection tool (Micco 2016 comment: "Our rerun mechanism is only used for tests that are marked as flaky or when users specifically request it").
- **Reservoir pattern:** new tests run in a loop for a week before entering the critical CI path — derivation happens *before* gating, not after.
- **Pass-rate tiers (digia.tech citing Google):** healthy ≥99%; quarantine <95%; suite-level alarm if >2–3% of tests are known flaky.
- **Key quote for derivation:** Google does not treat a single failure as flakiness; flakiness is a *rate over repeated runs on the same code*, and the system distinguishes a reliably failing test (clear signal) from a flaky one (noise).

### 2.2 Microsoft Research — Lam et al., ICSE 2020 ("A Study on the Lifecycle of Flaky Tests")

- Six large proprietary Microsoft projects; **async calls are the leading root cause** of flakiness.
- **Two-week "fix or remove" SLA** for flaky tests (TestDino 2026 benchmark, citing Microsoft).
- **13% of Microsoft test failures are flaky** (Atlassian, citing MS Research); 26% of tests flaky in the 2021 Semaphore survey.
- **Developers claim fixes that do not reduce flake frequency** (Lam 2020) → unquarantine must be gated on *measured* pass-rate improvement, not on a code change alone. This is the governance anchor for "human approval + evidence".

### 2.3 Meta Engineering — "Probabilistic Flakiness: How do you test your tests?" (2020)

- **Core thesis:** all real-world tests are flaky to some extent; the right question is *how flaky*, not *whether*. PFS is a universal 0–1 score computed by Bayesian inference over a moving window of historical runs.
- **Two parameters per test:** (1) probability of bad state (code/version/state-of-world failure), (2) probability of failure in a good state = PFS.
- **The asymmetry principle (the derivation rule):** "A passing test indicates the absence of corresponding regression, while a failure is merely a hint to run the test again." If a test *can* pass after an arbitrary number of retries on the same version + state, then *any* observed failure on that version + state must be considered flaky. This is exactly "derive, don't assign".
- **Posterior, not point estimate:** PFS yields a full posterior distribution; a narrow posterior = confident estimate; a wide/multi-modal posterior = need more data before classifying. This is the statistical-honesty mechanism against premature classification.
- **PFS piggybacks on normal CI runs** — no extra reruns required; score recalculated in a fraction of a second per test as each new result arrives. Scale: millions of tests, real time.
- **Incentive structure:** when a test's flakiness regresses beyond tolerance, a ticket is created for the declared owner; tests that deteriorate significantly are marked flaky and become *ineligible for change-based testing*. Status is a *consequence* of measured score, not a developer annotation.

### 2.4 Trunk — Flaky Tests documentation (detection + quarantine)

- **Monitors, not labels:** Trunk uses independent monitors (Pass-on-Retry, Failure Rate, Failure Count) that observe run data and *transition* a test to `flaky`/`broken`/`healthy` automatically; muting and `Flag as Flaky` are *overrides*, not the detection mechanism.
- **Minimum sample size gate:** a test with 8 runs / 3 failures (37.5%) is *not* flagged because N < 50 — insufficient data. This is the false-positive-control mechanism: never classify on small samples.
- **Hysteresis (activation vs. resolution thresholds):** activate at 30%, resolve at 15% to prevent flapping between flagged and healthy. A test hovering at 20% keeps its current status.
- **Branch-aware detection:** pass-on-retry is branch-agnostic; failure-rate monitors are scoped per branch (main strict, PR lenient, merge-queue strict) — derivation must be conditioned on where the run happened.
- **Dry-run via labels:** deploy a new health-classification monitor as a *labeling* monitor first, observe which tests it would flag, refine, then flip to "Classify test status". This is the governance pattern for introducing a new derivation rule without committing to status changes.

### 2.5 pytest-rerunfailures — manual marking as the *anti-pattern* to avoid

- pytest-rerunfailures exposes `@pytest.mark.flaky(reruns=5)` — a *developer-assigned* label that drives reruns. This is the **assignment** model the user is asking us to move away from: the test's flakiness is declared in source code, not derived from run data.
- Useful as the *quarantine response* (rerun a known-flaky test) but **not** as the *detection mechanism*. The project's own guidance (ContextQA 2026; Codecov; Google 2016) is to turn off blind auto-retries and derive flakiness from observed outcomes.
- `--max-suite-reruns` is the aggregate-cost guardrail that a derivation system must also enforce.

### 2.6 Microsoft Research survey + Atlassian Flakinator (corroborating)

- Atlassian Flakinator: 350M+ executions/day, 81% detection rate for certain products, implicit retries + DB logging — detection is *data-driven*, status is *computed*, not annotated.

---

## 3. Statistical Derivation

### 3.1 The model: binomial proportion CI over N runs on the same commit

- Treat each run on the **same commit + same seed + same environment** as a Bernoulli trial: `pass = 1`, `non-pass = 0`.
- Compute the sample pass rate `p̂ = x/N` and its confidence interval (Wilson score or Clopper-Pearson exact binomial CI).
- **Flag as flaky when the CI for the pass rate excludes 100%** — i.e., there is statistically significant evidence that the true pass rate is < 100% (Testriq 2025; project `research/flaky-test-handling-ci.md` §5.3).
- N = 30 is the practical minimum for a 3-in-30 measurable rate (Testriq; project CHK013). Range 20–50 (Testriq 2025). Below N = 20, do not classify — insufficient data (Trunk minimum-sample-size gate; project C-C2).

### 3.2 Why N = 30 specifically

- A 3-in-30 observed flake rate is the smallest *actionable* signal: 3 failures in 30 runs = 10% failure rate, whose Wilson 95% CI lower bound is ~2.8%, comfortably excluding 100%.
- With N = 10, a single failure gives a 95% CI of roughly [0.3%, 44.5%] for the failure rate — far too wide to exclude 100% at any reasonable confidence; classification would be noise.
- With N = 30, the same single failure gives a 95% CI whose upper bound on the pass rate is ~99.9%... actually a single failure in 30 gives pass rate 29/30 = 96.7%, CI excluding 100% at 95% confidence (Clopper-Pearson: P(pass=1) upper...). The point: N=30 is where a *single* observed flip becomes statistically distinguishable from a perfectly reliable test.

### 3.3 Confidence level and multiple-testing correction

- Default confidence level: **95%** (α = 0.05) for the per-test CI.
- Where rates are *compared* across conditions (RAFT resource configs; per-branch rates), apply **Benjamini–Hochberg** false-discovery-rate correction at q = 0.05 (FSE 2025 RAFT study; project J-C5, C-C7). Without BH, a suite of 1000 tests at α = 0.05 produces ~50 false flaky classifications by chance alone.
- Report the posterior width (Meta PFS style) alongside the CI: a narrow CI = high confidence in the classification; a wide CI = "need more data", not "healthy".

### 3.4 The verdict-aware derivation (project 4-state model)

Map verdicts onto the Bernoulli outcome so derivation is honest about *what* flipped:

| Verdict | Bernoulli outcome | Counts as flake signal? |
|---|---|---|
| `pass` | success (1) | no |
| `fail` | non-pass (0) | only if it passes on retry (classification retry) |
| `errored` | non-pass (0) | yes — primary infra-flake candidate (RAFT) |
| `partial` | non-pass (0) | yes — incomplete-execution flake candidate |
| `skipped` | excluded from denominator | no (track skip-rate separately) |

- A test is **flaky** iff, on the same commit + same seed, it yields ≥2 distinct verdicts across runs, **or** an `errored`/`partial`→`pass` flip (project C-C1, E-C3).
- `errored`/`partial` must be tracked **separately** from `fail`: a high `errored` rate with low `fail` rate = infra flakiness (RAFT candidate), not a product regression (project C-C4, J-C3). Conflating them inflates or hides the flake rate.

### 3.5 Derivation must be conditioned on commit + seed + environment

- Flakiness is a property of **(code version, state of the world)** (Meta PFS). A test that fails on commit A and passes on commit B is a *regression*, not a flake.
- Runs must be grouped by commit SHA + seed + environment fingerprint (Xvfb resolution, CPU quota, rasterizer) before computing the CI. Cross-commit aggregation is a category error.
- Flaky status **expires** after 14 days without a same-commit inconsistency (Harness pattern; project CHK012). A stale "flaky" label that no longer reflects current data is itself a false positive.

---

## 4. Automation vs. Human Approval

### 4.1 The split

| Responsibility | Owner | Rationale |
|---|---|---|
| Compute pass-rate CI, posterior, flip count | Automation | Deterministic given data; no judgment |
| Open a *pending* quarantine proposal when thresholds are met | Automation | Timely, consistent, auditable |
| Write the `quarantine.json` entry | **Human (maintainer)** | ones.com: "Automation flags; humans decide" |
| Approve extension beyond SLA | **Human** | Prevents graveyard drift |
| Retire/delete a quarantined test | **Human** | Irreversible; requires evidence |
| Unquarantine after a fix | Automation *proposal* + human sign-off | Lam 2020: claimed fixes often don't reduce flake frequency |

### 4.2 Why automation must not write status directly

- **Meta PFS uses status as an incentive** ("carrot and stick"): a test marked flaky becomes ineligible for change-based testing. That is a *consequential* action; it must be attributable to a human decision, not a silent classifier.
- **Google's rerun mechanism is gated on "marked as flaky"** — but Google's marking is itself a *human-curated* list derived from data, and the rerun is a *response*. The derivation (data) and the disposition (status change) are separate steps.
- **Trunk's "Flag as Flaky"** is explicitly a *user override* distinct from monitor-driven classification; even Trunk separates "monitor activates" (automation) from "quarantine" (configurable auto-or-manual, with human override).

### 4.3 Normative: automation proposes, humans dispose

> **FR-FLAKY-DERIVE-AUTO:** The system SHALL compute flaky status as a derived function of observed run data (pass-rate CI over N runs on the same commit+seed, plus flip count). The system SHALL NOT accept a developer annotation, source-code marker, or manual label as the *sole* basis for flaky status. A manual flag (e.g., `--flag-as-flaky`) MAY be recorded as an *override input* that is merged with, but never replaces, the derived status.
>
> **FR-FLAKY-DERIVE-HUMAN:** Automation MAY create a *pending quarantine proposal* when derived thresholds are met. The `quarantine.json` entry SHALL be written only after explicit maintainer approval (via `--quarantine-approve` or CODEOWNERS review). Original test results SHALL remain visible regardless of quarantine status. Automation SHALL NOT auto-approve its own proposal.

---

## 5. False-Positive Control

A *false positive* here is a test classified as flaky (and thus quarantined / made ineligible) when it is actually a real regression or a reliable test. Controls:

1. **Same-commit+seed grouping** (§3.5): prevents a regression on commit B being misread as a flake of commit A.
2. **Minimum sample size gate** (N ≥ 20, recommend 30): Trunk refuses to flag `test_signup` (8 runs, 3 failures, 37.5%) because N < 50. Small samples cannot exclude 100%.
3. **Hysteresis** (activate 30%, resolve 15%): prevents a test hovering at 20% from flapping between flaky and healthy every run.
4. **Verdict separation** (§3.4): a high `errored` rate with low `fail` rate is infra flakiness (RAFT), not a product regression. Without this, a genuine product bug that manifests as `errored` (crash) would be quarantined as "flake".
5. **RAFT awareness** (FSE 2025, 46.5% resource-affected): detection must vary CPU/memory/Xvfb configs; a test whose failure rate differs statistically across configs is resource-affected and must be classified as *infra* flake, not quarantined as a generic flake. Use χ² + Benjamini–Hochberg (q = 0.05) for the comparison.
6. **Rendering FP control** (project J-C4): perceptual comparison (pHash/SSIM), not pixel-exact; mask dynamic regions; settle N frames; pin fonts/resolution/rasterizer; baseline per platform. Pixel-exact comparison is the #1 source of false flaky classifications in visual tests.
7. **Posterior width reporting** (Meta PFS): a wide/multi-modal posterior means "need more data", not "flaky". Do not classify on an uncertain posterior.
8. **Never-quarantine list** (Trunk): critical-path tests (save/load, core crop growth) are never quarantined regardless of derived score — accepts the productivity hit to prevent loss of coverage (false negative on the most important paths).
9. **Stale expiry** (14 days): a flaky classification that no longer reflects current data is itself a false positive; auto-resolve.

### Normative

> **FR-FLAKY-FP-SAMPLE:** The system SHALL NOT classify a test as flaky when its run count on the same commit+seed is below `RunnerFlakyMinSampleSize` (default 30). Such a test SHALL be recorded as `insufficient-data`, never as `healthy` or `flaky`.
>
> **FR-FLAKY-FP-HYST:** A flaky classification SHALL activate only when the pass-rate CI excludes 100% at the configured confidence level AND the observed failure rate meets or exceeds the activation threshold (default 30%). A classification SHALL resolve only when the pass rate is at or above the resolution threshold (default 99%) over the same N. A test whose rate lies between the two thresholds SHALL retain its current status (no flapping).
>
> **FR-FLAKY-FP-RAFT:** Where detection varies resource configurations, the system SHALL apply a χ² test with Benjamini–Hochberg correction (q ≤ 0.05) before declaring a test resource-affected. A test SHALL NOT be classified as a generic flake on the basis of a single-configuration failure rate alone when multi-config data is available.

---

## 6. Governance

### 6.1 Suite-level alarm

- If >2–3% of tests are known flaky, the team has developed dysfunctional rerun norms (Google, digia.tech). This is a *governance* signal, not a per-test one: it triggers an escalation to the maintainer/owner, not a per-test quarantine.

### 6.2 Time-boxes and auto-unquarantine

- Default SLA 14 days; hard cap 30 days; escalate at 7 days (no progress); auto-disable/delete at 30 days unless extension approved (Testriq, QASkills, minware, TestDino/Microsoft, Trunk).
- Auto-unquarantine requires **measured** pass-rate improvement (≥99% over 30 runs on the same commit lineage), not a code change alone (Mill 2024; Lam 2020; TestDino; Datadog). Re-quarantine on any flip.

### 6.3 Owner and ticket

- Every quarantine entry carries: owner (feature/component owner, not test author), ticket ID, deadline, root-cause category, flake rate, verdict-history link (ones.com; Currents; Nx).
- Separate test owner from release approver (ones.com).

### 6.4 Audit trail

- Every status transition (healthy→flaky, flaky→quarantined, quarantined→healthy) SHALL be recorded in the evidence bundle (`claims.json`) with the evidence pointer (run IDs, commit SHA, CI bounds), the rule applied, and the decision owner (automation vs. maintainer). SLSA VSA style: a transition without an evidence pointer is invalid.

### Normative

> **FR-FLAKY-GOV-SUITE:** The runner SHALL compute and report the percentage of tests in `flaky` or `quarantined` status per suite. If this percentage exceeds `RunnerFlakySuiteAlarmPercent` (default 3%), the runner SHALL emit a suite-level alarm event and SHALL NOT auto-quarantine additional tests until a maintainer acknowledges the alarm.
>
> **FR-FLAKY-GOV-BOX:** Every quarantine entry SHALL include, as schema-mandatory fields: `owner`, `ticketId`, `deadline` (ISO-8601), `rootCauseCategory`, `flakeRate`, `commitSha`, `runIds[]`. An entry missing any mandatory field SHALL be rejected at write time.
>
> **FR-FLAKY-GOV-UNQ:** Unquarantine SHALL require measured evidence: pass rate ≥ `RunnerUnquarantinePassRate` (default 99%) over at least `RunnerUnquarantineRunCount` (default 30) runs on the same commit lineage, plus owner sign-off. A code change alone SHALL NOT unquarantine a test. Re-quarantine SHALL occur automatically on any subsequent flip.
>
> **FR-FLAKY-GOV-NEVER:** The project SHALL maintain an explicit `never-quarantine` list (critical paths: save/load, core crop growth). Tests on this list SHALL never be quarantined regardless of derived flake score; the productivity hit SHALL be accepted.
>
> **FR-FLAKY-GOV-AUDIT:** Every flaky-status transition SHALL be appended to `claims.json` with `{testId, from, to, commitSha, runIds[], ruleId, decisionOwner (automation|maintainer), timestamp}`. A transition without `runIds` or `ruleId` SHALL be rejected.

---

## 7. Normative Requirement Wording (consolidated, ready to paste into spec)

> **FR-FLAKY-001 (Derived, not assigned).** `flaky` SHALL be a *derived* mark computed from verdict history on the same commit+seed, never a primary verdict that a single run can emit. A single run SHALL NOT be able to emit `flaky`. Manual annotations (source markers, `@flaky`, `--flag-as-flaky`) SHALL be recorded as override inputs merged with, but never replacing, the derived status.
>
> **FR-FLAKY-002 (Derivation function).** The derived status SHALL be computed as: `p̂ = (pass count)/(total count)` over runs sharing the same commit SHA + seed + environment fingerprint; Wilson 95% CI; classify as `flaky` iff the CI excludes 100% **and** N ≥ `RunnerFlakyMinSampleSize` (default 30) **and** the observed failure rate ≥ `RunnerFlakyActivationRate` (default 30%). Below N, classify as `insufficient-data`.
>
> **FR-FLAKY-003 (Asymmetry).** Per Meta PFS: a passing result SHALL be trusted; a failing result SHALL be treated as a hint to retry. If a test can pass after retries on the same version+state, then every observed failure on that version+state SHALL be considered flaky. The system SHALL NOT trust a single passing result as proof of reliability, nor a single failure as proof of regression.
>
> **FR-FLAKY-004 (Verdict-aware).** Derivation SHALL track `errored`/`partial` rates separately from `fail` rates. A high `errored` rate with low `fail` rate SHALL be classified as infra flakiness (RAFT candidate), not a product regression. `skipped` SHALL be excluded from the denominator (track skip-rate separately).
>
> **FR-FLAKY-005 (Conditioning).** Derivation SHALL group runs by commit SHA + seed + environment fingerprint. Cross-commit aggregation for flakiness classification SHALL be prohibited. Flaky status SHALL expire after `RunnerFlakyObservationWindowDays` (default 14) without a same-commit inconsistency.
>
> **FR-FLAKY-006 (Automation proposes, humans dispose).** Automation SHALL compute the derived status and MAY create a pending quarantine proposal. Automation SHALL NOT write `quarantine.json`. The entry SHALL be written only after maintainer approval. Original results SHALL remain visible regardless of status.
>
> **FR-FLAKY-007 (False-positive controls).** The system SHALL enforce: minimum sample size gate (N ≥ 30), hysteresis (activate 30% / resolve 99%), RAFT-aware multi-config comparison with Benjamini–Hochberg (q ≤ 0.05), perceptual (not pixel-exact) comparison for rendering tests, posterior-width reporting, never-quarantine list, and 14-day stale expiry.
>
> **FR-FLAKY-008 (Governance).** The system SHALL maintain a suite-level alarm at >3% known-flaky; mandatory quarantine-entry schema fields; evidence-gated unquarantine (≥99% over 30 runs + owner sign-off); never-quarantine list; and an audit trail in `claims.json` for every transition.

---

## 8. Recommendation

**Adopt derivation-over-assignment as the project's stated policy**, with the following concrete decisions:

1. **Make `flaky` a derived mark only.** Amend FR-011 / E-C3 wording to: "`flaky` is computed by the runner from verdict history on the same commit+seed; no source annotation, no manual label, and no single run can set it." This closes the gap identified in CHK025 and aligns with Meta PFS and Google.
2. **Use N = 30, Wilson 95% CI, excluding 100%.** Adopt `RunnerFlakyMinSampleSize = 30` as the confirmation threshold (CHK013), with `insufficient-data` below it. Do not classify on fewer runs.
3. **Keep the automation/human split.** Automation computes + proposes; maintainer approves via `--quarantine-approve` (CHK014/CHK017 already imply this). Never let the classifier write `quarantine.json`.
4. **Add the false-positive controls as SHALLs**, not best practices: minimum sample size, hysteresis, RAFT multi-config + BH, perceptual rendering comparison, posterior width, never-quarantine list, 14-day expiry. These are the project's defense against the #1 risk (misclassifying a real regression as a flake and hiding it).
5. **Add governance SHALLs**: suite-level alarm at 3%, mandatory quarantine schema, evidence-gated unquarantine, audit trail in `claims.json`.
6. **Dry-run new derivation rules as labels first** (Trunk pattern): before flipping a new monitor to "Classify test status", run it as a labeling monitor (`would-be-flaky`) and compare against maintainer judgment. This is the safe rollout path for changing derivation thresholds without destabilizing status.

**Why this over the alternatives:**
- *Manual assignment (`@pytest.mark.flaky`)* is the pytest-rerunfailures model; it scales poorly, is stale-prone, and conflates "I think this is flaky" with "the data says it's flaky". The project's CHK012/CHK013/CHK014 already reject it.
- *Pure automation writing status* (no human gate) is the Meta PFS "carrot and stick" at scale, but Meta has millions of tests and a dedicated DevInfra org; for a single-maintainer project, the human gate is the correct governance level, and it is what the project's §6.6 already mandates.
- *N < 30* produces false classifications on tiny samples; *N > 50* delays detection unacceptably for a low-frequency verification runner.

**Net effect:** flaky status becomes a reproducible, auditable, data-derived quantity with a human-controlled disposition — satisfying the project's honesty (no auto-promotion), triangulation, and evidence-sufficiency cross-cutting attributes, and closing CHK012–CHK015.

---

## References

- Google Testing Blog — Micco, J. "Flaky Tests at Google and How We Mitigate Them." 2016. https://testing.googleblog.com/2016/05/flaky-tests-at-google-and-how-we.html
- Google Testing Blog — Pirocanac, G. "Test Flakiness — One of the main challenges of automated testing." 2020. https://testing.googleblog.com/2020/12/test-flakiness-one-of-main-challenges.html
- Google Testing Blog — "Where do our flaky tests come from?" 2017. https://testing.googleblog.com/2017/04/where-do-our-flaky-tests-come-from.html
- digia.tech. "Flaky Tests: The Silent Killer of Engineering Velocity." (Google pass-rate tiers, suite-level alarm.) https://www.digia.tech/post/flaky-tests-engineering-velocity
- Microsoft Research — Lam, W. et al. "A Study on the Lifecycle of Flaky Tests." ICSE 2020. https://www.microsoft.com/en-us/research/publication/a-study-on-the-lifecycle-of-flaky-tests/
- TestDino. "Flaky Test Benchmark Report 2026." (Microsoft 2-week SLA; 13% of failures flaky.) https://testdino.com/blog/flaky-test-benchmark
- Meta Engineering — "Probabilistic flakiness: How do you test your tests?" 2020. https://engineering.fb.com/2020/12/10/developer-tools/probabilistic-flakiness
- Trunk — Flaky Tests detection & quarantine docs. https://docs.trunk.io/flaky-tests/detection ; https://docs.trunk.io/flaky-tests/detection/failure-rate-monitor
- Trunk — "Eradicating flaky tests." https://trunk.io/blog/eradicating-flaky-tests
- pytest-rerunfailures — documentation (manual marking model + `--max-suite-reruns`). https://pytest-rerunfailures.readthedocs.io/
- Testriq. "Flaky Tests: Root Causes, Quarantine & Fixes." (N=20–50, 3-in-30, binomial CI.) https://www.testriq.com/blog/post/flaky-tests-root-causes-quarantine-strategy-fixes
- "The Effects of Computational Resources on Flaky Tests" (RAFT). arXiv 2310.12132, FSE 2025. https://arxiv.org/abs/2310.12132
- Parry, O. et al. "A Survey of Flaky Tests." https://o-parry.github.io/papers/2021a.pdf
- ones.com. "How Teams Can Quarantine Flaky Tests without Hiding Real Failures." (human approval, owner separation.) https://ones.com/blog/how-teams-can-quarantine-flaky-tests-without-hiding-real-failures
- QASkills.sh. "Flaky Test Quarantine." https://qaskills.sh/skills/Pramod/flaky-test-quarantine
- minware. "Flaky Test Quarantine." https://www.minware.com/guide/best-practices/flaky-test-quarantine
- Mill Build Tool. "How To Manage Flaky Tests in your CI Workflows." https://mill-build.org/blog/4-flaky-tests.html
- ContextQA. "Flaky Tests in CI/CD: Fix Them Without Retries (2026)." https://contextqa.com/blog/flaky-tests-in-ci-cd-without-retries
- Atlassian. "Taming Test Flakiness." https://www.atlassian.com/blog/how-we-build/taming-test-flakiness-how-we-built-a-scalable-tool-to-detect-and-manage-flaky-tests
- Project-internal: `research/flaky-test-handling-ci.md`; `research/requirements-quality-flaky-retry-verdict-honesty.md`; `research/reliability-flakiness-gaps-best-practices.md`; `specs/010-headless-verification-runner/`.

---

*End of report.*
