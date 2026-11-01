# Bug Assessment: SonarCloud S3776 — CompostingBinService.LoadData complexity

- **Slug**: composting-load-complexity
- **Created**: 2026-11-01T06:00:00Z
- **Source**: https://sonarcloud.io/project/issues?id=lfgranja_stardew-raizes-vivas&branch=dev (AaFoMEsoMxJk7g2GlOHq)
- **Verdict**: valid
- **Severity**: medium

## Symptom
LoadData has cognitive complexity 32 (>15) due to nested parsing, validation, world-state checks, and error handling.

## Proposed Remediation
Extract helpers: TryParseTileKey/ParseTileKey already exist; extract TryGetLocation, TryCreateBinStateFromRecord, ShouldQuarantineReadyRecord, etc., and flatten loops. Also extract TryLoadBinRecord.

**Files likely to change:** LivingRoots/Services/CompostingBinService.cs

## Risks
Behavior must remain identical (T056/T060/T079 rules). Must preserve locking/snapshot patterns.
