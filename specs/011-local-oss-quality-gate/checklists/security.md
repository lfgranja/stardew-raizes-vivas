# Security Requirements Quality Checklist: Local OSS Quality Gate

**Purpose**: Validate the quality, clarity, completeness, and measurability of the security (SAST/SCA) requirements before implementation.
**Created**: 2026-10-29
**Feature**: [spec.md](../spec.md)

**Note**: This custom checklist is a reviewer-owned requirements-quality review artifact. Mark an item `[x]` only when the reviewer determines the requirements-quality criterion is satisfied. It does NOT mean implementation work is complete.

## Requirement Completeness

- [x] CHK001 Are all security scanning tools (Semgrep, CodeQL, SCA) explicitly named with their roles? [Completeness, Spec §FR-004, FR-005, FR-006] — Spec names Semgrep (fast SAST), CodeQL (deep SAST), Trivy (SCA).
- [x] CHK002 Is the distinction between fast per-PR scan (Semgrep) and deep scheduled scan (CodeQL) explicitly required? [Completeness, Spec §FR-005, Clarifications] — Clarifications + FR-004/FR-005.
- [x] CHK003 Are requirements for dependency-vulnerability scanning (Trivy against the committed NuGet lock files) specified? [Completeness, Spec §FR-006] — FR-006 + contract.
- [ ] CHK004 Are requirements for handling secrets scanning (Gitleaks) included or explicitly excluded? [Completeness, Gap] — reason: no secrets-scanning tool (Gitleaks) is named or explicitly excluded anywhere in spec/plan/contract.
- [x] CHK005 Is the failure threshold for security findings (0 high/critical) quantified? [Completeness, SC-003] — SC-003.
- [ ] CHK006 Are requirements for IaC/container scanning specified or explicitly excluded? [Completeness, Gap] — reason: no IaC/container scanning requirement or explicit exclusion exists.

## Requirement Clarity

- [x] CHK007 Is "security finding" defined with a severity classification (high/critical)? [Clarity, Spec §SC-003] — SC-003 uses HIGH/CRITICAL.
- [x] CHK008 Is the scope of SAST scanning (which files/directories) explicitly defined? [Clarity, Gap] — SC-003: Semgrep whole `LivingRoots/`, CodeQL whole C# codebase.
- [ ] CHK009 Is the CodeQL scheduling cadence (nightly/weekly) specified or left ambiguous? [Clarity, Spec §FR-005] — reason: spec allows nightly/weekly either; no single cadence is fixed (contract uses weekly).
- [x] CHK010 Are the Semgrep rule sets (`p/csharp`) explicitly specified? [Clarity, Plan §contracts] — contract/tasks specify `--config p/csharp`.

## Requirement Consistency

- [x] CHK011 Do the security requirements align with the constitution's Security-First principle? [Consistency, Constitution §II] — Plan Constitution Check marks Principle II improved.
- [x] CHK012 Are the SAST requirements consistent with the non-goal of no gameplay changes? [Consistency, Spec §Non-Goals] — Non-Goals.
- [x] CHK013 Do the CodeQL requirements (scheduled) not conflict with the fast-gate requirement (Semgrep per-PR)? [Consistency, Spec §FR-004, FR-005] — Clarifications.

## Acceptance Criteria Quality

- [x] CHK014 Is SC-003 measurable and verifiable (0 high/critical findings on clean PR)? [Acceptance Criteria, SC-003] — SC-003.
- [x] CHK015 Are the acceptance scenarios for US2 testable independently (introduce vulnerability → scan fails)? [Acceptance Criteria, US2] — US2 scenarios.

## Scenario Coverage

- [ ] CHK016 Are requirements defined for the scenario where a security scan finds a false positive? [Coverage, Edge Cases] — reason: no security-scan-specific false-positive handling; Semgrep/Trivy suppressions are disallowed, only generic FR-012 analyzer suppression exists.
- [x] CHK017 Are requirements defined for the scenario where the CodeQL build step fails (missing game assemblies)? [Coverage, Plan §Research D3] — FR-005 uses `build-mode: none`, no game assemblies.
- [ ] CHK018 Are requirements defined for the scenario where a dependency has no fix available? [Coverage, Gap] — reason: no requirement for unfixed/no-fix-available dependencies.
- [ ] CHK019 Are requirements defined for handling newly disclosed vulnerabilities in existing dependencies? [Coverage, Gap] — reason: no explicit handling/cadence for newly disclosed vulns beyond per-PR scan.

## Edge Case Coverage

- [ ] CHK020 Is the fallback behavior specified when Semgrep/CodeQL service is unavailable? [Edge Cases, Gap] — reason: no fallback specified for unavailable Semgrep/CodeQL service.
- [x] CHK021 Is the behavior specified when a scan exceeds acceptable CI time? [Edge Cases, Spec §FR-005] — Spec edge case for CodeQL; Semgrep <2 min.
- [ ] CHK022 Are requirements defined for handling large numbers of low-severity findings? [Edge Cases, Gap] — reason: no handling specified for many low-severity findings.

## Non-Functional Requirements

- [x] CHK023 Are CI time requirements for the security scans specified (bounded)? [NFR, Spec §SC-001] — SC-001.
- [ ] CHK024 Are requirements for offline capability of security scanning specified? [NFR, Gap] — reason: "offline-capable" is aspirational in US2; no concrete offline capability requirement.
- [x] CHK025 Are requirements for SARIF output / GitHub Security tab integration specified? [NFR, Gap] — contract specifies SARIF upload for Semgrep/CodeQL.

## Dependencies & Assumptions

- [x] CHK026 Is the assumption that Semgrep CE covers C# adequately validated? [Assumptions, Research] — Plan D3: 6 rules, 0 findings on 78 files.
- [x] CHK027 Is the dependency on `BUILDDEPS_TOKEN` for the build-analysis job (not CodeQL) documented? [Dependencies, Plan §contracts] — FR-003 + contract inputs table.
- [x] CHK028 Is the LGPL license caveat (Semgrep CE) documented as a non-issue? [Dependencies, Research D6] — Plan D5 + Assumptions.

## Ambiguities & Conflicts

- [x] CHK029 Is there any ambiguity in whether security scans block merge or are advisory? [Ambiguity, Spec §FR-004, FR-005] — FR-004/FR-005/FR-010 + contract.
- [ ] CHK030 Is the term "high/critical" severity consistent with the chosen tools' severity scales? [Consistency, Terminology] — reason: only Trivy uses HIGH/CRITICAL; Semgrep uses ERROR and CodeQL uses error/warning.

## Notes

- Items are numbered sequentially for easy reference.
- Mark items `[x]` only after review confirms the requirement-quality criterion is satisfied.
- `/speckit-implement` reads checklist state as a gate and must not modify markers.