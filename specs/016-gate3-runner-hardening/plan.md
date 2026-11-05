# Implementation Plan: Gate 3 Runner Hardening

**Branch**: `016-gate3-runner-hardening` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/016-gate3-runner-hardening/spec.md`

## Summary

Harden the headless verification runner (spec 010) so the three defects that blocked Gate 3 cannot recur silently: save-name mismatch (align or fail loudly), two-line SMAPI excerpt (full capture default, documented opt-in excerpt), and schema-invalid manifest (single-producer + detached SHA-256 anchor enforced by the offline verifier). Additionally close the `subject[]` completeness gap (every bundle file covered, not just artifacts) and document the red-gate deviation. The core repair is already applied and committed; this feature adds regression guards and closes residual gaps.

## Technical Context

**Language/Version**: Bash (scripts), Python 3 (helpers), .NET 6 / C# (runner, tests)
**Primary Dependencies**: `scripts/lib/*.sh` (common, evidence, save, signals, verdict, promotion, retention, logging), `LivingRoots.Verification.Runner` (.NET), `jsonschema` (Python), `ajv-cli` (npm)
**Storage**: Filesystem (evidence bundles, manifest.json, manifest.sha256, claims.json, retention receipts)
**Testing**: `scripts/tests/test-*.sh` (TAP-style shell), xUnit + Moq + FsCheck + Coyote (C#), `scripts/tests/lib/schema-validate.sh` (two-validator agreement)
**Target Platform**: Linux (Fedora 44 KDE/Wayland with Xvfb), self-hosted runner
**Project Type**: CLI tooling + verification runner (scripts + .NET helper)
**Performance Goals**: Real-host scenario completes within 30 minutes (SC-008); verifier runs in <5 s per bundle
**Constraints**: No new dependencies; no new capabilities; `verified` remains human-only (T148); no input simulation (FR-006 of spec 010)
**Scale/Scope**: 3 regression guards + 1 completeness check + 1 documentation task; touches 6 scripts, 2 test files, 3 contracts, 1 schema

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Principle I (DDD)**: N/A — this is tooling, not domain logic. No domain code changes.
- **Principle II (Security)**: PASS — path sanitization preserved; no new user-influenced paths.
- **Principle III (Async)**: PASS — no I/O blocking changes.
- **Principle IV (TDD)**: PASS — regression guards are tests-first (revert → fail → restore → pass).
- **Principle V (Simplicity)**: PASS — smallest scope that prevents recurrence; no new abstractions beyond verifier completeness check.
- **Principle VI (Multiplayer)**: N/A — tooling only.

No violations. No complexity tracking needed.

## Project Structure

### Documentation (this feature)

```text
specs/016-gate3-runner-hardening/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── verifier-completeness.md
│   ├── save-alignment.md
│   └── log-capture.md
├── spec.md              # Feature specification
└── checklists/
    └── requirements.md  # Quality checklist
```

### Source Code (repository root)

```text
scripts/
├── lib/
│   ├── evidence.sh          # evidence_write_manifest_input, evidence_finalize
│   ├── save.sh              # save_prepare, save_align_inner_name
│   ├── signals.sh           # signals_smapi_log_excerpt (now full cp)
│   ├── verify-evidence-bundle.sh  # schema + digest + completeness
│   └── common.sh            # exit codes
├── tests/
│   ├── test-evidence.sh     # manifest assembly assertions
│   ├── test-bundle-negative.sh  # schema-valid fixtures
│   ├── test-save-preservation.sh # save alignment regression guard
│   └── lib/
│       └── runner-harness.sh    # staged loadable save
specs/010-headless-verification-runner/
├── contracts/
│   └── evidence-bundle.schema.json  # immutable v1 contract
└── verification/
    └── *.log                # recorded verification artifacts
```

**Structure Decision**: No new source directories. All changes are to existing scripts, tests, and verification artifacts. The feature adds 3 contract documents and 1 data-model document under `specs/016-gate3-runner-hardening/`.

## Complexity Tracking

> No violations. Table not needed.