# Concurrency Primitives: Headless Game Verification Runner

**Feature**: 010-headless-verification-runner
**Created**: 2026-10-27
**Normative source**: FR-018/FR-019/FR-020, C-CON-01…C-CON-23
**Traceability**: `traceability.md` (CHK → test case → code location)

This document records the invariants, errno failure modes, retryability, and recovery for every concurrency primitive the runner uses. It is the implementation-facing companion to the requirements; where it disagrees with `spec.md`, the spec wins.

## 1. Display allocation (`-displayfd`)

| Property | Contract |
|----------|----------|
| Operation | Spawn `Xvfb -displayfd <fd>`; read the allocated display number from the fd |
| Invariant | Each instance owns exactly one display; the server allocates the number atomically; no `/tmp/.X*-lock` probe |
| Failure modes | `EAGAIN` (no free display) → display unavailable (exit 10, retryable); `EPIPE`/EOF before a number → internal error (exit 13); `ENOENT` (Xvfb missing) → missing dependency (exit 9) |
| Retryability | Exit 10 is retryable within the bounded attempt policy; exit 9/13 are not |
| Recovery | Kill the spawned process group; wait for the server to remove its socket; never unlink a socket the runner did not allocate |
| Cleanup assertion | `kill -0` ESRCH ≤100 ms; socket ENOENT in 2 consecutive 50 ms polls (about 50 ms total, not 100 ms) ≤500 ms; no `Z` state; `connect()` returns ENOENT/ECONNREFUSED ≤500 ms (C-CON-22) |
| Test cases | TC-CON-001..005, TC-CON-026..029 |
| Code | `scripts/verify-scenario.sh` (`display_start`, `display_stop`, `display_verify_cleanup`) |

Owned-display identity is `(display number, Xvfb PID, PID start-time token)`. Only a display whose token matches this instance may be force-unlinked. If the socket persists beyond 500 ms and the token does not match, the runner logs `orphaned-socket`, appends an entry to `orphaned-displays.json`, and MUST NOT unlink it.

## 2. Instance namespace and temp roots

| Property | Contract |
|----------|----------|
| Operation | `temp_root="$(mktemp -d)"`; `umask 077`; per-instance `XDG_CONFIG_HOME`, `HOME`, `XDG_DATA_HOME`, `XDG_STATE_HOME`, `XDG_CACHE_HOME` |
| Invariant | No shared writable state between instances; each instance has a unique instance ID and its own save copy. The single, explicit exception is the host-shared **claim root** (§5); `XDG_STATE_HOME`, `HOME`, and the other `XDG_*` variables remain per-instance for all state other than claims |
| Failure modes | `EEXIST`/`EACCES`/`ENOSPC` → errored (exit 19); `EXDEV` on rename → internal error (exit 13) |
| Retryability | Not retryable as a class; a fresh attempt may succeed if the cause was transient disk pressure |
| Recovery | Trap/finally removes the temp root; removal failure is observable in the run log; original save untouched |
| Test cases | TC-CON-012..016, TC-CON-030..037 |
| Code | `scripts/verify-scenario.sh` (`isolate_env`, `cleanup`) |

## 3. Save snapshot and restore

| Property | Contract |
|----------|----------|
| Operation | Hash original save (`sha256sum`) → copy to temp root → run → restore → re-hash and compare |
| Invariant | Original save is byte-identical after the run (SC-005); sidecars are backed up in two layers (enumerated + whole mod-data tree) |
| Failure modes | Hash mismatch → `errored` (exit 19), non-retryable; `ENOSPC`/`EACCES` during restore → original untouched, `errored`; `EXDEV` → copy+fsync fallback |
| Retryability | Non-identical save is never retried; it is a preservation breach |
| Recovery | Atomic temp+rename restore; on any pre-rename failure the original remains; before/after hashes recorded in the bundle |
| Test cases | TC-CON-006..011, TC-CON-038..041 |
| Code | `scripts/verify-scenario.sh` (`save_snapshot`, `save_restore`) |

## 4. Advisory resource locks

| Property | Contract |
|----------|----------|
| Operation | `flock`/OFD lock around shared cache/build resources only; never a whole-run gate |
| Invariant | Advisory only; no mandatory locking; global order `game-cache → build-output → shared-doc`; local filesystem only; there is no whole-run lock |
| Failure modes | `EWOULDBLOCK`/`EAGAIN` → poll with backoff; timeout at `RunnerLockTotalTimeoutMs=10000` → lock contention (exit 6, retryable); `ENOLCK`/`ENOTSUP` → internal error (exit 13) |
| Retryability | Exit 6 is retryable; unsupported locking is not |
| Recovery | Release in reverse order; bounded poll loop (initial 100 ms, ×2, cap 2000 ms, 8 attempts, ±25% jitter); OFD locks (`F_OFD_SETLK`) preferred over process-associated locks |
| Test cases | TC-CON-017..021, TC-CON-042..045 |
| Code | `scripts/lib/concurrency.sh` (`lock_acquire`, `lock_release`) |

