#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

schema_v2="specs/010-headless-verification-runner/contracts/scenario.schema.json"
schema_v3="specs/010-headless-verification-runner/contracts/scenario-v3.schema.json"
schema_v4="specs/010-headless-verification-runner/contracts/scenario-v4.schema.json"
assert_file_exists "$schema_v2" "scenario schema v2 exists"
assert_file_exists "$schema_v3" "scenario schema v3 exists"
assert_file_exists "$schema_v4" "scenario schema v4 exists"

_schema_for_fixture() {
    local f="$1"
    python3 - "$f" "$schema_v2" "$schema_v3" "$schema_v4" <<'PY'
import json,sys
f,v2,v3,v4=sys.argv[1:5]
try: sv=json.load(open(f,encoding='utf-8')).get('schemaVersion',2)
except Exception: sv=2
print({4:v4,3:v3}.get(sv,v2))
PY
}

# valid fixtures accepted (schema selected by schemaVersion)
if ls specs/010-headless-verification-runner/fixtures/valid/*.json >/dev/null 2>&1; then
    for f in specs/010-headless-verification-runner/fixtures/valid/*.json; do
        schema="$(_schema_for_fixture "$f")"
        if python3 -c "import json,jsonschema,sys; s=json.load(open(sys.argv[1])); d=json.load(open(sys.argv[2])); jsonschema.validate(d,s); sys.exit(0)" "$schema" "$f" 2>/dev/null; then
            pass "valid fixture accepted: ${f##*/}"
        else
            fail "valid fixture accepted: ${f##*/}" "rejected by ${schema##*/}"
        fi
    done
else
    skip "valid fixtures" "none present"
fi

# invalid fixtures rejected (schema selected by schemaVersion; validate against declared schema)
if ls specs/010-headless-verification-runner/fixtures/invalid/*.json >/dev/null 2>&1; then
    for f in specs/010-headless-verification-runner/fixtures/invalid/*.json; do
        schema="$(_schema_for_fixture "$f")"
        if python3 -c "import json,jsonschema,sys; s=json.load(open(sys.argv[1])); d=json.load(open(sys.argv[2])); jsonschema.validate(d,s); sys.exit(0)" "$schema" "$f" 2>/dev/null; then
            fail "invalid fixture rejected: ${f##*/}" "accepted by ${schema##*/}"
        else
            pass "invalid fixture rejected: ${f##*/}"
        fi
    done
else
    skip "invalid fixtures" "none present"
fi

# schema-validate helper exists
assert_file_exists "scripts/tests/lib/schema-validate.sh" "schema-validate helper exists"

finish_tests
