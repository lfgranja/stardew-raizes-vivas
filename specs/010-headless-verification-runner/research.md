# Research: Headless Game Verification Runner

**Feature**: 010-headless-verification-runner
**Date**: 2026-10-26

This document consolidates the research and decisions that resolve the `NEEDS CLARIFICATION` items from the spec's Technical Context and the carried-forward assumptions (A1–A16). Each entry follows the Decision / Rationale / Alternatives format.

## R-01 — Game acquisition: cache-first with private-repo download

- **Decision**: Cache-first: reuse `STARDEW_GAME_PATH` when (a) all 6 required assemblies present, (b) game/SMAPI versions classify INFO or WARN (not HARD-FAIL) against `compat-matrix.json`, and (c) the pinned SHA-256 in the reviewed `gamepath.lock.json` matches; acquire from the private repo (GitHub Releases assets, `BUILDDEPS_TOKEN`) only on cache miss or `--force-refresh`. Auth fail-fast (401/403/404 terminal); retry transient only (3–5 attempts, exponential backoff + full jitter, ~5 min cap); best-effort resume via `.part` + atomic rename; final SHA-256 always verified against `gamepath.lock.json`. The release `.sha256` is advisory only and never trusted alone.
- **Rationale**: Eliminates interactive game-store authentication entirely; matches the dormant `build_and_test.yml` pattern; idempotent re-runs; offline-capable for hosts with a valid install. Pinning the digest in a reviewed, version-controlled file prevents an attacker who can replace the artifact from also replacing its checksum. Aligns with `research/resilient-github-download.md` §9.2.5.
- **Alternatives considered**: Always-download (wasteful, network-dependent, conflicts with retry/flaky policy); interactive game-store authentication (explicitly excluded by FR-002).

## R-02 — Scenario format: JSON (Draft 2020-12) + optional Markdown companion

- **Decision**: JSON as canonical machine format, validated against a strict JSON Schema (Draft 2020-12, `additionalProperties: false` at every object level, absolute `$id`, `schemaVersion: const 2`, `$defs`). `JsonSchema.Net` on the .NET side; `check-jsonschema`/`ajv-cli` in CI/pre-commit. Optional human-facing Markdown checklist. Thresholds reference `ModConstants` values by name + numeric value; the structured `thresholds[].predicate` is the machine oracle (`condition` is a deprecated human description).
- **Rationale**: `System.Text.Json` is built-in (no YAML dependency, YAGNI); schema strictness matches `save-data-schema.md`. Contract at `specs/010-…/contracts/scenario.schema.json` (docs) → `schemas/scenarios/v2/` once ≥3 features reuse.
- **Alternatives considered**: YAML (adds dependency, not used anywhere in stack); Markdown-only (not machine-validatable).

## R-03 — Crop-phase capture: in-process dump (primary) + save-file cross-check

- **Decision**: Dev/CI-only `LivingRoots.Verification` mod writes a JSON dump at `DayEnding` *after* `ApplySoilGrowthModifier`, via an SMAPI mod-provided API (`ILivingRootsModApi.SoilGrowthModifierCompleted` event, raised synchronously on the game thread at the tail of `OnDayEnding`). Save-file inspection of serialized `Crop` data is the independent cross-check. Screenshots/OCR supporting only.
- **Rationale**: Deterministic ordering without cross-mod handler-ordering assumptions; keeps CWE-489 debug code out of the production binary (OWASP ASVS 14.3.2). Divergence → `partial` verdict (never auto-promoted to `pass`).
- **Alternatives considered**: Embedding dump code in the production mod (rejected — CWE-489); Harmony patching (rejected — direct access settled by spec 009).

## R-04 — Scenario injection: SMAPI macros + save seeding + custom commands

- **Decision**: `debug runmacro <file>` (deterministic batch: sleep, warp, item give) + StarDojo two-phase save seeding (milestone saves ≤ ~5 in private repo + per-scenario typed `initCommands`: `{kind: "macro", file}` or `{kind: "console", command}`) + custom console commands (`lr_dump_crop_phase`, etc.). Input simulation (xdotool) excluded.
- **Rationale**: Sufficient determinism with lower effort/risk than a bot mod; StarDojo prior art (arXiv:2507.07445). Mod sidecars (`soil_health_data_{saveId}.json`, `composting_bins_{saveId}.json`) are generated, reviewable, version-stable.
- **Alternatives considered**: Verification bot mod (not needed for injection); xdotool (fragile, non-deterministic).

