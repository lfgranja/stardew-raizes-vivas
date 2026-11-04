# Decision: Do not adopt agent-desktop for the headless verification runner

- **Slug**: agent-desktop-usefulness
- **Decided**: 2026-11-04
- **Verdict**: kill
- **Artifacts reviewed**: intake.md, research.md, problem.md, concept.md

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Problem validity | strong | Gate 3 is genuinely blocked: the real-host run produced `partial` with `signalsComplete=false`, and specs 014/015 are gated behind it. The problem is real and costs the maintainer manual verification time. |
| Evidence strength | strong | The verdict rests on the full SMAPI log (title screen → `getLoadEnumerator('farm-year1-day5')` → `File does not exist` → return to title), the save directory listing, agent-desktop's own Linux skill page, and a local host inventory. Not assumption-driven. |
| Value vs. inaction | weak | For this idea specifically: agent-desktop cannot load the save, cannot produce the three signals, and cannot raise the claim above `machine-pass`. Its only plausible value is a failure screenshot or window listing, which X11 tools already provide. |
| Feasibility / appetite | weak | Working integration is a `large` appetite (AT-SPI bus, window manager, Ubuntu/GNOME target, native binary, Node toolchain, MCP server) against a `small` actual fix. The host is Fedora/KDE, not the measured platform. |
| Strategic fit | weak | Contradicts spec 010 FR-006 (input simulation excluded as fragile/non-deterministic), the reproducibility goal, and the runner's minimal trust boundary. `NO_AT_BRIDGE=1` is deliberate. |
| Risk posture | weak | Adds a days-old 1.0.x native dependency with clipboard/screenshot/window access to a verification gate that should change rarely, while changing the environment under test. |

## Verdict & Rationale

**Kill.** The idea solves a problem that is not the blocker. agent-desktop's value is the OS accessibility tree; a canvas-rendered SDL2/MonoGame game exposes none, so every semantic command degrades to the blind pointer/keyboard injection that spec 010 explicitly rejected. Meanwhile the actual T137 failure is a save-load bug — the isolated save folder is `farm-year1-day5` while the inner save file is `Agroeco_421179919` — which no desktop-automation tool can repair. Option A (repair the existing runner: align the save names, capture the full SMAPI log, re-run T137) is cheaper, keeps determinism, and is already the specified path. Killing this idea is not a judgment on the underlying problem; the problem stays valid and is addressed by completing spec 010, not by a new specification.

Revisit only if a *separate* desktop-automation need appears (installer, mod-manager GUI, Steam/SMAPI flow), where the target actually exposes an accessibility tree.

## If needs-clarification

Not applicable — the verdict is kill.

## Notes carried forward (not part of the idea)

- The full-SMAPI-log gap is a bundle-quality bug independent of agent-desktop; it should be fixed inside spec 010 so future failures are diagnosable.
- The two-line `artifacts/smapi-log.txt` misdiagnosis is recorded in `.specify/assessments/agent-desktop-usefulness/evidence/t137-save-load-excerpt.txt`.
- No code in this repository depends on agent-desktop; nothing needs removing.
