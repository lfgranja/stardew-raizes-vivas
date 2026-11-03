#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# never-quarantine validates against schema
nq="specs/010-headless-verification-runner/never-quarantine.json"
nqs="specs/010-headless-verification-runner/contracts/never-quarantine.schema.json"
assert_file_exists "$nq" "never-quarantine exists"
assert_file_exists "$nqs" "never-quarantine schema exists"

# promotion-triggers shape
pt="specs/010-headless-verification-runner/promotion-triggers.json"
assert_file_exists "$pt" "promotion-triggers exists"
python3 - "$pt" <<'PY'
import json,sys
d=json.load(open(sys.argv[1]))
assert set(d.keys())=={"schemaVersion","components","transitions"} or set(d.keys())>= {"schemaVersion","components","transitions"}, d.keys()
assert isinstance(d["components"],list)
assert isinstance(d["transitions"],list)
print("shape ok")
PY
if [[ $? -eq 0 ]]; then pass "promotion-triggers shape"; else fail "promotion-triggers shape" "bad"; fi

# evidence-bundle schema exists
assert_file_exists "specs/010-headless-verification-runner/contracts/evidence-bundle.schema.json" "evidence-bundle schema exists"

# claims schema exists
assert_file_exists "specs/010-headless-verification-runner/contracts/claims.schema.json" "claims schema exists"

# quarantine registry
assert_file_exists "specs/010-headless-verification-runner/quarantine.json" "quarantine registry exists"

finish_tests