## R-05 — Reproducible initial save: StarDojo hybrid

- **Decision**: (i) Milestone saves (≤ ~5 curated binary save folders) in the private builddeps repo with a JSON save manifest (game/SMAPI version, save name/id, SHA-256, documented contents); (ii) per-scenario typed `initCommands` (macros + custom commands) place bin, insert waste, set crop phases and soil health. Each run copies the milestone save into a temp `XDG_CONFIG_HOME`, runs macros, captures signals, restores, asserts byte-identical (SC-005). Version bumps are regeneration events.
- **Rationale**: Keeps mod-specific delta generated/reviewable/version-stable rather than opaque binary; aligns with FR-006/FR-018.
- **Alternatives considered**: Pure milestone saves (opaque binary, hard to review); pure `initCommands` (too slow, flaky).

## R-06 — Flaky/quarantine policy

- **Decision**: 2 retries (3 total attempts) for classified transient classes only (reuse smoke exit codes: 10 display, 4 launch timeout, 6 lock, 7 process conflict, save I/O, macro timeout; NOT retryable: threshold violation, crash, incomplete signals, version mismatch, missing dependency=9, host-guard breach). Fixed 5 s cooldown. `flaky` iff attempt 1 fails with a retryable class AND a later attempt passes; a flaky run exits `0` (PASS) with `flaky: true` and is never counted as a failure. A scenario becomes a **quarantine candidate** after 2 flaky runs in a rolling 14-day window (or 2 of last 3); automation proposes and a human approves entry. Approved quarantine has a 14-day time-box + one 14-day extension (28-day hard cap, no second extension), then escalates by **auto-disabling** the scenario (not re-enabling) + GitHub issue. Owner resolution chain: `--quarantine-owner` → `QUARANTINE_OWNER` env → git identity → repo owner (lfgranja), recorded in committed `quarantine.json`. Quarantined scenarios report as `skipped` in JUnit/Allure. No auto-promotion to `verified`.
- **Rationale**: Bounded, classified retry avoids masking real failures; quarantine prevents flaky scenarios from polluting `verified`; a hard cap prevents indefinite quarantine. Constants: `RunnerMaxTransientRetries=2`, `RunnerRetryCooldownMs=5000`, `RunnerFlakyObservationWindowDays=14`, `RunnerFlakyQuarantineTriggerCount=2`, `RunnerQuarantineTimeBoxDays=14`, `RunnerQuarantineExtendedTimeBoxDays=28`.
- **Alternatives considered**: Exponential backoff (rejected — fixed 5 s simpler and sufficient for env failures); auto-promotion after flaky pass (rejected — selection bias).

## R-07 — Version compatibility: single pinned stable line, 3-tier policy

- **Decision**: Latest SV 1.6.x + matching SMAPI, documented in version-controlled `compat-matrix.json`. HARD-FAIL (exit code, abort before Xvfb launch) if outside supported set or below `MinimumApiVersion`/`MinimumGameVersion`; WARN (proceed + record deviation) for newer-than-pinned or unverified-but-in-range; INFO for exact match. File-based detection before launch (parse `Stardew Valley.dll`, `StardewModdingAPI.dll`, `manifest.json`; mod self-check; matrix classification). Comparisons use SMAPI's `SemanticVersion` (never `string.Compare`; handle `1.11` < `1.2` and `-unofficial.*`). Manifest: add `"MinimumGameVersion": "1.6.14"` and bump `"MinimumApiVersion"` from `4.0.0` to `4.4.0` (the pinned line). Mod at 0.x is best-effort until 1.0.0. Evidence bundle gains a `compatibility` block. SMAPI null `MaximumGameVersion` → upper-bound deviation → warn.
- **Rationale**: Deterministic, file-based, no launch needed; avoids string-comparison bugs. Evidence: `research/version-compat-mismatch-policy-report.md` §5.2, §7.1–7.2.
- **Alternatives considered**: Auto-detect any version (too lenient, false confidence); exact-match only (too brittle across patches).

## R-08 — Host-guard breach detection

- **Decision**: In a scenario tagged as farmhand (non-host), a breach is indicated when execution evidence is present (the `Soil growth modifier elapsed: … ms` trace line, or a `phaseDays` mutation in the dump/save) *without* the `Soil growth modifier skipped: not main player.` skip line. Scenario definitions MUST declare expected player role (host vs. farmhand). Non-retryable; never quarantined or auto-promoted.
- **Rationale**: Direct, observable signal; aligns with Principle VI (host-authoritative) and the honesty-level discipline.
- **Alternatives considered**: Trusting `Context.IsMainPlayer` alone (no observable evidence); transport-level testing (deferred to Roadmap D1).