## 5. Process-conflict claim

| Property | Contract |
|----------|----------|
| Operation | Atomic claim file created with `O_EXCL` + `O_CREAT` + `O_NOFOLLOW`, containing instance ID + PID identity token (starttime + cmdline hash) |
| Invariant | Check-and-claim is one operation; never signal a PID without token match; scoped to the instance's own game process; the claim root is shared across instances, owned by `EUID`, mode `0700` |
| Failure modes | `EEXIST` → conflict (exit 7, retryable after the owning process exits); `EACCES`/`ELOOP` → errored (exit 19); claim root not owned by the current `EUID` or not mode `0700` → errored (exit 19); stale token → remove claim only after liveness check |
| Retryability | Exit 7 is retryable; symlink/`ELOOP` is a security event and is not |
| Recovery | Unverified PIDs are left alone; stale claims are removed only when the identity token is provably dead |
| Test cases | TC-CON-022..025 |
| Code | `scripts/lib/concurrency.sh` (`claim_acquire`, `pid_identity`) |

**Claim namespace.** Claims live in a single host-shared coordination directory — the **claim root** — deliberately outside the per-instance XDG/HOME tree. Resolution order: (1) `RUNNER_CLAIM_ROOT` when set (absolute path required; relative or empty values exit `19`); (2) otherwise `/run/user/${UID}/livingroots-claims` when `/run/user/${UID}` exists; (3) otherwise `/tmp/livingroots-claims-${UID}`. The runner creates the root when absent with mode `0700`, owned by the current `EUID`. A pre-existing root that is not a directory, is a symlink, is not owned by the current `EUID`, or has any group/world permission bit set is `errored` (exit `19`) and MUST NOT be used. The claim root is an **explicit exemption from per-instance `XDG_STATE_HOME`/`HOME` isolation**: `XDG_STATE_HOME`, `HOME`, `XDG_CONFIG_HOME`, `XDG_DATA_HOME`, `XDG_CACHE_HOME`, and the instance temp root remain per-instance for all other state (§2), and nothing under the claim root may be read as instance state. A claim path is `<resourceId>.claim`, where `resourceId` is a SHA-256 of exactly one of: (a) the **resolved per-instance working-copy save path** (the copied `Saves/<Name>/` under the instance temp root) or (b) `display:<N>` for a caller-forced display. The **source** `saveId` and `milestoneSave` are read-only inputs shared by design; they are **not** claim keys, because each instance copies them into its own temp root. Two instances of the same scenario therefore do **not** conflict merely because they share a scenario definition, source save, or milestone save.

**Conflict predicate.** A conflict exists iff both hold: (1) the claim file's identity token is live — the PID exists, its `/proc/<pid>/stat` start-time equals the recorded start time, and the recorded cmdline hash equals the current cmdline hash; and (2) the `resourceId` is identical (same resolved working-copy save path or same forced display). Claim age is never a conflict condition: a live, overlapping claim is a conflict regardless of how long it has existed. Different working-copy paths and different displays are not conflicts even when they share a host or source milestone save. A dead or mismatched token makes the claim stale; `RunnerClaimStaleGraceMs=30000` applies only to stale-claim cleanup, not to conflict detection. Stale-claim removal requires a separate reclaim guard (`<claim>.reclaim`) created with `O_EXCL|O_CREAT|O_NOFOLLOW|O_CLOEXEC`; the runner MUST re-check the identity token while holding that guard, then `unlink` the stale claim and remove the guard. If the guard itself persists, it is treated as stale only after `RunnerReclaimGuardStaleMs=30000` **and** its recorded owner token is dead; a would-be reclaimer that cannot prove both exits `6` (lock contention, retryable) and MUST NOT unlink the guard. `O_EXCL` is creation-only and MUST NOT be described as guarding deletion.

