# Claim records — 014 soil-health-crop-quality (018 pass 7, conservative)

**Scope:** derived strictly from `specs/014-soil-health-crop-quality/test.md` GATE3/claim and locally verified bundle presence. **No promotion above evidence.** D1/human/SC-004 never `complete`.

## Claim register

| claim | evidence_class | source_artifact_path | status | scope_notes |
|---|---|---|---|---|
| 014 host harvest matrix poor/moderate/healthy probes | `host-only-machine-pass` | `specs/014-soil-health-crop-quality/test.md` `evidence-bundle-run-20261106T014750Z-14b6` | complete | Runner-promoted GATE3, honesty R; bundle exists locally. |
| 014 host regression post-seam still pass | `host-only-machine-pass` | `specs/014-soil-health-crop-quality/test.md` `evidence-bundle-run-20261107T122915Z-2564` | complete | Cited in test.md claim; bundle exists locally. |
| 014 host NaN/unknown baseline → vanilla/no forced quality (A7) | `host-only-machine-pass` | `specs/014-soil-health-crop-quality/test.md` `evidence-bundle-run-20261107T123141Z-3531` | complete | Rerun clean; first attempt `122437Z-2f19` partial/no-dump disclosed in claim. Bundle exists locally. |
| 014 host measured-zero → Poor/force quality 0 (A7) | `host-only-machine-pass` | `specs/014-soil-health-crop-quality/test.md` `evidence-bundle-run-20261107T123257Z-08c9` | complete | Cited in test.md claim; bundle exists locally. |
| 014 farmhand guard / D1 transport | `d1-farmhand-convergence` | `specs/018-machine-pass-full-coverage/verification/t002-farmhand-host-guard-attempt.md` | blocked | Never machine-pass. |
| 014 human/visual/balance validation | `human-verified` | `specs/018-machine-pass-full-coverage/verification/final-inventory.md` | pending | Requires human session. |
