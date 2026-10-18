# Bug Fix: protected-paths-script-missing

- **Slug**: protected-paths-script-missing
- **Fixed**: 2026-10-18
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

Recreated `scripts/check-protected-paths.sh` (missing, referenced by `.github/workflows/protected-paths.yml:33` and `lefthook.yml`). The file was recovered from git history (`commit 4d5d385` — merge of the original security/gate commit). The script enforces the `.gitignore` Secrets block (`.env`, `.env.*`, `*.pem`, `secrets.json`, `credentials.json`, `.env.example`) and verifies `.gamepath/` exclusion plus banned proprietary assemblies in tracked files.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `scripts/check-protected-paths.sh` | added (recovered from `4d5d385`) | Executable bash script; runs Check 1 (`.gitignore` integrity) and Check 2 (banned tracked files) |

## Diff Highlights

The full file is 92 lines (recovered verbatim from the original commit). Key sections:
- Lines 18-19: `set -euo pipefail` + `cd $(git rev-parse --show-toplevel)`
- Lines 25-48: `PROTECTED` array mapping patterns to expected outcomes (`ignored`/`tracked`)
- Lines 41-48: `BANNED_TRACKED` array of proprietary assembly filenames
- Lines 52-68: Check 1 loop using `git check-ignore -q`
- Lines 70+: Check 2 loop using `git ls-files` against `BANNED_TRACKED`

## Tests Added or Updated

- Local verification: `bash scripts/check-protected-paths.sh` runs successfully (exit 1, reporting 10 failures — all expected: tracked `.gamepath/` DLLs that must be untracked). The script behaves exactly as the workflow requires.
- The workflow `.github/workflows/protected-paths.yml` now has its target script present; it will run properly on the next `dev` push / PR.

## Local Verification

- `ls -la scripts/check-protected-paths.sh` → `-rwxr-xr-x`, `#!/usr/bin/env bash` present
- `bash scripts/check-protected-paths.sh` → outputs structured failure list (10 tracked protected items), exits 1. This confirms the gate is active and catching real protected-file leakage (`.gamepath/` assemblies are tracked but must not be).
- `git log --all --full-history -- scripts/check-protected-paths.sh` confirms recovery from original commit (`4d5d385`, message: "ci(security): track check-protected-paths.sh, the gate CI and lefthook both invoke").

## Deviations from Assessment

None. The preferred remediation (create/recreate the script) was applied exactly. The script runs as designed. The open clarification questions from `assessment.md` remain open but do not block the fix:
- Whether `.gamepath/` tracked files should also be untracked (separate issue — the script correctly detects them).
- Whether `.specify/` exclusions need enforcement (out of scope for this fix).
- Executable/portability: script uses `#!/usr/bin/env bash` and `set -euo pipefail`, compatible with `ubuntu-latest`.

## Follow-ups

- The `.gamepath/` proprietary DLLs (`Stardew Valley.dll`, `MonoGame.Framework.dll`, etc.) and `.gamepath/Mods/LivingRoots/` assets are currently tracked but excluded by `.gitignore:481-483`. A separate fix (possibly `/speckit.bug.fix` for `.gamepath-not-gitignored` or a manual `git rm --cached`) is needed to resolve the tracked-file leaks the script now correctly reports.
- After the `.gamepath/` tracked files are cleaned, the `Protected Paths` workflow should turn green on `dev` PRs.
- Consider adding `chmod +x scripts/check-protected-paths.sh` enforcement in CI or pre-commit if the file is ever regenerated.
