# Provisioning & Validation Path — Spec 017 (Joja / Agrochemical MVP)

**Status:** validation-only. The automated fixture path is available today. The manual/Gate 3
console grant is a scoped decision that is **not implemented** in this MVP, so manual
S1–S5/playtest runs remain `blocked` until that path exists.

**Purpose:** Define how the single Joja/agrochemical input is obtained for validation without
adding `Data/Shops`, a Joja economy, or any shipped gameplay acquisition path.

## Boundaries (non-negotiable for this MVP)

- No `Data/Shops` edit and no shop/quest/economy acquisition.
- No `HoeDirt.fertilizer` write and no Harmony fertilizer hook.
- The item remains a mod-intercepted input; its only supported use is the controller path in
  `TryApplyAgrochemicalWithLock`.
- Provisioning must not create a second source of truth, a permanent free item source, or a
  claim of final balance.
- Any manual grant is validation-only and must not be presented as shipped content.

## Available now: automated test fixture

The automated S1–S5 checks do not need game content or a save. They construct the item directly
and inject it as the held item through the existing test seam.

**Exact fixture:** `LivingRoots.Tests/Controllers/ModControllerAgrochemicalTests.cs`,
`ControllerHarness.CreateAgrochemicalStack()`:

```csharp
Item item = ItemFactory.CreateItem(ModConstants.AgrochemicalItemId);
item.Stack = 3;
Player.CurrentItem = item;   // PlayerProviderStub implements IPlayerProvider
```

**Exact command:**

```bash
GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release \
  --filter "FullyQualifiedName~Agrochemical|FullyQualifiedName~AgrochemicalPersistence|FullyQualifiedName~ModControllerAgrochemical|FullyQualifiedName~AgrochemicalContentParity"
```

**Scope:** test process only. It proves controller/service behavior under mocks; it does not put
the item in a real save, does not exercise the real content pipeline, and is **not** Gate 3.

**Status:** available. Recorded as part of the automated claims in
`verification/claim-records.md` after T048/T049 produce their artifacts.

## Manual/playtest path: validation-only console grant (scoped decision)

The chosen manual path is a **verification-only console command**, registered in
`LivingRoots.Verification` rather than the shipped `LivingRoots` mod:

- **Exact command (intended):** `lr_give_agrochemical 1`
- **Intended behavior:** add one `(O)LivingRoots.JojaAgrochemical` to the host player's inventory
  using `Game1.player.addItemByMenuIfNecessary(ItemRegistry.Create(ModConstants.QualifiedAgrochemicalItemId, 1))`.
- **Where it belongs:** `LivingRoots.Verification/ModEntry.cs` console registration, alongside the
  existing validation-only commands. It must not be added to `Data/Shops` or to normal
  `LivingRoots` content registration.
- **Scope:** headless verification-only use through the injected queue. Manual/player-facing
  provisioning is a separate gap and is not claimed by this path.
- **017 status (Phase 8):** **implemented.** `lr_give_agrochemical` and `lr_apply_agrochemical` are
  validation-only queue commands consumed by `LivingRoots.Verification` (they are not player-facing
  SMAPI console commands). The hardened runner uses them for the host-only Gate 3 record in
  [`host-only-gate3-run.md`](host-only-gate3-run.md). The intended use in this document (a
  disposable validation save via a manual console path) has **not** been performed for S1–S5 and is
  still not a supported normal-play path.

**Current status:** validation-only headless grant implemented (Phase 8); no manual/player-facing
S1–S5 run exists. A manual run must not be recorded from the headless grant without documenting the
exact mechanism as a deviation.

### Rejected alternatives

| Alternative | Why rejected |
|---|---|
| `Data/Shops` entry | Explicitly outside the MVP contract; adds acquisition/economy behavior and a shipped item source. |
| Permanent free/gift/quest source | Same problem, plus a balance/gameplay claim the MVP does not support. |
| External item-spawner mod | Not guaranteed present, not deterministic across environments, and not repo evidence. Acceptable only as an ad hoc local aid if recorded as a deviation, never as a Gate 3 result. |
| `debug ` console commands in runner scenarios | The scenario contract rejects `debug ` prefixed init commands, and the current scenario vocabulary has no item-grant action. |
| Writing the item into a save file directly | Bypasses the content pipeline and risks corrupting/invalidating the save; not acceptable validation evidence. |

## Host-only Gate 3

Nothing in this document creates a Gate 3 scenario or a host-only machine pass. The current
runner cannot express the right-click/agrochemical interaction or grant the item, so the
host-only end-to-end run is recorded as `planned-run-not-executed` in
`verification/host-only-gate3.md`. That record is not a pass, and this provisioning path does not
change its classification.

## Validation-only checklist

- [x] Provisioning mechanism chosen and scoped (automated fixture, then validation-only headless queue grant for Gate 3).
- [x] `Data/Shops` and Joja economy explicitly excluded.
- [x] No fertilizer-slot/Harmony behavior introduced by provisioning.
- [x] Headless verification-queue grant implemented (`lr_give_agrochemical` + `lr_apply_agrochemical` in `LivingRoots.Verification`).
- [ ] Manual/player-facing S1–S5 run recorded with build, save, locale, command, and scope (not done).
