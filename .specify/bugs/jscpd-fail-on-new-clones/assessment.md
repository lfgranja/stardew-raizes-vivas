# Bug Assessment: jscpd fails with `--fail-on-new-clones requires --baseline`

- **Slug**: jscpd-fail-on-new-clones
- **Created**: 2026-11-01T00:55:00Z
- **Source**: https://github.com/lfgranja/stardew-raizes-vivas/actions/runs/41258000330/job/124705180377
- **Verdict**: valid
- **Severity**: high

## Report (verbatim or summarized)

**URL trust policy**

- Verbatim URL: `https://github.com/lfgranja/stardew-raizes-vivas/actions/runs/41258000330/job/124705180377`
- Host: `github.com`
- Policy branch: **allowlisted** (fetched with the GitHub CLI; no untrusted page content was obeyed).

**Run context** — run `41258000330`, pull request #544 (`story/style-fix-imports`), job `Quality Gate / static` (job id `124705180377`). The `jscpd` step failed after 7 s:

```
Run npx --yes jscpd@5.4.2 LivingRoots/ --format csharp --min-lines 5 --min-tokens 50 --reporters json --output jscpd-report --silent --fail-on-new-clones
Error: --fail-on-new-clones requires --baseline <file>, --baseline-from-ref <ref> or --changed
Error: Process completed with exit code 1.
```

Job step summary (verbatim from `gh run view`):

```
✓ Semgrep SARIF
✓ Semgrep (file and line)
✓ Upload Semgrep SARIF
✓ Install Gitleaks
✓ Gitleaks (per-PR diff)
- Gitleaks (full history)
X jscpd
- Duplication gate
- Upload jscpd report
```

The same run's `build-analysis` job also failed the coverage gate (`LivingRoots.Services.TimeProvider 0.0%`, `MulchApplicationService 6.7%`, `ModController 65.7%`, `ReservedNameHandler 78.0%`, … `< 79.5%`). That is a separate front in the same run and is already addressed by PR #543; it is recorded here for context only.

## Symptom

Every pull request whose branch is based on `dev` runs `Quality Gate / static`, the `jscpd` step invokes `--fail-on-new-clones` without any of the inputs that flag requires, and jscpd exits 1 before scanning. The job fails, the `Duplication gate` step is skipped, and `Quality Gate / gate` reports `static concluded failure`. Expected: jscpd produces `jscpd-report/jscpd-report.json` and the duplication gate evaluates it against `.jscpd-baseline.json`.

## Reproduction

1. Check out a commit that contains `.github/workflows/quality-gate.yml` from `origin/dev` (for example `origin/dev` itself, or any PR branch based on `e819a345`).
2. Run the exact workflow command locally:
   ```bash
   npx --yes jscpd@5.4.2 LivingRoots/ --format csharp --min-lines 5 --min-tokens 50 \
     --reporters json --output /tmp/jscpd-repro --silent --fail-on-new-clones
   ```
3. Observe `Error: --fail-on-new-clones requires --baseline <file>, --baseline-from-ref <ref> or --changed` and exit code 1.
4. In CI, open the run linked above; the `jscpd` step fails and the following `Duplication gate` step is skipped.

Verified locally on 2026-11-01 with jscpd 5.4.2 — the local error matches the CI error verbatim.

## Suspected Code Paths

- `.github/workflows/quality-gate.yml:79` (on `origin/dev`) — `run: npx --yes jscpd@5.4.2 … --fail-on-new-clones` passes none of `--baseline`, `--baseline-from-ref`, or `--changed`.
- `.github/workflows/quality-gate.yml:82` (on `origin/dev`) — `python -m quality_gate duplication --report jscpd-report/jscpd-report.json --baseline .jscpd-baseline.json`; never reached in CI because the previous step fails.
- `.jscpd-baseline.json` (repo root) — custom baseline consumed by the Python gate (`percentage`, `duplicates`, `lines`), **not** a jscpd clone-fingerprint baseline. jscpd rejects it: `Error: baseline file .jscpd-baseline.json: missing field 'version' at line 1 column 166` (verified locally).
- `scripts/quality-gate/quality_gate/duplication.py` — the authoritative gate; compares the aggregate duplication percentage against the baseline ceiling and only lists clone coordinates as diagnostics. It does **not** enforce "0 new clones".
- `specs/011-local-oss-quality-gate/spec.md:139` (Q20) — decision record requiring `--fail-on-new-clones` "(0 allowed)" alongside the aggregate ceiling. The workflow attempted to honor Q20 but omitted the flag's required inputs.
- `scripts/quality-gate/tests/test_workflow_build_analysis.py:229` — pins the jscpd command (`npx --yes jscpd@5.4.2`, `--format csharp`, `--min-lines 5`, `--min-tokens 50`, `--reporters json`, `--output jscpd-report`) but does not assert any relationship between `--fail-on-new-clones` and `--baseline`/`--baseline-from-ref`/`--changed`, which is why the broken combination passed the test suite.

## Root Cause Hypothesis

