# Claim records — 013 mulch-soil-cover (018 pass 7, conservative)

**Scope:** derived strictly from `specs/013-mulch-soil-cover/test.md` GATE3 + append-only notes and locally `--verify-bundle` exit 0 checks (pass 7). **No promotion above evidence.** `cropPhaseDelta` is explicitly `not-applicable`/reasoned for bare-tile v4 proofs (2 declared signals) — not fabricated. D1/human/SC-004 visual/farmhand never `complete` as machine-pass.

## Claim register

| claim | evidence_class | source_artifact_path | status | scope_notes |
|---|---|---|---|---|
| 013 host mulch 7-day expiry (SC-005, bare HoeDirt, v4 2-signal defensible) | `host-only-machine-pass` | `specs/013-mulch-soil-cover/test.md` `evidence-bundle-run-20261107T205338Z-08f8` | complete | Runner-promoted GATE3, honesty R; 50→49 (0.98), cropPhaseDelta not-applicable+reasoned; bundle exists and verify-bundle exit 0. Host-only; not D1/human. |
| 013 host bare-tile decay exemption (SC-004, 6 spring ticks, v4 2-signal defensible) | `host-only-machine-pass` | `specs/013-mulch-soil-cover/test.md` `evidence-bundle-run-20261107T205141Z-0508` | complete | Cited in append-only notes; verdict pass, soil 50→50 (1.0×), cropPhaseDelta not-applicable+reasoned; bundle verify-bundle exit 0. Also `205010Z-1fed` pass cited. |
| 013 host mulch apply on cropped tilled tile — no mutation + stable capture | `host-only-machine-pass` | `specs/013-mulch-soil-cover/test.md` `evidence-bundle-run-20261107T141941Z-18e6` | complete | Append-only note; 3-signal pass, soil 1.0×, LongPhaseCrop phaseDays stable (1-day). Control `142212Z-10d5` pass. Does not isolate bare-tile decay delta (honest caveat in test.md). |
| 013 host mulch refused on ineligible (already mulched, 2nd apply) | `host-only-machine-pass` | `specs/013-mulch-soil-cover/test.md` `evidence-bundle-run-20261107T142415Z-4e29` | complete | Append-only note; refusal attestable, no mutation. Bundle verify-bundle exit 0. |
| 013 farmhand guard (T028) | `d1-farmhand-convergence` | `specs/018-machine-pass-full-coverage/verification/t002-farmhand-host-guard-attempt.md` | blocked | Split-screen never machine-pass; T002 blocked-feasible. |
| 013 human/visual/balance validation | `human-verified` | `specs/018-machine-pass-full-coverage/verification/final-inventory.md` | pending | Requires human session. |
