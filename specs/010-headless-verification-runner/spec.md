# Feature Specification: Headless Game Verification Runner

**Feature Branch**: `010-headless-verification-runner`
**Created**: 2026-10-26
**Status**: Draft
**Input**: User description: "Automatizar a verificação de gameplay end-to-end: Construir o runner scriptado (Xvfb + SMAPI launch + injeção de cenário + captura de sinais + diff + thresholds + quit) para reutilizar para as outras features implementadas, do objetivo, do roadmap e em outros mods."

**Assessment artifacts**: `.specify/assessments/end-to-end-game-verification/` (intake, research, problem, concept, decision — verdict: go, Option B fully headless)

## Clarifications

### Session 2026-10-26

- Q: What format should scenario definitions use? → A: **JSON** (validated against a JSON Schema) as the canonical machine format, with an optional human-facing Markdown companion checklist. JSON has no parser dependency in .NET (`System.Text.Json` is built-in); YAML is not used anywhere in the stack and would add a dependency (YAGNI). Thresholds reference `ModConstants` values by name and numeric value.
- Q: How should `Crop.currentPhase`/`dayOfCurrentPhase` be captured unattended? → A: **In-process dump** (verification mod writes JSON at `DayEnding` after `ApplySoilGrowthModifier`, atomic temp-file+rename, "done" sentinel) as the primary oracle; **save-file inspection** of serialized Crop data as an independent cross-check (divergence is diagnostic). Screenshots/OCR are supporting evidence only.
- Q: What mechanism should the runner use to inject scenarios? → A: **SMAPI macros** (`debug runmacro <file>`) for deterministic batch command execution (sleep, warp, item give) + **save file seeding** (StarDojo pattern) for reproducible initial conditions + **custom mod console commands** (e.g., `lr_dump_crop_phase`) for mod-specific state capture. Input simulation (xdotool) is excluded (fragile, non-deterministic). No verification bot mod needed — macros + seeding + custom commands provide sufficient determinism with lower effort and risk. Prior art: StarDojo (arXiv:2507.07445, ECCV 2026).
- Q: How should the runner acquire game files? → A: **Private repository download via GitHub API** using a secret token (`BUILDDEPS_TOKEN`), following the same pattern already used by the dormant `Build and Test` workflow (`.github/workflows/build-and-test.yml`). The user's legally purchased game files are uploaded to a private repo; the runner downloads, verifies checksum, and extracts. This eliminates SteamCMD, Steam Guard, and interactive authentication entirely. The runner fails fast if the token is missing or the repo is inaccessible. No proprietary assemblies are distributed in the public repo.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Maintainer runs full verification unattended (Priority: P1) 🎯 MVP

As the maintainer, I want to invoke a single command that builds, deploys, launches Stardew Valley headless under SMAPI, injects the compost→growth scenario, captures all three signals, evaluates thresholds, and produces a pass/fail verdict plus an evidence bundle — without me opening the game, building a farm, planting, watering, or waiting days — so that Gate 3 verification becomes a repeatable, reliable, non-manual process.

**Why this priority**: This is the core value: eliminating the manual gameplay loop that currently blocks `verified` status and makes every feature's verification error-prone and non-reproducible.

**Independent Test**: On a host with `STARDEW_GAME_PATH` configured and a licensed Steam account, running the runner against feature 004's scenario completes unattended and produces (a) a pass/fail verdict, (b) an evidence bundle with all three signals, and (c) an updated `test.md` status — with zero human gameplay input.

**Acceptance Scenarios**:

