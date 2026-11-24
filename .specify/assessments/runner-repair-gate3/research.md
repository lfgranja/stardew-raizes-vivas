# Idea Research: Runner repair / quality work that unblocked Gate 3

- **Slug**: runner-repair-gate3
- **Created**: 2026-11-05
- **Evidence confidence (overall)**: high (internal evidence is direct and reproducible; external claims are limited to general engineering practice)

## Users & Demand

- The maintainer of Living Roots is the sole user of the runner; the pain is concrete and measured: the real-host run reported `partial` with `signalsComplete=false`, and specs 014/015 were blocked on the machine-pass-first rule. — [internal: specs/010-headless-verification-runner/verification/real-host.log (pre-fix), specs/004-playable-compost-loop/test.md] (confidence: high, cited)
- The same maintainer spent a multi-day diagnosis cycle on a two-line SMAPI excerpt that read as a stall; the full log showed the real cause in minutes. — [internal: .specify/assessments/agent-desktop-usefulness/research.md, "full-SMAPI-log gap"] (confidence: high, cited)
- No second maintainer or CI runner is currently expected (open question carried from agent-desktop-usefulness problem.md). — [internal: .specify/assessments/agent-desktop-usefulness/problem.md, Open Questions] (confidence: medium, assumption)

## Prior Art

- **Internal — spec 010 (headless verification runner)**: the runner, the honesty ladder (partial / machine-pass / verified), the three-signal contract, and the promotion owner already existed; the repair did not add new capabilities, it made existing ones produce valid evidence. — [internal: specs/010-headless-verification-runner/spec.md, data-model.md] (confidence: high, cited)
- **Internal — the manifest defect**: every pre-fix bundle's `manifest.json` was `{"runId": ...}` and failed `contracts/evidence-bundle.schema.json` with 13 missing required fields; the offline verifier never validated the schema, so `machine-pass` was granted over an invalid bundle. — [internal: specs/010-headless-verification-runner/verification/schema.log (pre-fix section), evidence-bundle-run-*/manifest.json] (confidence: high, cited)
- **Internal — the save-load defect**: the isolated save folder was `farm-year1-day5` while the inner save file was `Agroeco_421179919`; `SaveGame.Load("farm-year1-day5")` logged `DebugOutput: File does not exist` and returned to title. The runner already shipped `save_align_inner_name` to rename the single non-sidecar file to match the folder. — [internal: .specify/assessments/agent-desktop-usefulness/research.md, specs/010-headless-verification-runner/verification/real-host.log] (confidence: high, cited)
- **Internal — the SMAPI excerpt defect**: `artifacts/smapi-log.txt` was a three-pattern grep, ~2 lines, which hid the load failure. Widened to the full log. — [internal: .specify/assessments/agent-desktop-usefulness/research.md] (confidence: high, cited)
- **External — JSON Schema as a bundle contract**: Draft 2020-12 with strict objects and a single producer is a well-established pattern for machine-verifiable evidence (jsonschema/ajv agreement as a cross-check). — [ASSUMPTION: general engineering practice; no external source fetched in this pass] (confidence: medium, assumption)
- **External — single-producer / detached-anchor pattern**: computing a detached SHA-256 over finalized bytes and refusing to promote on mismatch is standard for tamper-evident evidence. — [ASSUMPTION: general engineering practice] (confidence: medium, assumption)

## Market & Context

- The alternative to repairing the runner is manual Gate 3 verification by the maintainer, which is the exact cost spec 010 was created to remove. — [internal: .specify/assessments/agent-desktop-usefulness/problem.md, Cost of Inaction] (confidence: high, cited)
- No competing or off-the-shelf runner exists for SMAPI/Stardew Valley headless verification; the runner is bespoke. — [ASSUMPTION: no search performed in this pass] (confidence: low, assumption)
- The repair is cheap relative to the alternatives: it touched 6 scripts and 2 test files, and the real-host run completed in ~120 s. — [internal: git commit 4b8452d and surrounding commits] (confidence: high, cited)

