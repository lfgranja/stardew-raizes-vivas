# Bug Assessment: CodeQL annotations step fails with IsADirectoryError

- **Slug**: codeql-sarif-directory-error
- **Created**: 2026-11-01T03:30:00Z
- **Source**: https://github.com/lfgranja/stardew-raizes-vivas/actions/runs/41269120163/job/124738095457
- **Verdict**: valid
- **Severity**: medium

## Report

CI run `41269120163`, job `124738095457` ("Quality Gate / codeql") failed with:
`IsADirectoryError: [Errno 21] Is a directory: 'codeql-results.sarif'` at the "CodeQL annotations" step's inline Python script (`path = "codeql-results.sarif"; ... with open(path)`). The CodeQL action emits `CODEQL_ACTION_SARIF_RESULTS_OUTPUT_DIR: codeql-results.sarif` and writes `codeql-results.sarif/csharp.sarif`, so the path is a directory, not a file.

## Suspected Code Paths

- `.github/workflows/quality-gate.yml` — `codeql` job, "CodeQL annotations" step (inline Python).

## Root Cause Hypothesis

The annotation script assumes `codeql-results.sarif` is a single SARIF file and calls `open()` on it. Since CodeQL v4 writes a directory of per-language SARIF files, `open()` raises `IsADirectoryError`. Confidence: high.

## Proposed Remediation

Make the script iterate over `*.sarif` files inside the directory (or a single file if that's what exists), loading each and emitting `::error` annotations. Keep the early-exit when nothing exists.

## Risks

- None functional; only annotation output changes.