1. **Given** a clean host with `STARDEW_GAME_PATH` set, Steam credentials configured, and the mod built, **When** the maintainer runs the runner for feature 004, **Then** the runner downloads/verifies game files via SteamCMD, launches Stardew under Xvfb + SMAPI, loads the verification mod, executes the compost→growth scenario, captures all three signals, evaluates thresholds, and exits with a pass/fail verdict — all without human gameplay input.
2. **Given** the runner completes a scenario, **When** the pass/fail verdict is `pass`, **Then** an evidence bundle (MIRROR-style: `manifest.json` with SHA-256 per file, `claims.json`, `artifacts/`, JUnit XML, Allure JSON) is written and `test.md` is updated from `partial` to `verified` with file paths to all three signals.
3. **Given** the runner detects a threshold violation (e.g., poor soil did not slow growth), **When** evaluation completes, **Then** the verdict is `fail` with a diagnostic naming the violated threshold and the observed vs. expected values, and `test.md` is NOT auto-promoted to `verified`.
4. **Given** the runner encounters a crash or incomplete signal set, **When** evaluation completes, **Then** the verdict is `errored` or `partial` (never auto-promoted to `pass`), with attached artifacts and per-attempt logs.

---

### User Story 2 - Contributor reuses the runner for another feature (Priority: P2)

As a contributor, I want to define a new scenario (checklist + thresholds) for my feature and run the same runner against it, so that Gate 3 verification is reproducible without inventing steps and the same harness serves all 19 roadmap features.

**Why this priority**: Reusability is the stated goal ("para reutilizar para as outras features implementadas, do objetivo, do roadmap e em outros mods"). Without a scenario contract, each feature reinvents verification.

**Independent Test**: A contributor can add a scenario definition for a second feature and run the runner against it without modifying the runner's core, producing a pass/fail verdict and evidence bundle.

**Acceptance Scenarios**:

1. **Given** a contributor has added a scenario definition (checklist + thresholds) for their feature, **When** they invoke the runner with that scenario, **Then** the runner executes it unattended and produces a verdict + evidence bundle identical in structure to US1.
2. **Given** a scenario definition is missing or malformed, **When** the runner starts, **Then** it fails fast with a clear error naming the missing/invalid field, before launching the game.

---

### User Story 3 - CI runs verification on a self-hosted runner (Priority: P3)

As CI infrastructure, I want the runner to be invocable from a self-hosted runner (with access to the private builddeps repository via `BUILDDEPS_TOKEN`), so that verification can be triggered automatically on push/PR without a human, while public CI remains dormant.

**Why this priority**: Tertiary — the runner must first work locally; CI integration is a follow-on. Public CI (`Build and Test`) stays dormant due to proprietary-DLL barriers.

**Independent Test**: On a self-hosted runner with `STARDEW_GAME_PATH` set and Steam credentials available, the runner executes a scenario and reports pass/fail to CI (JUnit XML + Allure JSON) without human intervention.

**Acceptance Scenarios**:

1. **Given** a self-hosted runner with access to the private builddeps repository via `BUILDDEPS_TOKEN`, **When** the runner is invoked in CI mode, **Then** it downloads game files, runs headless, produces JUnit XML + Allure JSON, and exits with a non-zero code on `fail`/`errored`/`partial`.
2. **Given** the public (GitHub-hosted) CI, **When** a PR is opened, **Then** the runner is NOT invoked (public CI has no game files); the manual/local runner remains the verification path.

---

### Edge Cases

- What happens when the private builddeps repository is inaccessible or the token is missing/invalid? The runner must fail fast with a clear error naming the missing token or repository, before attempting any download.
- What happens when the game version or SMAPI version mismatches the mod's expected version? The runner must detect and report the mismatch before scenario execution.
- What happens when a scenario step times out (e.g., crop does not grow within expected days)? The runner must abort with `errored`/`partial` and attach artifacts.
- What happens when the save file is corrupted or missing after the session? The runner must report `errored` with the before/after state, not crash.
- What happens when `STARDEW_GAME_PATH` is unset or points to an invalid path? The runner must fail fast with a clear error before attempting SteamCMD/game download.
- What happens when Xvfb fails to start or the display is unavailable? The runner must detect and report, not silently proceed.
- How does the runner handle a flaky scenario (passes on retry)? Flakies are quarantined with owner and time-box; retry is limited to transient/environmental failures with per-attempt logging.
- What happens when two runner instances run concurrently on the same host? They must use distinct save copies and distinct display numbers to avoid collision.

