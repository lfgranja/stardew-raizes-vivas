# Analyzer Baseline (Phase 3, T020)

**Command**: `dotnet build LivingRoots/LivingRoots.csproj -c Release`
**Result**: 98 errors (49 reported by MSBuild summary; 98 diagnostic lines), 0 warnings.
**AnalysisLevel**: 6.0-recommended (pinned per Q9); plan D1 assumed `latest` (155 diagnostics). The lower count is expected because the pinned level curates the rule set.

## Counts by rule (production project)

| Rule | Count | Source |
|---|---|---|
| CA1805 | 16 | NetAnalyzers |
| S1066 | 12 | SonarAnalyzer |
| CA1310 | 12 | NetAnalyzers |
| S3267 | 8 | SonarAnalyzer |
| CA1822 | 8 | NetAnalyzers |
| S2325 | 6 | SonarAnalyzer |
| S1144 | 6 | SonarAnalyzer |
| S6444 | 4 | SonarAnalyzer |
| CA1845 | 4 | NetAnalyzers |
| CA1711 | 4 | NetAnalyzers |
| S4487 | 2 | SonarAnalyzer |
| S4144 | 2 | SonarAnalyzer |
| S3400 | 2 | SonarAnalyzer |
| S3358 | 2 | SonarAnalyzer |
| S1172 | 2 | SonarAnalyzer |
| S101 | 2 | SonarAnalyzer |
| CS8602 | 2 | Compiler (nullable) |
| CS0162 | 2 | Compiler (unreachable code) |
| CA1305 | 2 | NetAnalyzers |

## Test project

The test project did not emit diagnostics in this run because the mod project failed to build first; its diagnostics will be captured after the mod project is clean (T022).

## Next step

Remediate or suppress (with justification) each diagnostic per T021/T022. No global `NoWarn`.