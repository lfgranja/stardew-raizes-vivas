# Governance Annex: Headless Game Verification Runner

**Feature**: 010-headless-verification-runner
**Created**: 2026-10-27
**Status**: Active
**Authoritative for**: ownership (R/A/C/I), review cadence, approval gates, threat-to-requirement mapping

This annex is the governance record referenced by C-SEC-19/C-SEC-20/C-SEC-24/C-SEC-26/C-SEC-27, C-REL-20, and FR-027. It is normative where the spec delegates ownership or cadence to this file. It does not restate requirements; it records who owns them, how often they are reviewed, and who may approve exceptions.

## Ownership Model

- **Single accountable owner**: repository maintainer `lfgranja` (GitHub handle) until a designated security/reliability owner is named.
- **Fallback**: repository owner (GitHub org/user with admin rights) if the maintainer is unavailable.
- **Succession**: if the maintainer role changes, the incoming maintainer accepts ownership in a reviewed PR that updates this annex; no requirement is left ownerless.
- **Contributors**: Consulted during PR review; Informed of security advisories and quarantine escalations.
- **CODEOWNERS**: `.github/CODEOWNERS` SHOULD name the maintainer for `specs/010-headless-verification-runner/**`, `scripts/verify-scenario.sh`, `scripts/verify-evidence-bundle.sh`, `LivingRoots.Verification/**`, and `LivingRoots/Domain/Interfaces/ILivingRootsModApi.cs`. CODEOWNERS is an enforcement hook, not a substitute for the accountability below.

## R/A/C/I Table

R = Responsible (does the work), A = Accountable (final decision, exactly one), C = Consulted, I = Informed.

| Domain | A | R | C | I | Last reviewed | Next due |
|--------|---|---|---|---|---------------|----------|
| Game-file acquisition (private repo, Releases assets, retry) | `lfgranja` | `lfgranja` | Contributors | Repo owner | 2026-10-27 | 2027-01-27 |
| Token handling (`BUILDDEPS_TOKEN`, scoping, rotation) | `lfgranja` | `lfgranja` | Contributors | Repo owner | 2026-10-27 | 2027-01-27 |
| Artifact integrity (`gamepath.lock.json`, SHA-256, extraction) | `lfgranja` | `lfgranja` | Contributors | Repo owner | 2026-10-27 | 2027-01-27 |
| Cache trust boundary and `cache-mode` | `lfgranja` | `lfgranja` | Contributors | Repo owner | 2026-10-27 | 2027-01-27 |
| Path sanitization and archive-member validation | `lfgranja` | `lfgranja` | Contributors | Repo owner | 2026-10-27 | 2027-01-27 |
| Secret handling and log redaction | `lfgranja` | `lfgranja` | Contributors | Repo owner | 2026-10-27 | 2027-01-27 |
| Evidence-bundle integrity and offline verifier | `lfgranja` | `lfgranja` | Contributors | Repo owner | 2026-10-27 | 2027-01-27 |
| Least privilege (PAT, runner, GITHUB_TOKEN) | `lfgranja` | `lfgranja` | Contributors | Repo owner | 2026-10-27 | 2027-01-27 |
| Fail-closed behavior and override approval | `lfgranja` | `lfgranja` | Contributors | Repo owner | 2026-10-27 | 2027-01-27 |
| Audit logging and retention | `lfgranja` | `lfgranja` | Contributors | Repo owner | 2026-10-27 | 2027-01-27 |
| Flaky detection, retry, quarantine | `lfgranja` | `lfgranja` | Contributors | Repo owner | 2026-10-27 | 2027-01-27 |
| Concurrency, isolation, save preservation | `lfgranja` | `lfgranja` | Contributors | Repo owner | 2026-10-27 | 2027-01-27 |
| Scenario contract and schema evolution | `lfgranja` | `lfgranja` | Contributors | Repo owner | 2026-10-27 | 2027-01-27 |
| GATE3 promotion and root-doc updates | `lfgranja` | `lfgranja` | Contributors | Repo owner | 2026-10-27 | 2027-01-27 |

