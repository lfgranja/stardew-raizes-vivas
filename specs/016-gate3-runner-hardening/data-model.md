# Data Model: Gate 3 Runner Hardening

**Feature**: 016-gate3-runner-hardening
**Date**: 2026-11-05
**Schema**: `specs/010-headless-verification-runner/contracts/evidence-bundle.schema.json` (Draft 2020-12, `$id: urn:lfgranja:livingroots:evidence-bundle:v1`, immutable; `schemaVersion: 1`). No schema version bump is required for this feature: the `subject[]` completeness rule is enforced by the offline verifier as a filesystem rule (FR-005).

This document hardens entities that already exist in feature 010. No new state transitions are introduced; the change is to validation strictness and bundle coverage, not to entity shape.

## Entities

### 1. Evidence Bundle

The run directory `evidence-bundle-<runId>/` produced by every run. `runId` format: `run-<YYYYMMDDTHHMMSSZ>-<4 lowercase hex>` (C-BND-02). Layout per `specs/009-end-to-end-game-verification/contracts/evidence-bundle.md` plus the spec-010 additions.

| Path | Role | Covered by `subject[]`? |
|------|------|------------------------|
| `manifest.json` | Single-producer manifest (`ManifestBuilder` only). Detached anchor's trust root input. | No — excluded (SKIP). |
| `manifest.sha256` | Detached SHA-256 anchor of the finalized `manifest.json` bytes. | No — excluded (SKIP). |
| `claims.json` | Claim → evidence pointers, risk, honesty level, safe wording, refusal site; validated by `contracts/claims.schema.json`. | Yes. |
| `artifacts/` | Captured signal artifacts and supporting evidence (soil-health before/after, crop-phase dump, full SMAPI log, anomaly reports, cross-check). | Yes (each file). |
| `attempts/<n>/attempt.json` | Per-attempt metadata. | Yes (each file). |
| `attempts/<n>/manifest.json` | Per-attempt digest manifest. | Yes (each file). |
| `retention-receipt.json` | Retention/WORM receipt (append-only list of receipt entries). | Yes (after the chosen approach below). |
| `junit.xml`, `allure.json` | Optional CI/rich reports. | Yes (if present). |
| `integrity-failure.json` | Tamper-evident record on integrity failure. | Yes (if present). |
| `.manifest-input.json`, `.attempts_list` | Builder work files (dot-prefixed). | No — excluded (SKIP). |
| `attempts/warmup/` | Discarded warmup directory; not an attempt. | No — not a bundle member; must not exist in a finalized bundle. |

Rules:
- The bundle directory component must equal `manifest.runId` (`evidence-bundle-<runId>/`).
- `runId` must match between `manifest.json` and `claims.json`.
- `honestyLevel` must match between `manifest.json` and `claims.json`.
- A missing `manifest.json`, `manifest.sha256`, or `claims.json` yields `errored` (exit 19).

### 2. Manifest (subject[] coverage)

The schema-validated, single-producer artifact. `ManifestBuilder` is the **only** component allowed to create or modify `manifest.json` and `manifest.sha256`. The manifest is validated by `scripts/verify-evidence-bundle.sh` against `contracts/evidence-bundle.schema.json` and the detached anchor is recomputed before any promotion.

Completeness rule (FR-005, verifier-enforced filesystem rule, no schema bump):

> Every regular file in the bundle directory **except** `manifest.json`, `manifest.sha256`, and builder work files (dot-prefixed files such as `.manifest-input.json` and `.attempts_list`) MUST appear in `subject[]` with a matching `{name, digest:{sha256}}` entry. Any file not covered causes the verifier to return 19.

This closes the gap where `retention-receipt.json` (and any post-finalization file) escaped the detached SHA-256 anchor.

Producer invariants:
- `manifest.sha256` is computed over the finalized manifest bytes and written **after** the manifest is closed; a producer that writes the anchor first is invalid.
- `claims.json` is written before manifest input assembly (FR-006), so it is captured by the `subject[]` walk.
- `subject[]` entries use the `{name, digest:{sha256}}` shape (never `{path, sha256}`); `files` is a deprecated alias with the same shape.

Cross-field rules enforced by the runner/verifier (not expressible in JSON Schema): verdict/honesty/claim consistency, mandatory signal artifacts for a pass, R/E evidence completeness, `E` requiring human attestation, and `manifest.sha256` matching the manifest bytes.

### 3. Subject Entry

A `{name, digest}` pair covering one bundle file.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `name` | string (relative path, no `..`, no leading `/`) | yes | Bundle-relative path, e.g. `artifacts/smapi-log.txt`, `claims.json`, `retention-receipt.json`. |
| `digest` | object `{sha256}` | yes | Lowercase hex SHA-256 (64 chars) of the file bytes. |

Validation:
- `name` must resolve to an existing regular file inside the bundle.
- Recomputed SHA-256 of that file must equal `digest.sha256`.
- `artifacts[]` entries must be a subset of `subject[].name` (already enforced).
- `subject[]` must be complete per the completeness rule above.

### 4. Save Alignment

The isolated save's folder name and inner save file name must agree. Stardew resolves a save as `<Saves>/<name>/<name>`: the folder and the main save file must share a name. A milestone packaged by renaming only the folder (e.g. `farm-year1-day5/Agroeco_421179919`) launches fine and then fails with `DebugOutput: File does not exist` when `SaveGame.Load` runs (FR-001).

Entity: the working copy save directory `<working>/<save_name>/` containing the main save file `<save_name>` (plus sidecars `SaveGameInfo`, `SaveGameInfo_old`, `*_old`, `*.json`, `*.xml`).

