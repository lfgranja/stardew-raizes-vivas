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

## Phase 10 semantic copy review addendum (2026-11-07) — commit-failed and missing-input ×3 locales

**Append-only; the review above is unchanged.** This addendum reviews the two strings rewritten or
introduced by Phase 10: `message.agrochemical.commit-failed` (new in Phase 10, T097) and
`message.agrochemical.missing-input` (tightened in Phase 10, T104), in `default.json`, `en.json`,
`pt.json`. Review type and boundaries are identical to the original review and the Phase 9
re-review: implementation-agent **AI-assisted** copy review — **not a human attestation, not
`human-verified`, not SC-004, not Gate 3**, not a rendering/font-fit check.

**Key count.** The 017 touched-key set is now **11 keys × 3 locales** (the original 10 reviewed
above plus `message.agrochemical.commit-failed`). `verification/summary.md` line ~26 still reads
"10 keys × 3 locales" — that figure is stale; this addendum is the authority for the 11-key
coverage. (summary.md is not edited by this addendum.)

### Exact strings under review

- **`message.agrochemical.commit-failed`** — `default`/`en`: "The agrochemical could not be
  committed to the soil, so nothing changed. Your agrochemical was not consumed. Please wait for
  the farm data to free up and try again." · `pt`: "Não foi possível aplicar o agroquímico ao
  solo, então ele não foi aplicado. Seu agroquímico não foi consumido. Aguarde os dados da
  fazenda liberarem e tente novamente."
- **`message.agrochemical.missing-input`** — `default`/`en`: "You need to be holding a Joja
  agrochemical when you apply it to the soil — you are not holding one, so nothing happened. Hold
  one in your hand, then try again." · `pt`: "Você precisa estar segurando um agroquímico Joja ao
  aplicá-lo no solo — você não está segurando um, então ele não foi aplicado. Segure um na mão e
  tente novamente."

### Structure (cause + explicit no-success/non-consumption + next step)

| Key | Locale | Cause | Explicit no-success / non-consumption | Next step | Result |
|---|---|---|---|---|---|
| commit-failed | default/en | "could not be committed to the soil" | "so nothing changed. Your agrochemical was not consumed." | "Please wait for the farm data to free up and try again." | Pass |
| commit-failed | pt | "Não foi possível aplicar o agroquímico ao solo" | "então ele não foi aplicado. Seu agroquímico não foi consumido." | "Aguarde os dados da fazenda liberarem e tente novamente." | Pass |
| missing-input | default/en | "You need to be holding … when you apply it to the soil — you are not holding one" | "so nothing happened." | "Hold one in your hand, then try again." | Pass |
| missing-input | pt vs. en | pt renders the no-success clause as "então ele não foi aplicado" (it was not applied) vs. en "so nothing happened" — same implication, fluent pt | ✓ | ✓ | Pass with note |
| missing-input | pt | "Você precisa estar segurando … ao aplicá-lo no solo — você não está segurando um" | "então ele não foi aplicado." | "Segure um na mão e tente novamente." | Pass |
| commit-failed | default ≡ en | byte-identical (English fallback rule) | ✓ | ✓ | Parity |

All five string instances state a cause, an explicit non-application/non-consumption outcome, and
a next step; none implies success, none consumes or credits anything; all three locales agree on
the structure (default≡en identical; pt carries the same three elements).

### FR-012 / parity nuance — pt commit-layer vs. en

- `en` renders the commit-drop with the commit-layer verb **"committed to the soil"**; `pt`
  renders it with the ordinary apply verb **"aplicar"** ("Não foi possível aplicar … então ele
  não foi aplicado"). The commit-layer nuance (a `TrySetSoilHealth` false result, i.e. the farm
  data/commit seam refused, distinct from an ordinary application refusal) is therefore **not
  lexically carried in pt** — a pt player and the pt copy cannot distinguish a commit-drop from an
  ordinary apply failure at the string level.
- This is a **lossy but honest** rendering: nothing is claimed that did not happen; no success,
  no consumption, no mechanical effect is implied in either locale. The distinction that en's
  "committed" encodes is invisible in outcome to the player anyway (same remediation: wait, then
  retry). FR-012 parity (pt mirrors en for these keys) holds at the structural level; it does not
  hold at the commit-vs-apply lexical level, and this addendum records that rather than claiming
  full nuance parity.
