# Bug Fix: `CompostingBinService.CollectCompost` untestable — concrete `Farmer` dependency and `Game1.playSound` static coupling

- **Slug**: compostingbin-collectcompost-untestable
- **Fixed**: 2026-09-30
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

Extracted an `IPlayerInventory` seam so `CollectCompost` no longer accepts a concrete `Farmer`, and
fixed the test-order dependency by having `CompostingBinServiceTests` install the game-state fixture
itself. `CompostingBinServiceTests` now runs standalone with 0 failures (was 13), and the full suite
drops from 12 failures to 2 — with a stable count that no longer varies with execution order.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `LivingRoots/Domain/IPlayerInventory.cs` | added | `bool TryAddItem(Item item)`. Mirrors the existing `IPlayerProvider` extraction pattern. |
| `LivingRoots/Services/PlayerInventory.cs` | added | Production impl delegating to `Game1.player.addItemToInventoryBool`. |
| `LivingRoots/Domain/Interfaces/ICompostingBinService.cs` | modified | `CollectCompost` dropped the `Farmer` parameter. Domain interface no longer references a game type for this call. |
| `LivingRoots/Services/CompostingBinService.cs` | modified | Constructor takes `IPlayerInventory`; `CollectCompost` grants via `TryAddItem` and warns when the grant is refused. |
| `LivingRoots/ModEntry.cs` | modified | Composition root wires `new PlayerInventory()`. |
| `LivingRoots/Controllers/ModController.cs` | modified | Call site drops the `Game1.player` argument. |
| `LivingRoots.Tests/Fixtures/RecordingPlayerInventory.cs` | added | Recording fake with an `AcceptsItems` switch to simulate a full inventory. |
| `LivingRoots.Tests/CompostingBinServiceTests.cs` | modified | Calls `GameStateFixture.Install()`; injects the recording fake; `new Farmer()` removed (0 occurrences remain). |
| `LivingRoots.Tests/CompostingBinServiceTests.cs` | added tests | Two regression tests (below). |

## Diff Highlights

```csharp
// LivingRoots/Services/CompostingBinService.cs
public int CollectCompost(string locationName, Vector2 tile)   // was: (..., Farmer player)
{
    ...
    for (int i = 0; i < outputCount; i++)
    {
        var compost = new StardewValley.Object(ModConstants.CompostItemId, 1);
        if (!_playerInventory.TryAddItem(compost))
        {
            _monitor.Log($"CompostingBin: inventory refused compost at ({tile.X}, {tile.Y}); bin still consumed.", LogLevel.Warn);
        }
    }
```

```csharp
// LivingRoots.Tests/CompostingBinServiceTests.cs
public CompostingBinServiceTests()
{
    // The service calls Game1.playSound on maturation and collection. Without this the class
    // only passes when a sibling test class happens to have installed the sound stub first.
    GameStateFixture.Install();
    ...
}
```

## Tests Added or Updated

- `CompostingBinServiceTests.CollectCompost_AddsOneItemPerMaturationLevel` — pins the new seam: the
  grant is observable through `IPlayerInventory.AddedItems`, not just the return value. Guards
  against the service computing the count and forgetting to hand the items over.
- `CompostingBinServiceTests.CollectCompost_WhenInventoryRefuses_StillConsumesBin` — covers
  `TryAddItem` returning false. Asserts the bin is still emptied and that the refusal is logged at
  `Warn`. This locks in the "always consume" decision as deliberate rather than accidental.
- `CompostingBinServiceTests` (19 existing) — migrated from `new Farmer()` to the recording fake;
  now pass standalone.

The assessment also requested a guard test asserting the class passes with no sibling class loaded.
That guard is enforced by the isolated `--filter` run in §Local Verification rather than as a test,
because a test cannot observe its own class-isolation property; a regression would surface as a
failure of that command, not of the suite.

## Local Verification

- `dotnet build Stardew-LivingRoots.sln --nologo` → **0 errors, 0 warnings**
- `dotnet format Stardew-LivingRoots.sln --verify-no-changes` → **clean**
- `dotnet test --filter "FullyQualifiedName~CompostingBinServiceTests"` → **21 passed, 0 failed**
  (was 6 passed / 13 failed). This is the regression check for both defects: Defect A because no
  `Farmer` is constructed, Defect B because the class installs its own game state.
