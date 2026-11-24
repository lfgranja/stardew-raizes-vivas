# Persistence Round-Trip Evidence — Spec 017 (Joja / Agrochemical MVP)

**Date:** 2026-11-06  
**Evidence class:** `automated` (integration-style, mocked persistence seam)  
**Scope:** the existing `ISoilHealthService.SaveData`/`LoadData` seam only. This is **not** a real
game save/load, disk integration, or Gate 3 result.

## Command

```bash
GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release \
  --filter "FullyQualifiedName~AgrochemicalPersistence" \
  --logger "console;verbosity=normal"
```

## Result

| Measure | Value |
|---|---:|
| Total | 3 |
| Passed | 3 |
| Failed | 0 |
| Skipped | 0 |
| Exit code | 0 |

**Source log:** `specs/017-joja-agrochemical-tradeoff/verification/persistence-roundtrip.log`

## Seam used

`AgrochemicalApplicationService` mutates the real `SoilHealthService` runtime cache through
`ISoilHealthService.SetSoilHealth`. The test then calls:

1. `SoilHealthService.SaveData(saveId)`, with a mocked `IModDataService` that captures the
   `SoilHealthState` passed to `SaveData`.
2. A fresh `SoilHealthService.LoadData(saveId)`, with the same mocked `IModDataService` returning
   the captured state.

No file, real save folder, or game process is involved. The mocked seam is the same pattern used
by the existing `SaveReloadConsistencyTests`.

## Assertions

### C2 unknown baseline persists as measured 90

`UnknownBaseline_Apply_SaveReload_PersistsMeasuredNinety`:

- Starts with an unknown (`NaN`) soil value and no measured entry.
- Applies the service; asserts `BaselineWasUnknown == true`, `BaselineUsed == 100`,
  `PostPenalty == 90`, and the live value is `90` (not `NaN`).
- Saves; asserts the captured state contains `"Farm"["10,10"] == 90`.
- Reloads into a fresh service; asserts `GetSoilHealth` returns `90` and `TryGetSoilHealth`
  succeeds with `90` (measured, not unknown).
- Asserts the value is within `[MinSoilHealth, MaxSoilHealth]`.

This is the required T049 C2 assertion for `P=10`.

### Measured baseline persists as measured 40

`MeasuredBaseline_Apply_SaveReload_PersistsMeasuredForty`:

- Starts at measured `50`; applies; asserts `BaselineWasUnknown == false`, `BaselineUsed == 50`,
  `PostPenalty == 40`, and the live value is `40`.
- Saves; asserts the captured state contains `40`.
- Reloads; asserts the measured value is `40` and `TryGetSoilHealth` succeeds.

### Sparse-zero scope

`SubPenaltyBaseline_Apply_Save_OmitsZeroFromSparseState_AndReloadsUnknown`:

- Starts at measured `5`; applies; asserts the bounded result is `PostPenalty == 0`,
  `AppliedPenalty == 5`, `WasClamped == true`, and the live value is an explicit `0`.
- Saves; asserts the sparse state **omits** the zero entry.
- Reloads; asserts the value is `NaN` and `TryGetSoilHealth` fails (unknown).

This documents an existing `SoilHealthService` sparse-cache behavior: a measured zero is not
persisted, so a clamped-to-zero tile reloads as unknown. It is **not** a 017 regression and does
not invalidate the C2 `90` result or the measured `40` result. It does mean the persistence claim
must be scoped to non-zero measured values and the tested seam.

## Bounds and scope

- All assertions stay within `ModConstants.MinSoilHealth` (`0`) and `ModConstants.MaxSoilHealth`
  (`100`).
- The penalty is the provisional `ModConstants.AgrochemicalSoilPenalty` (`10`).
- The test covers one location (`Farm`) and one tile (`10,10`); it does not prove multi-location
  or multi-save behavior.
- It does not prove real disk persistence, save-file compatibility, or Gate 3.
- It does not prove D1/farmhand convergence, human verification, or SC-004.
