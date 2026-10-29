# Non-Blocking Advisory Lock Acquisition: Timeout & Backoff in CI/Headless Runners

**Date:** 2026-10-27
**Context:** Linux headless game-verification runner (Stardew Valley SMAPI mod). Short-lived advisory locks (`flock`/OFD) around shared resources (cache → build → run ordering). CHK018–CHK021 (concurrency checklist).
**Scope:** Determine concrete numeric bounds for non-blocking advisory lock acquisition: initial delay, max delay, total timeout, jitter, retry count. Linux `flock(2)`, `fcntl(2)` OFD locks, CI flakiness, deadlock avoidance.

---

## 1. Kernel Semantics (Authoritative)

### 1.1 `flock(2)` — BSD advisory locks
- Advisory only: "given suitable permissions on a file, a process is free to ignore the use of flock() and perform I/O on the file." — `flock(2)` man page (man7.org, Linux man-pages 6.19).
- `LOCK_NB` makes the request non-blocking; on conflict `errno = EWOULDBLOCK` (also `EAGAIN`).
- **No native timeout**: "A call to flock() may block if an incompatible lock is held by another process." There is no `timeout` argument; blocking is unbounded unless `LOCK_NB` is used.
- **No deadlock detection**: "flock() does not detect deadlock." — `flock(2)` man page. (Contrast `fcntl F_SETLKW`, which does.)
- Locks are associated with an **open file description**: released when all FDs to that description close; preserved across `execve(2)`.
- On Linux ≥2.0, `flock()` and `fcntl()` locks **do not interact** locally; over NFS (≥2.6.12) flock is emulated as byte-range locks, so they *do* interact; over SMB (≥5.5) locks become mandatory.
- Conversion (shared↔exclusive) is **not atomic**: existing lock removed then re-established; a pending request by another process may be granted in between.

### 1.2 `fcntl(2)` / OFD locks (`F_OFD_SETLK`)
- `F_OFD_SETLK` (non-blocking) returns `-1`, `errno = EAGAIN` on conflict; `F_OFD_SETLKW` blocks (interruptible by signal → `EINTR`).
- **No deadlock detection for OFD locks**: "In the current implementation, no deadlock detection is performed for open file description locks." — `fcntl_locking(2)`.
- OFD locks are FD-scoped (like `flock`), avoiding the process-associated close hazard of POSIX `F_SETLK` ("If a process closes *any* file descriptor referring to a file, then all of the process's locks on that file are released").
- POSIX.1-2024 (Linux-specific since 3.15).
- Both traditional and OFD locks are advisory by default; mandatory locking requires setgid-without-group-execute + `mand` mount and is "unreliable"/removed in Linux ≥5.15.

### 1.3 Consequence for a runner
Because **neither `flock` nor OFD locks have a native timeout or deadlock detection**, a runner must:
1. Use `LOCK_NB`/`F_OFD_SETLK` + a **bounded poll loop** with backoff.
2. Enforce a **global lock ordering** (cache → build → run) and never acquire in inverse order — the only deadlock prevention available.
3. Treat timeout as a distinct, retryable (or terminal) condition, never block indefinitely.

---

## 2. Why Non-Blocking + Backoff (Not Blocking)

- Blocking locks let "the kernel be a lot smarter: it can wake up your process immediately when the lock is available… avoids network spam on network filesystems" (community guidance).
- But blocking locks on NFS/SMB can hang forever on a wedged holder or a lost lock (NFSv4 leasetime 90s); a CI runner must never depend on a peer process's liveness.
- Non-blocking poll with backoff gives: bounded wait, distinct timeout exit code (util-linux `flock` uses `-E` conflict-exit-code, default 1), and jitter to avoid thundering herd when many self-hosted runners start together.
- Blocking `F_SETLKW` deadlock detection has false negatives (chains >10 deep) and false positives (clone CLONE_FILES); relying on it is unsafe.

---

## 3. CI Flakiness & Retry Guidance (Authoritative)

