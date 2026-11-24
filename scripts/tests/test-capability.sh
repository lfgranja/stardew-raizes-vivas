#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

if grep -R "LivingRoots.Verification" LivingRoots/ >/dev/null 2>&1; then
    fail "production mod references verification mod" "found reference"
else
    pass "production mod has no verification reference"
fi

assert_contains_file "LivingRoots.Verification/manifest.json" "lfgranja.LivingRoots" "manifest declares dependency"

if grep -q "UpdateKeys\|updateKeys" LivingRoots.Verification/manifest.json; then
    fail "verification manifest has update keys" "found UpdateKeys"
else
    pass "verification manifest has no update keys"
fi

assert_contains_file "LivingRoots.Verification/LivingRoots.Verification.csproj" "EnableModZip" "csproj excludes release zip"

if grep -R "HttpClient\|WebClient\|System.Reflection" LivingRoots.Verification/ --include=*.cs 2>/dev/null | grep -v "AssemblyAttributes\|AssemblyInfo" >/dev/null; then
    fail "verification mod uses network/reflection" "found"
else
    pass "verification mod uses no network/reflection"
fi

finish_tests
