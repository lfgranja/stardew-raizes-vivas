# Bug Assessment: Gate 3 aborts at version gate (exit 19) before launching the game

- **Slug**: gate3-version-gate
- **Created**: 2026-11-03
- **Source**: pasted text (Gate 3 result report, Portuguese)
- **Verdict**: valid
- **Severity**: high

## Report (verbatim or summarized)

Gate 3 could not be completed automatically. `verify-scenario.sh` aborted with exit code 19 (`EXIT_ERRORED`) at the **version gate**, before launching the game. No stdout/stderr was captured (preflight is silenced with `2>/dev/null`), but exit 19 signals a version-classification failure (FR-024/FR-025). As a result, Gate 3 never ran — no soil-health delta, crop-phase delta, or SMAPI log was captured, and `machine-pass` was not written. The report's suggested actions were to (1) verify `compat-matrix.json` and `gamepath.lock.json` against the real install at `/home/luis/.steam/.../Stardew Valley`, (2) align `manifest.json` (`MinimumGameVersion`/`UpdateKeys`) with SMAPI 4.x + SV 1.6.15, (3) re-run `verify-scenario.sh`, and (4) record human attestation (T148).

## Symptom

Running `verify-scenario.sh --scenario <...> --game-path "<Stardew Valley>" --skip-build` against the real, installed game (SMAPI 4.5.2 / Stardew Valley 1.6.15) exits 19 during preflight's version gate, before display/acquisition/launch. Reproduced locally: `preflight_version_gate "$GAME_PATH" "$GAME_PATH/Mods/LivingRoots/manifest.json"` returns 19, and `version_gate_classify "$GAME_PATH" "$GAME_PATH/Mods/LivingRoots/manifest.json"` prints `HARD-FAIL`. The installed game and SMAPI both satisfy the mod's declared minimums (1.6.15 >= 1.6.14; 4.5.2 >= 4.4.0), so this is a false hard-fail, not a genuine incompatibility. The evidence bundle records `missingPrerequisite: "version-mismatch"`.

## Reproduction

1. `GAME_PATH="/home/luis/.steam/steam/steamapps/common/Stardew Valley"`
2. `bash -c 'source scripts/lib/common.sh; source scripts/lib/version-gate.sh; version_gate_classify "$GAME_PATH" "$GAME_PATH/Mods/LivingRoots/manifest.json"; echo rc=$?'` prints `HARD-FAIL`, rc=19.
3. Equivalently, run `verify-scenario.sh --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json --game-path "$GAME_PATH" --skip-build` exits 19, bundle `blocked.json` with `version-mismatch`.

NEEDS CLARIFICATION: whether Gate 3 is intended to run with `--no-acquire` against the already-installed game, or whether `gamepath.lock.json` must first be populated with real builddeps values; the placeholder lock (`failClosed: true`) would make `acquisition_run` exit 19 as a second blocker after the version gate is fixed.

## Suspected Code Paths

- `scripts/lib/version-gate.sh:version_gate_classify()` — the classifier. It accepts `<game_version> <smapi_version> [mod_min_game] [mod_min_api] [matrix]` and resolves file paths via `_vg_resolve`. The `_vg_resolve` `*.dll` branch greps for a whole-line semantic version, which does not match real assemblies (`StardewModdingAPI.dll` exposes `4.5.2.0` / `4.5.2+hash`, and `Stardew Valley.dll` exposes `1.6.15.24356`). The `*.json` branch resolves to `MinimumGameVersion` first, so a manifest passed as the smapi-version slot resolves to the game minimum, not a SMAPI version.
- `scripts/verify-scenario.sh:242-248` — calls `version_gate_classify "$GAME_PATH" "$local_man"`, i.e. a directory as the game version and a manifest as the smapi version. A directory is not a file, so `_vg_resolve` leaves it as the literal path string; `parse()` then yields `(0,0,0)`, which is below any minimum, producing unconditional `HARD-FAIL`.
- `scripts/lib/preflight.sh:preflight_version_gate()` (approx. 82-90) and the version-gate block in `preflight_run()` (approx. 177-186) — same misuse: `version_gate_classify "$game_path" "$manifest_check"`.
- `LivingRoots/manifest.json` — declares `MinimumGameVersion: "1.6.14"`, `MinimumApiVersion: "4.4.0"`; both within range of the installed game (1.6.15) and SMAPI (4.5.2). Not itself the cause.
- `specs/010-headless-verification-runner/compat-matrix.json` — `minimumGameVersion: "1.6.14"`, `minimumApiVersion: "4.4.0"`; consistent with the manifest. No `MaximumGameVersion`/pinned key, so no upper-bound warn is expected.
- `specs/010-headless-verification-runner/gamepath.lock.json` — placeholder (`releaseId: "PLACEHOLDER-UNAVAILABLE"`, `failClosed: true`); a secondary blocker for `acquisition_run` but not for the version gate, which runs first.

## Root Cause Hypothesis

