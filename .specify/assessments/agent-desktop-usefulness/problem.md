# Problem Definition: Machine-verifiable gameplay without a human at the keyboard

- **Slug**: agent-desktop-usefulness
- **Created**: 2026-11-04
- **Inputs used**: intake.md, research.md

## Problem Statement

Gameplay changes in this mod cannot be verified end-to-end without a human launching and playing Stardew Valley. The headless runner built for that purpose (spec 010) starts the game but has never completed a real-host scenario, so every promotion to `machine-pass` is blocked and the maintainer remains the only oracle for Gate 3.

## Affected Users & Stakeholders

- **Users**: the maintainer of Living Roots — cannot ship verified gameplay changes without manually sitting through a save load and a crop/soil cycle; every blocked verification is time taken from implementation. — [research: Users & Demand]
- **Stakeholders**: downstream features — specs 014 (soil-health crop quality) and 015 (Harmony harvest seam) cannot be implemented under the current machine-pass-first rule while the runner is broken. — [research: Users & Demand]
- **Stakeholders**: CI and future contributors — a runner that is not trustworthy on a real host cannot become a merge gate, so verification stays a single-person bottleneck. — [research: Prior Art, existing harness] `[NEEDS CLARIFICATION: whether a second maintainer or CI runner is expected in the next 6 months]`
- **Stakeholders**: the honesty ladder itself — `machine-pass` is only meaningful if the three signals come from the real game; weakening the runner to produce a green verdict would devalue every past and future attestation. — [research: Data & Constraints, "verified is human-only"]

## Goals

- A real-host run of the existing runner completes a scenario and writes `machine-pass` with `signalsComplete=true`, `signalsAgree=true`, and `thresholdsOk=true`.
- The three signals (soil-health JSON delta, crop phase/`phaseDays` delta, SMAPI log) come from the real game process, not from a mock or a fixture.
- Failures are diagnosable from the evidence bundle alone, without re-running the scenario to find out what happened.
- The verification path stays deterministic and reproducible across hosts, so a pass on one machine means the same thing on another.

## Non-Goals

- Replacing the human `verified` attestation. T148 stays human-only; no automation may raise a claim above `machine-pass`. — [research: Data & Constraints, FR-016]
- General-purpose desktop computer use (browsing, installing, mod-manager GUIs, Steam UI) beyond what Gate 3 needs.
- Driving the game through pixel coordinates, synthetic clicks, or accessibility refs. Input simulation is excluded by spec 010 FR-006 and the research found no accessibility tree to drive. — [research: Evidence Against 1 and 4]
- Adding a window manager, an AT-SPI bus, or a Node/native toolchain to the environment under test.
- Authoring new gameplay tests in the Phase 6 suite (T141–T145 run existing suites only).

## Success Metrics

- `verdict.json` reports `machine-pass` on the real host (baseline: `partial`, `signalsComplete=false`, `signalsAgree=false`, `thresholdsOk=false`). — [research: Users & Demand]
- The milestone save loads on the first attempt (baseline: `getLoadEnumerator('farm-year1-day5')` → `DebugOutput: File does not exist` → return to title). — [research: Data & Constraints]
- The evidence bundle contains the full SMAPI log, not a grep-filtered excerpt (baseline: 2 lines, which caused a multi-day misdiagnosis). — [research: Data & Constraints]
- T138 (second host), T139 (timed dry run), T140 (audit drill), T145 (farmhand path) and T146 (promotion) can run without a new blocker (baseline: all blocked behind T137).
- Specs 014 and 015 become implementable (baseline: blocked on the machine-pass-first rule).

## Cost of Inaction

Gate 3 stays human-dependent forever; specs 014/015 stay blocked; the runner keeps producing `partial` bundles that look like a five-second stall, so each future failure costs another full diagnosis cycle. The maintainer also keeps paying the manual-verification tax on every gameplay change, which is exactly the cost spec 010 was created to remove.

## Open Questions

- [NEEDS CLARIFICATION: Is there a desktop-automation need outside spec 010 — a future mod manager, installer, or Steam/SMAPI GUI flow — or was agent-desktop only ever a candidate for the runner?]
- [NEEDS CLARIFICATION: Would agent-driven assistance during the *human* Gate 3 session (T148) be welcome as convenience, given the attestation stays human?]
- [NEEDS CLARIFICATION: Is the runner allowed to add a window manager and an AT-SPI session to Xvfb if a future feature genuinely needs GUI automation, or is "no GUI driver" a permanent constraint?]
- [NEEDS CLARIFICATION: Should capturing the full SMAPI log be treated as a bundle-quality bug fix independent of this assessment? The research recommends yes.]
- Unverified by experiment: whether an SDL2 window ever appears in agent-desktop's `list-apps` on a real AT-SPI session. The research rates this low-value because the save-load bug blocks T137 regardless.
