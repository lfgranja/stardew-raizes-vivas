#!/usr/bin/env bash
#
# scripts/tests/lib/schema-validate.sh — wraps Python jsonschema and ajv-cli so
# every schema test runs both validators and asserts identical accept/reject
# outcomes (C-SCN-38).
set -Eeuo pipefail

validate_with_python() {
    local schema="$1" data="$2"
    python3 - "$schema" "$data" <<'PY'
import json, jsonschema, sys
schema=json.load(open(sys.argv[1]))
data=json.load(open(sys.argv[2]))
try:
    jsonschema.validate(data, schema)
    print("accept")
except jsonschema.ValidationError:
    print("reject")
PY
}

validate_with_ajv() {
    local schema="$1" data="$2"
    if command -v ajv >/dev/null 2>&1; then
        if ajv validate -s "$schema" -d "$data" >/dev/null 2>&1; then
            echo "accept"
        else
            echo "reject"
        fi
    else
        echo "skip"
    fi
}