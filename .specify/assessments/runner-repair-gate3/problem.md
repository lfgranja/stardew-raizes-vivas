# Problem Definition: Runner quality gaps that block machine-verifiable Gate 3

- **Slug**: runner-repair-gate3
- **Created**: 2026-11-05
- **Inputs used**: intake.md, research.md

## Problem Statement

The headless verification runner (spec 010) reaches the real game but has historically failed to produce a trustworthy `machine-pass` because its evidence path carried three independent defects: the isolated save folder and inner save file disagreed on name (so `SaveGame.Load` returned to title), the SMAPI log artifact was a two-line grep excerpt (which caused a multi-day misdiagnosis), and the bundle manifest was a bare `{"runId": ...}` that failed its own schema while the verifier never checked. As a result, every Gate 3 verification either stayed `partial` or claimed `machine-pass` over invalid evidence, and specs 014/015 stayed blocked behind a machine-pass-first rule that could not be honestly satisfied.

## Affected Users & Stakeholders

- **Users**: the maintainer of Living Roots — cannot ship verified gameplay changes without manually sitting through a save load and a crop/soil cycle; every blocked or misdiagnosed verification is time taken from implementation. — [research: Users & Demand]
- **Stakeholders**: downstream features — specs 014 (soil-health crop quality) and 015 (Harmony harvest seam) cannot be implemented under the current machine-pass-first rule while the runner is broken. — [research: Users & Demand]
- **Stakeholders**: CI and future contributors — a runner that is not trustworthy on a real host cannot become a merge gate, so verification stays a single-person bottleneck. — [research: Users & Demand, "no second maintainer or CI runner expected"]
- **Stakeholders**: the honesty ladder itself — `machine-pass` is only meaningful if the three signals come from the real game and the bundle satisfies its contract; weakening the runner to produce a green verdict would devalue every attestation. — [research: Data & Constraints, "verified is human-only"]

## Goals

- A real-host run completes a scenario and writes `machine-pass` with `signalsComplete=true`, `signalsAgree=true`, and `thresholdsOk=true`, backed by a schema-valid, verifier-accepted bundle.
- Failures are diagnosable from the evidence bundle alone, without re-running the scenario (full SMAPI log, not a filtered excerpt).
- The save-load step either succeeds on the first attempt or fails loudly with a named cause, never silently returning to title.
- The manifest's detached SHA-256 anchor and schema validation are enforced by the offline verifier before any promotion.

## Non-Goals

- Replacing the human `verified` attestation. T148 stays human-only; no automation may raise a claim above `machine-pass`. — [research: Data & Constraints, FR-016]
- General-purpose desktop computer use (browsing, installing, mod-manager GUIs, Steam UI) beyond what Gate 3 needs. — [research: agent-desktop-usefulness problem.md, Non-Goals]
- Driving the game through pixel coordinates, synthetic clicks, or accessibility refs. Input simulation is excluded by spec 010 FR-006. — [research: agent-desktop-usefulness problem.md]
- Authoring new gameplay tests in the Phase 6 suite (T141–T145 run existing suites only). — [research: tasks.md, Phase 6]
- Implementing specs 014/015 — those follow after machine-pass, not as part of this repair.

## Success Metrics

- `verdict.json` reports `machine-pass` on the real host (baseline: `partial`, `signalsComplete=false`, `signalsAgree=false`, `thresholdsOk=false`). — [research: Users & Demand] (achieved 2026-11-05, bundle `evidence-bundle-run-20261105T024756Z-07ba`)
- The milestone save loads on the first attempt (baseline: `getLoadEnumerator('farm-year1-day5')` → `DebugOutput: File does not exist` → return to title). — [research: Data & Constraints]
- The evidence bundle contains the full SMAPI log, not a grep-filtered excerpt (baseline: 2 lines, which caused a multi-day misdiagnosis). — [research: Data & Constraints]
- `scripts/verify-evidence-bundle.sh` returns 0 on a valid bundle and 19 on a schema/digest/mandatory-evidence failure (baseline: never validated the schema). — [research: Data & Constraints]
- T138 (second host), T139 (timed dry run), T140 (audit drill) and T146 (promotion) can run without a new blocker (baseline: all blocked behind T137). — [research: Data & Constraints]

## Cost of Inaction

Gate 3 stays human-dependent; specs 014/015 stay blocked; the runner keeps producing `partial` bundles that look like a five-second stall, so each future failure costs another full diagnosis cycle. The maintainer also keeps paying the manual-verification tax on every gameplay change, which is exactly the cost spec 010 was created to remove. Additionally, without schema enforcement, `machine-pass` could again be granted over invalid evidence, devaluing the honesty ladder. — [research: Cost of Inaction, agent-desktop-usefulness problem.md]

## Open Questions

- [NEEDS CLARIFICATION: Should the save-load alignment fix live in `save_prepare`/`save_align_inner_name` or in the milestone packaging step that produced the mismatched names?]
- [NEEDS CLARIFICATION: Is widening the SMAPI excerpt to the full log a permanent contract change, or should it be a configurable capture level?]
- [NEEDS CLARIFICATION: Should manifest schema validation be a hard gate in CI, or only in the offline verifier?]
- [NEEDS CLARIFICATION: Is this repair scoped to spec 010 only, or should it become a general pre-flight quality gate for every spec in the mod (and other mods)?]
- [NEEDS CLARIFICATION: Which parts of this repair are generalizable beyond Stardew Valley / SMAPI, and which are domain-specific?]
- [NEEDS CLARIFICATION: Should `subject[]` be required to cover every file in the bundle (not just artifacts), to close the retention-receipt gap?]