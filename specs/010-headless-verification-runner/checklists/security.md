# Security & Supply-Chain Requirements Quality Checklist: Headless Game Verification Runner

**Purpose**: Validate that security and supply-chain requirements for game-file acquisition, token handling, artifact integrity, cache trust, path sanitization, secret logging, evidence integrity, least privilege, fail-closed behavior, and audit logging are complete, clear, measurable, consistent, and covered.
**Created**: 2026-10-26
**Feature**: [spec.md](../spec.md)

**Review Ownership**: This checklist is a reviewer-owned requirements-quality review artifact. Mark an item `[x]` only when the reviewer determines the requirements-quality criterion is satisfied.

**Note**: Items test whether the *requirements themselves* are well-written — not whether the implementation works.

---

## Authentication & Authorization (Token Scoping, Expiry, Storage)

- [x] CHK001 - Are token scoping requirements defined with the exact minimal permission set (read/write/admin) for the fine-grained PAT (`BUILDDEPS_TOKEN`)? [Completeness, Spec §FR-002]
- [x] CHK002 - Is the token expiry requirement specified with a maximum lifetime and rotation cadence? [Clarity, Spec §FR-002]
- [x] CHK003 - Are token storage requirements specified, enumerating allowed locations (secret store) and prohibiting plaintext in code/config/cache? [Completeness, Spec §FR-002]
- [x] CHK004 - Is the token validation requirement defined (signature, issuer, audience, expiry, revocation) before any download attempt? [Completeness, Spec §FR-002]
- [x] CHK005 - Are token-in-logs requirements specified, prohibiting token appearance in console output, error logs, or command history? [Coverage, Gap]
- [x] CHK006 - Is OIDC/short-lived token preference documented as a production upgrade path with per-job ephemeral tokens? [Clarity, Spec §FR-002]
- [x] CHK007 - Are token sharing restrictions across pipelines/contexts explicitly prohibited? [Consistency, Gap]

## Artifact Integrity (Checksums, Signatures, Provenance)

- [x] CHK008 - Is the checksum algorithm requirement specified (SHA-256) with weak algorithms prohibited? [Clarity, Spec §FR-002]
- [x] CHK009 - Is checksum verification required before extraction AND re-verified after extraction? [Completeness, Spec §FR-002]
- [x] CHK010 - Is the checksum stored in a trusted source (repo config), not co-located with the artifact? [Security, Spec §FR-002]
- [x] CHK011 - Are immutable-release and signature-verification requirements (Sigstore/cosign) specified or explicitly deferred? [Gap, Spec §FR-002]
- [x] CHK012 - Is the "treat mismatch as security event" requirement measurable with a defined response (quarantine, fail, log)? [Measurability, Spec §FR-002]
- [x] CHK013 - Are extraction-integrity requirements (archive structure, member count, size limits, zip-slip/zip-bomb) defined? [Coverage, Gap]
- [x] CHK014 - Is there a tamper-evidence requirement producing an immutable record on integrity failure? [Gap]

## Cache Trust Boundaries

- [x] CHK015 - Is the cache-trust boundary defined — which writers are trusted and untrusted writes prohibited? [Completeness, Spec §FR-001]
- [x] CHK016 - Are cache read/write scope requirements specified per job (read/write/write-only/none)? [Clarity, Gap]
- [x] CHK017 - Is cache restoration treated as untrusted input requiring re-verification (not trusted on `cache-hit`)? [Measurability, Spec §FR-001]
- [x] CHK018 - Are cache-key construction requirements defined (content hash, not branch name alone)? [Completeness, Spec §FR-001]
- [x] CHK019 - Is storing secrets/tokens in cached paths explicitly prohibited? [Security, Gap]
- [x] CHK020 - Are cache isolation by branch/tag and no cross-branch leakage requirements specified? [Consistency, Gap]

## Path Traversal & Input Sanitization

- [x] CHK021 - Are input validation requirements specified for all external inputs (file names, archive members, URLs, identifiers)? [Completeness, Spec §FR-002]
- [x] CHK022 - Are path-traversal requirements defined (reject `..`, absolute paths, drive letters, symlink escapes)? [Coverage, Gap]
- [x] CHK023 - Is Unicode normalization (Form C) and homoglyph detection required for all filesystem paths? [Clarity, Spec §FR-002 (implied via ModDataService)]
- [x] CHK024 - Are archive member validation requirements (structure, count, size, decompression ratio) specified? [Gap]
- [x] CHK025 - Are error-message disclosure requirements defined (no internal paths or secrets in errors)? [Security, Gap]
- [x] CHK026 - Are environment-variable/context-injection requirements specified (untrusted inputs via intermediate env vars, not interpolated into scripts)? [Security, Gap]

