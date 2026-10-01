# Bug Assessment: `CompostingBinService.CollectCompost` untestable — concrete `Farmer` dependency and `Game1.playSound` static coupling

- **Slug**: compostingbin-collectcompost-untestable
- **Created**: 2026-09-30
- **Source**: pasted text
- **Verdict**: valid
- **Severity**: high

## Report (verbatim)

> 10 falhas (CompostingBinServiceTests) → NullReferenceException em new Farmer(). Não é asset —
> é falta de seam de produção: CollectCompost recebe um Farmer concreto e addItemToInventoryBool
> não é virtual, então Moq não substitui.

The report identifies the `Farmer` seam gap accurately. Investigation during triage found that it is
**not the only defect**, and that a second one is more severe: three tests in the same class pass
only because other test classes install process-global game state first. Details in §Root Cause.

## Symptom

`LivingRoots.Tests/CompostingBinServiceTests` cannot be executed in isolation. 13 of its 19 tests
fail when the class is run on its own, and 10 of those 13 still fail in the full suite. The reported
`NullReferenceException` in `new Farmer()` is one of two distinct failures, and the failure count
itself is non-deterministic (10 vs 13) depending on whether unrelated test classes run first —
which means the test gate currently reports a number that is not reproducible.

Expected: every test in `CompostingBinServiceTests` runs standalone, with no dependency on
execution order, and passes or fails for reasons intrinsic to the code under test.

## Reproduction

**Defect A — concrete `Farmer` dependency (10 failures, both isolated and full suite):**

```bash
cd /home/luis/development/stardew-raizes-vivas
dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj --no-build \
  --filter "FullyQualifiedName~CompostingBinServiceTests.CollectCompost_FromEmptyBin_ReturnsZero"
```

1. Construct `CompostingBinService` with mocked `IModDataService`, `ISaveIdProvider`,
   `IOrganicWasteValidator`, `IMonitor`, a real `TimeProviderStub` and a real `CompostingBinFactory`.
2. Call `new Farmer()` — required by the `CollectCompost` signature.
3. Observe: `System.NullReferenceException` from
   `StardewValley.BellsAndWhistles.PlayerStatusList.AddSpriteDefinition`, via
   `FarmerTeam..ctor` → `Farmer..ctor`.
4. Moq cannot substitute the type either — `Mock<Farmer>` rejects
   `addItemToInventoryBool` because the member is not virtual.

**Defect B — `Game1.playSound` static coupling (3 failures, isolated run only):**

```bash
cd /home/luis/development/stardew-raizes-vivas
dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj --no-build \
  --filter "FullyQualifiedName~CompostingBinServiceTests.AddWaste_ToReadyBin_IsIgnored"
```

1. Same fixture as above, but drive `AddWaste` then `ProcessDayStart("Farm")` — no `CollectCompost`.
2. `ProcessDayStart` transitions the bin to `Ready` and calls `Game1.playSound("Ship")`.
3. Observe: `System.NullReferenceException` from `StardewValley.Game1.parseText`, via
   `SoundsHelper.PlayLocal` → `Game1.playSound`.
4. The same test **passes** in a full-suite run, because another class has already replaced the
   `Game1.sounds` static. Confirmed order-dependent: `GameStateFixture.Install()` is called by
   `CompostApplicationServiceTests`, `OrganicWasteValidatorTests`, and `GameLocationFixture` — never
   by `CompostingBinServiceTests`.

Tests exhibiting Defect B (pass in full suite, fail in isolation):
`AddWaste_ToReadyBin_IsIgnored`, `ProcessDayStart_AfterTwoDays_TransitionsToReady`,
`ProcessDayStart_AtThreshold_TransitionsCorrectly`.

## Suspected Code Paths

- `LivingRoots/Services/CompostingBinService.cs:57` — `CollectCompost(string, Vector2, Farmer)`.
  The concrete `Farmer` parameter is Defect A: callers cannot supply a substitute because the only
  member consumed is non-virtual.
- `LivingRoots/Services/CompostingBinService.cs:74` — `player.addItemToInventoryBool(compost)`, the
  single non-virtual call that forces the dependency.
