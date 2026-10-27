# Concurrency & Save-Preservation Requirements Quality Checklist: Headless Game Verification Runner

**Purpose**: Validate that concurrency, isolation, and save-preservation requirements (display allocation, save copy isolation, XDG isolation, file locking, process conflict detection, orphan handling, temp cleanup, collision avoidance, backup/restore, atomic operations) are complete, clear, measurable, consistent, and covered.
**Created**: 2026-10-26
**Feature**: [spec.md](../spec.md)

**Review Ownership**: This checklist is a reviewer-owned requirements-quality review artifact. Mark an item `[x]` only when the reviewer determines the requirements-quality criterion is satisfied.

**Note**: Items test whether the *requirements themselves* are well-written — not whether the implementation works.

---

## Display Allocation Race-Freedom

- [x] CHK001 - Is the display allocation mechanism specified as atomic (server-side claim, no TOCTOU)? [Clarity, Spec §FR-020]
- [x] CHK002 - Is the absence of `/tmp/.X*-lock` probing required and the rationale documented? [Completeness, Spec §FR-020]
- [x] CHK003 - Are per-instance display numbers required (no shared display across instances)? [Measurability, Spec §FR-020]
- [x] CHK004 - Is cleanup of the allocated display (server-side socket removal) required and observable? [Coverage, Spec §FR-020]
- [ ] CHK005 - Is orphaned-display detection specified as detect-and-report (no signalling) due to no ownership token? [Clarity, Spec §FR-020]

## Save Copy Isolation & Byte-Identical Preservation

- [x] CHK006 - Is save copy isolation required (disposable copy, never the player's real save)? [Completeness, Spec §FR-018]
- [x] CHK007 - Is byte-identical preservation of the original save specified with a hash-before/hash-after assertion? [Measurability, Spec §SC-005]
- [x] CHK008 - Is the hash algorithm and tolerance defined (SHA-256 for saves, 0.0001f for float deltas in SaveComparator)? [Clarity, Spec §FR-012]
- [x] CHK009 - Is backup/restore of `Saves/<Name>/` and sidecars required before mutation? [Completeness, Spec §FR-018]
- [x] CHK010 - Is failure on non-identical save specified to produce an `errored` verdict with before/after state? [Coverage, Spec §FR-018]
- [x] CHK011 - Is atomic replace (write→fsync→rename→fsync) required for sidecar writes? [Measurability, Gap]

## XDG_CONFIG_HOME Isolation

- [x] CHK012 - Is `XDG_CONFIG_HOME` required to point to a per-instance temp dir (absolute path)? [Completeness, Spec §FR-018]
- [x] CHK013 - Is the temp root required to be created via `mktemp -d` (not predictable paths)? [Security, Spec §FR-020]
- [x] CHK014 - Is HOME redirection required if the game does not honor XDG vars? [Clarity, Gap]
- [x] CHK015 - Are per-instance temp dirs required to be cleaned up (trap/finally) with observable failure? [Coverage, Spec §FR-019]
- [x] CHK016 - Is the 0700 permission on runtime dirs required? [Security, Gap]

## File Locking & Mutual Exclusion

- [x] CHK017 - Is the run-wide `flock` explicitly prohibited as a whole-run gate (only short-lived locks around shared resources)? [Clarity, Spec §FR-020]
- [x] CHK018 - Are advisory locks required to cooperate (no mandatory locking relied upon)? [Consistency, Gap]
- [x] CHK019 - Is non-blocking lock acquisition with timeout required (no indefinite blocking)? [Measurability, Gap]
- [x] CHK020 - Are OFD locks preferred over process-associated locks for thread safety? [Clarity, Gap]
- [x] CHK021 - Is lock ordering documented to prevent deadlock? [Coverage, Gap]

## Process Conflict Detection

- [x] CHK022 - Is process conflict detection scoped to the instance's own game process (not any Stardew process)? [Completeness, Spec §FR-020]
- [ ] CHK023 - Is the conflict check required to be atomic (check-and-claim via O_EXCL or flock)? [Measurability, Gap]
- [x] CHK024 - Is PID liveness verification required before declaring a conflict? [Clarity, Gap]
- [x] CHK025 - Are distinct instance IDs required for concurrent instances? [Consistency, Gap]

## Orphan Handling

- [ ] CHK026 - Is orphaned-display detection specified as detect-and-report only (no signalling due to no ownership token)? [Clarity, Spec §FR-020]
- [x] CHK027 - Is child process reaping (wait, SIGTERM escalation) required? [Completeness, Spec §FR-019]
- [x] CHK028 - Is signal forwarding via process groups required for clean shutdown? [Coverage, Spec §FR-019]
- [x] CHK029 - Is stale-PID scanning specified with safety (no killing unrelated processes)? [Security, Gap]

## Temp Directory Cleanup

- [x] CHK030 - Is `mktemp -d` required (not deprecated `tempfile`)? [Clarity, Spec §FR-020]
- [x] CHK031 - Is cleanup required to be trap/finally-based with observable failure? [Measurability, Spec §FR-019]
- [x] CHK032 - Is temp dir creation required on local filesystem only (NFS locking caveats)? [Security, Gap]
- [x] CHK033 - Are temp dir permissions (0700) required? [Security, Gap]

## Concurrent Instance Collision Avoidance

- [x] CHK034 - Is a single unique instance namespace required (no shared writable run state; only shared docs under the short-lived `shared-doc` lock)? [Completeness, Spec §FR-020]
- [x] CHK035 - Is dynamic display allocation (not fixed numbers) required? [Clarity, Spec §FR-020]
- [x] CHK036 - Is sidecar isolation (distinct save copies) required via distinct temp roots? [Measurability, Spec §FR-020]
- [x] CHK037 - Are concurrent instances required to produce correct, non-interleaved verdicts? [Coverage, Spec §FR-020]

## Backup/Restore Guarantees

- [x] CHK038 - Is hash-verified backup required before any mutation? [Completeness, Spec §FR-018]
- [x] CHK039 - Is idempotent restore required (re-running produces same state)? [Measurability, Spec §FR-018]
- [x] CHK040 - Is atomic replace (write→fsync→rename→fsync) required for restore? [Clarity, Gap]
- [x] CHK041 - Is restore failure required to leave the original untouched? [Security, Spec §FR-018]

## Atomic Operations

- [x] CHK042 - Is atomic file creation (O_EXCL) required for lock/temp files? [Measurability, Gap]
- [x] CHK043 - Is fsync of data before rename and parent-directory fsync after rename required? [Clarity, Gap]
- [x] CHK044 - Is torn-write prevention required for all persisted artifacts? [Coverage, Gap]
- [x] CHK045 - Are atomic operations required to be documented with their failure modes? [Completeness, Gap]

## Cross-Cutting Consistency & Coverage

- [x] CHK046 - Are concurrency requirements consistent between local and CI modes? [Consistency, Spec §FR-023]
- [x] CHK047 - Are concurrency requirements covered by tests (property-based/fuzz)? [Coverage, Gap]
- [ ] CHK048 - Is there a traceability link from concurrency requirement → test case? [Traceability, Spec §SC-005]
- [ ] CHK049 - Are concurrency requirements reviewed for race conditions (TOCTOU)? [Security, Gap]

## Notes

- Items marked `[Gap]` indicate requirements-quality gaps where the spec does not currently define the criterion.
- Based on `xvfb-run(1)`, `Xvfb(1)`, `flock(2)`, `fcntl(2)`, `rename(2)`, `fsync(2)`, `tempfile(1)`, XDG Base Directory Spec v0.8, Principles of Chaos Engineering.