## R-09 — Farmhand session creation: `debug split`

- **Decision**: Split-screen via `debug split` as the default automated path for guard presence (sets `ScreenId != 0` → `Context.IsMainPlayer` false, exercising the guard branch without a second process). Manual protocol / Roadmap D1 as fallback for transport-level claims; two-instance host+client only when D1 funded. Runs documented as "host-authority guard exercised, transport not" (honesty W/R only; E never auto-assigned).
- **Rationale**: Exercises the actual guard branch cheaply; transport is not under test (guard only checks `Context.IsMainPlayer`). Aligns with `Invariants/SingleMultiplayerParityInvariantsTests.cs`.
- **Alternatives considered**: Two-instance launch (heavy, requires D1 funding); manual only (not automatable).

## R-10 — Divergence verdict: `partial` (signals disagree)

- **Decision**: When in-process dump and save-file cross-check disagree → verdict `partial`, recorded as an Anomaly Report entry; never auto-promoted to `pass`. Resolution requires a new full triad run that independently passes.
- **Rationale**: Divergence may expose the documented Phase-2 gap (hook writes state but `Crop.newDay()` ignores it) — the most valuable output, not noise. Aligns with Barr et al. (IEEE TSE 2015) pseudo-oracle formalism and the five-base-verdict model.
- **Alternatives considered**: Auto-promote on dump-only (weakens triangulation); treat divergence as `fail` (loses diagnostic nuance).

## R-11 — JSON Schema tooling/location

