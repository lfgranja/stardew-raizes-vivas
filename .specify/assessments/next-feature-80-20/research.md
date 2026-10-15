# Idea Research: Next Feature by Value-per-Effort (80/20)

- **Slug**: next-feature-80-20
- **Created**: 2026-10-03
- **Evidence confidence (overall)**: **medium**

> **Premise correction.** This research was commissioned under the intake assumption that multiplayer
> was out of scope. The constitution was amended to v2.0.0 mid-assessment, adding Principle VI
> (Multiplayer As A First-Class Requirement, NON-NEGOTIABLE) and revoking the v1.x single-player-only
> scope constraint. All findings below are evaluated with Principle VI in force. This invalidates the
> intake's original framing and is itself a finding.

## Users & Demand

- The maintainer is the primary user and states the originating intent directly: the mod was conceived
  to be played with his spouse. This is first-party, not inferred — [source: user input, constitution
  amendment rationale] (confidence: **high**)
- Stardew Valley's own co-op is a first-class vanilla mode, so multiplayer is a mainstream path
  through the base game rather than a niche — [source:
  https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Multiplayer (host not on safe list — see
  Sources)] (confidence: **medium**)
- **No third-party demand signal found.** No forum threads, issue counts, or download metrics for
  co-op-oriented Stardew mods were located in this session — [source: research attempt, no result]
  (confidence: **low**, absence of evidence)
- The mod's own ROADMAP targets a solo-player arc (Elena as personal mentor, a single farm's soil
  history) with no co-op framing — [source: `ROADMAP.md`] (confidence: **high**)

## Prior Art

### SMAPI's own save-data model is already host-authoritative

- `IModHelper.Data.WriteSaveData` and `ReadSaveData` **throw** `InvalidOperationException` when
  `!Context.IsOnHostComputer`, with the message *"Save files are stored on the main player's
  computer."* — [source: https://github.com/Pathoschild/SMAPI/blob/e5b8bf3d601bec0099252c8ee2f54e5571385f1a/src/SMAPI/Framework/ModHelpers/DataHelper.cs]
  (confidence: **high**)
- This is the single most important external finding: **the platform already enforces host
  authority.** Principle VI's host-authoritative requirement does not need to be invented; it needs to
  be *respected* rather than fought.
- Custom host↔client messaging exists: `IMultiplayerHelper.SendMessage` plus
  `helper.Events.Multiplayer.ModMessageReceived`, with `GetConnectedPlayers()` returning
  `IMultiplayerPeer` (`PlayerID`, `IsHost`, `HasSmapi`, `Mods`) —
  [source: https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Multiplayer (host not on safe
  list)] (confidence: **medium**)
