# Security Requirements Quality Checklist: Local OSS Quality Gate

**Purpose**: Validate the quality, clarity, completeness, and measurability of the security (SAST/SCA) requirements before implementation.
**Created**: 2026-10-29
**Feature**: [spec.md](../spec.md)

**Note**: This custom checklist is a reviewer-owned requirements-quality review artifact. Mark an item `[x]` only when the reviewer determines the requirements-quality criterion is satisfied. It does NOT mean implementation work is complete.

## Requirement Completeness

- [ ] CHK001 Are all security scanning tools (Semgrep, CodeQL, SCA) explicitly named with their roles? [Completeness, Spec §FR-004, FR-005, FR-006]
- [ ] CHK002 Is the distinction between fast per-PR scan (Semgrep) and deep scheduled scan (CodeQL) explicitly required? [Completeness, Spec §FR-005, Clarifications]
- [ ] CHK003 Are requirements for dependency-vulnerability scanning (Trivy against the committed NuGet lock files) specified? [Completeness, Spec §FR-006]
- [ ] CHK004 Are requirements for handling secrets scanning (Gitleaks) included or explicitly excluded? [Completeness, Gap]
- [ ] CHK005 Is the failure threshold for security findings (0 high/critical) quantified? [Completeness, SC-003]
- [ ] CHK006 Are requirements for IaC/container scanning specified or explicitly excluded? [Completeness, Gap]

## Requirement Clarity

- [ ] CHK007 Is "security finding" defined with a severity classification (high/critical)? [Clarity, Spec §SC-003]
- [ ] CHK008 Is the scope of SAST scanning (which files/directories) explicitly defined? [Clarity, Gap]
- [ ] CHK009 Is the CodeQL scheduling cadence (nightly/weekly) specified or left ambiguous? [Clarity, Spec §FR-005]
- [ ] CHK010 Are the Semgrep rule sets (`p/csharp`) explicitly specified? [Clarity, Plan §contracts]

## Requirement Consistency

- [ ] CHK011 Do the security requirements align with the constitution's Security-First principle? [Consistency, Constitution §II]
- [ ] CHK012 Are the SAST requirements consistent with the non-goal of no gameplay changes? [Consistency, Spec §Non-Goals]
- [ ] CHK013 Do the CodeQL requirements (scheduled) not conflict with the fast-gate requirement (Semgrep per-PR)? [Consistency, Spec §FR-004, FR-005]

## Acceptance Criteria Quality

- [ ] CHK014 Is SC-003 measurable and verifiable (0 high/critical findings on clean PR)? [Acceptance Criteria, SC-003]
- [ ] CHK015 Are the acceptance scenarios for US2 testable independently (introduce vulnerability → scan fails)? [Acceptance Criteria, US2]

## Scenario Coverage

- [ ] CHK016 Are requirements defined for the scenario where a security scan finds a false positive? [Coverage, Edge Cases]
- [ ] CHK017 Are requirements defined for the scenario where the CodeQL build step fails (missing game assemblies)? [Coverage, Plan §Research D3]
- [ ] CHK018 Are requirements defined for the scenario where a dependency has no fix available? [Coverage, Gap]
- [ ] CHK019 Are requirements defined for handling newly disclosed vulnerabilities in existing dependencies? [Coverage, Gap]

## Edge Case Coverage

- [ ] CHK020 Is the fallback behavior specified when Semgrep/CodeQL service is unavailable? [Edge Cases, Gap]
- [ ] CHK021 Is the behavior specified when a scan exceeds acceptable CI time? [Edge Cases, Spec §FR-005]
- [ ] CHK022 Are requirements defined for handling large numbers of low-severity findings? [Edge Cases, Gap]

## Non-Functional Requirements

- [ ] CHK023 Are CI time requirements for the security scans specified (bounded)? [NFR, Spec §SC-001]
- [ ] CHK024 Are requirements for offline capability of security scanning specified? [NFR, Gap]
- [ ] CHK025 Are requirements for SARIF output / GitHub Security tab integration specified? [NFR, Gap]

## Dependencies & Assumptions

- [ ] CHK026 Is the assumption that Semgrep CE covers C# adequately validated? [Assumptions, Research]
- [ ] CHK027 Is the dependency on `BUILDDEPS_TOKEN` for the build-analysis job (not CodeQL) documented? [Dependencies, Plan §contracts]
- [ ] CHK028 Is the LGPL license caveat (Semgrep CE) documented as a non-issue? [Dependencies, Research D6]

## Ambiguities & Conflicts

- [ ] CHK029 Is there any ambiguity in whether security scans block merge or are advisory? [Ambiguity, Spec §FR-004, FR-005]
- [ ] CHK030 Is the term "high/critical" severity consistent with the chosen tools' severity scales? [Consistency, Terminology]

## Notes

- Items are numbered sequentially for easy reference.
- Mark items `[x]` only after review confirms the requirement-quality criterion is satisfied.
- `/speckit-implement` reads checklist state as a gate and must not modify markers.