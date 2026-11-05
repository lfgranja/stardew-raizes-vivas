# Research: Gate 3 Runner Hardening

**Feature**: `specs/016-gate3-runner-hardening`
**Phase**: 0 (research consolidation)
**Sources**: `.specify/assessments/runner-repair-gate3/{intake,research,problem,concept,decision}.md`; `specs/016-gate3-runner-hardening/spec.md`; runner scripts below.

This document resolves the unknowns carried from the assess pipeline. Each section gives a **Decision**, **Rationale**, and **Alternatives considered**, plus filesystem references. Project-relative paths are used for docs; absolute paths for filesystem references are given where a concrete file is cited.

---

## Summary: already implemented vs. new (hardening)

### Already implemented (commits `4b8452d`..`44571eb`, machine-pass achieved 2026-11-05)

- Save-name alignment: `scripts/lib/save.sh` `save_align_inner_name()` (single non-sidecar candidate renamed to match folder; `_old` companion handled) invoked from `save_isolate()` and verified by `save_prepare()` (`[[ -f "$SAVE_WORKING_DIR/$milestone" ]]` → loud `EXIT_ERRORED`).
- Full SMAPI log: `scripts/lib/signals.sh` `signals_smapi_log_excerpt()` now `cp -f "$log" "$out"` (function name retained for contract compat).
- Manifest as full FR-014 payload: `scripts/lib/evidence.sh` `evidence_write_manifest_input()` walks the bundle and builds `subject[]`, `artifacts`, `attempts`, `environment`, `commands`, `materials`, `seeds`, etc.
- Claims precede manifest: `evidence_write_claims()` writes `claims.json` before manifest assembly (FR-006).
- Single producer + detached anchor: `evidence_finalize()` invokes `ManifestBuilder` (`LivingRoots.Verification.Runner`), then `retention_receipt`/`retention_export`.
- Schema + digest + claims honesty + runId enforced: `scripts/verify-evidence-bundle.sh` (jsonschema `Draft202012Validator`, detached `manifest.sha256` recompute, `artifacts ⊆ subject`, honesty agreement).
- Original `HOME` preserved for Python helpers importing user-installed modules: `scripts/verify-scenario.sh` captures `RUNNER_ORIGINAL_HOME` before `save_prepare` rewrites `HOME`, and invokes the verifier with `HOME="${RUNNER_ORIGINAL_HOME:-$HOME}"` (commit `97b2934`).
- Promotion mirrors `machine-pass` to `ROADMAP.md`/`COMPLETION_SUMMARY.md`/`specs/004-playable-compost-loop/test.md`.

### New (hardening scope of spec 016)

- Regression tests asserting: save-name alignment fails when reverted (SC-003); full-log capture fails when excerpt reintroduced (SC-004); schema validity; subject completeness (SC-001).
- Verifier enforcement that **every bundle file** (not only artifacts) is covered by `subject[]` (FR-005) — currently only `artifacts ⊆ subject` is checked.
- Reordering of retention-receipt/export so post-finalization files cannot escape the anchor (see §4).
- Documented opt-in excerpt capture level for LLM diagnosis, with density ranking, full log retained as forensic secondary (FR-002).
- Red-gate deviation documentation in `specs/010-headless-verification-runner/red-gate-phase-*.log` (FR-007); no fabricated RED evidence.
- Optional CI invocation of the offline verifier (verifier remains the mandatory trust root).

---

## Unknown 1 — Save-name alignment: root cause and fix

**Decision**: Keep the alignment fix inside `save_align_inner_name()` and call it from `save_isolate()` (i.e., during `save_prepare`), not in the milestone packaging step. Do **not** generalize to multi-candidate guessing. Ambiguous layouts remain a loud `EXIT_ERRORED`/`19`.

**Rationale**: The milestone source must remain byte-identical (enforced by `SAVE_SOURCE_HASH_BEFORE == SAVE_SOURCE_HASH_AFTER` in `save_isolate()`); mutating the source at packaging time would break the source-preservation invariant and the hash check. Aligning at isolation time operates on the working copy only, so the source is never touched. The single-candidate rule keeps the operation deterministic: a native Stardew save is `<Saves>/<name>/<name>`, and a packaged milestone differs only by the inner file's original name (e.g. `farm-year1-day5/Agroeco_421179919`). Guessing among multiple candidates would risk renaming a sidecar or a non-save file; failing loud (`[[ ${#candidates[@]} -eq 1 ]] || return 0` leaves the file absent, then `save_prepare` returns `EXIT_ERRORED`) is the correct failure mode. The successful 2026-11-05 run (`evidence-bundle-run-20261105T024756Z-07ba`, verifier rc 0) confirms this root cause for the current milestone.

**Alternatives considered**:
- *Align at milestone packaging time* (rename both folder and inner file in the committed milestone): rejected — violates the read-only-source contract and the source-hash check; also bakes a runner-specific transform into committed game data.
- *Generalize `save_align_inner_name` to handle multiple candidates via heuristics (size, extension, content sniffing)*: rejected — introduces ambiguity and a new failure class; the loud-failure path is preferred. If a second blocker appears behind this one, the full SMAPI log (Unknown 2) surfaces it.
- *Skip alignment and rely on `SaveGame.Load` error handling*: rejected — already observed to return to title silently, which is exactly the misdiagnosis that blocked Gate 3.

