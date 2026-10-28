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
| Gate 3 verdict ownership (`machine-pass` → `verified`) | `lfgranja` | Runner (machine `machine-pass`); maintainer (honesty-level E attestation) | Contributors | Repo owner | 2026-10-27 | 2027-01-27 |

When a second maintainer joins, split A/R by domain and record the change here in a reviewed PR.

**Gate 3 verdict ownership.** The runner is the sole emitter of the machine `machine-pass` mark, and only for a clean, non-flaky `pass` with three agreeing signals on a host run. `verified` is a human claim: it requires the maintainer's honesty-level E attestation over that machine bundle, and the runner never writes it on its own. Farmhand / split-screen (`debug split`) runs are never `machine-pass` — they exercise the host-authority guard only (transport not tested) and are capped at honesty levels W/R regardless of how clean the signal set is. A maintainer may always demote; neither mark is self-certified by the producing run alone.

## Review Cadence

Tiered, risk-based (C-SEC-27; C-REL-20). Annual-only review is explicitly rejected as an anti-pattern.

1. **Trigger-based (immediate)** — on any change to token handling, cache, extraction, permissions/redaction, a new external integration or dependency, a security incident or near-miss, new threat intelligence, or a change to this annex.
2. **Quarterly lightweight** — checklist currency (CHK001–CHK059 across the five requirements-quality checklists: requirements, security, concurrency, reliability, scenario-contract), token rotation verified ≤ 30 days, audit-log spot-check, SLSA level regression, OWASP/SLSA mapping refresh. Recorded in the review record below.
3. **Annual full** — STRIDE threat-model refresh of the runner and supply chain, security-posture evaluation, roadmap refresh, ownership reassignment, SLSA reassessment.
4. **Per-PR / per-release automated** — existing CI formatting and (when active) build/test checks; schema validation; bundle verification.

## Security Review Record

| Date | Type | Scope | Outcome | Reviewer |
|------|------|-------|---------|----------|
| 2026-10-27 | Initial baseline | Spec 010 security requirements CHK001–CHK053; ownership assigned; tiered cadence adopted; threat mapping added | Accepted; no open critical findings | `lfgranja` |
| 2026-10-27 | Threat-mapping correction and token/aggregation controls | Corrected CICD-SEC references; token-in-argv prohibition; token-event source split; 24-hour aggregation; local/CI control matrix | Accepted; no open critical findings | `lfgranja` |
| 2026-10-27 | Checklist-currency correction | Five requirements-quality checklists covering CHK001–CHK059 (requirements, security, concurrency, reliability, scenario-contract); supersedes the earlier `CHK001–CHK053` currency statement. No requirement text changed | Accepted; currency range corrected; ownership unchanged | `lfgranja` |

Future reviews append a row; rows are never rewritten.

## Reliability Review Record

| Date | Type | Scope | Outcome | Reviewer |
|------|------|-------|---------|----------|
| 2026-10-27 | Initial baseline | Flaky detection, retry budget, quarantine lifecycle, save preservation, concurrency isolation, evidence completeness | Accepted; quarantine candidate vs confirmed-flaky separated; auto-disable is the only automatic terminal action; deletion requires human approval | `lfgranja` |
| 2026-10-27 | Guidance crosswalk | Google 2016, Microsoft 2020, Meta PFS 2020, RAFT 2024, Luo et al. 2014; deviations documented | Accepted with documented deviations | `lfgranja` |

Future reliability reviews append a row; rows are never rewritten. A reliability review is triggered by a change to retry/quarantine logic, a save-preservation incident, a concurrency failure, or a new flaky-status threshold.

### Reliability Guidance Crosswalk

Dated 2026-10-27. Each row maps an external source's principle to the governing requirement and records compliance or a deliberate deviation.

