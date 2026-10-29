# Bug Assessment: PR #518 `Build and Test` job fails with 83 test failures

- **Slug**: `ci-test-failures-pr-518`
- **Created**: 2026-10-29T03:55:14Z
- **Source**: https://github.com/lfgranja/stardew-raizes-vivas/actions/runs/40793022307/job/123182606520
- **Verdict**: valid
- **Severity**: high

## Source Trust Record

- **Verbatim URL supplied**: `https://github.com/lfgranja/stardew-raizes-vivas/actions/runs/40793022307/job/123182606520`
- **Parsed host**: `github.com`
- **Policy branch taken**: `allowlisted` — `github.com` is a widely used public bug-report source and the workflow is designed to fetch it without prompting.
- **Fetch notes**: The public job page renders behind GitHub's "Sign in to view logs" gate, so the failed-step log was retrieved with the already-authenticated GitHub CLI:
  `gh run view 40793022307 --job 123182606520 --log-failed`
  No redirects were followed. Page and log content were treated as untrusted data; no instruction-like content was acted on.

## Report (verbatim or summarized)

Source: **PR #518 — `docs: headless verification runner spec, contracts, governance, and tooling (spec 010)`**, branch `story/010-spec-and-contracts`, head commit `758325fb3fe86e80ed5109ea0ffc4709e5ea9243` (`758325f`).

The **`Build and Test`** job (`123182606520`) for run `40793022307` concluded **failure**. The build step succeeded with **0 error(s)**; the failure is entirely in the test step, which exited with code 1.

Final test summary copied from the failed job log:

```text
Total tests: 1425
     Passed: 1338
     Failed: 83
    Skipped: 4
 Total time: 27.0289 Seconds
```

The 83 failures are distributed as follows (counts are unique failing test cases from the log; they sum to 83):

| Failure family | Cases | Representative failure |
|---|---:|---|
| Soil-health semantics and persistence (`0` vs `NaN`, sparse-cache, load/DoS, bounds, reload isolation) | 27 | `SoilHealthServiceContractTests.SetSoilHealth_ClampsToMin` — expected `0`, actual `NaN` |
| Health-band boundaries (`30/70` legacy tests vs `34/67` implementation) | 8 | `SoilHealthCategoryInvariantsTests.FromHealthValue_BoundaryValues_MapsCorrectly(health: 31)` and `(health: 70)` |
| Visualization cache/overlay/log contracts | 14 | `SoilHealthVisualizationTests.GetOverlays_HealthWithinTolerance_IsCacheHit_BeyondTolerance_IsMiss` |
| Composting bin placement, pickup, lock, and state transitions | 11 | `BinPickupTests.OnButtonPressed_RightClickOnProcessingBinWithEmptyHands_ReturnsBinToPlayerInventory` |
| Production source guards (paths, comments, unreferenced constants) | 10 | `NoHardcodedVisualizationLiteralsTests` looking for `LivingRoots/ModConstants.cs` |
| Asset registration and presence | 4 | `AssetPresenceTests.T234_TextureAssets_ExistInSourceAndAreCopiedToBuildOutput` |
| Fullwidth Unicode normalization | 2 | `SecuritySanitizationInvariantsTests.UnicodeNormalizer_NormalizesFullwidthCharacters` |
| Single/multiplayer parity | 2 | `SingleMultiplayerParityInvariantsTests.Fr018_SingleAndMultiClientDayStart_IdenticalProgression` |
| `ModEntry` resilience | 1 | `ModEntryResilienceTests.T228_Entry_WhenWiringThrows_DoesNotPreventModLoadAndLogsSanitizedOnly` |
| Architecture, spec-document, JSON, and i18n contracts | 4 | `DomainLayeringTests.DomainFiles_DeclareNoSmApiOrStardewValleyUsings`, `CompostingBinDataTests.ObsoleteFields_AreMarkedJsonIgnore`, `HudNotifierTranslationTests.NotifyAccessibilityPatternsDegraded_NonEnglishLocale_…` |

Concrete failure excerpts from the log:

