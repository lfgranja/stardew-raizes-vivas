# Host-Only Gate 3 — Real Run Record — Spec 017 Phase 10 closure (Joja / Agrochemical MVP)

**Claim class:** `host-only-machine-pass`
**Status:** `complete` (host-only machine evidence only)
**Recorded:** 2026-11-07 · run timestamp 2026-11-07T04:03:17Z
**Runner:** 016-hardened `scripts/verify-scenario.sh` (spec 010 base) against the local Stardew 1.6.x / SMAPI install; scenario `scenarios/agrochemical-tradeoff.scenario.json` (schemaVersion 3), macro `scenarios/macros/agrochemical-apply.txt`.

> **This is not human verification, not D1, and not SC-004.** It is machine evidence that a
> real isolated host session applied the 017 MVP once on an eligible tile and produced three
> agreeing signals — on the **post-Phase-10 tree** (commit `c9ff596db6268550b2559fccec894eb8cfcd8d7c`).
> It does not prove balance, in-game rendering, manual playability, comprehension, or the C2
> unknown-baseline path (covered by automated tests only).

## Result

| Measure | Value |
|---|---|
| Exit code | 0 |
| Verdict | `pass` (`actualVerdict` = `expectedVerdict` = `pass`) |
| Signals complete | `true` (dump vs save) · signals agree: `true` |
| Thresholds | 3/3 `passed` |
| `farmhandBreach` | `false` (host/main-player only) |
| `evaluatorMissing` | `false` |
| `test.md` status | promoted to `machine-pass` by the runner (honesty `R`, not `E`) |
| Offline verifier | `./scripts/verify-scenario.sh --verify-bundle evidence-bundle-run-20261107T040235Z-589a` → exit 0 |

## Run

```bash
./scripts/verify-scenario.sh \
  --scenario specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-tradeoff.scenario.json \
  --no-acquire --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" \
  --skip-build
```

Pre-state: solution rebuilt in Release before the run (both mod projects redeployed: `LivingRoots`
and `LivingRoots.Verification` into the Steam `Mods/` folder; the Verification mod's previously
deployed copy was from 2026-11-06 and was refreshed before this run — the stale-deploy failure
mode recorded in the 2026-11-06 run record was avoided). Pre-phase-10 verification record of the
same scenario: `host-only-gate3-run.md` (commit e5882c1, retained, not rewritten).

## Observed signals

- `artifacts/soil-health-before-after.json`: Farm `60,15` `before 50` → `after 40`, `delta -10`
  (provisional `P=10`; one host application via `lr_give_agrochemical 1` +
  `lr_apply_agrochemical 60 15 Farm`).
- `artifacts/crop-phase-dump.json`: verification-only fixture crop on `Farm 60,15`;
  `phaseDays` unchanged (`cropPhaseDelta` factor `1.0`); host `isMainPlayer: true`.
- `artifacts/smapi-log.txt`: host/main-player eligibility line present as required by the scenario's
  `smapiLog` threshold.

## Honesty boundaries

- Host-only machine evidence only; **not** `human-verified`, **not** `d1-farmhand-convergence`,
  **not** SC-004 (comprehension protocol `sc004-v1` still never run).
- Coverage: the **measured** 50 → 40 path only. The C2 unknown-baseline path remains
  automated-coverage only.
- `P=10` remains provisional/tunable; no final balance claim.
- This pass supersedes nothing: the 2026-11-06 bundle is retained; this record attests
  `c9ff596` only; D1, human-verified, and SC-004 remain `pending`.
- Note: the phase-10 verification tree contains the `TrySetSoilHealth` commit seam, the
  `RefusedCommitFailed` commit-drop class, the T099 overlong-name guard, the refusal-copy rewrites,
  and the zero-stack inventory-slot clear — this run exercises the measured host path end-to-end
  with all of those in place; it does not exercise the commit-drop/refusal paths (unit-covered).

## Provenance

One untracked scratch bundle exists from earlier attempts (kept untracked, not cited as passing
evidence). This bundle is the sole promoted run for the post-Phase-10 tree. Raw log capture is
via the bundle's `artifacts/smapi-log.txt`; no `/tmp` path is cited as provenance.
