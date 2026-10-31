# Bug Verification: Quality Gate CI failures (run 41197325085)

- **Slug**: quality-gate-ci-failures
- **Tested**: 2026-10-31
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial

## Summary

The Semgrep front is now resolved locally: `semgrep scan --config p/csharp --error --severity ERROR --metrics off LivingRoots/` reports 0 findings (6 rules, 82 files, exit 0). The CodeQL SARIF conflict, Trivy CVE suppressions, and Coverlet exclusion filters were already in place from the prior partial fix. The original CI symptom (run 41197325085) cannot be fully re-verified locally because CodeQL, Trivy, and the coverage gate require CI infrastructure (GitHub Actions runner, Trivy binary, CodeQL action). Additionally, the full regression suite cannot run locally because the test project has 92 pre-existing compile errors in `SoilHealthBoundsInvariantsTests.cs` (unrelated to this bug). Result is `partial`, not `verified`.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Semgrep (static) | `semgrep scan --config p/csharp --error --severity ERROR --metrics off LivingRoots/` | pass | 0 findings, 6 rules, 82 files, exit 0 |
| Workflow YAML parse | `python3 -c "yaml.safe_load(...)"` | pass | 5 jobs: static, sca, codeql, build-analysis, gate |
| CodeQL SARIF conflict | Code review (fix.md diff) | pass | Redundant `upload-sarif` removed; `category: "csharp"` added to analyze |
| Trivy CVEs | `.trivyignore` review | pass (config) | Justified suppressions for CVE-2017-11770 and CVE-2018-8292 with owner + expiry (2026-11-30) |
| Coverlet exclusions | `coverlet.runsettings` review | pass (config) | ExcludeByFile + Exclude filters for infrastructure/utility classes and mulch DTOs |
| Trivy binary scan | `trivy` | skipped | Trivy not installed locally |
| CodeQL analysis | `github/codeql-action` | skipped | Requires GitHub Actions runner |
| Coverage gate | `dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings` | not-run | Test project has 92 pre-existing compile errors in `SoilHealthBoundsInvariantsTests.cs` (unrelated to this bug) |
| Full regression suite | `dotnet test` | not-run | Same pre-existing compile errors block the suite |

## Output Excerpts

```
Semgrep scan: ✅ Scan completed successfully.
 • Findings: 0 (0 blocking)
 • Rules run: 6
 • Targets scanned: 82
 • Parsed lines: ~97.8%
Ran 6 rules on 82 files: 0 findings.

YAML: OK: ['static', 'sca', 'codeql', 'build-analysis', 'gate']

Build error (pre-existing, unrelated):
  SoilHealthBoundsInvariantsTests.cs(350,17): error CS0246: WeightAndValue<> not found
  92 errors total in LivingRoots.Tests
```

## Residual Risks

- **CodeQL, Trivy, and coverage gate** were not actually executed — they require CI infrastructure (GitHub Actions runner, Trivy binary, CodeQL action). The fixes are in place but must be confirmed by a CI run.
- **Full regression suite** could not run due to 92 pre-existing compile errors in `SoilHealthBoundsInvariantsTests.cs` (FsCheck `WeightAndValue<>` type resolution). This is unrelated to the quality-gate bug but blocks local verification of the coverage gate.
- **`.trivyignore` suppressions** expire 2026-11-30 — must be re-audited at the quarterly audit.
- The Semgrep finding from CI run 41197325085 no longer reproduces locally; if it was a transient match or the triggering code changed since, CI should confirm.

## Recommendation

Hold for CI confirmation — the Semgrep front is now resolved locally (0 findings), and the other 3 fronts (CodeQL SARIF removal, Trivy suppressions, Coverlet exclusions) are in place with documented justification. A CI run is needed to confirm all 4 fronts are green end-to-end. The pre-existing compile errors in `SoilHealthBoundsInvariantsTests.cs` should be investigated separately (likely a using directive or FsCheck version issue) as they block local regression testing. Do not mark as fully verified until CI confirms.