# Quickstart Validation Guide: 017 Joja / Agrochemical Tradeoff (MVP)

**Purpose**: Runnable validation scenarios for implementation and review. This is a validation/run guide, not implementation code.

## Prerequisites

- Repo: `/home/luis/development/stardew-raizes-vivas`
- .NET 6 SDK; Stardew/SMAPI build dependencies available via `GamePath` as in `AGENTS.md`.
- Feature branch/worktree aligned to `017-joja-agrochemical-tradeoff` spec/plan.
- Spec/contracts read: `spec.md`, `research.md`, `data-model.md`, `contracts/`.

## Static and automated checks (Gate 1 foundation)

```bash
GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release
dotnet format Stardew-LivingRoots.sln --verify-no-changes
```

**Expected:** tests pass (including new 017 unit/controller/invariant tests), no format drift. Record exact counts; do not claim verification beyond what ran.

### Targeted test runs (after 017 tests exist)

```bash
GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release \
  --filter "FullyQualifiedName~Agrochemical"
GamePath=/home/luis/development/stardew-raizes-vivas && GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release \
  --filter "FullyQualifiedName~ModController|FullyQualifiedName~SoilHealthService"
```

Adjust filters to actual 017 test names.

## Validation scenarios

### S1 — Unknown/`NaN` baseline applies explicit measured penalty (C2)
- **Setup:** soil target has no measured record (`NaN`/unknown).
- **Action:** host applies Joja/agrochemical input successfully.
- **Expected:** baseline treated as `MaxSoilHealth` (100); post-penalty `90` for provisional `P=10`; value is explicitly measured/stored (not Unknown, no invisible no-op). Soil remains within 0–100.
- **Evidence:** unit test asserts `SetSoilHealth` absolute write to expected value and no negative `UpdateHealth` path for unknown.

### S2 — Measured soil penalty is bounded and attributable (C1, C3)
- **Setup:** measured soil (e.g., 50, 31, 71, 0).
- **Action:** host applies successfully.
- **Expected:** `clamp(current - 10, 0, 100)` recorded; immediate shortcut confirmation feedback shown; no change to 014/015 quality/growth mechanics as an added MVP buff and no vanilla fertilizer-tier interaction.
- **Evidence:** unit/controller tests for bounds, clamp, and message/confirmation contract.

### S3 — Unsupported farmhand path refused safely (C4)
- **Setup:** multiplayer context where farmhand attempts MVP unsupported application.
- **Action:** farmhand uses agrochemical input.
- **Expected:** explicit localized refusal **before** soil mutation, item consumption, or false-success feedback; no authoritative/local soil mutation; no FR-009 misleading success side effects.
- **Evidence:** controller tests with `IsMainPlayerCheck=false`/MP context mocks.

### S4 — Host authority, lock, and FR-009 sync (MP contract)
- **Setup:** host eligible target.
- **Action:** host applies; also test contention/conflict case.
- **Expected:** lock acquired/released; on success soil committed and host snapshot broadcast after commit; on conflict explicit refusal with no unsafe mutation/consumption.
- **Evidence:** controller tests mocking `IHostAtomicLockService`, `IMultiplayerHelper`, `ISoilHealthService`.

### S5 — Refusals and ineligible targets
- **Setup:** ineligible target, missing item, unsupported location/state.
- **Expected:** explicit refusal, no soil mutation, no item consumption, no false success.
- **Evidence:** service/controller tests.

### S6 — Localization parity
- **Check:** new 017 player-facing keys exist in `LivingRoots/i18n/default.json`, `en.json`, `pt.json`.
- **Expected:** parity for touched keys; before-use framing and after-use/refusal messages are clear and cozy/non-punitive.

## Host-only Gate 3 (optional, if runner/scenario added later)
If implementation adds a headless verification scenario, run via `scripts/verify-scenario.sh` following repo protocol and record 3 required signals. Classify result as **host-only `machine-pass` only** unless separate human `verified`/D1 attestation exists.

## Not validated by this MVP quickstart
- Real-farmhand D1 convergence.
- Human `verified` attestation.
- Vanilla fertilizer-hook/Harmony behavior (explicitly out of MVP).
- Monoculture.
- Final balance/tuning (`P=10` provisional).