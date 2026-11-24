# Host-Only Gate 3 — Run Record — Spec 015 (Harmony Harvest Seam)

**Claim class:** `host-only-machine-pass`
**Status:** `complete` for the host 3-signal Harmony seam run; full SC-004 (D1 multiplayer / in-game Gate 3 completeness) remains `blocked`/`pending`
**Derived from:** `specs/015-harmony-harvest-seam/test.md` GATE3 block + append-only notes and `specs/015-harmony-harvest-seam/verification/claim-records.md` (018 pass 8)
**Record created:** 2026-11-08 (018 pass 10, conservative; no hand-edit of `test.md` GATE3 and no claim promotion)

> This record is **not** human verification, **not** D1, and **not** full SC-004. It restates runner-promoted
> machine evidence for the host Harmony harvest seam and separates it from the still-pending multiplayer layer.

## Runs / evidence attested

| Evidence | Run ID / source | Result | Notes |
|---|---|---|---|
| Host 3-signal Harmony `Crop.harvest` seam scenario | `run-20261107T210830Z-67f1` | `machine-pass` (runner-promoted GATE3, honesty R) | Poor forced quality 0, moderate vanilla untouched, healthy quality boost; consistent with 014 matrix passes. |
| Harmony registration observed in a real SMAPI log | `run-20261106T014750Z-14b6` (014 harvest matrix bundle) | log signal | `Harvest Harmony patch registered` with no Harmony/harvest-patch errors in checked logs. |
| `HarvestQualityService` seam unit tests (SC-001) | `dotnet test --filter "FullyQualifiedName~HarvestQualityServiceTests"` (documented in `test.md`, 2026-11-07) | 17 passed, 0 failed | Not re-run in pass 10; documented automated evidence (no log path token) — treated as pending linked-log confirmation, not upgraded. |

The `67f1` bundle was present locally and passed `./scripts/verify-scenario.sh --verify-bundle` (exit 0) in the pass 10 audit (2026-11-08); `14b6` also verified OK.

## Honesty boundaries

- Full SC-004 requires a second player for multiplayer verification (D1); this host-only run does not satisfy it.
- D1/farmhand transport: `blocked` (T002 split-screen/headless), never `machine-pass`.
- `machine-pass` (host scenario) ≠ full SC-004 ≠ human-verified / render / balance.
