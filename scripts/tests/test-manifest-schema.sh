#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

verifier="$HARNESS_REPO_ROOT/scripts/verify-evidence-bundle.sh"

make_bundle() {
    local dir="$1"; shift
    rm -rf "$dir"; mkdir -p "$dir/artifacts" "$dir/attempts/1"
    printf 'x' > "$dir/artifacts/soil.json"
    printf 'y' > "$dir/artifacts/crop.json"
    printf 'z' > "$dir/artifacts/smapi.txt"
    printf '{"attempt":1}\n' > "$dir/attempts/1/attempt.json"
    printf '{"schemaVersion":1,"runId":"run-20260101T000000Z-abcd","honestyLevel":"R","claims":[]}\n' > "$dir/claims.json"
    python3 - "$dir" <<'PY'
import hashlib, json, os, sys
bundle = sys.argv[1]
def sha(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()
SKIP = {"manifest.json", "manifest.sha256", ".manifest-input.json", ".attempts_list"}
artifacts = sorted(
    f"artifacts/{name}" for name in os.listdir(os.path.join(bundle, "artifacts"))
)
subject = []
for root, dirs, files in os.walk(bundle):
    dirs.sort()
    for name in sorted(files):
        if name.startswith("."):
            continue
        full = os.path.join(root, name)
        rel = os.path.relpath(full, bundle).replace(os.sep, "/")
        if rel in SKIP:
            continue
        subject.append({"name": rel, "digest": {"sha256": sha(full)}})
manifest = {
    "schemaVersion": 1,
    "runId": "run-20260101T000000Z-abcd",
    "timestamp": "2026-01-01T00:00:00Z",
    "toolVersions": {"runner": "0.1.0"},
    "gameVersion": "1.6.14",
    "modVersion": "0.4.0",
    "environment": {
        "gameVersion": "1.6.14",
        "smapiVersion": "4.4.0",
        "modVersion": "0.4.0",
        "apiVersion": "1.0.0",
        "saveId": "fixture",
    },
    "expectedSignals": ["soilHealthStateDelta", "cropPhaseDelta", "smapiLog"],
    "subject": subject,
    "verdict": "pass",
    "honestyLevel": "R",
    "attempts": [],
    "claimsFile": "claims.json",
    "artifacts": artifacts,
    "commands": [{"argv": ["fixture"]}],
    "materials": [{"uri": "file://fixture", "sha256": "0" * 64, "name": "fixture"}],
    "seeds": [{"seedId": "fixture", "source": "fixture"}],
}
with open(os.path.join(bundle, "manifest.json"), "w", encoding="utf-8") as f:
    json.dump(manifest, f, indent=2)
    f.write("\n")
PY
    printf '%s\n' "$(sha256sum "$dir/manifest.json" | awk '{print $1}')" > "$dir/manifest.sha256"
}

# 1. manifest not schema-valid (missing required field) => 19
d="$HARNESS_SANDBOX/ms1"
make_bundle "$d"
python3 - "$d" <<'PY'
import json, sys
bundle = sys.argv[1]
p = bundle + "/manifest.json"
data = json.load(open(p))
data.pop("timestamp", None)
json.dump(data, open(p, "w"), indent=2)
PY
printf '%s\n' "$(sha256sum "$d/manifest.json" | awk '{print $1}')" > "$d/manifest.sha256"
rc=0; bash "$verifier" "$d" >/dev/null 2>&1 || rc=$?
if (( rc == 19 )); then pass "schema-invalid manifest rejected (19)"; else fail "schema-invalid manifest" "rc=$rc"; fi

# 2. manifest.sha256 mismatch => 19
d="$HARNESS_SANDBOX/ms2"
make_bundle "$d"
printf '%s\n' "0000000000000000000000000000000000000000000000000000000000000000" > "$d/manifest.sha256"
rc=0; bash "$verifier" "$d" >/dev/null 2>&1 || rc=$?
if (( rc == 19 )); then pass "manifest sha mismatch rejected (19)"; else fail "manifest sha mismatch" "rc=$rc"; fi

# 3. extra file not in subject[] => 19
d="$HARNESS_SANDBOX/ms3"
make_bundle "$d"
printf 'extra' > "$d/artifacts/extra.json"
python3 - "$d" <<'PY'
import hashlib, json, sys
bundle = sys.argv[1]
p = bundle + "/manifest.json"
data = json.load(open(p))
data["artifacts"] = sorted(set(data.get("artifacts", []) + ["artifacts/extra.json"]))
json.dump(data, open(p, "w"), indent=2)
PY
printf '%s\n' "$(sha256sum "$d/manifest.json" | awk '{print $1}')" > "$d/manifest.sha256"
rc=0; bash "$verifier" "$d" >/dev/null 2>&1 || rc=$?
if (( rc == 19 )); then pass "extra file not in subject rejected (19)"; else fail "extra file not in subject" "rc=$rc"; fi

# 4. fully valid bundle => 0
d="$HARNESS_SANDBOX/ms4"
make_bundle "$d"
rc=0; bash "$verifier" "$d" >/dev/null 2>&1 || rc=$?
if (( rc == 0 )); then pass "valid bundle accepted (0)"; else fail "valid bundle" "rc=$rc"; fi

# 5. evidence_write_manifest_input produces schema-valid manifest
rid2="run-20260101T000000Z-abcd"
b2="$HARNESS_SANDBOX/evroot/evidence-bundle-$rid2"
mkdir -p "$b2/artifacts"
printf 'x' > "$b2/artifacts/soil-health-before-after.json"
printf 'y' > "$b2/artifacts/crop-phase-dump.json"
printf 'z' > "$b2/artifacts/smapi-log.txt"
call_runner_func evidence_write_claims "$b2" "$rid2" "R"
assert_fn_code 0 "write claims for manifest input"
ctx2="$HARNESS_SANDBOX/manifest-context.json"
python3 - "$ctx2" "$rid2" "$HARNESS_REPO_ROOT" <<'PY'
import json, sys
path, rid, repo = sys.argv[1:4]
ctx = {
    "runId": rid,
    "timestamp": "2026-01-01T00:00:00Z",
    "scenario": f"{repo}/specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json",
    "verdict": "pass",
    "honestyLevel": "R",
    "gamePath": "",
    "milestoneSave": "farm-year1-day5",
    "modManifest": f"{repo}/LivingRoots/manifest.json",
    "commit": "0" * 40,
    "buildSkipped": True,
    "toolVersions": {"dotnet": "10.0.400"},
    "environmentOverrides": [],
}
json.dump(ctx, open(path, "w"), indent=2)
PY
mi2="$HARNESS_SANDBOX/manifest-input.json"
call_runner_func evidence_write_manifest_input "$b2" "$ctx2" "$mi2"
assert_fn_code 0 "manifest input assembled"
if python3 - "$HARNESS_REPO_ROOT" "$mi2" <<'PY'
import json, jsonschema, sys
repo, path = sys.argv[1:3]
schema = json.load(open(f"{repo}/specs/010-headless-verification-runner/contracts/evidence-bundle.schema.json"))
data = json.load(open(path))
jsonschema.Draft202012Validator(schema).validate(data)
PY
then
    pass "evidence_write_manifest_input output satisfies schema"
else
    fail "evidence_write_manifest_input output" "fails evidence-bundle.schema.json"
fi

finish_tests