**Residual risk — the claim root is coordination-only.** Because the claim root is host-shared and exempt from XDG/HOME isolation, it MUST never hold verdicts, saves, sidecars, evidence bundles, logs, caches, quarantine state, or any other instance output; it holds only short-lived `<resourceId>.claim` and `<claim>.reclaim` files. Anything found under the claim root is not evidence: the offline verifier and the promotion path MUST ignore it, and no verdict, honesty level, or GATE3 block may be derived from it. Conversely, the correctness of claim conflict detection depends on this one directory being genuinely host-shared; a host where the claim root cannot be created privately (read-only `/run/user/${UID}`, `/tmp` shared across mutually untrusted tenants, or `RUNNER_CLAIM_ROOT` pointing at per-instance storage) exits `19` rather than degrading to a per-instance claim directory, because a per-instance claim root silently disables cross-instance conflict detection while still appearing to work. The directory's `0700` mode bounds the threat to same-UID processes; a same-UID attacker is outside the claim model (see §9 residual risk). In the `/tmp` fallback the parent is world-writable, so another UID can pre-create or squat the name: that is a denial of service (the ownership check makes the runner exit `19`), never a compromise, because a pre-existing root is accepted only when it is a real directory owned by `EUID` with mode `0700`.

## 6. Atomic writes

| Property | Contract |
|----------|----------|
| Operation | `mkstemp` in the target directory → write → `fsync` → `rename` → `fsync` parent directory |
| Invariant | No torn writes; target unchanged on failure; temp unlinked on failure; modes 0600/0700 |
| Failure modes | `EINTR` → retry the syscall; `ENOSPC`/`EDQUOT` → errored; `EXDEV` → internal error (temp must be in the same directory) |
| Retryability | `EINTR` is retried inline; space/quota errors are not retryable |
| Recovery | Remove temp; leave target untouched; record the failure in the run log |
| Test cases | TC-CON-042..045 |
| Code | `scripts/lib/atomic.sh` (`atomic_write`) |

## 7. Orphan handling

| Property | Contract |
|----------|----------|
| Operation | Detect orphaned displays via `xdpyinfo` handshake; report only; reap the runner's own children |
| Invariant | Never signal or delete a display the runner did not allocate; never kill an unrelated PID |
| Failure modes | `ESRCH` → already gone; `EPERM` → report, do not escalate; `ETIMEDOUT` → report orphaned |
| Retryability | Detect-and-report only; no retry |
| Recovery | Process-group SIGTERM → SIGKILL escalation for owned children; orphan report in the evidence bundle |
| Test cases | TC-CON-026..029 |
| Code | `scripts/lib/concurrency.sh` (`orphan_registry`); `scripts/verify-scenario.sh` (`cleanup`, `reap_children`) |

**Orphan registry.** `orphaned-displays.json` is an append-only array inside the evidence bundle. Each entry records `display`, `socketPath`, `observedAtUtc`, `ownerPid` (or `null`), `ownerStartTime` (or `null`), `ownerTokenMatch` (boolean), `classification` (`dead-owner` | `token-mismatch` | `unresponsive`), and `action` (`report-only`). The `action` field is always `report-only`: the orphan registry records unknown-ownership displays, which the runner MUST NOT signal or delete. An owned display whose token matches this instance may be force-unlinked during normal cleanup (C-CON-22); that action is recorded in the run log, not in the orphan registry. The registry is never edited in place; a later run appends a new file/entry rather than rewriting history. Entries are informational and never trigger a signal to an unowned process.

## 8. Test strategy by layer

| Layer | Tooling | What it proves |
|-------|---------|----------------|
| Bash orchestration | Bash test scripts (`scripts/tests/test-*.sh`), `shellcheck` (static defects), a stress harness (`xargs -P` / `GNU parallel`) | Display/temp-root isolation, lock backoff, signal/reap behavior, cleanup idempotency, 30-instance stress (C-CON-21) |
| .NET domain/services | xUnit + Moq; Microsoft Coyote for writer-interleaving schedules; FsCheck for property-based invariants | SaveComparator determinism, atomic write ordering, state-machine invariants, interleaving-free verdicts |
| Cross-layer | Differential golden oracle + 1000-run stress harness | Correct, non-interleaved verdicts across K ≥ 30 concurrent instances |

Coyote and FsCheck are .NET-only; they do not test the bash orchestrator. Bash tests are the primary evidence for display allocation, temp-root isolation, lock contention, and process reaping; .NET tests are the primary evidence for persistence and comparator logic. Neither layer alone satisfies C-CON-21.

## 9. TOCTOU review record

**Review basis**: every concurrency primitive is reviewed for a check-then-act sequence. A primitive with no check-then-act sequence is recorded as "no TOCTOU surface" with a one-line justification; a primitive whose check and act are separated is recorded with its guard. Review date **2026-10-27, revised same day** for the host-shared claim root creation/permission check, process-group reaping, and the XDG containment probe; reviewer maintainer (`lfgranja`). The review is repeated whenever a primitive changes, and the review date advances on each revision even when no other prose changes.

