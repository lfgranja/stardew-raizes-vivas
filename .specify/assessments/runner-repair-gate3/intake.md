# Idea Intake: Runner repair / quality work that unblocked Gate 3

- **Slug**: runner-repair-gate3
- **Created**: 2026-11-05
- **Source**: pasted text (conversation context from the spec 010 verification pass)
- **Type**: improvement

## Idea (as captured)

"runner-repair/quality work that actually unblocked Gate 3 (save-load alignment, full SMAPI log capture, manifest schema enforcement)"

## Restated

The headless verification runner (spec 010) was blocked on a real-host scenario that reported `partial` with `signalsComplete=false`. The work that unblocked it repaired three concrete defects: a save-load mismatch where the isolated save folder name disagreed with the inner save file name (causing `DebugOutput: File does not exist`), a bundle that carried only a two-line grep-filtered SMAPI excerpt instead of the full log (which had caused a multi-day misdiagnosis), and a manifest that was a bare `{"runId": ...}` and failed its own schema because the verifier never validated it. The goal of this intake is to capture that repair as a reusable, generalizable quality practice for this mod and others, so future Gate 3 runs do not repeat the same diagnosis cycle.

## Origin & Context

- **Raised by**: maintainer of Living Roots (conversation during the spec 010 T131-T150 verification pass)
- **Trigger**: the real-host T137 run produced `partial` and the evidence bundle was later found to be schema-invalid; the repair was executed inline and succeeded (machine-pass, 2026-11-05)

## First-Glance Unknowns

- [NEEDS CLARIFICATION: Should the save-load alignment fix live in `save_prepare`/`save_align_inner_name` or in the milestone packaging step that produced the mismatched names?]
- [NEEDS CLARIFICATION: Is widening the SMAPI excerpt to the full log a permanent contract change, or should it be a configurable capture level?]
- [NEEDS CLARIFICATION: Should manifest schema validation be a hard gate in CI, or only in the offline verifier?]
- [NEEDS CLARIFICATION: Is this repair scoped to spec 010 only, or should it become a general pre-flight quality gate for every spec in the mod (and other mods)?]
- [NEEDS CLARIFICATION: Which parts of this repair are generalizable beyond Stardew Valley / SMAPI, and which are domain-specific?]
