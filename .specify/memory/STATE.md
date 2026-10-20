# Session State — feat/008-smoke-validation

**Date**: 2026-10-19
**Branch**: `fix/protected-paths-script-confirm` (carries the 008 spec work)
**Feature**: Smoke Validation Script (spec 008)

## COMPLETION SUMMARY

### Checklist review round 5 (2026-10-19) — DONE
- 4 checklists with unchecked items reviewed by 4 parallel review agents; 6 background
  research agents gathered external evidence.
- **Result: 344/344 → 360/360 checkbox items passing across all 5 checklists**
  (operational-readiness 92/92 · requirements 16/16 · script-reliability 88/88 ·
  security-safety 82/82 · smapi-integration 82/82).
- 9 atomic commits (`feat/fix/docs: … — <file> — <why>`), format gate green.

### Evidence-resolved corrections applied
- **Bash trap semantics** (measured on bash 5.3.9): an inherited `EXIT` trap does **not**
  fire in command substitution / `( )` / background jobs / pipeline stages / functions-in-
  subshells — only the parent fires it, once. FR-005 now states the single-trap rule.
- `research.md` §5 still listed the superseded `8 (interrupted)` code → corrected to FR-007.
- `smapi-integration` 1.3 → `SMAPI_DEVELOPER_MODE` gates console verbosity only.
- `operational-readiness` 2.8 → now states the `timeout` 124→4 translation exception.
- `security-safety` Anti-Patterns row 9 + item 9.4 → aligned with the minimal Xvfb posture
  (spec.md:177). Residual risk recorded in spec.md Assumptions.
- FR-012 item 5 → pinned the X11 lock file's 11-byte `"%10lu\n"` format and the
  ESRCH/EPERM liveness test (xorg-server 21.1.24 `os/utils.c`).
- `spec.md` interruption clarification → `BashFAQ/062` corrected to `BashFAQ/047`.

### BLOCKED — awaiting the user's answers to 5 batch questions
1. Pre-flight dependency exit code: `3` (graphics) vs `9` (missing binary) overlap.
2. No code for "the script itself broke" / "the build failed"; `sysexits.h` label is wrong.
3. How the game enters its own process group (setsid / `set -m` / `timeout` / descendant walk).
4. X11 lock removal scope in the EXIT-trap cleanup (FR-010(3) vs FR-012(5)).
5. `--force` PID identity verification (`pgrep -x` is non-functional for the 17-char apphost).

## REMAINING (cannot be completed by agent — requires the real game)
- The script itself does not exist yet (`scripts/smoke-validation.sh`).
- Xvfb is **not installed** on the maintainer's machine (pre-flight will exit `3`).
- Gate 3 (end-to-end verification) stays ⏳ until a real run succeeds.

## Notes
- Working tree also contains pre-existing changes not made by this session:
  `.specify/bugs/protected-paths-script-missing/test.md` (modified) and
  `.specify/assessments/automate-ingame-testing/` (untracked). Do not commit those here.
- Previous session (feat/005-soil-health-to-crop-growth) is complete and committed; see git log.
