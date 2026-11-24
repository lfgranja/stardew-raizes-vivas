# Concept: Getting Gate 3 to machine-pass

- **Slug**: agent-desktop-usefulness
- **Created**: 2026-11-04
- **Recommended option**: Option A — Repair the existing runner

## Options

### Option A — Repair the existing runner (no agent-desktop)

- **Sketch**: Keep the current architecture — SMAPI launches under Xvfb, `LivingRoots.Verification` loads the milestone save and consumes a file queue, the runner captures three signals. Fix the two things the research proved are broken: make the isolated save's folder name and inner save-file name agree so `SaveGame.Load` succeeds, and put the full SMAPI log in the evidence bundle instead of the three-pattern grep excerpt. Then re-run T137 and continue the T138–T146 sequence.
- **Appetite**: small (days)
- **Trade-offs**: Wins: it targets the actual failure, keeps determinism and the no-input-simulation contract, adds no dependency, and unblocks the success metric that matters (`machine-pass` on a real host). Sacrifices: it delivers no desktop-automation capability at all, so any future GUI-driven need is still unmet. Risk: the save rename is an inferred root cause; if a second blocker sits behind it, the fix is iterative.
- **Rabbit holes**: the save-copy path (`save_prepare` / `save_seed`) may have other assumptions about save naming; widening the log excerpt may expose noise that the signal thresholds were tuned against; the screen-space render exception spam at the title screen may need separate triage.

### Option B — Adopt agent-desktop as an optional GUI-driver tier

- **Sketch**: Add agent-desktop alongside the queue, initially as a diagnostic aid (window listing, failure screenshot, focus) and later as a fallback driver when the queue path cannot reach a screen. Requires a new environment tier: AT-SPI bus, a window manager, and removing `NO_AT_BRIDGE=1` from the harness; the measured target is Ubuntu 24.04+/GNOME 46, not this Fedora/KDE host.
- **Appetite**: large (months)
- **Trade-offs**: Wins: a general desktop-automation capability and better failure artifacts if it works. Sacrifices: the accessibility tree — the tool's entire value — is absent for a canvas-rendered SDL2/MonoGame game, so the semantic commands degrade to blind pointer/keyboard injection, which spec 010 FR-006 explicitly excluded. It also contradicts the reproducibility goal by changing the environment under test and adding a native binary, an MCP server, and a Node toolchain to the trust boundary. It still cannot produce `machine-pass` alone, because the three signals come from the mod, not from the GUI.
- **Rabbit holes**: AT-SPI registration of an SDL window (probably impossible without game-side work); window-manager-dependent activation (`activation_not_granted`); Wayland vs X11 screenshots; clipboard and screenshot privacy surface; pinning a 1.0.x dependency that is days old.

### Option C — Do nothing; keep manual Gate 3

- **Sketch**: Stop work on the runner, verify by playing the game by hand, and revisit GUI automation when a concrete need appears.
- **Appetite**: none
- **Trade-offs**: Wins: zero engineering cost now. Sacrifices: the machine-pass goal, the specs 014/015 unblock, and the manual-verification tax the runner exists to remove. The already-built runner code stays as dead weight and its bundles keep misleading future readers.
- **Rabbit holes**: none — but the misdiagnosis risk (a two-line excerpt read as a stall) persists.

## Recommendation

**Option A.** The research shows the question "is agent-desktop useful here?" resolves to no for spec 010: its value is the accessibility tree, a canvas-rendered SDL2 game exposes none, and the T137 failure is a save-load bug that no desktop-automation tool can fix. Option A is the only option that moves `signalsComplete`/`signalsAgree`/`thresholdsOk` off `false`, and it does so without weakening determinism, the honesty ladder, or the trust boundary. Option B should be revisited only if a separate GUI-automation need appears; Option C should be rejected while specs 014/015 are gated on machine-pass.

## Out of Scope (for the recommended option)

- Any agent-desktop integration, MCP server, or C FFI usage.
- AT-SPI, window managers, or accessibility-tree work in the runner.
- Screenshot/OCR as a correctness oracle (screenshots remain supporting evidence only).
- Changing the honesty ladder: `verified` stays a human attestation (T148).
- Implementing specs 014/015 — those follow after machine-pass, not as part of this fix.
- General desktop computer use (installers, mod managers, Steam UI).

## Assumptions to Validate

- The save folder / inner-file name mismatch is the root cause of `DebugOutput: File does not exist`; one controlled run with matching names confirms or refutes it.
- No second blocker sits behind the save fix; if the scenario still fails, the next diagnosis starts from the full log, not the excerpt.
- The three-pattern log excerpt can be widened to the full log without breaking existing threshold checks or bundle size limits.
- The screen-space render exceptions at the title screen are unrelated noise, not a cause of the load failure.
- No separate desktop-automation need exists outside spec 010 (open question carried from `problem.md`).
