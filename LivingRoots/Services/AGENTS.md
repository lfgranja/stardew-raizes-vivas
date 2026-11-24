# SERVICES

Application services — implement domain interfaces, integrate with SMAPI, manage persistence.

## FILES

| File | Purpose |
|------|---------|
| `SoilHealthService.cs` | Load/save/query soil health. Runtime cache: `Dictionary<string, Dictionary<Point, float>>`. Sparse cache (no zero values). DoS protection via tile limits. Snapshot-outside-lock. |
| `CompostingBinService.cs` | State machine: Empty → Processing → Ready. Validates organic waste via `IOrganicWasteValidator`. |
| `CompostApplicationService.cs` | Apply compost to tiles. Validates farm/Greenhouse, HoeDirt terrain, health cap, held item. |
| `SoilDecayService.cs` | Daily decay at day start. Uses `SeasonalDecayMultiplier` domain service. Greenhouse exempt. |
| `ModDataService.cs` | SMAPI data persistence. Path sanitization, TOCTOU race prevention, no raw exception messages in logs. |
| `PathTraversalValidator.cs` | Adapter → domain `IPathValidationService` |
| `UnicodeNormalizer.cs` | Adapter → domain `IUnicodeNormalizationService` |
| `FileNameSanitizer.cs` | Adapter → domain `IFileNameSanitizationService` |
| `SaveIdProvider.cs` | Abstraction over `Constants.SaveFolderName` with length validation. |
| `CompostingBinFactory.cs` | Creates `CompostingBinStateModel` instances with default state. |

## CONVENTIONS

- **Adapter pattern**: Security validators delegate to domain services — no duplicated logic.
- **Constructor injection** for all services.
- **Sparse cache**: `SoilHealthService` omits zero/default health values to keep saves small.
- **Snapshot outside lock**: Capture data, release lock, then perform I/O.
- **Fail-fast**: Return `null` for expected failures (invalid saveId, sanitization failure); don't throw.
- **Data key format**: `"data/{sanitizedKey}.json"` via `IModHelper.Data`.
- **Log security**: Generic messages only — never embed raw exception content.

## ANTI-PATTERNS

- DO NOT use `.Result` or `.Wait()` on async calls.
- DO NOT hold lock during I/O operations.
- DO NOT expose raw exception messages in logs.
- DO NOT create files outside SMAPI data API.
