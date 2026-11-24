# Problem Definition: Headless Verification Runner for Gate 3

- **Slug**: headless-verification-runner
- **Created**: 2026-11-02
- **Inputs used**: intake.md | research.md | user input

## Problem Statement

Gate 3 (in-game verification) has 0 runs: the harvest-quality effects (Poor → 0 quality, Living → +0.08 boost + extra harvest) and the growth modifier cannot be attested as "verified" because no automated or manual session has captured the 3 required signals (SoilHealthState JSON delta, Crop.phase/phaseDays delta, SMAPI log). Without this, every "verified" claim is unsupported and the mod's central promise (Living Soil pays off at harvest) remains unproven.

## Affected Users & Stakeholders

- **Users**: Maintainer (lfgranja) and future contributors — cannot confirm that shipped features actually work in-game; blocked from honest "verified" status.
- **Stakeholders**: Players (co-op mainstream per Principle VI) — receive a mod whose harvest-quality effects are unverified; risk of regressions shipping unnoticed.
- **Stakeholders**: Reviewers/CI — no automated evidence trail for Gate 3; manual 30-min sessions per change are unsustainable.

## Goals

- Produce machine-attestable evidence for Gate 3 (3 signals) without requiring a human to play.
- Make verification reproducible and repeatable on every change touching runnable code.
- Cap honesty at `machine-pass` (runner) vs `verified` (human E attestation) — never overclaim.

## Non-Goals

- Pixel-perfect screenshot comparison as a correctness oracle (rendering differs under software GL).
- Byte-identical deterministic replays (Stardew Valley is not deterministic).
- Replacing manual playtesting or human judgment for "verified" status.
- Implementing the full 161-task spec in one pass (MVP = 3-signal capture).

## Success Metrics

- At least 1 automated run producing `machine-pass` with all 3 signals captured (baseline: 0).
- Gate 3 status moves from "partial (smoke)" to "machine-pass" (baseline: smoke-only).
- Time to verify a change drops from ~30 min manual to < 5 min automated (baseline: manual only).
- Zero false "verified" claims (runner never writes `verified`).

## Cost of Inaction

- The mod's central promise (Living Soil pays off at harvest) remains unproven and unverified.
- Every future change risks shipping regressions in harvest-quality effects with no automated safety net.
- Manual verification becomes a bottleneck, discouraging contributions and slowing iteration.
- The 19 unimplemented features and 12 mid/end-game items cannot be validated against a working baseline.

## Open Questions

- [NEEDS CLARIFICATION: whether the runner should reuse `scripts/smoke-validation.sh` infrastructure or be separate]
- [NEEDS CLARIFICATION: communication mechanism between C# companion and running game process (SMAPI mod API vs. file-based IPC)]
- [NEEDS CLARIFICATION: whether `verified` attestation can be partially automated or remains human-only]
- [NEEDS CLARIFICATION: acceptance of the 161-task scope vs. a smaller MVP that only captures the 3 Gate 3 signals]
- [NEEDS CLARIFICATION: CI runner availability (self-hosted vs. GitHub-hosted with gamepath.zip)]