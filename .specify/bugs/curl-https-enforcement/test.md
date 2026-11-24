# Bug Verification: SonarCloud S6506 — curl does not enforce HTTPS in gitleaks install

- **Slug**: curl-https-enforcement
- **Tested**: 2026-11-01T04:35:00Z
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial

## Summary

The code-level condition behind SonarCloud rule `githubactions:S6506` is fixed: the only `curl` invocation in the repository now carries `--proto '=https'` and the ineffective `# NOSONAR` was removed, and the workflow YAML still parses. However, the original reproduction mechanism (SonarCloud reporting issue `AaGOebPpQ4iSPc4Vg4Rw`) was not actually exercised, because no SonarCloud scanner/credentials are available in this environment, so closure of the finding is inferred from the rule's documented remediation rather than observed.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) — offending condition | `grep -rn "curl" .github/workflows/` | pass | Single match now contains `--proto '=https'`; no curl without protocol enforcement remains |
| Reproduction (post-fix) — SonarCloud analysis | (not available) | skipped | No `sonar-scanner`/credentials locally; cannot re-run SonarCloud to observe issue closure |
| New / updated tests | (none added) | not-run | No automated test harness covers workflow YAML in this repo (assessment marked optional) |
| Regression suite — .NET format | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | pass | Exit 0; no formatting regressions (change is YAML-only) |
| Lint / type-check — YAML parse | `python3 -c "import yaml; yaml.safe_load(open('.github/workflows/quality-gate.yml'))"` | pass | `YAML OK` |

## Output Excerpts

```
$ grep -rn "curl" .github/workflows/
.github/workflows/quality-gate.yml:59:          curl --proto '=https' -sSfL "https://github.com/gitleaks/gitleaks/releases/download/${version}/gitleaks_${version#v}_linux_x64.tar.gz" -o /tmp/gitleaks.tgz

$ python3 -c "import yaml; yaml.safe_load(open('.github/workflows/quality-gate.yml'))"
YAML OK

$ dotnet format Stardew-LivingRoots.sln --verify-no-changes
... avisos ao carregar o workspace ...
EXIT: 0
```

## Residual Risks

- SonarCloud issue `AaGOebPpQ4iSPc4Vg4Rw` has not been re-analyzed; closure is expected based on the documented `--proto '=https'` remediation but is not yet observed.
- No automated guard prevents future `curl`/`wget` invocations in workflows from omitting `--proto '=https'` (assessment optional item).
- The open `[NEEDS CLARIFICATION]` items in the assessment (checksum pinning for the gitleaks download; scanning non-workflow scripts) remain unaddressed.

## Recommendation

Hold — the code-level fix is in place and locally sound, but the SonarCloud reproduction was not exercised in this environment. Re-run SonarCloud analysis on `dev`/PR to confirm issue `AaGOebPpQ4iSPc4Vg4Rw` resolves to `CLOSED`; once observed, upgrade the result to `verified`. If SonarCloud still reports the finding, re-run `/speckit.bug.assess` with the new evidence.