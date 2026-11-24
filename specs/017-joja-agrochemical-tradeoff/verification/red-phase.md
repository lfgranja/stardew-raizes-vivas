# RED-Phase Evidence — Spec 017 (Joja / Agrochemical MVP)

**Status:** `red` — valid targeted RED gate recorded on 2026-11-06.

All 017 tests were written first against the frozen API; production behavior was still the
compile-safe `NotImplementedException` scaffold when this run was captured. Implementation
(`T026+`) may begin only because this record exists.

## Purpose

Record the RED gate for 017: the targeted test run must fail for the expected missing-behavior
reasons, so implementation starts from a genuine failing baseline.

## RED evidence (T025)

| Field | Value |
|---|---|
| Exact command | `GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release --filter "FullyQualifiedName~Agrochemical\|FullyQualifiedName~AgrochemicalPersistence\|FullyQualifiedName~ModControllerAgrochemical\|FullyQualifiedName~AgrochemicalContentParity" --logger "console;verbosity=normal"` |
| Date | 2026-11-06 |
| Filter used | `FullyQualifiedName~Agrochemical\|FullyQualifiedName~AgrochemicalPersistence\|FullyQualifiedName~ModControllerAgrochemical\|FullyQualifiedName~AgrochemicalContentParity` |
| Total tests | 60 |
| Passed | 0 |
| Failed | 60 |
| Skipped | 0 |
| Failing test names | All 60 listed below, grouped by test class. |
| Failure-reason category (per failure) | 35 × expected `System.NotImplementedException` from the RED scaffolds (13 service, 19 controller, 3 persistence); 25 × expected missing-017-key assertion failures (content parity). No compile/build breakage, no unrelated failures. |
| Raw output/log path | `specs/017-joja-agrochemical-tradeoff/verification/red-phase-run.log` (relative to repo root) |
| Observed gate status | `red` |

Run breakdown by class:

| Class | Cases | Expected failure category |
|---|---:|---|
| `AgrochemicalApplicationServiceTests` | 13 | 10 × `NotImplementedException`; 3 × FsCheck `PropertyFailedException` wrapping the same missing behavior |
| `ModControllerAgrochemicalTests` | 19 | 15 × `NotImplementedException` (14 orchestration scaffold, 1 unwired-service guard); 3 × `Assert.Null()` (scaffold throws before the no-escape assertion); 1 × FsCheck `PropertyFailedException` wrapping the same missing behavior |
| `AgrochemicalPersistenceTests` | 3 | 3 × `NotImplementedException` |
| `AgrochemicalContentParityTests` | 25 | 25 × missing 017 i18n key assertions (`017 i18n parity: ...`) |

The shell wrapper printed `zsh:1: read-only variable: status` after the test process because the
helper script used `status` as a local variable name. That wrapper error occurred after output was
redirected and does not affect the captured log or the counts above. The test process itself exited
non-zero, as expected for a RED run.

### Failing test names

#### `AgrochemicalApplicationServiceTests` (13)

1. `MeasuredBaseline_AppliesExplicitPenalty(baseline: 50, expectedPostPenalty: 40)`
2. `MeasuredBaseline_AppliesExplicitPenalty(baseline: 100, expectedPostPenalty: 90)`
3. `UnknownBaseline_ResolvesToMaxHealth_AndWritesMeasuredNinety`
4. `MeasuredZero_RefusesAtSoilMinimum_WithoutWrites`
5. `MeasuredFive_ClampsPenaltyAndReportsAppliedDelta`
6. `InvalidTarget_BlankLocationName_RefusesWithoutSoilAccess(locationName: "")`
7. `InvalidTarget_BlankLocationName_RefusesWithoutSoilAccess(locationName: "   ")`
8. `InvalidTarget_NullLocation_RefusesWithoutSoilAccess`
9. `C1_ServiceBoundary_OnlyDependsOnSoilHealth_AndNeverUsesUpdateHealth`
10. `MeasuredBaseline_ProducesBoundedHonestPenalty` (FsCheck)
11. `UnknownBaseline_AlwaysUsesExactlyMaxSoilHealth` (FsCheck)
12. `SubPenaltyBaseline_AppliesOnlyAvailableDelta` (FsCheck)
13. `RepeatedApplication_AppliesSameBoundedRule_WithoutHiddenState`

#### `AgrochemicalPersistenceTests` (3)

1. `UnknownBaseline_Apply_SaveReload_PersistsMeasuredNinety`
2. `MeasuredBaseline_Apply_SaveReload_PersistsMeasuredForty`
3. `SubPenaltyBaseline_Apply_Save_OmitsZeroFromSparseState_AndReloadsUnknown`

