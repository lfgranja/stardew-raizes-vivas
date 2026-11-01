# Bug Assessment: CodeQL code scanning failure (job 124530776150)

- **Slug**: codeql-code-scanning-failure
- **Created**: 2026-10-31T18:00:00-04:00
- **Source**: https://github.com/lfgranja/stardew-raizes-vivas/actions/runs/41198874549/job/124530776150
- **Verdict**: likely valid, needs reproduction
- **Severity**: medium

## Report (verbatim or summarized)

GitHub Actions run 41198874549 (Quality Gate) failed. Job `Quality Gate / codeql` failed during CodeQL analysis/upload. The workflow uses `github/codeql-action/init@v4` with `build-mode: none`, `github/codeql-action/analyze@v4` with `category: "csharp"`, and earlier removed the redundant `upload-sarif` step. The job shows CodeQL initialized and built/checked out repo, but the final status is failure (gate depends on it).

## Symptom

Quality Gate fails due to `codeql` job failure; `gate` job reports `static concluded failure` / `build-analysis concluded failure` depending on run, but this specific run shows codeql failing in context.

## Reproduction

1. Trigger Quality Gate on PR/push (codeql runs if event is schedule/workflow_dispatch/push; for PRs in current config codeql has `if: github.event_name == 'schedule' || github.event_name == 'workflow_dispatch' || github.event_name == 'push'` so it may not run on PRs). 
2. Observe job failure.
[NEEDS CLARIFICATION: exact failure reason from logs - need to see analyze/upload step errors]

## Suspected Code Paths

- `.github/workflows/quality-gate.yml:133-173` — codeql job configuration
- `.github/workflows/quality-gate.yml:42-56` — semgrep uploads SARIF via codeql-action/upload-sarif (separate from codeql job)

## Root Cause Hypothesis

Possible issues: (1) SARIF upload conflicts between semgrep upload and codeql upload (category/name conflicts), (2) codeql action version incompatibility, (3) permissions missing (security-events: write present), (4) build-mode none with C# needing specific setup. Confidence: medium.

## Proposed Remediation

**Preferred**: Check CodeQL action logs for specific error. Common fixes: ensure unique SARIF categories, avoid uploading duplicate SARIFs, use correct permissions, or disable codeql on PRs if not needed. Also verify `upload-sarif` isn't called twice.

**Files likely to change**:
- `.github/workflows/quality-gate.yml`

**Tests to add or update**:
- None (CI config)

## Risks & Considerations

- Code scanning may be required by branch protection

## Open Questions

- [NEEDS CLARIFICATION: actual error message from codeql analyze/upload step]
- [NEEDS CLARIFICATION: does codeql run on PRs in this repo?]
