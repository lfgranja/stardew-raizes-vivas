# Claim records — 015 harmony-harvest-seam (018 pass 8, conservative)

**Scope:** derived strictly from `specs/015-harmony-harvest-seam/test.md` GATE3 + append-only notes and local `--verify-bundle` exit 0 (pass 8). **No promotion above evidence.** 015 SC-004 (D1 multiplayer / explicit Gate 3 completeness as defined by spec) remains **not satisfied** by host-only machine-pass alone.

## Claim register

| claim | evidence_class | source_artifact_path | status | scope_notes |
|---|---|---|---|---|
| 015 host 3-signal Harmony `Crop.harvest` seam scenario (runner-promoted GATE3) | `host-only-machine-pass` | `specs/015-harmony-harvest-seam/test.md` `evidence-bundle-run-20261107T210830Z-67f1` | complete | Runner-promoted GATE3, honesty R, 3 signals cited; bundle exists locally and `--verify-bundle` exit 0. Host-only; strong runtime evidence of seam, but does not satisfy full 015 SC-004/D1. |
| 015 HarvestQualityService seam unit tests (SC-001, no game runtime) | `automated` | `specs/015-harmony-harvest-seam/test.md` | complete | Append-only note cites `dotnet test --filter "FullyQualifiedName~HarvestQualityServiceTests"` → **17 passed, 0 failed** (2026-11-07). Not re-run in pass 8; log not linked as path token here — treat as documented automated evidence pending linked log path. |
| 015 Harmony registration observed in smapiLog (SC-003 signal) | `automated` | `specs/015-harmony-harvest-seam/test.md` `evidence-bundle-run-20261106T014750Z-14b6` | complete | Append-only note cites 014 harvest-matrix bundle log showing `Harvest Harmony patch registered`; bundle exists locally and verified OK in pass 5. Log evidence, not GUI/visual. |
| 015 SC-004 D1 multiplayer / full in-game Gate 3 completeness | `d1-farmhand-convergence` | `specs/015-harmony-harvest-seam/test.md` | blocked | Per test.md: D1 second player not satisfied; farmhand/split-screen blocked-feasible (T002). Do not promote 015 to `verified`. |
| 015 human/visual/balance validation | `human-verified` | `specs/018-machine-pass-full-coverage/verification/final-inventory.md` | pending | Requires human session. |
