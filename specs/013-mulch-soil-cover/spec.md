# Feature Specification: Mulch / Soil Cover (Epic 4)

**Feature Branch**: `013-mulch-soil-cover`

**Created**: 2026-10-30

**Status**: Draft — promoted from assessment `mulch-soil-cover` (verdict: go)

**Input**: User description: "Mulch / Soil Cover — cobertura morta aplicada em tile arado; zera/reduz decay do solo por 7 dias; 1 objeto + 1 receita + 1 hook em SoilDecayService. Briefing: specs/future-roadmap/briefings/mulch-soil-cover.md. Assessment: .specify/assessments/mulch-soil-cover/ (intake, research, problem, concept, decision — all Go)."

## Clarifications

### Session 2026-10-30

- Q: MulchDecayMultiplier value (0f vs 0.25f)? → A: 0f for Y1 (full protection), subject to playtesting; partial protection (0.25f) deferred to a balance pass.
- Q: Elena dependency (shop sale)? → A: Shop sale criterion deferred; craftable recipe (fiber ×10) is the primary path. Elena shop sale is a non-blocking acceptance criterion.
- Q: Asset creation? → A: `mulch.png` 16×16 must be created; placeholder viable with specified colors (#92400E/#A16207/#78716C) until artist is available.
- Q: 7-day duration? → A: Fixed at 7 days for Y1; configurability deferred.
- Q: Weed prevention scope? → A: Visual/secondary in Y1; core mechanic deferred.
- Q: Overlay z-order? → A: To be verified during implementation; must not conflict with spec 001 health overlays.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Apply Mulch to Protect Soil (Priority: P1)

As a farmer, I want to apply mulch to a tilled tile so that its soil health stops decaying, so that I can leave soil fallow between plantings without losing health.

**Why this priority**: This is the core value — the first preventive tool for soil health. Without it, soil health is a treadmill (lose 2/day, recover with compost). Mulch converts soil health from reactive to managed.

**Independent Test**: Craft mulch with 10 fiber, apply to a tilled tile, advance 7 days, confirm soil health unchanged; on day 8, decay resumes.

**Acceptance Scenarios**:

1. **Given** a tilled tile with soil health 50, **When** the player applies mulch, **Then** the tile's decay rate becomes 0 for 7 days and soil health remains 50.
2. **Given** a mulched tile, **When** 7 days pass, **Then** mulch expires and decay resumes at the normal rate (respecting seasonal multiplier).
3. **Given** a mulched tile, **When** rain occurs, **Then** mulch is not removed and decay remains 0.
4. **Given** a non-tilled tile, **When** the player attempts to apply mulch, **Then** the message `message.mulch.invalid-tile` is shown and no mulch is consumed.
5. **Given** an already-mulched tile, **When** the player attempts to apply mulch again, **Then** the message `message.mulch.already` is shown and no mulch is consumed.

### User Story 2 - Craft Mulch Early (Priority: P1)

As a Year 1 farmer, I want to craft mulch from fiber on day 1, so that I can protect soil before any crops are planted.

**Why this priority**: Accessibility — mulch must be available from the start of the game (fiber = weeding, accessible day 1) to be useful as a preventive tool.

**Independent Test**: With 10 fiber in inventory, craft mulch at the crafting menu; confirm 1 mulch item is produced and 10 fiber consumed.

**Acceptance Scenarios**:

1. **Given** 10 fiber in inventory, **When** the player crafts mulch, **Then** 1 mulch item is produced and 10 fiber consumed.
2. **Given** fewer than 10 fiber, **When** the player opens the crafting menu, **Then** the mulch recipe is visible but not craftable.
3. **Given** the mulch recipe, **When** `dotnet format --verify-no-changes` runs, **Then** it passes clean.

### User Story 3 - Visual Mulch Indicator (Priority: P2)

As a farmer, I want to see which tiles are mulched, so that I can manage my soil protection visually.

**Why this priority**: Visual feedback is essential for the player to know which tiles are protected. Without it, mulch is invisible and unmanageable.

**Independent Test**: Apply mulch to a tile; confirm the tile overlay shows a brown tint (`MulchColor`) with `Dots` pattern at 0.5 opacity.

**Acceptance Scenarios**:

1. **Given** a mulched tile, **When** the overlay renders, **Then** the tile shows a brown tint (`MulchColor` = #92400E) at 0.5 opacity with `Dots` pattern.
2. **Given** a mulched tile with a plant, **When** the overlay renders, **Then** the tile shows a dotted border (mulch + plant indicator).
3. **Given** the overlay, **When** rendered, **Then** the mulch color is applied via `IVisualizationService` (never hardcoded in controller).

### Edge Cases

- What happens when mulch is applied to a tile with an existing plant? → Mulch applies to tilled soil; if a plant is present, mulch surrounds it (dotted border indicator).
- What happens when the player re-applies mulch before expiry? → `message.mulch.already` shown; no consumption, no reset.
- What happens when mulch expires during a season transition? → Mulch persists across seasons (rain doesn't remove); expiry is day-based, not season-based.
- What happens when the tile is tilled again while mulched? → Mulch is removed (re-tilling clears the tile state).
- What happens in multiplayer when a farmhand applies mulch? → Host-authoritative: farmhand's action is mediated by host; lock per tile (Principle VI).
- What happens when `MulchDecayMultiplier = 0f` and seasonal multiplier is 1.5× (Summer)? → Decay is 0 (0 × 1.5 = 0). Mulch fully protects regardless of season.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST register a `LivingRoots_Mulch` object in `Data/Objects` with Category −26, ContextTags `{"mulch_item", "compostable_item"}`, Price 5, Texture `Mods/LivingRoots/Mulch`, SpriteIndex 0.
- **FR-002**: System MUST register a crafting recipe `LivingRoots_MulchRecipe` in `Data/CraftingRecipes` using the legacy string format: `"771 10/Home/LivingRoots.Mulch 1/false/null/Mulch"` (Fiber ×10).
- **FR-003**: System MUST define `IMulchApplicationService` in `Domain/Interfaces/` with an implementation in `Services/`, following the DDD pattern (interface in Domain, impl in Services).
- **FR-004**: System MUST provide a new constant `MulchItemId = "LivingRoots.Mulch"` and `QualifiedMulchItemId = "(O)LivingRoots.Mulch"` in `ModConstants`.
- **FR-005**: System MUST provide `MulchDurationDays = 7` constant in `ModConstants`.
- **FR-006**: System MUST provide `MulchDecayMultiplier = 0f` constant in `ModConstants` (Y1 value; subject to playtesting).
- **FR-007**: System MUST provide `MulchTag = "mulch_item"` constant in `ModConstants`.
- **FR-008**: System MUST provide `MulchColor = #92400E` constant in `ModConstants` (146, 64, 14).
- **FR-009**: `ISoilDecayService` MUST check `tile.HasMulch` and apply `decay *= MulchDecayMultiplier` when mulch is present.
- **FR-010**: Mulch state (`HasMulch` + days remaining) MUST be persisted via `ModDataService` with `*_KeyPrefix` keys, using sparse cache (zeros omitted), respecting `MaxTilesPerLocation`/`MaxTilesPerSave`.
- **FR-011**: System MUST hook mulch application in `ModController.OnButtonPressed` (following the compost pattern at `ModController.cs:845`), guarded by `ExecuteWithConcurrencyGuard`.
- **FR-012**: System MUST show `message.mulch.invalid-tile` when mulch is applied to a non-tilled tile.
- **FR-013**: System MUST show `message.mulch.already` when mulch is applied to an already-mulched tile.
- **FR-014**: System MUST render mulched tiles with a brown tint (`MulchColor`) at 0.5 opacity (`DefaultOpacity`) with `Dots` pattern via `IVisualizationService`.
- **FR-015**: System MUST use vanilla sounds: `hoeHit` or `sandyStep` for application, `axe` for crafting. No custom `.ogg` in Y1.
- **FR-016**: System MUST add i18n keys to `en.json` and `default.json`: `mulch.name`, `mulch.description`, `message.mulch.invalid-tile`, `message.mulch.already`.
- **FR-017**: System MUST follow DDD: `IMulchApplicationService` in `Domain/Interfaces/`, implementation in `Services/`, `new` only in `ModEntry.cs`.
- **FR-018**: System MUST follow multiplayer (Principle VI): state farm-keyed via `SaveIdProvider`, host-authoritative writes, `Context.IsMainPlayer` guard, no `.Result`/`.Wait()`, no lock across I/O, logs without raw messages.
- **FR-019**: System MUST respect `CheckForOverflowUnderflow=true` project-wide.
- **FR-020**: Mulch recipe MUST be available for crafting with fiber on day 1 (no Elena dependency for the craftable path).

### Key Entities

- **Mulch (item)**: `LivingRoots_Mulch`, Category −26, craftable, 16×16 sprite, ContextTags `{"mulch_item", "compostable_item"}`.
- **Mulch tile state**: `HasMulch` (bool) + `DaysRemaining` (int), persisted per-tile via `ModDataService`, farm-keyed via `SaveIdProvider`.
- **IMulchApplicationService**: service contract for applying mulch to a tile, validating tile state, and consuming the item from inventory.

### Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `dotnet build Stardew-LivingRoots.sln -c Release` passes with 0 warnings and 0 errors after implementation.
- **SC-002**: `dotnet test` passes with 0 failures (baseline 1436 + new mulch tests).
- **SC-003**: `dotnet format --verify-no-changes` passes clean.
- **SC-004**: A tilled tile with mulch applied maintains soil health for 7 in-game days (decay = 0).
- **SC-005**: After 7 days, mulch expires and decay resumes at the normal rate (respecting seasonal multiplier).
- **SC-006**: Rain does not remove mulch or reset the duration.
- **SC-007**: Applying mulch to a non-tilled tile shows `message.mulch.invalid-tile` and consumes no item.
- **SC-008**: Applying mulch to an already-mulched tile shows `message.mulch.already` and consumes no item.
- **SC-009**: The mulch overlay uses `MulchColor` (#92400E) at 0.5 opacity with `Dots` pattern, applied via `IVisualizationService`.
- **SC-010**: All new tests follow the one-test-file-per-subject convention (`MulchApplicationServiceTests.cs`, etc.).
- **SC-011**: No global `NoWarn`; all suppressions are scoped and justified.

## Assumptions

- Target users are Stardew Valley players using Living Roots who leave tiles fallow between crops.
- Mobile support is not in scope (Stardew Valley is PC-focused).
- The existing authentication/permission system (SMAPI multiplayer) will be reused for host-authority guards.
- `MulchDecayMultiplier = 0f` for Y1; partial protection (0.25f) is deferred to a balance pass if playtesting shows 0f is overpowered.
- Elena NPC is not implemented; the shop sale criterion is deferred. The craftable recipe (fiber ×10) is the primary acquisition path.
- The `mulch.png` sprite will be created with the specified colors; a placeholder is acceptable for development.
- Existing soil health system (spec 001, 002, 005) is fully functional and will be reused.
- `ModDataService` sparse cache pattern (zeros omitted) will be reused for mulch state persistence.
- `GamePath` is set for local dev; CI `build-analysis` job materialises build-deps via `BUILDDEPS_TOKEN`.