---

## Requirements *(mandatory)*

### Functional Requirements

#### Host & Environment

- **FR-001**: The runner MUST verify that `STARDEW_GAME_PATH` is set and points to a valid Stardew Valley + SMAPI installation before any other step; if not, it MUST fail fast with a clear, actionable error.
- **FR-002**: The runner MUST acquire game files from a **private repository** (e.g., `lfgranja/stardew-raizes-vivas-builddeps` or equivalent) via the GitHub API, authenticated with a secret token (`BUILDDEPS_TOKEN` or equivalent), following the same pattern already used by the dormant `Build and Test` workflow (`.github/workflows/build-and-test.yml`). The runner MUST verify the token is present and valid before attempting download; if the token is missing or the repository is inaccessible, it MUST fail fast with a clear, actionable error. The runner MUST NOT use SteamCMD or interactive Steam authentication — the private-repo pattern eliminates the Steam Guard barrier entirely. The downloaded archive MUST be verified (checksum) before extraction.
- **FR-003**: The runner MUST start a virtual display (Xvfb) and verify it is available before launching the game; if Xvfb fails to start, the runner MUST abort with a clear error.
- **FR-004**: The runner MUST deploy the mod (and the verification mod) into the game's `Mods/` folder and verify deployment succeeded before launch.

#### Launch & Scenario Injection

- **FR-005**: The runner MUST launch Stardew Valley under SMAPI headlessly and wait for the game to reach a ready state (mod loaded, save available) before injecting the scenario.
- **FR-006**: The runner MUST inject the scenario into the game without human input using **SMAPI macros** (`debug runmacro <file>`) for deterministic batch command execution (sleep, warp, item give, etc.) combined with **save file seeding** (StarDojo pattern) for reproducible initial conditions, plus **custom mod console commands** (e.g., `lr_dump_crop_phase`) for mod-specific state capture. The injection mechanism MUST be documented and reproducible. Input simulation (xdotool) is explicitly excluded (fragile, non-deterministic). A verification bot mod is NOT required — SMAPI macros + save seeding + custom commands provide sufficient determinism with lower effort and risk.
- **FR-007**: The runner MUST support a scenario definition (checklist + thresholds) as input, so that different features can be verified without modifying the runner's core. The scenario definition MUST be authored as **JSON** (validated against a JSON Schema), with an optional human-facing **Markdown** companion checklist for contributor readability. JSON is the canonical machine format (no parser dependency in .NET — `System.Text.Json` is built-in; YAML is not used anywhere in the stack and would add a dependency, violating YAGNI). Thresholds in the scenario MUST reference `ModConstants` values by name and numeric value.
- **FR-008**: The runner MUST validate the scenario definition before launch (required fields, valid thresholds, valid boundaries); if invalid, it MUST fail fast with a field-level error.

#### Signal Capture & Evaluation

