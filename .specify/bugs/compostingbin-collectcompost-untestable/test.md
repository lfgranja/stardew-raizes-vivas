# Bug Verification: `CompostingBinService.CollectCompost` untestable — concrete `Farmer` dependency and `Game1.playSound` static coupling

- **Slug**: compostingbin-collectcompost-untestable
- **Tested**: 2026-09-30
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

Both defects are gone. The assessment's two reproduction commands now pass, `CompostingBinServiceTests`
runs standalone with 0 failures, and the full-suite failure count is stable at 2 across repeated runs —
so the order-dependence that made the number unreliable is resolved. Two **negative controls** confirm
the new regression tests actually pin the fix rather than merely passing: reverting the
`GameStateFixture.Install()` guard produced 12 failures, and breaking the `IPlayerInventory` grant made
both new tests fail on their intended assertions.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction Defect A (post-fix) | `dotnet test --filter "...CompostingBinServiceTests.CollectCompost_FromEmptyBin_ReturnsZero"` | **pass** | Assessment reproduction, steps 1–4. Was `NullReferenceException` in `new Farmer()`. |
| Reproduction Defect B (post-fix) | `dotnet test --filter "...CompostingBinServiceTests.AddWaste_ToReadyBin_IsIgnored"` | **pass** | Assessment reproduction. Exercises `AddWaste` + `ProcessDayStart` only — never `CollectCompost`. Was `NullReferenceException` in `Game1.playSound`. |
| Class isolation (SC-5) | `dotnet test --filter "FullyQualifiedName~CompostingBinServiceTests"` | **pass** | 21/21 with no sibling class loaded. Directly exercises the `GameStateFixture.Install()` guard. |
| New regression tests | `dotnet test --filter "...CollectCompost_AddsOneItemPerMaturationLevel\|...CollectCompost_WhenInventoryRefuses_StillConsumesBin"` | **pass** | Both named in the fix report; 2/2. |
| Negative control A — guard removed | Deleted `GameStateFixture.Install()` from the ctor in a `/tmp` copy | **fail (expected)** | 12 of 21 failed. Proves the guard is load-bearing. |
| Negative control B — seam broken | Removed the `TryAddItem` call from `CollectCompost` in a `/tmp` copy | **fail (expected)** | Exactly the 2 new tests failed, on their intended assertions. Proves they pin the seam. |
| Regression suite | `dotnet test` (full, 3 consecutive runs) | **pass** | 811 passed / 2 failed / 813 total, identical all 3 runs. |
| Build / type-check | `dotnet build Stardew-LivingRoots.sln --nologo` | **pass** | 0 errors, 0 warnings. |
| Lint / format | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | **pass** | Exit 0, no diagnostics. |
| Workspace integrity | `md5sum` of `git status --porcelain` before and after | **pass** | `a95b7b0a…` both times. No source file modified by this verification. |

### Negative control detail

Both controls ran on throwaway copies under `/tmp`, since this command must not touch the workspace.
Both copies were deleted afterwards.

**Control A** — removing the single `GameStateFixture.Install()` line from the test constructor:

```
Com falha! – Com falha: 12, Aprovado: 9, Total: 21
```

Worth flagging: the assessment predicted removing this guard would break only Defect B's **3** tests.
It actually broke **12**. `GameStateFixture.Install()` is doing more than neutralising audio — it also
installs `Game1.content`, `Game1.objectData`, `ItemRegistry` and `Game1.game1`, without which
`ItemFactory`'s `new StardewValley.Object` also fails. So the one-line guard is load-bearing for both
defects plus item construction. That is a correction to the assessment's impact estimate, not to its
diagnosis.

**Control B** — removing the `TryAddItem` grant from `CollectCompost`:

```
CollectCompost_AddsOneItemPerMaturationLevel
  Assert.Equal() Failure: Values differ
  Expected: 1
  Actual:   0

CollectCompost_WhenInventoryRefuses_StillConsumesBin
  Moq.MockException: Expected invocation on the mock once, but was 0 times:
  x => x.Log(It.Is<string>(s => s.Contains("inventory refused compost")), LogLevel.Warn)
```

