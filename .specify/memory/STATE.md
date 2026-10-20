# Session State — feat/008-smoke-validation

**Date**: 2026-10-19
**Branch**: `fix/protected-paths-script-confirm` (carries the 008 spec work)
**Feature**: Smoke Validation Script (spec 008)

## COMPLETION SUMMARY

### Checklist review round 8 (2026-10-19) — IN PROGRESS
- All 5 checklists re-verified against the current artifacts by **5 parallel review agents**
  (one per checklist), each **editing its own checklist** to set the correct `[x]`/`[ ]`
  marker and rewrite stale justifications.
- **10 parallel research agents** gathered evidence: path-allowlist scope; declared
  side-effect completeness; test-coverage gaps (ISO/IEC/IEEE 29119); timeout budget scope;
  SMAPI marker-file semantics; process-group signal hygiene; TOCTOU / "open directly,
  branch on the result"; umask & file-mode policy; CLI option inventory contract;
  SMAPI log-parsing robustness.
- Primary sources used throughout: the upstream **SMAPI 4.5.2** source
  (`Pathoschild/SMAPI` tag `4.5.2`, commit `821167e5c511bf3a2d98f604e5e838561c469219`,
  extracted at `/tmp/opencode/smapi-src/`) — byte-identical to the installed binary at
  `~/.steam/steam/steamapps/common/Stardew Valley`.

### Round-8 findings that were MATERIAL (all corrected in the artifacts)
1. **The success gate's skip-leg was vacuous** — SMAPI writes the skipped-mods header with
   three leading spaces *inside the message*, so an unpadded anchor never matched and a
   **skipped** mod was reported as a **pass**. FR-003 now pins three explicitly scoped legs.
2. **The level field is variable width** — `PadRight(MaxLevelLength)` is a no-op on the
   internal `CRITICAL` (8 chars), which shifts the source column by 3. The level must be
   bound by **label**, never by a column slice.
3. **The sentinel is TRACE-level** (`Monitor.Log` defaults to `LogLevel.Trace`), so a
   parser reading only `INFO`+ never finds the run boundary.
4. **Continuation lines** carry no prefix and the log is **mixed CRLF/LF**; any `$`-anchored
   pattern never matches a CRLF-terminated line.
5. **The crash marker *aborts* (exit 1), it does not hang**, under the script's `/dev/null`
   stdin — and it is **not self-clearing**, so every later run fails identically.
6. **The `Loaded N mods:` gate is not graphics-independent** — SMAPI creates the graphics
   device before resolving mods, so a graphics failure yields a timeout (`4`), the very
   misclassification the spec forbids.
7. **`set -m` must wrap each child launch**; enabling it script-wide breaks the `INT`
   handler (measured under a pty).
8. **The timeout is launch-scoped**, and the rebuild was previously **unbounded**.
9. **The path allowlist rejects the script's own default** `--game-path` and the game
   assembly it must find — an unsatisfiable requirement. **Still open (D1).**
10. **The `lefthook.yml` `bash -n` gate parsed only the first staged file** — a broken
    second file passed. Fixed to parse every file.
11. **The game was not muted** — Xvfb virtualizes the display, not the audio device, so the
    run would have played the title-screen music on the maintainer's speakers. Now muted
    via `SDL_AUDIODRIVER=dummy` + `ALSOFT_DRIVERS=null`.

### Round-8 open decisions (raised as a batch)
- **D1** — path-allowlist scope (the default `--game-path` contains a space).
- **D2** — adopt a single normative CLI options table?
- **D3** — exit-code gaps: `16` unreachable, FR-012 item 7 assigns no code, "SMAPI aborted
  before mod resolution" has no code.
- **D4** — test-coverage gaps (FR-001/FR-008/FR-013; exit codes 2/10/11/13/14/16).
- **D5** — where the "open directly, branch on the result" rule lives.

### Environment facts (re-verified 2026-10-19)
- `Xvfb` **NOT installed** → the pre-flight exits `9`; **no end-to-end run is possible** and
  every TC is unexecuted. `shellcheck` also absent (the lint gate now skips it).
- `uconv` absent (irrelevant — the normalizer dependency was dropped in round 7).
- Steam install path is a **symlink chain** (`~/.steam/steam` → `~/.local/share/Steam`);
  bash `$PWD` is logical, `/proc` is physical.
- Measured: rebuild 6.6 s warm / 5.0–26.2 s under load; launch→verdict ~15 s unloaded /
  29.6–41.8 s under load; digest comparison ~37 ms.