## Data & Constraints

- Real-host run: exit 0, ~120 s wall clock, bundle `evidence-bundle-run-20261105T024756Z-07ba`, verifier rc 0. — [internal: verification/real-host.log] (confidence: high, cited)
- Test suite: 1630 tests, 1626 passed, 4 ignored, 0 failed; shell suite 43 files, 42 passed, 1 skipped, 0 failed. — [internal: verification/test.log, launch-free.log] (confidence: high, cited)
- Build: 0 errors, 14 pre-existing MSB3277 warnings, 94 s. — [internal: verification/build.log] (confidence: high, cited)
- Schema validation: 30/30 valid, 14/14 invalid (one error each), 2/2 runner-invalid schema-accepted, 0 validator mismatches. — [internal: verification/schema.log] (confidence: high, cited)
- Platform: Fedora 44 KDE/Wayland with Xvfb; game 1.6.14, SMAPI 4.4.0, mod 0.4.0. — [internal: verification/real-host.log] (confidence: high, cited)
- Constraint: `verified` stays a human attestation (T148); no automation may raise a claim above `machine-pass`. — [internal: specs/010-headless-verification-runner/spec.md, FR-016] (confidence: high, cited)

## Evidence Against the Idea

- The repair is reactive, not preventive: it fixes three specific defects found during T137 but does not add regression guards that would catch the next class of defect (e.g., a new bundle file not covered by `subject[]`). — [internal: scripts/verify-evidence-bundle.sh (only checks artifacts ⊆ subject, not completeness)] (confidence: high, cited)
- The manifest's `subject[]` is a snapshot taken before retention receipts are written; any file added to the bundle after finalization is not covered. This is a known residual gap. — [internal: scripts/lib/evidence.sh, evidence_finalize] (confidence: high, cited)
- The red-gate logs (red-gate-phase-2/3/4/5.log) are post-implementation runs, not true RED gates; T130 remains open and the repair did not address that. — [internal: .specify/bugs/test-harness-hangs/test.md, tasks.md T130] (confidence: high, cited)
- T138 (second host) and T145 (farmhand path) remain blocked, so the repair alone does not complete the Gate 3 attestation chain. — [internal: tasks.md T138, T145] (confidence: high, cited)
- The repair touches the verifier to import `jsonschema`, which requires restoring the original `HOME` under the isolated save environment; this coupling is a maintenance hazard. — [internal: scripts/lib/save.sh, scripts/verify-scenario.sh] (confidence: high, cited)

## Gaps & Open Questions

- [NEEDS CLARIFICATION: Should the save-load alignment fix live in `save_prepare`/`save_align_inner_name` or in the milestone packaging step that produced the mismatched names?]
- [NEEDS CLARIFICATION: Is widening the SMAPI excerpt to the full log a permanent contract change, or should it be a configurable capture level?]
- [NEEDS CLARIFICATION: Should manifest schema validation be a hard gate in CI, or only in the offline verifier?]
- [NEEDS CLARIFICATION: Is this repair scoped to spec 010 only, or should it become a general pre-flight quality gate for every spec in the mod (and other mods)?]
- [NEEDS CLARIFICATION: Which parts of this repair are generalizable beyond Stardew Valley / SMAPI, and which are domain-specific?]

## Sources

- Internal: `specs/010-headless-verification-runner/` (spec.md, data-model.md, tasks.md, verification/*, gate3-evidence/*) — host: local repo, policy: not applicable (local)
- Internal: `.specify/assessments/agent-desktop-usefulness/` (problem.md, research.md, concept.md, decision.md) — host: local repo
- Internal: git commits 42fd751, 166d637, 4b8452d, 405467d, 3d797bd, c870244, facb81c, 44571eb — host: local repo
- External: none fetched in this pass; external claims are tagged ASSUMPTION.