# Converge → Phase 9 / Phase 10 traceability — Spec 017 (Joja / Agrochemical MVP)

**Date:** 2026-11-06 · **Produced by:** Phase 10 task T103 · **Status:** append-only provenance record.
Nothing here promotes a claim; D1, human `verified`, and SC-004 remain `pending`.

**Filename note:** `tasks.md` (T103) suggested `converge-assessment-017.md`; this file is named
`convergence-assessment-017.md` per the delegating instruction. Same artifact.

## Provenance of this reconstruction

The 017 converge assessment (the F1–F26 finding set and the V-01..V-15 re-audit) was produced in
the converge session; **no F-level or V-level finding detail is committed to the repository.**
This reconstruction is anchored to:

1. Commit `cbacc34` ("docs(tasks): append Phase 9 convergence tasks — … F1-F26 …"), which
   appended Phase 9 tasks T064–T089 with their FR/spec references and themes.
2. `tasks.md` Phase 9 preamble, which commits two anchors verbatim:
   "i18n trio = F2/F5/F18" and "test files = F19/F21/F22/F24".
3. `tasks.md` Phase 10 preamble ("V-01..V-15 re-audit") and the T090–T104 task descriptions.
4. The Phase 9 Wave A/B commit subjects quoted in `verification/convergence-rerun.md`
   (reproduced below), which tie each task to its fixing commit(s).

