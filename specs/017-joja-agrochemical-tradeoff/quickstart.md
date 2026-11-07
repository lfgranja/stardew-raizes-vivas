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

**Expected:** tests pass (including new 017 unit/controller/invariant tests), no format drift. Record exact counts; do not claim verification beyond what ran. Such results map to `evidence_class: automated` claims with a concrete `source_artifact_path` and `status: pending` until evidence is recorded.

### Targeted test runs (017 test inventory)

The 017 suite is split across four classes. The combined filter below is the authoritative
targeted command for T048; the RED gate used the same filter (see
`verification/red-phase.md`).

```bash
GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release \
  --filter "FullyQualifiedName~Agrochemical|FullyQualifiedName~AgrochemicalPersistence|FullyQualifiedName~ModControllerAgrochemical|FullyQualifiedName~AgrochemicalContentParity"
```

| Test class | Cases | Covers |
|---|---:|---|
| `LivingRoots.Tests.AgrochemicalApplicationServiceTests` | 13 | Service baseline/penalty/clamp/refusal rules and FsCheck invariants (S1, S2, S5). |
| `LivingRoots.Tests.AgrochemicalPersistenceTests` | 3 | Mocked `ISoilHealthService.SaveData`/`LoadData` seam (S1, S2). |
| `LivingRoots.Tests.ModControllerAgrochemicalTests` | 19 | Authority, ordering, lock contention, consumption, feedback, broadcast/refusal invariants (S3, S4, S5). |
| `LivingRoots.Tests.AgrochemicalContentParityTests` | 25 | Required 017 keys, locale parity, tone/boundary proxies (S6). |

The targeted run currently discovers 60 cases. T048 records the authoritative pass/fail counts
in `verification/automated-tests.md`; do not cite this table as a passing result by itself.

### Verification artifacts and scope

| Artifact | Purpose |
|---|---|
| `verification/claim-records.md` | Claim register; statuses change only after an artifact exists. |
| `verification/automated-tests.md` | T048 targeted results and T050 full-regression results. |
| `verification/persistence-roundtrip.md` | Mocked-seam persistence evidence only; not real disk/save or Gate 3. |
| `verification/static-checks.md` | T051 formatting/build gate. |
| `verification/manual-review.md` | Manual copy semantics/tone review; not feature verification. |
| `verification/provisioning-validation.md` | Validation-only item provisioning decision; no `Data/Shops`. |
| `verification/host-only-gate3.md` | Historical 2026-11-06 infeasibility record (superseded by the Phase 8 run). |
| `verification/host-only-gate3-run.md` | Host-only Gate 3 run record; links the tracked evidence bundle. |
| `evidence-bundle-run-20261107T015936Z-7f35/` | Tracked host-only Gate 3 bundle (full log, three agreeing signals). |
| `verification/sc004-comprehension-protocol.md` | SC-004 protocol; execution stays `pending` unless a real run is appended. |

If a host-only end-to-end run is infeasible or blocked, record it in
`verification/host-only-gate3.md` as `planned-run-not-executed` with the reason and the exact
capability that is missing. Do not invent a scenario, log, bundle, or pass. Lower-level
automated/persistence evidence remains separately claimable.

**017 status (Phase 8):** run performed and passed; see
[`verification/host-only-gate3-run.md`](verification/host-only-gate3-run.md) and
`evidence-bundle-run-20261107T015936Z-7f35/` (`test.md` `machine-pass`, honesty `R`). The run
asserts only `host-only-machine-pass`, never `human-verified` or `d1-farmhand-convergence`, and
does not cover the C2 unknown-`NaN` baseline path (covered by automated tests).

## Validation scenarios

### S1 — Unknown/`NaN` baseline applies explicit measured penalty (C2)
- **Setup:** soil target has no measured record (`NaN`/unknown).
- **Action:** host applies Joja/agrochemical input successfully.
- **Expected:** baseline treated as `MaxSoilHealth` (100); post-penalty `90` for provisional `P=10`; value is explicitly measured/stored (not Unknown, no invisible no-op). Soil remains within 0–100.
- **Evidence:** `AgrochemicalApplicationServiceTests.UnknownBaseline_*` asserts the absolute `SetSoilHealth(90)` write and no negative `UpdateHealth` path for unknown; `AgrochemicalPersistenceTests.UnknownBaseline_*` covers the mocked save/load seam.

### S2 — Measured soil penalty is bounded and attributable (C1, C3)
- **Setup:** measured soil (e.g., 50, 31, 71, 0).
- **Action:** host applies successfully.
- **Expected:** `clamp(current - 10, 0, 100)` recorded; immediate shortcut confirmation feedback shown; no change to 014/015 quality/growth mechanics as an added MVP buff and no vanilla fertilizer-tier interaction.
- **Evidence:** `AgrochemicalApplicationServiceTests.MeasuredBaseline_*`, `MeasuredFive_*`, and `SubPenaltyBaseline_*` cover bounds/clamp; controller feedback tests cover the confirmation/clamped-key contract.

