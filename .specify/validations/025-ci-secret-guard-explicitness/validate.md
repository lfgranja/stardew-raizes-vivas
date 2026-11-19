# Validation — Spec 025 (Explicit secret-dependent CI failures)

- **Slug**: 025-ci-secret-guard-explicitness
- **Validated**: 2026-11-18
- **Cycle**: 025 (autonomous 80/20, CI elevation)
- **Validation status**: **aprovado** (no remote claim promoted; [NV] preserved)

## Gate 1 — Static checks & tests (executed in the feature worktree)

- `dotnet format Stardew-LivingRoots.sln --verify-no-changes`: **exit 0**.
- `dotnet restore --locked-mode`: **exit 0**.
- `GamePath=/home/luis/stardew-assemblies dotnet build Stardew-LivingRoots.sln -c Release --no-restore -m:1 --disable-build-servers`: **exit 0**.
- `GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln -c Release --no-build --verbosity minimal`: **Aprovado — 1999 passed / 0 failed / 4 skipped (2003)**. A first run hit one FsCheck property failure (`VisualizationCacheInvalidationInvariantsTests.RenderOverlays_ForAnyDisableEnableInterleaving...`); isolated reruns were 1 fail / 2 pass and the full rerun was green — recorded as a **pre-existing flaky property test**, not a regression from this change (no C# file touched).
- `./scripts/verify-scenario.sh --check-docs`: **exit 0**.
- `./scripts/check-protected-paths.sh`: **exit 0**.
- Baseline `scripts/release-gate.sh` run in the shared worktree (2026-11-18T10:48:36Z): steps 1–4 OK (format, restore, build, tests); step 5 launch-free smoke still in progress at the time of this record. The smoke scripts are unchanged by this feature.

## Gate 2 — Product-level quality bar

- No user-facing behavior, soil-health value, save round-trip, or locale string changed. No new player-facing text → i18n parity unaffected.
- Fail-closed semantics preserved: missing/empty credential produces `::error::` + `exit 1` before first use; `continue-on-error`/`::warning::` best-effort path untouched; tag-only owner-gated `release.yml` excluded.
- No regressions: zero new failures in the full Python quality-gate suite (base at `origin/dev`: 12 failed / 161 passed; this branch: 11 failed / 167 passed — 5 new tests pass, 11 pre-existing failures unchanged, 1 pre-existing failure resolved). The CI `Quality Gate` job does not invoke this pytest suite (it runs the `quality_gate` CLI), so this is recorded as local debt, not a broken authoritative gate.

## Gate 3 — End-to-end verification

- **N/A** — this feature touches only CI configuration, Python contract tests and documentation. No runnable C#, no game state, no asset. Per `VERIFICATION_PROTOCOL.md`, Gate 3 real-SMAPI evidence is not claimed. `machine-pass` is not invoked.

## SDD gates

- R1 decision: **go** (`.specify/assessments/025-ci-secret-guard-explicitness/decision.md`).
- R3 clarification: 0 questions; coverage recorded in `clarify.md`.
- Requirements-quality checklists: `requirements.md` 16/16, `ci-reliability.md` 18/18 (requirements review only — not implementation claims).
- `analyze` (read-only): 0 critical findings; one FR-003 coverage gap fixed at source (`e421db2`) and re-run.
- `converge.md`: append-only traceability recorded (spec 025 < 088 → `analyze.md` not required by target convention).

## Honest boundaries

- [NV] The step-level root cause of the six open dependency-update PRs (#790–#796) was not re-read from remote logs in this cycle. Remote check status was observed live via `gh` on 2026-11-18 but no remote claim is promoted here.
- No tag, release, branch protection, secret, or auto-merge change was made or simulated.

## Veredito

**APROVADO** — local deterministic evidence supports merge; no Gate 3 claim.
