# Feature Test Record: 013-mulch-soil-cover

<!-- GATE3:BEGIN -->
- **Status**: planned-run-not-executed
- **Verdict**: —
- **Flaky**: —
- **Run ID**: —
- **Commit**: —
- **Date**: —
- **Player role**: host (planned)
- **Signals**: —
- **Bundle**: —
- **Honesty level**: R
- **Claim**: No machine-pass yet (018 W4). Planned host scenarios: `mulch-decay-exemption` (apply on cropped tilled tile; soil 1.0x no-mutation, LongPhaseCrop phaseDays 1.0x stable capture — FR-009/SC-004 decay exemption implemented via `MulchDecayMultiplier=0`; `SoilDecayService` skips decay on cropped HoeDirt, so bare-tile decay delta not isolated by this setup), `mulch-refused-on-ineligible` (already-mulched second apply refused, no generation failure, no mutation). Farmhand guard (T028) **blocked** (T002: queue rejects non-`lr_*` `/debug split`, ack 19; split-screen never `machine-pass`). Never fabricate.
- **Reviewer**: —
- **Review date**: —
- **Session ID**: —
<!-- GATE3:END -->
