# Bug Assessment: SonarCloud S2699 — test method with no assertion (SoilHealthServiceContractTests)

- **Slug**: contract-test-missing-assertion
- **Created**: 2026-11-01T04:41:18Z
- **Source**: https://sonarcloud.io/project/issues?id=lfgranja_stardew-raizes-vivas&s=IMPACT_RANK&sinceLeakPeriod=true&issueStatuses=OPEN%2CCONFIRMED (issue key `AaFPP3NvEHNTGUcquNLc`)
- **Source host**: sonarcloud.io
- **URL fetch policy**: confirmed-by-user (host not on allowlist; user approved fetch) — page is a JS-rendered SPA; issue details retrieved via the SonarCloud public issues API (`/api/issues/search?issues=AaFPP3NvEHNTGUcquNLc`).
- **Verdict**: valid
- **Severity**: low

## Report

SonarCloud issue `AaFPP3NvEHNTGUcquNLc`, rule `csharpsquid:S2699`, type `CODE_SMELL`, SonarCloud severity `BLOCKER`, impact `MAINTAINABILITY / BLOCKER`. Component `LivingRoots.Tests/ContractTests/SoilHealthServiceContractTests.cs`, line 160. Message: "Add at least one assertion to this test case." The offending test is `OnTileHealthChanged_DoesNotFireWhenNoSubscriber` (lines 159-167), which calls `SetSoilHealth` and `UpdateHealth` with no subscriber registered but performs no assertion — it only relies on the calls not throwing. Created 2026-10-18, status `OPEN`.

## Symptom

The test `OnTileHealthChanged_DoesNotFireWhenNoSubscriber` exercises `SetSoilHealth` and `UpdateHealth` on a `SoilHealthService` with no `OnTileHealthChanged` subscriber, intending to verify that mutation does not throw in that case. Because the method body contains no `Assert.*` call, SonarCloud rule `csharpsquid:S2699` ("Add at least one assertion to this test case") flags it as a BLOCKER code smell, weakening the test's ability to detect regressions (a throwing or no-op implementation would still pass).

## Reproduction

1. Open `LivingRoots.Tests/ContractTests/SoilHealthServiceContractTests.cs` at lines 159-167.
2. Observe the test body calls `service.SetSoilHealth(...)` and `service.UpdateHealth(...)` with no assertion.
3. Run SonarCloud analysis — issue `AaFPP3NvEHNTGUcquNLc` is reported on line 160.

## Suspected Code Paths

- `LivingRoots.Tests/ContractTests/SoilHealthServiceContractTests.cs:159-167` — the test method `OnTileHealthChanged_DoesNotFireWhenNoSubscriber` with no assertion.
- `LivingRoots/Services/SoilHealthService.cs` (`SetSoilHealth`, `UpdateHealth`) — the methods under test; they invoke `OnTileHealthChanged?.Invoke(...)` only when a subscriber exists, so the no-subscriber path is the one being exercised.

## Root Cause Hypothesis

The test was written to assert "no throw when no subscriber" but relied on the absence of an exception rather than an explicit assertion. SonarCloud's S2699 rule requires at least one assertion in every test method, so the method is flagged. The fix is to add an assertion that captures the test's intent (the calls complete and mutate state without throwing). Confidence: high.

## Proposed Remediation

**Preferred**: Add an assertion verifying the resulting health value after the no-subscriber mutations, which proves the calls ran to completion and had their intended effect:

```csharp
Assert.Equal(60f, service.GetSoilHealth("Farm", tile), 5);
```

This keeps the test's original intent (no throw on the no-subscriber path) while satisfying S2699 and strengthening the assertion.

**Alternatives**:
- Wrap the calls in `Assert.DoesNotThrow` (xUnit has no such helper; would require a try/catch + `Assert.True(true)`, which is hacky and still risks S2699).
- Split into two tests (one for "no throw", one for state). Over-engineering for a single no-subscriber smoke check.

**Files likely to change**:
- `LivingRoots.Tests/ContractTests/SoilHealthServiceContractTests.cs`

**Tests to add or update**:
- Update `OnTileHealthChanged_DoesNotFireWhenNoSubscriber` to include an assertion on the resulting health value.

## Risks & Considerations

- The added assertion assumes `SetSoilHealth(50)` then `UpdateHealth(+10)` yields `60`, consistent with `UpdateHealth_PositiveDelta_IncreasesValue` (50 → 60). If `UpdateHealth` with no subscriber behaved differently, the test would now catch it — which is desirable.
- Severity is judged `low` because this is a test-only code smell (no production behavior change), despite SonarCloud rating it BLOCKER for maintainability.
- No production code changes; regression risk is limited to the test itself.

## Open Questions

- [NEEDS CLARIFICATION: should the no-subscriber path also assert that `OnTileHealthChanged` is never invoked (e.g., via a counter)? The current test only checks "no throw"; the existing `OnTileHealthChanged_FiresOnMutation` covers the firing case.]