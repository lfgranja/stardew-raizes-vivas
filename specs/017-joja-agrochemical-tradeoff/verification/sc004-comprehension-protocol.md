# SC-004 Comprehension Protocol (Spec 017 — Joja / Agrochemical Tradeoff MVP)

## Purpose and linkage

**Purpose:** Define a minimal, buildable playtest comprehension protocol for evaluating whether participants understand the Joja/agrochemical MVP tradeoff at first supported use.

**Linked spec requirements:**
- [`../spec.md`](../spec.md) **SC-004** — playtest follow-up evidence (not automated/runner Gate 3). Target: >=90% pass when a comprehension check is run. If not run, SC-004 remains **pending qualitative follow-up** and MUST NOT be claimed complete.
- [`../spec.md`](../spec.md) **FR-002** — input presented as an industrial shortcut with a soil consequence before use, using clear player-facing framing consistent with the mod’s cozy, non-punitive tone.
- [`../spec.md`](../spec.md) **FR-007** — immediate, legible after-use feedback that attributes the soil consequence to the Joja/agrochemical choice.

**Evidence class:** `qualitative-follow-up`

**Protocol/execution status:** `pending`  
*Allowed values: `pending | complete | blocked | infeasible`.* **Do not mark `complete` without a recorded run.** Default is `pending` until the protocol is executed and evidence is recorded.

**Finalization note:** T045 finalized the target scope, timing, prompt, scoring, and evidence fields on
2026-11-06. No participant run has been executed. The status remains `pending`; this document is a
protocol, not SC-004 evidence.

## Participant and context scope

**Target scope (defined before run):**
- **Participant profile:** Adults (18+) who have played Stardew Valley before, using any mix of
  modded/unmodded experience. They must not have prior knowledge of spec 017, its implementation,
  or this protocol.
- **Context:** A host-controlled validation save (single-player, or the host in a multiplayer
  session) with at least one supported tilled farm or greenhouse tile and the Joja/agrochemical
  input available through the validation-only provisioning path. Use the participant's preferred
  supported locale (`default`/`en`/`pt`); record the actual locale. Use a release build with the
  017 MVP behavior enabled.
- **Inclusion criteria:** Can read the selected locale; consents to a short comprehension check;
  is seeing the Joja/agrochemical option for the first time in this session.
- **Exclusion criteria:** Maintainers/authors of the 017 feature or spec; anyone previously briefed
  on SC-004 scoring or the expected tradeoff; anyone who already used the item in an earlier
  session; anyone who cannot answer independently.

**Actual scope (record when run):**
- **Participants recruited:** _[actual count and brief profile summary]_
- **Actual context/build/scenario:** _[actual values, including locale]_
- **Deviations from target scope:** _[none / describe]_

## First-use timing

Administer the comprehension check **before or immediately after the participant’s first supported use** of the Joja/agrochemical choice.

**Recorded timing used (when run):** _[`before first supported use` | `immediately after first supported use` | other — explain]_

**Timing note:** The check should capture first-use comprehension. Do not substitute delayed recall unless explicitly recorded as a scope deviation.

## Prompt / instrument

Ask the participant to identify **both** elements of the tradeoff:

1. **Short-term shortcut:** What short-term benefit/shortcut does the Joja/agrochemical choice provide right now?
2. **Soil-health cost:** What soil-health cost/consequence does that same choice cause?

**Prompt version:** `sc004-v1` — use the wording below. If the facilitator changes wording, record the
deviation and the exact final wording with the run evidence.

**Suggested neutral prompt text (adapt only if needed; record final wording):**
> “After seeing/using this Joja/agrochemical option for the first time, please tell me: (1) what short-term shortcut or immediate benefit it gives you, and (2) what cost it has for soil health.”

**Instrument notes:** Use the same prompt version across participants in a run where possible. Record any facilitator clarification that may affect scoring.

## Scoring

**Scoring type:** Binary `pass | fail`.

**Pass requires both elements correct:**
- Correctly identifies the **short-term shortcut/immediate benefit** of the Joja/agrochemical choice (player-understandable shortcut/confirmation framing; does **not** need to cite internal numbers or claim mechanical 014/015/vanilla fertilizer-tier buffs).
- Correctly identifies the **soil-health cost/consequence** of that same choice (soil is harmed/penalized/degraded in a bounded, recoverable sense as communicated by MVP framing; does **not** need to cite exact penalty magnitude).

**Fail:** Missing either element, conflating elements, attributing a false mechanical buff (e.g., claims improved 014/015 quality/growth or vanilla fertilizer-tier benefit), denying the soil cost, or giving an answer too ambiguous to support both required elements.

**Objective scoring guidance:**
- Score from the participant’s stated understanding, not from whether they liked the choice.
- If wording differs but meaning clearly covers both required elements, score `pass`.
- If facilitator intervention supplied an element, record it and score conservatively; document in observations.

## Evidence note fields (record when run)

| Field | Value |
|---|---|
| Date | _[YYYY-MM-DD]_ |
| Facilitator/recorder | _[name/role]_ |
| Sample size / denominator | _[N]_ |
| Pass count | _[n pass]_ |
| Fail count | _[n fail]_ |
| Pass rate (%) | _[(pass count / denominator) * 100]_ |
| Timing used | _[before / immediately after first supported use]_ |
| Prompt version | _[version + exact wording or reference]_ |
| Participant/context summary | _[anonymized; actual scope]_ |
| Anonymized observations/quotes (optional) | _[brief notes; remove identifying details]_ |
| Artifact paths/screenshots (if any) | _[relative paths or N/A]_ |
| Scope note | _[what this evidence does/does not support]_ |

## Target

**Target:** **>= 90% pass rate** (participants correctly identifying both short-term shortcut and soil-health cost).

**Interpretation:** Meeting the target supports SC-004 only for the recorded scope/context/sample. It does **not** constitute automated evidence, runner Gate 3 evidence, human `verified` attestation, or D1/farmhand convergence.

## Claim record stub

Use this lightweight record for any SC-004 claim. Do not promote status above available evidence.

| Field | Value |
|---|---|
| Claim | SC-004 comprehension follow-up: participants understand short-term shortcut and soil-health cost of Joja/agrochemical choice at first supported use. |
| Evidence class | `qualitative-follow-up` |
| Source artifact path | `specs/017-joja-agrochemical-tradeoff/verification/sc004-comprehension-protocol.md` _[append run evidence artifact path(s) when available]_ |
| Status | `pending` _[change only to `complete`, `blocked`, or `infeasible` based on recorded evidence/constraints]_ |
| Scope note | _[pending execution / or describe actual sample, context, timing, and limits]_ |

## Provenance / honesty note

- **No retroactive promotion:** Prior evidence or artifacts MUST NOT be rewritten or retroactively upgraded to satisfy SC-004.
- **No implementation claims:** This document defines a protocol template only; it does not claim the feature was playtested or comprehended.
- **Pending until run:** If the comprehension check is not run, SC-004 remains **pending qualitative follow-up** and MUST NOT be claimed complete.
- **Evidence boundary:** SC-004 is **not** automated evidence and **not** runner Gate 3 evidence. Any completed run is qualitative follow-up evidence limited to its recorded scope.
- **Completeness rule:** Do not mark status `complete` without a recorded run containing at minimum date, facilitator/recorder, denominator, pass/fail counts, pass rate, timing used, prompt version, and scope note.
