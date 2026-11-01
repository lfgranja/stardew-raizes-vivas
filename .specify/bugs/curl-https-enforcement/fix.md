# Bug Fix: SonarCloud S6506 — curl does not enforce HTTPS in gitleaks install

- **Slug**: curl-https-enforcement
- **Fixed**: 2026-11-01T04:25:00Z
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

Added `--proto '=https'` to the single `curl` invocation in `.github/workflows/quality-gate.yml` (the gitleaks install step) so curl refuses any redirect that downgrades away from HTTPS, and removed the now-unnecessary `# NOSONAR` trailing comment. The downloaded artifact and gitleaks version are unchanged.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `.github/workflows/quality-gate.yml` | modified | Line 59: `curl --proto '=https' -sSfL ...` instead of `curl -sSfL ... # NOSONAR` |

## Diff Highlights

```diff
-          curl -sSfL "https://github.com/gitleaks/gitleaks/releases/download/${version}/gitleaks_${version#v}_linux_x64.tar.gz" -o /tmp/gitleaks.tgz # NOSONAR
+          curl --proto '=https' -sSfL "https://github.com/gitleaks/gitleaks/releases/download/${version}/gitleaks_${version#v}_linux_x64.tar.gz" -o /tmp/gitleaks.tgz
```

## Tests Added or Updated

- None — no automated test harness covers workflow YAML in this repository. Verification is via SonarCloud re-analysis (see Local Verification).

## Local Verification

- Commands run: `python3 -c "import yaml; yaml.safe_load(open('.github/workflows/quality-gate.yml'))"` → `YAML OK` (workflow still parses).
- Commands run: `grep -rn "curl" .github/workflows/` → single match, now contains `--proto '=https'`.
- Manual checks: confirmed only one `curl` invocation exists in the repository and it now enforces HTTPS; the `# NOSONAR` suppression was removed since the underlying condition is fixed.

## Deviations from Assessment

None — the applied change matches the assessment's preferred remediation exactly (added `--proto '=https'`, removed `# NOSONAR`).

## Follow-ups

- Re-run SonarCloud analysis on `dev`/PR to confirm issue `AaGOebPpQ4iSPc4Vg4Rw` resolves to `CLOSED`.
- Consider adding a lightweight lint/check that workflow `curl`/`wget` invocations include `--proto '=https'` (assessment optional item).
- Revisit the open `[NEEDS CLARIFICATION]` items in the assessment (checksum pinning for the gitleaks download; scanning non-workflow scripts) in a separate triage pass.