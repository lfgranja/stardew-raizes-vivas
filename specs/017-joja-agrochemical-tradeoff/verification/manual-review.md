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

## Post-Phase-9 re-review (2026-11-06) — rewritten copy after T065/T068/T081

**Append-only; the review above is unchanged.** Re-review of the touched 017 keys after the
Phase 9 rewrite (refusal structure per FR-008, bounded/recoverable description clause per
FR-002, pt magnitude/comparison per FR-012/FR-014). Same review type and boundaries as above:
implementation-agent AI-assisted review, not a human attestation; not SC-004; not a rendering
check.

### Rewritten refusals (cause + explicit non-application + next step, ×3 locales)

| # | Key | `default` / `en` (pt mirrors, see below) | Cause stated | Explicit non-application | Next step | Result |
|---:|---|---|---|---|---|---|
| 6 | `message.agrochemical.invalid-target` | "The agrochemical can only be applied to tilled soil, so it was not applied. Till that soil first, then try again." | Tilled-soil-only target rule | "so it was not applied" | "Till that soil first, then try again." | Pass |
| 7 | `message.agrochemical.missing-input` | "Hold a Joja agrochemical to apply it to the soil; nothing happened. Try again with one in hand." | Not holding the input | "nothing happened" | "Try again with one in hand." | Pass |
| 8 | `message.agrochemical.action-conflict` | "Another player is already working this tile, so the agrochemical was not applied. Wait for the tile to free up, then try again." | First-wins lock contention | "so the agrochemical was not applied" | "Wait for the tile to free up, then try again." | Pass |
| 9 | `message.agrochemical.unsupported-authority` | "Only the host can apply agrochemicals in multiplayer, so it was not applied. Ask the host to apply it for you." | Host-authority rule (C4) | "so it was not applied" | "Ask the host to apply it for you." | Pass |
| 10 | `message.agrochemical.soil-at-minimum` | "This soil is already at the lowest health, so the agrochemical was not applied. Let the soil regenerate with some care, then try again." | Minimum-health boundary | "so the agrochemical was not applied" | "Let the soil regenerate … then try again." | Pass |

`pt` carries the same three elements in each of the five refusals (e.g. `soil-at-minimum`:
"…então o agroquímico não foi aplicado. Deixe o solo se regenerar com um pouco de cuidado e
tente novamente."). All five refusals now state the cause, an explicit no-success/non-application
outcome, and a next-step implication in all three locales — the structure the Phase 9 parity
tests assert. No refusal implies an application occurred; none is punitive or blaming.

### item.agrochemical.description — bounded/recoverable clause

`default`/`en`: "An industrial Joja shortcut. It works fast, but applying it costs 10 soil
health. **The soil consequence is bounded and recoverable through your usual soil care.**"
`pt`: "Um atalho industrial da Joja. Funciona rápido, mas custa 10 de saúde do solo. A
consequência para a saúde do solo é limitada e o solo se recupera com cuidado."
The new clause names the consequence as bounded and recoverable through ordinary soil care.
It does not claim invulnerability, does not claim a final balance value, and does not imply a
fertilizer-tier/quality buff. Honest before-use framing, non-punitive. Pass.

### pt applied magnitude and clamped full-penalty comparison

- `message.agrochemical.applied` (pt) now carries the magnitude: "Agroquímico aplicado. A
  saúde do solo agora está **10 pontos** menor." — parity with the `default`/`en` "10 health
  lower" wording. Pass.
- `message.agrochemical.applied-clamped` (pt) now states the full-penalty comparison
  explicitly: "Agroquímico aplicado parcialmente: a perda de saúde do solo foi **menor do que
  a penalidade completa** desta aplicação, pois o solo tinha pouca saúde disponível." — the
  earlier "pouca saúde" ellipsis is resolved; the comparative against the full penalty is now
  legible. Pass.

### Tone re-affirmation (cozy / non-punitive)

The rewritten strings keep the cost factual, not punishing: refusals explain why nothing
happened and what to do next; the description bounds the consequence and points at ordinary
soil care as recovery; the clamped message discloses the partial outcome without scolding. No
string shames, threatens, or implies recklessness. The 10-point magnitude is stated as a fact
in before-use and after-use framing. Conclusion reaffirmed for the new strings:
cozy/non-punitive, legible in all three locales, no overclaim of success, no false
full-penalty claim, no balance claim.

### Row-52 scope re-anchor (claim-records.md)

This addendum is the post-Phase-9 anchor for the claim-records **row 52** scope note (the
AI-assisted copy review). Row 52 itself is intentionally not edited; its scope note
("Review performed by the implementation agent … does not satisfy human-verified … not
SC-004") now stands against the rewritten strings reviewed here. The claim remains a narrow
copy-review claim; it is not `human-verified`, not SC-004, and not in-game rendering evidence.

### What this addendum does not prove

Unchanged from the original review: not automated coverage, not a human `verified`
attestation, not SC-004 comprehension evidence, not in-game rendering/font-fit, not final
balance/tuning of the provisional `P=10`.
