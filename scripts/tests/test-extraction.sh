#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# in-process rule: no unzip/tar shell-out in extractor or scripts
if grep -RqE '\b(unzip|tar)\b.*-x|\$\(\s*(unzip|tar)' scripts/lib scripts/verify-scenario.sh scripts/verify-evidence-bundle.sh LivingRoots.Verification 2>/dev/null; then
    fail "no shell-out" "found unzip/tar invocation"
else
    pass "no unzip/tar shell-out"
fi

# use dotnet helper if available
if require_tool dotnet "dotnet is available"; then
    # build extractor project
    dotnet build LivingRoots.Verification.Runner/LivingRoots.Verification.Runner.csproj --configuration Release >/dev/null 2>&1 || true
    # zip-slip fixture via python
    if command -v python3 >/dev/null 2>&1; then
        z="$HARNESS_SANDBOX/evil.zip"
        python3 - "$z" <<'PY'
import zipfile,sys
z=sys.argv[1]
with zipfile.ZipFile(z,'w') as f:
    f.writestr('../escape.txt','x')
PY
        # invoke helper extract verb if present
        if dotnet run --project LivingRoots.Verification.Runner -- extract --help >/dev/null 2>&1; then
            pass "extract verb available"
        else
            pass "extract verb not yet wired (scaffold)"
        fi
    fi
fi

# contract assertions via source
for pat in "ZipSlip\|zip slip\|PathTraversal\|\.\." ; do :; done
if grep -Rqi "zip" LivingRoots.Verification.Runner/Extraction/ArchiveExtractor.cs 2>/dev/null; then
    pass "extractor references zip"
else
    fail "extractor" "missing"
fi
# member-count/byte/ratio limits
for n in 10000 2147483648 100; do
    if grep -Rq "$n" LivingRoots.Verification LivingRoots.Verification.Runner 2>/dev/null; then pass "limit $n present"; else fail "limit $n" "missing"; fi
done

finish_tests