- **FR-009**: The runner MUST capture all three signals for every run: (i) `SoilHealthState` JSON delta (before/after `modData/soil_health_data_{saveId}.json`), (ii) `Crop.currentPhase` / `dayOfCurrentPhase` delta captured via **in-process dump** (a verification mod/bot hook writes a JSON dump at `DayEnding` after `ApplySoilGrowthModifier`, with before/after pairs, atomic temp-file+rename, and a "done" sentinel) as the primary oracle, with **save-file inspection** of serialized `Crop` data as an independent cross-check (divergence between dump and save is diagnostic, not noise), and (iii) SMAPI console log excerpt confirming event execution and elapsed ms.
- **FR-010**: The runner MUST apply the threshold rules: poor soil (health < 34) MUST slow growth (1.15× on `phaseDays`), healthy soil (health ≥ 67) MUST accelerate growth (0.85×), moderate [34, 67) MUST be unchanged (1.00×); `phaseDays` MUST never go below 1 and the 99999 sentinel MUST never be touched. Tiles with NaN health MUST be skipped (no change).
- **FR-011**: The runner MUST compute a pass/fail/errored/partial/skipped verdict using the 4-state model: crash → `errored` with attached artifacts; incomplete signal set → `partial` (never auto-promoted to `pass`); transient/environmental failure → retry with bounded attempts and per-attempt logging; confirmed flaky → quarantine with owner and time-box.
- **FR-012**: The runner MUST use the `SaveComparator` (0.0001f float tolerance, missing keys treated as 0, NaN tiles ignored) for the SoilHealthState delta, producing `added`/`removed`/`changed` entries.
- **FR-013**: The runner MUST parse the SMAPI log for the elapsed-ms trace line and hard-fail if elapsed > 5 ms for the run (SC-009 criterion 6).

#### Evidence & Output

- **FR-014**: The runner MUST produce a MIRROR-style evidence bundle: `manifest.json` (schema version, run id, timestamp ISO 8601, tool/game/mod versions, environment, SHA-256 per file), `claims.json` (claim → evidence pointer, risk, safe wording), `artifacts/` (screenshots, SMAPI logs, save-deltas before/after, reflection outputs), JUnit XML (CI), and Allure JSON (rich report). Honesty levels: W (launch/connect) / R (runtime evidence collected) / E (human judgment, never auto-assigned).
- **FR-015**: The runner MUST produce an offline-verifiable bundle: a verifier recomputes digests and validates schema; structural pass ≠ truth.
- **FR-016**: The runner MUST update `test.md` from `partial` to `verified` only when all three signals agree and the verdict is `pass`; on `fail`/`errored`/`partial`, it MUST NOT auto-promote.
- **FR-017**: The runner MUST update `COMPLETION_SUMMARY.md` and `ROADMAP.md` Gate 3 status on a `pass` verdict, recording the promotion trigger for deferred components (reflector/logger/runner promoted only after 2+ features reuse the loop).

#### Process & Lifecycle

