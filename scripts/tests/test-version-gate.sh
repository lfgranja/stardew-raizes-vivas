#!/usr/bin/env bash
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/runner-harness.sh"

# version_gate_classify exists
if declare -F version_gate_classify >/dev/null 2>&1; then pass "version_gate_classify defined"; else fail "version_gate_classify defined" "missing"; fi

# version comparison: 1.11 < 1.2 (SemanticVersion)
call_runner_func version_gate_classify "1.11.0" "1.2.0" "1.6.14" 2>/dev/null
pass "version_gate exercised (rc=$RUNNER_FN_CODE)"

# no Xvfb on hard-fail: source guard
assert_contains_file "scripts/lib/version-gate.sh" "version_gate" "version-gate lib present"

# -unofficial suffix handling
assert_contains_file "scripts/lib/version-gate.sh" "unofficial" "unofficial handling"

# null MaximumGameVersion handling
assert_contains_file "scripts/lib/version-gate.sh" "MaximumGameVersion" "max game version handling"

# ---------------------------------------------------------------------------
# Directory form: preflight_version_gate passes <game_dir> <manifest>.
# The gate must derive the observed game/SMAPI versions from the installation
# instead of parsing the directory path as a version (the Gate 3 exit-19 bug).
# ---------------------------------------------------------------------------
VG_FIXTURE="${FIXTURE_ROOT}/version-gate"
mkdir -p "${VG_FIXTURE}/Mods/LivingRoots"

# Crafted assemblies: the version resolver reads the product version out of
# the .dll with `strings`, so a minimal MZ prefix plus the version is enough.
printf 'MZ\000\000' > "${VG_FIXTURE}/Stardew Valley.dll"
printf '1.6.15' >> "${VG_FIXTURE}/Stardew Valley.dll"
printf 'MZ\000\000' > "${VG_FIXTURE}/StardewModdingAPI.dll"
printf '4.5.2' >> "${VG_FIXTURE}/StardewModdingAPI.dll"
cat > "${VG_FIXTURE}/Mods/LivingRoots/manifest.json" <<'JSON'
{
  "UniqueID": "lfgranja.LivingRoots",
  "Version": "0.0.1",
  "MinimumApiVersion": "4.4.0",
  "MinimumGameVersion": "1.6.14"
}
JSON

call_runner_func version_gate_classify \
    "${VG_FIXTURE}" "${VG_FIXTURE}/Mods/LivingRoots/manifest.json" 2>/dev/null
assert_fn_code 0 "directory form: a compatible install does not hard-fail"
assert_contains "$RUNNER_FN_OUT" "INFO" "directory form: a compatible install is INFO"

# Preflight wrapper: preflight_version_gate <game_dir> <manifest> must pass
# the same compatible install (the caller contract that produced Gate 3 exit 19).
if declare -F preflight_version_gate >/dev/null 2>&1; then
    call_runner_func preflight_version_gate         "${VG_FIXTURE}" "${VG_FIXTURE}/Mods/LivingRoots/manifest.json" 2>/dev/null
    assert_fn_code 0 "preflight_version_gate: a compatible install returns 0"
else
    fail "preflight_version_gate defined" "missing"
fi

# Older game version must still hard-fail (the gate must not be weakened).
VG_OLD_GAME="${FIXTURE_ROOT}/version-gate-old-game"
mkdir -p "${VG_OLD_GAME}/Mods/LivingRoots"
printf 'MZ\000\000' > "${VG_OLD_GAME}/Stardew Valley.dll"
printf '1.5.0' >> "${VG_OLD_GAME}/Stardew Valley.dll"
printf 'MZ\000\000' > "${VG_OLD_GAME}/StardewModdingAPI.dll"
printf '4.5.2' >> "${VG_OLD_GAME}/StardewModdingAPI.dll"
cp "${VG_FIXTURE}/Mods/LivingRoots/manifest.json" \
    "${VG_OLD_GAME}/Mods/LivingRoots/manifest.json"

call_runner_func version_gate_classify \
    "${VG_OLD_GAME}" "${VG_OLD_GAME}/Mods/LivingRoots/manifest.json" 2>/dev/null
assert_fn_code 19 "directory form: an older game version still hard-fails (19)"

# Older SMAPI version must still hard-fail.
VG_OLD_SMAPI="${FIXTURE_ROOT}/version-gate-old-smapi"
mkdir -p "${VG_OLD_SMAPI}/Mods/LivingRoots"
printf 'MZ\000\000' > "${VG_OLD_SMAPI}/Stardew Valley.dll"
printf '1.6.15' >> "${VG_OLD_SMAPI}/Stardew Valley.dll"
printf 'MZ\000\000' > "${VG_OLD_SMAPI}/StardewModdingAPI.dll"
printf '4.3.0' >> "${VG_OLD_SMAPI}/StardewModdingAPI.dll"
cp "${VG_FIXTURE}/Mods/LivingRoots/manifest.json" \
    "${VG_OLD_SMAPI}/Mods/LivingRoots/manifest.json"

call_runner_func version_gate_classify \
    "${VG_OLD_SMAPI}" "${VG_OLD_SMAPI}/Mods/LivingRoots/manifest.json" 2>/dev/null
assert_fn_code 19 "directory form: an older SMAPI version still hard-fails (19)"

finish_tests
