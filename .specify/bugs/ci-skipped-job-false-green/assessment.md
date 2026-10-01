# Bug Assessment: Skipped build/test job makes a green CI run imply tests ran when they did not

- **Slug**: ci-skipped-job-false-green
- **Created**: 2026-10-01T08:41:24-04:00
- **Source**: pasted text
- **Verdict**: valid
- **Severity**: medium

## Report (verbatim)

> Enquanto STARDEW_GAME_PATH estiver vazia, Build and Test reporta skipped — workflow verde não significa que testes rodaram.

Note on the variable name: the report spells it `STARDEW_GAME_PATH`; the repository uses `STARDIEW_GAME_PATH` (4 occurrences in `.github/workflows/build_and_analyze.yml`). Treated as a typo in the report, not a second variable.

## Symptom

The `build-test` job is gated by `if: ${{ vars.STARDIEW_GAME_PATH != '' }}`. While that repository variable is unset, GitHub reports the job as **skipped** and the enclosing workflow as **successful**. A run therefore shows a green workflow and a green `Check Formatting` check next to a `Build and Test` check that never executed, so the run is indistinguishable at a glance from a run in which all tests passed. Expected: a CI run's status should never overstate what it actually verified.

## Reproduction

1. Ensure the `STARDIEW_GAME_PATH` repository variable is unset (it is unset today; `gh api repos/lfgranja/stardew-raizes-vivas/actions/variables` returns none).
2. Push any code change to `dev`, or open a pull request against `dev`.
3. Observe the run summary: workflow `Build, Test & Analyze` concludes **green/success**.
4. Observe the checks list: `Check Formatting` is green; `Build and Test` is reported as **skipped**, not as failed.
5. Conclude, from the run status alone, that the build and the 44-file test suite were validated. They were not.

Steps 3–5 are not directly observable yet, because the workflow has not been pushed (`test.md` for `ci-workflow-misplaced` recorded `total_count: 0` and `origin/dev` still holding the old path). The mechanism is therefore grounded in documented GitHub Actions behaviour plus the workflow source, not yet in a live run.

## Suspected Code Paths

- `.github/workflows/build_and_analyze.yml:57-60` — the `build-test` job definition and its job-level `if: ${{ vars.STARDIEW_GAME_PATH != '' }}`. This is the entire mechanism: a skipped job is a success, not a failure.
- `.github/workflows/build_and_analyze.yml:1` — workflow name is `Build, Test & Analyze`, which advertises build, test **and** analysis while only formatting actually executes. The name overclaims relative to the green-run behaviour.
- `.github/workflows/build_and_analyze.yml:32-33` — the `format` job, whose `Check Formatting` check is the only one that genuinely runs. Its green result is what a reader will take as the workflow's verdict.
- `.github/workflows/build_and_analyze.yml:52-56` — the `paths-ignore` filter on both triggers. Interacts badly with this bug: if a check is ever promoted to `required_status_checks`, a docs-only PR is filtered out entirely and then blocks forever waiting for a check that never reports.
- `.specify/memory/constitution.md` — "Quality Gates & Definition of Done", Gate 1 (`dotnet build` and `dotnet test` must succeed) and Gate 3 (end-to-end verification). A green CI run that executed neither build nor test contradicts the project's own definition of done.
- `AGENTS.md:96` — `# Format check (CI gate)`. The repository documentation already describes CI as a format gate, which reinforces that a green run should not be read as build/test coverage.

## Root Cause Hypothesis

Confidence: **high**.

The conditional job was introduced deliberately by the fix for `ci-workflow-misplaced`. Its purpose was to avoid shipping a job that fails unconditionally while the Stardew Valley assembly-provisioning decision was still open — the user had answered "Sei lá" to that question. Gating on a repository variable converts a loud, unavoidable failure into a quiet skip, and GitHub treats a skipped job as non-blocking: the workflow's conclusion is success.

That trade is sound for avoiding permanent red, but it was made at the wrong granularity. The defect is not the `if` condition; it is that **the same workflow asserts scope it does not deliver**. `Build, Test & Analyze` names three activities and delivers one. A reader scanning run status has no way to know that the tests were never attempted, so the workflow's green result silently overstates its own coverage. This recreates, in a softer and more deceptive form, the exact failure that `ci-workflow-misplaced` reported: a quality gate that is not actually running while appearing to exist. The misplaced file was at least visibly absent (`total_count: 0`); a green run with a skipped sibling invites more trust than it deserves.

There is also a compounding factor: with `paths-ignore` in place, the same required-check promotion that would activate this bug also risks hanging docs-only pull requests. Both are downstream of the same design choice — a filter plus a conditional job inside one workflow whose name promises more than it runs.

## Proposed Remediation

**Preferred**: split into two workflows whose names encode exactly what each one does, and remove the conditional job from the always-running one. This makes the green signal truthful by construction rather than by decoration.