- `LivingRoots/Domain/Interfaces/ICompostingBinService.cs:26` — the same concrete `Farmer` is baked
  into the domain interface, so the gap is not local to the implementation.
- `LivingRoots/Services/CompostingBinService.cs:86` — `Game1.playSound("Ship")` inside
  `CollectCompost`; Defect B.
- `LivingRoots/Services/CompostingBinService.cs:121` — `Game1.playSound("Ship")` inside
  `ProcessDayStart`, on the `Processing → Ready` transition; Defect B, and the reason three tests
  that never call `CollectCompost` are still affected.
- `LivingRoots/Controllers/ModController.cs:868` — the only production caller, passing
  `Game1.player`; constrains the seam's signature but does not itself break.
- `LivingRoots.Tests/CompostingBinServiceTests.cs:100,137,150` — `new Farmer()` at three of the call
  sites; more sites exist further in the file.
- `LivingRoots.Tests/Stubs/NoOpSoundsHelperStub.cs` — proves `Game1.sounds` is a public static field
  of public interface type, so Defect B is fixable in tests without a production change.
- `LivingRoots.Tests/Fixtures/GameStateFixture.cs` — installs `Game1.sounds`, which is why the full
  suite masks Defect B.

## Root Cause Hypothesis

`CompostingBinService` reaches for two pieces of live game state through the concrete game API
rather than through interfaces: a `Farmer` parameter and the `Game1.playSound` static. Both are
correct for production but make the class untestable in a unit-test host, where the game object graph
is absent. `addItemToInventoryBool` and `Farmer`'s constructor sprite lookups are the specific
blockers — neither can be satisfied by Moq or by constructing the object.

The two defects differ in blast radius. Defect A is the *intended* consequence of a missing seam. But
Defect B is masked in the full suite by `GameStateFixture`, which silently substitutes `Game1.sounds`
globally — so three tests report green while depending on a side effect from an unrelated class. That
is worse than a plain failure: it means the gate's 12-failure figure is an artifact of ordering, not a
stable count. Any future test that touches `CompostingBinService` will appear to pass for the wrong
reason.

Confidence: **high**. Both failure modes were reproduced directly and the order-dependence was
isolated by diffing per-test results between a filtered run and a full-suite run.

## Proposed Remediation

**Preferred — extract two seams, matching the pattern the codebase already established** for
`ITimeProvider`, `ISeasonProvider`, `IPlayerProvider` and `ILocationProvider`.

1. Introduce `IPlayerInventory` in `LivingRoots/Domain/` with a single method
   `bool TryAddItem(Item item)`. A production implementation wraps `Game1.player`
   (`TryAddItem` delegates to `addItemToInventoryBool`); tests inject a recording fake.
2. Change `CollectCompost` to `CollectCompost(string locationName, Vector2 tile)` and resolve the
   inventory through an injected `IPlayerInventory` held by the service, mirroring how
   `SoilDecayService` takes `ILocationProvider`. Removing the parameter rather than retyping it keeps
   the domain interface free of game types — the existing `CompostApplicationService` already reads
   the held item from `IPlayerProvider` instead of accepting a `Farmer`, so this makes
   `CompostingBinService` consistent with its sibling.
3. Inject an `ISoundPlayer` (or reuse a single `IGameAudio`-style abstraction) for the two
   `Game1.playSound` call sites, so playback is substitutable and no test needs `Game1.sounds`.

**Alternative — test-side only, no production change** (smaller blast radius, leaves the design gap):

1. Call `GameStateFixture.Install()` from the `CompostingBinServiceTests` constructor, matching the
   other three classes. This makes all 19 tests deterministic and fixes Defect B outright.
2. Defect A still needs production change — `new Farmer()` cannot be made to work without reflection
   into `Game1.game1`, which NFR-4 forbids and which still would not let Moq verify
   `addItemToInventoryBool`.

This alternative is the pragmatic first step: it fixes the order-dependence and drops failures from 13
to 10, at the cost of leaving `CollectCompost` coupled to a concrete `Farmer`.

