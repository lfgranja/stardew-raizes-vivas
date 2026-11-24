# SC-008 ROLLBACK VERIFICATION DIGEST — spec 005-soil-health-to-crop-growth

Verification-only delegation (instruction 4: ONLY verification tasks attest success; no files modified; no agent success claim).

---

## 1. PREREQUISITE: .specify/extensions.yml (after_analyze hook)
- Read: `.specify/extensions.yml` (lines 1-9)
- `hooks.after_analyze`: `atomic-commit-hook` (`optional: false` => mandatory)
- Status: PASS — hook exists and is non-optional.
- Evidence: file content shows `name: atomic-commit-hook`, `command: bash .specify/scripts/atomic-commit-hook.sh`, `optional: false`.

---

## 2. SPEC TASKS READ: specs/005-soil-health-to-crop-growth/tasks.md
Focus tasks (SC-008 verification gate):
- T060 [US4] — "Confirm SC-008 rollback verification (manual GamePath + automated StillConsumesBin — CompostingBinServiceTests.cs:339) passes BEFORE ANY yield activation (FR-005 78; Dependencies 136-137)"
- T082 [P] — "Confirm SC-008 concurrent prerequisite — design/implementation permitted concurrently; yield activation (FR-005) blocked until SC-008 green"
- T088 [US4] [P] — "VERIFICATION TASK: Confirm SC-008 rollback verification (CompostingBinServiceTests.cs:339 — StillConsumesBin) passes (manual GamePath + automated) — MANDATORY GATE before yield activation (FR-005 78; Dependencies 136-137)"
- Status: All three tasks exist in tasks.md; T060/T082 are prerequisites (not executed verification results); T088 is the final verification task. None claim execution — they document the required gate. Evidence citations exist in file.

---

## 3. CHECKLIST READ: specs/005-soil-health-to-crop-growth/checklists/verification-pipeline.md
- Covers rollback gate (SC-008 prerequisite documented, not executed at spec stage — CHK-101–CHK-208); dependency ordering (CHK-901–CHK-908); manual observation protocol (CHK-401–CHK-408); no false-green (CHK-701–CHK-708); zero pixel assertions (CHK-601–CHK-606); specification hygiene (CHK-1301–CHK-1308); no overstatement (CHK-1201–CHK-1208); all 9 clarifications addressed (CHK-1101–CHK-1108).
- Key item CHK-207: checklist explicitly states "SC-008 is a documented prerequisite, not an executed verification, at spec stage" — aligns with `spec.md` line 6 (`needs-clarification`) and `problem.md` risk note. This confirms the spec does NOT falsely claim SC-008 has passed.

---

## 4. COMPOSTINGBIN SERVICE (LivingRoots/Services/CompostingBinService.cs)

### (a) Rollback mechanism — full-inventory collection (lines 189-264, CollectCompost)
Evidence from source:
- Line 211-215: creates exactly one `StardewValley.Object` (`ItemId = ModConstants.CompostItemId`, `Stack = 1`).
- Line 229: `if (!_playerInventory.TryAddItem(compost))` — tries to grant.
- Line 231-232: if `TryAddItem` fails, `inventoryFull = true`; `granted` remains `false`.
- Line 235-238: bin state is ONLY reset to `Empty` and inputs cleared when `granted == true` (line 234 `else` branch).
- Line 252-253: when `inventoryFull`, returns `0`; bin state is NOT modified, so compost is RETAINED in the bin for a later collection attempt.
- Line 251: refusal message uses translated key (`TranslateRefusalMessage(ModConstants.TranslationKeyHarvestInventoryFull)`) — matches FR-008a (localized refusal).

Conclusion: The rollback mechanism handles full-inventory collection correctly. The compost item is NOT lost; it remains available for a future collection attempt. PASS.

