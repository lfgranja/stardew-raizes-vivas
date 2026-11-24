# Living Roots — Technical & Verification Landscape (2026-11-09)

Research for Gate 3 / D1 (real-multiplayer) verification. All URLs accessed 2026-11-09 unless noted.
Confidence: **H** = primary source fetched directly · **M** = secondary/corroborated · **L** = not verified.
Repo baseline: `.specify/memory/constitution.md` Principle VI; `VERIFICATION_PROTOCOL.md`; `specs/010`, `specs/016`.

## 1. Verified facts table

| # | Fact | Source | Conf |
|---|---|---|---|
| 1 | SMAPI latest = **4.5.2**; requires Stardew Valley **1.6.14+** (Linux/macOS/Win/Steam Deck). 4.5.2 published via GitHub tag 2026-03-22; release-notes.md says "Released 14 March 2026"; CurseForge file uploaded 2026-03-14. Date discrepancy noted, substance agrees. | github.com/Pathoschild/SMAPI/releases/tag/4.5.2; raw…/develop/docs/release-notes.md; smapi.io | H |
| 2 | SMAPI 4.5.0/4.5.1 tags published 2026-01-28 (release notes say 25 Jan 2026 for 4.5.0); 4.5.1 fixes Linux/macOS installer error + translations; 4.5.2 = perf + Linux/macOS color-scheme installer fix + translations. | same as above | H |
| 3 | SMAPI 4.5 headline features: in-game config UI via Generic Mod Config Menu; automated/attested builds; separate "SMAPI for developers" edition **retired** (developer mode now a GMCM toggle); malicious loose-file detection; perf gains. | github.com/Pathoschild/SMAPI release notes (4.5.0) | H |
| 4 | SMAPI 4.5.2 bundles `smapi-internal/0Harmony.dll` (AssemblyVersion **2.2.2.0** → Lib.Harmony **2.2.2**; NuGet shows 2.2.0/2.2.1/2.2.2, plus later 2.4.x — irrelevant to the SMAPI-bundled reference). SMAPI references it as `$(GamePath)\smapi-internal\0Harmony.dll` when `EnableHarmony=true`; it is **not** a mod NuGet dependency. | raw 4.5.2 tree + binary strings; src/SMAPI.ModBuildConfig/build/smapi.targets; api.nuget.org | H |
| 5 | Living Roots csproj: net6.0, ModBuildConfig **4.4.0** (latest; 2025-04-13), `EnableHarmony=true` → links SMAPI's bundled 0Harmony (2.2.2.0), not a newer Lib.Harmony. | repo csproj; NuGet registration | H |
| 6 | IModHelper exposes: DirectoryPath, Events, ConsoleCommands (ICommandHelper), GameContent, ModContent, ContentPacks, Data, Input, Reflection, ModRegistry, **Multiplayer (IMultiplayerHelper)**, **Translation (ITranslationHelper)**. | raw 4.5.2 src/SMAPI/IModHelper.cs | H |
| 7 | `IMultiplayerHelper.SendMessage<TMessage>(message, messageType, modIDs, playerIDs)` broadcasts JSON-serialized mod messages (ModMessageModel via JsonHelper); receiving side = `IMultiplayerEvents.ModMessageReceived` with `FromPlayerID`, `FromModID`, `Type`, `ReadAs<TModel>()`. Also PeerContextReceived/PeerConnected/PeerDisconnected. FR-009/A8 implementation matches this surface. | raw 4.5.2 IMultiplayerHelper.cs / IMultiplayerEvents.cs / MultiplayerHelper.cs | H |
| 8 | `IGameLoopEvents` includes `DayStarted` / `DayEnding` (host broadcast hook), `SaveCreated/SaveLoaded`, `GameLaunched`, update ticks. | raw 4.5.2 IGameLoopEvents.cs | H |
| 9 | i18n: translation helper via `Helper.Translation` + `i18n/*.json` (e.g. `default.json`); mods reference keys in code. (Wiki docs blocked today; source confirms interface.) | Living Roots i18n exists; SMAPI source above | H/M |
| 10 | Latest **PC game = 1.6.15** (released **2024-12-20** on PC+mobile; console got 1.6.15.1 later). SMAPI 4.5.x targets 1.6.14+; JunimoServer decompiled tree is `sdv-1.6.15-24356`; its badge pins "SDV v1.6.15". | TheGamer (2024-12-20); github.com/stardew-valley-dedicated-server/server; stardewvalley.net (1.6.15.1 console patch URL) | H |
| 11 | **Stardew Valley 1.7** announced, in active development, **no release date**. 2026-02-26 = 10th anniversary; new marriage candidates (Clint & Sandy); new farm type; expanded social features; ConcernedApe uses a team and a "relaxed pace" while focusing on Haunted Chocolatier; ~50k watched the anniversary premiere. | stardewvalley.net/stardew-valley-10-year-anniversary; Game Informer (2026-02-26); Notebookcheck; hauntedchocolatier.net (2025-09-02); Wikipedia | H |
| 12 | **No official ConcernedApe dedicated server** exists or is announced; game is host-based P2P; "dedicated" = headless host process. Commercial hosts confirm. | Survival Servers, Space-node, Shockbyte blogs; Wikipedia mods section (over 30k mods, no official server) | H/M |
| 13 | **JunimoServer** repo **moved**: `Junimo-Server/JunimoServer` → **404**; canonical = **github.com/stardew-valley-dedicated-server/server** (MIT, 383★, pushed 2026-11-08, not archived). | GitHub API/redirect check | H |
| 14 | JunimoServer **latest stable GitHub release = `sdvd-server-v1.4.0`** (2026-01-17; siblings v1.3.0/v1.2.0 also 2026-01-17, v1.1.0 2026-01-03). Tag `v1.4.1` exists but has **no GitHub release**. Docs banner: *"The latest release is unstable. Use preview builds instead."* Preview channel: `v1.5.0-preview.140` (pushed 2026-11-08). | GitHub API releases/tags; docs.junimoserver.com banner | H |
| 15 | Docker Hub `sdvd/server`: `latest` tag **stale** (2026-01-07), `preview` = 1.5.0-preview.140 (2026-09-21). Pin by digest/tag in any automation. | hub.docker.com API | H |
| 16 | JunimoServer targets **SDV v1.6.15**; requires a Steam account that owns the game; game files via SteamCMD-like `steam-auth` container; Steam refresh tokens expire periodically. | README badge; docs (e2e-testing.html, Quick Start) | H |
| 17 | JunimoServer architecture: steam-auth (game files + session) → **server** container (game + SMAPI + mods; UDP 24642; VNC/noVNC 5800; HTTP API 8080) → optional **client** container (game + `JunimoTestClient` mod; HTTP API **5123**). X11 + window manager required: headless without WM crashes at startup; no OpenGL without GPU (software Mesa). jlesage/baseimage-gui (Debian) in prod; Alpine/musl `docker/modern/` is experimental. | docs (e2e-testing.html, modern-docker.html); docker/Dockerfile.test-client | H |
| 18 | JunimoServer runs an **E2E test harness**: Testcontainers + custom xUnit runner; leases server/client containers; captures screenshots/video; publishes CTRF + optional Cloudflare R2 reports; multi-host via SSH Docker API; **manual, maintainer-gated** CI (`e2e-tests.yml`, workflow_dispatch / `!run-tests-e2e` PR comment), never an automatic merge gate; needs a VPS + Steam accounts + owned copies. | docs (e2e-testing.html, test-harness.html); repo badge; .github/workflows | H |
| 19 | `JunimoTestClient` (in-repo `tests/test-client/`, net6.0, EnableHarmony, ModBuildConfig 4.4.0) — HTTP API on 5123 for client automation: menu navigation, character/movement, chat, co-op join/auth, screenshots, god-mode tweaks, skip-intro. **Not published as a package**; MIT; designed for JunimoServer's own harness. | raw manifest.json / ModEntry.cs / tree | H |
| 20 | `JunimoServer.Shared` is shared server/client code (e.g. RenderingController) — reuse requires copying or referencing internal projects. | tests/test-client csproj | H |
| 21 | Server-only mods work headless; content mods must match versions on server **and** every client; UI/input/window-dependent mods useless on server; host-side event/cutscene mods can stall day transitions. No mods reported incompatible ("none so far"), but list is not maintained. | docs/features/mods.html | H |
| 22 | **SMAPIDedicatedServerMod** (ObjectManagerManager): v1.0.1, net5.0, ModBuildConfig 4.1.0, `MinimumApiVersion 3.0.0`; **last commit 2024-02-07**; zero releases; open issues into 2026 (#20, #22). SMAPI 4.5.x / SV 1.6 compatibility **not verified** — do not adopt blindly. | GitHub API; raw manifest.json/csproj | H |
| 23 | **ValleyServer** (Lixeer, Apache-2.0, 580★, pushed 2026-11-07): 2.x = real client + automation MOD (ALOS) + Docker, **maintenance mode**; 3.x = decompiled/mock protocol endpoint, **experimental, "not for production"**; latest release v3.0.3 (2026-09-20). | github.com/Lixeer/ValleyServer README; releases API | H |
| 24 | **Always On Server** (funny-snek, Nexus #2677) is legacy; actively kept alive via forks (e.g. `theghost99/StardewUnattendedServer`: .NET 6, SV 1.6.9+, SMAPI 4.1.7; `truemanlive/puppy-stardew-server`: Docker + Xvfb + VNC + ALOS). | Nexus (via search; direct page 403); GitHub forks | M |
| 25 | **StardewModders/mod-reference-assemblies** exists for CI builds (reference DLLs incl. Stardew Valley.dll, StardewModdingAPI.dll, SMAPI.Toolkit, Harmony); pushed 2026-01-17. | GitHub API + contents | H |
| 26 | **Pathoschild/StardewMods** (Content Patcher, Automate, Lookup Anything…) CI on ubuntu-latest: `dotnet build` + `dotnet test --no-build`, using the reference-assemblies repo; tests are **pure unit tests** (e.g. LexerTests, TokenStringTests, Tests.Common); **game is never launched** in CI. | .github/workflows/build.yml; tree | H |
| 27 | **StardewValleyExpanded** (FlashShifter/StardewValleyExpanded): no CI workflows found; content-pack mod, untested in CI. **GenericModConfigMenu** (spacechase0/StardewValleyMods): no workflows found. No major SMAPI mod runs in-game headless tests in CI; none has an official headless test framework. | GitHub API (workflows endpoints) | H |
| 28 | **stardewvalleywiki.com** (official SMAPI modding docs) returns **403 (Cloudflare)** to non-browser agents today; **Nexus** mod pages also 403 to direct fetches; **smapi.io/mods** compatibility list is deprecated/"sunset". | live fetch attempts | H |

## 2. Headless / dedicated landscape (Nov 2026)

- **JunimoServer** is the only actively maintained, Linux-first, Docker-packaged, **automated-client-capable** solution. For a Living Roots D1 test you'd add the mod locally to the server container + a client container, drive both via REST (`/diagnostics/state` snapshot, long-poll players/status, time/clock-speed, new-game, screenshot) and `JunimoTestClient` (movement, chat, coop join, menu actions), assert on server-side `modData`/log/signals, exactly the flow its own E2E suite uses. Costs: Steam account + owned copy, game files (~GBs) download via steam-auth, VPS/SSH for CI, all manual and maintainer-gated; **no path to run in public CI without credentials** (fork PRs need approval + secrets environment). Also caveat: `docs` says **latest stable is unstable — use preview**; v1.5.0-preview.140 is current; expect churn.
- **SMAPIDedicatedServerMod**: conceptually the same (headless host + automation) but **unmaintained since Feb 2024**, pre-1.6, pre-SMAPI-4.x tooling (net5.0/ModBuildConfig 4.1.0). Treat as historical reference, not a dependency.
- **ValleyServer**: interesting but 2.x frozen (MOD+Docker, VNC on 5800) and 3.x explicitly experimental/single-maintainer; no test automation API; Chinese-community primary docs.
- **Always On Server / ALOS lineage**: what most 24/7 hosting panels actually run; proven "time freezes when empty" behavior (shockbyte docs confirm); not automation-oriented; no API, no scripted-client story.
- **Commercial/"official-feeling" dedicated**: all are ALOS-style headless hosts, not an official product.
- **Official ConcernedApe dedicated server**: none; no announcement; 1.7 roadmap is content (social/farm/marriage), not server architecture.
- **Xvfb path validated**: production JunimoServer = X11 + Openbox; docs explicitly call out WM requirement (null Farmer crash on window resize without it), disabled screensaver/DPMS (display blanking), software-GL Mesa footprint, `SERVER_FPS=0` to disable rendering; modern Alpine image uses Xvfb+Openbox+PipeWire. Living Roots' existing `Xvfb` runner (spec 010) mirrors this and remains sound for host-only scenarios.

## 3. SMAPI 4.5.x API notes (verified vs. blocked)

- **Targeting**: net6.0 + ModBuildConfig 4.4.0 is current practice (JunimoTestClient uses the same). `EnableHarmony=true` binds SMAPI's bundled 0Harmony **2.2.2.0** — `[HarmonyPatch]` surface = Lib.Harmony 2.2.2 semantics (prefix/postfix/transpiler/reverse-patch). NuGet offers newer Lib.Harmony (2.4.3; 3.0.0 only as RC) but the SMAPI-bundled DLL is what compiles/loads for `EnableHarmony` mods.
- **MP messaging**: `Helper.Multiplayer.SendMessage(msg, type, modIDs, playerIDs)` → `ModMessageReceived` handlers in receiving mods; `modIDs` filter is recommended (perf). Messages are JSON; `ReadAs<T>()` deserializes. ✓ matches FR-009/A8.
- **Events**: `IGameLoopEvents` (DayStarted, DayEnding, SaveLoaded/Saved/Created, GameLaunched, update ticks), `IMultiplayerEvents` above, plus input/display/content events in the same `Events/` folder. All verified in 4.5.2 source.
- **Console commands**: mods register via `Helper.ConsoleCommands.Add(string name, string documentation, Action<string, string[]> callback)` (verified in 4.5.2 source); SMAPI's built-ins (add, list-items, set-farm-type, etc.) ship in `SMAPI.Mods.ConsoleCommands`. Living Roots' console commands follow the documented pattern.
- **i18n**: `Helper.Translation` + per-locale `i18n/<locale>.json`; fallback to `default.json`. (Interface verified in source; wiki guide blocked.)
- **Retired surface**: "SMAPI for developers" build gone — don't reference it; use GMCM developer mode. **Mod compatibility list on smapi.io is sunset** — check GitHub/Nexus/Discord per-mod instead.
- **Not verified (blocked)**: stardewvalleywiki.com Modding:* docs (403 Cloudflare); stardewvalley.net patch notes (page reachable; 1.6.15 PC = 2024-12-20 confirmed via TheGamer + TheGamer-linked dev post); 1.6.15.1 scope for PC = none found.

## 4. Testing practices of major mods

| Project | Practice |
|---|---|
| **Pathoschild/StardewMods** (Content Patcher, Automate, LookupAnything) | CI `ubuntu-latest`: clone `StardewModders/mod-reference-assemblies` into fake GamePath → `dotnet build` → `dotnet test --no-build`. NUnit + FluentAssertions unit tests of pure logic (lexer, token strings, common utils). **No game launch.** |
| **StardewValleyExpanded** | No CI/workflows found; content-pack JSON; manual playtesting. |
| **GenericModConfigMenu** (spacechase0/StardewValleyMods) | No CI/workflows found; manual + downstream mods' integration tests. |
| **JunimoServer** | Only project with full headless E2E (Testcontainers, client bot, screenshots/video, R2 reports) — bespoke, self-hosted, maintainer-gated; needs Steam accounts + owned game; ~seconds-scale container startup (~28 s cold start). |
| **Living Roots** | Already exceeds typical practice: 1747 unit tests + isolated headless runner (`scripts/verify-scenario.sh`, Xvfb) + evidence bundles. Gap = multi-peer (farmhand) coverage, i.e. D1. |

## 5. Limitations & blocked sources

- **stardewvalleywiki.com** (official SMAPI docs): **403 Cloudflare** to this agent; SMAPI release notes sourced from GitHub instead; API surface double-checked against source. Any wiki-only claim below is marked M/L.
- **Nexus mods pages** (ReGen Valley, Always On Server 2677, etc.): **403** to direct fetch; cited via search snippets — treat exact versions as **unverified**.
- **GitHub code search** (`/search/code`): **401** unauthenticated; code evidence gathered via raw.githubusercontent (OK) and API trees.
- **GitHub API rate limit** reached at end of session (unauthenticated); not all Events/ interfaces enumerated — DayStarted confirmed, rest deferred.
- **smapi.io/mods** compatibility list "no longer comprehensively updated… future in doubt" (sunset) — not a verification source.
- **SMAPI 4.5 release-date drift**: GitHub tags (2026-03-22 / 2026-01-28) vs release-notes.md text (14 Mar 2026 / 25 Jan 2026). No impact on API facts.
- **JunimoServer "latest is unstable"**: docs banner + Docker `latest` staleness (2026-01-07) mean the "known-good" recommendation is the preview channel; all headless claims verified against master + docs.
- **Steam-licensed game cannot be downloaded in CI without accounts** — no public-CI path to a real multiplayer run; local/VPS only.
- **1.7 timing**: existence H, content H (Clint/Sandy, new farm, social); **date = unknown**, no signal for 1H2027 either way.
- **junimoserver.com Community/Discord content** not reviewed (not public-indexed reliably).

## 6. Implications for Living Roots

**D1 — real multiplayer convergence (T109-class)**
1. Best available harness is JunimoServer's (server + client containers + JunimoTestClient + REST diagnostics). It is proven for *its own* mod and would give Living Roots: scripted farmhand join, server-side state snapshots (`/diagnostics/state` for soil-health tiles via mod readout), screenshots, and a verdict pipeline very close to `evidence-bundle` schema. Adopting it means: own Steam account/game copy, a VPS (~2 GB images ×2, cold start ~28–42 s), pinned preview tag (v1.5.0-preview.x), a small adapter mod exposing state over the REST surface, and acceptance that this is **manual/VPS verification, not PR CI**. Budget as Gate-3+/release-gate evidence, like `verify-scenario.sh` today.
2. Fallback without new infra: two local SMAPI instances (host + farmhand) both under Xvfb on one host — same X11 pattern, LAN/localhost join. No auth broker needed; simplest honest D1 evidence. JunimoTestClient can even be built standalone (it's just a SMAPI mod, net6.0/ModBuildConfig 4.4.0) — reuse its client-automation HTTP API for the farmhand side.
3. SMAPIDedicatedServerMod/ALOS routes are dead ends (stale or no automation API); do not block D1 on them.
4. FR-009/A8 sync is host-authoritative and already unit-tested; the missing piece is **live convergence under real network timing** — exactly what JunimoServer's harness (or the two-Xvfb fallback) exercises.
5. Honesty bar unchanged: JunimoServer presence does not retroactively make host-only `machine-pass` a multiplayer pass; D1 needs the two-peer run.

**CI / Gate 3**
1. CI can build + unit-test with `StardewModders/mod-reference-assemblies` (Pathoschild's own pattern) — Living Roots already has GamePath assemblies locally; replicating in CI would unblock `dotnet test` without a proprietary game. (Note: repo currently treats GamePath as a hard prerequisite; CI parity is feasible.)
2. No major SMAPI mod launches the game in CI; Living Roots' headless Xvfb runner is **above ecosystem norm** and should stay the Gate-3 mechanism.
3. Full E2E-in-PR is not viable publicly (Steam license + VPS + secrets). Keep E2E manual/dispatched like JunimoServer (`workflow_dispatch`, maintainer-gated).
4. Watch 1.7: any 1.7 preview will shift SMAPI (4.6?), game assemblies, Harmony patch targets — pin `GamePath` references and re-verify HarvestPatches after any update; Living Roots' `CheckForOverflowUnderflow` + strict warnings make this cheap to detect (build failures), but Harmony-target drift needs Gate-3 re-run.
5. SMAPI 4.5 specifics to keep: ITranslationHelper/GMCM developer-mode workflow (no "SMAPI for developers"), `Helper.Multiplayer` mod IDs filter for FR-009, `IGameLoopEvents.DayStarted` for host broadcast, and **0Harmony 2.2.2** semantics for the harvest seam — do not adopt NuGet Lib.Harmony 3.x RCs.
6. Mod-distribution implications: Living Roots on a headless/dedicated host is legitimate (host-only logic, no UI/input deps in core paths); if a farmhand-side UI feature is added later, it must ship as a **client-only companion** — server can't render it (per JunimoServer mod-support rules).

**Bottom line**: the D1 blocker is tooling access, not API knowledge — JunimoServer (or two local Xvfb instances + JunimoTestClient) is the credible path to real-multiplayer evidence in late 2026; official 1.7 and an official dedicated server will not arrive to rescue or redefine this.

---
*Method: high-trust sources fetched live (GitHub API/raw, NuGet, official docs, wikis where reachable, release notes) on 2026-11-09; blocked sources recorded verbatim. No file outside this research doc was modified.*