- Host detection primitives available: `Context.IsMainPlayer`, `Context.IsOnHostComputer`,
  `Context.IsMultiplayer`, `Context.HasRemotePlayers`, `Context.ScreenId` —
  [source: https://github.com/Pathoschild/SMAPI/blob/e5b8bf3d601bec0099252c8ee2f54e5571385f1a/src/SMAPI/Context.cs]
  (confidence: **high**)
- Connection lifecycle events available: `PeerContextReceived`, `PeerConnected`, `PeerDisconnected` —
  [source: SMAPI events docs (host not on safe list)] (confidence: **medium**)

### Content registration from C# does not require Content Patcher

- Items can be added to `Data/Objects` purely via `AssetRequested` +
  `asset.AsDictionary<string, string>()`, the same mechanism already used at `ModEntry.cs:118` for
  `Data/CraftingRecipes` — [source: SMAPI Content API docs (host not on safe list)] (confidence:
  **medium**)
- Machines use the identical pattern via `Data/Machines` — [source: SMAPI Machines docs (host not on
  safe list)] (confidence: **medium**)
- Reference implementations named by the research agent: `ContentPatcher`, `Automate`,
  `CustomFarmingNPCs` — [source: https://github.com/Pathoschild/StardewMods]. **Caveat**: the agent
  explicitly marked these as not verified by direct file read (confidence: **low**)
- Stardew 1.6 uses 1-based string item IDs with a qualified prefix, e.g. `(O)128`; custom IDs should
  carry a mod-unique prefix — [source: SMAPI Items docs (host not on safe list)] (confidence:
  **medium**)

### Internal prior art: the state model is already farm-keyed

- All persisted state is keyed by **farm/location, not player** — `SoilHealthService` caches
  `Dictionary<string /*locationName*/, Dictionary<Point, float>>`; `CompostingBinService` uses the
  same shape; keys derive from `Constants.SaveFolderName` via `SaveIdProvider` — [source:
  `LivingRoots/Services/SoilHealthService.cs:17`, `LivingRoots/Services/SaveIdProvider.cs:23`] (confidence: **high**)
- Nothing anywhere in the codebase derives state from `Farmer.UniqueMultiplayerID` or any per-player
  identity — [source: repo-wide search] (confidence: **high**)
- **This is a significant finding for Principle VI compliance**: the domain model already treats soil
  health as a property of the farm. The constitution's rationale ("soil health is a property of the
  farm") matches what the code already does.

### Internal prior art: zero multiplayer primitives are used

- `Context` is never imported. No occurrence of `Game1.multiplayer`, `Context.IsMainPlayer`,
  `Context.ScreenId`, `Game1.farmers`, `Game1.netWorldState`, `UniqueMultiplayerID`, or `WarpFarmer`
  anywhere in `LivingRoots/` — [source: repo-wide search of production code] (confidence: **high**)
- All player access is local-only: `Game1.player` at `PlayerProvider.cs:13`,
  `PlayerInventory.cs:16`, `ModController.cs:859` — [source: those files] (confidence: **high**)

## Market & Context

- The mod is pre-release (`manifest.json` version `0.0.1`, UniqueID `lfgranja.LivingRoots`, no Content
  Patcher dependency) with no published release — [source: `LivingRoots/manifest.json`] (confidence: **high**)
- There is no installed base to disrupt and no user base to migrate. Multiplayer work has **no
  backward-compatibility surface to protect in practice**, though Principle VI still mandates it in
  principle — [source: `LivingRoots/manifest.json`, git history] (confidence: **high**)
- CI currently runs **no build/test gate** (requires `vars.BUILDDEPS_ENABLED == 'true'` plus a
  private release asset), so a large multiplayer change would land without automated verification —
  [source: `.github/workflows/build_and_test.yml`] (confidence: **high**)

## Data & Constraints

### Hard constraints on any multiplayer design

- **Persistence transport is host-local.** State is written via `IModHelper.Data.WriteJsonFile` to
  `data/{key}.json` (`ModDataService.cs:55`, `:379-386`). On a farmhand this resolves to that
  machine's local save folder, which the game does **not** synchronize. Consequence: host and
  farmhand diverge silently, with no exception raised — [source: `LivingRoots/Services/ModDataService.cs:55`]
  (confidence: **medium** — behaviour depends on SMAPI version; note the asymmetry with the
  `WriteSaveData` guard cited above, which *does* throw)
- **`OnSaving` fires on every machine.** `ModController.OnSaving` (`:745-777`) writes unconditionally,
  so a farmhand's write is silently discarded on rejoin while the host's copy is canonical —
  [source: `LivingRoots/Controllers/ModController.cs:745-777`] (confidence: **high**)
- **`DayStarted` double-processes in multiplayer.** It operates on `Game1.currentLocation` only
  (`ModController.cs:826-843`). With two players on the farm it fires on both machines, so soil decay
  and compost maturation run **N times per day**. It also processes whatever location the *local*
  player occupies, so a farmhand in the mine advances mine tiles — [source:
  `LivingRoots/Controllers/ModController.cs:826-843`] (confidence: **high**)
- **`OnButtonPressed` mutates local state only.** `Game1.player.CurrentItem` (`:859`) and
  `Game1.player.addItemToInventoryBool` (`PlayerInventory.cs:16`) mean a farmhand applying compost
  changes only their own cache; the host never learns of it — [source: those files] (confidence: **high**)
- **`Game1.playSound` is local-only** and is not broadcast to other players — [source:
  `CompostingBinService.cs:91,126`, `CompostApplicationService.cs:25,33,41,52`] (confidence: **medium**)

### Testability constraints

- **Existing test doubles cannot simulate a farmhand.** `PlayerProviderStub` sets only a single
  `CurrentPlayer`; `GameStateFixture` installs `Game1.content`, `objectData`, `ItemRegistry`, and
  `sounds` but **never** `Game1.multiplayer` or `Game1.farmers`; no `IModHelper.Context` mock exists
  anywhere in the suite — [source: `LivingRoots.Tests/Stubs/`, `LivingRoots.Tests/Fixtures/GameStateFixture.cs:129-153`]
  (confidence: **high**)
- Estimated cost to make multiplayer testable under TDD (Principle IV): **200-400 LOC of new test
  helpers**, plus `GameStateFixture` changes — [source: research estimate, flagged as ASSUMPTION]
  (confidence: **low**)
- `Farmer` construction is currently not achievable in tests because its constructor dereferences
  sprite data unavailable in the test environment — [source:
  `LivingRoots.Tests/Fixtures/GameLocationFixture.cs` notes] (confidence: **medium**)

### Governance constraints

- **Eight spec/plan documents still assert single-player-only scope**, directly contradicting binding
  governance: `specs/001/spec.md:110,147,178,190`; `specs/001/.../plan.md`; `specs/002/spec.md:214`;
  `specs/002/plan.md:21,30`; `specs/003/plan.md:21` — [source: those files] (confidence: **high**)
- `specs/001/spec.md:190` lists "Multiplayer synchronization of visualization settings" under
  **Out of Scope** — [source: that file] (confidence: **high**)
- Two design docs also frame multiplayer as a future concern:
  `docs/design-system/REALITY-CHECK-EVALUATION.md:40`,
  `docs/design-system/LIVING-ROOTS-UI-UX-DESIGN-SYSTEM.md:228,249` — [source: those files]
  (confidence: **high**)

### Effort signals (raw numbers)

| File | LOC |
|---|---|
| `Controllers/ModController.cs` | 1,178 |
| `Services/SoilHealthService.cs` | 977 |
| `Services/ModDataService.cs` | 548 |
| `Services/Visualization/VisualizationService.cs` | 430 |
| `Services/CompostingBinService.cs` | 261 |

Suite total: **816 tests passing** (verified by execution this session, not read from docs).
D3-sized fix estimates from the internal audit: `SetSoilHealth` wiring 10-20 LOC; `Data/Objects` +
`Data/Machines` handler 30-50 LOC; visualization data pipeline 40-80 LOC; `Initialize()` wiring
40-80 LOC — [source: internal audit, ASSUMPTION-grade estimates] (confidence: **low**)

## Evidence Against the Idea

Recorded because the ranking must not be decided by enthusiasm.

1. **The idea's own premise is now invalid.** The intake asked "next 80/20" assuming multiplayer was
   out of scope. It is now the *only* mandated capability. Any ranking produced from the stale
   premise would be wrong — [source: constitution v2.0.0 vs `intake.md`] (confidence: **high**)

2. **Multiplayer is a poor "small effort" candidate.** It has no existing primitives in the codebase,
   no farmhand test doubles, and at least two latent correctness bugs (N-times daily decay; silent
   state divergence) that must be found and fixed, not merely extended. Calling it 80/20 misreads it —
   [source: internal audit + Constitution Gate 3] (confidence: **high**)

3. **Green tests are actively misleading right now.** 816 tests pass over a mod with no playable
   surface. Test count is not evidence of working software here, so any effort estimate anchored on
   "we already have good coverage" is unsound — [source: verified build/test run + `ModEntry.cs:114-130`]
   (confidence: **high**)

4. **Principle VI cannot be verified end-to-end yet.** Gate 3 now requires a second connected player
   observing consistent values. With no `Data/Objects` handler there is nothing to observe, so the
   mandated verification is currently impossible — [source: Constitution Gate 3 + `ModEntry.cs:114-130`]
   (confidence: **high**)

5. **Zero external demand evidence was found.** No third-party signal supports multiplayer as a
   *player-valued* feature — only first-party intent. If the mod's audience is primarily solo players,
   D1 has low external value despite high internal mandate — [source: absence of research results]
   (confidence: **low**, absence of evidence)

6. **Reconciling stale specs is prerequisite work.** Eight spec/plan documents must be corrected
   before D1 can be specified coherently; that cost is not attributable to any single feature —
   [source: cited spec lines] (confidence: **high**)

7. **Migrating persistence risks Principle VI's own save-compatibility clause.** `WriteJsonFile` and
   `WriteSaveData` have different host-authority semantics; changing the transport could invalidate
   existing saves, which Principle VI forbids — [source: SMAPI `DataHelper.cs` vs
   `ModDataService.cs:55`] (confidence: **medium**)

8. **The visualization layer throws every frame.** `VisualizationService.Initialize()` is never called
   in production, so `GetTexture()` throws `InvalidOperationException`, swallowed at
   `ModController.cs:800`. A separate static (`VisualizationTextures.WhiteTexture`) is set but never
   read. This is a pre-existing defect independent of multiplayer — [source:
   `LivingRoots/Services/Visualization/VisualizationService.cs:37,44`] (confidence: **high**)

## Gaps & Open Questions

- [NEEDS CLARIFICATION: `WriteJsonFile` vs `WriteSaveData` host-authority semantics on the exact
  SMAPI version this project pins. The guard is confirmed on `WriteSaveData`; whether it applies to
  `WriteJsonFile` determines whether a farmhand throws or silently diverges. Must be verified against
  the pinned SMAPI before designing D1.]
- [NEEDS CLARIFICATION: is host-authoritative read sufficient, or must farmhands see live tile health
  without waiting for a save/load cycle? The latter forces a `SendMessage` design and materially
  raises effort.]
- [NEEDS CLARIFICATION: should a farmhand be able to *write* soil health, or only the host? Principle
  VI says state is host-authoritative but does not say whether to route client intent to the host or
  deny it.]
- [NEEDS CLARIFICATION: visual rendering for farmhands — should a farmhand see soil overlays at all,
  or is visualization host-only?]
- [NEEDS CLARIFICATION: is a real second machine available for Gate 3 verification, or is a split-screen
  / localhost second client sufficient?]
- [NEEDS CLARIFICATION: does Principle VI require multiplayer *now* (re-prioritizing the roadmap), or
  does it require only that nothing be designed single-player-only while multiplayer arrives later?]
- [NEEDS CLARIFICATION: which of D1/D3 should be ranked first — the constitutional mandate, or the
  playable-surface prerequisite? This is a judgement call, not an evidence question.]
- [NEEDS CLARIFICATION: no `dotnet format --verify-no-changes` result was obtained this session
  (exceeded a 5-minute timeout), so the Gate 1 formatting gate is unverified.]

## Sources

- https://github.com/Pathoschild/SMAPI/blob/e5b8bf3d601bec0099252c8ee2f54e5571385f1a/src/SMAPI/Framework/ModHelpers/DataHelper.cs (host: github.com, policy: allowlisted)
- https://github.com/Pathoschild/SMAPI/blob/e5b8bf3d601bec0099252c8ee2f54e5571385f1a/src/SMAPI/Context.cs (host: github.com, policy: allowlisted)
- https://github.com/Pathoschild/StardewMods (host: github.com, policy: allowlisted)
- https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Multiplayer (host: stardewvalleywiki.com, policy: **auto-refused — host not on safe list**; retrieved by a background research agent, therefore `[UNVERIFIED]` pending re-verification against the allowlisted SMAPI source above)
- https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Content (host: stardewvalleywiki.com, policy: **auto-refused — host not on safe list**; same caveat as above)
- Repository files under `/home/luis/development/stardew-raizes-vivas` (host: local, policy: read-only, in-project)
- Verified execution this session: `dotnet build --configuration Release` (0 warnings, 0 errors) and `dotnet test --no-build --configuration Release` (816 passed, 0 failed), using `GamePath=/tmp/opencode/gamepath`
