#!/usr/bin/env bash
# Living Roots — release gate (spec 019).
#
# Deterministic, fail-closed, secret-free. No human in the loop: every step either
# passes with evidence on this host, or the gate exits non-zero with the failing
# step named. This is the local authoritative gate; the hosted CI jobs
# (.github/workflows/*.yml) are the remote re-statement of the same contract.
#
# Caveats (honest, by design):
#   * Gate 3 (real-game attestation) is NOT part of this script. It stays a
#     deliberate, separately-evidenced step (VERIFICATION_PROTOCOL.md). This
#     gate proves build/test/smoke, which is "production-deployable code".
#   * The game is never launched or modified by this script.
#   * A green gate here does NOT enable auto-merge, releases, or push. Those are
#     host-side settings and explicit pushes handled outside this repository.
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/.." && pwd -P)"
cd "$REPO_ROOT"
date_utc() { date -u +%Y-%m-%dT%H:%M:%SZ; }
say() { printf '[release-gate %s] %s\n' "$1" "$2"; }
fail() { printf '[release-gate FAIL] %s\n' "$1" >&2; exit "${2:-1}"; }

ALLOW_DIRTY=0
for arg in "$@"; do
    case "$arg" in
        --allow-dirty) ALLOW_DIRTY=1 ;;
        -h|--help) sed -n '2,20p' "$0"; exit 0 ;;
        *) fail "unknown argument: $arg" 64 ;;
    esac
done

if (( ALLOW_DIRTY == 0 )); then
    if ! git diff --quiet || ! git diff --cached --quiet; then
        fail "working tree is not clean; release builds ship committed content only (or use --allow-dirty for diagnostics)" 3
    fi
fi
say "$(date_utc)" "clean tree: OK"

GAME_PATH="${GamePath:-${STARDEW_GAME_PATH:-}}"
if [[ -z "$GAME_PATH" ]]; then
    fail "GamePath is not set (Stardew Valley managed assemblies are required to build); e.g. GamePath=/home/luis/stardew-assemblies" 4
fi
say "$(date_utc)" "GamePath: $GAME_PATH"

set -x
dotnet format Stardew-LivingRoots.sln --verify-no-changes \
    || fail "step 1 failed: formatting (dotnet format --verify-no-changes)" 11
set +x
say "$(date_utc)" "step 1 OK: format"

set -x
dotnet restore --locked-mode \
    || fail "step 2 failed: locked restore (dotnet restore --locked-mode) — lock file drift is a hard stop" 12
set +x
say "$(date_utc)" "step 2 OK: locked restore"

set -x
# -m:1 keeps a single MSBuild node: a parallel solution build can die with
# MSB4166 (child node killed) on memory-constrained hosts and that is not a
# code failure. --disable-build-servers avoids stale/corrupted server state.
# Determinism first.
GamePath="$GAME_PATH" dotnet build Stardew-LivingRoots.sln --configuration Release --no-restore -m:1 --disable-build-servers \
    || fail "step 3 failed: Release build" 13
set +x
say "$(date_utc)" "step 3 OK: build (Release)"

set -x
GamePath="$GAME_PATH" dotnet test Stardew-LivingRoots.sln --configuration Release --no-build --verbosity minimal \
    || fail "step 4 failed: full test suite" 14
set +x
say "$(date_utc)" "step 4 OK: tests"

set -x
TMPDIR="${LR_RELEASE_TMPDIR:-/home/luis/.smoke-tmp-lrv}" SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh \
    || fail "step 5 failed: launch-free smoke suite" 15
set +x
say "$(date_utc)" "step 5 OK: smoke suite (launch-free)"

set -x
./scripts/verify-scenario.sh --check-docs \
    || fail "step 6 failed: docs markers (--check-docs)" 16
set +x
say "$(date_utc)" "step 6 OK: docs markers"

set -x
./scripts/check-protected-paths.sh \
    || fail "step 7 failed: protected paths / secrets" 17
set +x
say "$(date_utc)" "step 7 OK: protected paths"

say "$(date_utc)" "ALL STEPS PASSED"
printf '\n[release-gate] gate passed. Commit: %s\n' "$(git rev-parse HEAD)"