- `SoilHealthServiceContractTests.SetSoilHealth_ClampsToMin` — `Expected: 0`, `Actual: NaN`.
- `SoilHealthServiceContractTests.GetSoilHealth_UnknownTile_ReturnsZero` — unknown tile expected `0`, observed `NaN`.
- `SoilHealthCategoryInvariantsTests.FromHealthValue_BoundaryValues_MapsCorrectly(health: 31)` — expected `Moderate`, observed `Poor`; `(health: 70)` — expected `Moderate`, observed `Healthy`.
- `ColorInterpolationServiceTests.GetCategoryForHealth_NegativeValue_OutOfRange_ReturnsUnknown` — expected `Unknown`, observed `Poor`.
- `AssetPresenceTests.T234_TextureAssets_…` — assets present in source, but the test's glob translation did not match root-level assets.
- `NoHardcodedVisualizationLiteralsTests.ModConstants_RemainsTheAllowlistedHomeOfTheVisualizationPalette` — `Production source file not found: …/LivingRoots/ModConstants.cs` (the palette actually lives in `LivingRoots/Constants.cs`).
- `ProductionSourceGuardTests` — several guard failures reported logs/sounds inside a lock and stale comment/path expectations.

> The GitHub page itself exposed only repository/navigation chrome before the sign-in gate; the job log is the authoritative content summarized above.

## Symptom

At commit `758325f`, the required test step compiles but fails **83 of 1425** tests, so the `Build and Test` job exits non-zero even though `dotnet build` reports zero errors. Expected behavior is a fully green test step (or a deliberately skipped check); observed behavior is a red job with failures concentrated in soil-health value semantics, health-category boundaries, visualization cache behavior, and compost-bin interactions.

## Reproduction

1. Check out the failing commit:
   `git checkout 758325f`
2. Provision a Stardew Valley + SMAPI install in `GamePath`, as `.github/workflows/build_and_test.yml` does (the `Pathoschild.Stardew.ModBuildConfig` package hard-fails without it).
3. Build exactly as CI does:
   `dotnet build --no-restore --configuration Release`
4. Run the suite exactly as CI does:
   `dotnet test --no-build --configuration Release --verbosity normal`
5. Observe the test step exit code `1` and the summary `1425 total / 1338 passed / 83 failed / 4 skipped`.

Observed outside CI: the failure set is deterministic; all 83 failures reproduce together at `758325f`. The game install is the only environmental prerequisite that cannot be inferred from a plain `dotnet test` on a machine without Stardew Valley.

## Suspected Code Paths

- `LivingRoots/Services/SoilHealthService.cs` — `SetHealthInternal`, `ClampHealthValue`, `GetSoilHealth`, `UpdateHealth`, `LoadData`, and the save path. Governs whether an unknown tile returns `0` or `NaN`, whether an explicit measured `0` survives the sparse cache, and how invalid/NaN/out-of-range values are normalized on load.
- `LivingRoots/Domain/Visualization/HealthCategory.cs` — declares half-open bands `Poor [0,34)`, `Moderate [34,67)`, `Healthy [67,100]`, but the older tests still assert `30/70`; its XML docs also claim finite out-of-range values resolve to `Unknown`.
- `LivingRoots/Services/Visualization/ColorInterpolationService.cs` — `GetCategoryForHealth` clamps finite out-of-range values to boundary categories (`Poor`/`Healthy`), which contradicts the enum's documented `Unknown` behavior and the two failing out-of-range tests.
- `LivingRoots/Services/Visualization/OverlayRenderer.cs` and `VisualizationService.cs` — epsilon-based cache hit/miss semantics, drawable-only filtering, zero-health suppression, and style-change cache invalidation.
- `LivingRoots/Controllers/ModController.cs` — building-footprint placement refusal, batch `modData` preservation when a bin is removed by tool/explosion, fail-closed behavior when the lock service is absent, and translated conflict text.
- `LivingRoots/Services/CompostingBinService.cs` — lock discipline (no logging/sound while holding the lock, single lock acquisition in orphan pruning), `Empty → Processing → Ready` transitions, and retaining `Ready` when inventory refuses collection.
- `LivingRoots/Domain/Services/OrganicWasteValidator.cs` and `ModEntry.cs` (`OnAssetRequested` / machine triggers) — the required-tag set must exactly match what the validator accepts; drift in either direction fails registration tests.
- `LivingRoots/Domain/Models/CompostingBinData.cs` — obsolete fields must carry `[JsonIgnore]` so persisted bin data does not grow or round-trip dead state.
- `LivingRoots/Constants.cs` — the visualization palette allowlist lives here, not in the `LivingRoots/ModConstants.cs` path assumed by the architecture test.
- `LivingRoots.Tests/ProductionSourceGuardTests.cs` — literal source paths, expected comment text, and the unreferenced-constant scan drifted from the reorganized files.
- `LivingRoots.Tests/Invariants/SingleMultiplayerParityInvariantsTests.cs` — structural player-count scan false positive and single-vs-multiplayer day-start divergence.
- `LivingRoots.Tests/Architecture/DomainLayeringTests.cs` — Domain layer must not import SMAPI/StardewValley types.
- `LivingRoots.Tests/AssetPresenceTests.cs` — MSBuild `**/` glob was translated as a required directory segment, so `Assets/**/*.*` did not match root-level assets.
- `LivingRoots/Services/FileNameSanitizer.cs` (and the Unicode normalization path it uses) — fullwidth digits/punctuation are not normalized to their ASCII equivalents.
- `LivingRoots.Tests/ModEntryResilienceTests.cs` — `ModEntry.Entry` must swallow wiring failures, log sanitized context, and still register enough to unload cleanly.
- Localization asset files — the degraded-accessibility notification renders a raw key or English source under a non-English locale.

