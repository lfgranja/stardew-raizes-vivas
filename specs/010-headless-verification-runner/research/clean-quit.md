# Clean-Quit Research (C-REL-25)

**Captured:** 2026-11-02 (implementation-time, Phase 3 / US1)
**Status:** Empirical items below are marked `UNVERIFIED` until a real-host run
validates them; the spec's prescribed approach (file-mtime stability,
graceful-exit-first, SIGKILL backstop) stands regardless.

## Scope (C-REL-25)

Implementation-time validation owed for three questions:

1. Whether SIGTERM on Mono/XNA invokes SMAPI's `OnExiting` / `Dispose`.
2. The exact log line (if any) signaling save completion.
3. Whether XNA `Game.Exit()` triggers `OnExiting` reliably under Xvfb.

Source spec: `specs/010-headless-verification-runner/spec.md` §C-LCH-02 and
§C-REL-25.

## Findings

### 1. SIGTERM / OnExiting behavior

- **Observation (UNVERIFIED on this host):** SMAPI registers no POSIX signal
  handlers for SIGTERM/SIGINT. `SCore.OnGameExiting` is wired to the
  MonoGame `Game.OnExiting` event, which is only raised when the game loop
  exits normally (e.g. via `Game.Exit()` or window-close). A bare SIGTERM
  therefore does **not** guarantee `OnExiting`/`Dispose` run, and Stardew does
  not auto-save on process death.
- **Citation:** Pathoschild/SMAPI `SCore.OnGameExiting`, `LogFileManager.AutoFlush`
  (SMAPI source, `StardewModdingAPI/Framework/SCore.cs`); MonoGame
  `Microsoft.Xna.Framework.Game.OnExiting(Object, EventArgs)` (MonoGame.Framework
  XML doc, shipped with the game's managed assemblies).
- **Implication:** The runner must NOT rely on SIGTERM alone. It must drive a
  graceful exit (custom mod command calling `Game1.game1.Exit()`, or X11
  `WM_DELETE_WINDOW`) and only then SIGKILL as a backstop at 90–120 s.
- **Runner code:** `scripts/lib/quit.sh` implements save-mode request →
  `debug sleep` end-of-day progression → mtime/size stability ≥ 30 s → graceful
  exit → SIGKILL backstop; sets `cleanQuit` consumed by `scripts/lib/cleanup.sh`.

### 2. Save-complete signal

- **Observation (UNVERIFIED):** No documented SMAPI/Stardew log line signals
  save completion. Empirical log inspection is owed; until then the runner
  uses **file-mtime/size stability for ≥ 30 s** as the proxy.
- **Citation:** Stardew Valley Wiki console commands (`debug sleep`); SMAPI
  log format (`SMAPI-latest.txt`, `AutoFlush=true` per `LogFileManager`).
- **Implication:** `scripts/lib/signals.sh` records `cleanQuit: false` (and
  verdict `partial`, never `pass`) when only the SIGKILL backstop succeeds
  (FR-019). `scripts/lib/quit.sh` enforces the ≥ 30 s stability window.

### 3. Game.Exit under Xvfb

- **Observation (UNVERIFIED on this host):** `Game.Exit()` is expected to
  raise `OnExiting` and run `Dispose` under Xvfb because the X11 display
  remains available; however Xvfb teardown races and headless GL (software
  rendering) can delay or skip the exit event. Empirical validation required
  before promotion beyond Phase 1.
- **Citation:** MonoGame `Game.Exit()` / `OnExiting` contract; Xvfb(1) man
  page (display lifecycle); JunimoServer `startapp.sh` / operations docs
  (marker-file + graceful-shutdown pattern referenced in spec.md §C-LCH-02).
- **Implication:** The runner keeps `SMAPI_NO_TERMINAL=1`, software GL and
  SDL/audio env vars (see `scripts/lib/launch.sh`) and uses per-instance
  displays (`scripts/lib/display.sh` `-displayfd`) to avoid cross-instance
  interference. Orphan displays are reported, not force-unlinked (owned-token
  rule in `scripts/lib/display.sh`).

## Runner wiring

- `scripts/lib/quit.sh` — save-mode request, `debug sleep` progression,
  mtime/size stability ≥ 30 s, graceful exit, SIGKILL backstop 90–120 s,
  `cleanQuit` flag consumed by cleanup.
- `scripts/lib/cleanup.sh` — trap/finally, process-group teardown, exit-code
  preservation; treats SIGKILL-only as `cleanup_failed`-adjacent and never
  upgrades the verdict to `pass`.
- `scripts/lib/signals.sh` — emits `artifacts/anomaly-report.json` and sets
  `partial` on any divergence; `cleanQuit: false` ⇒ not `pass`.

## Verification status

- Real-host validation of items 1–3 is **owed** (C-REL-25) and must be
  recorded in `specs/010-headless-verification-runner/verification/` before
  promotion beyond Phase 1. This document captures the research baseline and
  citations; it does not claim empirical verification.
