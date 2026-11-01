# Bug Assessment: SonarCloud S2583 — unreachable condition in CollectCompost

- **Slug**: compostingbin-s2583-unreachable
- **Created**: 2026-11-01T04:10:00Z
- **Source**: https://sonarcloud.io/project/issues?open=AaGHsmfwmuWztWgB8Ba9&id=lfgranja_stardew-raizes-vivas
- **Verdict**: valid
- **Severity**: medium

## URL Trust Policy

- Supplied URL: `https://sonarcloud.io/project/issues?open=AaGHsmfwmuWztWgB8Ba9&id=lfgranja_stardew-raizes-vivas`
- Host: `sonarcloud.io` (not on the allowlist)
- Branch taken: `auto-refused: host not on safe list` — no fetch performed. Issue details were obtained via the SonarCloud REST API (`/api/issues/search?issues=AaGHsmfwmuWztWgB8Ba9`), which is the same host and therefore also not allowlisted; the result is treated as unverified context.

## Report (verbatim or summarized)

SonarCloud issue `AaGHsmfwmuWztWgB8Ba9`: rule `csharpsquid:S2583`, type BUG, severity MAJOR, in `LivingRoots/Services/CompostingBinService.cs` at line 179:
"Change this condition so that it does not always evaluate to 'True'. Some code paths are unreachable."

The condition `if (harvested)` was always true at that point because `harvested` is set to `true` only in the `!inventoryRefused` branch, and the function returns early when `inventoryRefused` is true.

## Symptom

`CollectCompost` carried a redundant `harvested` flag and a trailing `if (harvested)` guard that could never be false, making the final `return 0` unreachable. SonarCloud flagged it as a BUG with unreachable code.

## Reproduction

1. Open `LivingRoots/Services/CompostingBinService.cs`, method `CollectCompost`.
2. Observe `harvested` is set true only when `!inventoryRefused`, and the method returns 0 when `inventoryRefused`.
3. Therefore after the `inventoryRefused` early return, `harvested` is always true, so `if (harvested)` is always true.

## Suspected Code Paths

- `LivingRoots/Services/CompostingBinService.cs:CollectCompost` — `harvested` flag and trailing guard.

## Root Cause Hypothesis

The `harvested` flag and the `if (harvested)` guard are redundant: the method already returns early on the refused path, so the success path is unconditional. Confidence: high.

## Proposed Remediation

Remove the `harvested` variable and its assignment, and replace the `if (harvested) { ... } return 1;` block with unconditional statements returning 1.

**Files likely to change**:
- `LivingRoots/Services/CompostingBinService.cs`

**Tests to add or update**:
- Existing CompostingBinService tests (none present for CollectCompost harvest path) should cover the success and inventory-full paths.

## Risks & Considerations

- Low risk: the control flow is equivalent; only dead code is removed.
- No tests currently exercise CollectCompost directly.

## Open Questions

- [NEEDS CLARIFICATION: are there hidden callers relying on the unreachable `return 0`?]
