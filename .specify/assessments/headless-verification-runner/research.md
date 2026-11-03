# Idea Research: Headless Verification Runner for Gate 3

- **Slug**: headless-verification-runner
- **Created**: 2026-11-02
- **Evidence confidence (overall)**: medium

## Users & Demand

- Headless game servers for Stardew Valley are a well-established community practice: SMAPI mods run under Xvfb (`xvfb-run -a .../StardewModdingAPI`) on Linux without a GUI (SMAPI Dedicated Server Mod, JunimoHost, stardew-always-on-server). — [source: github.com/ObjectManagerManager/SMAPIDedicatedServerMod, github.com/slayermaster/stardew-always-on-server] (confidence: high, cited)
- JunimoServer (Docker) runs Stardew as a genuine headless dedicated server with SMAPI, supporting automated host play and 24/7 farms. — [source: github.com/stardew-valley-dedicated-server/server] (confidence: high, cited)
- Automated playtest runners with screenshot diffing exist for Godot (godot-qa, godot-game-harness) and browser games (instaplay playtest gate, Playwright screenshot guide) — pattern: headless launch + input replay + screenshot capture + JUnit/JSON report. — [source: github.com/youichi-uda/godot-qa, github.com/jonathancaruso/godot-game-harness, snap-render.com playwright guide] (confidence: high, cited)
- The project already has a working smoke launcher (spec 008): `scripts/smoke-validation.sh` launches Stardew under Xvfb and checks mod load — proven feasible on this host (real run exit 0). — [source: specs/008-smoke-validation/tasks.md, in-repo evidence] (confidence: high, cited)
- Demand signal: spec 010 is 0/161 tasks; Gate 3 has 0 in-game runs; the harvest-quality effects (Poor → 0, Living → boost) cannot be attested as "verified" without evidence. — [source: ROADMAP.md, specs/009-.../checklists/gate3-verification-contract.md] (confidence: high, cited)

## Prior Art

- **SMAPI Dedicated Server Mod / JunimoServer**: headless host automation via Xvfb; config-driven farm loading; no human host. Pattern: Xvfb + automated bot. — [source: github.com/ObjectManagerManager/SMAPIDedicatedServerMod, github.com/stardew-valley-dedicated-server/server] (confidence: high, cited)
- **JunimoServer E2E infrastructure**: xUnit v3 + Testcontainers + pooled game clients/servers + Vue/TS monitoring UI; shared mod code between server and test client. Pattern: in-process SMAPI mod + test client mod + orchestrator. — [source: github.com/stardew-valley-dedicated-server/server CLAUDE.md] (confidence: high, cited)
- **Godot QA (youichi-uda)**: headless SceneTree script, input replay, node/text asserts, screenshot diff vs baselines, JUnit XML output, CI integration. Pattern directly transferable to scenario JSON. — [source: github.com/youichi-uda/godot-qa] (confidence: high, cited)
- **Godot Game Harness (jonathancaruso)**: `make test` / `make shot` (headless screenshots via xvfb + software GL), no GPU needed. Pattern: Makefile-driven headless capture. — [source: github.com/jonathancaruso/godot-game-harness] (confidence: high, cited)
- **Hayao (hellojanpacan)**: headless SVG screenshots in Node; `?capture=1` URL installs scripted-session API (`pump`, `probe`, `hash`, `shot`); determinism hash for replay verification. Pattern: in-game capture hook exposed via URL flag. — [source: github.com/hellojanpacan/hayao-js docs/VERIFICATION.md] (confidence: high, cited)
- **Instaplay playtest gate**: headless browser plays canvas game, returns PUBLISH/FIX with evidence; monkey-testing + determinism checks. Pattern: seeded RNG + frame diff. — [source: github.com/kushagra0526/instaplay-playtest-gate-prototype] (confidence: medium, cited)
- **In-repo smoke validation (spec 008)**: Xvfb + SMAPI + two-phase readiness wait + log gate + SHA-256 deploy digest. Already proven on this host. — [source: specs/008-smoke-validation/tasks.md, STATE.md] (confidence: high, cited)

## Market & Context

- Stardew Valley has no official dedicated server; community uses Xvfb or Docker (JunimoServer). Headless execution is a solved problem on Linux. — [source: survivalservers.com, hostmygame.eu, valebyte.com] (confidence: high, cited)
- SMAPI 4.5.2 is the current target; `--mods-path` and `--no-terminal` flags support scripted launches. — [source: smapi.io, SMAPI docs/technical/smapi.md] (confidence: high, cited)
- Screenshot capture under Xvfb requires software GL (`llvmpipe`); the host already has `glxinfo` reporting GLX+RANDR and `llvmpipe`. — [source: in-repo smoke evidence, STATE.md] (confidence: high, cited)
- Video capture: ffmpeg + Xvfb can record the virtual display; no game-native video export. — [source: AI_Hell gameplay-capture.md (headless Chromium + ffmpeg pattern)] (confidence: medium, cited)
- The cost of NOT automating: Gate 3 remains 0 runs; "verified" status for harvest-quality effects is blocked; manual 30-min sessions per change are unsustainable. — [source: ROADMAP.md, VERIFICATION_PROTOCOL.md] (confidence: high, cited)

