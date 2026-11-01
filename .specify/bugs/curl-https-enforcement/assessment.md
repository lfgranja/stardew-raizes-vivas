# Bug Assessment: SonarCloud S6506 — curl does not enforce HTTPS in gitleaks install

- **Slug**: curl-https-enforcement
- **Created**: 2026-11-01T04:12:22Z
- **Source**: https://sonarcloud.io/project/issues?id=lfgranja_stardew-raizes-vivas&s=IMPACT_RANK&severities=BLOCKER%2CCRITICAL%2CMAJOR%2CMINOR&sinceLeakPeriod=true&issueStatuses=OPEN%2CCONFIRMED&types=VULNERABILITY&open=AaGOebPpQ4iSPc4Vg4Rw&tab=code
- **Source host**: sonarcloud.io
- **URL fetch policy**: confirmed-by-user (host not on allowlist; user approved fetch) — page is a JS-rendered SPA; issue details retrieved via the SonarCloud public issues API (`/api/issues/search?issues=AaGOebPpQ4iSPc4Vg4Rw`).
- **Verdict**: valid
- **Severity**: medium

## Report

SonarCloud issue `AaGOebPpQ4iSPc4Vg4Rw`, rule `githubactions:S6506`, type `VULNERABILITY`, SonarCloud severity `MAJOR`, impact `SECURITY / MEDIUM`. Component `.github/workflows/quality-gate.yml`, line 59. Message: "Not enforcing HTTPS here might allow for redirections to insecure websites. Make sure it is safe here." Created 2026-10-30, status `OPEN`. The offending line downloads the gitleaks release tarball with `curl -sSfL "https://github.com/gitleaks/..."` and already carries a `# NOSONAR` trailing comment that did not clear the finding. The URL itself is HTTPS, but curl is not instructed to refuse protocol downgrades on redirects.

## Symptom

In the `static` job of `.github/workflows/quality-gate.yml`, gitleaks is installed by downloading a release tarball from GitHub over HTTPS using `curl -sSfL`. Because curl is not told to enforce HTTPS, a redirect (or a compromised/misconfigured endpoint) could downgrade the transfer to plaintext HTTP, exposing the downloaded binary to tampering before it is installed into `/usr/local/bin`. SonarCloud's `githubactions:S6506` therefore flags the line as a security vulnerability.

## Reproduction

1. Open `.github/workflows/quality-gate.yml` at line 59.
2. Observe `curl -sSfL "https://github.com/gitleaks/gitleaks/releases/download/..." -o /tmp/gitleaks.tgz # NOSONAR`.
3. Run SonarCloud analysis on the repository (or the `quality-gate` workflow) — the `githubactions:S6506` vulnerability is reported on line 59.
4. The `# NOSONAR` suppression on the same line does not clear the finding (the issue remains `OPEN` in SonarCloud).

## Suspected Code Paths

- `.github/workflows/quality-gate.yml:59` — the only `curl` invocation in the repository; downloads the gitleaks binary and is the exact line SonarCloud flags.
- `.github/workflows/quality-gate.yml:55-62` — surrounding "Install Gitleaks" step that owns the download and install.

## Root Cause Hypothesis

The curl command uses an HTTPS URL but does not restrict the protocols curl may use, so curl's default redirect-following behavior could accept a redirect to a non-HTTPS (plaintext) endpoint. SonarCloud rule `githubactions:S6506` ("Allowing downgrades to a clear-text protocol is security-sensitive") requires explicit protocol enforcement via `--proto '=https'`. The existing `# NOSONAR` comment did not suppress the finding (likely because this VULNERABILITY is not cleared by inline suppression, or was raised before the comment was added), so the finding persists as `OPEN`. Confidence: high.

## Proposed Remediation

**Preferred**: Add `--proto '=https'` to the curl invocation so curl refuses any redirect that downgrades away from HTTPS, and drop the now-unnecessary `# NOSONAR` comment since the underlying condition is being fixed rather than suppressed:

```
curl --proto '=https' -sSfL "https://github.com/gitleaks/gitleaks/releases/download/${version}/gitleaks_${version#v}_linux_x64.tar.gz" -o /tmp/gitleaks.tgz
```

This matches the fix pattern proven to clear SonarCloud S6506 in other projects (e.g. `curl --proto '=https' -fsSL ...`).

**Alternatives**:
- Also add `--proto-redir '=https'` to additionally constrain redirect targets; redundant in practice with `--proto '=https'` for this trusted, pinned URL, and not required by the rule.
- Mark the hotspot as "safe" in the SonarCloud UI; not code-reviewable and leaves the finding technically open in code, so the code-level fix is preferred.

**Files likely to change**:
- `.github/workflows/quality-gate.yml`

**Tests to add or update**:
- No automated tests cover workflow YAML; verify by re-running SonarCloud analysis (or the `Quality Gate / static` job) and confirming issue `AaGOebPpQ4iSPc4Vg4Rw` is resolved/closed. Optionally add a lint check that workflow `curl` invocations include `--proto '=https'`.

## Risks & Considerations

- `--proto '=https'` is supported by the curl version shipped on `ubuntu-latest` runners; no compatibility risk.
- Removing `# NOSONAR` is safe once the root cause is addressed; if other SonarCloud findings on the same line surface later, re-evaluate rather than re-suppressing.
- Does not change the downloaded artifact or gitleaks version; supply-chain behavior is unchanged aside from refusing plaintext redirects.
- Severity is judged `medium` rather than `high` because the URL is a pinned, trusted GitHub release endpoint and the exposure is limited to the CI runner, but the finding is a genuine security hardening gap.

## Open Questions

- [NEEDS CLARIFICATION: should the project also pin the gitleaks download via checksum verification, given it is a security tool installed into `/usr/local/bin`?]
- [NEEDS CLARIFICATION: are there other curl/wget invocations in non-workflow scripts (e.g. `scripts/`) that SonarCloud has not yet scanned?]