- **Decision**: Draft 2020-12, strict `additionalProperties: false` at every object level, a versioned logical `$id` (`urn:lfgranja:livingroots:scenario:v2`, not a file-path mirror), `schemaVersion: const 2`, `$defs` for reusable fragments. Location: `specs/010-…/contracts/scenario.schema.json` (documentation) → `schemas/scenarios/v2/` once Rule of Three (≥3 features) justifies extraction. Validation: `JsonSchema.Net` (.NET 6, MIT) on runner side; `check-jsonschema`/`ajv-cli` in CI/pre-commit; `$ref`s bundled at build time. `System.Text.Json` cannot validate (dotnet/runtime #29887). **Compatibility mode:** `BACKWARD_TRANSITIVE` (C-SCN-31). JSON Schema has no native compatibility keyword, so the policy is enforced by immutable published `vN` files, integer-major bump discipline (any strict-schema change is a bump), snapshot/regression tests on required fields and enums, and CI structural validation. Old scenarios remain readable for the supported major; an unsupported `schemaVersion` exits `14`.
- **Rationale**: Strictness idiom matches `save-data-schema.md`; bundled refs avoid network at runtime.
- **Alternatives considered**: YAML (adds dependency); `System.Text.Json` alone (cannot validate).

## R-12 — Verifier ownership: two-phase

- **Decision**: Phase 1 (now): in-repo `scripts/verify-evidence-bundle.sh` (stepping stone A) validates bundle format, verifier logic, refusal-site behavior while requirements emerge. Phase 2 (promotion trigger per FR-017, once 2+ features/mods reuse): extract to independently maintained open-source tool (B) with user-supplied trust anchors, documented bounded claims, refusal-site liveness testing.
- **Rationale**: SLSA/in-toto producer→consumer separation; Regra dos Três (no full external framework before 2+ reuses).
- **Alternatives considered**: Immediate external tool (premature, requirements still emerging); producer-as-sole-trust-anchor (defeats verification).

## R-13 — Farmhand session: `debug split`

- **Decision**: Split-screen via `debug split` as the default automated path for guard presence; manual protocol / Roadmap D1 as fallback for transport-level claims; two-instance host+client only when D1 funded. Document runs as "host-authority guard exercised, transport not" (honesty W/R only; E never auto-assigned).
- **Rationale**: `debug split` sets `ScreenId != 0` → `Context.IsMainPlayer` false, exercising the guard branch without a second process; guard only checks `Context.IsMainPlayer`. Aligns with honesty-level discipline and `Invariants/SingleMultiplayerParityInvariantsTests.cs`.
- **Alternatives considered**: Two-instance launch (heavy, D1-dependent); manual only (not automatable).

## R-14 — test.md / root-doc location: marker-based injection

- **Decision**: Feature `test.md` under `specs/<NNN-slug>/test.md`, identified via required `testMdPath` field in the Scenario Definition JSON (fail-fast validated in FR-008). The file MUST exist and contain exactly one `<!-- GATE3:BEGIN -->` … `<!-- GATE3:END -->` block; updates replace only the bytes inside that block. Root-doc updates (`ROADMAP.md`, `COMPLETION_SUMMARY.md`) via marker-based injection using the same HTML comment anchors, surgical replacement, idempotency check before write, `--check-docs` CI gate. Timestamps update only on actual state change.
- **Rationale**: Eliminates slug-resolution ambiguity; deterministic/idempotent/diff-minimal; matches the only existing convention (bug slug-resolution) being insufficient for features.
- **Alternatives considered**: Slug resolution (ambiguous for features); full-file rewrite (noisy diffs).

## R-15 — Delivery form: bash orchestrator

- **Decision**: Bash orchestrator (`scripts/verify-scenario.sh`) extending the spec-008 `smoke-validation.sh` substrate (Xvfb `-displayfd`, process-group/signal discipline, run log under XDG state, exit-code taxonomy). Invoked identically locally and in CI mode per FR-023. Docker deferred until 2+ mods reuse the loop (Regra dos Três).
- **Rationale**: Reuses the only implemented, tested orchestration substrate; no Dockerfile/registry exists; private-repo-download model means no game files to bundle; spec's own wording calls it an "orchestration script". Aligns with Principle V (YAGNI).
- **Alternatives considered**: Docker container (greenfield infra, justified only at 2+ reuses); .NET console app (re-implements process-group discipline, adds runtime dependency).

## R-16 — Exit-code contract: full contract, new codes at 18+

- **Decision**: Full contract: `0=PASS`, `18=FAIL`, `19=ERRORED` (incl. version mismatch), `20=NO_TESTS` (quarantined), `21=THRESHOLD`, `22=reserved`. Infrastructure classes inherited from smoke (1–17, 8 retired; code 11 is the inherited game-store/Steam prerequisite class and is never emitted by this runner because acquisition uses the private repo). `flaky` is metadata (exits 0, flagged); `skipped` exits 20 with JUnit `<skipped>`. `--ignore-exit-code <list>` escape hatch; published, semver-governed enum. ABORTED/USAGE/CRASH covered by inherited smoke codes (4, 14, 13). Suite aggregation: `19 > 18 > 21 > 20 > 0`.
- **Rationale**: Honors spec 008 allocation rule (no published code re-assigned); gives retry/quarantine classifier a machine-readable input; keeps POSIX-reserved values untouched. Aligns with pytest/MTP/TestNG gold-standard class distinction.
- **Alternatives considered**: Binary gate only (loses distinguishability); reusing 0–8 (collides with smoke taxonomy); keeping version mismatch at 2 (collides with `EX_SMAPI_NOT_FOUND`).

## R-17 — Concurrent allocation: Xvfb `-displayfd` + per-instance `mktemp`

- **Decision**: Each instance starts its own Xvfb with `-displayfd <fd>` (server atomically allocates free display number; no `/tmp/.X*-lock` probe, no TOCTOU race). Save isolation via per-instance `temp_root="$(mktemp -d)"` (absolute) with `XDG_CONFIG_HOME="${temp_root}/xdg"` and milestone save copied in; distinct temp roots guarantee distinct save copies/sidecars. Run-wide `flock` NOT reused (only short-lived locks around shared cache/build). `preflight_process_conflict` scoped to the instance's own game process. Original save hashed before/after, asserted byte-identical (SC-005).
- **Rationale**: Reuses proven `-displayfd` allocation; removes the one thing blocking concurrency (run-wide flock). Aligns with `scripts/smoke-validation.sh` `display_start()` and `research-report-reproducible-saves.md` §6.
- **Alternatives considered**: Script-chosen display numbers (racy); `/tmp/.X*-lock` probing (TOCTOU, Debian bug #1095028); global flock (serializes runs, defeats FR-020).

## R-18 — Host-guard breach detection

- **Decision**: In a farmhand-tagged scenario, a breach is indicated when execution evidence is present (the `Soil growth modifier elapsed: … ms` trace line, or a `phaseDays` mutation in the dump/save) WITHOUT the `Soil growth modifier skipped: not main player.` skip line. Scenario definitions MUST declare expected player role. Non-retryable; never quarantined or auto-promoted.
- **Rationale**: Direct, observable signal; aligns with Principle VI (host-authoritative) and honesty-level discipline.
- **Alternatives considered**: Trusting `Context.IsMainPlayer` alone (no observable evidence).

## R-19 — Public contract: SMAPI mod-provided API

- **Decision**: Override `ModEntry.GetApi()` to return `ILivingRootsModApi` (SMAPI-free, in `Domain/Interfaces/`) carrying `event EventHandler<SoilGrowthModifierCompletedEventArgs> SoilGrowthModifierCompleted`, raised synchronously on the game thread at the tail of `OnDayEnding`, after the `ApplySoilGrowthModifier` loop completes for all locations (success or caught exception), before the day transition. EventArgs carry `DayKey`, `IsMainPlayer`, `LocationsProcessed`, `ElapsedMilliseconds`, `Success`, `Error`. Verification mod subscribes via `helper.ModRegistry.GetApi<ILivingRootsModApi>("lfgranja.LivingRoots")` in `GameLoop.GameLaunched`. Subscriber exceptions isolated.
- **Rationale**: Converts implicit cross-mod handler-ordering assumption into a deterministic contract; keeps production binary free of CWE-489 debug code. Aligns with existing `ISoilHealthService.OnTileHealthChanged` event template and SMAPI's Integrations API. Prior art: StarDojo (arXiv:2507.07445).
- **Alternatives considered**: Runtime-gated debug code in main mod (rejected — CWE-489/OWASP ASVS 14.3.2); cross-mod handler ordering (non-deterministic).

## R-20 — Performance gate: single-shot after discarded warmup, Gen0 attribution

- **Decision**: One warmup invocation discarded before the measured run; the single per-location elapsed-ms trace line compared against 5 ms; Gen0 collection-count delta recorded in the evidence bundle; when > 0 the failure tagged `gc-attributed` (diagnostic; still a fail). Elapsed value per location (up to 3 lines/day), each evaluated independently.
- **Rationale**: Avoids cold-JIT false failures without full p95 statistical gate complexity. Aligns with FR-013/FR-014.
- **Alternatives considered**: Hard-fail on single line including cold run (false failures); p95 over ≥10 warm runs (harness complexity).

## R-21 — Missing-key semantics in threshold evaluation

- **Decision**: Tiles with no `SoilHealthState` entry (missing key) and tiles with health `<= 0` are non-participants (skipped, no change); only strictly positive stored health `< 34` counts as Poor. Missing keys treated as `0` only inside `SaveComparator` (persistence-layer diff, FR-012), not conflated with runtime health (`GetSoilHealth`: missing → NaN → skip → vanilla). Aligns with `ApplySoilGrowthModifier` (`health <= 0f` skip) and spec 009 CHK032.
- **Rationale**: Prevents conflating "no data" with "Poor soil"; matches the modifier's existing skip logic.
- **Alternatives considered**: Treat missing as 0/Poor (would incorrectly slow growth on untracked tiles).

## R-22 — Linux (Xvfb) hosts only for the automated runner

- **Decision**: Automated runner supports Linux (Xvfb) hosts only (NFR-001 narrowed). The mod remains cross-platform. Windows/macOS use build + unit tests plus the manual fallback (FR-021 / `VERIFICATION_PROTOCOL.md`). Xvfb has no admin-free, driver-free equivalent on Windows/macOS; CI stays `ubuntu-latest`.
- **Rationale**: Xvfb is Linux-only; no reliable equivalent elsewhere. Aligns with existing smoke-validation scope.
- **Alternatives considered**: Cross-platform runner (would require Windows/macOS display virtualization, out of scope).

## R-23 — Error handling / security conventions

- **Decision**: All user-influenced paths sanitized (Unicode normalization FormC, homoglyph detection, path traversal validation); no raw exception messages in logs (log `ex.GetType().FullName` + HResult at Trace); `CheckForOverflowUnderflow` enabled project-wide; soil health validated 0–100% per `ModConstants`. Token: fine-grained PAT scoped to single private repo (`Contents: read` + `Metadata: read`), shortest viable expiry; upgrade to GitHub App installation token for production. Checksum mismatch treated as security event.
- **Rationale**: Principle II (Security-First) and existing `ModDataService.SanitizePathSegments()` pattern. Aligns with OWASP CICD-SEC-9 (improper artifact integrity validation).
- **Alternatives considered**: Co-located checksum (attacker can tamper both); Contents API (1,000-entry cap, base64 overhead, 5-min URL expiry).
