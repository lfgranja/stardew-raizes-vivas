# Verification: CI / Release Gate (spec 019)

**Honest status:** green on this host (2026-11-08, `gate_v4_exit=0`, commit `4860cf2`). No hosted-CI green is claimed (host enablement required).

## Execution record (real, this host)

| Step | Command | Result |
|---|---|---|
| 0 | dirty-tree check | pass (`[release-gate …] clean tree: OK`) |
| 1 | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | **OK** 19:15:22 |
| 2 | `dotnet restore --locked-mode` | **OK** 19:15:27 |
| 3 | `dotnet build Stardew-LivingRoots.sln -c Release --no-restore -m:1 --disable-build-servers` | **OK** 19:16:59 (~93s) |
| 4 | `dotnet test Stardew-LivingRoots.sln -c Release --no-build` | **OK** 19:18:17 — 1937 passed / 4 skipped / 0 failed (1941 total, 1m8s) |
| 5 | `SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh` (TMPDIR outside repo) | **OK** 19:42:35 — 50 files, 49 passed, 0 failed, 1 skipped (`test-integration.sh` intentional) |
| 6 | `./scripts/verify-scenario.sh --check-docs` | **OK** 19:42:35 |
| 7 | `./scripts/check-protected-paths.sh` | **OK** 19:42:35 — "all checks passed" |
| — | Observation | ALL STEPS PASSED, `gate passed. Commit: 4860cf27858d1924acb0c09dd8ff7629d0e10183` |

Log: `/tmp/opencode/release-gate-pass14-v4.log`.

## Failure history (recorded, not hidden)

| Attempt | Outcome | Root cause & fix |
|---|---|---|
| gate v1 (`release-gate-pass14.log`) | fail at step 3 | `MSB4166`: MSBuild child node killed under memory pressure (4-way parallel + tight RAM). Fix: `-m:1 --disable-build-servers`; `--no-build` for step 4 |
| gate v2 (`release-gate-pass14-v2.log`) | died at startup, empty log | background process starved/dropped by host load; not a repo/content failure |
| gate v3 (`release-gate-pass14-v3.log`) | fail-closed exit 3 | working tree dirty (uncommitted T081 fix) — gate worked as designed; fix committed (`4860cf2`) |
| smoke (pass 12) | exit 1, `test-preflight` T060 ×2 | my `TMPDIR` inside repo made the "outside" fixture internal; not code |
| smoke (in-gate, pass 14) | exit 15 | `test-preflight` T081: `/proc/<pid>/exe` read raced the fork/exec; fixed with a bounded poll (`4860cf2`); `test-preflight` now **380/380** |
| gate v4 | **exit 0 — ALL STEPS PASSED** | above fixes held |

## Not done / not claimed

- Hosted CI (`Build and Test`) greenness; required checks; branch protection; auto-merge (quickstart host checklist; owner actions).
- Publication of any release artifact (tag push is the owner's).
- 014 `D1`/human-verified/SC-004 — owner (tonight), outside this feature.
- 018 residual (15 of 19 scenarios without 2× mutation/determinism).

## Reproduce

```bash
GamePath=/home/luis/stardew-assemblies scripts/release-gate.sh
```

## Fresh re-certification — 2026-11-09T14:00Z (current tree at `e567fe8`)

`GamePath=/home/luis/stardew-assemblies TMPDIR=/home/luis/.smoke-tmp-lrv SMOKE_SKIP_INTEGRATION=1 scripts/release-gate.sh`

| Step | Command | Result |
|---|---|---|
| 1 | `dotnet format ... --verify-no-changes` | OK 13:59:32Z |
| 2 | `dotnet restore --locked-mode` | OK 13:59:35Z |
| 3 | `dotnet build ... -m:1 --disable-build-servers` | OK 14:00:25Z |
| 4 | `dotnet test -c Release --no-build --verbosity minimal` | OK 14:01:06Z — **1937 passed / 4 skipped / 0 failed** (1941 total, 36s) |
| 5 | `SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh` | OK 14:24:41Z — **50 files, 49 passed, 0 failed, 1 skipped** (`test-integration.sh` intentional) |
| 6 | `./scripts/verify-scenario.sh --check-docs` | OK 14:24:42Z |
| 7 | `./scripts/check-protected-paths.sh` | OK 14:24:42Z — "all checks passed" |
| — | Observation | **ALL STEPS PASSED**, exit 0, **commit e567fe87b26c4d9b5eb50b4d505ea19c4e165a38** |

Full log: `/tmp/opencode/release-gate-pass24-v3.log`.

Notes:
- The `Aprovado!` line reports 1937 — consistent with the pre-README-refresh suite. The two `FeatureStatusTests` that failed in the first run today were caught by this step (full suite failed with 2/1941) and fixed in the same commit (`test(FR-016/SC-006 017): README taxonomy set atualizado`) — the gate working as designed.
- No code changed after this step; the fresh record captures `e567fe8` directly.
- No push/merge/tag was performed for this record.
