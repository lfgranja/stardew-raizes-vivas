# Decision: Scripted, reusable end-to-end gameplay verification runner (Gate 3)

- **Slug**: end-to-end-game-verification
- **Decided**: 2026-10-26
- **Verdict**: go
- **Artifacts reviewed**: intake.md (2026-10-26) | research.md (2026-10-26) | problem.md (2026-10-26) | concept.md (2026-10-26)

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | `In-game runs: 0` documented across `test.md`, `VERIFICACAO_ESTADO.md`, `ROADMAP.md`, `COMPLETION_SUMMARY.md`; constitution Gate 3 mandates end-to-end verification for runnable-code changes; Phase 2 hook is live but unverified. [source: problem.md; research.md] |
| Evidence strength | adequate | Strong internal evidence for the gap and constraints (proprietary DLLs, dormant CI, 3-signal rule, existing `SaveComparator`). Medium evidence for headless substrate (JunimoServer proves Xvfb+SMAPI+Stardew on Linux). Scenario injection and reusability contract are low/unknown evidence, explicitly carried as assumptions. [source: research.md; concept.md] |
| Value vs. inaction | strong | Maintainer explicitly requires full automation to increase evidence level and reliability without manual gameplay; manual protocol does not scale to 19 roadmap features or other mods. [source: user directive 2026-10-26; problem.md] |
| Feasibility / appetite | weak (Option B) | Option B (fully headless: Xvfb + SteamCMD + bot mod) is large-appetite (months) with serious unmitigated risks (Steam Guard/auth, Xvfb+MonoGame stability, scenario-injection contract, proprietary-DLL licensing, maintenance). Maintainer explicitly accepts this cost and risk. [source: concept.md; research.md] |
| Strategic fit | adequate | Aligns with constitution Gate 3 and the "COMPLETA" automation goal. Overrides the Regra dos Três / YAGNI deferral recorded in spec 009 clarify rounds 4–5 by explicit maintainer decision. [source: user directive 2026-10-26; constitution.md Principle V] |
| Risk posture | weak (Option B) | Serious, partially unmitigated risks remain: proprietary-DLL licensing exposure (`.gamepath`), false-confidence from incomplete signal capture, maintenance burden across SMAPI/Stardew bumps, over-engineering for a single consumer. Maintainer explicitly accepts these risks and the possibility of rework. [source: research.md Evidence Against; concept.md] |

## Verdict & Rationale

**go — for Option B (Fully headless runner: Xvfb + SteamCMD + bot mod driving scenario).** The maintainer has explicitly and repeatedly directed full automation of the gameplay verification ("Quero AUTOMATIZAR completamente"), overriding the hybrid/manual fallback and the prior YAGNI/Regra dos Três deferral. The problem is real and worth solving (problem validity strong); evidence strength is adequate for the problem and substrate (JunimoServer proves headless Xvfb+SMAPI+Stardew on Linux; the 3-signal rule, thresholds, and evidence-bundle format are defined and partially implemented). Feasibility and risk posture for Option B are weak, but the maintainer has explicitly accepted the large appetite, the cost, and the unmitigated risks, and has rejected the manual/hybrid alternatives. Per the maintainer's authority over the project's scope, the verdict is `go` for Option B, with all risks and assumptions documented and carried forward. This is a deliberate, informed acceptance of risk — not a claim that the evidence makes Option B low-risk.

## If needs-clarification

_(Not applicable — verdict is go. The carried-forward items below are specification-time assumptions and risks, not blockers.)_

## If go — Handoff to `/speckit.specify`