**Filesystem refs**: `/home/luis/development/stardew-raizes-vivas/scripts/lib/save.sh` (`save_align_inner_name`, `save_isolate`, `save_prepare`).

---

## Unknown 2 — Full vs. configurable SMAPI log capture

**Decision**: Full capture is the **default and contract**. Any excerpting is an explicit, documented, opt-in capture level (never a silent filter). Size caps via density ranking apply **only to LLM diagnosis**, with the full log retained as a forensic secondary artifact. No size cap exists on `artifacts/smapi-log.txt` itself.

**Rationale**: The pre-fix bundle carried a three-pattern grep (~2 lines) that made a save-load failure look like a five-second stall and cost a multi-day diagnosis (assess `research.md`, "full-SMAPI-log gap"). Removing the filter (`cp -f "$log" "$out"`) resolved it. Keeping full capture as the default makes failures diagnosable from the bundle alone (problem.md goal). An opt-in excerpt level is acceptable for LLM-facing diagnosis output, but it must be a documented, named mode and must never replace the full log in the bundle. The edge case in spec.md ("What happens when the SMAPI log grows beyond a reasonable size?") is answered by documenting the capture level, not by silent truncation.

**Alternatives considered**:
- *Always excerpt with a larger window (e.g. N lines)*: rejected — any filter is a potential hiding place for the next root cause; the cost of a full log is disk, not correctness.
- *Configurable cap on the bundle artifact itself*: rejected — a cap would reintroduce the original defect class; caps are confined to the LLM diagnosis view.
- *Keep the function name `signals_smapi_log_excerpt` but document that it now copies the whole log*: accepted — contract compatibility is preserved; a rename would ripple through scenario contracts and tests for no benefit. The function's comment already records the change.

**Implementation note (new)**: add an explicit `RUNNER_SMAPI_CAPTURE` (or scenario-level) knob defaulting to `full`, with `excerpt` mode producing a density-ranked view as a *separate* artifact (e.g. `artifacts/smapi-log-excerpt.txt`) while `artifacts/smapi-log.txt` stays full. The verifier's signal check keys on `smapi-log.txt` presence/size (see `signals_all_present`, `signals_require_expected_signals`), so the full file remains mandatory.

**Filesystem refs**: `/home/luis/development/stardew-raizes-vivas/scripts/lib/signals.sh` (`signals_smapi_log_excerpt`, `signals_all_present`, `signals_require_expected_signals`).

---

## Unknown 3 — Manifest schema enforcement and verifier

**Decision**: Schema validation is a **hard gate in the offline verifier** (fail-closed on missing `jsonschema`), not CI-only. The verifier is the trust root for any promotion; CI may *invoke* the verifier but cannot substitute for it. The verifier validates manifest schema, digest, subject entries, claims/manifest honesty, and runId agreement before returning 0 (FR-004).

**Rationale**: `machine-pass` must be reproducible from a bundle alone on any host, including offline/air-gapped review (problem.md success metrics: verifier rc 0 / 19). A CI-only check would make promotion host-dependent and would not catch a tampered or schema-invalid bundle received from another machine. Fail-closed on missing `jsonschema` (commit `97b2934`) ensures a missing validator can never be read as "valid". The schema (`contracts/evidence-bundle.schema.json`) is Draft 2020-12 with strict objects and a single producer (`ManifestBuilder`), which is the right contract shape for machine-verifiable evidence.

**Alternatives considered**:
- *CI-only schema validation*: rejected — breaks offline reproducibility and lets a bundle be promoted on a host that never ran the check.
- *Verifier warns but does not fail on schema errors*: rejected — would reintroduce "machine-pass over invalid evidence", the original defect.
- *Bundling jsonschema into the repo to remove the HOME coupling*: considered; current mitigation is preserving `RUNNER_ORIGINAL_HOME`. A vendored copy is a future option if the coupling becomes a maintenance hazard (assess research.md, "Evidence Against the Idea").

**Filesystem refs**: `/home/luis/development/stardew-raizes-vivas/scripts/verify-evidence-bundle.sh`; `/home/luis/development/stardew-raizes-vivas/specs/010-headless-verification-runner/contracts/evidence-bundle.schema.json`; `/home/luis/development/stardew-raizes-vivas/scripts/verify-scenario.sh` (HOME capture + verifier invocation).

---

## Unknown 4 — `subject[]` completeness (closing the retention-receipt gap)

**Decision**: The verifier must reject any bundle where a file exists that is not covered by `subject[]`. This is enforced as a **filesystem rule** in the verifier; **no schema version bump** is required unless the field set changes (clarification 2026-11-05). Additionally, `claims.json` is written before manifest assembly (already done, FR-006), and retention receipts/exports must be written **before** the manifest is finalized so they are included in the `subject[]` snapshot.

