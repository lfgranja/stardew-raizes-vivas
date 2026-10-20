# Session State — feat/008-smoke-validation

**Date**: 2026-10-19
**Branch**: `fix/protected-paths-script-confirm` (carries the 008 spec work)
**Feature**: Smoke Validation Script (spec 008)

## COMPLETION SUMMARY

### Checklist review round 8 (2026-10-19) — DONE
- All 5 checklists re-verified against the current artifacts by **5 parallel review agents**
  (one per checklist), each **editing its own checklist** to set the correct `[x]`/`[ ]`
  marker and rewrite stale justifications.
- **10 parallel research agents** gathered evidence: path-allowlist scope; declared
  side-effect completeness; test-coverage gaps (ISO/IEC/IEEE 29119); timeout budget scope;
  SMAPI marker-file semantics; process-group signal hygiene; TOCTOU / "open directly,
  branch on the result"; umask & file-mode policy; CLI option inventory contract;
  SMAPI log-parsing robustness.
- Primary source used throughout: the upstream **SMAPI 4.5.2** source
  (`Pathoschild/SMAPI` tag `4.5.2`, commit `821167e5c511bf3a2d98f604e5e838561c469219`,
  extracted at `/tmp/opencode/smapi-src/`) — byte-identical to the installed binary at
  `~/.steam/steam/steamapps/common/Stardew Valley`.
- **Result: 361/361 → 340/361 → 349/361 → 361/361 items passing.**

### Round-8 user decisions (2026-10-19) — user accepted all 5 recommendations
1. **Path allowlist scope** → the character allowlist is **withdrawn** for
   environment-derived names (the user-supplied roots, the game's own file names such as
   `Stardew Valley.dll`, and everything inherited from them). It is retained only for the
   script's **own authored literals** (`[A-Za-z0-9._-]+`). **Quoting becomes the load-bearing
   control**: every path `"$var"`, every path argument after `--`. No Unicode normalization.
   Rationale: the allowlist permits `..` (not a traversal defence), the script runs as the
   attacker (no privilege boundary), and it rejected the tool's own default — ~86 % of Steam
   titles contain a space.
2. **CLI options** → a single normative **Command-Line Options** table in `spec.md`; `--help`
   and the quickstart are **renderings** of it (the `help2man` relation). Long form only
   except `-h`; `--opt=VALUE` accepted; **no abbreviation** (`--mod` is ambiguous);
   `--` ends option processing; no positional arguments; usage error → `14`.
3. **Exit-code gaps** → **one** new code, `17` = "SMAPI aborted before mod resolution";
   `16` made **reachable** by splitting FR-017's check (pre-flight: the resolved
   `--mods-path` must be an existing, writable directory); FR-012 item 7's path failure
   exits `16`; and **log-based classification takes precedence over the status translation**
   (a run with a sentinel but no mod-resolution header is `17` whatever the status was).
4. **Test coverage** → **both**: nine new launch-free cases TC-18…TC-26 (usable *before*
   `Xvfb` is installed) plus a **Requirement-Coverage Summary** table recording the
   exclusions with their rationale (ISO/IEC/IEEE 29119-3 §6.2.4.3). FR-001 is closed by
   asserting the launch contract from the run log inside TC-1/TC-15.
5. **"Open directly, branch on the result"** → normative in FR-012 item 7 as an
   *Open-without-gating* clause scoped to **gating**; the Clarification no longer calls it
   "load-bearing" (CWE-367's own mitigation says it does not resolve the underlying issue).

### Round-8 findings that were MATERIAL (all corrected in the artifacts)
1. **The success gate's skip-leg was vacuous** — SMAPI writes the skipped-mods header with
   three leading spaces *inside the message*, so an unpadded anchor never matched and a
   **skipped** mod was reported as a **pass**.
2. **The level field is variable width** — `PadRight(MaxLevelLength)` is a no-op on the
   internal `CRITICAL` (8 chars), which shifts the source column by 3. Bind by **label**.
3. **The sentinel is TRACE-level** (`Monitor.Log` defaults to `LogLevel.Trace`).
4. **Continuation lines** carry no prefix and the log is **mixed CRLF/LF**; any `$`-anchored
   pattern never matches a CRLF-terminated line.
5. **The readiness/verdict sequencing made the success path unreachable** — the sentinel is
   written in the `SCore` constructor; the mod-resolution block arrives tens of seconds
   later. FR-002 now defines a **two-phase wait**.
6. **The marker *aborts* (status 1), it does not hang**, under the `/dev/null` stdin — and
   it is **not self-clearing**, so every later run failed identically.
7. **`set -m` must wrap each child launch**; enabling it script-wide breaks the `INT`
   handler (measured under a pty).
8. **The timeout is launch-scoped**, and the rebuild was previously **unbounded**.
9. **The path allowlist rejected the script's own default** — an unsatisfiable requirement.
10. **The `lefthook.yml` `bash -n` gate parsed only the first staged file** — a broken
    second file passed. Fixed to parse every file; `shellcheck` is skipped when absent.
11. **The game was not muted** — Xvfb virtualizes the display, not the audio device, so the
    run would have played the title-screen music on the maintainer's speakers. Now muted
    via `SDL_AUDIODRIVER=dummy` + `ALSOFT_DRIVERS=null`.
12. **Exit `16` was unreachable**, and "SMAPI aborted before mod resolution" had no code.

### Environment facts (re-verified 2026-10-19)
- `Xvfb` **NOT installed** → the pre-flight exits `9`; **no end-to-end run is possible** and
  every TC is unexecuted. `shellcheck` also absent (the lint gate now skips it).
- `uconv` absent (irrelevant — the normalizer dependency was dropped in round 7).
- Steam install path is a **symlink chain** (`~/.steam/steam` → `~/.local/share/Steam`);
  bash `$PWD` is logical, `/proc` is physical.
- Measured: rebuild 6.6 s warm / 5.0–26.2 s under load; launch→verdict ~15 s unloaded /
  28–42 s under load; digest comparison ~37 ms.

## Next
- `Xvfb` must be installed (`dnf install xorg-x11-server-Xvfb`) before Gate 3 can be
  satisfied and any TC can be executed.
- `scripts/smoke-validation.sh` does not exist yet — implementation is the next phase.
