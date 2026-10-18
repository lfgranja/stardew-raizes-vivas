# Quickstart Validation Guide — Soil Health → Crop Growth

**Feature**: `005-soil-health-to-crop-growth` (`feat/005-soil-health-to-crop-growth`)
**Prerequisites (before validation)**: `spec-004` rollback verification (`SC-008`) must pass before final yield activation (`Dependencies` 136-137; user selected concurrent prerequisite `B` — design/implementation may proceed concurrently, but activation requires `SC-008` green). `GamePath` spike (`A1`) must verify `Crop.newDay` public-field ordering with vanilla growth (`Dependencies` 141; `Clarifications` Q1 27).

---

## End-to-End Validation Scenarios

These scenarios prove the feature works from the player's perspective. Each scenario links to contracts (`contracts/i-soil-health-service.md`) and data model (`data-model.md`) without duplicating design details.

### Scenario 1: Growth Speed Change (US1 / `SC-001` / `SC-002` / `SC-003`)

**Prerequisites**: Mod loaded; `GamePath` verified; compost applied to 1 tile; same crop/season/water on 3 tiles.

**Steps**:
1. Launch with `GamePath`; plant same seed on 3 tiles (same season, same water condition).
2. Apply compost to 1 tile (or apply 3 times to raise to `Healthy` tier `≥71`).
3. Observe growth progress daily.
4. Confirm: `Poor` (`[0,30]`) tile grows slower than `Moderate` (`[31,70]`); `Healthy` (`[71,100]`) tile grows faster; `Moderate` = vanilla (`SC-003` parity).

**Expected outcome**:
- `SC-001` (`Poor` > `Moderate` in days to harvestable phase) — yes
- `SC-002` (`Healthy` < `Moderate` in days) — yes
- `SC-003` (`Moderate` = vanilla — no divergence from untreated) — yes
- No hardcoded values (`ModConstants` only: `SoilGrowthPoorMultiplier`=1.15, `SoilGrowthHealthyMultiplier`=0.85)

**Verification method**: Manual observation (`SC-008` / `FR-009` 82 — manual play-through is dominant verification path). Compare 3 tiles (`P`/`M`/`L`) over same crop lifecycle.

---

### Scenario 2: Harvest Quality Reflects Tier (US2 / `SC-004`)

**Prerequisites**: `SC-008` rollback verification concurrent in progress; harvest from `P`/`M`/`L` tiles (same seed, season, water).

**Steps**:
1. Harvest from `Poor` (`[0,30]`) tile: observe quality roll = 0 stars (`FR-002` 75 — override permitted only for Poor).
2. Harvest from `Moderate` (`[31,70]`) tile: observe vanilla roll (`p = 0.2·L/10 + 0.2·B·(L+2)/12 + 0.01` — `FR-002` 75).
3. Harvest from `Healthy` (`[71,100]`) tile: observe enhanced probability (additive `+0.08` `SoilQualityHealthyBoost`).
4. Confirm floating-text message appears when tier modifies outcome (`Clarifications` Q3/A4 33-34; `VisualizationService.cs` 247-285; `CompostApplicationService.cs` 178-191 reuse pattern).

**Expected outcome**:
- `SC-004`: `Poor` → 0 stars; `Moderate` → vanilla; `Healthy` → enhanced probability (`FR-002` 75; `SC-004` 110).
- No pixel assertions (`FR-008` 81); manual observation only (`SC-008` / `FR-009`).

---

### Scenario 3: Extra Harvest on Living Soil (US3 / `SC-005`)

**Prerequisites**: `Healthy` (`≥71`) tile; same seed/season; no duplication (`FR-005` rollback concurrent — duplication risk documented but accepted during verification window per user B).

**Steps**:
1. Harvest multiple times from `Healthy` tile vs. `Moderate` tile.
2. Confirm: `Healthy` tile shows extra item above `Data/Crops.ExtraHarvestChance` (`base + 0.10` `SoilExtraHarvestHealthyBoost` — `FR-003` 76).
3. Confirm message confirms extra item (`Clarifications` Q3/A4 33-34).
4. Confirm: no duplication (`US3` 58: "sem duplicar itens"; `FR-005` rollback concurrent protects but requires verification).