When a second maintainer joins, split A/R by domain and record the change here in a reviewed PR.

## Review Cadence

Tiered, risk-based (C-SEC-27; C-REL-20). Annual-only review is explicitly rejected as an anti-pattern.

1. **Trigger-based (immediate)** — on any change to token handling, cache, extraction, permissions/redaction, a new external integration or dependency, a security incident or near-miss, new threat intelligence, or a change to this annex.
2. **Quarterly lightweight** — checklist currency (CHK001–CHK053), token rotation verified ≤ 30 days, audit-log spot-check, SLSA level regression, OWASP/SLSA mapping refresh. Recorded in the review record below.
3. **Annual full** — STRIDE threat-model refresh of the runner and supply chain, security-posture evaluation, roadmap refresh, ownership reassignment, SLSA reassessment.
4. **Per-PR / per-release automated** — existing CI formatting and (when active) build/test checks; schema validation; bundle verification.

## Security Review Record

| Date | Type | Scope | Outcome | Reviewer |
|------|------|-------|---------|----------|
| 2026-10-27 | Initial baseline | Spec 010 security requirements CHK001–CHK053; ownership assigned; tiered cadence adopted; threat mapping added | Accepted; no open critical findings | `lfgranja` |

Future reviews append a row; rows are never rewritten.

## Reliability Review Record

| Date | Type | Scope | Outcome | Reviewer |
|------|------|-------|---------|----------|
| 2026-10-27 | Initial baseline | Flaky detection, retry budget, quarantine lifecycle, save preservation, concurrency isolation, evidence completeness | Accepted; quarantine candidate vs confirmed-flaky separated; auto-disable is the only automatic terminal action; deletion requires human approval | `lfgranja` |

Future reliability reviews append a row; rows are never rewritten. A reliability review is triggered by a change to retry/quarantine logic, a save-preservation incident, a concurrency failure, or a new flaky-status threshold.

## Audit Event and Retention Mapping

| Event class | Examples | Retention | Approval for early deletion |
|-------------|----------|-----------|-----------------------------|
| Access / download | game-asset download, cache reuse, bundle read | ≥ 365 days (audit log) | Maintainer + repo owner |
| Permission / token | token creation, rotation, revocation, scope change | ≥ 365 days (audit log) | Maintainer + repo owner |
| Build / upload | runner build, artifact upload, release publish | ≥ 365 days (audit log) | Maintainer + repo owner |
| Integrity failure | checksum mismatch, archive-member rejection, extraction failure | ≥ 365 days (audit log + evidence bundle) | Never before expiry |
| Override | `--ignore-exit-code`, `--override` | ≥ 365 days (audit log + bundle) | Maintainer + repo owner |
| Quarantine lifecycle | candidate, entry, extension, disable, deletion | ≥ 365 days (audit log + `quarantine.json` history) | Deletion requires human approval |
| Promotion | `verified` update, E attestation | ≥ 365 days (audit log + `test.md` history) | Maintainer + repo owner |
| Application / step log | runner step logs, SMAPI console log | ≥ 30 days | Maintainer |

Logs containing a leaked secret are retained indefinitely with the secret purged and the incident recorded. GitHub Actions artifacts (90-day default) are not the durable store; evidence bundles are exported to durable storage for the 365-day requirement.

## Infrastructure Realization (not enforced by code)

FR-026 and C-SEC-26 separate ownership from infrastructure realization. The following are ops/governance obligations recorded here; the runner's startup guard only enforces the non-root check.

