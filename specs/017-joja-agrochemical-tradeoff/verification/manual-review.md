# Manual Copy Review — Spec 017 (Joja / Agrochemical MVP)

**Review type:** structured semantic/tone/legibility review of the shipped 017 keys.  
**Reviewer:** implementation agent (AI-assisted review)  
**Date:** 2026-11-06  
**Artifacts reviewed:** `LivingRoots/i18n/default.json`, `en.json`, `pt.json`  
**Scope:** the 10 touched 017 keys in all three locale files (30 entries).

> **Not a human attestation.** This review was performed by the implementation agent, not an
> independent human reviewer. It supports only a narrow copy-review claim. It does **not**
> satisfy `human-verified`, does not execute SC-004, and does not replace in-game playtesting.

## Checklist

| # | Key | `default` / `en` | `pt` | Shortcut / cost / attribution / refusal meaning | Tone and legibility | Result |
|---:|---|---|---|---|---|---|
| 1 | `item.agrochemical.name` | Joja Agrochemical | Agroquímico Joja | Names the single Joja/industrial input; no mechanical buff claim. | Short, readable, neutral. | Pass |
| 2 | `item.agrochemical.description` | An industrial Joja shortcut. It works fast, but applying it costs 10 soil health. | Um atalho industrial da Joja. Funciona rápido, mas custa 10 de saúde do solo. | Before-use framing names the shortcut **and** the 10-point soil cost. No fertilizer-tier/quality/growth claim. | Clear cause-and-effect; "costs" is non-punitive and factual. | Pass |
| 3 | `message.agrochemical.applied` | Agrochemical applied. The soil is now 10 health lower. | Agroquímico aplicado. A saúde do solo agora está menor. | Immediate confirmation; attributes the soil consequence to the application. Full-penalty wording is only used on the full-penalty path. | Direct, non-shaming. PT omits the number but still states the soil is lower; the exact number is not required for comprehension. | Pass |
| 4 | `message.agrochemical.applied-clamped` | Agrochemical applied only partially: the soil had less health available than the full penalty. | Agroquímico aplicado parcialmente: o solo tinha pouca saúde disponível. | Explicitly says the result is partial and avoids claiming the full 10-point penalty. | Honest boundary wording; not punitive. **Note:** PT says "pouca saúde" (little health) rather than the EN comparative "less than the full penalty"; it still communicates partial application and does not overclaim. | Pass with note |
| 5 | `message.agrochemical.applied-unknown-baseline` | Agrochemical applied. There was no prior soil health reading, so the starting health was treated as 100. | Agroquímico aplicado. O solo estava sem leitura de saúde, então a saúde inicial foi tratada como 100. | States the C2 neutral baseline and that no prior reading existed; invents no history. | Transparent and non-technical enough for players; "treated as 100" is legible. | Pass |
| 6 | `message.agrochemical.invalid-target` | The agrochemical can only be applied to tilled soil. | O agroquímico só pode ser aplicado em solo arado. | Refusal names the eligible target (tilled soil). No success implication. | Clear, calm, actionable. | Pass |
| 7 | `message.agrochemical.missing-input` | Hold a Joja agrochemical to apply it to the soil. | Segure um agroquímico Joja para aplicá-lo no solo. | Refusal names the missing input and the required action. | Direct and non-blaming. | Pass |
| 8 | `message.agrochemical.action-conflict` | Another player is already working this tile. | Outro jogador já está trabalhando neste tile. | Refusal explains first-wins contention without claiming success or blame. | Cozy/non-punitive; "tile" matches existing PT mod copy usage. | Pass |
| 9 | `message.agrochemical.unsupported-authority` | Only the host can apply agrochemicals in multiplayer. | Apenas o anfitrião pode aplicar agroquímicos no multiplayer. | Refusal states the host-authority rule clearly; no local-success implication. | Clear and neutral. | Pass |
| 10 | `message.agrochemical.soil-at-minimum` | This soil is already at the lowest health; the agrochemical would have no effect. | Este solo já está com a saúde mínima; o agroquímico não teria efeito. | Refusal at the 0-health boundary; explicitly says there would be no effect. No false success. | Non-punitive, explains why nothing happened. | Pass |

## Cross-locale meaning parity

- All 10 keys exist in `default.json`, `en.json`, and `pt.json` with non-empty values.
- `default.json` and `en.json` are identical for these keys (English fallback).
- `pt.json` preserves the same shortcut/cost, attribution, boundary-honesty, and refusal
  categories. The only nuance noted is the clamped message's less-explicit comparison (row 4);
  it does not create a misleading full-penalty or success claim.
- No locale claims a mechanical quality/growth/fertilizer-tier buff, a final balance value, or
  prior measurement history.

## Tone and legibility notes

- The copy is short, concrete, and non-shaming. Costs are stated as facts, not punishments.
- The item description works as before-use framing; success/refusal messages work as immediate
  after-use feedback.
- No string is excessively long for a HUD toast or tooltip in the reviewed languages. This is a
  text-level judgment; in-game font/line-wrap behavior was not measured.
- The PT clamped wording (row 4) is the only refinement candidate; it was not changed in this
  pass because it remains honest and comprehensible.

## What this review does not prove

- It is not automated test coverage; `AgrochemicalContentParityTests` remains the lexical proxy.
- It is not a human `verified` attestation.
- It is not SC-004 comprehension evidence; no participant run exists.
- It does not prove in-game rendering, localization loading, font fit, or controller/HUD behavior.
- It does not validate final balance or the provisional `P=10` tuning.
