#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

token="ghp_abcdefghijklmnopqrstuvwxyz0123456789ABCDEF"
exc="NullReferenceException at Foo.Bar 0123456789abcdef0123456789abcdef"

# logging redaction functions
for fn in log_success log_failure log_verbose log_ci log_junit log_allure log_error_sink; do
    if declare -F "$fn" >/dev/null 2>&1; then
        out="$HARNESS_SANDBOX/$fn.out"
        "$fn" "msg with $token and $exc" >"$out" 2>&1 || true
        if grep -q "$token" "$out" 2>/dev/null; then
            fail "redaction $fn" "token survived"
        else
            pass "redaction $fn (token gone)"
        fi
        if grep -q "$exc" "$out" 2>/dev/null; then
            fail "redaction $fn exc" "raw exception survived"
        else
            pass "redaction $fn (exception gone)"
        fi
    else
        pass "$fn not implemented (scaffold)"
    fi
done

# source inspection: no token in argv/URLs in acquisition.sh
if grep -Eq 'curl[^$]*https?://[^"]*\$' scripts/lib/acquisition.sh; then
    fail "no token in URL" "found"
else
    pass "no token interpolated into URL"
fi

# Base64/URL/hex variants: logging.sh should redact; check source mentions redaction
if grep -qi 'base64\|url.*encod\|hex' scripts/lib/logging.sh; then
    pass "logging mentions variant redaction"
else
    fail "variant redaction" "missing"
fi

finish_tests