| Item | Realization | Owner |
|------|-------------|-------|
| High-privilege runner group | May hold `BUILDDEPS_TOKEN` and write the game cache; scoped to this repository/org; no cross-repo access | `lfgranja` |
| Low-privilege runner group | Runs untrusted pull-request code; no token, no game-cache write, no shared workspace | `lfgranja` |
| Job separation | A job MUST NOT span both groups; fork PRs default to `cache-mode: none` | `lfgranja` |
| Host hardening | Non-root account, ephemeral hosts where feasible, firewall egress allowlist, patched Xvfb/Mono | `lfgranja` |
| Token phases | Phase 1 fine-grained PAT (`BUILDDEPS_TOKEN`); Phase 2 GitHub App installation token; Phase 3 OIDC federation when the private repo supports it | `lfgranja` |
| Durable evidence export | Append-only/WORM storage outside GitHub Actions artifacts; annual restore drill | `lfgranja` |

## Approval Gates

| Action | Approver | Audit trail |
|--------|----------|-------------|
| Quarantine entry, extension, retirement | Maintainer (`lfgranja`) or fallback repo owner | `quarantine.json` entry + linked issue + audit event |
| Automatic quarantine disable at the 28-day cap | No approval (automatic terminal action) | `quarantine.json` status + linked issue + audit event |
| Deletion of a quarantined scenario or its evidence | Maintainer + repo owner | Audit event + documented reason; never automatic |
| Override of a failing exit code (`--ignore-exit-code`) | Maintainer; only for expected-failure lanes or a documented transient-infrastructure incident | Run log + evidence bundle records the override and listed codes; the verdict is unchanged |
| Deletion of audit/evidence before 365-day expiry | Maintainer + repo owner | `integrity-failure.json`/audit event + documented reason |
| Update to `gamepath.lock.json` | Reviewed PR (maintainer approval) | Git history |
| Change to the never-quarantine set | Reviewed PR (maintainer approval) | Git history + audit event |
| Promotion to `verified` (R honesty level) | Automatic only on a clean, non-flaky `pass` with three agreeing signals; a human may demote | `test.md` GATE3 block + bundle |
| Human attestation at honesty level E | Maintainer (human judgment; never auto-assigned) | `claims.json` + `test.md` GATE3 block |

## Threat-to-Requirement Mapping

| Threat (OWASP CICD-SEC / SLSA) | Requirement(s) |
|--------------------------------|----------------|
| CICD-SEC-1 insufficient flow control | FR-001/FR-002 cache-first + pinned digest; C-SEC-22 cache-trust tuple |
| CICD-SEC-2 inadequate identity management | FR-002 fine-grained PAT; C-SEC-26 ownership |
| CICD-SEC-3 dependency chain abuse | FR-002 private repo + SHA-256; C-SEC-02 |
| CICD-SEC-4 poisoned pipeline execution | Strict scenario schema (FR-007); typed `initCommands`; no untrusted interpolation (C-SEC-21) |
| CICD-SEC-5 insufficient PBAC | C-SEC-25 `cache-mode`; least-privilege runner; C-SEC-26 |
| CICD-SEC-6 insufficient credential hygiene | FR-002 token storage/rotation; C-SEC-05/C-SEC-06 redaction |
| CICD-SEC-7 insecure system configuration | C-CON-07 permissions; C-SEC-18 runner group |
| CICD-SEC-8 unapproved usage | Audit events (C-SEC-24); override approval gate |
| CICD-SEC-9 improper artifact integrity validation | C-SEC-02/C-SEC-28 fail-closed; `gamepath.lock.json` |
| CICD-SEC-10 insufficient logging and monitoring | C-SEC-24 audit events; NFR-004 JSON logs |
| SLSA Build L2/L3 provenance | C-BND-01 manifest + offline verifier (FR-015); Phase 2 Sigstore deferred |
| Zip-slip / zip-bomb | C-SEC-13 archive-member validation |
| Path traversal / homoglyph | C-SEC-11/C-SEC-12 sanitization (Form C) |
| Secret leakage in logs | C-SEC-05/C-SEC-06; NFR-004 |
| Tamper-evident evidence | C-SEC-14 `integrity-failure.json`; FR-027 retention |

## Change Control

Changes to this annex require a reviewed PR approved by the accountable owner. The review record above is append-only. If a requirement gains or loses an owner, the R/A/C/I table is updated in the same PR that changes the requirement.
