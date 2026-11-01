# Feature Specification: SonarCloud to Local Gate Baseline Readiness

**Feature Branch**: `012-sonarcloud-to-local-gate-baseline-readiness`

**Created**: 2026-11-01

**Status**: Draft

**Input**: User description from assessment - capture fresh baselines for local OSS quality gate after zeroing SonarCloud issues.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Capture fresh coverage and duplication baselines (Priority: P1)

As the maintainer, I want fresh coverage and duplication baselines measured on the current dev state after SonarCloud fixes, so that the local quality gate (spec 011) can set fair thresholds via ratchet.

**Why this priority**: Required by FR-009/FR-017 of spec 011; old baselines may be stale due to refactorings.

**Independent Test**: Run coverage via ReportGenerator and duplication via jscpd on current codebase and record results in baseline files.

**Acceptance Scenarios**:

1. **Given** current dev branch with SonarCloud issues fixed, **When** coverage is measured, **Then** `.coverage-baseline` is updated with current values.
2. **Given** current dev branch, **When** duplication is measured, **Then** `.jscpd-baseline.json` is updated with current values.
3. **Given** fresh baselines exist, **When** thresholds are set, **Then** they reflect current state (not stale values).

### User Story 2 - Verify readiness for spec 011 TDD sequence (Priority: P2)

As the maintainer, I want to confirm Phase 2 tests are in place and RED before proceeding with analyzer implementation, so that the TDD discipline of spec 011 is followed.

**Why this priority**: Phase 2 is blocking per spec 011 plan.

**Independent Test**: Check feature branch 011-local-oss-quality-gate for committed RED tests.

**Acceptance Scenarios**:

1. **Given** feature branch exists, **When** tests are run, **Then** gate tests are RED as expected.
2. **Given** tests are RED, **When** implementation proceeds, **Then** TDD sequence is followed correctly.

### User Story 3 - Anticipate analyzer rule differences (Priority: P3)

As the maintainer, I want to be prepared for new warnings from local analyzers (AnalysisLevel=6.0-recommended + TreatWarningsAsErrors), so that any issues are treated as part of feature 011 work not regressions.

**Why this priority**: SonarCloud rulesets differ from local analyzers; new warnings may appear.

**Independent Test**: Run analyzer probe/build to identify potential new warnings.

**Acceptance Scenarios**:

1. **Given** clean SonarCloud state, **When** local analyzers run with strict settings, **Then** any new warnings are catalogued.
2. **Given** new warnings exist, **When** triaged, **Then** they are planned as part of feature 011 remediation.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST capture fresh coverage baseline using ReportGenerator on current dev state
- **FR-002**: System MUST capture fresh duplication baseline using jscpd on current dev state  
- **FR-003**: System MUST record baselines in `.coverage-baseline` and `.jscpd-baseline.json`
- **FR-004**: System MUST verify Phase 2 TDD test state on branch `011-local-oss-quality-gate`
- **FR-005**: System MUST document analyzer differences and potential new warnings
- **FR-006**: All work MUST follow the plan in spec 011 without skipping phases

### Key Entities

- **Coverage Baseline**: Measured code coverage percentages (lines, branches)
- **Duplication Baseline**: Measured code duplication metrics
- **Analyzer Warnings**: New warnings from local analyzers not caught by SonarCloud

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Fresh baselines are captured and committed to track current post-fix state
- **SC-002**: Phase 2 RED tests are confirmed present and committed before implementation
- **SC-003**: Analyzer probe shows no unexpected blocking issues or issues are triaged
- **SC-004**: Feature 011 can proceed with fair thresholds based on current state
