#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# --- helpers ---
assert_code() { assert_eq "$2" "$RUN_VERIFY_CODE" "$1"; }

# unknown flag
run_verify --bogus-flag
assert_verify_code 14 "unknown flag exits 14"

# missing required value
run_verify --scenario
assert_verify_code 14 "missing --scenario value exits 14"

# unknown positional
run_verify foo.scenario.json
assert_verify_code 14 "unknown positional exits 14"

# --game-path empty
run_verify --scenario /dev/null --game-path ""
assert_verify_code 14 "empty --game-path exits 14"

# --no-acquire + --force-refresh incompatible
run_verify --scenario /dev/null --no-acquire --force-refresh
assert_verify_code 14 "incompatible acquire flags exit 14"

# --ignore-exit-code invalid values
for bad in 0 22 99 foo; do
    run_verify --scenario /dev/null --ignore-exit-code "$bad"
    assert_verify_code 14 "--ignore-exit-code $bad exits 14"
done

# --ignore-exit-code valid single + list
run_verify --scenario /dev/null --ignore-exit-code 18,19
assert_verify_code 14 "no scenario yet still usage? absent scenario exits 14"

# missing scenario for a run
run_verify --timeout 5
assert_verify_code 14 "missing --scenario exits 14"

# --help
run_verify --help
assert_verify_code 0 "--help exits 0"

# --verify-bundle launch-free dispatch (absent dir => 19, not 13/127)
run_verify --verify-bundle "$HARNESS_SANDBOX/nope"
assert_verify_code 19 "--verify-bundle absent exits 19"

# --check-docs launch-free dispatch
run_verify --check-docs
assert_verify_code 0 "--check-docs no markers? empty tree may be 0/14"

# --override allow list accepted at parse (will still fail later, but not 14)
# write a min scenario
scn="$HARNESS_SANDBOX/min.scenario.json"
write_min_scenario "$scn"
run_verify --scenario "$scn" --override environment.gameVersion=1.6.14 --no-acquire
# either 9 (cache miss) or further; must not be 14
if (( RUN_VERIFY_CODE == 14 )); then fail "allowed override not usage error" "got 14"; else pass "allowed override passes usage"; fi

# forbidden/unknown override
run_verify --scenario "$scn" --override foo.bar=baz
assert_verify_code 14 "unknown override key exits 14"
run_verify --scenario "$scn" --override environment.config.x=1
# environment.config.* is allowed, so not 14 necessarily; but unknown top-level
run_verify --scenario "$scn" --override environment.saveIdExtra=x
assert_verify_code 14 "unknown override key exits 14"

# --scenario file/dir accepted (dir with one scenario)
mkdir -p "$HARNESS_SANDBOX/scendir"
cp "$scn" "$HARNESS_SANDBOX/scendir/a.scenario.json"
run_verify --scenario "$HARNESS_SANDBOX/scendir" --no-acquire
if (( RUN_VERIFY_CODE == 14 )); then fail "dir scenario not usage error" "got 14"; else pass "dir scenario passes usage"; fi

# --scenario-glob
run_verify --scenario "$HARNESS_SANDBOX/scendir" --scenario-glob '*.scenario.json' --no-acquire
if (( RUN_VERIFY_CODE == 14 )); then fail "scenario-glob not usage error" "got 14"; else pass "scenario-glob passes usage"; fi

finish_tests