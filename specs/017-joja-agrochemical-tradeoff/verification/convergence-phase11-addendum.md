# Phase 11 Disposition Record — T109 commit-failed capacity-remedy rewrite

**Date:** 2026-11-07 · **Phase 12 / T121** · **Author:** implementation agent (AI-assisted)
**Type:** append-only disposition record (new file). No claim is promoted; no status is changed
anywhere. Companion to the T120 corrective addendum in `manual-review.md` (same date, same
author) and a parent to T124's final-wave verification. Prior artifacts are not rewritten.

**Scope:** the `message.agrochemical.commit-failed` key ×3 locales, rewritten by T109 in Phase
11. The two commits that made the shipped state current, plus the fallback sync:
`da6f7c7` (pt), `cda08fa` (en), `047d589` (default ≡ en), `b21d485` (`ModController.cs`
English-fallback literal synced to the shipped en copy).

## (i) What was wrong with the pre-T109 copy

Pre-T109, `default`/`en` shipped (quoted verbatim from `cda08fa`'s pre-image):

> "The agrochemical could not be committed to the soil, so nothing changed. Your agrochemical
> was not consumed. Please wait for the farm data to free up and try again."

and `pt` shipped (quoted verbatim from `da6f7c7`'s pre-image):

> "Não foi possível aplicar o agroquímico ao solo, então ele não foi aplicado. Seu agroquímico
> não foi consumido. Aguarde os dados da fazenda liberarem e tente novamente."

The next step — "Please wait for the farm data to free up and try again." / "Aguarde os dados da
fazenda liberarem e tente novamente." — was **unactionable** and dishonest to the player:

- The condition that produces this message is the DoS capacity guard refusing a **new**
  location/tile entry. Those guards are `ModConstants.MaxLocationsPerSave = 50` and
  `ModConstants.MaxTilesPerLocation = 500` (`LivingRoots/Constants.cs`), enforced at runtime in
  the cache paths of `SoilHealthService` (location-count guard, tile-count guard, runtime-cache
  guards — see `LivingRoots/Services/SoilHealthService.cs`). They are hard caps: they refuse
  additions but **never free up at runtime** during ordinary play. Nothing the player can wait
  for, and no player action short of removing data (which the mod does not support at MVP),
  lowers the count.
- The pre-T109 next step therefore named a remedy that does not exist; it failed the Phase-9
  refusal-structure minimum (cause + explicit non-application + **actionable** next step) and
  implied success-by-waiting, which the runtime cannot produce.
- The same text also leaked the wait-based remediation into the FR-012 nuance note of the T116
  addendum ("same remediation: wait, then retry"), which is precisely why that addendum was
  reviewed against stale copy and why T120 supersedes its commit-failed scope.

## (ii) The decision — replace the next step with the capacity remedy

T109 rewrote the next step in all three locales to the truthful, actionable remedy: apply the
agrochemical to a tile that already has soil data. Updating an already-tracked tile adds **no new
entry**, so it is not cap-gated and the named action is achievable. Shipped strings (verified
verbatim 2026-11-07 against `LivingRoots/i18n/`):

- `default` ≡ `en` (byte-identical, English-fallback rule; `047d589`):

  > "The agrochemical could not be committed to the soil, so nothing changed. Your agrochemical
  > was not consumed. The farm data is at capacity; apply the agrochemical to a tile that already
  > has soil data, then try again later."

- `pt`:

  > "Não foi possível gravar no solo, então nada mudou. Seu agroquímico não foi consumido. Os
  > dados da fazenda estão no limite; aplique em um tile que já tenha leitura de solo e tente
  > novamente mais tarde."

What was kept deliberately: the pinned cause phrase (`could not be committed to the soil`,
`EnglishCommitFailedCauseTokens` cause pin; pt renders the same seam as `gravar no solo`,
`PortugueseCommitFailedCauseTokens` admits `não foi possível`), the explicit no-consumption /
no-change outcome ("so nothing changed. Your agrochemical was not consumed." / "então nada mudou.
Seu agroquímico não foi consumido."), and the `… at capacity` / `… no limite` diagnosis. What
changed is only the next step: unactionable waiting → actionable capacity remedy.

## (iii) pt rendering decision — "gravar no solo" accepted as the commit-layer diagnosis

Decision recorded here (and relied on by the T120 addendum in `manual-review.md`): pt ships
"Não foi possível **gravar** no solo" as the commit-layer rendering — the farm-data commit/write
seam refusing (a `TrySetSoilHealth` false result), not an ordinary application refusal.

- "Gravar" (to write/record) is ordinary, legible Portuguese; it keeps player-facing copy out of
  software jargon ("commitar" was rejected deliberately) while now carrying the commit/write-seam
  nuance that the pre-T109 pt ("Não foi possível **aplicar** …") lexically lacked — the absence
  the T116 addendum had recorded as "lossy but honest".
- **Accepted residual drift, stated explicitly:** pt does not reuse the en loan-verb "commit"; it
  renders the outcome clause as "então nada mudou" (nothing changed) and the cap clause as "os
  dados da fazenda estão no limite" (the farm data is at the limit), and the remedy as "aplique
  em um tile que já tenha leitura de solo … tente novamente mais tarde". These are direct
  mappings with no meaning change, but no claim of lexical (token-level) parity with en is made.
  FR-012 parity holds at the **structural level** in all three locales: cause / explicit
  non-change and non-consumption / remedy next step. Structural parity is the acceptance bar.
- Structural parity supersedes the T116 "same remediation: wait, then retry" note for the pt
  leg: both locales now name the **same** remedy (a tile with existing soil data); the pre-T109
  divergence in remediation no longer exists.
- No pt copy change is proposed or implied by this record.

## (iv) No claim promoted

This record changes **no** claim status anywhere: no claim is promoted, broadened, or
re-scoped by it. The AI-assisted copy-review claim remains narrow; `human-verified`, D1 (farmhand
convergence), and SC-004 remain **`pending`**; the two `blocked` register rows (normal-play
acquisition; measured-zero save/load persistence) remain **`blocked`**; the host-only Gate 3
machine-pass rows are unaffected and their scope (measured 50 → 40 path on the attested tree)
is unchanged. No Gate 3 re-run was performed or is implied. This record is disposition
documentation, not evidence of runtime behavior.

## (v) Row-52 re-anchor reference

`verification/claim-records.md` row 52 — the AI-assisted copy-review claim produced by T052
(referenced as "row 52" in `manual-review.md` and its addenda) — is re-anchored to the **shipped
(post-T109)** commit-failed strings and the unchanged missing-input strings by the T120
corrective addendum in `manual-review.md` ("Corrective addendum (2026-11-07, Phase 12 / T120)").
The row itself is **not edited** (this record does not touch `claim-records.md` either); the
re-anchor takes effect by that addendum's text. The T116 addendum's row-52 re-anchor is
superseded for the commit-failed scope only.

## (vi) Fallback literal matches shipped en since b21d485

The English fallback literal in `LivingRoots/Controllers/ModController.cs`
(`AgrochemicalEnglishFallbacks[ModConstants.TranslationKeyAgrochemicalCommitFailed]`) has matched
the shipped `en.json` copy **byte-identically since `b21d485`** (2026-11-07; and `default.json`
≡ `en.json` since `047d589`). Equality was re-verified in-session on 2026-11-07 by programmatic
string comparison of the three sources (`en.json` ≡ `default.json` ≡ the fallback literal;
exact-match check). No code is changed or implied to change by this record.

## Provenance and verification basis

- T109 commits (2026-11-07): `da6f7c7` (pt), `cda08fa` (en), `047d589` (default ≡ en),
  `b21d485` (fallback literal sync). T109 re-ran the content-parity pins as part of the
  committed Phase-11 targeted run (`verification/convergence-phase11-targeted.log`, 123/123,
  exit 0 — committed by T119); this record adds no new test execution. The T115 remarks refresh
  and the capacity-guard pins (`AgrochemicalCapacityTests`, `AgrochemicalApplicationServiceTests`
  capacity rejection at `MaxTilesPerLocation`/`MaxLocationsPerSave`) live in the same suite.
- Caps: `MaxLocationsPerSave = 50`, `MaxTilesPerLocation = 500` — `LivingRoots/Constants.cs`;
  runtime enforcement — `LivingRoots/Services/SoilHealthService.cs` (load-path abort and
  runtime-cache guards refuse **new** entries; existing entries update unchanged).
- Missing-input copy was **not** changed by T109; its review stands as recorded in the T116
  addendum and is re-affirmed in the T120 corrective addendum.
- Not proven by this record: automated coverage status beyond the committed logs; human
  attestation; D1; SC-004; Gate 3; in-game rendering/font-fit; final balance/`P=10`. Per the
  repo rule, nothing here attests the Phase 12 wave — `T124` alone closes the wave.
