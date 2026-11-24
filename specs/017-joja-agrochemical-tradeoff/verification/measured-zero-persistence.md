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

## Decision 2026-11-06 — sub-1e-4 postPenalty storage semantics

**Type:** Adjudication (Wave B, T100); append-only; no code change.
**Decision (1):** Keep the sparse-cache drop rule unchanged. `SetHealthInternal` is not modified; the persistence layer is not modified.

Semantics after this decision:

- An agrochemical application landing `postPenalty <= 1e-4` — i.e. a fractional baseline within 1e-4 of the penalty, including exactly `0f` — is not persisted across save/load. The save writer omits entries with magnitude `<= 1e-4`, and the load reader drops the same, so the tile reloads as Unknown/`NaN` after a save/load round trip.
- In-memory, a nonzero sub-1e-4 `postPenalty` is dropped by the pre-existing sparse-cache near-zero rule (`value != 0f`). An exact `0f` `postPenalty` is retained in-memory for the session by the pre-existing explicit-`0f` measured-reading exception (T166/T182), but is still omitted on save and on load — so a measured exact `0f` behaves the same as any other sub-1e-4 result across save/load.
- Sub-1e-4 `postPenalty` is therefore stored neither durably (save/load) nor — when nonzero — in-memory; exact `0f` survives only in-memory within the session and never across save/load. This subsumes the documented clamped-to-zero limitation above (a clamped-to-zero outcome reloads as Unknown).
- The T011 measured-write intent holds for any nonzero `postPenalty > 1e-4`: such results persist and reload as measured values, bounded, and are unaffected by this decision.
- Revisit only with a persistence migration (explicit allow-zero sparse entries or sentinel scheme, with compatibility review for existing saves); any change to these semantics requires its own review, tests, and re-verification.

**Provenance:** Appended 2026-11-06; no prior sections edited.
