# 013 Mulch — Scenario thresholds / signals (T024, 018 Wave 4)

**Honest, evidence-oriented notes.** Mulch application does **not** mutate soil health; decay exemption/expiry affects the soil-decay path (bare tilled `HoeDirt`, crop=null) per `LivingRoots/Services/SoilDecayService.cs` + `MulchDecayMultiplier`/mulch service. Crop-growth/phase signals depend on whether the observed tile has a crop.

## Signals

| Scenario | `soilHealthStateDelta` | `cropPhaseDelta` | `smapiLog` | `signalPolicy` |
|---|---:|---:|---:|---|
| `mulch-decay-exemption.scenario.json` (v3, cropped 60,15, 1 day) | `1.0×` (no mutation, soil 50) | `1.0×` (LongPhaseCrop stable in 1-day capture) | `isMainPlayer eq true` | 3 signals |
| `mulch-bare-tile-decay-exemption.scenario.json` (v4, bare 61,15, 6 ticks) | `1.0×` (mulch protects spring ticks 1–6) | n/a | `isMainPlayer eq true` | `cropPhaseDelta=not-applicable` (bare no-crop decay-path) |
| `mulch-7day-expiry.scenario.json` (v4, bare 61,15, 7 ticks) | `0.98×` (50→49: tick 7 removes mulch before decay → one unprotected spring decay `2*0.5=1`) | n/a | `isMainPlayer eq true` | `cropPhaseDelta=not-applicable` |
| `mulch-refused-on-ineligible.scenario.json` | refusal/no-mutation (see scenario thresholds) | as declared | refusal/log assertions | see scenario |

## References

- `LivingRoots/Services/MulchApplicationService.cs` (`TryApplyMulch`)
- `LivingRoots/Services/SoilDecayService.cs` (mulch/decay interaction)
- `LivingRoots/Controllers/ModController.cs` mulch route
- 018 plan/research: `specs/018-machine-pass-full-coverage/{plan.md,research.md}`
- 013 test record: `specs/013-mulch-soil-cover/test.md` (runner-promoted GATE3; do not hand-edit)

**Caveat:** threshold rationale here summarizes scenario intent; authoritative assertions are the scenario JSON + runner verdict. Machine-pass ≠ human-verified. Farmhand D1/guard blocked-feasible (T002).
