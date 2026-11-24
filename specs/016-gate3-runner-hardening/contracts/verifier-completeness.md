# Verifier Completeness Contract

**Feature**: 016-gate3-runner-hardening
**Scope**: Offline evidence-bundle verifier (`scripts/verify-evidence-bundle.sh`) and the detached `manifest.sha256` anchor.
**Related**: `specs/010-headless-verification-runner/contracts/evidence-bundle.schema.json` (subject, `$defs/fileEntry`), `specs/010-headless-verification-runner/contracts/exit-codes.md` (code 19).

## Rule

Every regular file in an evidence bundle MUST be covered by `subject[]` in `manifest.json`, with a matching SHA-256 digest. This is enforced as a **filesystem rule**, not a schema change: the schema's `subject` field set is unchanged and no schema version bump is required.

A file is "covered" iff there exists an entry in `manifest.get("subject")` whose `name` equals the file's bundle-relative path (forward slashes) and whose `digest.sha256` equals the recomputed digest of that file.

## SKIP set

The following bundle-relative paths are exempt from coverage because they are the manifest, its detached anchor, or the builder's own work files:

- `manifest.json`
- `manifest.sha256`
- `.manifest-input.json`
- `.attempts_list`

No other path is exempt. In particular, `retention-receipt.json`, any post-finalization file, any dotfile other than the two listed work files, and any file under `attempts/` other than those named in `attempts[]`/`subject[]` must be covered.

## Algorithm

1. Presence checks: `manifest.json`, `manifest.sha256`, `claims.json` must exist; otherwise exit `19`.
2. Detached anchor check: recompute SHA-256 of `manifest.json` and compare to `manifest.sha256`; mismatch exits `19`.
3. Schema validation of `manifest.json` and `claims.json` against the published schemas; any failure exits `19`.
4. Subject digest check (existing):
   - For every entry in `subject[]`, the referenced file must exist and its digest must match; otherwise exit `19`.
   - Every `artifacts[]` entry must appear in `subject[].name`; otherwise exit `19`.
5. **Completeness check (new):**
   - Enumerate every regular file under the bundle root recursively.
   - For each file, compute its bundle-relative path `rel` (forward slashes).
   - If `rel` is in the SKIP set, skip it.
   - Otherwise, require `rel` to be present in the set of `subject[].name` values; if absent, exit `19` with a message naming the uncovered file.
   - (Digest correctness for covered files is already enforced in step 4.)
6. Claims honesty / runId agreement checks; any mismatch exits `19` (or `21` for claim overreach).
7. Mandatory signal and honesty gating checks; failures exit `19`/`21` per existing rules.

The completeness check is a pure filesystem enumeration; it does not consult `artifacts[]`, `attempts[]`, or any schema field. Any file on disk that is not in the SKIP set and not in `subject[]` is a violation.

## Exit code on violation

`19` (`ERRORED`), per `specs/010-headless-verification-runner/contracts/exit-codes.md`. The refusal site names the uncovered file, e.g. `runner: error: class=ERRORED message=uncovered bundle file: retention-receipt.json`.

## Edge cases

- A new file type added to the bundle layout is rejected unless `subject[]` is regenerated to include it.
- A file written after finalization (for example `retention-receipt.json`) is detected as uncovered and rejected.
- Hidden/dot files other than `.manifest-input.json` and `.attempts_list` are treated as bundle files and must be covered; a stray dotfile therefore fails verification.
- Empty directories do not count as files and do not require coverage.
- The SKIP set is authoritative and minimal; widening it requires a contract update and is not a per-bundle decision.

## Producer alignment

`evidence_write_manifest_input` (in `scripts/lib/evidence.sh`) already enumerates bundle files with the identical SKIP set when assembling `subject[]`. The verifier's completeness check is the offline counterpart of that enumeration; the two must agree on the SKIP set. Any change to one requires the matching change to the other.