- `dotnet test` (full suite, 3 consecutive runs) → **811 passed, 2 failed, 813 total** — identical on
  every run. Previously the count varied between 10 and 12 depending on ordering.

### Defect-by-defect outcome

| Defect | Tests | Before | After |
|--------|-------|--------|-------|
| A — concrete `Farmer` | 10 | fail (isolated + full) | **pass** |
| B — `Game1.playSound` | 3 | pass (full) / fail (isolated) | **pass, deterministically** |

### Remaining 2 failures are out of scope

`CompostApplicationServiceTests.TryApplyCompost_ValidTile_IncreasesHealth` and
`.TryApplyCompost_HealthCappedAtMax` remain red. These trace to the missing `LivingRoots.Compost`
asset, tracked as `A001` in `specs/003-composting-tests/tasks.md` and briefed in
`specs/003-composting-tests/briefings/compost-item.md`. Unrelated to this bug.

## Deviations from Assessment

1. **No `ISoundPlayer` seam was added.** The assessment's preferred remediation listed an audio
   abstraction as step 3. Per your decision, Defect B was fixed with test-side wiring only
   (`GameStateFixture.Install()`), and the assessment's own §Risks had already noted that
   `NoOpSoundsHelperStub` proves test wiring is sufficient and that adding an interface with no
   consumer would violate Principle V. Consequence: the `Ship` cue is no longer assertable from a
   test. It remains a silent side effect. That is acceptable because playback is presentation, not
   behaviour — but it does mean the assessment's suggested
   `ProcessDayStart_OnMaturationComplete_RequestsShipCue` test was not written.

2. **The bulk of the change was already applied before this command ran.** When I opened the files to
   begin, `IPlayerInventory`, `PlayerInventory`, the `CollectCompost` signature change, the
   `ModEntry` wiring, the `ModController` call site, `RecordingPlayerInventory` and the test
   migration were all already present and uncommitted, timestamped 23:40 — after the assessment was
   written at 23:34. I did not author them, and no tool I invoked produced them. This matches an
   earlier warning in this session that another process was editing the same test files
   concurrently. **I treated the existing work as untrusted and verified it rather than assuming it
   correct**: it built clean, and after adding the two missing regression tests the isolated class
   run reached 0 failures. I then applied `dotnet format` (3 missing final newlines) and added the
   two tests the assessment required but that the applied change had omitted. Whoever ran that
   process should confirm its output matches intent before commit.

3. **Inventory-full semantics are unchanged, as you directed.** `TryAddItem`'s return value is
   surfaced only as a `Warn` log; the bin is consumed either way. This preserves the pre-existing
   behaviour where `addItemToInventoryBool`'s result was discarded. It is a gameplay wart, not a bug
   fix — flagged for a future task rather than changed here.

4. **Co-op semantics assumed "the collecting player's inventory."** `PlayerInventory` wraps
   `Game1.player`, which in a multiplayer host is the local player — so the collected compost goes to
   whoever right-clicked. That matches the pre-fix behaviour (the controller passed `Game1.player`)
   and your answer, but it is not exercised by any test: co-op has no test coverage in this suite.

## Follow-ups

- **Correct the briefings.** `specs/003-composting-tests/briefings/README.md` states "10 failures" and
  describes the `Farmer` seam as the sole cause. Both statements are now stale: the count is 2, and
  the `Game1.playSound` order-dependency was not mentioned. The order-dependent behaviour was never
  in any briefing, so a reader cannot tell it was found and fixed.
- **Verify the concurrent edit before committing.** See Deviation 2. The change is on the working
  tree uncommitted alongside unrelated in-flight work from earlier in this session.
- **Asset A001.** Still blocking `CompostApplicationServiceTests` (2 tests) and all in-game compost
  behaviour. Needs `content.json` + a Content Patcher dependency.
- **Consider inventory-full semantics.** A refused grant currently consumes the bin, losing the
  compost. Needs a product decision and its own task.
- **Co-op coverage.** `PlayerInventory` resolves `Game1.player`; no test asserts what happens with a
  remote farmhand. Low priority while co-op is unexercised.