- **Problem**: No automated, unattended verification of the gameplay loop exists; every feature ships with `In-game runs: 0`, and the manual SC-009 session does not scale to 19 roadmap features or other mods.
- **Chosen approach**: Option B — Fully headless runner: containerized pipeline (Docker/Xvfb/SteamCMD) that downloads Stardew + SMAPI on a licensed host, launches the game headless, loads a verification mod that drives the scenario (place bin, insert waste, sleep N days, harvest, apply compost, dump Crop state to file), captures all 3 signals, diffs saves, applies threshold rules, and quits with a pass/fail verdict.
- **In scope**: (1) headless runner (Docker/Xvfb/SteamCMD + game download + SMAPI launch + mod loading + process lifecycle + quit); (2) scenario-injection mechanism (bot mod or SMAPI console commands driving the full compost→growth loop); (3) signal capture (SoilHealthState JSON delta, Crop.currentPhase/dayOfCurrentPhase delta, SMAPI log with elapsed-ms trace line); (4) `SaveComparator` integration (before/after diff, 0.0001f tolerance, NaN handling, missing-key-as-0); (5) threshold evaluation (poor < 34 → 1.15× slower, healthy ≥ 67 → 0.85× faster, never below 1, never touch 99999 sentinel); (6) MIRROR evidence bundle assembly (manifest.json with SHA-256, claims.json, artifacts/, JUnit XML, Allure JSON, W/R/E honesty levels); (7) pass/fail verdict with 4-state model (pass/fail/errored/partial/skipped); (8) documentation in `CONTRIBUTING.md`/`AGENTS.md`; (9) reusable scenario contract for other features/mods.
- **Out of scope**: resolution of `.gamepath`/proprietary DLL licensing (`.gamepath` remains as-is; runner requires a pre-configured licensed host); redesign of `Crop.newDay()` or hook integration; CI-hosted execution on GitHub-hosted runners (requires self-hosted runner with Steam account + game files); standalone product packaging for other mods (reusable module only).
- **Success metrics**: runner executes the full compost→growth loop unattended on a host with `STARDEW_GAME_PATH` set, producing all 3 signals with pass/fail verdict; `test.md` updated to `verified` by runner output; ≥ 1 second feature scenario runs through the same runner without forking; runner invocation documented and reproducible; manual protocol retained as fallback.
- **Carried-forward open questions (assumptions/risks to validate during spec)**:
  - A1: SMAPI log reliably contains the elapsed-ms trace line and `DayEnding` event name in a parseable, stable format across SMAPI versions.
  - A2: Scenario injection can be driven via a bot mod or SMAPI console commands without human input (mechanism unproven — dedicated-server bot mod pattern is the closest precedent).
  - A3: `Crop.currentPhase`/`dayOfCurrentPhase` can be captured unattended (in-process dump to file/log or save-file inspection of serialized Crop data).
  - A4: Xvfb + MonoGame rendering is stable under unattended execution with mod loading (JunimoServer proves hosting, not testing under load).
  - A5: SteamCMD + Steam Guard authentication can be automated or pre-configured on the host (manual prerequisite, not removed by the runner).
  - A6: The scenario contract (minimal interface a feature implements to be "runnable") can be defined without over-engineering for a single consumer.
  - A7: The 7 proprietary DLLs in `.git` (`.gamepath`) are acceptable for a runner that requires a pre-configured host (no distribution).
  - A8: Maintainer accepts the large appetite (months), the maintenance burden across SMAPI/Stardew bumps, and the risk of false confidence from incomplete signal capture.

## Superseded implementation details (2026-10-27)

The `go` verdict and the fully-headless goal stand. The following *implementation* details in this historical decision were resolved differently during specification and are superseded by `specs/010-headless-verification-runner/spec.md`:

- **Delivery form**: a **bash orchestrator** (`scripts/verify-scenario.sh`) extending the spec-008 `smoke-validation.sh` substrate, not a Docker/container pipeline. Docker is deferred until 2+ mods reuse the loop (Regra dos Três).
- **Game acquisition**: private GitHub Releases assets fetched with `BUILDDEPS_TOKEN` (cache-first; authoritative SHA-256 pinned in `gamepath.lock.json`), not SteamCMD/Steam Guard. Interactive game-store authentication is eliminated.
- **Verification mod**: the dev/CI-only `LivingRoots.Verification` mod is a separate SMAPI mod consuming `ILivingRootsModApi`; it is not a bot mod embedded in production.
- **Proprietary assemblies**: six required assemblies are preflighted, not seven.
- **Manifest versions**: `MinimumApiVersion` is `4.4.0` and `MinimumGameVersion` is `1.6.14`.

---

*Decision judges; it does not spec or build. Verdict is `go` for Option B (fully headless) per explicit maintainer directive, overriding the prior YAGNI/Regra dos Três deferral and the hybrid recommendation. Feasibility and risk posture for Option B are weak but explicitly accepted by the maintainer. The 8 carried-forward items are specification-time assumptions and risks, not blockers to specifying. Implementation details superseded during specification are listed above.*