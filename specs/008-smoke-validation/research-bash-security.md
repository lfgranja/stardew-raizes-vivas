# Research: Bash Script Security — Path Traversal & TOCTOU

**Date**: 2026-10-19
**Context**: `specs/008-smoke-validation` — smoke validation bash script handling game path, mod path, SMAPI log path; mod data under `<mod folder>/data/`.
**Authority**: OWASP OS Command Injection Defense Cheat Sheet; CWE-22 (Path Traversal), CWE-36 (Absolute Path Traversal), CWE-59 (Link Resolution), CWE-78 (OS Command Injection), CWE-367 (TOCTOU), CWE-377/378/379 (Insecure Temp File), CWE-732 (Incorrect Permission Assignment); David A. Wheeler *Secure Programs HOWTO* (avoid-race, file-names, character-encoding); BashFAQ/062; `ModDataService.SanitizePathSegments()` (project reference implementation).

**Scope**: Unchecked security-safety checklist items `1.3, 1.5, 1.6, 1.7, 1.8, 1.10, 3.3, 8.2, 8.3, 8.6, 8.7`. Each entry: finding, recommendation, autonomous (can be implemented non-interactively by an agent/script).

---

## 1.3 — Paths containing `../` sequences (literal or percent-encoded) rejected after canonicalization

**Finding**: CWE-22 / CWE-23. A denylist that strips `../` is bypassable via double encoding (`%252e%252e%252f`), partial sequences (`.../...//` → `../` after naive removal, CWE-182), and OS-specific separators. OWASP requires decode → canonicalize → validate (CWE-180), and forbids re-decoding. Bash has no native canonicalizer; `realpath -e`/`readlink -f` resolves `..` and symlinks at the OS level. Percent-encoded input must be decoded *once* before canonicalization, then the canonical result must be re-checked. The robust check is not "does the string contain `../`" but "does the canonicalized path remain under the approved base prefix".