- **Google (Micco, 2016)**: flaky tests are inevitable at scale; rerun-once-and-pass is the common mitigation; keep failure information (don't silently drop). Mark flaky rather than delete; data-mine results.
- **Microsoft Azure DevOps flaky-test management**: system detection reruns failed tests within the same execution; a test that fails then passes on rerun is marked flaky; flaky tests can be suppressed from pass percentage.
- **AWS retry-with-backoff**: exponential backoff `delay = base * 2^(attempt-1)`, with **full jitter** `sleep = random(0, min(delay, cap))` to prevent thundering herd; treat timeout as unknown, not failed; cap retries.
- **Project precedent (spec 010)**: `RunnerMaxTransientRetries = 2` (3 attempts), fixed 5 s cooldown between attempts; lock contention (exit 6) is a retryable transient class. "2 retries reduce a 1% flaky rate to 0.0001% (Mill 2024); beyond 2 retries, cost grows 170%+ with diminishing returns."

Implication: lock-acquisition timeout itself must be short enough that a retry (5 s cooldown + re-attempt) stays within per-scenario budget, and long enough to ride out a legitimately slow holder on a loaded CI runner.

---

## 4. Deadlock Avoidance

1. **Global ordering** (project-defined): cache → build → run, by absolute path / numeric rank. Always acquire ascending; release in reverse. Never hold one lock while acquiring an unrelated one out of order. Document in code (CHK021).
2. **No nested locks across different resources** unless ordering is verified; prefer acquiring all needed locks up front in order, or fail fast.
3. **Short hold times**: lock only the critical section (shared resource mutation), never the whole run (CHK017 prohibits run-wide flock gate).
4. **Bounded wait + timeout** so a deadlocked/cyclic acquisition can't hang the runner; surface as exit 6 (lock contention) and let the scenario retry layer handle it.
5. **Avoid blocking syscalls** (`F_SETLKW`/`LOCK_EX` without `NB`) — they give no timeout and (for OFD) no deadlock detection.

---

## 5. Numeric Bounds — Derivation

Constraints:
- Holder critical sections are short (cache index update, build artifact claim): typically <1 s, occasionally 2–5 s on slow/loaded runners.
- CI runner wall-clock budgets are tight; a 30 s lock wait × 3 attempts = 90 s is too much. Target ≤ 10 s per acquisition attempt.
- Need enough retries to ride out a holder that is mid-operation (a few hundred ms to a couple seconds) without busy-spinning.
- Jitter must decorrelate simultaneous starters (self-hosted runners) without making worst-case waits unpredictable.

Selected scheme: **exponential backoff, factor 2, full-ish jitter, hard cap, bounded by both retry count and total timeout** (whichever comes first).

| Parameter | Value | Rationale |
|-----------|-------|-----------|
| Initial delay | **100 ms** | Below human perception; lets a holder finish a short critical section; avoids busy-spin (10–50 ms would hammer a loaded runner). |
| Backoff multiplier | **2×** | Standard exponential; matches AWS/Google retry guidance. |
| Max delay cap | **2 000 ms** | Caps tail latency; a holder still running after 2 s is likely stuck → fail fast rather than stall. |
| Total timeout | **10 000 ms** | ≤ per-attempt budget; 10 s is long enough for a legitimately slow holder, short enough that 3 scenario attempts stay within ~45 s. |
| Retry count (max attempts) | **8** (delays: 100, 200, 400, 800, 1600, 2000, 2000, 2000 ≈ 8.1 s ≤ 10 s) | Reaches the 10 s cap; 8 retries is the point of diminishing returns for lock contention (holder is either gone or stuck). |
| Jitter | **±25 %** (full jitter acceptable: `random(0, delay)`) | Decorrelates simultaneous starters; ±25 % keeps worst case bounded and testable; full jitter (`random(0, delay)`) is the AWS-recommended variant and may be used instead. |

Effective wait sequence (deterministic, no jitter): 100 + 200 + 400 + 800 + 1600 + 2000 + 2000 + 2000 = **9 100 ms** ≤ 10 000 ms. With ±25 % jitter the worst case is ≈ 11.4 s, still within scenario cooldown tolerance; total-timeout check is the authoritative bound (stop when elapsed ≥ `RunnerLockTotalTimeoutMs`, regardless of retry count).

---

## 6. Recommended ModConstants Values

```csharp
// Advisory lock acquisition (CI/headless runner, CHK018–CHK021).
// Non-blocking poll: LOCK_NB / F_OFD_SETLK + bounded backoff.
// Global lock order (deadlock prevention): cache -> build -> run.
public const int RunnerLockInitialDelayMs = 100;   // first backoff
public const int RunnerLockMaxDelayMs = 2000;      // cap per backoff step
public const int RunnerLockBackoffMultiplier = 2;  // exponential factor
public const int RunnerLockTotalTimeoutMs = 10000; // hard wall-clock bound
public const int RunnerLockMaxRetries = 8;         // attempts before timeout
public const int RunnerLockJitterPercent = 25;     // +/- 25% per step
```

Naming follows the existing `Runner*` prefix convention (e.g., `RunnerMaxTransientRetries`, `RunnerRetryCooldownMs`). All values are integers (ms / count / percent) to keep them `const` and testable, consistent with `CheckForOverflowUnderflow` and the no-magic-numbers rule.

---

## 7. Implementation Notes (Non-Normative)

- Prefer **OFD locks** (`F_OFD_SETLK`) where thread safety across subprocesses matters; `flock(2)` is acceptable for whole-file coordination. Avoid POSIX process-associated `F_SETLK` (close releases all locks).
- Advisory only — never mandatory locking (CHK018).
- Lock files on **local** filesystem only (CHK032); NFS/SMB caveats (flock emulated, locks may become mandatory over SMB).
- Atomic claim via `O_CREAT|O_EXCL|O_NOFOLLOW|O_CLOEXEC`, mode 0600 (CHK042); handle `EINTR` by retrying the same attempt, `EEXIST` by backing off.
- Record `{pid, hostname, starttime, cmdline-hash}` token; verify before any termination (CHK024/CHK029).
- Document every atomic op's failure modes (CHK045); maintain `CONCURRENCY.md` / traceability (CHK048).

---

## 8. Authoritative Sources

- `flock(2)` — Linux man pages (man7.org, 6.19): advisory-only, `LOCK_NB`, `EWOULDBLOCK`, no deadlock detection, no native timeout, open-file-description scope, NFS/SMB semantics.
- `fcntl(2)` / `fcntl_locking(2)` — Linux man pages (6.19): `F_OFD_SETLK`/`F_OFD_SETLKW`, `EAGAIN`, no deadlock detection for OFD, process-vs-OFD close hazard.
- `open(2)` / POSIX `open(3p)` — `O_CREAT|O_EXCL` atomicity, NFS O_EXCL caveat.
- POSIX.1-2024 — OFD lock type.
- Google Testing Blog — "Flaky Tests at Google and How We Mitigate Them" (Micco, 2016): rerun mitigation, mark-don't-delete, data mining.
- Microsoft Learn — "Manage flaky tests" (Azure DevOps): system detection via rerun; flaky suppression.
- AWS Prescriptive Guidance — "Retry with backoff" + Well-Architected REL05-BP03: exponential backoff, full jitter, retry caps.
- util-linux `flock(1)` — `-w seconds` (decimal) timeout, `-n` non-blocking, `-E` conflict exit code.
- Project: `research/concurrency-best-practices.md`, `specs/010-headless-verification-runner/spec.md` (FR-011, CHK017–CHK021), `research/flaky-test-handling-ci.md`.
- Mill (2024) cited in project spec: 2 retries → 0.0001% residual flake rate.

---

## 9. Decision Summary

Non-blocking advisory lock acquisition in CI must use `LOCK_NB`/`F_OFD_SETLK` with a bounded exponential-backoff poll loop (initial 100 ms, ×2, cap 2 000 ms, ≤ 8 retries, total ≤ 10 000 ms, ±25 % jitter), because neither `flock` nor OFD locks provide a native timeout or deadlock detection. Deadlock is prevented by documented global lock ordering (cache → build → run) and short hold times, not by kernel detection. Lock contention is a retryable transient class (exit 6) under the runner's existing 2-retry/5 s-cooldown layer.