| Primitive | Check → act | Finding | Disposition |
|-----------|-------------|---------|-------------|
| Display allocation (`-displayfd`) | Read allocated number → bind `DISPLAY` | No shared-path check; the X server allocates atomically | No TOCTOU surface; no lock-file probe |
| Display cleanup / socket unlink | `stat()` socket → `unlink()` | Socket existence can change between check and act | Unlink only after owned-token (PID + start-time) match; otherwise `orphaned-socket` + report-only |
| Claim root creation / permission check | `lstat()`/`stat()` root → create or open claim under it | The root can be swapped, pre-created by another UID, or be a symlink between the check and the claim `open()`; `RUNNER_CLAIM_ROOT` can change mid-run | Resolve the root once at startup; require an absolute path, a real directory (not a symlink), owner `EUID`, and exactly `0700`; create with `mkdir(0700)` under `umask 077` and `fstat()` the directory handle used for the claim; mismatch → `errored` (19), never a per-instance fallback |
| Claim acquisition | `open(O_EXCL)` → write token | Atomic creation; no check-then-act | No TOCTOU surface; `O_EXCL|O_CREAT|O_NOFOLLOW|O_CLOEXEC` |
| Stale-claim reclaim | Token check → `unlink()` | Token can change or a new owner can claim between check and unlink | Create `<claim>.reclaim` with `O_EXCL`, re-check token while holding it, then unlink; remove guard last |
| PID liveness / signal | `kill -0` → signal | PID can be reused between check and signal | Start-time + cmdline-hash token match required; unverified PIDs are never signalled |
| Temp-root creation | `mktemp -d` → use path | Atomic creation; path is private 0700 | No TOCTOU surface; `umask 077` |
| Save snapshot / restore | Hash source → copy/restore | Source or restore target can change during the operation | Hash before and after; atomic temp+rename restore; mismatch is `errored` (19) |
| Atomic write | `mkstemp` in target dir → write → `fsync` → `rename` → `fsync` parent | Target path can be swapped before rename | Temp in same directory; `O_NOFOLLOW` for attacker-influenced paths; target unchanged on failure |
| Orphan registry | Detect socket → append entry | Registry is append-only and informational | No signal or unlink; unknown ownership is report-only |
| Cleanup / trap | Check temp root → `rm -rf` | Path could be empty or swapped | `${tmpdir:?}` guard; owned 0700 path; failure recorded, exit code preserved |
| Process-group signal / reap | `kill(-pgid, 0)` → `kill(-pgid, SIGTERM/SIGKILL)` | PGID can be reused after the leader exits and before the signal; `waitpid` can race a recycled PID | Record each child's PID + start-time token; signal only the process group this instance created; re-verify the leader token before escalation; `waitpid` the known children; never signal an unverified PGID |
| XDG containment probe | Resolve `Constants.DataPath` → assert inside `XDG_CONFIG_HOME` | A symlink or late env change can redirect resolution between probe and first write | Resolve once before launch with `realpath`; reject symlinked path components; re-assert containment immediately before the first write and before restore; mismatch → `errored` (19) |
| Lock acquisition / release | Poll `flock`/OFD lock → proceed | Advisory lock has no check-then-act gap; timeout is authoritative | Bounded backoff; global lock order `game-cache → build-output → shared-doc`; no whole-run lock |
| Shared-doc read-merge-write | Read `test.md`/root docs → merge → atomic write | Another writer can change the file between read and write | Hold the short-lived `shared-doc` lock across read-merge-write; atomic rename |
| Verdict merge | Read per-instance verdict files → merge | Another instance can append between read and merge | Merge under the `shared-doc` lock; per-instance files are append-only until merged |

Residual risk: a privileged local attacker who can rewrite `/proc` or the runner's private temp tree is out of scope.

## 10. Traceability vocabulary

- **Mapped** means a checklist item has a named test case and code location in `traceability.md`; it does not mean the test exists or passes.
- **Covered** means the named test exists in the repository and has produced a passing result on the current commit.
- **Verified** means the behavior was observed in an end-to-end run with multi-signal evidence (spec 009 SC-009); it is never inferred from a unit test alone.

A checklist item may be `mapped` without being `covered`; only `covered` items are eligible for a `[x]` requirements-quality mark when the criterion is about implementation evidence. Requirements-quality criteria about the requirement text itself may be marked from the document alone.

## Traceability

The CHK → test case → code location matrix is in `traceability.md` §Concurrency. This file supplies the per-operation invariants and failure modes required by C-CON-15; the matrix is not duplicated here to avoid drift.
