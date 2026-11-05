# Quickstart: Gate 3 Runner Hardening

**Feature**: 016-gate3-runner-hardening
**Date**: 2026-11-05

Validation/run guide for the hardening of the headless verification runner (spec 010). This document does not duplicate implementation details; see `spec.md`, `plan.md`, the 010 `contracts/`, and `data-model.md` for those.

## Prerequisites

### Launch-free (fake host)

- Bash 4+, `python3` with `jsonschema`, `sha256sum`, `jq` (optional but recommended), `mktemp`, `realpath`.
- Repository checkout at the feature branch; no game install required.
- Run from the repository root so relative paths resolve.

### Real host (machine-pass validation)

- Everything in `specs/010-headless-verification-runner/quickstart.md` § Prerequisites: Linux host with `Xvfb`, `gh`, `curl`, `sha256sum`, `jq`, `dotnet` SDK, `flock`, `mktemp`, `realpath`; a valid Stardew Valley 1.6.x + SMAPI 4.4.0+ install (or `BUILDDEPS_TOKEN` for acquisition); the mod built and deployed; `compat-matrix.json` and `gamepath.lock.json` present.
- A real-host save set containing the milestone save used by the scenario (e.g. `farm-year1-day5`).

## Invocation

Launch-free suite (all test files):

```bash
SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh
```

Single test file:

```bash
scripts/tests/run-all.sh test-save-preservation.sh
scripts/tests/run-all.sh test-signals.sh
scripts/tests/run-all.sh test-bundle-negative.sh
scripts/tests/run-all.sh test-evidence.sh
```

Real-host run (machine-pass attestation):

```bash
./scripts/verify-scenario.sh --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json
```

Offline verification of an existing bundle:

```bash
./scripts/verify-evidence-bundle.sh evidence-bundle-<run-id>
```

## Validation scenarios

### VS-H1 — Save-name alignment regression guard

**Goal**: prove `save_align_inner_name` cannot be removed without breaking the suite.

**Steps**:

1. Temporarily neutralize `save_align_inner_name` in `scripts/lib/save.sh` so it returns `0` without renaming (e.g. make its first statement `return 0`).
2. Run the save-preservation suite.
3. Restore `scripts/lib/save.sh` from git.
4. Re-run the suite.

**Commands**:

```bash
cp scripts/lib/save.sh /tmp/opencode/save.sh.bak
# (edit save_align_inner_name to return 0 on entry)
bash scripts/tests/test-save-preservation.sh            # expected: FAIL
cp /tmp/opencode/save.sh.bak scripts/lib/save.sh
bash scripts/tests/test-save-preservation.sh            # expected: PASS (all cases)
```

**Expected outcome**: the neutralized run fails on the "inner save file matches the folder name" / "old inner save-file name removed" cases; the restored run passes all cases (24/24).

### VS-H2 — Full SMAPI log capture regression guard

**Goal**: prove the filtered three-pattern excerpt cannot be reintroduced silently.

**Steps**:

1. Temporarily replace the body of `signals_smapi_log_excerpt` in `scripts/lib/signals.sh` with the legacy filter that keeps only elapsed/DayEnding/host-guard lines.
2. Run the signals suite.
3. Restore `scripts/lib/signals.sh`.
4. Re-run the suite.

**Commands**:

```bash
cp scripts/lib/signals.sh /tmp/opencode/signals.sh.bak
# (restore the legacy three-pattern grep filter in signals_smapi_log_excerpt)
bash scripts/tests/test-signals.sh                      # expected: FAIL
cp /tmp/opencode/signals.sh.bak scripts/lib/signals.sh
bash scripts/tests/test-signals.sh                      # expected: PASS
```

**Expected outcome**: the filtered run fails the "full log keeps the save-load failure" case (`File does not exist` dropped); the restored run keeps the full log and passes.

### VS-H3 — Manifest schema enforcement

**Goal**: prove a schema-invalid manifest is rejected with exit 19.

**Steps**:

1. Produce or reuse a valid bundle (e.g. from `test-evidence.sh`'s assembled bundle, or any `evidence-bundle-*`).
2. Remove a required field from `manifest.json` (e.g. `runId`).
3. Recompute the detached digest so the failure is a schema failure, not a digest mismatch.
4. Run the verifier.

**Commands**:

```bash
b=evidence-bundle-<run-id>
python3 - "$b/manifest.json" <<'PY'
import json, sys
p = sys.argv[1]
d = json.load(open(p))
d.pop("runId", None)
json.dump(d, open(p, "w"), indent=2)
PY
sha256sum "$b/manifest.json" | awk '{print $1}' > "$b/manifest.sha256"
./scripts/verify-evidence-bundle.sh "$b"; echo "rc=$?"   # expected: 19
```

**Expected outcome**: verifier prints a schema failure at the missing field and exits 19.

### VS-H4 — Subject completeness (uncovered file)

**Goal**: prove a bundle file not listed in `subject[]` is rejected with exit 19.

**Steps**:

1. Take a valid bundle.
2. Add a file that is not covered by `subject[]` (e.g. `retention-receipt.json` written after finalization).
3. Do not update `subject[]`.
4. Run the verifier.

**Commands**:

```bash
b=evidence-bundle-<run-id>
printf '{"receipt":true}\n' > "$b/retention-receipt.json"
./scripts/verify-evidence-bundle.sh "$b"; echo "rc=$?"   # expected: 19
```

**Expected outcome**: verifier reports the uncovered file and exits 19. After adding a matching `{name, digest}` entry to `subject[]` (and re-finalizing via `ManifestBuilder`) the same bundle verifies with exit 0.

### VS-H5 — Real-host run reaches machine-pass

**Goal**: prove the hardened runner completes a scenario on a real host and writes `machine-pass`.

**Steps**:

1. Run the scenario against a real host.
2. Confirm exit code 0.
3. Confirm the claim root and mirrors were promoted.
4. Verify the bundle offline.

**Commands**:

```bash
./scripts/verify-scenario.sh --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json
echo "rc=$?"                                            # expected: 0
grep -n "machine-pass" specs/004-playable-compost-loop/test.md
./scripts/verify-evidence-bundle.sh evidence-bundle-*    # expected: 0
```

**Expected outcome**: exit 0 with `flaky: false`; `test.md` promoted from `partial` to `machine-pass`; `ROADMAP.md` and `COMPLETION_SUMMARY.md` mirrored as `machine-pass`; bundle contains the full `artifacts/smapi-log.txt` (not a filtered excerpt); verifier exits 0.

## Exit codes

| Code | Meaning |
|------|---------|
| 0 | Valid bundle / successful run |
| 19 | Schema/digest/mandatory-evidence failure (including uncovered files after hardening) |
| 21 | Claim/honesty overreach |

## Links

- Spec: `specs/016-gate3-runner-hardening/spec.md`
- Plan: `specs/016-gate3-runner-hardening/plan.md`
- Contracts: `specs/010-headless-verification-runner/contracts/evidence-bundle.schema.json`, `specs/010-headless-verification-runner/contracts/claims.schema.json`
- Data model: `specs/010-headless-verification-runner/data-model.md`
- Runner quickstart (prerequisites, flags, VS-01..VS-10): `specs/010-headless-verification-runner/quickstart.md`
- Verification protocol: `VERIFICATION_PROTOCOL.md`
