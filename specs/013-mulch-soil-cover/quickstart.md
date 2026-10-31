# Quickstart: Mulch / Soil Cover (Spec 013)

## Prerequisites

- .NET 6 SDK + SMAPI + Stardew Valley (for in-game testing)
- GamePath set in `LivingRoots.csproj` (local dev only)
- `dotnet format` clean baseline

## Validation Scenarios

### 1. Craft mulch
1. Start a new game (day 1).
2. Gather 10 fiber (weed any grass).
3. Open crafting menu → craft "Mulch".
4. **Expected**: 1 mulch item produced, 10 fiber consumed.

### 2. Apply mulch to tilled tile
1. Till a tile.
2. Hold mulch, right-click on tilled tile.
3. **Expected**: mulch consumed, tile shows brown tint (MulchColor) at 0.5 opacity with Dots pattern, `hoeHit`/`sandyStep` sound plays.

### 3. Verify 7-day protection
1. Apply mulch to a tilled tile with soil health 50.
2. Advance 7 days (no crops planted).
3. **Expected**: soil health remains 50 (decay = 0).

### 4. Verify expiry
1. After 7 days, advance 1 more day.
2. **Expected**: mulch expires, decay resumes at normal rate (respecting seasonal multiplier).

### 5. Verify rain resistance
1. Apply mulch, then let a rainy day pass.
2. **Expected**: mulch persists, decay remains 0.

### 6. Verify invalid-tile message
1. Attempt to apply mulch to a non-tilled tile (grass, stone, water).
2. **Expected**: `message.mulch.invalid-tile` shown, no mulch consumed.

### 7. Verify already-mulched message
1. Apply mulch to a mulched tile.
2. **Expected**: `message.mulch.already` shown, no mulch consumed.

### 8. Verify mulch on planted tile
1. Plant a crop on tilled soil.
2. Apply mulch to the same tile.
3. **Expected**: mulch applies (no invalid-tile message), overlay shows dotted border (mulch + plant).

### 9. Verify build/test/format
```bash
dotnet build Stardew-LivingRoots.sln --configuration Release
dotnet test Stardew-LivingRoots.sln --no-build --verbosity normal
dotnet format Stardew-LivingRoots.sln --verify-no-changes
```
All must pass with 0 errors/warnings/failures.

### 10. Verify multiplayer (stub)
- Construct `ModController` with a mock `IHostAtomicLockService`.
- Apply mulch as farmhand → verify host-mediated, lock acquired per tile.