Confidence: **high**. jscpd 5.4.2 treats `--fail-on-new-clones` as dependent on a comparison source and validates the arguments before scanning; the workflow supplies none, so the process exits 1 during argument validation. The step was presumably added to satisfy spec decision Q20 without a comparison baseline, and no test asserts the flag's required companions. Secondary hazard: the repository already has a file named `.jscpd-baseline.json` in a format jscpd cannot parse, and jscpd's `--changed` mode defaults to writing `.jscpd-baseline.json` at the repo root — so the obvious "just add `--baseline .jscpd-baseline.json`" repair fails, and `--changed` would silently clobber the Python gate's baseline.

## Proposed Remediation

**Preferred (short term, already implemented on PR #543 and PR #539)**: remove `--fail-on-new-clones` from the `jscpd` invocation and keep the Python duplication gate as the single authority:

```yaml
- name: jscpd
  run: npx --yes jscpd@5.4.2 LivingRoots/ --format csharp --min-lines 5 --min-tokens 50 --reporters json --output jscpd-report --silent

- name: Duplication gate
  run: python -m quality_gate duplication --report jscpd-report/jscpd-report.json --baseline .jscpd-baseline.json
```

This is deterministic, requires no new committed state, and preserves the aggregate percentage ceiling. It does deviate from Q20's "0 new clones" requirement, so amend `specs/011-local-oss-quality-gate/spec.md` (Q20), `plan.md`, and `tasks.md` (T042/T088) to state that the Python gate owns the duplication check and that new-clone detection is deferred.

**Preferred if Q20 must be honored (follow-up, larger)**: commit a jscpd **fingerprint** baseline at a path that cannot collide with the Python gate, e.g. `.jscpd-clones-baseline.json`, generated once with:

```bash
npx --yes jscpd@5.4.2 LivingRoots/ --format csharp --min-lines 5 --min-tokens 50 \
  --reporters json --output jscpd-report --silent \
  --baseline .jscpd-clones-baseline.json --update-baseline
```

then run CI with `--baseline .jscpd-clones-baseline.json --fail-on-new-clones`. Verified locally: the generated file has the shape `{"version": 1, "fingerprints": {"<hash>": 1, …}}`, and a second run with `--fail-on-new-clones` exits 0 when the tree is unchanged. Add a documented ratchet step (regenerate the baseline in the same PR whenever a legitimate clone appears or disappears), and rename or isolate the Python baseline so jscpd's `--changed` default path can never overwrite it.

**Alternative**: add `fetch-depth: 0` to `actions/checkout` and use `--baseline-from-ref origin/dev` (or the PR base ref) with `--fail-on-new-clones`. No committed baseline to maintain, but it increases checkout cost, needs explicit ref resolution per event type (`pull_request` vs `push` to `dev`/`main`), and is ambiguous on a push to `dev` where there is no "base" revision.

**Files likely to change**:
- `.github/workflows/quality-gate.yml`
- `.jscpd-clones-baseline.json` (only for the fingerprint-baseline option)
- `specs/011-local-oss-quality-gate/spec.md`, `plan.md`, `tasks.md` (only if Q20 is amended or redefined)
- `scripts/quality-gate/tests/test_workflow_build_analysis.py`

**Tests to add or update**:
- Extend `test_jscpd_is_pinned_csharp_only_and_gated` to assert the invariant directly: if the command contains `--fail-on-new-clones`, it must also contain one of `--baseline`, `--baseline-from-ref`, or `--changed`; and the baseline path used by jscpd must not be `.jscpd-baseline.json` (the Python gate's file).
- CI verification: re-run `Quality Gate / static` and confirm `jscpd` succeeds, `Duplication gate` executes, and the artifact uploads.
- If the fingerprint option is chosen: a local test that introduces a deliberate clone into a scratch directory and confirms exit 1, plus an unchanged-tree run that confirms exit 0.

## Risks & Considerations

- Dropping `--fail-on-new-clones` is a **documented weakening** of Q20. The repo's own weakening procedure (Q19) requires justification, approval, expiry, and an audit-log entry — record it rather than silently diverging.
- `.jscpd-baseline.json` is a name collision waiting to happen: jscpd's `--changed` mode writes that exact path at the repo root, which would replace the Python gate's baseline with an incompatible format and break the gate.
- `--baseline-from-ref` requires the ref to exist in the local clone; the workflow currently uses `actions/checkout@v7` with no `fetch-depth`, so `origin/dev` is not present.
- The failure is **fail-closed** (the job fails), so no PR can be falsely green; the cost is that every PR is blocked and the duplication gate never executes.
- The same run also fails the coverage gate; that front is independent and covered by PR #543. All current story PRs (#540–#544) are based on `e819a345`, so they all inherit this jscpd failure until the fix lands on `dev`.
- Removing the flag does not change what the gate enforces at the aggregate level, but it does reduce detection granularity from "any new clone" to "aggregate percentage above ceiling".

## Open Questions

- Should Q20 be amended (aggregate ceiling only) or honored with a committed fingerprint baseline? This determines whether the already-open fix on PR #543 is final or transitional.
- [NEEDS CLARIFICATION: after the fix lands on `dev`, should the open story PRs be rebased onto `dev` (so their CI runs the corrected workflow), or should the workflow fix be merged first and the PRs re-run?]
