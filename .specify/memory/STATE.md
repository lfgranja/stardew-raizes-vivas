# Session State — feat/008-smoke-validation

**Date**: 2026-10-19
**Branch**: `fix/protected-paths-script-confirm` (carries the 008 spec work)
**Feature**: Smoke Validation Script (spec 008)

## COMPLETION SUMMARY

### Checklist review round 7 (2026-10-19) — AWAITING USER BATCH
- All 5 checklists re-verified against the current artifacts by **5 parallel review agents**;
  **8 external research agents** gathered evidence (bash trap/process-group semantics,
  lockfile design, exit-code taxonomy, TOCTOU/file-safety, headless graphics probing,
  IEEE 829 → ISO/IEC/IEEE 29119, CLI output & error-message design, artifact identity &
  reproducible builds).
- **Result: 360/360 → 317/361 items passing** (operational-readiness 81/92 ·
  requirements 12/16 · script-reliability 78/89 · security-safety 72/82 ·
  smapi-integration 74/82). **44 items unchecked**, collapsing into **10 decision clusters**
  now presented to the user as a batch.
- 8 atomic commits (`fix/docs: … — <file> — <why>`), format gate green.

### Round-7 evidence-resolved corrections (no decision needed) — applied
- **SMAPI log format**: level field padded to **5** (not 8) + one separator space; there is
  **no `[ERROR]` token** — the level lives inside the bracket group and must be matched as a
  column. FR-003, `data-model.md`, `research.md` corrected.
- **Crash marker**: SMAPI's blocking `Console.ReadKey()` is gated by
  `smapi-internal/StardewModdingAPI.crash.marker`, **not** by `SMAPI-crash.txt` (which is
  only a log copy). FR-002 now requires removing the marker.
- **Relocated apphost** exits **154** (no dll) / **131** (no runtime) / **137** (no CoreCLR) —
  never 134. FR-001's rationale about SMAPI's path resolution was wrong and is corrected.
- **`SMAPI_USE_CURRENT_SHELL` does not exist** in SMAPI 4.5.2 (only `SMAPI_NO_TERMINAL`,
  `SMAPI_MODS_PATH`, `SMAPI_DEVELOPER_MODE`).
- **`Mods go here:` is `~`-anonymised**; the version banner and that line precede the
  `Log started at` sentinel.
- **Sentinel** = `Log started at <UtcNow:s> UTC` — no offset, so the `T0` comparison must
  treat it as UTC; the inode baseline must be captured **before** the stale log is removed.
- **Trap design pinned**: reset-and-re-raise (`trap - SIG; kill -SIG $$`), `trap '' INT TERM`
  **prohibited**, `wait -f`, children with `</dev/null`.
- **Lock file pinned**: stable per-user XDG path, `exec 9<>` (never `>`), never removed.
- `Fix:` (not `hint:`) is the trailing remediation key; `research.md`'s four stale claims
  (`DEVELOPER_MODE`, `pgrep`, `BashFAQ/062`, "mod's own data folder") corrected.

### BLOCKED — awaiting implementation
- The script itself does not exist yet (`scripts/smoke-validation.sh`).
- Xvfb is **not installed** on the maintainer's machine (pre-flight will exit `9`).
- Gate 3 (end-to-end verification) stays ⏳ until a real run succeeds.

### Round 6 decisions (2026-10-19) — user accepted all 5 recommendations
1. `9` = required host tool **absent**; `3` = graphics stack **present but unusable**.
   Required-tool set enumerated in FR-012 item 8.
2. "sysexits.h convention" label **withdrawn** (it is not sysexits numbering).
   Codes `12` (build/artifact failure) and `13` (internal error) added additively.
   FR-007 now has a general translation table — no raw child status ever escapes.
3. Game launched under bash job control (`set -m`) → PGID == PID; mandatory PGID
   assertion before every group signal. No new dependency.
4. EXIT trap removes X locks only for the script's own display and only when the
   owning PID is dead.
5. Process identity by `/proc/$PID/exe` restricted to the resolved game path;
   `pgrep -x` is non-functional for the 17-char apphost — use `pidof`.

## Notes
- **Round-7 side effect**: two review agents ran `dotnet build`, which re-deployed
  `LivingRoots.dll` into `~/.steam/.../Mods/LivingRoots/` (mtime 21:01 and 21:21). The
  repository working tree is unaffected (`LivingRoots/bin/` is gitignored). The deployed
  copy is now the current build, which is *not* the state the spec's stale-artifact
  scenario assumes.
- Working tree also contains pre-existing changes not made by this session:
  `.specify/bugs/protected-paths-script-missing/test.md` (modified) and
  `.specify/assessments/automate-ingame-testing/` (untracked). Do not commit those here.
- Previous session (feat/005-soil-health-to-crop-growth) is complete and committed; see git log.
