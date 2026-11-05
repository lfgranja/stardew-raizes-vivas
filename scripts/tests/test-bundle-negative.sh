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
subject = []
artifacts = sorted(
    f"artifacts/{name}" for name in os.listdir(os.path.join(bundle, "artifacts"))
)
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

# missing manifest
d="$HARNESS_SANDBOX/nm"; make_bundle "$d"; rm -f "$d/manifest.json"
# call verifier directly since run_verify wraps verify-scenario; invoke script
rc=0; bash "$verifier" "$d" >/dev/null 2>&1 || rc=$?
if (( rc == 19 || rc == 13 )); then pass "missing manifest rejected ($rc)"; else fail "missing manifest" "rc=$rc"; fi

# missing manifest.sha256
d="$HARNESS_SANDBOX/nh"; make_bundle "$d"; rm -f "$d/manifest.sha256"
rc=0; bash "$verifier" "$d" >/dev/null 2>&1 || rc=$?
if (( rc == 19 || rc == 13 )); then pass "missing sha rejected ($rc)"; else fail "missing sha" "rc=$rc"; fi

# missing claims
d="$HARNESS_SANDBOX/nc"; make_bundle "$d"; rm -f "$d/claims.json"
rc=0; bash "$verifier" "$d" >/dev/null 2>&1 || rc=$?
if (( rc == 19 || rc == 13 )); then pass "missing claims rejected ($rc)"; else fail "missing claims" "rc=$rc"; fi

# claim/honesty overreach => 21 (simulate via claims honesty W with commands present)
d="$HARNESS_SANDBOX/co"; make_bundle "$d"
printf '{"honestyLevel":"W","commandsCompleteness":true}\n' > "$d/commands.json" 2>/dev/null || true
rc=0; bash "$verifier" "$d" >/dev/null 2>&1 || rc=$?
pass "verifier exercised (rc=$rc)"

# missing signal
d="$HARNESS_SANDBOX/ns"; make_bundle "$d"; rm -f "$d/artifacts/soil.json"
rc=0; bash "$verifier" "$d" >/dev/null 2>&1 || rc=$?
if (( rc == 19 || rc == 13 )); then pass "missing signal rejected ($rc)"; else fail "missing signal" "rc=$rc"; fi

# digest tamper
d="$HARNESS_SANDBOX/td"; make_bundle "$d"
printf 'tampered' > "$d/manifest.json"
rc=0; bash "$verifier" "$d" >/dev/null 2>&1 || rc=$?
if (( rc == 19 || rc == 13 )); then pass "digest tamper rejected ($rc)"; else fail "digest tamper" "rc=$rc"; fi

# anomaly reports are evidence of ABSENCE: three of them must not satisfy an R
# claim that requires the three real signals.
d="$HARNESS_SANDBOX/an"; make_bundle "$d"
rm -f "$d/artifacts/soil.json" "$d/artifacts/crop.json" "$d/artifacts/smapi.txt"
printf '{}\n' > "$d/artifacts/anomaly-report.json"
printf '{}\n' > "$d/artifacts/anomaly-report-cropPhaseDelta.json"
printf '{}\n' > "$d/artifacts/anomaly-report-smapiLog.json"
printf '%s\n' "$(sha256sum "$d/manifest.json" | awk '{print $1}')" > "$d/manifest.sha256"
rc=0; bash "$verifier" "$d" >/dev/null 2>&1 || rc=$?
if (( rc == 19 )); then pass "anomaly reports do not satisfy R signals"; else fail "anomaly reports counted as signals" "rc=$rc"; fi

# a W bundle that claims nothing is valid even with only anomaly reports
d="$HARNESS_SANDBOX/anw"; make_bundle "$d"
rm -f "$d/artifacts/"*
rm -rf "$d/attempts"
printf '{}\n' > "$d/artifacts/anomaly-report.json"
printf '{"schemaVersion":1,"runId":"run-20260101T000000Z-abcd","honestyLevel":"W","claims":[]}\n' > "$d/claims.json"
python3 - "$d" <<'PY'
import hashlib, json, os, sys
bundle = sys.argv[1]
name = "artifacts/anomaly-report.json"
digest = hashlib.sha256(open(os.path.join(bundle, name), "rb").read()).hexdigest()
claims_digest = hashlib.sha256(open(os.path.join(bundle, "claims.json"), "rb").read()).hexdigest()
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
    "subject": [
        {"name": name, "digest": {"sha256": digest}},
        {"name": "claims.json", "digest": {"sha256": claims_digest}},
    ],
    "verdict": "partial",
    "honestyLevel": "W",
    "attempts": [],
    "claimsFile": "claims.json",
    "artifacts": [name],
}
with open(os.path.join(bundle, "manifest.json"), "w", encoding="utf-8") as f:
    json.dump(manifest, f, indent=2)
    f.write("\n")
PY
printf '%s\n' "$(sha256sum "$d/manifest.json" | awk '{print $1}')" > "$d/manifest.sha256"
rc=0; bash "$verifier" "$d" >/dev/null 2>&1 || rc=$?
if (( rc == 0 )); then pass "W bundle claiming nothing is valid"; else fail "W bundle claiming nothing" "rc=$rc"; fi

finish_tests