# Tasks: SonarCloud to Local Gate Baseline Readiness

## Phase 0: Baseline Capture
- [ ] T001 Run dotnet test with coverage to generate coverage reports
- [ ] T002 Generate coverage summary with ReportGenerator
- [ ] T003 Run jscpd to generate duplication report
- [ ] T004 Update .coverage-baseline with fresh values
- [ ] T005 Update .jscpd-baseline.json with fresh values

## Phase 1: Readiness Verification  
- [ ] T006 Check branch 011-local-oss-quality-gate exists and has Phase 2 tests
- [ ] T007 Run analyzer probe script
- [ ] T008 Document any new warnings found

## Phase 2: Commit and Report
- [ ] T009 Commit updated baselines
- [ ] T010 Report readiness status