- **FR-018**: The runner MUST always run against a disposable copy of the save (never the player's real save); it MUST back up/restore `Saves/<Name>/` and sidecars, using `XDG_CONFIG_HOME` pointing to a temp dir; the original save MUST be byte-identical at the end.
- **FR-019**: The runner MUST quit the game cleanly after scenario completion and capture all artifacts before exit.
- **FR-020**: The runner MUST support concurrent instances on the same host using distinct save copies and distinct display numbers.
- **FR-021**: The runner MUST preserve the manual SC-009 protocol as a documented fallback (`VERIFICATION_PROTOCOL.md` §3, `gate3-verification-contract.md`) for hosts where the runner cannot execute.

#### Reusability

- **FR-022**: The runner MUST expose a scenario contract so that a feature can define its own scenario (steps + thresholds) without modifying the runner's core.
- **FR-023**: The runner MUST be invocable both locally (interactive) and in CI mode (non-interactive, machine-readable output).

### Key Entities

- **Scenario Definition**: A declarative description of a verification scenario — steps (place bin, insert waste, sleep N days, harvest, apply compost, observe), expected signals, and threshold rules. Provided by a feature; consumed by the runner.
- **Evidence Bundle**: A directory (`evidence-bundle-<run-id>/`) containing `manifest.json`, `claims.json`, `artifacts/`, JUnit XML, and Allure JSON. Produced by every run.
- **Run Verdict**: One of `pass` / `fail` / `errored` / `partial` / `skipped`, with attached diagnostics and artifact paths.
- **Signal Set**: The three required signals (SoilHealthState delta, Crop phase delta, SMAPI log) plus optional visual evidence.

### Non-Functional Requirements

- **NFR-001**: The runner MUST be reproducible on any host with `STARDEW_GAME_PATH` set and a licensed Steam account; it MUST NOT require changes to the mod's source code.
- **NFR-002**: The runner MUST NOT distribute or embed proprietary game assemblies in the public repository; it MUST acquire them from a private repository via authenticated API (secret token). The runner relies on a pre-configured host with access to the private repo.
- **NFR-003**: The runner MUST produce a non-zero exit code on `fail`/`errored`/`partial` for CI integration.
- **NFR-004**: The runner MUST log each step with timestamps for debugging; on failure, it MUST attach all available artifacts.

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A maintainer can verify feature 004's compost→growth loop end-to-end with zero human gameplay input, producing a `pass`/`fail` verdict and a complete evidence bundle, in a single invocation.
- **SC-002**: The runner produces all three signals (SoilHealthState JSON delta + Crop phase delta + SMAPI log) for every run, with no auto-promotion to `pass` on incomplete signals.
- **SC-003**: Threshold evaluation correctly identifies poor soil (health < 34) slowing growth (1.15×) and healthy soil (health ≥ 67) accelerating growth (0.85×) in the captured evidence.
- **SC-004**: A second feature scenario runs through the same runner without modifying the runner's core (validates the scenario contract).
- **SC-005**: The original save is byte-identical after a runner execution (save preservation verified).
- **SC-006**: The runner exits non-zero on `fail`/`errored`/`partial` and zero on `pass` for CI integration.
- **SC-007**: Evidence bundle digests (SHA-256) are independently verifiable by an offline verifier.
- **SC-008**: A maintainer with `STARDEW_GAME_PATH` set can reproduce a verification run following the documented invocation, without inventing steps.

---

## Assumptions

- **A1**: A host with access to the private builddeps repository (via `BUILDDEPS_TOKEN` secret) is available; the runner downloads game files from there rather than via SteamCMD. The user's legally purchased game files are hosted in the private repo (same pattern as the dormant `Build and Test` workflow).
- **A2**: The private builddeps repository contains a complete, valid game + SMAPI installation archive (`gamepath.zip` or equivalent) with all 6 required assemblies; the runner verifies checksums before extraction.
- **A3**: Xvfb + MonoGame rendering is stable enough for unattended scenario execution on Linux (precedent: JunimoServer runs Stardew + SMAPI headless on Linux via Docker/Xvfb).
- **A4**: Scenario injection can be driven by a verification mod (bot mod) or SMAPI console commands without human input (precedent: dedicated-server mods automate host behavior).
- **A5**: `Crop.currentPhase` / `dayOfCurrentPhase` can be captured unattended via in-process dump to file/log or save-file inspection of serialized Crop data.
- **A6**: The SMAPI console log reliably contains the elapsed-ms trace line and `DayEnding` event name in a parseable format (format may need version-pinning).
- **A7**: The manual SC-009 protocol (`VERIFICATION_PROTOCOL.md` §3, `gate3-verification-contract.md`) remains a documented fallback for hosts where the runner cannot execute.
- **A8**: The runner is an internal reusable module first; "para outros mods" is aspirational and requires the scenario contract to be defined and proven (Regra dos Três).
- **A9**: The 7 proprietary DLLs in `.git` (`.gamepath`) are not distributed by the runner; the runner relies on a pre-configured host.
- **A10**: The 3-signal triangulation and threshold rules from spec 009 (34/67 boundaries, 1.15×/0.85× multipliers, never below 1, never touch 99999 sentinel, NaN tiles skipped) are the authoritative evaluation criteria for the runner.

---

*Specification for the fully headless game verification runner. Source: assess pipeline (intake/research/problem/concept/decision) under slug `end-to-end-game-verification`, verdict `go` (Option B). All functional requirements are testable; success criteria are measurable and technology-agnostic where possible (tool names appear only where the tool is the feature's subject).*