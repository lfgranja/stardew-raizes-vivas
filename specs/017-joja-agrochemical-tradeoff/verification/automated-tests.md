# Automated Test Evidence — Spec 017 (Joja / Agrochemical MVP)

**Date:** 2026-11-06  
**Configuration:** `Release`  
**Build dependency:** `GamePath=/home/luis/stardew-assemblies`  
**Scope:** automated unit/controller/property tests only. No real game process, real save/load, or
Gate 3 evidence is claimed here.

## T048 — targeted 017 suite

**Exact command:**

```bash
GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release \
  --filter "FullyQualifiedName~Agrochemical|FullyQualifiedName~AgrochemicalPersistence|FullyQualifiedName~ModControllerAgrochemical|FullyQualifiedName~AgrochemicalContentParity" \
  --logger "console;verbosity=normal"
```

**Result:**

| Measure | Value |
|---|---:|
| Total | 60 |
| Passed | 60 |
| Failed | 0 |
| Skipped | 0 |
| Exit code | 0 |

**Source log:** `specs/017-joja-agrochemical-tradeoff/verification/automated-tests-targeted.log`

**Test inventory:**

| Test class | Cases | Result |
|---|---:|---|
| `LivingRoots.Tests.AgrochemicalApplicationServiceTests` | 13 | 13 passed |
| `LivingRoots.Tests.AgrochemicalPersistenceTests` | 3 | 3 passed |
| `LivingRoots.Tests.ModControllerAgrochemicalTests` | 19 | 19 passed |
| `LivingRoots.Tests.AgrochemicalContentParityTests` | 25 | 25 passed |

This supports the `automated` service/controller/content-parity claims only. It does not support
host-only machine-pass, real-game persistence, D1, human verification, or SC-004.

## T050 — full regression suite

**Exact command:**

```bash
GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release \
  --logger "console;verbosity=minimal"
```

**Result on the final tree:**

| Measure | Value |
|---|---:|
| Total | 1816 |
| Passed | 1812 |
| Failed | 0 |
| Skipped | 4 |
| Exit code | 0 |

**Source log:** `specs/017-joja-agrochemical-tradeoff/verification/automated-tests-full.log`

**Skipped tests (pre-existing, unrelated to 017):**

- `LivingRoots.Tests.Invariants.T114_T126_RedPhaseTests.T116_HarvestQuality_Poor_Soil_Quality_Zero_Hook`
- `LivingRoots.Tests.Invariants.T114_T126_RedPhaseTests.T117_HarvestQuality_Healthy_Soil_Quality_Boost_Hook`
- `LivingRoots.Tests.Invariants.T114_T126_RedPhaseTests.T118_ExtraHarvest_Prob_Poor_Soil_Zero_Hook`
- `LivingRoots.Tests.Invariants.T114_T126_RedPhaseTests.T119_ExtraHarvest_Prob_Healthy_Soil_Increased_Hook`

These are explicitly marked `Skip = "Deferred: harvest quality/extra harvest out of scope for spec
009 verified claim"` in `LivingRoots.Tests/Invariants/T114_T126_RedPhaseTests.cs`. They are not 017
tests and were not changed by this feature.

### Interim failure and correction (provenance note)

An earlier full-suite run in this same verification pass reported **1 failure**:

```text
LivingRoots.Tests.ProductionSourceGuardTests.ProductionSources_NoUnreferencedConstantsSurvive
Unreferenced constants survive in LivingRoots/Constants.cs (expected zero):
AgrochemicalItemNameKey, AgrochemicalItemDescriptionKey.
```

That was a genuine 017 defect: the two translation-key constants were declared but not referenced
by production code. It was fixed in commit `5dd838d` by using the keys in the localized
`Data/Objects` item registration (`LocalizeOrFallback` in `LivingRoots/ModEntry.cs`). The failed run
is **not** a pass and is not the record above; the final-tree run is the evidence. The correction
was followed by a fresh targeted run, a fresh persistence run, and a fresh full-suite run.

## Classification

- Both runs are `automated` evidence under the 017 claim schema.
- No `host-only-machine-pass`, `human-verified`, `d1-farmhand-convergence`, or
  `qualitative-follow-up` claim is supported by these runs.
- The mocked persistence tests are covered separately in
  `specs/017-joja-agrochemical-tradeoff/verification/persistence-roundtrip.md`.
- Full-suite success means the repository's automated suite is green with 017 included; it does
  not prove real-game behavior or final balance.