- `.github/workflows/code-style.yml` — new workflow, name `Code Style (format only)`. Contains the existing `format` job, unchanged: checkout, `setup-dotnet`, restore, `dotnet format --verify-no-changes`. It has no game-assembly dependency and no conditional jobs, so it is unconditionally green and unambiguously means "formatting passed".
- `.github/workflows/build_and_test.yml` — the existing `build-test` job, moved out. Name `Build and Test`. It carries the same `if: ${{ vars.STARDIEW_GAME_PATH != '' }}` gate, plus the `GamePath` env plumbing and the preflight step, so it stays dormant until assemblies are provisioned — but now its dormancy is visible as *a workflow that does not exist yet*, which is honest and cannot be misread as a passing test run.
- Delete `.github/workflows/build_and_analyze.yml` once its contents are redistributed. Also resolves the stale "Analyze" in the name, since SonarCloud was already removed.
- Do **not** promote either check to `required_status_checks` until `Build and Test` has produced at least one genuinely green run. While it is dormant, promoting `Code Style (format only)` alone would legally satisfy branch protection while running zero tests — the same false green, one level up.
- Decide the `paths-ignore` question at the same time. With the build/test job split into its own workflow, the cost model changes: docs-only PRs no longer need the code workflow at all, so `paths-ignore` on `code-style.yml` is optional rather than necessary. Leaving it off is simpler and avoids the required-check hang entirely.

The net effect: a green run means precisely "formatting passed, and nothing else was claimed." There is no skipped job to misread.

**Alternatives**:
- *Always-run guard job that fails when the variable is unset.* Honest red; forces the assembly decision and can never lie. Rejected because it means permanently red CI until that decision lands, and a permanently red gate is one people learn to ignore — which degrades the gate it was meant to protect. Worth reconsidering once the assembly source is settled.
- *Keep one workflow, rename the job to `Build & Test (opt-in)` and add a `::warning::` annotation.* Cheapest change, and the comment already in the file explains the dormancy. Still leaves a workflow named `Build, Test & Analyze` going green while running only a format check, so the core defect survives; this only makes the dormancy discoverable to someone who reads the file.

**Files likely to change**:
- `.github/workflows/code-style.yml` — new workflow containing the format gate
- `.github/workflows/build_and_test.yml` — new workflow containing the build/test job
- `.github/workflows/build_and_analyze.yml` — deleted after redistribution (the only deletion this assessment authorises)
- Repository settings (not a file) — `STARDIEW_GAME_PATH` variable, branch protection required checks

**Tests to add or update**:
- No unit test applies; the regression surface is CI configuration. The project convention of one `{Subject}Tests.cs` file per subject does not have an analogue here.
- Instead, assert mechanically after the change that: no workflow in `.github/workflows/` combines a name advertising build/test with a job set that only formats; and no job in the always-running workflow is conditional on a repository variable. Both are checkable with a short script or `actionlint`, and are worth running once rather than encoding as a permanent test.
- Record the "do not promote to `required_status_checks` until a real green run exists" rule in `AGENTS.md`, which already documents CI as a format gate. Without it, the rule lives only in this assessment and will be forgotten.

## Risks & Considerations

- **The false green is latent, not active.** `required_status_checks.contexts` is currently `[]` on both `dev` and `main`, and the workflow has never run. Nothing is being falsely validated *today*. Severity is driven by what happens on promotion, which is exactly the moment a reader is most likely to trust the green run — which is why the escalation condition is called out below.
- **Escalates to high** if `Build & Test` or the workflow-level check is added to `required_status_checks` while still dormant. From that moment, every pull request merges while presenting a green test check that never ran, and the failure is invisible without deliberate inspection.
- **Splitting workflows costs a second workflow entry** in the Actions tab and duplicated trigger/`paths-ignore` blocks. Duplication is the price of names that are individually truthful; keep the two trigger blocks identical so they cannot drift.
- **A dormant workflow is invisible, which cuts both ways.** It is honest about absence, but a team member may not notice that `Build and Test` is missing at all. Mitigate by keeping the intended workflow file committed but permanently skipped? No — that reintroduces this bug. Instead, note the pending workflow in `AGENTS.md` so its absence is documented somewhere people read.
- **The `paths-ignore` / required-check hang persists independently.** Even after the split, adding `paths-ignore` to `code-style.yml` while also requiring that check will hang docs-only pull requests. Removing the filter is the only clean resolution, and it is cheap because the expensive part no longer shares a workflow with docs.
- **`build-test` still has never executed even once.** Splitting the workflow changes no evidence about whether `dotnet build` and `dotnet test` pass on a provisioned `GamePath`; that remains unverified, as recorded in `ci-workflow-misplaced/test.md`.
- **Two source bugs in one lineage.** This bug was introduced by the fix for `ci-workflow-misplaced`, and the underlying assembly-provisioning blocker is still unresolved. Splitting workflows removes the misleading signal but does not make tests runnable.

## Open Questions

- [NEEDS CLARIFICATION: Should `code-style.yml` keep a `paths-ignore` filter, or run on every change? Removing it avoids the required-check hang but spends a short format run on documentation-only pull requests.]
- [NEEDS CLARIFICATION: Is `Build and Test` expected to become a required check eventually? The answer determines whether dormancy must be resolved before or after promotion.]
- [NEEDS CLARIFICATION: Does the project want a genuinely failing guard instead, accepting permanently red CI until the assembly decision is made? Recorded as the rejected alternative; worth an explicit decision rather than a silent omission.]
- [NEEDS CLARIFICATION: Should the dormant `Build and Test` workflow exist as a tracked-but-skipped file for discoverability, or be absent until the assemblies are provisioned? Tracked-and-skipped reintroduces this bug; absent is honest but easy to overlook.]
- [NEEDS CLARIFICATION: Should `AGENTS.md` be updated to record the pending workflow and the promotion rule, and does that belong in this fix or a documentation follow-up?]