Confidence: high. The version gate is invoked with arguments that do not match its contract. `version_gate_classify` is designed to receive observed version strings/files (`<game_version> <smapi_version>`), but every caller passes the game installation directory as the first argument and the mod manifest as the second. Because a directory is not a regular file, `_vg_resolve` does not resolve it and the literal path string parses to `(0,0,0)`, which is always below the minimum, yielding `HARD-FAIL` (exit 19) regardless of the actual installed versions. The manifest passed as the second argument is also misinterpreted: its `_vg_resolve` `*.json` branch returns `MinimumGameVersion` (1.6.14), which is then treated as the observed SMAPI version, and the "manifest-derived minimums" block intended to read `MinimumGameVersion`/`MinimumApiVersion` from a manifest path is unreachable because its guard (`-z "$mod_min_game"`) is false whenever a path is supplied. In addition, the `*.dll` resolver's whole-line regex cannot extract a version from real .NET assemblies, so even a corrected caller pointing at `StardewModdingAPI.dll` would fail to obtain `4.5.2`. The observed game/SMAPI versions (1.6.15 / 4.5.2) both satisfy the declared minimums (1.6.14 / 4.4.0); verified by calling `version_gate_classify "1.6.15" "4.5.2" "1.6.14" "4.4.0"` which returns `INFO` (rc 0).

## Proposed Remediation

Preferred: make the version gate honor its actual caller contract `preflight_version_gate <game_dir> <manifest>` (and the equivalent call in `verify-scenario.sh`):

1. In `version_gate_classify`, detect when the first argument is a directory and derive observed versions from it: game version from `$dir/Stardew Valley.dll` and SMAPI version from `$dir/StardewModdingAPI.dll`.
2. When the second argument is a manifest JSON, read `MinimumGameVersion`/`MinimumApiVersion` from it as the mod minimums (fixing the currently dead "manifest-derived minimums" branch), instead of treating the manifest as the observed SMAPI version.
3. Rework the `*.dll` resolver to extract a 3-part product version from real assemblies (for example `grep -oE` for `1\.[0-9]+\.[0-9]+` on the game dll and `4\.[0-9]+\.[0-9]+` on the SMAPI dll, excluding the CLR's `4.0.30319`), rather than requiring a whole-line match.
4. Keep the string/file behavior for the existing unit test (`version_gate_classify "1.11.0" "1.2.0" "1.6.14"`).

Alternatives:
- Change the callers to pass explicit version strings/files (`version_gate_classify "$GAME_PATH/Stardew Valley.dll" "$GAME_PATH/StardewModdingAPI.dll" "$manifest" ...`). Rejected: it leaves the directory/manifest caller contract unimplemented and still requires the dll resolver fix; also the contract in `us1-runner-library-api.md` is `preflight_version_gate <game_dir> <manifest>`.
- Bypass the gate with `--ignore-exit-code 19`. Rejected: defeats FR-024/FR-025 honesty and the gate exists precisely to catch real mismatches.

Files likely to change:
- `scripts/lib/version-gate.sh` (`version_gate_classify`, `_vg_resolve`, dll extraction)
- `scripts/lib/preflight.sh` (`preflight_version_gate`, version-gate block in `preflight_run`) — only if the caller signature changes; ideally no change needed
- `scripts/verify-scenario.sh` (version-gate block) — only if the caller signature changes
- `LivingRoots/manifest.json` — optional housekeeping: raise `MinimumGameVersion` to `1.6.15` and add an `UpdateKey` (for example `GitHub:lfgranja/Stardew-LivingRoots`) per the report's alignment ask; not required to unblock Gate 3
- `specs/010-headless-verification-runner/gamepath.lock.json` — either populate with real builddeps values or document that Gate 3 runs use `--no-acquire` against the installed game

Tests to add or update:
- Extend `scripts/tests/test-version-gate.sh` to call `version_gate_classify` with a fake game directory (as `make_fake_game_dir` produces) and assert rc 0 / `INFO` (or `WARN` only when appropriate), and a case where the game dll reports an older version than the minimum producing rc 19.
- Add a preflight-level test asserting `preflight_run` against a fake game dir with a manifest reaches the display phase (exit 10) instead of 19.
- Keep the existing string-argument test passing.

## Risks & Considerations

- The dll-version extraction must not regress the fake-game test fixtures: `make_fake_game_dir` appends the literal `4.5.2` to `StardewModdingAPI.dll`, so a regex that accepts 3-part tokens keeps working; switching to `grep -oE` must still find it.
- The fix must not weaken the gate: a genuinely older game/SMAPI must still hard-fail (19). Keep the `game[:3] < mod_min_game[:3]` / `smapi[:3] < mod_min_api[:3]` comparisons and the prerelease-below-release rules.
- `gamepath.lock.json` remains a separate, later blocker for runs that do not pass `--no-acquire`; document this so Gate 3 is invoked with `--no-acquire` against the installed game, or populate the lock.
- Manifest `MinimumGameVersion`/`MinimumApiVersion` changes affect SMAPI's load-time compatibility check for end users; raising the minimums is a compatibility narrowing and should be deliberate.
- `verified`/`machine-pass` remain human-attested (T148) and must not be auto-attributed even after the gate passes.

## Open Questions

- NEEDS CLARIFICATION: should Gate 3 be run with `--no-acquire` (recommended for the already-installed game), or should `gamepath.lock.json` be populated from real builddeps?
- NEEDS CLARIFICATION: should `MinimumGameVersion` be raised from 1.6.14 to 1.6.15, or kept permissive at 1.6.14?
- NEEDS CLARIFICATION: should an `UpdateKey` be added to `manifest.json` now, or deferred?

Slug: gate3-version-gate