**Files likely to change** (preferred path):

- `LivingRoots/Domain/IPlayerInventory.cs` (new)
- `LivingRoots/Services/PlayerInventory.cs` (new — wraps `Game1.player`)
- `LivingRoots/Domain/Interfaces/ICompostingBinService.cs` — `CollectCompost` signature
- `LivingRoots/Services/CompostingBinService.cs` — constructor, both `playSound` sites
- `LivingRoots/ModEntry.cs` — composition root, wires the new collaborators
- `LivingRoots/Controllers/ModController.cs:868` — call site drops the `Game1.player` argument
- `LivingRoots.Tests/CompostingBinServiceTests.cs` — injects the recording fake

**Tests to add or update**:

- Every one of the 19 existing tests should pass standalone after the fix; assert that explicitly by
  running the class with `--filter` and confirming 0 failures.
- `CollectCompost_AddsOneItemPerMaturationLevel` — verify the fake received exactly
  `MaturationLevel` items (the existing assertion only checks the return value).
- `CollectCompost_WhenInventoryFull_ReportsFailure` — cover `TryAddItem` returning false, once the
  method's contract is defined. **[NEEDS CLARIFICATION: should a failed add still consume the bin? Currently the return value is ignored.]**
- `ProcessDayStart_OnMaturationComplete_RequestsShipCue` — verify the sound seam was invoked, so the
  cue stops being a silent side effect.
- A guard test that `CompostingBinServiceTests` passes with no sibling class loaded, to prevent
  regression of Defect B.

## Risks & Considerations

- **API breakage.** Changing `ICompostingBinService.CollectCompost` breaks the public contract. The
  only production caller is `ModController.cs:868`, but any third-party mod consuming the interface
  would be affected. The mod is `0.0.1` and unreleased, so this is cheap now and expensive later.
- **Behavioural subtlety.** `addItemToInventoryBool` returns a bool that the production code ignores.
  Introducing `TryAddItem` surfaces that; deciding what happens when the inventory is full changes
  gameplay. Needs a design call, not a mechanical refactor.
- **`Game1.playSound` inside `lock (_lock)`.** Both call sites fire while holding the service lock
  (lines 74-88 and 121 sit inside `lock` blocks). Wrapping audio in an injected seam makes it more
  tempting to move those calls outside the lock; if done, the ordering guarantee the log messages
  imply would weaken. Worth fixing deliberately rather than incidentally.
- **The stub already exists.** `NoOpSoundsHelperStub` shows Defect B is solvable purely by wiring,
  so the production change for audio is optional — the test-side alternative is genuinely
  sufficient. Prefer not to add an interface the codebase does not need (Principle V).
- **Scope creep risk.** Spec 003's constitution gate defines a quality bar; a green suite obtained
  only in full-suite ordering does not meet "tests run in any order" (SC-5). Fixing Defect B is
  therefore not optional polish.

## Open Questions

- **[NEEDS CLARIFICATION: should `CollectCompost` still accept a `Farmer`?** The report implies the
  parameter should go, but `CompostApplicationService` uses `IPlayerProvider.CurrentItem` — there may
  be a reason the bin service takes the player directly (e.g. co-op: which player's inventory?).
- **[NEEDS CLARIFICATION: co-op semantics.** In multiplayer, does collected compost go to the
  collecting player or to the farm host? An `IPlayerInventory` resolved from `Game1.player` assumes
  "the local player", which may be wrong. Worth confirming before the signature is locked.
- **[NEEDS CLARIFICATION: inventory-full behaviour.** Currently ignored. See Risks.
- **[NEEDS CLARIFICATION: should the audio seam be production code or test-only wiring?]** The
  evidence points to test-only being sufficient; confirm before adding an interface.
- **[NEEDS CLARIFICATION: are the 3 order-dependent tests currently *believed* to be passing?** If
  anyone has been reading 12 as a stable failure count, that number needs correcting in
  `specs/003-composting-tests/briefings/README.md`, which currently states 10 and does not mention
  the ordering effect.