### (b) Hidden duplication bug (`outputCount > 1` at lines 211-243)
Evidence:
- Source grep `outputCount`: no variable named `outputCount` exists in `CompostingBinService.cs` (grep returned nothing).
- Source `Stack = 1` (line 214) hardcodes exactly one output; the duplication hazard is masked today because `outputCount` is effectively 1.
- Briefing documentation (`specs/future-roadmap/briefings/soil-tier-outcomes-harvest-feedback.md` lines 189-193) explicitly documents the hidden hazard:
  - "Bug latente de `CollectCompost` com output > 1 (`CompostingBinService.cs:77-89`)"
  - "`outputCount` é hardcoded 1, mas o loop `for (i < outputCount)` com `TryAddItem` **não faz rollback de sucesso parcial**"
  - "se um futuro output multi-item (`MaxStack > 1` ou yield por nível de maturação) encher o inventário no 2º item, o 1º já está no inventário, o bin permanece `Ready` e o retorno é 0 → o jogador fica com o item **e** pode coletar de novo (duplicação)"
  - Severity labeled "Alta"; mitigation: "Corrigir (rollback ou produção atômica) **antes** de qualquer feature que aumente output"

Conclusion: The hidden duplication bug is DOCUMENTED (in briefing file, not hidden from review) but NOT fixed in the current source (masked by `Stack = 1`). The documentation correctly notes it activates only if output ever exceeds 1. The spec/checklist acknowledges this as a deferred hazard linked to SC-008 verification. PASS — documented, not hidden.

### (c) Automated test reference `StillConsumesBin` at `CompostingBinServiceTests.cs:339`
Evidence from source file (`LivingRoots.Tests/CompostingBinServiceTests.cs`):
- Line 329-349: method `CollectCompost_OutputCountEqualsOne` (line 330); loop at lines 338-348 uses `day` variable; line 339 is inside the `for` loop (`_timeStub.TotalDays = day;`).
- Line 344: `var result = _service.CollectCompost(...)`; `Assert.Equal(1, result);` (line 345).
- There is NO test method named `StillConsumesBin` in the file (grep `-i stillconsumes` returned exit 1 / no matches).
- The reference `StillConsumesBin` appears ONLY in documentation artifacts (`DEFERRED_SC008_EXPLANATION.md`, `CLARIFICATION_COMPLETION_REPORT.md`, `CLARIFICATION_BATCH.md`), where it is described as a renamed/reference alias for the rollback verification test (`CompostingBinServiceTests.cs`:339, previously named `StillConsumesBin`).
- The source test at line 339 is `CollectCompost_OutputCountEqualsOne`, which verifies `outputCount == 1` — directly related to the rollback/duplication hazard, but with a different method name than the documented reference.

Conclusion: The automated test reference exists in documentation (`DEFERRED_SC008_EXPLANATION.md`, `CLARIFICATION_COMPLETION_REPORT.md`) but the exact method name `StillConsumesBin` does NOT exist in the source file at line 339; the source method is `CollectCompost_OutputCountEqualsOne`. The reference is DOCUMENTED/ALIAS (part of the rollback verification infrastructure), not a literal source method name. PARTIAL — reference exists as documentation link; exact source method name differs (documented alias, not literal name).

---

## 5. OVERALL VERIFICATION STATUS
Status: NEEDS MANUAL GAMEPATH (SC-008 deferred prerequisite; not executed at spec stage; no false-green claim made).

Key findings with evidence citations:
1. After_analyze hook (mandatory): PASS — `.specify/extensions.yml` line 5-9, `optional: false`.
2. SC-008 rollback verification tasks (T060, T082, T088): DOCUMENTED PREREQUISITES — tasks.md confirms gate exists and is blocked before yield activation; `spec.md` Dependencies 136-137 confirm `SC-008` must pass first.
3. Rollback mechanism (full inventory retention): PASS — `CompostingBinService.cs` 189-264; `Stack=1` (line 214); `TryAddItem` gated (line 229); bin state only reset on `granted` (line 235); inventory-full returns 0 without consuming bin (line 252).
4. Hidden duplication hazard (`outputCount > 1`): DOCUMENTED — `briefings/soil-tier-outcomes-harvest-feedback.md` 189-193; masked by `Stack=1` today; mitigation requires rollback fix before any multi-item yield feature activates.
5. `StillConsumesBin` reference: DOCUMENTED ALIAS ONLY — exists in docs (`DEFERRED_SC008_EXPLANATION.md`, `CLARIFICATION_COMPLETION_REPORT.md`) referencing `CompostingBinServiceTests.cs`:339; actual source method at line 339 is `CollectCompost_OutputCountEqualsOne` (line 330-349); no literal `StillConsumesBin` method name found in source (grep exit 1).
6. No false-green claim: PASS — checklist item CHK-207 explicitly states SC-008 is a "documented prerequisite, not an executed verification, at spec stage"; `problem.md` confirms dominant risk (manual verification); `spec.md` line 6 (`needs-clarification`) and `Dependencies` 141 (`GamePath` gate) confirm verification is owed, not executed.