**Recommendation**:
1. Reject NUL bytes first (`%00`, `\0`) — already covered by 1.4.
2. Percent-decode exactly once (use `python3 -c "import urllib.parse,sys; print(urllib.parse.unquote(sys.argv[1]))"` or equivalent; never decode twice).
3. Canonicalize with `realpath -e -- "$candidate"` (or `readlink -f`); if resolution fails → reject.
4. Verify the canonical path equals the base or starts with `${base}/` (prefix check on canonical result, not raw input).
5. Additionally reject any path whose *raw* form still contains a `..` segment after splitting on `/` and `\` (defense-in-depth, mirrors `SanitizePathSegments`/`IsPathTraversalSegment`).
6. Log failure generically (no raw malicious input) — already 1.9.

**Autonomous**: Yes.

---

## 1.5 — Unicode-normalize all path components (NFC or NFKC) before comparison

**Finding**: CWE-22 (homoglyph/Unicode bypass). The C# side uses `FormC` (NFC) in `ModDataService`/`ModConstants` save-id sanitization; the bash script must match or a homoglyph that normalizes identically on the C# side could bypass the bash prefix check (or vice versa). OWASP input-validation guidance: normalize before validation and validate the normalized form. NFKC aggressively collapses compatibility characters (fullwidth, ligatures) which can create new traversal-like sequences; NFC is the safer, lossless choice and matches the mod's `FormC`.

**Recommendation**:
1. Choose **NFC** (not NFKC) to stay consistent with `ModDataService` (FormC) and avoid compatibility-collapse surprises.
2. Normalize *both* the candidate path and the approved base with the same routine before the prefix comparison.
3. Implement via a small helper: `python3 -c "import unicodedata,sys; print(unicodedata.normalize('NFC', sys.argv[1]))"` (python3 is already a likely dependency for percent-decoding). Fall back to `uconv -x '::nfc;'` (ICU) if python3 is unavailable; if neither exists, fail closed.
4. Apply normalization *before* canonicalization and *before* the `../` segment check, so encoded/normalized traversal is caught.
5. Keep the normalized value for all downstream comparisons; never compare raw input against the base.

**Autonomous**: Partial — scriptable, but depends on `python3`/`uconv` being present in the execution environment; pre-flight should verify the dependency (mirrors 8.1 style) and fail closed if absent.

---

## 1.6 — Absolute paths rejected when a relative path within a known base is expected

**Finding**: CWE-36 (Absolute Path Traversal). When the script expects a path *inside* game/mod/save base, an absolute input (e.g. `/etc/passwd`, or a save name like `/home/...`) must be rejected before joining, otherwise `os.path`-style join semantics can discard the base (Python `os.path.join` precedent cited in CWE-22 examples). On Linux the only absolute marker is a leading `/`; also reject `~`, and empty components that resolve to root.

**Recommendation**:
1. If the field is documented as relative (save id, mod-relative filename, game-relative path), reject any value matching `^/` or containing a NUL.
2. Reject values containing `..` segments (covered by 1.3) and any value that, after `realpath`, is not under base.
3. Validate against an allowlist regex where the domain is closed (e.g. save id `^[A-Za-z0-9_-]+$`, per checklist 5.4) — preferred over path manipulation.
4. Never concatenate user input onto a base and then `cd`/`open` without the prefix re-check; use `--` before variable paths (already 5.3).

**Autonomous**: Yes.

---

## 1.7 — `O_NOFOLLOW` (or equivalent) when opening lock/temp files

**Finding**: CWE-59 / CWE-367. Bash builtins (`exec 9>"$lockfile"`, `>`, `>>`) use `open(2)` without `O_NOFOLLOW`, so a pre-planted symlink at the lock path is followed — even inside a 0700 directory an attacker who can write the directory (or win a race) can redirect the write. `flock` locks the inode that was opened, so following a symlink locks the attacker's target instead. True `O_NOFOLLOW` requires a helper (python `os.open(..., os.O_NOFOLLOW)`, perl `sysopen` with `O_NOFOLLOW`, or a small C helper); pure bash cannot set the flag.

**Recommendation**:
1. Primary control (already partially in place): lock/temp files live in a directory created with `mktemp -d` + `chmod 0700` owned by the current user (checklist 3.3/4.7/8.4). This removes write access for other users, which is the realistic threat model for a per-user smoke script.
2. Defense-in-depth: before opening, verify with `[[ -L "$lockfile" ]]` (uses `lstat`, checklist 8.3) and reject if it is a symlink; verify ownership/mode (checklist 8.7).
3. Where a helper is acceptable, open with `python3 -c "import os,sys; fd=os.open(sys.argv[1], os.O_WRONLY|os.O_CREAT|os.O_EXCL|os.O_NOFOLLOW, 0o600); ..."` so the kernel refuses to follow a symlink atomically. Pass the fd back to bash via `/dev/fd` or do the lock inside the helper.
4. Combine with `flock` on the resulting fd; never `rm` the lock file after (already 3.8) to avoid re-creating a race window.

**Autonomous**: Partial — the 0700-directory + lstat + ownership controls are fully scriptable; true `O_NOFOLLOW` needs a python/perl helper, which is scriptable but adds a dependency that must be pre-flighted.

---

## 1.8 — Game save directory paths validated to resolve to the expected Stardew Valley save location

**Finding**: CWE-22 / CWE-73. The save directory is derived from `Constants.SaveFolderName` (SMAPI) and typically lives under `~/.config/StardewValley/Saves/<SaveName>` (Linux) or the SMAPI-configured saves path. An attacker-controlled or misconfigured game path could point saves elsewhere; the script must ensure any save path it touches stays under the canonical save root. Save names themselves are constrained by the game to `[A-Za-z0-9_-]`-ish identifiers.

**Recommendation**:
1. Resolve the save root once: prefer `STARDew`/SMAPI env or default `~/.config/StardewValley/Saves`; canonicalize with `realpath`.
2. For any save path input, canonicalize and require it to be `${saveRoot}/<name>` (prefix check) with `<name>` matching `^[A-Za-z0-9_-]+$` (already 5.4).
3. Reject symlinks in the save path (checklist 8.6) and verify ownership (8.7).
4. If `--save`/save name is not provided, skip save-data checks (P2 per spec 008 termination decision) and label them `SKIPPED:` (operational-readiness 1.10).
5. Never trust a save path supplied via environment without the same canonicalization + prefix + normalization pipeline (1.3/1.5/1.6).

**Autonomous**: Yes (with a configurable save-root override for non-default installs, validated the same way).

---

## 1.10 — Apply the same sanitization rigor as `ModDataService.SanitizePathSegments()` in the script

**Finding**: The project's reference implementation (`LivingRoots/Services/ModDataService.cs:SanitizePathSegments`) splits on `/` and `\`, skips `.` segments, rejects `..` (exact and traversal patterns, >2 consecutive dots), runs `SanitizeFileName` per segment, drops empty/whitespace segments, requires ≥1 valid segment, and joins with `/`. It also applies FormC (NFC) upstream. The bash script currently has no equivalent, so a path accepted by the script might be rejected (or vice versa) by the mod, causing false pass/fail in smoke validation. Parity is needed for the script's verdict to reflect mod behavior.

**Recommendation**: Port the algorithm to bash as a reusable function `sanitize_path_segments()`:
1. Reject null/empty/whitespace-only.
2. Unicode-normalize (NFC) first (1.5).
3. Percent-decode once (1.3).
4. Reject NUL (1.4).
5. Split on `[\/]`; for each segment: skip `.`; reject exact `..`; reject >2 consecutive dots; run a filename sanitizer (strip control chars, reject shell metacharacters per 5.6, allowlist `^[A-Za-z0-9._-]+$` or the mod's `SanitizeFileName` rules); drop empty/whitespace.
6. Require ≥1 surviving segment; join with `/`.
7. Then apply canonicalization + base-prefix check (1.1/1.2/1.3/1.6).
8. Keep the function in a sourced library (`scripts/lib/path-sanitize.sh`) so both the smoke script and any future script share one implementation; add a self-test mirroring `SanitizePathSegmentsTests.cs`/`ModDataServiceTests.cs`.

**Autonomous**: Yes.

---

## 3.3 — Lock file stored in a directory with restrictive permissions (`chmod 0700`)

**Finding**: CWE-377/379. A lock file in a world-writable/shared directory (e.g. `/tmp` with a fixed name) is vulnerable to squatting and symlink redirection. `mktemp -d` already creates a private dir, but the lock file's *containing* directory must be explicitly 0700 and owned by the current user; relying on umask alone is insufficient if the dir is later chmod'd or inherited.

**Recommendation**:
1. Create the lock directory with `lockdir=$(mktemp -d)` then `chmod 700 "$lockdir"` (and verify `stat -c %a` == 700 and owner == `$(id -u)`).
2. Place the lock file inside: `lockfile="$lockdir/livingroots-smoke.lock"`; open with `exec 9>"$lockfile"` (already 3.4).
3. Set `umask 077` at script start (already 4.4) so the lock file itself is 0600.
4. Do not remove the lock file on exit (already 3.8); remove the directory only if empty and only after the fd is closed, or leave it for the OS/tmp cleaner.
5. Pre-flight: verify the directory is not a symlink and not group/world-writable before use (8.6/8.7).

**Autonomous**: Yes.

---

## 8.2 — Never use `access()`/`stat()` to check before `open()` (TOCTOU window)

**Finding**: CWE-367. Patterns like `[[ -f "$f" ]] && cat "$f"` or `[ -r "$f" ]` then `open` create a TOCTOU window: the file can be replaced (e.g. with a symlink) between the check and the use. OWASP/CWE mitigation: do not check-then-use; perform the operation and handle the error. In bash, `[[ -f ]]`/`[[ -r ]]` are `stat(2)`-based and are exactly the anti-pattern.

**Recommendation**:
1. Remove all pre-checks (`-f`, `-r`, `-e`, `-w`) that gate an immediately-following open/read/write.
2. Open directly and branch on the syscall result: `exec 9>"$lockfile"` then `if (( $? != 0 )); then ...; fi`, or use `if ! { exec 9>"$lockfile"; } 2>/dev/null; then ...`.
3. For reads, attempt `cat -- "$f"` and handle failure rather than pre-testing existence.
4. Where existence must be reported to the user, stat *after* a successful open (fd-based), or accept the inherent race and document it.
5. Keep `set -e`/`set -o pipefail` (5.1) so unexpected failures propagate instead of being masked.

**Autonomous**: Yes.

---

## 8.3 — Use `lstat()` to verify a file is not a symlink before operating

**Finding**: CWE-59 / CWE-367. Bash's `[[ -L "$path" ]]` invokes `lstat(2)` and returns true if the path itself is a symbolic link, without following it — this is the correct primitive (as opposed to `[[ -e ]]`/`-f` which follow). It must be applied to the path *and* to each component when symlink traversal in intermediate directories is a concern.

**Recommendation**:
1. Add a helper `is_symlink() { [[ -L "$1" ]]; }` and call it before opening any game/mod/save/lock/temp path.
2. For directory trees (game dir, mod dir), reject if any entry is a symlink: `if find -- "$dir" -type l -print -quit | grep -q .; then reject; fi` (or `find ... -type l | read`).
3. Combine with `realpath` mismatch detection: if `realpath "$p"` differs from the lstat-resolved expectation, a symlink is involved.
4. Treat symlink presence as a hard failure (exit non-zero) for game/mod dirs; for optional paths, label `SKIPPED:` and log generically.
5. Note the residual TOCTOU: lstat then open is still racy; mitigate with 0700 dirs + O_NOFOLLOW (1.7) and by not re-checking.

**Autonomous**: Yes.

---

## 8.6 — Symlinks in the game directory and mod directory checked before reading/writing

**Finding**: CWE-59. The game install and mod folder are attacker-influenced surfaces (a compromised or malicious mod could replace a file with a symlink pointing outside the tree, causing the script to read/write an unintended target, e.g. SMAPI log or save data). Wheeler *Secure Programs HOWTO* (file-names / avoid-race) recommends resolving and validating paths rather than trusting directory contents.

**Recommendation**:
1. During pre-flight, walk both `GAME_DIR` and `MOD_DIR` and fail if any symlink is found: `find "$dir" -type l -print` must be empty (or only an explicit allowlist of known-safe links).
2. Canonicalize both dirs with `realpath` and verify they are not themselves reached via a symlinked parent (compare `realpath "$dir"` to `readlink -f` of each parent).
3. When reading specific files (SMAPI log, mod data), apply the same 1.3/1.5/1.6/8.3 checks to the full path.
4. Document the check in pre-flight output ("What → Where → How to fix") with a distinct exit code (per research.md §5/§8).
5. Re-check is not required per read if pre-flight passed and dirs are 0700/owned; but re-run pre-flight on each invocation.

**Autonomous**: Yes.

---

## 8.7 — File ownership and permissions verified before reading sensitive files

**Finding**: CWE-732 / CWE-276. Sensitive files (SMAPI log, mod data under `<mod folder>/data/`, save files) must be owned by the current user and not group/world-writable; otherwise another local user may have tampered with them (and, on shared hosts, read secrets). Wheeler and OWASP recommend least privilege and verifying attributes before use.

**Recommendation**:
1. Add a helper `check_owner_mode()`: verify `stat -c %u` == `$(id -u)` and `stat -c %a` has no group/world write bits (mode & 0o022 == 0). For lock/temp dirs require exactly 0700 (8.4).
2. Apply to: SMAPI log file, mod data directory, each data file read, save directory, game/mod directories.
3. Fail closed (non-zero exit) on mismatch; log generically (no full path exposure beyond what's already logged, per 9.2).
4. On multi-user systems, also reject files owned by root unless explicitly expected (game install may be root-owned read-only — allow read-only root-owned game files but require non-writable by group/other).
5. Combine with 8.2 (no check-then-use): perform the ownership check, then open; accept the residual race or mitigate via 0700 dirs.

**Autonomous**: Yes.

---

## Summary

| Item | Finding (primary CWE) | Recommendation (short) | Autonomous |
|------|----------------------|------------------------|------------|
| 1.3 | CWE-22/23/180/182 | Decode once → canonicalize (`realpath`) → NFC → base-prefix check + `..` segment reject | Yes |
| 1.5 | CWE-22 (homoglyph) | NFC (match FormC), normalize candidate+base, fail closed if no normalizer | Partial (needs python3/uconv) |
| 1.6 | CWE-36 | Reject leading `/`, `~`, empty; allowlist closed domains; prefix re-check | Yes |
| 1.7 | CWE-59/367 | 0700 dir + lstat + ownership; true O_NOFOLLOW via python/perl helper | Partial (helper for O_NOFOLLOW) |
| 1.8 | CWE-22/73 | Canonicalize save root; require `${saveRoot}/<name>` with allowlisted name | Yes |
| 1.10 | Parity with `SanitizePathSegments` | Port algorithm to `scripts/lib/path-sanitize.sh` + self-tests | Yes |
| 3.3 | CWE-377/379 | `mktemp -d` + `chmod 700` + verify owner/mode; lock file inside | Yes |
| 8.2 | CWE-367 | No `-f/-r/-e` pre-checks; open directly and branch on result | Yes |
| 8.3 | CWE-59/367 | `[[ -L ]]` (lstat) helper before open; reject symlinks in trees | Yes |
| 8.6 | CWE-59 | Pre-flight `find -type l` on game/mod dirs; canonicalize parents | Yes |
| 8.7 | CWE-732/276 | Verify owner == current user and no group/world write; fail closed | Yes |

**Cross-cutting notes**:
- All controls are scriptable; the only non-fully-autonomous items are 1.5 and 1.7, solely due to the need for an external normalizer / `O_NOFOLLOW` helper (`python3`). Pre-flight should verify `python3` (or `uconv`) availability and **fail closed** when absent, consistent with checklist 8.1/4.3 style.
- Recommended implementation order: 8.2 → 8.3 → 8.7 → 3.3 → 1.6 → 1.3 → 1.5 → 1.10 → 1.8 → 8.6 → 1.7 (cheapest/highest-impact first; O_NOFOLLOW last as it needs the helper).
- Residual risk: bash cannot make check-then-open atomic; the 0700-directory + ownership + no-pre-check combination reduces but does not eliminate the TOCTOU window. For a per-user, non-privileged smoke script this is an accepted, documented residual risk (Wheeler: minimize time between check and use; here minimized to zero by removing the check).
- After implementing, update `specs/008-smoke-validation/checklists/security-safety.md` flipping each `[ ]` to `[x]` with a pointer to the implementing commit (process #6 atomic commit).