## Data & Constraints

- **Platform limit**: Stardew Valley requires a display (X11) even in headless mode; Xvfb provides it. Software GL (`llvmpipe`) is available. — [source: in-repo smoke evidence] (confidence: high, cited)
- **SMAPI mod API**: `ILivingRootsModApi` must be additive (interface in `Domain/Interfaces`, impl in `Services`) per Principle I (DDD). — [source: constitution.md Principle I, plan.md] (confidence: high, cited)
- **Async-only**: bash is synchronous by nature; plan acknowledges. No `.Result`/`.Wait()` in production code. — [source: constitution.md Principle III] (confidence: high, cited)
- **Multiplayer honesty**: farmhand path via `debug split`; honesty capped at R (not "verified"). Transport-level claims deferred to Roadmap D1. — [source: constitution.md Principle VI, spec 010] (confidence: high, cited)
- **Security**: SHA-256 mandatory, in-process extraction, path sanitization parity with `ModDataService.SanitizePathSegments`. — [source: constitution.md Principle II, spec 010] (confidence: high, cited)
- **Evidence quality**: `machine-pass`/`verified` are doc statuses, never verdicts; runner never writes `verified` (human-only E attestation). — [source: spec 010, spec 009 gate3-verification-contract.md] (confidence: high, cited)

## Evidence Against the Idea

- **Complexity vs. YAGNI (Principle V)**: 161 tasks, 50+ bash libs, 2 C# projects — large scope for a mod that has 0 in-game runs. Risk of building infrastructure before validating the core loop. — [source: spec 010 tasks.md, constitution Principle V] (confidence: high, cited)
- **Headless graphics limitations**: software GL (`llvmpipe`) may not render identically to hardware; screenshot baselines captured in CI may differ from local. Requires baseline capture in CI, not locally. — [source: godot-qa README, Playwright guide (Docker pitfalls)] (confidence: high, cited)
- **No deterministic replay yet**: Stardew Valley is not deterministic (RNG, timing); "verified" cannot mean byte-identical replays. The runner must rely on multi-signal evidence (save diff + phase delta + log), not pixel-perfect screenshots. — [source: spec 009 VERIFICATION_PROTOCOL.md, hayao docs (determinism caveats)] (confidence: high, cited)
- **Headless runner depends on game install**: GitHub-hosted runners lack Stardew DLLs; requires self-hosted runner or `gamepath.zip` (BUILDDEPS_TOKEN). CI integration (US3) is non-trivial. — [source: ROADMAP.md CI section, spec 010 US3] (confidence: high, cited)
- **Potential false-green risk**: if the runner reports `machine-pass` without human attestation, it could be mistaken for `verified`. The spec explicitly guards against this (T148) but enforcement is unimplemented. — [source: spec 010 tasks.md T148, spec 009] (confidence: high, cited)

## Gaps & Open Questions

- [NEEDS CLARIFICATION: whether the runner should reuse `scripts/smoke-validation.sh` infrastructure or be separate]
- [NEEDS CLARIFICATION: communication mechanism between C# companion and running game (SMAPI mod API vs. file-based IPC)]
- [NEEDS CLARIFICATION: whether `verified` attestation can be partially automated or remains human-only]
- [NEEDS CLARIFICATION: MVP scope — full 161 tasks vs. minimal 3-signal capture]
- [NEEDS CLARIFICATION: CI runner type (self-hosted vs. GitHub-hosted with gamepath.zip)]

## Sources

- https://github.com/ObjectManagerManager/SMAPIDedicatedServerMod (host: github.com, policy: allowlisted)
- https://github.com/slayermaster/stardew-always-on-server (host: github.com, policy: allowlisted)
- https://github.com/stardew-valley-dedicated-server/server (host: github.com, policy: allowlisted)
- https://github.com/youichi-uda/godot-qa (host: github.com, policy: allowlisted)
- https://github.com/jonathancaruso/godot-game-harness (host: github.com, policy: allowlisted)
- https://github.com/hellojanpacan/hayao-js (host: github.com, policy: allowlisted)
- https://github.com/kushagra0526/instaplay-playtest-gate-prototype (host: github.com, policy: allowlisted)
- https://snap-render.com/blog/playwright-screenshot-guide (host: snap-render.com, policy: unrecognized — fetched as research source)
- https://smapi.io, https://github.com/Pathoschild/SMAPI (host: github.com/smapi.io, policy: allowlisted)
- In-repo: specs/008-smoke-validation/tasks.md, specs/009-.../checklists/gate3-verification-contract.md, VERIFICATION_PROTOCOL.md, ROADMAP.md, STATE.md, constitution.md