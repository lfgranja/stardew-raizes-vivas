# Implementation Plan: 018 — Machine-Pass Full Coverage (research-driven)

**Branch**: `018-machine-pass-full-coverage` (planned) | **Date**: 2026-11-07
**Research**: [`research.md`](research.md) (authoritative for this plan)
**Input**: `specs/017-joja-agrochemical-tradeoff/` (current best-practice reference for scenario authoring, verification commands, claim honesty), `specs/010-headless-verification-runner/` (runner base), `specs/016-gate3-runner-hardening/` (hardening).

## Summary

Close the gap between "host-only machine-pass for the measured path of three features" and
"host machine-pass for every applicable path of every feature that ships runnable code", while
honestly recording every path that is structurally incapable of being `machine-pass`
(farmhand transport, SC-004 comprehension, render legibility, balance) as `partial`/`blocked`
with reasons — never fabricating a run. Work is dominated by **scenario authoring** and, for
mulch, **new verification commands**; no production gameplay behavior changes are required
except the mulch verification seam. All new evidence is append-only and classed honestly.

## Technical Context

- **Language/Version**: C# (latest) on .NET 6 (unchanged).
- **Primary Dependencies**: SMAPI, `Pathoschild.Stardew.ModBuildConfig`, xUnit + Moq + FsCheck (unchanged).
- **Storage**: Stardew save/mod data via existing seams (`ISoilHealthService.SaveData/LoadData`, `IModDataService`); no new persistence.
- **Testing**: `dotnet test` (xUnit), Moq, FsCheck; Gate 3 via `scripts/verify-scenario.sh` (016-hardened); offline bundle check; claims validator `scripts/tests/test-017-claims.sh`.
- **Target Platform**: Stardew Valley 1.6 + SMAPI (Linux dev; Windows/macOS supported by game).
- **Project Type**: SMAPI mod (in place).
- **Performance Goals**: negligible — scenario authoring only; new verification commands mirror the agrochemical seam (O(1) per apply).
- **Constraints**: DDD layering; `ModEntry.cs` sole composition root; host-authoritative farm state (Constitution VI); no Harmony fertilizer hook; no `Data/Shops`; no second source of truth; `ModConstants` for tunables; i18n parity for any new player-facing strings; Gate 3 honesty/provenance rules (append-only evidence, no retroactive promotion, no fabricated runs); farmhand runs capped at `partial` by runner design.

## Constitution Check

| Principle | Status | Notes |
|---|---|---|
| I. DDD (NON-NEGOTIABLE) | PASS (planned) | New verification commands follow the Phase-8 pattern: contract in `Domain/Interfaces`, impl in `Services`, wiring in `ModEntry.cs`; scenario authoring touches no domain logic. |
| II. Security-First Data Handling | PASS | `lr_give_mulch`/`lr_apply_mulch` must reuse `SanitizePathSegments`/bounds validation; soil values clamped 0–100 via existing semantics; no new user-influenced paths. |
| III. Async-Only Concurrency | PASS | Reuse existing synchronous snapshot/lock patterns; no blocking async. |
| IV. TDD | PASS (planned) | New verification commands get unit tests (mocked seams); scenario thresholds are the E2E assertion; mutation-check each new scenario assertion once (game-verification skill rule 5). |
| V. Simplicity / YAGNI | PASS with explicit scope | Mulch seam is the only new code; everything else is scenario authoring. No farmhand transport harness (infeasible, documented blocked). |
| VI. Multiplayer First-Class | PASS with explicit boundary | Farmhand paths exercised only via split-screen guard (partial, never machine-pass); D1 transport recorded blocked with reason; no second source of truth. |

**Gate result: PASS (planned).** No constitution violations; the farmhand `partial` cap is a runner design constraint, not a violation.

## Project Structure

### Documentation (this feature)

```
specs/018-machine-pass-full-coverage/
├── research.md        # this research (authoritative)
├── plan.md            # this file
├── tasks.md           # sequenced waves (Wave 0..5)
└── quickstart.md      # commands per scenario
```

### Source Code (repository root) — only Wave 0 + Wave 4 touch code