Operation `save_align_inner_name <working_dir> <save_name>`:
- If `$working_dir/$save_name` already exists, no-op (success).
- Otherwise collect the single non-sidecar regular file in `$working_dir` (sidecars matching `SaveGameInfo`, `SaveGameInfo_old`, `*_old`, `*.json`, `*.xml` are skipped).
- If exactly one candidate exists, rename it to `$save_name` (and rename `${candidate}_old` to `${save_name}_old` if present).
- If zero or more than one candidate exists, leave the layout untouched; `save_prepare` then fails loudly with a named cause (missing `$working_dir/$save_name`) rather than guessing.

Validation:
- After `save_prepare`, `$SAVE_WORKING_DIR/$milestone` must exist as a regular file; otherwise `save_prepare` returns `errored` (exit 19).
- The source milestone save is never mutated; only the working copy is aligned.
- `HOME` (and the other XDG roots) are rewritten by `save_setup_env` to the instance tree; Python helpers that import user-installed modules must restore the original `HOME` (FR-008) — the verifier already does this via `HOME="${RUNNER_ORIGINAL_HOME:-$HOME}"`.

### 5. SMAPI Log Capture

The bundle artifact `artifacts/smapi-log.txt` (FR-002).

- Default: **full copy** of the SMAPI log (`SMAPI-latest.txt`) into `artifacts/smapi-log.txt`. The capture function is named `signals_smapi_log_excerpt` for contract compatibility but performs a full `cp` (never a silent filter).
- Any excerpting must be an explicit, documented, opt-in capture level — never a silent filter. Size caps via density ranking apply only to LLM diagnosis; the full log is retained as a forensic secondary artifact.
- The verifier's mandatory-signal check requires a non-empty `artifacts/smapi-log.txt` for an `R`/`E` claim.

Validation:
- An `R`/`E` bundle whose `artifacts/smapi-log.txt` is a filtered excerpt (or absent) is rejected or flagged by the verifier (exit 19).
- The verifier's signal check (`signals_all_present` / `signals_require_expected_signals`) treats the exact filename `smapi-log.txt` as the signal artifact; anomaly reports never satisfy it.

## Validation Rules (summary)

1. `subject[]` must cover every bundle file except `manifest.json`, `manifest.sha256`, and builder work files (dot-prefixed). Enforced by the offline verifier as a filesystem rule; violation → exit 19 (SC-001).
2. `artifacts ⊆ subject` (already enforced).
3. Every `subject[]` entry's digest must match the recomputed SHA-256 of the referenced file; missing file → exit 19.
4. `claims.json` is written before manifest input assembly, so `subject[]` covers it (FR-006).
5. `retention-receipt.json` must be covered (see chosen approach below).
6. `manifest.json` must satisfy `contracts/evidence-bundle.schema.json` (Draft 2020-12, strict).
7. `manifest.sha256` must equal `sha256(manifest.json)`; missing → exit 19.
8. `runId` and `honestyLevel` must agree between `manifest.json` and `claims.json`.
9. `claimsFile` must be `"claims.json"`.
10. Save alignment: folder name == inner save file name; `save_prepare` fails loudly (exit 19) if the aligned file is missing.
11. SMAPI log: full copy by default; any excerpting is explicit and documented.

## Retention Receipt Coverage — Chosen Approach

**Chosen: move retention-receipt generation before manifest input assembly.**

Rationale: `retention_receipt()` (in `scripts/lib/retention.sh`) currently runs inside `evidence_finalize()` **after** `ManifestBuilder` has closed the manifest, so `retention-receipt.json` is written post-finalization and can never be covered by the detached anchor. Generating it before `evidence_write_manifest_input()` (alongside `evidence_write_claims()`, per FR-006) means the receipt file exists when the `subject[]` walk runs and is anchored like every other bundle member. The post-finalization `retention_receipt()` call in `evidence_finalize()` must be removed (or guarded to a no-op when the receipt already exists) to avoid duplicate append-only entries; `retention_export()` may remain post-finalization because exports live outside the bundle. This keeps `ManifestBuilder` the single producer of `manifest.json`/`manifest.sha256` and requires no post-hoc manifest mutation.

Rejected alternative: appending `retention-receipt.json` to `subject[]` after finalization would require re-finalizing `manifest.json` and recomputing `manifest.sha256`, violating the single-producer/detached-anchor contract.

## SKIP Set (excluded from `subject[]`)

| File / pattern | Reason |
|----------------|--------|
| `manifest.json` | The manifest itself; its digest is the detached anchor. |
| `manifest.sha256` | The detached anchor; external trust root. |
| `.manifest-input.json` | Builder work file (dot-prefixed). |
| `.attempts_list` | Builder work file (dot-prefixed). |
| Any other dot-prefixed file | Builder work file by convention. |

All other regular files (including `claims.json`, `retention-receipt.json`, everything under `artifacts/` and `attempts/`, and optional `junit.xml`/`allure.json`/`integrity-failure.json`) MUST be in `subject[]`.

## In-Scope / Out-of-Scope Entities

- **In scope**: the five entities above plus their validation rules.
- **Out of scope**: new entity types, schema field additions, or state-machine changes. T138 (second host) and T145 (farmhand path) remain environment-blocked and are not modeled here.

## State Transitions

None new. This feature hardens existing entities (stricter `subject[]` coverage, full-log capture, save-name alignment, schema enforcement) without changing their lifecycle or state machines. Run verdict flow, quarantine flow, save preservation, and version gate transitions remain as defined in `specs/010-headless-verification-runner/data-model.md`.