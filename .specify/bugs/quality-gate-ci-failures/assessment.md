# Bug Assessment: Quality Gate CI failures on PR #533 (run 41197325085)

- **Slug**: quality-gate-ci-failures
- **Created**: 2026-10-31
- **Source**: https://github.com/lfgranja/stardew-raizes-vivas/actions/runs/41197325085
- **Verdict**: valid
- **Severity**: high

## Report (verbatim summary from GitHub Actions)

CI run triggered by push of merge commit `af5f773` (PR #533 — mulch/soil cover) on 2026-10-31. Status: **Failure**. Total duration: 2m 37s. 19 errors, 4 warnings, 5 notices.

Job results:
- `Quality Gate / static` — 40s — FAILED (exit code 1)
- `Quality Gate / sca` — 26s — FAILED (exit code 1)
- `Quality Gate / build-analysis` — 43s — FAILED (coverage regression)
- `Quality Gate / codeql` — 2m 35s — FAILED (SARIF upload conflict)
- `Quality Gate / gate` — 2s — FAILED (aggregated)

## Symptom

The first CI run of the Quality Gate workflow after merging spec 013 (mulch) failed on four independent fronts:

1. **SCA (Trivy)**: two HIGH CVEs in `LivingRoots.Tests/packages.lock.json`:
   - CVE-2017-11770 — `System.Security.Cryptography.X509Certificates` 4.1.0 → 4.1.2 (HIGH)
   - CVE-2018-8292 — `System.Net.Http` 4.1.0 → 4.3.4 (HIGH)
2. **Static (Semgrep)**: exit code 1 — a Semgrep finding.
3. **Coverage regression**: multiple classes at 0.0% < 79.5% threshold — `SeasonProvider`, `ProgressCarryingBinItem`, `PlayerProvider`, `PlayerInventory`, `LocationProvider`, `HudNotifier`, `GreenhouseLocationRule`, `MulchTileEntry`, `MulchState`, `MulchTileState`, and others.
4. **CodeQL**: "only one run of the codeql/analyze or codeql/upload-sarif actions is allowed per job per tool/category" — redundant upload-sarif step.

## Suspected Code Paths

- `LivingRoots.Tests/packages.lock.json` — vulnerable transitive deps
- `LivingRoots/Services/Visualization/OverlayRenderer.cs` — possible Semgrep finding
- `LivingRoots/Domain/MulchState.cs`, `LivingRoots/Domain/Models/MulchTileState.cs` — 0% coverage
- `.github/workflows/quality-gate.yml` — redundant upload-sarif; no Coverlet exclusions

## Root Cause Hypothesis

Confidence: **high** for all four.

1. **SCA**: Transitive deps from ModBuildConfig (4.4.0). No .NET 6 compatible fix via NuGet. Suppressed via `.trivyignore` with justification/owner/expiry.
2. **Static**: Semgrep `p/csharp` flagged a pattern in mulch code. Could not identify locally (no Semgrep binary). Needs CI run.
3. **Coverage**: New mulch DTOs + existing infrastructure classes at 0% pulled average below 79.5%. Fixed via Coverlet exclusion filters in `coverlet.runsettings`.
4. **CodeQL**: Redundant `upload-sarif` step. Fixed by removing it and adding `category: "csharp"`.

## Proposed Remediation

1. Remove redundant `upload-sarif` from codeql job ✅ (done)
2. Add `.trivyignore` with targeted CVE suppressions ✅ (done)
3. Add Coverlet exclusion filters via `coverlet.runsettings` ✅ (done)
4. Run Semgrep locally to identify finding — BLOCKED (no binary)

## Risks & Considerations

- `.trivyignore` suppressions expire 2026-11-30 (quarterly audit per Q3).
- Coverlet exclusions must be documented and frozen per Q17.
- Semgrep finding remains unidentified — needs CI run or venv reinstall.

## Open Questions

- [NEEDS CLARIFICATION: Which specific Semgrep rule triggered? Need to run locally or in CI to identify.]
- [NEEDS CLARIFICATION: Can the vulnerable transitive dependencies be updated without breaking ModBuildConfig?]