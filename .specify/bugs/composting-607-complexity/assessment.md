# Bug Assessment: SonarCloud S3776 — CompostingBinService.CollectCompost complexity (line ~607)

- **Slug**: composting-607-complexity
- **Created**: 2026-11-01T06:15:00Z
- **Source**: https://sonarcloud.io/project/issues?id=lfgranja_stardew-raizes-vivas&branch=dev (AaGHsmfwmuWztWgB8Ba-)
- **Verdict**: valid
- **Severity**: medium

## Symptom
CollectCompost has cognitive complexity 19 (>15).

## Proposed Remediation
Extract TryHarvestCompost / ApplyHarvestSuccess helpers, flatten lock scope and post-lock actions.

**Files likely to change:** LivingRoots/Services/CompostingBinService.cs