| Source | Principle | Requirement(s) | Compliance / deviation |
|--------|-----------|----------------|------------------------|
| Google Testing Blog, “Flaky Tests at Google and How We Mitigate Them” (2016) | Re-run failures to distinguish flakiness; move persistently flaky tests out of the blocking CI path | FR-011; C-REL-10/C-REL-14/C-REL-21 | Compliant: bounded retries, 2-run candidate, human approval, 28-day cap, auto-disable. Deviation: no automatic deletion; deletion requires human approval. |
| Microsoft Research, “A Study on the Lifecycle of Flaky Tests” (2020) | Asynchronous calls are a leading cause; “fixed” claims require empirical verification; track the lifecycle | C-REL-26/C-REL-27; FR-011 | Compliant: derived flaky status, expiry, shadow-pass evidence. Deviation: no automated root-cause classifier in Phase 1. |
| Meta Engineering, “Probabilistic flakiness: How do you test your tests?” (2020) | Probabilistic flakiness score (failure in a good state); use it to set reliability goals | C-REL-07/C-REL-08/C-REL-26 | Compliant: N ≥ 30 Wilson 95% CI plus pass-rate tiers. Deviation: PFS is a monitoring signal, not a quarantine trigger. |
| RAFT, “The Effects of Computational Resources on Flaky Tests” (ICST-CCIW 2024; arXiv:2310.12132) | Resource-affected flaky tests; Pearson χ² at p < 0.05 with Benjamini–Hochberg correction; vary resource configurations | C-REL-09/C-REL-26 | Compliant: resource-varied detection is required; χ² + BH are the documented method. Deviation: Phase 1 may use a reduced config matrix until the runner is stable. |
| Luo et al., “An empirical analysis of flaky tests” (FSE 2014) | Flakiness is common and driven by async, order, and resource factors | C-REL-09/C-REL-26 | Compliant: retry classification plus order/resource detection. |

The crosswalk is reviewed with the reliability review record; a new external source is added only when it changes a requirement or documents a deviation.

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

### Audit Event Sources and Aggregation

| Event class | Source | Retention | Linkage |
|-------------|--------|-----------|---------|
| Runner-emitted events (access, download, integrity, override, quarantine, promotion) | `audit.jsonl` written by each run | ≥ 365 days in the durable store | `runId`, `timestamp`, `actor`, `action`, `resource`, `outcome` |
| GitHub-emitted events (token creation/rotation/revocation, permission changes, release/asset access) | GitHub organization/repository audit log, exported to the durable store | GitHub plan retention, then the durable store's ≥ 365 days | `actor` + `timestamp` + repository; `runId` recorded when the action is run-scoped |
| Aggregation results | Scheduled `security-aggregation` workflow | ≥ 365 days | query window, query hash, issue URL |

**Token lifecycle events are not written by the runner.** The runner cannot observe GitHub-side token creation, rotation, or revocation; those events come from the GitHub audit log. The runner's `audit.jsonl` records only token *use* (acquisition attempts, 401/403 outcomes, cache reuse) with a `runId`. The annex's retention table applies to both sources once exported.

**24-hour auth-failure aggregation.** A scheduled workflow (`security-aggregation.yml`, at least daily and after each verification run) queries the durable audit store for the trailing 24-hour window (`windowStart`/`windowEnd` recorded in the query result). Three or more authentication failures (`401`/`403`) in that window open or update a GitHub issue assigned to the security owner, with links to the offending `runId`s. The job records the query window, query hash, result count, and issue URL as a runner-emitted audit event. If the durable store is unavailable, the job opens an infrastructure issue and does not silently treat the window as clean. This is the Phase 1 mechanism; SIEM ingestion remains deferred.

### Local/CI Control Matrix

| Control | Local | CI | Enforcement |
|---------|-------|----|-------------|
| Non-root startup guard (`EUID != 0`) | Required | Required | Code (`verify-scenario.sh` startup guard) |
| `BUILDDEPS_TOKEN` required | Only when acquisition is required | Only when acquisition is required | Code (FR-001/FR-002) |
| Cache isolation (`cache-mode`) | N/A — local filesystem cache is single-process | Per-job (`read`/`write`/`write-only`/`none`) | Platform + code re-verification on reuse |
| `GITHUB_TOKEN` read-only | N/A | Required (`permissions: contents: read`) | Workflow |
| High/low runner groups | N/A | Required; no job spans both | Infrastructure + governance |
| Durable evidence export | Recommended | Required | Code + governance |
| JUnit XML + Allure JSON | Optional | Required | `--ci` |
| Non-interactive execution | No (interactive allowed) | Yes | `--ci` |
| Secret redaction and audit logging | Required | Required | Code (shared logging facade) |