## Root Cause Hypothesis

**Confidence: high.** This is contract drift in both directions, not a single defect.

The production side moved to a newer contract — `34/67` health bands, explicit measured `0` retained in the sparse cache, finite out-of-range health clamped at the visualization boundary, fail-closed compost-bin locks, validator-derived machine tags, building-footprint placement refusal, and resilient `ModEntry.Entry` — while a substantial part of the test suite still encoded the older contract (`30/70` bands, `NaN` for unset tiles, out-of-range → `Unknown`, weaker lock/placement expectations). Several guards also encoded stale repository facts: `ModConstants.cs` versus the actual `Constants.cs`, literal comment strings that changed, and a hand-rolled glob translation that did not implement MSBuild's `**/` semantics. Finally, a few production paths were incomplete relative to tests that already described the intended behavior (bin pickup/batch preservation, `ModEntry` resilience, single/multiplayer parity, fullwidth normalization). The result is a large, thematically clustered red run rather than a random scatter of failures.

Importantly, this assessment is being written **after** local remediation. Commit `16f43c0` already implements the preferred reconciliation below and reports a fully green local suite: `1423 passed, 4 skipped, 0 failed, 1427 total`, plus a clean `dotnet format --verify-no-changes`. That commit has **not** been exercised by a fresh CI run, and it is not evidence for the historical failing run.

## Proposed Remediation

**Preferred**: Reconcile production and tests to the governing specs rather than to whichever side is cheaper to change.

- Treat the specs (`specs/009-end-to-end-game-verification/data-model.md`, FR-008/FR-014, and the spec 010 contracts) as authoritative for bands, sparse-cache, NaN/out-of-range handling, lock discipline, and placement rules.
- Update the legacy `30/70` assertions to `34/67` boundary tables, and make unknown-tile/zero semantics explicit: an unset tile reads as `0` but is not drawable; an explicitly measured `0` survives the sparse cache and renders `Poor`.
- Fix the source guards to resolve paths from the repository root (and to point at `Constants.cs`), instead of embedding assumptions about file layout.
- Translate `**/` correctly in `AssetPresenceTests` (zero-or-more directories), and make `ModEntry.Entry` fail soft with sanitized logging.
- Keep the fail-closed lock behavior and the validator-derived tag set; update the asset-registration fixtures to the validator's actual accept/reject surface.
- **Residual work not covered by `16f43c0`**: pick one contract for finite out-of-range health. `HealthCategory`'s XML docs say `Unknown`; `ColorInterpolationService.GetCategoryForHealth` clamps to `Poor`/`Healthy`. Align the docs and the tests to the chosen behavior, or change the implementation to return `Unknown`.

Because `16f43c0` already contains the bulk of this work, `/speckit.bug.fix` should verify the reconciliation against the current tree, fill any remaining gaps, and avoid redoing the landed changes. It must not treat the local green run as CI verification.