```
LivingRoots/
├── Verification/Queue/QueueCommandParser.cs   # + lr_give_mulch, lr_apply_mulch (arity/validate)
├── Verification/ModEntry.cs                  # + GrantMulch, ApplyMulch handlers
├── Domain/Interfaces/ILivingRootsModApi.cs   # + ApplyMulch doc (host-only)
├── Services/LivingRootsModApi.cs             # + ApplyMulch impl
└── ModEntry.cs                              # + mulch wiring (composition root only)

specs/<feature>/scenarios/
├── <feature>-<path>.scenario.json            # new host-path scenarios
└── macros/
    ├── agrochemical-apply.txt                # existing
    ├── split-screen.txt                      # NEW (W0.1) — farmhand guard
    └── place-bin.txt                         # NEW (W0.2) — compost fixture
```

No new projects/solutions; no Harmony; no `Data/Shops`.

## Waves (summary; full task list in tasks.md)

- **Wave 0 (infra, 1–2 days):** create missing macros (`split-screen.txt`, `place-bin.txt`) and test `debug split` headless; add `lr_give_mulch`/`lr_apply_mulch` commands + API seam + wiring; fix `modVersion` declarations (0.4.0 → 0.1.0) or document declared-not-observed.
- **Wave 1 (017 paths, ~1 day):** unknown-baseline, clamped, refused-at-minimum, conflict, farmhand-guard scenarios + runs.
- **Wave 2 (005 + 014, ~1.5 days):** run 005 (currently partial), add poor/moderate variants; 014 unknown + measured-zero harvest scenarios; 014 farmhand-guard (if W0.1 succeeds).
- **Wave 3 (004, ~1 day):** moderate/healthy compost scenarios; 004 farmhand-guard.
- **Wave 4 (013 mulch, ~2–3 days):** mulch commands + scenario + farmhand-guard.
- **Wave 5 (evidence + honesty, ~1 day):** claim rows, validate/quickstart addenda, honesty review, smoke suite, macros fix, modVersion fix.

## Success Criteria (measurable)

- **SC-MP-1**: Every feature with a host-runnable scenario (004, 005, 014, 017) has at least one `host-only-machine-pass` (honesty R) recorded against the post-Wave-0 tree, OR an explicitly recorded `planned-run-not-executed`/`infeasible`/`blocked` claim with reason (FR-015 schema).
- **SC-MP-2**: Every host path that is exercisable with existing or Wave-0-added commands has a passing scenario with 3 agreeing signals and offline bundle verifier exit 0.
- **SC-MP-3**: Every farmhand path is exercised as a split-screen guard scenario and recorded as `partial` (never `machine-pass`), with the honesty cap disclosed in claim rows.
- **SC-MP-4**: 013 mulch has at least one host `machine-pass` (decay exemption path) OR a recorded infeasibility with reason.
- **SC-MP-5**: No claim is promoted above its evidence class; D1/human-verified/SC-004 remain `pending`/`blocked` until real evidence; prior bundles remain retained (append-only provenance).
- **SC-MP-6**: Every new scenario assertion is mutation-checked once (game-verification skill rule 5) and recorded.
- **SC-MP-7**: Determinism: each new scenario runs twice on the same seed and diffs the event log before its first promotion.
- **SC-MP-8**: No fabrication: no invented scenario, log, bundle, or pass; infeasible runs recorded with reason and capability missing.

## Complexity Tracking

No constitution violations. The farmhand `partial` cap and the D1/SC-004 blocked statuses are deliberate honesty boundaries (Principle VI + FR-015), not violations. The mulch verification seam is the only new code; everything else is scenario authoring and documentation. Complexity is justified by the explicit "100% of applicable paths" goal.<|end|><|start|>startup<|channel|>analysis<|message|>Plan written. Now commit it.<|end|><|start|>start of reasoning<|channel|>analysis <|constrain|>xml<|message|><rfl:parameter name="command">
git add specs/018-machine-pass-full-coverage/plan.md && git diff --cached --stat && git commit -m "docs(plan): 018 machine-pass full coverage implementation plan — specs/018-machine-pass-full-coverage/plan.md — waves 0-5, constitution check, project structure, success criteria SC-MP-1..8, complexity tracking; no constitution violations" -- specs/018-machine-pass-full-coverage/plan.md