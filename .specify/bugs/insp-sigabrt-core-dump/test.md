# Bug Verification: insp SIGABRT core dump

- **Slug**: insp-sigabrt-core-dump
- **Tested**: 2026-11-02T01:50:00-04:00
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial

## Summary

The fix was recorded as `not-applied` (no repository code changed), because the crashing binary is an external, ephemeral diagnostic tool (`/tmp/opencode/insp`), not part of `stardew-raizes-vivas`. The current (rewritten) version of that tool runs successfully (exit 0), so the original SIGABRT could not be re-tested against the crashing version (it no longer exists). No repo source files were modified by this fix, so no regression in the mod is expected or observed.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) | `DOTNET_ROOT=/home/luis/.local/share/mise/dotnet-root dotnet run` in `/tmp/opencode/insp` | pass | Exit 0; printed SMAPI.Toolkit.CoreInterfaces version 4.2.1.0. No SIGABRT. |
| Reproduction of original crashing version | Run prior reflection-inspector build | not-run | That source version no longer exists on disk (rewritten); cannot reproduce the original abort. |
| New / updated tests | N/A | not-run | Assessment listed "None applicable"; no tests added. |
| Regression suite (repo) | `dotnet test` | skipped | No code changed in this repo by the fix; running the full suite is unrelated to this bug and the workspace has concurrent in-progress edits. |
| Lint / type-check | `dotnet format --verify-no-changes` | skipped | No code changed in this repo by the fix. |

## Output Excerpts

```
LivingRoots asm version: 1.0.0.0
REF: StardewModdingAPI 4.5.2.0
REF: SMAPI.Toolkit.CoreInterfaces 4.5.2.0
EXIT: 0
```

## Residual Risks

- The original crashing version of `insp` no longer exists, so the SIGABRT was not actually re-exercised; the managed exception type (JIT frames #8/#9) remains unknown.
- The second core dump (PID 1821087) from the same binary was not analyzed.
- If a future diagnostic tool is added to this repo, its reflection calls must be null-checked and wrapped in try/catch to avoid unhandled exceptions escalating to `Environment.FailFast`.

## Recommendation

Hold — reproduction inconclusive because the crashing version of the external tool no longer exists and no repo code was changed to verify. No reopen needed: the bug is external to `stardew-raizes-vivas` and the current tool runs cleanly. If the SIGABRT recurs, capture managed diagnostics (SOS/dotnet-dump) and re-run `/speckit.bug.assess`.