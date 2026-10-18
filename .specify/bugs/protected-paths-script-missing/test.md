# Bug Verification: protected-paths-script-missing

- **Slug**: protected-paths-script-missing
- **Tested**: 2026-10-18
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The original symptom (`Protected Paths` workflow fails with "file not found" for `scripts/check-protected-paths.sh`) no longer reproduces. The missing script was recovered from git history (`commit 4d5d385`) and is now executable at `scripts/check-protected-paths.sh`. The script runs correctly (outputs structured failure list for tracked `.gamepath/` DLLs, exits 1 — the gate is active). The residual 10 tracked protected-file failures are a pre-existing data issue (tracked `.gamepath/` assemblies), not a regression from this fix.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) — script exists | `ls -la scripts/check-protected-paths.sh` | pass | File present, executable (`-rwxr-xr-x`), `#!/usr/bin/env bash` |
| Reproduction — script runs (not file-not-found) | `bash -c 'bash scripts/check-protected-paths.sh; echo $?'` | pass | Exits 1 with structured failure message (not "No such file or directory") |
| Check 1 (`.gitignore` integrity) | script output lines 52-68 | pass | All `.env`, `.env.*`, `.pem`, `secrets.json`, `credentials.json`, `.env.example` verified; `.gamepath/` entries detected as tracked (expected) |
| Check 2 (banned tracked files) | script output after line 70 | pass | Detects 6 proprietary DLLs (`Stardew Valley.dll`, `MonoGame.Framework.dll`, etc.) as tracked |
| Workflow reference intact | `grep -n 'check-protected-paths' .github/workflows/protected-paths.yml` | pass | Line 33 references `./scripts/check-protected-paths.sh` |
| `.gitignore` Secrets block intact | `grep -n -A2 'Secrets.' .gitignore` | pass | Lines 461-463 confirm block present |
| New / updated tests | assessment called for minimal local test; fix applied script recreation (no separate test file added) | not-run | Assessment recommended a test PR with dummy `.env`; out of scope for this verification |

## Output Excerpts

Direct script execution (post-fix):
```
==> Check 1: protected .gitignore entries are in force
    ok    .env                                                       ignored
    ok    .env.local                                                 ignored
    ...
    FAIL  .gamepath/Stardew Valley.dll                               expected ignored, git says tracked
==> Check 2: no prohibited assembly is tracked
    FAIL  Stardew Valley.dll IS TRACKED (run: git rm --cached -- Stardew Valley.dll)
    ...
protected-path preflight: 10 failure(s).
Direct exit code: 1
```

This confirms the gate is operational: it no longer crashes with `file not found`; it reports the real protected-file leakage (`.gamepath/` tracked DLLs) that the `.gitignore` is designed to prevent.

## Residual Risks

- The script detects 10 tracked protected items (`.gamepath/` proprietary DLLs + assemblies). These are a separate pre-existing issue (`.gamepath-not-gitignored`) that requires untracking (`git rm --cached`) or cleaning those files. The fix for `protected-paths-script-missing` does not address them — it only restores the gate.
- If the `.gamepath/` files remain tracked, the `Protected Paths` workflow will stay red. The assessment's `Next Step` calls this out explicitly: the gate must turn green after `.gamepath/` cleanup.
- The script was recovered from a merge commit (`4d5d385`). If the original file content diverges from the intended design (e.g., `.specify/` exclusions from `assessment.md` open questions), a future update may be needed.

## Recommendation

**Verified (for the fix scope)** — the missing script is restored and the `Protected Paths` workflow will no longer fail with `No such file or directory`. The gate detects real leaks correctly.

Not fully green in CI yet, because the tracked `.gamepath/` proprietary DLLs (pre-existing data issue) still trigger Check 2 failures. That requires a separate cleanup step (`git rm --cached .gamepath/`) or a separate bug fix (`.gamepath-not-gitignored`). Once `.gamepath/` tracked files are cleaned, the workflow should turn green.

Suggested next step: address `.gamepath/` tracked files (either manually untrack or open a separate assessment), then verify the full workflow turns green with a `dev` push.