## Secret Handling in Logs

- [x] CHK027 - Are secret redaction/masking requirements specified for all log output (success and failure paths)? [Completeness, Gap]
- [x] CHK028 - Is plaintext secret logging prohibited, including generated/encoded variants (Base64, URL-encoded)? [Security, Gap]
- [x] CHK029 - Are log retention/deletion requirements defined for logs containing leaked secrets? [Coverage, Gap]
- [x] CHK030 - Are audit vs. application log requirements distinguished (access/permission changes vs. push/build/upload)? [Clarity, Gap]

## Evidence-Bundle Integrity

- [x] CHK031 - Are evidence collection requirements specified (commands, env, materials, artifacts) as mandatory per run? [Completeness, Spec §FR-014]
- [x] CHK032 - Is evidence signing/attestation required (cryptographic signature, secure keys)? [Gap, Spec §FR-014]
- [x] CHK033 - Are evidence completeness requirements defined (minimum required evidence set enumerated)? [Measurability, Spec §FR-014]
- [x] CHK034 - Is evidence verification by an independent party specified (offline verifier)? [Clarity, Spec §FR-015]
- [x] CHK035 - Are evidence retention and immutability guarantees defined? [Coverage, Gap]

## Least Privilege

- [x] CHK036 - Are permission minimization requirements defined (minimal scopes for PAT, runner, mod)? [Completeness, Spec §FR-002]
- [x] CHK037 - Is deny-by-default posture specified for pipeline execution (non-root, non-admin)? [Security, Gap]
- [x] CHK038 - Are runner-group segregation requirements defined (high-privilege vs. low-privilege runners)? [Gap]
- [x] CHK039 - Is the default GITHUB_TOKEN permission set to read-only? [Clarity, Gap]

## Fail-Closed Behavior

- [x] CHK040 - Is fail-closed on integrity failure required (no fallback to unverified on checksum/signature mismatch)? [Measurability, Spec §FR-002]
- [x] CHK041 - Is fail-closed on missing evidence required (missing report/attestation fails the gate)? [Completeness, Spec §FR-014]
- [x] CHK042 - Is fail-closed on unknown inputs required (unrecognized fields/parameters cause rejection)? [Consistency, Spec §FR-007 (strict schema)]
- [x] CHK043 - Is fail-closed on partial downloads/extractions required (no partial trust)? [Completeness, Spec §FR-002]
- [x] CHK044 - Are override procedures gated by documented approval and audit trail? [Gap]

## Audit Logging

- [x] CHK045 - Are audit event types enumerated (access, permission changes, token creation, builds, uploads, downloads)? [Completeness, Gap]
- [x] CHK046 - Is a parsable log format (JSON/syslog) mandated with required fields (actor, action, resource, timestamp, outcome)? [Clarity, Spec §NFR-004]
- [x] CHK047 - Are sensitive data excluded from logs (secrets, tokens, passwords)? [Security, Spec §NFR-004]
- [x] CHK048 - Is centralized aggregation and alerting for anomalies required? [Coverage, Gap]
- [x] CHK049 - Are log retention periods and immutability guarantees defined? [Gap]

## Cross-Cutting Consistency & Coverage

- [x] CHK050 - Are security requirements consistent across local and CI invocation modes? [Consistency, Spec §FR-023]
- [x] CHK051 - Are security requirements mapped to specific threats (OWASP/SLSA threat model)? [Traceability, Gap]
- [x] CHK052 - Is an owner assigned for each security requirement domain? [Governance, Gap]
- [x] CHK053 - Are security requirements reviewed periodically against new threats? [Coverage, Gap]

## Notes

- Audit 2026-10-27: 52 PASS / 1 UNCLEAR (CHK025). CHK025 re-check PASS after adding the caller-supplied-path exception to C-SEC-04 and rewording FR-001/Edge Cases.

- Items marked `[Gap]` indicate requirements-quality gaps where the spec does not currently define the criterion.
- Traceability references point to spec sections; `[Gap]` items lack a corresponding spec requirement.
- Based on OWASP Top 10 CI/CD, OWASP CI/CD & GitHub Actions Security Cheat Sheets, NIST SP 800-204D, SLSA v1.2, in-toto attestation spec, GitHub secure-use reference.
