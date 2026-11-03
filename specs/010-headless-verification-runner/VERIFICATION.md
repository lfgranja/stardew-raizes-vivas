# Verification: Headless Game Verification Runner

**Feature**: 010-headless-verification-runner
**Date**: 2026-11-03

This document records the exact verification commands and their **placeholders** for outputs.
Actual results are recorded by T131–T147 in `specs/010-headless-verification-runner/verification/` and `gate3-evidence/`. Values below are placeholders only; never pre-stated actuals.

## Commands

### T131 — Build
```
dotnet build Stardew-LivingRoots.sln --configuration Release
```
Output: `verification/build.log` (placeholder: <build result>)

### T132 — Tests
```
dotnet test Stardew-LivingRoots.sln --no-build --verbosity normal
scripts/tests/run-all.sh
```
Output: `verification/test.log` (placeholder: <test counts>)

### T133 — Formatting
```
dotnet format Stardew-LivingRoots.sln --verify-no-changes
```
Output: `verification/format.log` (placeholder: <format result>)

### T134 — Shell
```
bash -n scripts/**/*.sh ; shellcheck scripts/**/*.sh
```
Output: `verification/shell.log` (placeholder: <shell result>)

### T135 — Schema
```
scripts/tests/lib/schema-validate.sh
```
Output: `verification/schema.log` (placeholder: <schema result>)

### T136 — Launch-free e2e
VS-01 … VS-10, VS-09b, VS-09c against the fake host.
Output: `verification/launch-free.log` (placeholder: <exit codes>)

### T137 — Real-host e2e
VS-01 on a host with `STARDEW_GAME_PATH` and valid cache/token.
Output: `verification/real-host.log` (placeholder: <result or BLOCKED>)

### T138 — Second-host 3/3
Output: `verification/second-host.log` (placeholder: <verdict equality>)

### T139 — Timed clean-host dry run
Output: `verification/clean-host-dry-run.log` (placeholder: <wall-clock>)

### T140 — Quarterly audit/deletion drill
Output: `verification/audit-deletion-drill.log` (placeholder: <receipts>)

### T141–T145 — Negative suites
- bundle-negative.log, check-docs-negative.log, acquisition-negative.log, suite-negative.log, farmhand.log

### T146 — Promotion (machine-pass only)
Confirmed via `scripts/lib/promotion.sh` after T131–T145. Never `verified`.

### T147 — Gate 3 evidence
`gate3-evidence/` (observation notes, artifact comparison, SMAPI log, session date, bundle pointer).

### T148 — Human E attestation (sole writer of `verified`)
Reviewer, date, session recorded in `specs/004-playable-compost-loop/test.md`, `ROADMAP.md`, `COMPLETION_SUMMARY.md`.

### T149 — Checklist/traceability audit
Re-run one reviewer per checklist; update `traceability.md`.

### T150 — Final commit
Commit all artifacts above with `docs(010): ...`.
EOF
echo "created"