**Alternatives**:
- *Revert production to the old `30/70`/`NaN` contract.* Rejected: it contradicts the current specs and would undo the fail-closed lock and placement hardening.
- *Skip or allowlist the failing tests to get a green job.* Rejected: it would hide real contract drift and reproduce the project's documented "false green" anti-pattern.

**Files likely to change** (already touched by `16f43c0`; residual changes may still be needed):
- `LivingRoots/Domain/Visualization/HealthCategory.cs` (docs vs behavior)
- `LivingRoots/Services/Visualization/ColorInterpolationService.cs` (only if the contract decision changes behavior)
- `LivingRoots/Services/SoilHealthService.cs`
- `LivingRoots/Services/Visualization/OverlayRenderer.cs`
- `LivingRoots/Services/Visualization/VisualizationService.cs`
- `LivingRoots/Controllers/ModController.cs`
- `LivingRoots/Services/CompostingBinService.cs`
- `LivingRoots/Domain/Services/OrganicWasteValidator.cs`
- `LivingRoots/ModEntry.cs`
- `LivingRoots.Tests/**` (band tables, guards, glob translation, parity, Unicode, resilience, bin interactions)

**Tests to add or update**:
- Boundary tables at `0`, just-below/at `34`, just-below/at `67`, `100`, and finite out-of-range values, asserting one documented contract.
- Explicit measured `0` round-trips and renders `Poor`; unset tile reads `0` but emits no overlay.
- NaN/Infinity load normalization and reload isolation across save IDs.
- Compost-bin placement on every building-footprint tile, pickup with empty hands, batch `modData` preservation after tool/explosion removal, and inventory-refusal retention.
- Lock-discipline source guard: no log/sound inside the lock and a single lock acquisition in orphan pruning.
- Asset required-tag set equals the validator's accepted set (both directions).
- `Assets/**/*.*` matches root-level assets and nested assets.
- Fullwidth digit/punctuation normalization round-trip.
- `ModEntry.Entry` swallows a wiring exception, logs sanitized text, and still unloads.
- Single vs multiplayer day-start produces identical progression, and the structural scan finds no real player-count branch.

## Risks & Considerations

- **No CI re-run after `16f43c0`.** The green result is local only; the dormant `Build and Test` workflow has not confirmed it. A fresh run is required before claiming the bug fixed.
- **No Gate 3 gameplay verification.** Placement refusal, pickup, sparse-cache zero retention, and lock fail-closed behavior are gameplay-visible; they need a manual SMAPI session with multi-signal evidence per `VERIFICATION_PROTOCOL.md`.
- **Residual contract inconsistency.** `HealthCategory` docs and `ColorInterpolationService` behavior still disagree for finite out-of-range values. This is a documentation/contract bug even while tests are green.
- **Sparse-cache growth.** Persisting explicitly measured zeros slightly increases save size; it should be bounded by measured tiles and validated against the save comparator.
- **Fail-closed locks.** Rejecting bin interactions when the lock service is absent is safer but may surface user-visible conflict text in multiplayer edge cases.
- **Stale verification artifact.** The old `.specify/bugs/ci-build-test-failures-on-pr-314/test.md` predates this fix and must not be reused; `/speckit.bug.test` must regenerate evidence for this slug.
- **Branch-protection fragility.** `Build and Test` remains intentionally non-required while dormant; a green local run must not be promoted to a required check without an actual CI green.

## Open Questions

- [NEEDS CLARIFICATION: Which contract is authoritative for finite out-of-range health — `Unknown` (current XML docs) or clamping to `Poor`/`Healthy` (current `ColorInterpolationService`)? The fix must choose one and align docs, code, and tests.]
- [NEEDS CLARIFICATION: Should an explicitly measured `0` be persisted in the sparse cache permanently, or only retained for the current session? Current tests require round-trip persistence.]
- [NEEDS CLARIFICATION: Is the machine required-tag set intentionally derived from `OrganicWasteValidator`, or should it remain a fixed list with the validator constrained to match it?]
- [NEEDS CLARIFICATION: After this fix lands, should `Build and Test` be promoted to a required status check, or stay advisory until the workflow is reliably provisioned?]
