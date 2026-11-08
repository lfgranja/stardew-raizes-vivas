# Claim records — 004 playable-compost-loop (018 pass 7, conservative)

**Scope:** derived strictly from `specs/004-playable-compost-loop/test.md` GATE3/claim and locally verified bundle presence. **No promotion above evidence.** D1/human/SC-004 never `complete`.

## Claim register

| claim | evidence_class | source_artifact_path | status | scope_notes |
|---|---|---|---|---|
| 004 host compost growth poor 10 / 1.15× (hardening primary) | `host-only-machine-pass` | `specs/004-playable-compost-loop/test.md` `evidence-bundle-run-20261105T053313Z-535d` | complete | Runner-promoted GATE3, honesty R; bundle exists locally. |
| 004 host compost growth moderate 50 / 1.0× parity | `host-only-machine-pass` | `specs/004-playable-compost-loop/test.md` `evidence-bundle-run-20261107T120435Z-1ea9` | complete | Cited in test.md claim; bundle exists locally, `--verify-bundle` exit 0 (pass 3/5). |
| 004 host compost growth healthy 80 / 0.85× | `host-only-machine-pass` | `specs/004-playable-compost-loop/test.md` `evidence-bundle-run-20261107T120602Z-69ea` | complete | Cited in test.md claim; bundle exists locally, `--verify-bundle` exit 0 (pass 3/5). |
| 004 farmhand guard / D1 transport | `d1-farmhand-convergence` | `specs/018-machine-pass-full-coverage/verification/t002-farmhand-host-guard-attempt.md` | blocked | Never machine-pass. |
| 004 human/visual/balance validation | `human-verified` | `specs/018-machine-pass-full-coverage/verification/final-inventory.md` | pending | Requires human session. |
