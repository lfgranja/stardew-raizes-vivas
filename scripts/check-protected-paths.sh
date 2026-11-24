#!/usr/bin/env bash
#
# Asserts that the protective entries in .gitignore are actually in force.
#
# Two independent checks, because they fail for different reasons:
#
#   1. Integrity  — every path in PROTECTED must be ignored (or, for an intentional
#                   negation, must NOT be ignored). Catches a deleted or renamed rule.
#   2. Blast radius — no file matching BANNED_TRACKED may already be tracked. Catches the
#                   case where check 1 was bypassed and the commit already happened.
#
# Check 1 alone is not enough: it only knows about paths someone declared here. Check 2 is
# what notices that something undeclared got in anyway.
#
# Neither check can catch a sensitive file type nobody thought to list. That limit is real
# and is the reason this script exists rather than being trusted as a complete guarantee.

set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

# Sample path <TAB> expected outcome.
#   ignored — git must ignore this path
#   tracked  — git must NOT ignore it (an intentional negation, e.g. .env.example)
PROTECTED=(
    ".env:ignored"
    ".env.local:ignored"
    "server.pem:ignored"
    "secrets.json:ignored"
    "credentials.json:ignored"
    ".env.example:tracked"
    ".gamepath/Stardew Valley.dll:ignored"
    ".gamepath/MonoGame.Framework.dll:ignored"
    ".gamepath/StardewValley.GameData.dll:ignored"
    ".gamepath/smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll:ignored"
)

# Filenames that must never appear in `git ls-files`. These are the proprietary Stardew
# Valley / SMAPI assemblies plus MonoGame; see lfgranja/stardew-raizes-vivas-builddeps,
# whose README states redistribution is not permitted.
BANNED_TRACKED=(
    "Stardew Valley.dll"
    "StardewModdingAPI.dll"
    "StardewValley.GameData.dll"
    "MonoGame.Framework.dll"
    "xTile.dll"
    "SMAPI.Toolkit.CoreInterfaces.dll"
)

failures=0

echo "==> Check 1: protected .gitignore entries are in force"
for entry in "${PROTECTED[@]}"; do
    path="${entry%%:*}"
    expected="${entry##*:}"

    if git check-ignore -q -- "$path"; then
        actual="ignored"
    else
        actual="tracked"
    fi

    if [ "$actual" = "$expected" ]; then
        printf '    ok    %-58s %s\n' "$path" "$actual"
    else
        printf '    FAIL  %-58s expected %s, git says %s\n' "$path" "$expected" "$actual"
        failures=$((failures + 1))
    fi
done

echo "==> Check 2: no prohibited assembly is tracked"
# Match on the basename. `git ls-files` emits full paths, so anchoring on the raw line would
# only ever match a file at the repository root and would silently pass for every nested
# path — including .gamepath/, which is exactly where these land.
tracked=$(git ls-files | sed 's|.*/||')
for name in "${BANNED_TRACKED[@]}"; do
    # -F literal, -x whole-line. On the basename this catches the assembly at any depth while
    # still not matching a merely-similar name such as xTile.dll.bak.
    hits=$(printf '%s\n' "$tracked" | grep -Fx -- "$name" || true)
    if [ -z "$hits" ]; then
        printf '    ok    %s not tracked\n' "$name"
    else
        printf '    FAIL  %s IS TRACKED (run: git rm --cached -- %s)\n' "$name" "$name"
        failures=$((failures + 1))
    fi
done

if [ "$failures" -gt 0 ]; then
    echo
    echo "protected-path preflight: $failures failure(s)."
    echo "On a public repo a committed secret or proprietary binary is publication, not a"
    echo "mistake you can take back. Fix .gitignore (or untrack the file) and re-run."
    exit 1
fi

echo
echo "protected-path preflight: all checks passed."