**Rationale**: The current verifier only checks `artifacts ⊆ subject`, so `retention-receipt.json` and any post-finalization file escapes the detached SHA-256 anchor (assess research.md, "manifest defect"; spec FR-005). Walking the bundle filesystem and requiring every regular file (excluding `manifest.json`, `manifest.sha256`, and dotfiles) to appear in `subject[]` with a matching digest makes the anchor tamper-evident for the whole bundle. Because the change is a verifier-side rule over existing fields, no `$id`/`schemaVersion` bump is needed; a bump is only warranted if `subject`'s shape or the required field set changes.

**Residual ordering gap to fix (new)**: in `evidence_finalize()`, `retention_receipt "$bundle"` and `retention_export` are invoked **after** `ManifestBuilder` produces `manifest.json`. Those writes happen after the `subject[]` snapshot was taken inside `evidence_write_manifest_input()`, so any file they create is uncovered. Hardening must either (a) call `retention_receipt`/`retention_export` before `evidence_write_manifest_input` and re-walk, or (b) re-run the subject walk immediately before `ManifestBuilder`. The cleanest is to finalize retention first, then regenerate the manifest input, then build.

**Alternatives considered**:
- *Require a schema version bump for subject completeness*: rejected by clarification — the field set is unchanged; only the verifier's rule changes.
- *Allow an allow-list of uncovered files (retention receipt, etc.)*: rejected — an allow-list reopens the gap for any new file type; the filesystem rule is stricter and self-extending.
- *Keep retention writes post-build and add them to subject manually*: rejected — duplicates the snapshot logic and risks drift; better to reorder.

**Filesystem refs**: `/home/luis/development/stardew-raizes-vivas/scripts/lib/evidence.sh` (`evidence_write_manifest_input`, `evidence_finalize`); `/home/luis/development/stardew-raizes-vivas/scripts/verify-evidence-bundle.sh` (subject checks); `/home/luis/development/stardew-raizes-vivas/scripts/lib/retention.sh`.

---

## Unknown 5 — Red-gate evidence reproduction

**Decision**: **Document the deviation; do not reconstruct or fabricate true RED gates.** The red-gate logs (`specs/010-headless-verification-runner/red-gate-phase-2..5.log`) are post-implementation green runs. Each log should state that implementation pre-existed and that genuine RED evidence cannot be reconstructed retroactively; the tests are **regression guards**, not TDD RED evidence. Follow the tdd-guardian policy: absence → unverified, never violation.

**Rationale**: Tests were written alongside or after implementation, so no failing-before-fix run exists to cite. Fabricating a "RED" run (e.g., reverting a fix and re-running) would misrepresent the test's origin even if it produced a failing line. The honest resolution is to mark the attestation as unverified at the RED-gate criterion rather than to invent evidence. This keeps the honesty ladder intact (problem.md non-goal: no automation raises a claim above `machine-pass`).

**On SC-002 tension**: spec.md SC-002 says every red-gate log contains at least one `not ok`/`FAIL` line. Under this decision, that criterion is reinterpreted as "contains a documented deviation marker (and, where a genuine failing assertion was captured without fabrication, that line)". Do not add synthetic `FAIL` lines. If a real failing assertion was captured during the original repair (e.g., from `test-bundle-negative.sh`), it may be cited; otherwise the log is marked unverified.

**Alternatives considered**:
- *Reconstruct true RED gates by reverting each fix and re-running the suite*: rejected as the default — it produces a failing run that did not exist at the time and would be labeled as historical RED evidence. It remains an available *optional* exercise for new tests added under spec 016, where the RED step can be captured honestly before implementation.
- *Leave logs as-is*: rejected — silent green runs overstate the attestation; documentation of the deviation is the minimum honest action.
- *Delete the red-gate logs*: rejected — removes audit trail; the deviation note preserves traceability.

**Filesystem refs**: `/home/luis/development/stardew-raizes-vivas/specs/010-headless-verification-runner/red-gate-phase-{2,3,4,5}.log`; `/home/luis/development/stardew-raizes-vivas/scripts/tests/test-bundle-negative.sh`.

---

## Cross-cutting decisions

- **Scope**: Option A (harden the existing runner, no new capabilities) per assess `decision.md`. No generalization to other mods/specs before a second consumer exists (rule of three). T138 (second host) and T145 (farmhand path) remain environment-blocked and out of scope.
- **Honesty ladder preserved**: `verified` stays human-only (T148); no automation raises above `machine-pass`.
- **No schema version bump** for subject completeness; bump only if field set changes.
- **No input simulation** (spec 010 FR-006) — all verification remains file-queue + log based.

## Open items carried to planning

1. Implement the filesystem subject-completeness check in `scripts/verify-evidence-bundle.sh`.
2. Reorder retention writes before manifest finalization in `evidence_finalize()`.
3. Add `RUNNER_SMAPI_CAPTURE` opt-in excerpt mode producing a separate artifact.
4. Add regression tests in `scripts/tests/` (save alignment, full-log, schema, subject completeness).
5. Append deviation notes to the four red-gate logs.
6. Decide whether CI invokes the verifier (recommended: yes, but verifier remains trust root).