Local-only deviations are limited to the controls marked N/A; all other controls apply in both modes. A local run that cannot satisfy a CI-only control records the deviation in `manifest.json#/environment` and is not eligible for `verified` promotion unless the deviation is reviewed and accepted.

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

### Durable Store, Records, and Audit Operations

| Obligation | Realization | Owner | Cadence |
|------------|-------------|-------|---------|
| Durable evidence store (append-only/WORM) | Evidence bundles and `audit.jsonl` exported to append-only/WORM storage outside GitHub Actions artifacts (90-day artifacts are not the durable store); write-once retention ≥ 365 days | `lfgranja` | Written on every run; immutability policy and retention configuration re-verified at each quarterly lightweight review |
| Promotion-trigger record | `specs/010-headless-verification-runner/promotion-triggers.json` — machine-readable mirror of the Deferred Items and Promotion Triggers table; this annex remains normative and the JSON file is the record consumed by the runner/CI | `lfgranja` | Updated in the same PR as any trigger change and at every promotion decision; read at the quarterly lightweight review |
| Quarterly audit-log spot-check | Sample runner-emitted `audit.jsonl` events and the exported GitHub audit log against the retention and source tables above; record gaps as issues | `lfgranja` | Quarterly |
| Quarterly deletion drill | Exercise the 365-day expiry/deletion path on a synthetic bundle in the WORM store, including the documented-approval step; confirm deletions are refused without approval | `lfgranja` | Quarterly |
| Annual restore drill | Restore a sampled bundle from the WORM store and run `scripts/verify-evidence-bundle.sh` against it; confirm the restored bundle still verifies offline | `lfgranja` | Annual |

A failed spot-check, deletion drill, or restore drill opens an issue assigned to the security owner and is recorded in the security review record above; the drill is not "passed" until the issue is closed or an accepted deviation is documented.

## Deferred Items and Promotion Triggers

This is the single complete list referenced by the spec's Out of Scope section. An item moves into scope only when its trigger is met and the change is recorded here with a date and owner.

| Deferred item | Promotion trigger | Owner |
|---------------|-------------------|-------|
| Docker/container delivery | 2+ mods reuse the runner loop (Rule of Three) | `lfgranja` |
| Sigstore/DSSE signing and attestation | Verifier extracted to an independent tool with 2+ mods, or an external trust boundary appears | `lfgranja` |
| External SIEM ingestion | A second runner group or an external trust boundary appears | `lfgranja` |
| Second runtime implementation (non-.NET reference runner) | The .NET runner is stable across ≥2 features and a non-.NET consumer appears | `lfgranja` |
| Scenario parameterization, outline/matrix, composite keyword/step library | ≥3 features share a parameterized scenario shape | `lfgranja` |
| GitHub App / OIDC token phases | Runner moves to shared/self-hosted CI or a second maintainer is added | `lfgranja` |
| Windows/macOS automated execution | Concrete host demand plus a supported headless display path | `lfgranja` |
| Multi-mod packaging/distribution | ≥2 mods consume the runner as a packaged artifact | `lfgranja` |

The machine-readable mirror of this table is `specs/010-headless-verification-runner/promotion-triggers.json`; this annex remains normative if the two disagree, and the JSON file is updated in the same PR as any change here.

## Approval Gates

