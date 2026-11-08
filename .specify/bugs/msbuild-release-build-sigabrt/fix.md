# Bug Fix: SIGABRT core dump in `dotnet build` (Release solution build)

- **Slug**: msbuild-release-build-sigabrt
- **Fixed**: 2026-11-08T18:58:00Z (superseding assessment attribution)
- **Assessment**: ./assessment.md
- **Status**: applied (pre-existing mitigation from pass 14, verified green; no code changed in this fix step)

## Summary

The assessment's preferred remediation — building the solution with `-m:1 --disable-build-servers` and running tests with `--no-build` instead of full-node rebuild — was already applied to `scripts/release-gate.sh` in pass 14 and verified green end-to-end (`release-gate-pass14-v4.log`: `ALL STEPS PASSED`, `gate_v4_exit=0`). The SIGABRT is a host orchestration/memory-pressure failure (MSBuild node killed; `MSB4166` on the unmodified command), not a Living Roots defect. Nothing in this fix modified production code or the test suite.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `scripts/release-gate.sh` | modified (pre-existing) | step 3 build: `-m:1 --disable-build-servers`; step 4 test: `--no-build`; comment explains MSB4166 sizing. Landed in commit `50e773b`. |
| `specs/019-ci-release-gate/verification/verification.md` | modified (pre-existing, pass 14) | failure history recorded (v1 MSB4166 → fixed with `-m:1`; v3 dirty-tree → committed; v4 green). |
| (this fix step) | — | no additional edits required by the assessment |

## Diff Highlights

From `scripts/release-gate.sh` (already in tree):

```bash
# -m:1 keeps a single MSBuild node: a parallel solution build can die with
# MSB4166 (child node killed) on memory-constrained hosts and that is not a
# code failure. --disable-build-servers avoids stale/corrupted server state.
# Determinism first.
GamePath="$GAME_PATH" dotnet build Stardew-LivingRoots.sln --configuration Release --no-restore -m:1 --disable-build-servers \
    || fail "step 3 failed: Release build" 13
```

## Tests Added or Updated

- None. The assessment itself lists no test addition (the failure mode is an MSBuild host orchestration crash, not an application/domain contract). The regression record is the gate's green run: `specs/019-ci-release-gate/verification/verification.md` (`ALL STEPS PASSED`), logs `/tmp/opencode/release-gate-pass14-v4.log`.
- If recurrence happens on a quieted host: capture `DOTNET_DbgEnableMiniDump=1` minidump for SOS analysis before any downgrade.

## Local Verification

- Commands run: `bash -n scripts/release-gate.sh` → OK; `./scripts/release-gate.sh --help` → prints usage; grep confirms step 3 `-m:1 --disable-build-servers` and step 4 `--no-build` in HEAD.
- Real run (pre-existing): `GamePath=/home/luis/stardew-assemblies scripts/release-gate.sh` → **exit 0**, steps: format OK (19:15:22Z), restore OK (19:15:27Z), build OK (19:16:59Z, ~93s), tests OK (19:18:17Z, 1937 passed / 4 skipped / 0 failed), smoke OK (19:42:35Z, 50 files / 49 passed / 0 failed / 1 skipped), docs OK, protected-paths OK → `ALL STEPS PASSED`, commit `4860cf2`. Source: `/tmp/opencode/release-gate-pass14-v4.log`.
- Manual checks: the reported core dump (PID 2621637, root-owned, 0600) is not readable by this user; MSBuild failure files `MSBuildTempnjuevI/MSBuild_pid-2621637_*.failure.txt` are 0 bytes confirmed (assessment's direct evidence).

## Deviations from Assessment

1. **Attribution fix (chronology is not as the assessment implies):** the `-m:1`/`--disable-build-servers` mitigation is in commit `50e773b` (spec 019 creation), **not** `4860cf2`. `4860cf2` carries the smoke T081 de-flake. The assessment conflated them; the fix report corrects the record.
2. **Order of operations:** the assessment was written **after** the remediation landed (pass 14, before `/speckit.bug.fix`), not before. The assessment's chronological premise is inverted; the outcome is unchanged because the fix was already verified.
3. **No new change in this fix step:** the assessment asked for nothing else; the fallback options (diagnostic env vars, downgrade) were not applied — the `-m:1` fix holds (`gate_v4_exit=0`).
4. Hosted-CI note: `.github/workflows/build_and_test.yml` builds on `ubuntu-latest` with sufficient RAM; I did not change it (needs `BUILDDEPS_*` host enablement and remains fail-closed/dormant by design).

## Follow-ups

- Pending `[NEEDS CLARIFICATION]` from the assessment (exact managed trigger for the ABRT) is still unresolved; acceptable: the fix is orchestration, and the gate green run bounds the impact.
- If the ABRT recurs under `-m:1` on a quieter host: collect a minidump and run `dotnet-dump analyze` with SOS (or reassess with a new slug) before any SDK downgrade.
- Host hygiene: this host was at 85%+ tmpfs and fully-used swap during the incident; a host-level cleanup (tmux/agent/idle processes or RAM upgrade) would reduce recurrence probability further — owner decision.
- Next suggested step: `/speckit.bug.test slug=msbuild-release-build-sigabrt` — decision on whether a dedicated verification record is worth keeping given the gate already covers the path.