Every mapping row below is labeled **[committed]** (anchored in 2 or quoted in 4) or
**[inferred]** — inferred from commit cbacc34 + the converge session log; not from committed
F-level evidence. Where only a theme anchor exists (commit cbacc34's message), it is quoted.

## F1–F26 → Phase 9 (T064–T089) dispositions

| F-ID | Finding theme (as recoverable) | Closing task(s) | Closing evidence (commits/artifacts) | Anchor |
|---|---|---|---|---|
| F1 | Commit-honesty seam: soil write could be silently dropped (capacity rejection) while reporting success | T064 | e9b8044 (domain contract), f5cabb7 (`TrySetSoilHealth` impl), 412d94d (service commit → honest non-success), 1cd0265 (DIM scaffold), RED fbbad22, tests ea7bd2a. Phase 10: seam named in contracts T101(c) | [inferred] |
| F2 | Refusal copy lacks cause + explicit no-success + next step (FR-008) | T065 | e7cf687, fdf4b7e, c3f7297 (×3 locales); RED parity theory de52e6a. Phase 10: re-reviewed in `manual-review.md` addendum (T093) | [committed] (i18n trio) |
| F3 | No FR-004 composition proof (agrochemical vs harvest-quality outcome) | T066 | No behavioral proof exists; accepted-risk record `verification/double-counting-risk.md` (00e26fe), re-flagged in T092(b) addendum | [inferred]; theme matches cbacc34 message ("FR-004 composition test") |
| F4 | Normal-play acquisition path unresolved | T067 | Descope decision recorded in `verification/provisioning-validation.md` (28125f4); blocked claim row appended (6361d46; register row 57) | [inferred]; theme matches cbacc34 message ("acquisition … decisions") |
| F5 | Bounded/recoverable consequence not communicated to players (FR-002) | T068 | e7cf687, fdf4b7e, c3f7297 (description clause ×3); RED 0fa5bfa. Phase 10: re-reviewed (T093) | [committed] (i18n trio) |
| F6 | Icon-strategy obligation outstanding (FR-013) | T069 | Decision recorded in `contracts/content-registration.md`: vanilla springobjects fallback, explicit acceptance, render legibility unverified in-game (3ae46fc, section-separation fix 564ced1) | [inferred]; theme matches cbacc34 message ("icon … decisions") |
| F7 | `RefusedSoilAtMinimum` unadjudicated | T070 | Adjudicated in `contracts/agrochemical-application-service.md` (ad619e4, section-separation fix 619e3c5): no write/consume/broadcast, spec-faithful FR-005/FR-008 reading; `data-model.md` validation rule 12 (938aa3c). Phase 10: Refusal-classes amended table appended (T101(c)) | [inferred] |
| F8 | US1/AC3 recovery after application untested (compost after agrochemical) | T071 | Recovery characterization test through real `SoilHealthService`: unknown-baseline apply (90) then compost path, bounded in-range recovery (20f380b) | [inferred] |
| F9 | Claim register had no automated schema/honesty validator (FR-015, SC-005) | T072 | `scripts/tests/test-017-claims.sh` (5de6ebf); wired into shell suite; hardened in Phase 10 (T095, 4d0da8f) | [inferred]; theme matches cbacc34 message ("claim-register validator") |
| F10 | Measured-zero result lost across save/load (sparse-cache zero omission) | T073 | `verification/measured-zero-persistence.md` (0c5e21c); blocked claim row appended (26a1265; register row 58) | [inferred] |
| F11 | Refusal result shape fabricated `WasClamped=true`/0f readings; NaN semantics undocumented | T074 | f28029c (honest invalid-target result), 4e5b199 (enum remarks + NaN sentinel docs), ea7bd2a (test moves), RED fbbad22 | [inferred] |
| F12 | Unmappable tile (NaN/Infinity/out-of-grid) misrouted to soil-at-minimum refusal | T075 | 97336e8 (route to `RefusedInvalidTarget`), RED fbbad22, tests ea7bd2a | [inferred] |
| F13 | Silent fallback when `ShowAgrochemicalFeedback` is null (committed success, zero confirmation) | T076 | 8b56cb3 (Warn naming the context; SC-001/FR-007/FR-013 remain unpromoted) | [inferred] |
| F14 | `multiplayer-gate3.md` host row contradicted shipped lock-release ordering | T077 | 18dd04e (contract re-aligned: measured commit → release lock → consume → confirmation → broadcast) | [inferred] |
| F15 | Silent `_agrochemicalApplicationService == null` branch (composition failure) | T078 | 5ef7569 (Warn + fail-closed refusal via `ShowAgrochemicalRefusal(AgrochemicalActionConflict)`, returns false); RED pin e79100b | [inferred] |
| F16 | Zero-stack agrochemical left a ghost inventory slot | T079 | 6124518 (slot cleared by reference at stack 0; best-effort, Trace diagnostics); live-session observation outstanding (Phase 10 T096 records this) | [inferred] |
| F17 | `ILivingRootsModApi.ApplyAgrochemical` doc overclaimed host-only binding | T080 | e323894 (doc-only alignment: binding unconditional, host authority enforced downstream by controller; not a privilege boundary; dev/CI-only label kept) | [inferred] |
| F18 | pt magnitude parity gap: no "10" in `applied`; `applied-clamped` comparison unbounded (FR-012/FR-014) | T081 | c3f7297 (pt rewrite), RED cc6ee1a; Phase 10: re-reviewed (T093) | [committed] (i18n trio) |
| F19 | Fallback-branch controller test missing (FR-007/FR-008) | T082 | e79100b (characterization seams incl. null-service refusal+log pin, repeated-application ordering) | [committed] (test files) |
| F20 | Stale RED-phase header remarks contradict green state | T083 | fbbad22, 20f380b, 16ed2ad, e79100b (headers refreshed to green-state/contract wording); Phase 10: remaining stale comments assigned T102 | [inferred] |
| F21 | Degenerate FsCheck property (`UnknownBaseline_AlwaysUsesExactlyMaxSoilHealth` discarded arbitrary) | T084 | fbbad22 (dropped the parameter; property yields signal) | [committed] (test files) |
| F22 | Chained two-application controller test missing (repeated-applications edge) | T085 | e79100b (stack 4→3→2 characterization seam) | [committed] (test files) |
| F23 | `IAgrochemicalApplicationService.Apply` precondition undocumented (callers could bypass host authority/lock) | T086 | 4e5b199 (xmldoc: authoritative lock-held host precondition; farmhand refusal precedes the call; NaN sentinel semantics on the result record) | [inferred]; theme matches cbacc34 message ("seam precondition") |
| F24 | Registration wiring unpinned (exactly one Basic item, qualified/unqualified ID pair, no context tags) | T087 | f8d0061 (`AgrochemicalRegistrationTests` — 3 cases pin the `Data/Objects` contract) | [committed] (test files) |
| F25 | `LocalizeOrFallback` not mirrored for 017 feedback keys (raw i18n key could surface) | T088 | 6384151 (`TranslateAgrochemicalMessage`: English literal + Warn naming only the key) | [inferred] |
| F26 | No append-only end-to-end re-verification after convergence | T089 | 5addbdf (Phase 9 closure: `convergence-rerun.md` + claim-records addendum; no claims promoted); Phase 9 re-audit then found V-01..V-15 → Phase 10 | [inferred]; theme matches cbacc34 message ("append-only verification closure") |

**Coverage:** 26 findings ↔ 26 tasks (T064–T089). All Phase 9 tasks are marked `[x]` in
`tasks.md`; their artifacts are committed. F-level wording is reconstructed, not quoted — the
authoritative finding texts lived in the (uncommitted) converge session output.

## V-01..V-15 audit appendix — Phase 10 re-audit dispositions

The Phase 10 converge pass re-audited the Phase 9 closure. Only **V-01, V-02, and V-06** are
named explicitly in committed text (`tasks.md` T103: "items V-06/V-01/V-02 were opened and closed
here" — i.e. opened by the re-audit and closed in Phase 10, not in Phase 9). V-01 and V-02 have
documented content; V-06's specific content is not present in committed evidence. All other
V-IDs below are **[inferred]** assignments from the Phase 10 task list (ordered HIGH-first) and
task descriptions; their numbering has no committed anchor and must not be cited as such.

| V-ID | Phase-10 finding (as recoverable) | Disposition | Anchor |
|---|---|---|---|
| V-01 | Offline Gate 3 bundle-check stdout cited as `/tmp/opencode/bundle-check.log`, which was 0 bytes; exit-0 attestation rested on the session, not retained output | T091 (re-run offline bundle check capturing full stdout into a committed artifact; corrective note) + T092(a) (acknowledgement appended to `honesty-review.md`; bundle itself verified by parent session 2026-11-06: manifest.json 16/16 digest spot-audit OK, claims.json honestyLevel `R`, verdict `pass`) | [committed] (tasks.md T091/T092; honesty-review.md addendum) |
| V-02 | Attestation integrity: "92/92" targeted figure vs source-enumerated 88; no committed raw log for the re-run | T091 (targeted filter re-run; raw retained log committed under `verification/` or `verification/logs/`; append-only corrective note in `convergence-rerun.md` reconciling 92 vs 88; prior text not rewritten) + T092(b,c) (counts re-stated; rows 48–50/53 annotated as Phase-7/8-tree scope) | [committed] (tasks.md T091/T092; claim-records.md addendum) |
| V-03 | Host-only Gate 3 never re-run on the post-Phase-9 tree; machine-pass claim still e5882c1-scoped | T090 (re-run via 016-hardened runner; new bundle; promote only via runner; new append-only claim row; D1/human/SC-004 stay pending) — status per `tasks.md` at time of writing | [inferred] |
| V-04 | `quickstart.md` stale: four test classes, "60 cases", no AgrochemicalRegistrationTests | T094 (refreshed 2026-11-06: five classes, 16/4/20/45/3 = 88, pointer to convergence-rerun.md; historical RED gate marked historical; commit e62ba4d) | [inferred] |
| V-05 | `manual-review.md` did not cover the rewritten copy; claim-records row 52 scope note unanchored | T093 (post-Phase-9 re-review addendum; row 52 re-anchored without edits; commit 71b7e54) | [inferred] |
| V-06 | Re-audit finding whose content is not recoverable from committed evidence | Opened and closed in Phase 10 per `tasks.md` T103; disposition consistent with the T101/T102 hygiene trio, but that assignment is **[inferred]** and unanchored — the converge session log (not committed) is the only source of the finding text | [committed] only for "opened and closed in Phase 10"; content [inferred/missing] |
| V-07 | Claim-register validator too weak: first-token path check only, no 0-byte log rejection, no `planned-run-not-executed ∧ complete` prohibition | T095 (validator hardened; current register passes honestly: 16 tokens, 1 digest-checked; commit 4d0da8f) | [inferred] |
| V-08 | Zero-stack inventory slot-clear path untested headless | T096 (headless unit test for `ConsumeHeldAgrochemical`; live-session observation for T079 remains outstanding) | [inferred] |
| V-09 | `TrySetSoilHealth` false result mapped to `ActionConflict` (wrong cause class; commit-drop refused as contention) | T097 (introduce `RefusedCommitFailed` or record the accepted mismatch) | [inferred] |
| V-10 | Applied magnitude "10" in copy not tied to `ModConstants.AgrochemicalSoilPenalty` | T098 (format at display time or mandatory sync comment at `Constants.cs:223`; pin test) | [inferred] |
| V-11 | Location-axis validation gap: overlong/invalid location names could route to `RefusedSoilAtMinimum` instead of `RefusedInvalidTarget` | T099 (reject invalid location names before baseline computation) | [inferred] |
| V-12 | No real-seam capacity coverage; `postPenalty` sub-1e-4 storage semantics unpinned | T100 (real `SoilHealthService.TrySetSoilHealth` capacity test; record/decide storage semantics) | [inferred] |
| V-13 | Contract/doc hygiene: `TrySetSoilHealth` unnamed in contracts; ISoilHealthService xmldoc "zero round-trip" overclaim; ulp tolerance on applied-delta undocumented | T101 (this phase: (c) contract amendments appended 2026-11-06, commit 54659fc; (a) xmldoc and (b) ulp tolerance assigned to T101 in `tasks.md`) | [inferred]; (c) part [committed] via this phase's contracts |
| V-14 | Stale test comments: `AgrochemicalApplicationServiceTests.cs:19-22` ("SetSoilHealth commits through TrySetSoilHealth"), `ModControllerAgrochemicalTests.cs:29-31`/`:243-250` ("one RED test") | T102 (comment refresh + `Monitor.Verify(LogLevel.Warn)` pin) | [inferred] |
| V-15 | Low-risk polish: `missing-input` copy lacks diagnosis; `tasks.md` task-count line stale | T104 (optional bundle) | [inferred] |

**Reading the table:** V-01/V-02 (and V-06 by named reference) are the only findings with
committed evidence trails; both were opened by the post-Phase-9 re-audit and are being closed in
Phase 10 (V-01/V-02 via T091/T092, V-06 per the T103 note). V-03..V-15 dispositions point at
T090–T104; their completion state is tracked in `tasks.md`, not here. This file itself (T103)
discharges the F-level traceability gap the re-audit found; it changes no claim, status, or
artifact.

## What this record does not do

- Does not rewrite `tasks.md`, `claim-records.md`, or any Phase 7/8/9 artifact.
- Does not promote any claim; D1, human `verified`, SC-004 remain `pending`; acquisition and
  measured-zero persistence remain `blocked`.
- Does not re-verify the implementation; see `verification/convergence-rerun.md` and, once run,
  the T090/T091 records.
