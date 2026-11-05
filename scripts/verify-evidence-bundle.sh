#!/usr/bin/env bash
set -Eeuo pipefail
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
REPO_ROOT="${RUNNER_REPO_ROOT:-$(cd -- "$SCRIPT_DIR/.." && pwd -P)}"
source "$SCRIPT_DIR/lib/common.sh"
source "$SCRIPT_DIR/lib/evidence.sh"

if [[ $# -ne 1 ]]; then
    echo "runner: error: code=14 class=USAGE_ERROR message=missing bundle dir" >&2
    exit 14
fi
bundle="$1"
if [[ ! -d "$bundle" ]]; then
    echo "runner: error: code=19 class=ERRORED message=missing bundle" >&2
    exit 19
fi
# presence checks
if [[ ! -f "$bundle/manifest.json" ]]; then exit 19; fi
if [[ ! -f "$bundle/manifest.sha256" ]]; then exit 19; fi
if [[ ! -f "$bundle/claims.json" ]]; then exit 19; fi
# digest recompute: the detached anchor is the trust root
calc="$(sha256sum "$bundle/manifest.json" | awk '{print $1}')"
exp="$(tr -d '[:space:]' < "$bundle/manifest.sha256")"
if [[ "$calc" != "$exp" ]]; then exit 19; fi

# Structural validation against the published contracts (FR-015). Structural
# pass is not truth, but a bundle that fails its own schema is never valid.
schema_rc=0
python3 - "$REPO_ROOT" "$bundle" <<'PY' || schema_rc=$?
import hashlib
import json
import os
import sys

repo, bundle = sys.argv[1:3]
contracts = os.path.join(repo, "specs", "010-headless-verification-runner", "contracts")

try:
    import jsonschema
except Exception:
    # Fail closed: without the validator the bundle cannot be declared valid.
    print("runner: error: class=ERRORED message=jsonschema unavailable", file=sys.stderr)
    sys.exit(19)

def load(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)

try:
    manifest_schema = load(os.path.join(contracts, "evidence-bundle.schema.json"))
    claims_schema = load(os.path.join(contracts, "claims.schema.json"))
    manifest = load(os.path.join(bundle, "manifest.json"))
    claims = load(os.path.join(bundle, "claims.json"))
except Exception as exc:
    print(f"runner: error: class=ERRORED message=unreadable bundle: {exc}", file=sys.stderr)
    sys.exit(19)

for name, schema, data in (
    ("manifest.json", manifest_schema, manifest),
    ("claims.json", claims_schema, claims),
):
    errors = sorted(jsonschema.Draft202012Validator(schema).iter_errors(data),
                    key=lambda e: list(e.absolute_path))
    if errors:
        first = errors[0]
        where = "/".join(str(p) for p in first.absolute_path) or "<root>"
        print(f"runner: error: class=ERRORED message={name} fails schema at {where}: {first.message}",
              file=sys.stderr)
        sys.exit(19)

def sha256(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()

subject = manifest.get("subject") or []
names = {entry["name"] for entry in subject}
for entry in subject:
    full = os.path.join(bundle, entry["name"])
    if not os.path.isfile(full):
        print(f"runner: error: class=ERRORED message=subject file missing: {entry['name']}", file=sys.stderr)
        sys.exit(19)
    if sha256(full) != entry["digest"]["sha256"]:
        print(f"runner: error: class=ERRORED message=subject digest mismatch: {entry['name']}", file=sys.stderr)
        sys.exit(19)

for artifact in manifest.get("artifacts") or []:
    if artifact not in names:
        print(f"runner: error: class=ERRORED message=artifact not in subject: {artifact}", file=sys.stderr)
        sys.exit(19)

if manifest.get("claimsFile") != "claims.json":
    print("runner: error: class=ERRORED message=claimsFile must be claims.json", file=sys.stderr)
    sys.exit(19)

# claims honesty and manifest honesty must not disagree.
if claims.get("honestyLevel") != manifest.get("honestyLevel"):
    print("runner: error: class=ERRORED message=honestyLevel mismatch", file=sys.stderr)
    sys.exit(19)
if claims.get("runId") != manifest.get("runId"):
    print("runner: error: class=ERRORED message=runId mismatch", file=sys.stderr)
    sys.exit(19)

sys.exit(0)
PY
if (( schema_rc != 0 )); then exit 19; fi

# mandatory signals: an anomaly report is evidence of ABSENCE, so it must never
# satisfy the presence check. W claims launch/connect only; R/E require all
# three signal kinds as real artifacts.
sig_rc=0
python3 - "$bundle" <<'PY' || sig_rc=$?
import glob,json,os,sys
b=sys.argv[1]
try:
    with open(os.path.join(b,"claims.json")) as f:
        hl=json.load(f).get("honestyLevel","W")
except Exception:
    sys.exit(19)
if hl in ("R","E"):
    names=[os.path.basename(p).lower() for p in glob.glob(os.path.join(b,"artifacts","*"))
           if os.path.isfile(p) and os.path.getsize(p)>0]
    names=[n for n in names if not n.startswith("anomaly-report")]
    def has(*keys):
        return any(any(k in n for k in keys) for n in names)
    if not (has("soil","delta") and has("crop","phase","dump") and has("smapi","log","excerpt")):
        sys.exit(19)
sys.exit(0)
PY
if (( sig_rc != 0 )); then exit 19; fi

# claim/honesty overreach
if grep -q 'overreach' "$bundle/claims.json" 2>/dev/null; then exit 21; fi

# honesty gating: R/E must carry commands, materials and at least one seed in
# the manifest (FR-014). The schema enforces presence; this enforces non-empty
# and refuses an `E` claim the runner can never make.
python3 - "$bundle" <<'PY'
import json,sys,os
b=sys.argv[1]
claims=json.load(open(os.path.join(b,"claims.json")))
manifest=json.load(open(os.path.join(b,"manifest.json")))
hl=claims.get("honestyLevel","W")
if str(hl).lower() in ("verified","v"):
    sys.exit(21)
if hl == "E":
    # E is human-assigned only; a runner-produced bundle never carries it.
    sys.exit(21)
if hl in ("R","E"):
    for name in ("commands","materials","seeds"):
        value = manifest.get(name)
        if not isinstance(value, list) or not value:
            sys.exit(19)
sys.exit(0)
PY
rc=$?
exit $rc