#### `ModControllerAgrochemicalTests` (19)

1. `TryApplyAgrochemical_WithNullFeedback_CommitsConsumesAndBroadcasts`
2. `TryApplyAgrochemical_OnUnknownBaselineSuccess_FeedbackUsesUnknownBaselineKey`
3. `TryApplyAgrochemical_WithNoHeldItem_RefusesMissingInputBeforeService`
4. `TryApplyAgrochemical_WithThrowingSuccessFeedback_CommitsConsumesBroadcastsAndDoesNotThrow`
5. `TryApplyAgrochemical_OnServiceConflictRefusal_DoesNotConsumeOrBroadcast`
6. `TryApplyAgrochemical_OnLockContention_RefusesWithoutServiceMutationOrConsumption`
7. `TryApplyAgrochemical_OnHostSuccess_OrdersLockServiceReleaseFeedbackThenBroadcastAndConsumesOnce`
8. `TryApplyAgrochemical_WithWrongHeldItem_RefusesMissingInputBeforeService`
9. `TryApplyAgrochemical_OnSoilAtMinimumRefusal_DoesNotConsumeOrBroadcast`
10. `TryApplyAgrochemical_OnUnsupportedAuthority_RefusesBeforeServiceLockMutationOrConsumption`
11. `TryApplyAgrochemical_OnUntilledFarmTile_RefusesInvalidTargetBeforeService`
12. `TryApplyAgrochemical_WithoutLockService_FailsClosedWithConflictRefusal`
13. `TryApplyAgrochemical_WithoutService_FailsClosedWithoutFeedback`
14. `TryApplyAgrochemical_OnNonFarmLocation_RefusesInvalidTargetBeforeService`
15. `TryApplyAgrochemical_OnClampedSuccess_FeedbackUsesClampedKeyNotFullPenaltyClaim`
16. `TryApplyAgrochemical_OnSoilAtMinimumRefusal_FeedbackUsesSoilAtMinimumKey`
17. `TryApplyAgrochemical_OnClampedSuccess_ConsumesOneAndUsesClampedFeedback`
18. `ForAnyServiceRefusalOutcome_NoConsumptionNoSuccessFeedbackNoBroadcastNoSoilWrite` (FsCheck)
19. `TryApplyAgrochemical_WithThrowingFeedbackOnAuthorityRefusal_DoesNotMutateOrThrow`

#### `AgrochemicalContentParityTests` (25)

Each `[Theory]` case below is one failing case for `default.json`, `en.json`, and `pt.json`, plus
the single cross-locale parity case.

1. `AllAgrochemicalCopy_UsesCozyNonPunitiveTone(localeFileName: "default.json")`
2. `AllAgrochemicalCopy_UsesCozyNonPunitiveTone(localeFileName: "en.json")`
3. `AllAgrochemicalCopy_UsesCozyNonPunitiveTone(localeFileName: "pt.json")`
4. `ClampedSuccessMessage_AcknowledgesBoundsAndAvoidsFullPenaltyClaim(localeFileName: "default.json")`
5. `ClampedSuccessMessage_AcknowledgesBoundsAndAvoidsFullPenaltyClaim(localeFileName: "en.json")`
6. `ClampedSuccessMessage_AcknowledgesBoundsAndAvoidsFullPenaltyClaim(localeFileName: "pt.json")`
7. `ItemDescription_TelegraphsSoilHealthCostAndIndustrialShortcut(localeFileName: "default.json")`
8. `ItemDescription_TelegraphsSoilHealthCostAndIndustrialShortcut(localeFileName: "en.json")`
9. `ItemDescription_TelegraphsSoilHealthCostAndIndustrialShortcut(localeFileName: "pt.json")`
10. `LocaleFiles_ParseAsJsonObjects_AndExposeIdentical017KeySets`
11. `RefusalMessages_AreNonEmptyAndNameTheirRefusalCause(localeFileName: "default.json")`
12. `RefusalMessages_AreNonEmptyAndNameTheirRefusalCause(localeFileName: "en.json")`
13. `RefusalMessages_AreNonEmptyAndNameTheirRefusalCause(localeFileName: "pt.json")`
14. `RequiredAgrochemicalKeys_ExistWithNonEmptyStringValues(localeFileName: "default.json")`
15. `RequiredAgrochemicalKeys_ExistWithNonEmptyStringValues(localeFileName: "en.json")`
16. `RequiredAgrochemicalKeys_ExistWithNonEmptyStringValues(localeFileName: "pt.json")`
17. `SuccessMessages_MakeNoMechanicalBuffClaims_C1ConfirmationOnly(localeFileName: "default.json")`
18. `SuccessMessages_MakeNoMechanicalBuffClaims_C1ConfirmationOnly(localeFileName: "en.json")`
19. `SuccessMessages_MakeNoMechanicalBuffClaims_C1ConfirmationOnly(localeFileName: "pt.json")`
20. `SuccessMessages_StateAnAppliedSoilConsequence(localeFileName: "default.json")`
21. `SuccessMessages_StateAnAppliedSoilConsequence(localeFileName: "en.json")`
22. `SuccessMessages_StateAnAppliedSoilConsequence(localeFileName: "pt.json")`
23. `UnknownBaselineMessage_RequiresNoHistorySignalAndInventsNoPriorReading(localeFileName: "default.json")`
24. `UnknownBaselineMessage_RequiresNoHistorySignalAndInventsNoPriorReading(localeFileName: "en.json")`
25. `UnknownBaselineMessage_RequiresNoHistorySignalAndInventsNoPriorReading(localeFileName: "pt.json")`

