# Idea Research: Stabilize overlay cache invalidation under interleaved events

- **Slug**: overlay-cache-interleaving
- **Created**: 2026-11-18
- **Evidence confidence (overall)**: high

## Users & Demand

- The direct affected party is the maintainer/CI pipeline: `Build and Test` is a **required** status check on `dev` (verified live via `gh api repos/lfgranja/stardew-raizes-vivas/branches/dev/protection`: contexts `sdd-gate`, `Build and Test`, `strict: true`, 0 reviews). A red mainline blocks every PR merge. [source: live `gh api` | confidence: high]
- No end-user gameplay complaint is attached to this idea; it is a CI-stability fix, not a player-facing feature. [ASSUMPTION: no support ticket was found in `.specify/bugs/` for this property | confidence: medium]

## Prior Art

- `.specify/memory/STATE.md` (origin/dev) already records this property as flaky: "one flaky FsCheck property seed (isolated reruns 1 fail / 2 pass; no C# touched)". [source: `git show origin/dev:.specify/memory/STATE.md` | confidence: high]
- PR 797 made secret-dependent CI failures explicit but did not address this property; PR 801 fixed 10 stale quality-gate pytest expectations and deferred one item to a human Gate 3 session. [source: ledger + `gh pr checks 797/801` | confidence: high]
- Spec 010 defines the headless verification runner and Gate 3 evidence contract; wall-clock frame budget belongs to that layer, not to unit properties. [source: `specs/010-headless-verification-runner/` | confidence: high]

## Market & Context

- Cost of doing nothing: every push to `dev` has a ~80% chance (4/5 local runs) of failing the required check, forcing manual reruns and blocking autonomous delivery. [source: local 5-run reproduction | confidence: high]
- No market/external dimension applies; this is internal CI determinism. [ASSUMPTION | confidence: high]

## Data & Constraints

- Live CI run `44309131539` (`dev` push after PR 800): 2003 total / 1998 passed / **1 failed** / 4 skipped; sole failure `RenderOverlays_ForAnyDisableEnableInterleaving_RebuildsOnFirstFrameAfterReEnableWithinBudget`, FsCheck StdGen `(1549256609,297700084)`. [source: `gh run view ... --log` | confidence: high]
- Local 5-run reproduction: 4 failures with distinct StdGens and a concrete 23-event counterexample `[2;1;0;2;-2;2;-2;2;-1;-2;1;-2;-2;-1;0;-1;0;0;1;0;-1;1;2]`. [source: `dotnet test` loop | confidence: high]
- **Instrumented diagnosis**: temporary `Diag(...)` labels on all seven `&=` assertions; the thrown label was `DIAG:budget` — `stopwatch.Elapsed.TotalMilliseconds <= ModConstants.MaxRenderTimeMs` (16.67 ms). All cache-consistency assertions passed. Instrumentation was restored from a backup; no diagnostic code remains. [source: instrumented test run | confidence: high]
- The `VisualizationService`/`OverlayRenderer` cache key already includes viewport, map bounds, visible tile-health content (tolerance), palette generation, opacity, pattern toggle, degradation mode, unknown color, location name and mulch version (`OverlayRenderer.cs:419-434`), plus `_overlaysWereDisabled` re-enable invalidation (`VisualizationService.cs:190-215`). No cache-key defect was found. [source: code reading | confidence: high]

## Evidence Against the Idea

- Strongest counter-hypothesis: an intermittent **production** invalidation regression could also present as a flaky property. Refuted by instrumentation: every consistency assertion passed; only the wall-clock budget assertion failed.
- Second counter-hypothesis: removing a budget check weakens FR-009. Mitigated by replacing it with a **deterministic work bound** (at most one color-service consultation per visible tile per frame); wall-clock budget remains Gate 3/verification-runner evidence, where a real game loop runs.

## Gaps & Open Questions

- [NEEDS CLARIFICATION: does the maintainer want a separate wall-clock benchmark retained outside the property? Not required for this CI fix; recorded as a debt.]

## Sources

- live GitHub Actions log (host: github.com, policy: allowlisted via `gh` CLI) — run `44309131539`.
- live GitHub branch protection API (host: api.github.com, policy: allowlisted via `gh` CLI).
- local repository files and executed `dotnet test` runs (no URL fetched).
