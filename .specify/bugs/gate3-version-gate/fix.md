# Bug Fix: version gate derives observed versions from the game directory

- **Slug**: gate3-version-gate
- **Fixed**: 2026-11-04
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

`version_gate_classify` now accepts the caller contract actually used by `preflight.sh` and `verify-scenario.sh` — a game installation directory plus the mod manifest — and derives the observed game/SMAPI versions from the installation's assemblies. The mod minimums are read from the manifest, and the `*.dll` resolver now extracts real .NET product versions (`1.6.15`, `4.5.2`) instead of requiring a whole-line match. This removes the false `HARD-FAIL` (exit 19) that blocked Gate 3 before launch.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `scripts/lib/version-gate.sh` | modified | Directory argument derives game version from `Stardew Valley.dll` and SMAPI version from `StardewModdingAPI.dll`; manifest arg2 supplies `MinimumGameVersion`/`MinimumApiVersion`; `*.dll` extraction uses `grep -oE` + `sort -V` and filters the CLR's `4.0.30319`; manifest-derived minimums block fixed. |
| `scripts/tests/test-version-gate.sh` | modified | Added hermetic directory-form tests: compatible install → `INFO`/rc 0; older game → rc 19; older SMAPI → rc 19; `preflight_version_gate` wrapper → rc 0. |

## Diff Highlights

```bash
# Directory handling in version_gate_classify
if [[ -d "$game_ver" ]]; then
    local game_dir="$game_ver"
    game_ver="$game_dir/Stardew Valley.dll"
    smapi_ver="$game_dir/StardewModdingAPI.dll"
    if [[ -f "${2:-}" && "${2:-}" == *.json ]]; then
        mod_min_game="$(python3 - "${2}" ... MinimumGameVersion ...)"
        mod_min_api="$(python3 - "${2}" ... MinimumApiVersion ...)"
    fi
fi
```

```bash
# Real .NET assemblies expose 4-part / +hash versions; extract the product triple.
*.dll)
    local base; base="$(basename "$v")"
    if [[ "$base" == *"Stardew Valley"* ]]; then
        strings "$v" 2>/dev/null | grep -oE '1\.[0-9]+\.[0-9]+' | sort -Vu | tail -1
    elif [[ "$base" == *"StardewModdingAPI"* ]]; then
        strings "$v" 2>/dev/null | grep -oE '4\.[0-9]+\.[0-9]+' | grep -v '^4\.0\.30319$' | sort -Vu | tail -1
    fi
    ;;
```

## Tests Added or Updated

- `scripts/tests/test-version-gate.sh` — directory form with a compatible install (1.6.15 / 4.5.2, manifest minimums 1.6.14 / 4.4.0) returns `INFO` and rc 0.
- `scripts/tests/test-version-gate.sh` — older game (1.5.0) still hard-fails with 19.
- `scripts/tests/test-version-gate.sh` — older SMAPI (4.3.0) still hard-fails with 19.
- `scripts/tests/test-version-gate.sh` — `preflight_version_gate <game_dir> <manifest>` returns 0 for the compatible install.
- Existing string-argument test and source-shape assertions (`unofficial`, `MaximumGameVersion`) kept passing.

## Local Verification

- `SMOKE_SKIP_INTEGRATION=1 bash scripts/tests/test-version-gate.sh` → 10/10 passed, exit 0.
- `bash -n scripts/lib/version-gate.sh scripts/tests/test-version-gate.sh` → syntax OK.
- `SMOKE_SKIP_INTEGRATION=1 bash scripts/tests/run-all.sh test-version-gate.sh test-preflight.sh test-game-path.sh test-verify-cli.sh` → `test-version-gate.sh`, `test-game-path.sh`, `test-verify-cli.sh` passed; `test-preflight.sh` failed with pre-existing exit-13 failures (see Deviations).
- Real-install check: `version_gate_classify "/home/luis/.steam/steam/steamapps/common/Stardew Valley" ".../Mods/LivingRoots/manifest.json"` → `INFO`, rc 0 (was `HARD-FAIL`, rc 19).
- Real-install check: `preflight_version_gate "<game>" "<manifest>"` → rc 0.
- End-to-end: `verify-scenario.sh --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json --game-path "<game>" --skip-build` → exit 0 (no `blocked.json` with `version-mismatch` in the new bundle).
- `git diff --check` → clean.

## Deviations from Assessment

- The assessment proposed adding a preflight-level test that runs the full `preflight_run` and asserts exit 10 (display phase). The full preflight requires a synthetic game tree plus Xvfb stubs; that path is already covered by the pre-existing `test-preflight.sh` suite (which is red on this host for unrelated, pre-existing reasons — 47 failures at HEAD vs 45 after the fix, all exit 13). Instead, the fix adds a focused `preflight_version_gate` wrapper test to `test-version-gate.sh`, which pins the exact caller contract that produced the bug without duplicating the heavy preflight fixture.
- The assessment listed `LivingRoots/manifest.json` and `gamepath.lock.json` as optional. They were left unchanged: neither is required to unblock the version gate, and raising `MinimumGameVersion` / adding an `UpdateKey` is a deliberate compatibility/release decision. They remain follow-ups.
- The `test-preflight.sh` failures were confirmed pre-existing by reverting `version-gate.sh` to HEAD and re-running the suite (47 `not ok` at HEAD). They are not caused by this fix.

## Follow-ups

- Decide whether to raise `LivingRoots/manifest.json` `MinimumGameVersion` from `1.6.14` to `1.6.15` and add an `UpdateKey` (e.g. `GitHub:lfgranja/stardew-raizes-vivas`).
- Decide whether Gate 3 runs should use `--no-acquire` against the installed game, or whether `specs/010-headless-verification-runner/gamepath.lock.json` should be populated with real builddeps values (its placeholder `failClosed: true` is a separate potential exit-19 blocker).
- Gate 3 itself remains pending: the version-gate blocker is fixed, but the in-game 3-signal evidence and human attestation (T148) still need to be produced.
- Investigate the pre-existing `test-preflight.sh` exit-13 failures separately (not in scope for this bug).