Full names with timings and stack traces are in the raw log.

## RED acceptance rules

- A valid RED result means the targeted 017 tests fail because behavior is missing: expected
  missing behavior, `NotImplementedException`, or an expected assertion failure.
- Compile/build breakage and failures unrelated to 017 do **not** count as RED. Fix the
  scaffolding or environment and rerun before recording anything.
- A run that discovers zero 017 tests is not RED. Fix the filter/test names and rerun.
- Record exact counts and failing test names. Summary-only statements are not evidence.
- Do not write implementation code to make the run green before the RED record exists.
- No status other than `pending` applies to a scaffold. T025 sets `red` only after a valid
  targeted failing run.

## Provenance / no retroactive promotion

- Host-only end-to-end evidence is classified separately as `host-only-machine-pass`; it is never
  human `verified` and never D1.
- If a host-only end-to-end run is infeasible, record it as `planned-run-not-executed` with status
  `infeasible` (or `blocked`) plus a reason. Do not fabricate a run, log, or bundle.
- Prior evidence and prior verification artifacts are never rewritten, overwritten, or
  retroactively promoted. New runs produce new records.
- No claim is marked `complete` without a source artifact that supports it at its stated evidence
  class. Lower-level evidence supports only the lower-level claim.

## Infeasible host-only e2e classification

When a real-game host-only end-to-end run cannot be performed, record a
`planned-run-not-executed` claim with:

- **Status:** `infeasible` (environment cannot support the run) or `blocked` (missing prerequisite).
- **Reason:** the concrete limitation (for example, no game assemblies, no display/Xvfb, runner
  unavailable, scenario not implemented).
- **Attempted command:** the exact command or script invocation tried.
- **Environment limitation:** what was missing and why it prevents the run.
- **Date and recorder:** when and by whom the infeasibility was determined.

A `planned-run-not-executed` record is not a pass. Automated tests, mocked-seam persistence tests,
and the RED run only support their own lower-level claims; none of them substitutes for
`host-only-machine-pass`, `human-verified`, or `d1-farmhand-convergence`.

## Claim classes

| Claim class | May support | May not support |
|---|---|---|
| Automated unit/property (`automated`) | Service/controller contract and invariant behavior under mocks. | Real game behavior, disk persistence, D1, human attestation, SC-004. |
| Mocked persistence seam (`automated`, integration-style) | Save/load seam round-trip through `ISoilHealthService` with a mocked `IModDataService`. | Real disk save/load, Gate 3, real-game persistence. |
| Host-only machine-pass (`host-only-machine-pass`) | Host-only end-to-end run with the required signals. | Human `verified`, D1 farmhand convergence, SC-004 comprehension. |
| D1 farmhand (`d1-farmhand-convergence`) | Real farmhand shared-farm convergence. | Human `verified`; host-only claims. Requires actual farmhand evidence. |
| Human verified (`human-verified`) | Recorded human review/playtest attestation. | Machine-pass, D1, SC-004 comprehension unless separately recorded. |
| SC-004 qualitative (`qualitative-follow-up`) | Comprehension result for the recorded sample/context only. | Automated evidence, Gate 3, human `verified`, D1. Protocol alone is not a run. |

## T025 completion checklist

- [x] Exact command, date, filter, and counts recorded.
- [x] Every failing test named and categorized.
- [x] Raw output/log path recorded and reachable.
- [x] No compile/build breakage or unrelated failures remain in the record.
- [x] Observed gate status changed from `pending` to `red`.
- [x] This scaffold's placeholder text removed; real evidence substituted.
