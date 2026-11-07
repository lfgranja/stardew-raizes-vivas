# Measured-Zero Persistence Note — Spec 017 (T073)

**Date:** 2026-11-06
**Related task:** T073 (SC-003, FR-011, spec edge case "Save/load after application")
**Status:** Recorded contradiction; pre-existing project behavior, not a 017 regression.

## Finding

A measured soil-health result of exactly `0` — i.e. a successful application clamped at `MinSoilHealth` (e.g. measured baseline 5, penalty 10 → `PostPenalty=0`, `AppliedPenalty=5`, `WasClamped=true`) — is **omitted by the existing sparse save cache**. On reload, the tile presents as **Unknown/`NaN`**, not as measured `0`.

Evidence: `AgrochemicalPersistenceTests.SubPenaltyBaseline_Apply_Save_OmitsZeroFromSparseState_AndReloadsUnknown` (passing; recorded in `verification/persistence-roundtrip.md`, "Sparse-zero scope"): saves, asserts the sparse state omits the zero entry, reloads, asserts `GetSoilHealth` returns `NaN` and `TryGetSoilHealth` fails.

## Classification

- This is **pre-existing project persistence behavior** of `SoilHealthService`'s sparse cache (never store default/zero entries), **NOT a 017 regression**, and it does not invalidate the C2 measured-90 or measured-40 round-trip results.
- However, the spec edge case **"Save/load after application: the recorded soil result persists and reloads consistently within existing soil persistence expectations" does NOT hold for clamped-to-zero outcomes.** For zero results, the recorded outcome is lost across save/load and the tile reloads as Unknown.
- The `claim-records.md` persistence row already carries the matching scope note ("A clamped-to-zero result is omitted by the existing sparse cache and reloads as unknown"), but the outcome-level contradiction is recorded here explicitly so it is not lost.
- A matching **blocked** claim-register row was appended on 2026-11-06 ("Measured-zero outcome persists as measured across save/load"; `automated`; source `verification/persistence-roundtrip.md`; do-not-promote scope note). No existing row was edited.

## Follow-up (not implemented at MVP)

A follow-up may persist measured zeros explicitly (e.g. sentinel or allow-zero sparse entry, with migration/compatibility review for existing saves) so clamped-to-zero outcomes round-trip as measured `0`. Any such change touches existing persistence semantics shared with specs 009/014/015 and requires its own review, tests, and Gate 3 re-verification. Until then, zero outcomes reload as Unknown and must be described as such.

**Provenance:** New-file record (2026-11-06); append-only thereafter. Prior persistence artifacts are unchanged.