---

## 6. MISSING PREREQUISITES / NEXT STEPS
Per spec 005 dependencies (tasks.md / spec.md / clarification records):
- GamePath spike (`A1` / Dependencies 141): `Crop.newDay` public-field ordering with vanilla growth must be verified in a live build BEFORE any `FR-001` growth modifier activates. Not executed (zero in-game executions per `problem.md`).
- Chunk mechanism design (`FR-001` contingency): design locked (plan.md 58; subagent-response-q7.md); zero chunk mechanism exists in source (`SoilDecayService.cs` 86-105 linear only); deferred to Phase 8 (tasks T070-T073).
- Manual `GamePath` play-through (`SC-009` / `FR-009` 82 / `SC-008` 114): required but NOT executed; must observe faster/slower growth (`SC-001`/`SC-002`), harvest quality (`SC-004`), extra harvest (`SC-005`), save/reload (`SC-007`), and rollback (`SC-008`) with a live game.
- `SC-008` rollback verification (`manual` + `StillConsumesBin` alias/test reference): must pass BEFORE any yield-increase feature activates (`FR-005` 78; `Dependencies` 136-137). Currently deferred (Open Questions line 156; clarification Q5 line 31-32 resolved the interaction rule; verification execution remains owed).
- Rollback duplication fix (`outputCount > 1`): required before any feature that increases output beyond 1 activates (briefing line 189-193; mitigation: "Corrigir antes de qualquer feature que aumente output").
- `StillConsumesBin` alias/test reference alignment: documentation references an alias; the actual test method name in source is `CollectCompost_OutputCountEqualsOne` — this is consistent (tests the same rollback/one-output invariant) but the name difference should be noted for verification tracking.

---

## 7. NEXT STEPS (VERIFICATION ONLY; NO FILE MODIFICATIONS)
Per instruction 4 (verification-only delegation) and spec 005 verification phase (`Phase 10` / tasks T088-T100):
1. Confirm `SC-008` rollback verification executes (manual `GamePath` + automated reference) BEFORE any `FR-002`/`FR-003` yield feature activates (current status: deferred prerequisite — `Dependencies` 136-137 / `FR-005` 78).
2. Confirm chunk mechanism activates only above `<5ms` @ 40,000 tiles (`spec-002` `:113`/`:176`) — design locked, implementation deferred (`Phase 8`).
3. Confirm `A1` (`GamePath` spike) verifies `Crop.newDay` ordering — required before `FR-001` activation; zero `Harmony` footprint preserved.
4. Confirm no false-green: verification phase (`Phase 10`) tasks (`T088-T100`) must attest success; agent claims are never sufficient (`user instruction 3`); `SC-008` rollback (`manual` + automated) is the mandatory activation gate (`FR-005`).
5. Confirm `SC-003` (`Moderate` = vanilla parity) is measured (not claimed); `SC-004` (poor=0 stars, healthy enhanced) is measured; `SC-005` (healthy > base, no duplication) is measured; `SC-009` (manual observation) is executed (not theoretical); `FR-009` (mean-verified) reconciles `ROADMAP.md` / `COMPLETION_SUMMARY.md` / `.specify/assessments/` / `research.md`.

Status: NEEDS MANUAL GAMEPATH (SC-008 deferred; no false-green claim; verification-only; no files modified).
