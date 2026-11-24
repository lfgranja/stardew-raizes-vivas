# Concept: Sandboxed, Visual End-to-End Game Test Harness

- **Slug**: sandboxed-game-e2e-harness
- **Created**: 2026-10-31
- **Recommended option**: Option A — do not build a separate sandboxed visual harness now; complete the already-specified headless runner (spec 010) first.

## Options

### Option A — Finish the existing headless runner (spec 010) as-is

- **Sketch**: Use the runner already specified in `specs/010-headless-verification-runner/`: a Linux/Xvfb headless session, milestone saves plus SMAPI macro seeding, three independent signals, threshold evaluation, an evidence bundle with per-file SHA-256, retries/flaky/quarantine handling, and local/CI parity. The game runs with no visible window and no audio device by construction; per-instance `XDG_CONFIG_HOME` and a copied mods folder provide run isolation. No new sandbox, visual oracle, or video pipeline is added.
- **Appetite**: large (months) — but this appetite is already committed to spec 010 and is not new work introduced by this idea.
- **Trade-offs**: Reuses the only implemented substrate (`smoke-validation.sh`), avoids duplicating maintenance surfaces, keeps screenshots as supporting evidence (as spec 010 already decided), and preserves the honesty discipline. It does not, however, provide visual proof artifacts for human review, a desktop-minimized mode, or a stronger security boundary than per-instance directories.
- **Rabbit holes**: Spec 010 itself carries weak feasibility and risk posture (per its decision record); scope creep into "just one more visual check" would re-open the deferred items. The risk is that the runner is never finished because its appetite is large.

### Option B — Incremental visual-evidence add-on to spec 010

- **Sketch**: Keep spec 010 as the engine and add an optional, supporting-only layer: capture a screenshot at defined scenario points (and optionally a short video) into the evidence bundle, with baseline/threshold comparison available for human review but not as the pass/fail oracle. No change to the three-signal rule or to promotion rules.
- **Appetite**: medium (weeks), on top of spec 010.
- **Trade-offs**: Adds a familiar, reviewable artifact (matching web/mobile testing expectations) and may help diagnose rendering regressions. It also adds storage/retention cost, baseline management, and a new flakiness source (animation, timing, renderer differences), and it does not make the verdict more trustworthy because visual evidence stays supporting-only.
- **Rabbit holes**: Baseline drift across game/SMAPI updates; Xvfb depth/pixel-format mismatches producing black captures; deciding which scenario points to capture; retention policy and size growth. This option is only worth its cost if a concrete, recurring "we could not diagnose a rendering regression from the three signals" case is documented.

### Option C — Full sandboxed visual harness (the idea as captured)

- **Sketch**: A stronger sandbox (container/namespace/VM), a minimized and muted desktop window or virtual display, a self-contained or generated test save, automated E2E execution, screenshots and/or video per proof type, saved execution conditions and results, and automatic validation against per-feature specifications.
- **Appetite**: large (months), and largely additional to spec 010 rather than replacing it.
- **Trade-offs**: Maximum isolation and the richest visual proof, and it directly matches the captured idea. It also duplicates most of spec 010, introduces GPU/audio/X11 plumbing and storage costs, risks performance measurements that say more about the sandbox than the game, and has no demonstrated demand beyond the single maintainer. It would also need a licence review for game assets in recorded footage.
- **Rabbit holes**: Containerizing a MonoGame/Steam game; GPU passthrough or software-rendering fidelity; sandbox escape and security review; visual-oracle reliability and baseline management; video storage and retention; keeping two runners in sync.

## Recommendation

Recommend **not proceeding with the idea as a separate sandboxed visual harness** at this time, and instead **completing Option A (spec 010) first**.

Rationale, tied to the problem's goals and metrics:

- The core problem is the absence of trustworthy, reproducible Gate 3 evidence. Spec 010 already targets that problem with three independent signals and an honesty discipline; it is specified but unimplemented. Building a second harness before the first ships would split a single-maintainer effort across two large surfaces and risk neither finishing.
- The research found no evidence that visual evidence improves the verdict: spec 010 explicitly makes screenshots/OCR supporting-only, and visual comparison adds flakiness and storage cost without strengthening the oracle.
- The "sandbox" and "minimized/muted" deltas are largely satisfied by mechanisms spec 010 already uses (Xvfb gives an invisible, audio-less session; per-instance directories give run isolation). A stronger sandbox is only justified by a threat model or isolation need that has not been stated.
- Demand for the broader harness is concentrated in one maintainer, with no external consumer, so the Rule of Three and the project's YAGNI convention argue against a general platform now.

Option B is the only sanctioned future delta, and only conditionally: if, after spec 010 ships, a rendering or UI regression is documented that the three signals could not diagnose, Option B may be proposed as a small/medium add-on. Until then it stays out of scope.

## Out of Scope (for the recommended option)

- No new sandbox (container, namespace, VM, or microVM) and no stronger security boundary than per-instance directories.
- No desktop (real-window) minimized or muted mode.
- No video recording and no new visual oracle or baseline-management system.
- No change to the three-signal rule, the pass/fail verdict, or the promotion rules (partial / machine-pass / verified).
- No new per-feature specification format beyond spec 010's scenario JSON.
- No reuse claim for other mods until a second consumer actually appears.

## Assumptions to Validate

- Spec 010 can be implemented within its stated appetite and will deliver sufficient Gate 3 evidence on its own.
- Visual evidence is not required as an oracle; supporting-only screenshots remain adequate for human diagnosis.
- Per-instance `XDG_CONFIG_HOME` plus a copied mods folder is sufficient isolation for config/mod interference during verification.
- Xvfb's software rendering is acceptable for gameplay verification, because the metrics in scope are behavioral (growth/phase), not representative frame timing.
- The maintainer's appetite for spec 010 remains after this assessment; if it does not, the problem (not this concept) should be revisited.
- If a second mod or consumer appears, the sandbox/visual options should be re-evaluated under the Rule of Three.