- Reciprocally, `en`'s "committed to the soil" leaks implementation vocabulary ("commit") into
  player-facing copy. A player unfamiliar with software terms may read "committed" as
  "applied" anyway; no comprehension hazard of consequence was identified, but the phrasing is a
  wording-pass candidate (e.g. "could not be applied to the soil because the farm data was busy"),
  deliberately **not changed here** — this is a review, not a copy edit.
- `pt` missing-input diverges from `en` in the no-success clause ("ele não foi aplicado" vs. "so
  nothing happened") — same implication, fluent pt rendering; accepted.

### Cozy / non-punitive tone

Both messages are diagnostic, not blaming. `commit-failed` explicitly protects the player's item
("not consumed") and asks politely ("Please wait…" / "Aguarde…"). `missing-input` states a fact
about what the player is holding without scolding ("you are not holding one"). Neither string
shames, moralizes, or implies recklessness; both preserve the factual, non-punitive voice
established in the Phase 9 rewrite. Tone: Pass.

### Legibility (player understanding vs. implementation jargon)

- `en` commit-failed: "committed" and "the farm data to free up" are implementation-adjacent
  (farm-data lock/commit semantics). Understandable as "the farm is busy; try again shortly"
  for most players; not a false claim; legibility acceptable with the jargon note above.
- `pt` commit-failed: "os dados da fazenda liberarem" is the same farm-data concept rendered
  literally; same acceptability; a future pt pass could say "aguarde a fazenda liberar" — not
  changed here.
- `missing-input` is fully legible in both locales; no jargon.
- Neither string was measured against an in-game font/HUD (rendering unverified, as with all
  prior rows of this review).

### What this addendum does not prove

Not automated coverage (`AgrochemicalContentParityTests` remains the lexical proxy); not a human
`verified` attestation; not SC-004 comprehension evidence (no participant run exists); not
in-game rendering/font-fit; not final balance/`P=10` tuning; not Gate 3. It does not change any
claim status.

### Row-52 re-anchor (claim-records.md)

This addendum is the post-Phase-10 anchor for the claim-records **row 52** scope note (the
AI-assisted copy review), now covering commit-failed and missing-input alongside the previously
reviewed keys (11 keys × 3 locales). Row 52 itself is intentionally not edited; its scope note
("Review performed by the implementation agent … does not satisfy human-verified … not SC-004")
stands against the strings reviewed here. The claim remains a narrow copy-review claim; it is not
`human-verified`, not SC-004, and not in-game rendering evidence.

## Corrective addendum (2026-11-07, Phase 12 / T120) — T116 superseded by the shipped T109 copy

**Append-only; nothing above is modified, re-dated, or rewritten.** This addendum corrects a
material accuracy defect in the Phase 10 semantic copy review addendum above (the "T116" addendum,
2026-11-07): it reviewed and endorsed the **pre-T109** copy of `message.agrochemical.commit-failed`,
which T109 (2026-11-07) rewrote into the shipped, actionable capacity-remedy strings. The T116
addendum is retained verbatim above as a record of what was reviewed at the time; its commit-failed
content is **superseded** by this addendum. The T116 review of `message.agrochemical.missing-input`
remains accurate — that key was not touched by T109 (see "Missing-input unchanged" below).

Review type and boundaries are identical to the original review and the T116 addendum:
implementation-agent **AI-assisted** copy review — **not** a human attestation, **not**
`human-verified`, **not** SC-004, not Gate 3, not a rendering/font-fit check, not a balance check.
No claim is promoted by this addendum; the acceptance recorded here for the shipped strings is a
copy-review judgment, not evidence of any kind beyond that narrow scope.

### Why the T116 addendum went stale (disclosure)

T116 was authored against the tree at the time and quoted the commit-failed copy that shipped
before T109 landed (`da6f7c7` pt, `cda08fa` en, `047d589` default, `b21d485` ModController fallback
sync — all 2026-11-07). The strings it quoted and marked "Pass" are the pre-T109 strings:

- pre-T109 `default`/`en` (verbatim): "The agrochemical could not be committed to the soil, so
  nothing changed. Your agrochemical was not consumed. Please wait for the farm data to free up
  and try again."
- pre-T109 `pt` (verbatim): "Não foi possível aplicar o agroquímico ao solo, então ele não foi
  aplicado. Seu agroquímico não foi consumido. Aguarde os dados da fazenda liberarem e tente
  novamente."

The T116 addendum's commit-failed findings — including its "next step: 'Please wait for the farm
data to free up and try again.' | Pass" row, its legibility note ("Understandable as 'the farm is
busy; try again shortly'"), and its FR-012 note ("same remediation: wait, then retry") — describe
copy that **no longer ships**. The pre-T109 next step was also not actionable for the player: the
DoS caps whose refusal this message reports (`MaxLocationsPerSave` = 50,
`MaxTilesPerLocation` = 500) are hard guards that never free up at runtime, so "wait … and try
again" named a remedy that does not exist. Why T109 rewrote it, and the disposition of that
rewrite, are recorded in `verification/convergence-phase11-addendum.md` (Phase 12 / T121).

### The shipped strings as of T109 (quoted verbatim; verified 2026-11-07 against `LivingRoots/i18n/`)

- **`message.agrochemical.commit-failed`** — `default` ≡ `en` (byte-identical, English-fallback
  rule; commit `047d589`):

  > "The agrochemical could not be committed to the soil, so nothing changed. Your agrochemical
  > was not consumed. The farm data is at capacity; apply the agrochemical to a tile that already
  > has soil data, then try again later."

  `pt` (commit `da6f7c7`):

  > "Não foi possível gravar no solo, então nada mudou. Seu agroquímico não foi consumido. Os
  > dados da fazenda estão no limite; aplique em um tile que já tenha leitura de solo e tente
  > novamente mais tarde."

### Structure (cause + explicit non-change/non-consumption + actionable capacity remedy)

| Key | Locale | Cause | Explicit no-change / non-consumption | Next step (capacity remedy) | Result |
|---|---|---|---|---|---|
| commit-failed | default/en | "could not be committed to the soil" | "so nothing changed. Your agrochemical was not consumed." | "The farm data is at capacity; apply the agrochemical to a tile that already has soil data, then try again later." | Pass |
| commit-failed | pt | "Não foi possível gravar no solo" | "então nada mudou. Seu agroquímico não foi consumido." | "Os dados da fazenda estão no limite; aplique em um tile que já tenha leitura de solo e tente novamente mais tarde." | Pass |
| commit-failed | default ≡ en | byte-identical | ✓ | ✓ | Parity |

- **Cause.** en pins the commit-layer diagnosis ("could not be committed to the soil" — a
  `TrySetSoilHealth` false result, distinct from an ordinary application refusal). pt renders the
  same seam as "gravar no solo" ("to write … into the soil"); see the pt rendering decision below.
- **Explicit non-consumption.** en: "so nothing changed. Your agrochemical was not consumed."
  pt: "então nada mudou. Seu agroquímico não foi consumido." Both protect the player's item and
  state that nothing happened; neither locale implies success, crediting, or consumption.
- **Next step (actionable).** en: "The farm data is at capacity; apply the agrochemical to a tile
  that already has soil data, then try again later." pt: "Os dados da fazenda estão no limite;
  aplique em um tile que já tenha leitura de solo e tente novamente mais tarde." The remedy is
  truthful for the DoS caps: the guards refuse **new** location/tile entries only, and updating a
  tile that already has soil data does not add an entry, so it is not cap-gated. The named action
  exists, is player-executable, and is the same remedy in both languages. The pre-T109 "wait for
  the … to free up" remedy implied that time alone would clear the refusal — not true of a hard
  cap — which is why the T116 "Pass" cannot stand.

### FR-012 / parity and the pt rendering decision (accepted — the T121 decision)

- `default` ≡ `en`, byte-identical (English-fallback rule). Parity at the byte level holds.
- `pt` carries the same three structural elements (cause / explicit non-change and
  non-consumption / remedy next step). FR-012 parity holds at the **structural level** in all
  three locales.
- **pt rendering decision (recorded here and in `convergence-phase11-addendum.md` / T121):**
  "Não foi possível **gravar** no solo" is accepted as the pt rendering of the commit-layer
  diagnosis (the farm-data commit/write seam refusing). "Gravar" is ordinary, legible Portuguese
  for "to write/record"; the T116-era pt used the ordinary apply verb ("aplicar"), which was
  recorded then as *lossy but honest* because the commit-layer nuance was lexically absent. The
  shipped pt is closer to en than the pre-T109 pt on every structural element, and the T109
  rewrite moves the pt **into** lexically carrying a commit/write-seam diagnosis.
- **Accepted residual drift, recorded explicitly:** pt does not carry the en loan-verb "commit"
  and deliberately avoids software jargon ("commitar"); "gravar no solo" renders the seam rather
  than translating the token. pt also renders the no-change clause as "então nada mudou"
  (nothing changed) and the cap clause as "os dados da fazenda estão no limite" (the farm data is
  at the limit) — direct mappings with no meaning change. No nuance-claim beyond structural
  parity (cause / non-change / non-consumption / remedy) is made for pt.
- `en`'s "committed to the soil" still leaks implementation vocabulary; this phrasing is pinned by
  the content-parity cause tokens (`EnglishCommitFailedCauseTokens`) and was deliberately retained
  by T109. It remains a wording-pass candidate, not executed here; no comprehension hazard of
  consequence was identified.
- The `AgrochemicalEnglishFallbacks` literal in `ModController.cs` matches the shipped `en`
  byte-identically as of `b21d485` (re-verified 2026-11-07; see T121 record).

### Cozy / non-punitive tone (re-affirmed for the shipped strings)

Both shipped commit-failed strings are diagnostic and protective, not blaming: they state the
cause plainly, protect the player's item ("not consumed" / "não foi consumido"), and give a
concrete, achievable next step instead of a vague wait. Neither shames, moralizes, implies
recklessness, or overclaims effect. The factual, non-punitive voice of the Phase 9 rewrite is
preserved. Tone: Pass (re-affirmed on the shipped strings, not inherited from the T116 text).

### Legibility

- `en` "committed" and "the farm data is at capacity" are implementation-adjacent; understandable
  as "the farm is at its data limit; use an already-tracked tile" for most players. Not a false
  claim; acceptable with the jargon note above.
- `pt` "os dados da fazenda estão no limite" and "aplique em um tile que já tenha leitura de solo"
  are ordinary Portuguese and consistent with the in-game soil-reading concept; no jargon is
  introduced by the rewrite.
- Neither string was measured against an in-game font/HUD (rendering unverified, as with all rows
  of this review).
- The T109 next step names a player-executable action; the pre-T109 "wait" was not. Legibility
  of the *remedy* improved with the rewrite.

### Missing-input — unchanged by T109, remains accurate as reviewed

`message.agrochemical.missing-input` was **not** touched by T109. Its shipped copy is
byte-identical to what the T116 addendum reviewed and passed (×3 locales):

- `default`/`en`: "You need to be holding a Joja agrochemical when you apply it to the soil —
  you are not holding one, so nothing happened. Hold one in your hand, then try again."
- `pt`: "Você precisa estar segurando um agroquímico Joja ao aplicá-lo no solo — você não está
  segurando um, então ele não foi aplicado. Segure um na mão e tente novamente."

The T116 structure/parity/tone/legibility findings for this key (Pass; Pass with note on the
pt no-success clause "então ele não foi aplicado" vs. en "so nothing happened" — same
implication, fluent pt rendering) stand unmodified.

### Row-52 re-anchor (claim-records.md)

Claim-records row 52 — the AI-assisted copy-review claim produced by T052, most recently
re-anchored by the T116 addendum — is **re-anchored to the shipped (post-T109) commit-failed
strings and the unchanged missing-input strings by this addendum.** Row 52 itself is
intentionally **not edited** (this addendum does not touch `claim-records.md`); the re-anchor
takes effect by this text. The T116 addendum's re-anchor text is superseded for the
commit-failed scope only. Row 52 remains a narrow copy-review claim covering 11 keys × 3 locales
(count unchanged); it is not `human-verified`, not SC-004, and not in-game rendering evidence.
No claim status changes anywhere in the register; D1 / human-verified / SC-004 remain `pending`;
the two `blocked` rows remain `blocked`; the host-only machine-pass rows are unaffected.

### summary.md note (unchanged by this addendum)

`verification/summary.md` (~line 26) still reads "10 keys × 3 locales"; per the T116 addendum the
current touched-key set is **11 keys × 3 locales**, and this addendum reaffirms that figure.
summary.md is not edited here (Phase 12 / T120 does not own that file); the line remains stale
pending a maintainer-scoped edit.

### What this addendum does not prove

Not automated coverage (`AgrochemicalContentParityTests` remains the lexical proxy, including its
`PortugueseCommitFailedCauseTokens` pins); not a human `verified` attestation; not SC-004
comprehension evidence (no participant run exists); not in-game rendering/font-fit or HUD
behavior; not final balance/`P=10` tuning; not Gate 3. It changes no claim status. The T116
addendum above is retained verbatim for provenance; on conflict, the shipped-string findings in
this addendum govern for `message.agrochemical.commit-failed`.
