# Bug Assessment: protected-paths-tracked-files

- **Slug**: protected-paths-script-missing (reused; assessment overwritten — previous content described a missing script; this assessment describes a different symptom: script exists but fails)
- **Created**: 2026-10-18
- **Source**: pasted text (user input describing `Check Protected Paths` failure with 10 errors from `scripts/check-protected-paths.sh`) + local reproduction (`bash scripts/check-protected-paths.sh` executed in repo root)
- **Verdict**: valid
- **Severity**: high (public repo; proprietary Stardew Valley / SMAPI assemblies committed; `.gitignore` enforcement broken; allows IP leakage and proprietary binary redistribution)

## Report (verbatim or summarized)

Pasted input (translated from user message):
> "O action 'Check Protected Paths' falhou (10 erros) porque arquivos proprietários de Stardew Valley / SMAPI estão rastreados no repo, violando .gitignore."

Script output reproduced locally (`bash scripts/check-protected-paths.sh`):
```
Check 1 (integrity):
  FAIL  .gamepath/Stardew Valley.dll          expected ignored, git says tracked
  FAIL  .gamepath/MonoGame.Framework.dll        expected ignored, git says tracked
  FAIL  .gamepath/StardewValley.GameData.dll    expected ignored, git says tracked
  FAIL  .gamepath/smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll  expected ignored, git says tracked

Check 2 (blast radius — tracked assemblies):
  FAIL  Stardew Valley.dll IS TRACKED
  FAIL  StardewModdingAPI.dll IS TRACKED
  FAIL  StardewValley.GameData.dll IS TRACKED
  FAIL  MonoGame.Framework.dll IS TRACKED
  FAIL  xTile.dll IS TRACKED
  FAIL  SMAPI.Toolkit.CoreInterfaces.dll IS TRACKED

protected-path preflight: 10 failure(s).
```

No URL fetched; no embedded instructions or secrets in pasted text.

## Symptom

The `Protected Paths` CI workflow (`.github/workflows/protected-paths.yml`) executes `scripts/check-protected-paths.sh`. The script exists and runs, but exits with code 1 and 10 failures: 4 integrity failures (`.gamepath/` DLLs not ignored) and 6 blast-radius failures (prohibited assembly basenames tracked in `git ls-files`). Expected behavior: `.gamepath/` exclusions active and zero tracked proprietary assemblies.

## Reproduction

1. Run locally: `bash scripts/check-protected-paths.sh`
2. Observe `FAIL` for `.gamepath/*.dll` (expected `ignored`, actual `tracked`)
3. Observe `FAIL` for each `BANNED_TRACKED` basename (`Stardew Valley.dll`, `StardewModdingAPI.dll`, etc.)
4. Confirm `.gitignore` contains `.gamepath/` (line 481) but `git check-ignore` still reports `tracked` for those DLLs.

Verified locally; fully reproducible.

## Suspected Code Paths

- `scripts/check-protected-paths.sh:52-68` (Check 1 — `git check-ignore`) — verifies `.gitignore` exclusions are active. Fails when tracked files bypass `.gitignore`.
- `scripts/check-protected-paths.sh:71-86` (Check 2 — `git ls-files` + basename grep) — detects already-committed prohibited assemblies.
- `.gitignore:469-474` (`Secrets` block) and `:481` (`.gamepath/`) — rules that should exclude proprietary files.
- `.github/workflows/protected-paths.yml:33` — the CI gate that calls the script.

Root cause is not in the script logic; the script correctly detects the violation. The violation is that files matching `.gamepath/` and the `BANNED_TRACKED` list are present in the git index/tree despite `.gitignore` rules.

## Root Cause Hypothesis

**Confidence: high.**

The `.gitignore` contains `.gamepath/` (line 481) and the `Secrets` block (lines 469-474). The `PROTECTED` array in `scripts/check-protected-paths.sh` expects `.gamepath/*.dll` to be `ignored`. However, `git check-ignore` reports `tracked` for those paths, which only happens when a file is already tracked by git (`.gitignore` does not affect tracked files — `git rm --cached` is required). Additionally, `git ls-files | sed 's|.*/||'` shows the basenames of those DLLs are tracked (likely committed in `.gamepath/` and possibly at root or nested paths). The PR #282 (`fix/protected-paths-script`) appears to have attempted a script-level fix, but the script was never broken; the repo state (tracked proprietary files) is the actual failure cause.

