# Save-Alignment Contract

**Feature**: 016-gate3-runner-hardening
**Scope**: Milestone save isolation and loadability in `scripts/lib/save.sh` (`save_prepare`, `save_isolate`, `save_align_inner_name`).
**Related**: `specs/009-end-to-end-game-verification/contracts/evidence-bundle.md` (save preservation), `specs/010-headless-verification-runner/contracts/exit-codes.md` (code 19).

## Contract

Stardew Valley resolves a save as `<Saves>/<name>/<name>`: the containing folder and the main save file must share a name. A milestone packaged by renaming only the folder (for example `farm-year1-day5/Agroeco_421179919`) launches and then fails at `SaveGame.Load` with `DebugOutput: File does not exist`, returning to title.

Therefore, during `save_prepare`, the isolated working copy MUST satisfy:

- folder name == inner save file name (the `save_name` / milestone name).

`save_prepare` either aligns the names or fails loudly with a named cause (exit `19`). It never silently returns to title.

## save_align_inner_name

`save_align_inner_name <working_dir> <save_name>` operates on the working copy only and renames the single non-sidecar file to match the folder.

Algorithm:

1. If `$working_dir/$save_name` already exists, return `0` (already aligned; no change).
2. Enumerate regular files directly in `$working_dir` (maxdepth 1).
3. Exclude sidecar/auxiliary files by basename:
   - `SaveGameInfo`
   - `SaveGameInfo_old`
   - any name matching `*_old`
   - any name matching `*.json`
   - any name matching `*.xml`
4. Collect the remaining basenames as candidates.
5. If there is **exactly one** candidate:
   - rename `$working_dir/$candidate` to `$working_dir/$save_name`;
   - if `$working_dir/${candidate}_old` exists, rename it to `$working_dir/${save_name}_old`.
   - Return `0` on success; return `19` (`EXIT_ERRORED`) if either rename fails.
6. If there are **zero or multiple** candidates:
   - leave the directory untouched (no rename, no deletion);
   - return `0` without changes.

Ambiguity is not resolved by guessing. `save_prepare` then checks for the presence of `$SAVE_WORKING_DIR/$save_name`; if it is absent, `save_prepare` returns `19` with a named cause (`missing <path>`). Thus an ambiguous or unalignable milestone fails loudly at prepare time rather than at game time.

## save_prepare obligations

- Resolve the milestone under `${RUNNER_SAVES_ROOT:-${XDG_CONFIG_HOME:-$HOME/.config}/StardewValley/Saves}`.
- Missing milestone or empty milestone name => exit `19` with an actionable message.
- Isolate into a per-run instance directory via `save_isolate` (copy, never mutate the source).
- Call `save_align_inner_name` on the working copy.
- Verify `$SAVE_WORKING_DIR/$milestone` exists after alignment; otherwise exit `19`.
- Export `RUNNER_SAVE_ID`, `RUNNER_SAVE_DIR`, `RUNNER_SAVE_INSTANCE_DIR`, and (unless pinned) `RUNNER_SMAPI_LOG` pointing into the isolated tree.
- The source milestone tree must be byte-identical before and after the copy (hash check); a mismatch exits `19`.
- The player's real save directory is never opened, copied, or modified (see save preservation in `specs/009-end-to-end-game-verification/contracts/evidence-bundle.md`).

## Exit codes

- `0`: aligned (or already aligned) and loadable.
- `14` (`USAGE_ERROR`): wrong arity or missing required argument.
- `9` (`MISSING_DEPENDENCY`): source milestone directory absent at the filesystem level passed to `save_isolate`.
- `19` (`ERRORED`): missing milestone, empty milestone, failed rename, ambiguous layout (no aligned target file), source-hash mismatch, or save I/O failure after retries.

## Test contract

The automated test must fail when the alignment fix is reverted. Concretely:

- A milestone whose folder and inner file disagree (`renamed-only`) is aligned to a loadable `<name>/<name>` layout and `save_prepare` returns `0`.
- A milestone with zero or multiple non-sidecar candidates (`ambiguous`) is left untouched and `save_prepare` returns `19`.
- A missing milestone returns `19`.
- The source milestone tree hash is unchanged by the operation.