**Expected outcome**:
- `SC-005`: per-tile probability > base on `Healthy` tiles (`FR-003` 76; `SC-005` 111).
- No global per-seed extra (`US3` 58: `Data/Crops` must not provide this globally).

---

### Scenario 4: Trust / Verification Pipeline (US4 / `FR-009` / `SC-008`)

**Prerequisites**: `SC-008` rollback verification concurrent (user B); `ROADMAP.md` reconciliation confirmed (`A3` 88); `ModConstants` values (`SoilGrowthPoorMultiplier`=1.15, `SoilGrowthHealthyMultiplier`=0.85, `SoilQualityHealthyBoost`=0.08, `SoilExtraHarvestHealthyBoost`=0.10) documented in `Clarifications` (lines 29-30).

**Steps** (manual play-through):
1. Apply compost → observe `SoilHealthService` tile health update (`SC-006` 113 — overlay reflects updated health).
2. Confirm faster/slower growth (`SC-001`/`SC-002`) and parity (`SC-003`).
3. Confirm harvest quality (`SC-004`) and extra harvest (`SC-005`) differences (`P`/`M`/`L` comparison).
4. Save/reload (`SC-007` 115 — growth effect preserved after reload; `SoilHealthService` persistent data consistent).
5. Confirm no contradiction with `ROADMAP.md` scale (`FR-004` 77) and no `spec-001` half-open leakage (`A3` 88).

**Verification prerequisites before activation** (per clarification loop results):
- `SC-008` rollback verification (`manual GamePath` + automated `StillConsumesBin`) — concurrent (user B); must complete before final activation.
- `GamePath` ordering spike (`A1` 86) — verify `Crop.newDay` public-field scan ordering with vanilla growth before `FR-001` activation.
- `ReGen Valley` interaction (`Assumes` 145) — documented only; no design change needed.

---

## Validation Commands (Manual / Automated)

**Manual verification (`SC-008` / `FR-009` 82)**:
```bash
# Launch with GamePath (prerequisite: SC-008 rollback verification concurrent in progress)
# Apply compost; observe growth/harvest; save/reload; verify consistency
```

**Automated verification (`SC-008` prerequisite — concurrent)**:
```bash
# StillConsumesBin test (CompostingBinServiceTests.cs 339) — verifies rollback mechanism
# Manual verification remains dominant path (FR-009 82; SC-008 114)
```

**Formatting / Style check** (`AGENTS.md` conventions):
```bash
dotnet format --verify-no-changes specs/005-soil-health-to-crop-growth/
dotnet build specs/005-soil-health-to-crop-growth/
```

---

## References (Used in Planning — Not Duplicated in Spec)

- `spec.md` (updated after clarification loop: `Clarifications` 2026-10-16 — `Q1` hook A, `Q2` scale A, `Q3` growth A, `Q4` quality/extra A, `Q6` application A, `Q7` chunk A, harvest `Q3/A4` A; rollback `Q5` B integrated at line 36)
- `CLARIFICATION_BATCH.md` (batch format per instruction)
- `CLARIFICATION_COMPLETION_REPORT.md` (completion status: 2 integrated — rollback B concurrent; performance A design; harvest A resolved)
- `DEFERRED_SC008_EXPLANATION.md` (rollback codebase evidence)
- `subagent-response-q7.md` (performance chunk mechanism evidence: linear only; zero chunk in code)
- `research-harvest-feedback.md` (harvest feedback: engineering + analogical agronomic evidence)
- `checklists/requirements.md` (16/16 passing; clarification reinforces criteria)
- `.specify/memory/constitution.md` (v2.0.0 — Principle VI multiplayer; security-first; async-only; DDD; YAGNI)
- `.specify/extensions.yml` (no executable `before_plan` hooks; `after_analyze` hook only — no action needed before plan)
