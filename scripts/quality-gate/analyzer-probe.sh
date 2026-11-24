#!/usr/bin/env bash
# Proves the analyzer gate fails on a deliberate defect with file, line and rule.
# Both projects are probed because both must enforce the analyzer configuration.
# The probes are removed on every exit path, including build failure and Ctrl-C.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
mod_probe="$repo_root/LivingRoots/__AnalyzerGateProbe.cs"
test_probe="$repo_root/LivingRoots.Tests/__AnalyzerGateProbe.cs"

cleanup() {
    rm -f "$mod_probe" "$test_probe"
}
trap cleanup EXIT INT TERM

cat > "$mod_probe" <<'CS'
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

cat > "$test_probe" <<'CS'
namespace LivingRoots.Tests
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

status=0

for project in "LivingRoots/LivingRoots.csproj" "LivingRoots.Tests/LivingRoots.Tests.csproj"; do
    echo "--- probing $project ---"
    set +e
    output="$(cd "$repo_root" && dotnet build "$project" -c Release --no-restore 2>&1)"
    build_status=$?
    set -e
    printf '%s\n' "$output"

    if [ "$build_status" -eq 0 ]; then
        echo "FAIL: the analyzer gate did not fail on the probe in $project (build succeeded)." >&2
        status=1
        continue
    fi

    if ! grep -q "__AnalyzerGateProbe.cs" <<<"$output"; then
        echo "FAIL: $project failed but did not name the probe file." >&2
        status=1
        continue
    fi

    if ! grep -Eq '__AnalyzerGateProbe\.cs\([0-9]+,[0-9]+\)' <<<"$output"; then
        echo "FAIL: $project failed but did not report a line and column." >&2
        status=1
        continue
    fi

    if ! grep -Eq '(S[0-9]{4}|CS[0-9]{4})' <<<"$output"; then
        echo "FAIL: $project failed but did not report a rule ID." >&2
        status=1
        continue
    fi
done

if [ "$status" -ne 0 ]; then
    exit "$status"
fi

echo "PASS: the analyzer gate failed on both probes with file, line, and rule."