### S3 — Unsupported farmhand path refused safely (C4)
- **Setup:** multiplayer context where farmhand attempts MVP unsupported application.
- **Action:** farmhand uses agrochemical input.
- **Expected:** explicit localized refusal **before** soil mutation, item consumption, or false-success feedback; no authoritative/local soil mutation; no FR-009 misleading success side effects.
- **Evidence:** `ModControllerAgrochemicalTests.TryApplyAgrochemical_OnUnsupportedAuthority_*` and `ForAnyServiceRefusalOutcome_*` with `IsMainPlayerCheck=false`/MP context mocks.

### S4 — Host authority, lock, and FR-009 sync (MP contract)
- **Setup:** host eligible target.
- **Action:** host applies; also test contention/conflict case.
- **Expected:** lock acquired/released; on success soil committed and host snapshot broadcast after commit; on conflict explicit refusal with no unsafe mutation/consumption.
- **Evidence:** `ModControllerAgrochemicalTests.OnHostSuccess_*`, `OnLockContention_*`, and `WithoutLockService_*` mocking `IHostAtomicLockService`, `IMultiplayerHelper`, `ISoilHealthService`.

### S5 — Refusals and ineligible targets
- **Setup:** ineligible target, missing item, unsupported location/state.
- **Expected:** explicit refusal, no soil mutation, no item consumption, no false success.
- **Evidence:** service/controller refusal tests (`InvalidTarget_*`, `MissingInput_*`, `OnServiceConflictRefusal_*`, `OnSoilAtMinimumRefusal_*`, `OnNonFarmLocation_*`, `OnUntilledFarmTile_*`).

### S6 — Localization parity
- **Check:** new 017 player-facing keys exist in `LivingRoots/i18n/default.json`, `en.json`, `pt.json`.
- **Expected:** parity for touched keys; before-use framing and after-use/refusal messages are clear and cozy/non-punitive.
- **Evidence:** `AgrochemicalContentParityTests` covers key presence/parity and lexical tone/boundary proxies. It cannot prove semantic meaning; T052 manual review is separate.

## Host-only Gate 3 (optional, if runner/scenario added later)
If implementation adds a headless verification scenario, run via `scripts/verify-scenario.sh` following repo protocol and record 3 required signals. Record claims using the normalized schema canonical in `contracts/multiplayer-gate3.md`: `claim`, `evidence_class` (`automated` | `host-only-machine-pass` | `human-verified` | `d1-farmhand-convergence` | `qualitative-follow-up` | `planned-run-not-executed`), `source_artifact_path`, `status` (`pending` | `complete` | `blocked` | `infeasible`), and `scope_note`. A passing host-only end-to-end run is `host-only-machine-pass` only — never `human-verified` or `d1-farmhand-convergence`. If a host-only end-to-end run is **infeasible/unavailable**, do not fabricate a run: record a `planned-run-not-executed` claim with `status` `infeasible`/`blocked` plus reason, claim only completed lower-level evidence (e.g., `automated`) separately, and keep `human-verified`/`d1-farmhand-convergence` pending. `planned-run-not-executed` is not a pass. Preserve prior evidence provenance (no rewrites/retroactive promotion).

**017 status:** a host-only Gate 3 run has been performed and passed. See
[`verification/host-only-gate3-run.md`](verification/host-only-gate3-run.md) and the tracked
bundle `evidence-bundle-run-20261107T015936Z-7f35/` (`test.md` `machine-pass`, honesty `R`). This
is `host-only-machine-pass` only: not `human-verified`, not D1, and not SC-004. The earlier
infeasibility record in [`verification/host-only-gate3.md`](verification/host-only-gate3.md) is
kept as historical context and is not the current status.

## Not validated by this MVP quickstart
- `d1-farmhand-convergence` (real-farmhand D1 convergence).
- `human-verified` attestation.
- SC-004 playtest comprehension.
- Vanilla fertilizer-hook/Harmony behavior (explicitly out of MVP).
- Monoculture.
- Final balance/tuning (`P=10` provisional).
- SC-004 comprehension playtest (`qualitative-follow-up`; protocol if run: participant/context scope, before/immediately-after-first-use timing, identify shortcut + soil-health cost, binary pass requiring both, evidence note with sample size/pass count; `pending` if not run).

Claims for the items above remain `pending` — or `planned-run-not-executed` with reason if a planned run was skipped — and are not passes.