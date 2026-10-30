# SonarCloud Removal Checklist (SC-007, deferred)

**Rollout start date**: (record on first green `Quality Gate` run)
**Earliest removal date**: rollout start + 2–4 weeks of green runs

## Green-run criteria (FR-010)
Every blocking job (`static`, `sca`, `build-analysis`, `gate`) concluded `success` on a pull request or push to `dev`/`main`, with no skipped blocking job and no accepted-risk exception.

## Parity checklist
- [ ] Reliability: Roslyn analyzers (SonarAnalyzer.CSharp + NetAnalyzers) with TreatWarningsAsErrors — 0 warnings/errors on clean build.
- [ ] Security: Semgrep (fast per-PR) + CodeQL (scheduled deep scan) + Trivy (dependency scanning).
- [ ] Maintainability: Sonar rules via SonarAnalyzer.CSharp (code smells) — note: Semgrep CE `p/csharp` runs 6 rules, not SonarCloud's full set.
- [ ] Duplication: jscpd C#-only threshold (0.42% ceiling).
- [ ] Coverage: Coverlet + ReportGenerator 5.5.12 (79.5% lines / 70.7% branches).

## Accepted parity gaps (explicit)
- Semgrep CE `p/csharp` currently runs 6 rules and is the fast per-PR gate; CodeQL `build-mode: none` is the scheduled deep scan.
- Neither provides SonarCloud's new-code hotspots or maintainability rating.
- Semgrep/Trivy scan the whole scope rather than new code only.

## Removal steps (follow-up, NOT in this feature)
1. Disable the SonarCloud GitHub app/check.
2. Remove `.vscode` SonarLint connected mode.
3. Update docs.

**SonarCloud is NOT removed by this feature (011).**