| Action | Approver | Audit trail |
|--------|----------|-------------|
| Quarantine entry, extension, retirement | Maintainer (`lfgranja`) or fallback repo owner | `quarantine.json` entry + linked issue + audit event |
| Automatic quarantine disable at the 28-day cap | No approval (automatic terminal action) | `quarantine.json` status + linked issue + audit event |
| Deletion of a quarantined scenario or its evidence | Maintainer + repo owner | Audit event + documented reason; never automatic |
| Override of a failing exit code (`--ignore-exit-code`) | Maintainer; only for expected-failure lanes or a documented transient-infrastructure incident | Run log + evidence bundle records the override and listed codes; the verdict is unchanged |
| Environment override (`--override`) | Maintainer; only declared environment keys (`environment.gameVersion`, `environment.smapiVersion`, `environment.modVersion`, `environment.saveId`, `environment.config.*`); never `testMdPath`, `expectedSignals`, `thresholds`, `playerRole`, or `hostGuard` | `manifest.json#/environmentOverrides` + audit event |
| Cache bypass (`--force-refresh`) | Maintainer; only for integrity re-acquisition, cache corruption, or a documented cache-staleness incident | Audit event + acquisition record in the bundle |
| Integrity failure or leaked secret | Never overridable | `integrity-failure.json` / incident record; the run must fail closed |
| Deletion of audit/evidence before 365-day expiry | Maintainer + repo owner | `integrity-failure.json`/audit event + documented reason |
| Update to `gamepath.lock.json` | Reviewed PR (maintainer approval) | Git history |
| Change to the never-quarantine set | Reviewed PR (maintainer approval) | Git history + audit event |
| Machine `machine-pass` emission | No approval — runner-emitted only, and only for a clean, non-flaky `pass` with three agreeing signals on a host run; never for farmhand / split-screen runs | `claims.json` + the three signal artifacts in the bundle |
| Promotion to `verified` | Maintainer E attestation over a machine `machine-pass` whose bundle verifies offline; never automatic; a human may demote | `test.md` GATE3 block + bundle + reviewer/date/session |
| Human attestation at honesty level E | Maintainer (human judgment over the machine bundle; never auto-assigned) | `claims.json` + `test.md` GATE3 block |

**Root-doc GATE3 markers are a promotion precondition.** `ROADMAP.md` and `COMPLETION_SUMMARY.md` MUST each contain exactly one intact `<!-- GATE3:BEGIN -->` … `<!-- GATE3:END -->` block before any promotion, and the markers are maintained by the spec-010 setup task, not created or guessed by the runner at promotion time. A promotion attempt against a missing, duplicated, or inverted marker block is refused before launch (exit `14`), so a bundle can be `machine-pass` while root-doc promotion remains blocked. Only a clean, non-flaky host `pass` writes the block; flaky passes never update root docs.

## Threat-to-Requirement Mapping

| Threat (OWASP CICD-SEC / SLSA) | Requirement(s) |
|--------------------------------|----------------|
| CICD-SEC-1 insufficient flow control | FR-001/FR-002 cache-first + pinned digest; C-SEC-22 cache-trust tuple |
| CICD-SEC-2 inadequate identity management | FR-002 fine-grained PAT; C-SEC-26 ownership |
| CICD-SEC-3 dependency chain abuse | FR-002 private repo + SHA-256; C-SEC-02 |
| CICD-SEC-4 poisoned pipeline execution | Strict scenario schema (FR-007); typed `initCommands`; no untrusted interpolation (C-SEC-04/C-SEC-31) |
| CICD-SEC-5 insufficient PBAC | C-SEC-25 `cache-mode`; least-privilege runner; C-SEC-26 |
| CICD-SEC-6 insufficient credential hygiene | FR-002 token storage/rotation; C-SEC-01/C-SEC-14/C-SEC-21 |
| CICD-SEC-7 insecure system configuration | C-SEC-13/FR-026 runner groups; C-CON-07 permissions |
| CICD-SEC-8 unapproved usage | Audit events (C-SEC-24); override approval gate |
| CICD-SEC-9 improper artifact integrity validation | C-SEC-02/C-SEC-28 fail-closed; `gamepath.lock.json` |
| CICD-SEC-10 insufficient logging and monitoring | C-SEC-24 audit events; NFR-004 JSON logs |
| SLSA Build L2/L3 provenance | C-BND-01 manifest + offline verifier (FR-015); Phase 2 Sigstore deferred |
| Zip-slip / zip-bomb | C-SEC-12/FR-025 archive-member validation |
| Path traversal / homoglyph | C-SEC-04/C-SEC-31 sanitization (Form C) |
| Secret leakage in logs | C-SEC-05/C-SEC-09; NFR-004 |
| Tamper-evident evidence | C-SEC-11/FR-024 `integrity-failure.json`; FR-027 retention |

## Change Control

Changes to this annex require a reviewed PR approved by the accountable owner. The review record above is append-only. If a requirement gains or loses an owner, the R/A/C/I table is updated in the same PR that changes the requirement.