## Proposed Remediation

**Preferred**:
1. Untrack the proprietary assemblies: `git rm --cached -- .gamepath/Stardew\ Valley.dll .gamepath/MonoGame.Framework.dll .gamepath/StardewValley.GameData.dll .gamepath/smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll` (and any nested `.gamepath/` files). Also remove `StardewModdingAPI.dll`, `xTile.dll`, and any other tracked `BANNED_TRACKED` basenames from the index (check with `git ls-files`).
2. Verify `.gitignore` covers `.gamepath/` (it does at line 481). If any `BANNED_TRACKED` file exists at root or elsewhere not under `.gamepath/`, ensure a `.gitignore` rule excludes it (e.g., add `.gamepath/` covers nested paths; add explicit exclusions for any stray files).
3. After untracking, `git commit` the `.gitignore` / index cleanup (not the DLLs — the DLLs must not be re-added). Re-run `bash scripts/check-protected-paths.sh` locally; it should report all `ok`.
4. Push to `dev`; the `Protected Paths` workflow should turn green.

**Alternative**: If the files must remain in the working tree locally (e.g., `builddeps` provides them during build), ensure `.gamepath/` is fully covered by `.gitignore` and never committed. The script should then pass because `git check-ignore` will return `ignored` and `git ls-files` will show no hits.

**Files likely to change**:
- `scripts/check-protected-paths.sh` — no change needed (script is correct).
- `.gitignore` — may need additional exclusions if any `BANNED_TRACKED` files exist outside `.gamepath/`.
- Index / commit — remove tracked DLLs (`git rm --cached`), not source file changes.

**Tests to add or update**:
- After fix, open a test PR to `dev` to confirm `Protected Paths` is green.
- Consider adding a `lefthook` or pre-commit hook that runs `scripts/check-protected-paths.sh` locally (already referenced in `.specify/bugs/protected-paths-script-missing/assessment.md` as Gate 1).
- Optionally add an automated check in CI that fails if any `BANNED_TRACKED` basename appears in `git ls-files`.

## Risks & Considerations

- Removing tracked files from index (`git rm --cached`) does NOT delete the working-tree files; developers with `.gamepath/` locally will retain them if `.gitignore` is active. Confirm this behavior before committing the cleanup.
- The repo is public. If any of these DLLs contain proprietary code or were accidentally committed, removing them from future commits stops further exposure, but historical commits may still contain them. If full removal from git history is required, use `git filter-repo` or BFG (out of scope for this assessment, but should be flagged to maintainers).
- `.gamepath/` links to the private `builddeps` repo (`gamepath-v1`). Committing these files violates redistribution terms (`README` of `builddeps`). The `Protected Paths` script exists specifically to prevent this.
- The previous `assessment.md` in this slug described a missing script; this assessment supersedes it because the script now exists but fails for a different root cause (tracked files, not missing file).

## Open Questions

- [NEEDS CLARIFICATION: Were `.gamepath/*.dll` files committed intentionally (e.g., for local build reference) or accidentally? Confirm before `git rm --cached` to avoid deleting needed build artifacts that should be managed by `.gitignore` instead.]
- [NEEDS CLARIFICATION: Should the proprietary DLLs also be removed from git history (`git filter-repo`) given the public repo status and redistribution restriction?]
- [NEEDS CLARIFICATION: Does `.specify/bugs/gamepath-not-gitignored/assessment.md` (referenced in `.gitignore` line 483) contain additional context about the `.gamepath/` mechanism? Check that assessment before finalizing the fix.]

## Additional CI Observations (Warnings)

- `LivingRoots.Tests/Invariants/SoilHealthPersistenceInvariantsTests.cs` (commit `b361ab3`, not present in local `dev`) introduces 11 `CS8602` (dereference of possibly null reference) warnings at lines 69, 70, 91, 93, 120, 121, 123, 151, 152, 154. These occur after `Assert.NotNull()` calls on `GetCapturedState()`; the static analyzer does not recognize the `Assert` as a null-suppressor. These are cosmetic (analysis noise), not functional bugs, and are separate from the `exit code 1` failure.

## Next Step

Run `/speckit.bug.fix slug=protected-paths-script-missing` after confirming `git rm --cached` targets. The fix is a repository-state cleanup (untrack DLLs), not a code change to the script.
