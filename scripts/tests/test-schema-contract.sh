#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

schema="specs/010-headless-verification-runner/contracts/scenario.schema.json"
assert_file_exists "$schema" "scenario schema exists"

# valid fixtures accepted
if ls specs/010-headless-verification-runner/fixtures/valid/*.json >/dev/null 2>&1; then
    for f in specs/010-headless-verification-runner/fixtures/valid/*.json; do
        if python3 -c "import json,jsonschema,sys; s=json.load(open('$schema')); d=json.load(open('$f')); jsonschema.validate(d,s); sys.exit(0)" 2>/dev/null; then
            pass "valid fixture accepted: ${f##*/}"
        else
            fail "valid fixture accepted: ${f##*/}" "rejected"
        fi
    done
else
    skip "valid fixtures" "none present"
fi

# invalid fixtures rejected
if ls specs/010-headless-verification-runner/fixtures/invalid/*.json >/dev/null 2>&1; then
    for f in specs/010-headless-verification-runner/fixtures/invalid/*.json; do
        if python3 -c "import json,jsonschema,sys; s=json.load(open('$schema')); d=json.load(open('$f')); jsonschema.validate(d,s); sys.exit(0)" 2>/dev/null; then
            fail "invalid fixture rejected: ${f##*/}" "accepted"
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
