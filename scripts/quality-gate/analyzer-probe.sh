#!/usr/bin/env bash
# Proves the analyzer gate fails on a deliberate defect with file, line and rule.
# The probe is removed on every exit path, including build failure and Ctrl-C.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
probe="$repo_root/LivingRoots/__AnalyzerGateProbe.cs"

cleanup() {
    rm -f "$probe"
}
trap cleanup EXIT INT TERM

cat > "$probe" <<'CS'
namespace LivingRoots
{
    internal static class AnalyzerGateProbe
    {
        public static void Run()
        {
            // Deliberately assigned and never read: CS0219 / S1481.
            int unused = 42;
        }
    }
}
CS

set +e
output="$(cd "$repo_root" && dotnet build LivingRoots/LivingRoots.csproj -c Release --no-restore 2>&1)"
status=$?
set -e

printf '%s\n' "$output"

if [ "$status" -eq 0 ]; then
    echo "FAIL: the analyzer gate did not fail on the probe (build succeeded)." >&2
    exit 1
fi

if ! grep -q "__AnalyzerGateProbe.cs" <<<"$output"; then
    echo "FAIL: the build failed but did not name the probe file." >&2
    exit 1
fi

if ! grep -Eq '__AnalyzerGateProbe\.cs\([0-9]+,[0-9]+\)' <<<"$output"; then
    echo "FAIL: the build failed but did not report a line and column." >&2
    exit 1
fi

if ! grep -Eq '(S[0-9]{4}|CS[0-9]{4})' <<<"$output"; then
    echo "FAIL: the build failed but did not report a rule ID." >&2
    exit 1
fi

echo "PASS: analyzer gate failed on __AnalyzerGateProbe.cs with file, line and rule."