Both failed on the intended assertion — item count and refusal log — not incidentally. The other 19
tests stayed green, confirming the failure is attributable to the broken seam and not to the mutation
itself.

## Output Excerpts

Isolated class (the Defect B guard):

```
Aprovado!  – Com falha: 0, Aprovado: 21, Ignorado: 0, Total: 21, Duração: 2 s
```

Full suite, 3 runs — count no longer varies:

```
Com falha! – Com falha: 2, Aprovado: 811, Ignorado: 0, Total: 813, Duração: 4 s
Com falha! – Com falha: 2, Aprovado: 811, Ignorado: 0, Total: 813, Duração: 4 s
Com falha! – Com falha: 2, Aprovado: 811, Ignorado: 0, Total: 813, Duração: 4 s
```

Residual 2 failures — confirmed the asset-scoped ones, not regressions:

```
CompostApplicationServiceTests.TryApplyCompost_ValidTile_IncreasesHealth
CompostApplicationServiceTests.TryApplyCompost_HealthCappedAtMax
```

## Residual Risks

- **2 failures remain, out of scope.** Both trace to the missing `LivingRoots.Compost` asset (task
  `A001`, briefed in `specs/003-composting-tests/briefings/compost-item.md`). They were failing before
  this fix and are unchanged by it. Not regressions.
- **The `Ship` cue remains unassertable.** Per the decision recorded in the fix report, Defect B was
  fixed with test-side wiring only, so no `ISoundPlayer` seam exists. Nothing verifies that the cue
  fires. Accepted trade-off — playback is presentation — but a future change could silently drop the
  sound.
- **Inventory-full semantics are preserved, not fixed.** A refused grant still consumes the bin, so
  compost is lost. The new test locks this in deliberately, which makes the behaviour explicit and
  prevents accidental divergence — but it also makes changing it require a deliberate test update.
  That is the intent.
- **Co-op untested.** `PlayerInventory` wraps `Game1.player`, so compost goes to the local player. No
  test exercises a remote farmhand. Pre-existing gap; co-op is unexercised across this suite.
- **ModConstants id-convention inconsistency found during verification.** `ModConstants.cs:63-65`
  declares three ids in two different conventions:
  - `CompostItemId = "LivingRoots.Compost"` (dot)
  - `CompostingBinItemId = "LivingRoots.CompostingBin"` (dot)
  - `CompostingBinRecipeId = "LivingRoots_CompostingBin"` (underscore)

  `CompostingBinRecipeId` has no consumers, so it breaks nothing today. But per the
  `briefings/compost-item.md` analysis, `Data/Objects` keys must match the constant exactly or the
  comparison in `CompostApplicationService.cs:46` fails silently with no error or log. When `A001`
  lands, the recipe id must be reconciled to the same convention as the item ids — or that silent
  failure mode becomes live. **Not introduced or touched by this fix**; raised here because it sits in
  the same constant block the asset work depends on.
- **The concurrent-edit concern from `fix.md` is unresolved.** The production change was applied by a
  process outside this session's control. It now passes build, format, isolated run and both negative
  controls, so its behaviour is verified — but its provenance is still unexplained, and it shares the
  working tree with unrelated in-flight work.

## Recommendation

Close the bug — verified end-to-end. Both defects are resolved, the reproduction no longer triggers,
the order-dependence is gone (stable 2-failure count), and two negative controls demonstrate the new
regression tests fail when the fix is withdrawn. The one thing to settle before commit is provenance:
confirm the concurrent editor intended the `CollectCompost` signature change, since it breaks
`ICompostingBinService` for any external consumer and was not authored under review. Separately,
reconcile the `ModConstants` id convention before `A001` implements `content.json`, or that asset will
silently fail its item-id comparison.