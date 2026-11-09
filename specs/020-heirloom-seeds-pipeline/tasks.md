# Tasks: Heirloom Seeds / Sementes Crioulas Pipeline (Crioulo Parsnip)

**Input**: Design documents from `specs/020-heirloom-seeds-pipeline/`

**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/, quickstart.md, checklists/

**Tests**: TDD is mandated by the project constitution (Principle IV) — **test tasks are included and MUST fail first** before their implementation tasks are started.

**Organization**: Tasks are grouped by user story (P1 → P2) to enable independent implementation and testing. Story order: US1 → US2 → US3 → US4 (US3 independent of US1/US2; US4 last).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on incomplete tasks)
- **[Story]**: Which user story this task belongs to
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Assets and scaffolding needed by every story (no behavior yet).

- [ ] T001 [P] Generate placeholder `LivingRoots/Assets/heirloom_seeds.png` (16×16 32-bit RGBA, paper-envelope w/ crioulo broto; procedural, disclosed placeholder — Spec §FR-017) and `LivingRoots/Assets/heirloom_produce.png` (16×16, torto parsnip crioulo look). Do not AI-generate; do not trace vanilla art.
- [ ] T002 [P] Generate placeholder crop sheet `LivingRoots/Assets/Crops/heirloom_crops.png` (128×32 per phase row: 2 seed-variant sprites + 4 phase sprites at 16×32 each, 7-day silhouette; placeholder disclosed).
- [ ] T003 [P] Add heirloom block to `LivingRoots/Constants.cs` `ModConstants`: `HeirloomItemId` (`LivingRoots.HeirloomParsnipSeeds`), `HeirloomProduceItemId` (`LivingRoots.HeirloomParsnip`), qualified forms `QualifiedHeirloomItemId`/`QualifiedHeirloomProduceItemId` (`(O)...`), `HeirloomSeedsTextureAssetName` (`Mods/lfgranja.LivingRoots/HeirloomSeeds`), `HeirloomProduceTextureAssetName` (`Mods/lfgranja.LivingRoots/HeirloomProduce`), `HeirloomCropsTextureAssetName` (`Mods/lfgranja.LivingRoots/HeirloomCrops`), prices (`HeirloomSeedPrice=35`, `HeirloomProducePrice=45`), `HeirloomPlantBonus=2.0f`, `HeirloomHarvestBonus=1.0f`, `HeirloomSavedSeedYield=1`, `HeirloomSeedTag`, `HeirloomProduceTag`, tags `heirloom_seed`/`heirloom_produce`, translation keys for seed-saved / seed-saved-inventory-full / plant-bonus-authority-unsupported, message type `HeirloomPlantBonusRequest`. All with 017-style "tuning requires sync" comments mirroring `AgrochemicalSoilPenalty`.
- [ ] T004 Audit `LivingRoots.csproj` to confirm all `Assets/` content (incl. new PNGs) is copied to output (existing pattern covers compost/mulch PNGs — verify with a dry publish/run log, don't assume).

---

## Phase 2: Foundational (Blocking prerequisites — registration contracts, RED-first)

**Purpose**: Data-layer contract tests then implementation; every story depends on this phase.

- [ ] T005 [US1→foundational] Write failing `HeirloomRegistrationTests.cs` in `LivingRoots.Tests/HeirloomRegistrationTests.cs`: asserts C-1/C-2/C-3/C-4/C-5 contracts of `specs/020-heirloom-seeds-pipeline/contracts/heirloom-seed-registration.md` — seed `Type=Seeds`, `Category=-74`, price 35, tags; produce `Category=-75`, `compostable_item`; crop key == seed ID, `DaysInPhase` sums to 7, `HarvestItemId` resolves, harvest method Grab; `Traveler` entry present with `AvailableStockLimit=Player` + `Condition="SEASON Spring"` (or disclosed fallback); `SeedShop` untouched (no registration branch registered); textures served by name; i18n keys referenced in entries exist.
- [ ] T006 [US1→foundational] Write failing `HeirloomI18nTests.cs` in `LivingRoots.Tests/HeirloomI18nTests.cs`: all `item.heirloom.*` and `message.heirloom.*` keys present in `default.json`/`en.json`/`pt.json`; parity of meaning (same tokens/structure); no whitelisted nitrogen-vocabulary tokens in any crioulo string; no Pierre-vilification tokens; "sementes crioulas" (PT) present.
- [ ] T007 Implement `OnAssetRequested` branches in `LivingRoots/ModEntry.cs` to register C-1/C-2/C-3/C-5 + `LoadFromModFile` for the three PNGs + `Data/Shops` `Traveler` edit (append `Items` entry; do NOT edit `SeedShop`); reuse existing patterns (AsDictionary<...>, IsEquivalentTo, anchors with 013/017 comments). Make T005 fail→green one assertion group at a time.
- [ ] T008 [P] Add crioulo assets to build copy list if missing; extend existing smoke XML docs / class docs where registration contracts are mentioned (keep consistent with 017 conventions).
- [ ] T009 [P] Add `lr_give_heirloom_seed <x> <y> [location]` and `lr_sow_heirloom <x> <y> <seedId> [location]` to `LivingRoots.Verification/ModEntry.cs` + `Queue/QueueCommandParser.cs` (strict parsers; additive verification-only; NOT used outside verification; `lr_sow_heirloom` calls real `HoeDirt.plant` on the existing dirt — not the direct-Crop bypass — so the production plant path is exercised).

**Checkpoint**: registration contracts green (targeted tests pass); i18n parity green.

---

## Phase 3: User Story 1 - Acquire and Sow a Crioulo Seed (Priority: P1) 🎯 MVP-ish (combined with Phase 4 for any player value)

**Goal**: crioulo seed obtainable (Traveler) and plantable, with soil bonus, host-authoritative.
Independent Test: host save → cart seed (or `lr_give_heirloom_seed`) → plant → soil +2 recorded/measured; 7-day crop grows; farmhand path sends request or unsupported refusal.

- [ ] T010 [US1] Write failing `HeirloomSoilEffectsTests.cs` in `LivingRoots.Tests/HeirloomSoilEffectsTests.cs`: plant-bonus math (baseline measured / NaN→100 / clamp 0–100 / idempotent correlation), healthy-band gating helper, farmhand request routing decision (host-applies vs unsupported refusal) with mocked authority checks, `HeirloomPlantBonusRequest` serialization/`modIDs` scoping via existing sync stubs.
- [ ] T011 [US1] Write failing `HeirloomSeedSaveTests.cs` in `LivingRoots.Tests/HeirloomSeedSaveTests.cs` (shared with US2): `HeirloomSavedSeedYield == 1`; the helper grants exactly one seed on completion metadata; Junimo harvest → no grant; inventory full → refusal flag (no destruction).
- [ ] T012 [US1] Implement `Domain/HeirloomIdentity.cs` (pure: ID sets, tag predicates `IsHeirloomSeed`, `IsHeirloomCrop`, `IsHeirloomProduce`; unit-testable without SMAPI) in `LivingRoots/Domain/HeirloomIdentity.cs`.
- [ ] T013 [US1] Implement plant-bonus path: new `Patches/HeirloomPatches.cs` (Harmony postfix on `HoeDirt.plant(string, Farmer, bool)`) OR fold into existing patch scaffold with the same `IsMainPlayer` + try/catch hygiene as `HarvestPatches`; host path uses existing `ISoilHealthService` + `HostAtomicLockService` + bounded write (NaN→100) + `SoilHealthSyncMessage` broadcast; farmhand path sends `HeirloomPlantBonusRequest` (new typed message `HeirloomPlantBonusRequest`, handled in `ModController.OnModMessageReceived`-adjacent handler or `HeirloomMessageHandler` following 017's handler pattern) applying or refusing per HE-1; `ModEntry.RegisterHarmonyPatch` extended (keep Harmony registration in the same lazy-registration idiom).
- [ ] T014 [US1] Wire host receive path: in `Controllers/ModController.cs` add `HeirloomPlantBonusRequest` handler (typed read, bounds validation, `(tile, day)`/request-id idempotency guard, lock, bounded write, broadcast) mirroring the FR-009/A8 handler tone + refusal grammar; refusal copy routed through `IHudNotifier`/localized keys.
- [ ] T015 [US1] Add refusal/authority i18n keys (`message.heirloom.plant-bonus-authority-unsupported`) in `default.json`/`en.json`/`pt.json`; final EN/PT wording honest + cozy (no crioulo triumphalism; Pierre unvillified; abstractions labeled as game rules in descriptions only if at all).
- [ ] T016 [US1] `scenarios/heirloom-plant-bonus.scenario.json` in `specs/020-heirloom-seeds-pipeline/scenarios/` (schema v3) plus `scenarios/macros/heirloom-sow.txt`: init = `lr_till 60 15` + `lr_set_soil_health 60 15 50`; steps macro = `/lr_give_heirloom_seed 60 15` + `/lr_sow_heirloom 60 15 LivingRoots_HeirloomParsnipSeeds Farm` + `/sleep`; runner brackets before/after dumps around steps. Asserts: `soilHealthStateDelta` as `multiplier` `field=soilHealth` `base=pre.soilHealth` `factor=1.04` (50→52) `tolerance=0.0001`; `cropPhaseDelta` as `multiplier` `factor=1.0` (planting does not advance growth); `smapiLog` `cmp isMainPlayer eq true`. Plus `specs/020-heirloom-seeds-pipeline/test.md` with the single GATE3 marker pair (schema v3 contract: exactly one `<!-- GATE3:BEGIN/END -->`).
- [ ] T017 [US1] Run targeted tests (T010/T011 fail→green); run `scripts/verify-scenario.sh --scenario specs/020-heirloom-seeds-pipeline/scenarios/heirloom-plant-bonus.scenario.json --no-acquire --game-path "$HOME/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build`; record honest host-only machine-pass vs pending evidence gap; do not promote any claim.

---

## Phase 4: User Story 2 - Harvest, Get Paid, and Save the Seed (Priority: P1)

**Goal**: harvest of heirloom parsnip pays 45g premium, heals Healthy-band soil +1, returns exactly one heirloom seed, 014 math unchanged, Junimo exempt.
Independent Test: host plants crioulo parsnip on ≥71-health tile → 7 days → harvest → produce + 1 seed + soil +1; on <71 tile no soil write; Junimo harvest → no seed; full inventory → refusal copy, no loss.

- [ ] T018 [US2] Extend harvest seam: narrow heirloom filter in existing `HarvestPatches` (post-decision branch for crop seed ID == heirloom seed): (a) apply `+HeirloomHarvestBonus` bounded write when measured Healthy band; (b) after 014 outcome resolution (success path incl. Junimo-guarded path per 014 semantics), call the seed-save path for non-Junimo harvesters with inventory delta snapshot (`HarvestInventoryDelta`/`IPlayerInventory` reused; full → `message.harvest.seed-saved-inventory-full` copy, zero destruction). Keep 014 outcome untouched otherwise.
- [ ] T019 [US2] Implement seed-save helper in `Services/HeirloomSeedSaveService.cs` (or `Domain/Services/`) enforcing `HeirloomSavedSeedYield == 1` deterministic; grants `(O)LivingRoots_HeirloomParsnipSeeds` via `ItemRegistry.Create`; unit-wired for T011 green.
- [ ] T020 [US2] [P] Add post-harvest i18n keys `message.heirloom.seed-saved` + `message.heirloom.seed-saved-inventory-full` in `default.json`/`en.json`/`pt.json` (warm, honest, locale parity; explicit "seed saved for your next planting" semantics; EN/PT).
- [ ] T021 [US2] 014 integration test: extend/process existing harvest-quality tests so crioulo crop enters `IHarvestQualityService.Decide` with *identical* inputs as vanilla crop (snapshot/input-capture), verifying FR-008/FR-007 no-double-counting (bonus is soil-write only).
- [ ] T022 [US2] Run T011 + T021 green; re-run full targeted set; run Gate 3 scenario (still valid) to confirm no regression in harvest path; update `claim-records.md` honesty rows.

---

## Phase 5: User Story 3 - Understand the Crioulo Choice (Priority: P2)

**Goal**: player-facing copy is honest, warm, locale-parity; educates without preaching; labeled abstractions stay out of user copy.
Independent Test: read EN/PT/default descriptions and harvest message; automated parity tests (T006) green; manual copy review recorded.

- [ ] T023 [US3] Finalize crioulo copy in `default.json`/`en.json`/`pt.json`: seed/produce name+description (crioulo vocabulary: "sementes crioulas", "solo vivo", slow-growth tradeoff stated plainly, refund promise "one seed returns each harvest", premium justified as "crioulo" care); nitrogen NEVER claimed; Pierre hybrids never vilified (optionally: "híbridos do Pierre continuam o caminho certo pra quem produz em sistema intensivo" — neutral, respectful); Elena not mentioned (unimplemented).
- [ ] T024 [US3] [P] Copy lint vs agronomy audit: grep-able CI-friendly check script `scripts/tests/test-020-heirloom-copy.sh` (new) asserting absence of forbidden tokens (fixar/nitrogênio claims) in crioulo i18n entries; wired into smoke suite documentation (optional hook; not mandatory for release).
- [ ] T025 [US3] Manual copy review note in `claim-records.md`/validate.md: EN/PT/default parity confirmed; crioulo framing honest (abstractions labeled in dev docs only); reviewer = this cycle (AI assist, not human attestation).

---

## Phase 6: User Story 4 - Verify the Slice Honestly (Priority: P2)

**Goal**: build/tests/format green, Gate 3 host-only machine-pass, claims honest, docs annotated (append-only).

- [ ] T026 [US4] `dotnet format Stardew-LivingRoots.sln --verify-no-changes` clean; build Release exit 0; note `-m:1` host mitigation unchanged.
- [ ] T027 [US4] Full suite green (no regressions vs 1937/0/4 baseline); record exact counts in validation artifact; all targeted heirloom tests included.
- [ ] T028 [US4] Run quickstart flow end-to-end (`specs/020-heirloom-seeds-pipeline/quickstart.md`); fix any broken steps honestly.
- [ ] T029 [US4] `./scripts/verify-scenario.sh --verify-bundle evidence-bundle-run-<new>` on the produced bundle; verify claims with `scripts/tests/test-claims.sh`-style local validator if present; record honesty R, `machine-pass`, `verdict pass`; explicitly record D1/human/SC-comprehension `pending`.
- [ ] T030 [US4] Create `specs/020-heirloom-seeds-pipeline/claim-records.md` with the 017-pattern claim table (claims: automated, host-only-machine-pass, d1-farmhand-convergence, human-verified, sc-comprehension — statuses and notes).
- [ ] T031 [US4] Write `.specify/validations/020-heirloom-seeds-pipeline/validate.md` recording validation evidence (tests/build/Gate 3/quickstart); status = aprovado only if all pass; explicit "no release/tag/publish decisions taken — maintainer actions"; honesty statement; no claims promoted.
- [ ] T032 [US4] Append-only annotations to `ROADMAP.md` + `OBJETIVO.md` + `COMPLETION_SUMMARY.md` (2026-11-09 block for spec 020, no rewrites; keep machine-pass ≠ verified honesty) — do NOT tag SemVer or bump manifest.

---

## Phase 7: Polish & cross-cutting

- [ ] T033 Append-only annotation of `specs/020-heirloom-seeds-pipeline/tasks.md` with final statuses (this file) at cycle end.
- [ ] T034 Consistency pass: `git add` explicit files only; confirm no stray/gitignored artifacts committed; `.gitignore` covers evidence bundles (verify).
- [ ] T035 Post-cycle housekeeping note in this file + `COMPLETION_SUMMARY.md`: what remains (final art, human playtest SC, D1, maintainer release decision, next two crops on the same seam, SeedShop/Pierre untouched, Elena dependent features unblocked only after Elena).

---

## Dependencies & Execution Order

- Phase 1 (T001–T004) → Phase 2 (T005–T009) **blocks everything else**.
- Phase 3 (T010–T017) and Phase 4 (T018–T022): both depend on Phase 2; Phase 4 additionally uses the seam wired in T012/T013.
- Phase 5 (T023–T025): depends on Phase 2/3 i18n keys; largely parallel to Phase 3–4.
- Phase 6 (T026–T032): after Phases 3–5 green.
- Phase 7 (T033–T035): after Phase 6.

### Parallel opportunities

- T001, T002, T003, T009 [P] in setup.
- T005+T006 authored in parallel; T008, T009 parallel with registration implementation.
- T010/T011 tests authored in parallel before implementation.
- T020, T023/T024 [P] after keys exist.

## Parallel Example: User Story 1

```bash
Task: "Write failing HeirloomRegistrationTests.cs contract assertions (C-1..C-5)"
Task: "Write failing HeirloomSoilEffectsTests.cs plant-bonus/farmhand-routing math"
```

## Implementation Strategy

### MVP first

1. Phases 1–2 → registration under contract control.
2. Phase 3 (US1) + Phase 4 (US2) → the playable loop (seed → plant → grow → harvest → save).
3. Phase 5 → copy honesty. 4. Phase 6 → evidence. 5. Phase 7 → paper trail.

### Stop / escalation criteria

- If `SEASON Spring` condition on the Traveler entry proves broken at runtime → fall back to unconditional cart entry (disclosed); if that also fails, **stop** and escalate as explicit scope decision (new `se_decision_request`-style note or file edit ask), never silently widen to `SeedShop`.
- If farmhand request-correlation cannot be made idempotent with existing primitives in reasonable effort → substitute explicit refusal grammar (017 C4:A mirror) with full disclosure in validate.md; do **not** ship half-guaranteed MP writes.
- If any test cannot be made green without diluting bounds/honesty → **do not** loosen the test; report failure.

## Notes

- [P] = different files, no incomplete dependencies.
- Commit after each task or small logical group via the atomic-commit-hook (`bash .specify/scripts/atomic-commit-hook.sh <file> <summary> <why>`).
- Never `git add -A`; never stage gitignored evidence bundles; never rewrite prior commits/annotations.
- machine-pass ≠ verified; D1/human-E remain `pending`; no release/tag/publish actions autonomously.
