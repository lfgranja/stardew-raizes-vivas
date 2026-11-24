# Feature Specification: CI / Release Gate Rigoroso (local-only)

**Feature Branch**: `019-ci-release-gate`

**Created**: 2026-11-08

**Status**: implementation (local-only; hub-first MDMV proposal `proposta-pendente`)

**Input**: Policy directive (Lucio, 2026-11-08): production must not depend on human approval; CI/CD + rigorous release gates are sufficient, or become the immediate next 80/20. MDMV `hub-first-policy-contract.md` (022) governs policy-default reach.

## Constitution alignment

- Constitution (LivingRoots) I. Domain-first, IV. TDD, III. strictest feasible linting, VI. host-authoritative multiplayer, VII. determinism-first: the gate is a deterministic verification vehicle; it asserts nothing human.
- 014/010: evidence discipline (no claim above machine evidence). Release work has **no** MDMV `machine-pass` for the human-attested dimension (the user will run D1/SC-004/verified separately); this feature gates only build/test/smoke reproducibility.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Local gate is a single deterministic command (Priority: P1)

As a maintainer, I want one command — `scripts/release-gate.sh` — that fails or passes with a named step and zero human input, covering format, locked restore, Release build, the full unit suite, the launch-free smoke suite, docs markers, and protected paths.

**Why this priority**: every later host-side required check needs a local green equivalent. A gate that depends on a human to interpret it is not a gate.

**Acceptance Scenarios**:

1. **Given** a clean tree and a set `GamePath`, **When** the gate runs on this host, **Then** every step passes and the gate exits 0 with the commit SHA.
2. **Given** a failing step (formatting drift, locked-restore drift, build error, failing test, failing smoke, docs drift, protected-path violation), **When** the gate runs, **Then** it exits non-zero, names the step, and prints the failing output.
3. **Given** a dirty tree (uncommitted changes), **When** the gate runs without flags, **Then** it exits 3 before touching the build.

**Independent Test**: revert one fix at a time (e.g., break formatting or a test expectation) and confirm the gate fails at the named step; restore and confirm green. The smoke and suite steps were verified for real (see verification/).

### User Story 2 - Honest failure output (Priority: P2)

As a reviewer, I want every refusal to restate the failing step and its output, without exiting 0 on any silent skip, mirroring the 014 evidence discipline.

**Why this priority**: false-green is this factory's known failure class; the gate is worthless if any step can quietly no-op.

**Acceptance Scenarios**:

1. **Given** the gate script, **When** any step fails, **Then** stderr names the step and the step's own tail output appears.
2. **Given** `SMOKE_SKIP_INTEGRATION=1`, **When** the launch-free suite has a failed file, **Then** the gate exits 15.
3. **Given** `SMOKE_SKIP_INTEGRATION` unset (out of scope for this host today), **Then** the integration suite is not run; the gap is documented and not claimed.

### User Story 3 - Release checklist with rollback (Priority: P2)

As a release operator, I want a checklist (SemVer bump, tag, artifact, rollback = git revert + known-good SHA) that the gate feeds, without enabling any host-side automation from this repo.

**Why this priority**: production-readiness without human gates requires every pre-publish step to be executable or explicitly externalized.

**Acceptance Scenarios**:

1. **Given** a commit that passes the gate, **When** a release operator (or automation) reads `specs/019-ci-release-gate/quickstart.md`, **Then** every step is named and either executable locally or listed under "Requires host authority".
2. **Given** a bad release, **Then** rollback is a git revert to the recorded known-good SHA; no redeploy steps are improvised.

## Gap analysis (honest; nothing here is enabled by this feature)

| Item | State | Why |
|---|---|---|
| Local deterministic gate | **implemented, verified** (steps 1–7) | this feature |
| Hosted CI `Build and Test` green | **dormant** | requires `BUILDDEPS_ENABLED=true` + `BUILDDEPS_TOKEN` (private builddeps release at `lfgranja/stardew-raizes-vivas-builddeps`) — host-side secret/var |
| `Check Formatting` / smoke as required checks | **NOT required** | false-green risk; drop `paths-ignore` before requiring; never require a check that has never been green on hosted infra |
| Branch protection / rulesets (main, dev) | **host-side** | no repository-admin credentials in this repo's hands |
| Auto-merge (provider native) | **host-side** | MDMV 022: auto-merge only with rigorous CI/CD + review |
| Release artifact publishing (SMAPI / mod sites) | **manual** | feeds a hub, not this repo |
| Gate 3 (real game) attestation | **separate** | not a merge gate; D1/SC-004/human will be done tonight by the user |

## Out of scope

- Pushing, merging, tagging, releasing, enabling host-side settings (explicit user authorization).
- Declaring this the MDMV policy default (hub-first 022: `proposta-pendente`, `local-only`).

## FR-019-1

The release gate SHALL fail closed: any skipped, failing, or silently absent step yields non-zero.

## FR-019-2

The release gate SHALL cover: working-tree cleanliness, lock-file restore, formatting, Release build, the full test suite, the launch-free smoke suite (or an explicit, documented refusal), docs markers, and protected paths.

## FR-019-3

All claims above machine evidence in this feature SHALL be recorded in `verification/`, with no step claimed green that was not executed on this host.
