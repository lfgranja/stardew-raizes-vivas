# Bug Verification: Soil Health tooltip shows "Unknown" and flashes constantly on all cursor positions

- **Slug**: soil-health-unknown-tooltip-flashing
- **Tested**: 2026-11-07
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial

## Summary

The targeted unit tests covering the reported scope and flashing regressions pass (11/11) after the fix, and the formatter reports no changes. However, the original in-game reproduction (real SMAPI session, cursor over non-applicable tiles, tilled/watered HoeDirt behavior, visual confirmation of no flashing) was **not** executed in this environment. Per guardrails, the result is downgraded to `partial` pending Gate 3 / manual in-game verification.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix, in-game) | Manual SMAPI session: hover non-tilled tiles, till/water HoeDirt, observe tooltip stability/scope | not-run | No real game/SMAPI interactive session executed in this environment. Required for full verification of this runnable visualization fix (Gate 3). |
| New / updated tests | `GamePath=/home/luis/stardew-assemblies dotnet test --configuration Release --filter "FullyQualifiedName~TooltipRendererTests\|FullyQualifiedName~VisualizationServiceTooltipTests" --nologo` | pass | 11 passed, 0 failed, 0 skipped (LivingRoots.Tests.dll, net6.0), fresh run 2026-11-07. |
| Regression suite (broader) | Not executed in full | not-run | Targeted filter used for fast evidence; full suite not run in this session. Pre-push hook/smoke suite remains applicable before merge/push as per repo workflow. |
| Lint / format | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | pass | No formatting changes required (2026-11-07). |

## Output Excerpts

```text
Aprovado!  – Com falha:     0, Aprovado:    11, Ignorado:     0, Total:    11, Duração: 1 s - LivingRoots.Tests.dll (net6.0)
```

```text
dotnet format Stardew-LivingRoots.sln --verify-no-changes
(no output; exit 0)
```

## Residual Risks

- **No in-game visual confirmation:** The flashing symptom is frame/render-path observable; unit tests pin the logic (missing-key null, present-NaN unknown, throttle returns cached tooltip) but do not render in the actual game UI. Gate 3 / real SMAPI verification is still needed.
- **HoeDirt baseline semantics unchanged:** Tilled/watered HoeDirt without an initialized soil-health record may still show `Soil Health: Unknown` (FR-014) if present as NaN in the snapshot. That matches the fix's scope but may not satisfy player expectations; product/spec clarification is open (assessment open questions 1–2).
- **Broader regression coverage:** Only tooltip renderer/service tests were run; visualization/controller/soil-health interactions outside this path were not exercised here.

## Recommendation

Hold for Gate 3/manual in-game verification before closing the bug as fully verified. Run a real SMAPI session with tooltips enabled and confirm: (1) non-tilled/non-applicable tiles show no soil-health tooltip; (2) applicable HoeDirt with unavailable data shows a stable `Soil Health: Unknown` tooltip without flashing; (3) tiles with measured health show stable percentage/category; (4) FR-015 throttle does not cause visible flicker. If behavior matches, upgrade to `verified` with evidence. If HoeDrit baseline remains confusing, address assessment open questions and follow up with a separate change.
