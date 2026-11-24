# Bug Fix: Triage of residual risks — not a defect report

- **Slug**: residual-risks-triage
- **Fixed**: 2026-10-03
- **Assessment**: ./assessment.md
- **Status**: not-applied

## Summary

No code change was made, per the assessment's own `invalid` verdict and its **Preferred**
remediation ("no code change. Close this as not-a-defect"). However, re-verifying the
assessment against the current tree produced a material finding: the assessment's single
**Actionable Finding** and its first remediation item were both remediated **after** the
assessment was written (2026-09-30 → 2026-10-03). The report below is the closure record for
the bug loop plus that correction.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `.specify/bugs/residual-risks-triage/fix.md` | added | This report. Loop closure, status `not-applied`. |

No production or test file was touched.

## Verification Performed

Assessment re-checked line by line against the tree at `7540bd9`. Three of its claims no
longer hold:

| Assessment claim (line) | Current state | Evidence |
|-------------------------|---------------|----------|
| `CompostApplicationService.cs:46` compares `QualifiedItemId` against `CompostItemId` with a **bare `!=`**, so a 1.6-qualified item fails silently (`:98-103`) | **Already fixed.** Now a three-way check including `QualifiedCompostItemId` | `LivingRoots/Services/CompostApplicationService.cs:47-49`; commits `f59fa09`, `301726e` (2026-10-03) |
| `CompostingBinRecipeId = "LivingRoots_CompostingBin"` — underscore convention (`:91-94`) | **Already reconciled.** Value is now `"LivingRoots.CompostingBinRecipe"` — dot convention | `LivingRoots/Constants.cs:66` |
| `CompostingBinItemId` 0 consumers, `CompostingBinRecipeId` 0 consumers (`:96`) | **Both now have a consumer** — the crafting-recipe registration | `LivingRoots/ModEntry.cs:121`, wired by commit `67d3a33` |
| `content.json` does not exist yet; hazard becomes live at `A001` (`:105-107`) | `A001` has effectively landed, and landed on the **correct** dot convention | `ModEntry.cs:117-123` |

The assessment's §Root Cause finding — that the `ICompostingBinService` signature change broke
no external consumer, because the mod is unreleased (`0.0.1`, no tags, no releases) — was
spot-checked and still holds.

Its §Actionable Finding hazard is therefore **closed by intervening commits, not by this
command**. No reconciliation or deletion is needed, and deleting the two constants as
YAGNI-suggested would now break `ModEntry.cs:121`.

## Deviations from Assessment

1. **The assessment's central actionable finding is stale.** It is written as a live latent
   defect requiring a `ModConstants` fix. As of `7540bd9` it is already fixed in code. Acting
   on the assessment as written would have produced a redundant or actively harmful change
   (deleting constants that now have a consumer).

2. **The assessment cites a file that does not exist.** It references
   `LivingRoots/ModConstants.cs:63-65`. That file is absent; the class `ModConstants` lives in
   `LivingRoots/Constants.cs`. Root `AGENTS.md` documents this exact naming confusion as a
   known trap. The assessment was not edited, per guardrail.

3. **The gate condition no longer holds.** Remediation item 1 said to "gate this on `A001`".
   `A001` is no longer hypothetical — the recipe registration shipped in `67d3a33` — so the
   gate is moot rather than pending.

4. **The suite could not be re-run.** See §Local Verification.

## Local Verification

- **No test suite was run.** `Pathoschild.Stardew.ModBuildConfig` hard-fails without a game
  folder, and `Stardew Valley.dll` is not present on this machine. The assessment's claim of
  "exactly 2 failures, both `CompostApplicationServiceTests`" (`:36`) is therefore **neither
  confirmed nor refuted** here — it is inherited unverified. Given `CompostApplicationService`
  was edited three commits ago (`f59fa09`, `301726e`), those two failures are a plausible
  consequence of that edit and should be re-checked wherever the suite can actually run.
- **Static verification only**, via `rg` and `git log -S` over `LivingRoots/`: item-id constant
  values, consumer counts, and commit provenance — cited inline above.
- No manual in-game check was performed, and none is possible here.

## Tests Added or Updated

None. The assessment specifies none (`:133-134`): the existing
`CollectCompost_WhenInventoryRefuses_StillConsumesBin` test already pins the
full-inventory-loses-compost behaviour as intentional.

## Follow-ups

- **Re-run the suite** and confirm the state of the 2 `CompostApplicationServiceTests` failures
  the assessment recorded. They predate the 2026-10-03 item-id edits and may now be stale or
  newly significant. This is the one open thread from the assessment that this command could
  not close.
- **The four `[NEEDS CLARIFICATION]` items in the assessment (`:147-159`) remain open** and are
  *not* resolved here, since resolving them would require product decisions, not a fix. Two are
  now moot:
  - "reconcile or delete the two constants?" — **moot**, reconciled and in use by `ModEntry.cs:121`.
  - "assess the inconsistency now or defer to `A001`?" — **moot**, `A001` has shipped.
  - Still live: is full-inventory-loses-compost intended design? Was concurrent authoring of the
    working tree expected? The latter is a process question the assessment correctly routes to
    process level, not to a bug.
- **Do not batch-commit the working tree.** The assessment warns 96 entries are uncommitted and
  spans an API change plus unrelated in-flight work. Current count is lower but the
  uncommitted `.specify/`, `AGENTS.md`, `.agents/`, and `.opencode/` churn should still be kept
  out of any functional commit.
- **`LivingRoots/Constants.cs` vs `ModConstants.cs`.** The assessment tripped on this; other
  artifacts likely will. Worth a canonical note in the spec templates.

## Next

`/speckit.bug.test slug=residual-risks-triage` — records the verification report that closes
this bug. Note that the test stage cannot validate a code change, because there is none; its
job here is to confirm the closure and re-check the two inherited test failures.
