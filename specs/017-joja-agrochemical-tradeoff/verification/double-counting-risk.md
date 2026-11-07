# Double-Counting Risk Note — Spec 017 (T066 accepted risk)

**Date:** 2026-11-06
**Related task:** T066 (FR-004, SC-006, US4/AC1, C1 boundary; no-double-counting vs 014/015 harvest-quality pipeline)
**Status:** Accepted risk — recorded, not resolved.

## What exists

The C1 "no double-counting" claim for the 017 agrochemical path is supported by:

1. **Service-dependency boundary check** — the 017 application service/controller path does not reference `IHarvestQualityService`, harvest-quality boost logic, `HoeDirt.fertilizer`, extra-harvest boosts, or `Crop.phaseDays`/growth modifiers. The C1 automated tests pin this dependency boundary (no 014/015 mechanical buff hooks are added or wired).
2. **Lexical message proxies** — the shipped 017 copy keys contain no claim of a quality/growth/fertilizer-tier buff; the content-parity tests assert the absence of such claims lexically.

## What does NOT exist

**No behavioral composition proof exists or is claimed.** There is no test that applies the agrochemical to a tile (through the real or composed 017 path) and then asserts equality of the 014/015 harvest-quality outcome versus a control tile, because composing the 017 path with `IHarvestQualityService` (or the documented growth/quality seam) is not unit-composable without a heavy harness (game `HoeDirt`/crop state, SMAPI runtime, and harvest-quality pipeline wiring). The claim-records.md row for automated 017 tests already carries the matching scope note ("The C1 test is a service-dependency boundary check; it is not a full harvest/growth composition proof").

Accepted risk: an integration-level interaction defect in which the 017 write (or its feedback path) perturbs an existing harvest-quality input would **not** be caught by the current C1 evidence. The design-level mitigation is that the 017 path never writes harvest-quality/fertilizer/phase state at all, so the failure mode requires a wiring error the boundary check targets — but this is an argument from code structure, not a demonstrated behavioral equivalence.

## Revisit condition

Revisit (implement the composition test or a lighter seam-level equivalent) when any of the following occurs:

- the 017 path gains any new output that feeds soil-driven quality/growth evaluation;
- `IHarvestQualityService` or the 014/015 phase/quality seam is refactored to expose a headless-composable surface;
- a Gate 3/host-only run or playtest surfaces a quality/growth anomaly attributable to agrochemical application;
- a follow-up spec strengthens the MVP short-term gain beyond C1:A (confirmation/proxy only).

Until then, the C1 claim stays scoped to the boundary check + lexical proxies in `verification/claim-records.md` and MUST NOT be promoted or broadened. This note is append-only; do not rewrite prior claim records to imply composition proof exists.
