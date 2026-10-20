#!/usr/bin/env bash
#
# scripts/tests/test-preflight.sh — the pre-flight test suite for feature 008.
#
# Covers the five numbered pre-flight cases of quickstart.md — TC-20 … TC-24 —
# plus the two table-driven properties tasks.md T010 asks for:
#
#   * the FR-012 item 8 host-tool sweep (a missing tool must fail closed with
#     exit 9 and must be NAMED), and
#   * the FR-012 item 7 component-boundary prefix table.
#
# Every case is launch-free: the script under test is expected to abort inside
# its pre-flight, before the game is launched. Nothing here touches the real
# game installation — every game directory is a synthetic fixture under the
# harness sandbox.
#
# Two PATH profiles keep the suite hermetic:
#
#   * NO_DISPLAY_PATH — the failing Xvfb stub comes first, so the display
#     phase fails with the stub's status and no real X server is ever started;
#   * NO_BUILD_PATH — the real Xvfb is used (TC-20 needs the display phase to
#     SUCCEED, see below) and a `dotnet` stub that refuses to build comes
#     first, so no case can rebuild the real mod or write into the repository
#     tree.
#
# OBSERVABILITY
# -------------
# Three properties are not directly observable from the outside, so they are
# established with observable proxies:
#
#   * "the game was not launched" — the fixtures replace SMAPI's apphost with
#     a stub that records its own execution (install_launch_sentinel). The
#     ABSENCE of that sentinel file is the evidence; it is not an inference
#     from the absence of output.
#   * "the build was not run" — the deploy target (<mods-path>/LivingRoots) is
#     absent after every run, and no case in this file is allowed to reach the
#     build phase: each one aborts in the pre-flight. The default
#     --mod-project is deliberately NOT overridden, so FR-016 (a)'s prefix
#     test against the repository root stays satisfiable; the `dotnet` stub
#     and --build-timeout 5 bound the worst case if an implementation reaches
#     the build anyway.
#   * "the pre-flight rejected the path" — the script's own prefix-test helper
#     is not reachable from the outside (the script is not designed to be
#     sourced), so the path table is asserted through the CLI. See below.
#
# PATH-PROPERTY TABLE (FR-012 item 7)
# ----------------------------------
# The table describes the component-boundary decision the script makes for a
# CANDIDATE path relative to a BASE, where the base is the canonicalized game
# directory:
#
#   row | candidate                          | decision | asserted
#   ----+------------------------------------+----------+----------
#   P1  | <base>/Mods  (a genuine child)     | accept   | direct
#   P2  | <base>-evil (a shared prefix)      | reject   | indirect
#   P3  | <base>/      (trailing slash)      | accept   | direct
#   P4  | <base>/child/../child2 (a "..")    | accept   | direct
#   P5  | <base>/escape (symlink outside)    | reject   | indirect
#   P6  | a path containing a space          | accept   | direct
#   P5' | the CONSTRUCTED <base>/smapi-internal/… |
#        |             escaping the base      | reject   | direct
#
# "Direct" means the decision shows up in the script's exit code and report for
# that candidate. "Indirect" means the candidate can only be supplied as a
# USER-SUPPLIED ROOT, and FR-012 item 7 makes a user-supplied root its own
# base ("an absolute --mods-path outside the game directory is legal") — so
# the prefix test never runs on it and the table's decision cannot be observed
# as a rejection. For those rows the observable consequence is asserted
# instead, and it is the consequence that actually matters:
#
#   * P2 — the sibling shares the base's textual prefix, so a naive
#     `case "$real" in "$base"*` would treat <base>-evil/... as being inside
#     the base. The observable consequence is that the script must not touch
#     the sibling: the sentinel inside it survives the run and the directory
#     listing is byte-for-byte unchanged.
#   * P5 — canonicalisation resolves the symlink, so the candidate is outside
#     the base. The observable consequence is that the escape TARGET is not
#     touched. P5' is the direct manifestation of the CLI: the game
#     directory's own smapi-internal/ entry is a symlink pointing outside the
#     game directory, so the CONSTRUCTED marker path escapes the base and the
#     script must refuse the SMAPI installation (exit 2, FR-012 item 2)
#     instead of reading the escaping files.
#
# Accept rows are asserted as "the run reached the display phase" (exit 10
# with the failing Xvfb stub in front of the real one): the pre-flight order
# of FR-012 (0) puts the game/SMAPI/Steam/path checks BEFORE the display, so
# reaching the display is proof that the candidate path was accepted, and the
# code is additionally asserted not to be one of the rejection codes
# (1 game, 2 SMAPI, 11 Steam, 14 usage, 16 mods path/manifest).
#
# Spec:   specs/008-smoke-validation/spec.md
# Tasks:  specs/008-smoke-validation/tasks.md (T010)
# Quickstart: specs/008-smoke-validation/quickstart.md (TC-20 … TC-24)

set -Eeuo pipefail

source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/harness.sh"

# ---------------------------------------------------------------------------
# Fixture layout
# ---------------------------------------------------------------------------

# The harness sandbox is this file's scratch area: everything synthetic lives
# under it and it is removed by the harness's EXIT trap.
TMP="${HARNESS_SANDBOX}"

# The canonical "game" fixtures used by the numbered cases.
GAME_DIR="${FIXTURE_ROOT}/game"
BROKEN_SMAPI_DIR="${FIXTURE_ROOT}/broken-smapi"
BAD_STEAM_DIR="${FIXTURE_ROOT}/bad-steam"

# The directory that holds the failing Xvfb stub. It is put in FRONT of the
# real PATH for the display case and for every path-table row, so the display
# phase fails with the stub's exit status instead of starting a real X server.
STUB_BIN_DIR="${FIXTURE_ROOT}/bin"

# A directory that holds a `dotnet` stub. It is put in front of the real PATH
# for the one case that needs the display phase to SUCCEED (TC-20, see
# below): the stub answers version queries and refuses to build, so no test
# can ever rebuild the real mod or write into the repository tree.
BLOCK_BIN_DIR="${FIXTURE_ROOT}/block"

# A PATH that contains every host tool EXCEPT Xvfb — the FR-012 item 8 case.
MIN_PATH_DIR="${FIXTURE_ROOT}/minpath"

# The launch sentinels: one file per fixture, created by the apphost stub when
# (and only when) the game is actually launched.
LAUNCH_LOG_DIR="${FIXTURE_ROOT}/launched"

# The path-property table's base: a valid game directory whose parent
# contains a sibling with the same textual prefix.
BASE_GAME="${FIXTURE_ROOT}/base/game"
BASE_EVIL_GAME="${FIXTURE_ROOT}/base/game-evil"
BASE_DOTDOT_MID="${BASE_GAME}/child"
BASE_DOTDOT_TARGET="${BASE_GAME}/child2"
BASE_ESCAPE_LINK="${BASE_GAME}/escape"
BASE_SPACE_GAME="${FIXTURE_ROOT}/Stardew Valley"

# The symlink's target and the escaping SMAPI directory: both live OUTSIDE
# the base, and neither may be touched by a run.
OUTSIDE_GAME="${FIXTURE_ROOT}/outside/real-game"
ESCAPE_GAME_DIR="${FIXTURE_ROOT}/escape-game"
ESCAPE_SMAPI_DIR="${FIXTURE_ROOT}/outside/smapi-internal"

# A mods path that does not exist, and a state root that cannot be written to.
BAD_MODS_PATH="${TMP}/no-such-mods-dir"
READONLY_STATE="${TMP}/readonly"

# The exit codes the pre-flight uses to REJECT a candidate path. An "accept"
# row must not end in one of them.
REJECTION_CODES="1,2,11,14,16"

# The PATH used by every case that must not start a real X server: the failing
# Xvfb stub comes first, and the blocked `dotnet` neutralises the build phase.
NO_DISPLAY_PATH="${BLOCK_BIN_DIR}:${STUB_BIN_DIR}:${PATH}"

# The PATH used by TC-20, where the display phase has to SUCCEED so that the
# run reaches the mods-path check whichever side of the display phase the
# implementation puts it on. The blocked `dotnet` still prevents a build.
NO_BUILD_PATH="${BLOCK_BIN_DIR}:${PATH}"

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

# launch_marker <name> — the path of a fixture's launch sentinel.
launch_marker() {
    printf '%s\n' "${LAUNCH_LOG_DIR}/${1}.launched"
}

# install_launch_sentinel <game-dir> <name>
#
# Replaces SMAPI's apphost stub with one that records its own execution. The
# sentinel's absence is what proves "the game was not launched".
install_launch_sentinel() {
    local game_dir="${1-}"
    local name="${2-}"
    local marker

    marker="$(launch_marker "${name}")"

    mkdir -p -- "${LAUNCH_LOG_DIR}"
    cat > "${game_dir}/StardewModdingAPI" <<STUB
#!/usr/bin/env bash
# Test stub standing in for SMAPI's native apphost. It launches nothing; it
# only records that it was executed, so a test can assert "the game was not
# launched" as a fact instead of an inference.
printf 'launched\n' >> "${marker}"
exit 0
STUB
    chmod 0755 -- "${game_dir}/StardewModdingAPI"
}

# install_build_blocker
#
# Installs a `dotnet` stub that answers version queries and refuses to build.
# It is put in front of the real PATH so that no test can ever rebuild the
# real mod, restore NuGet packages or write into the repository tree — even if
# an implementation reaches the build phase when it should not.
install_build_blocker() {
    mkdir -p -- "${BLOCK_BIN_DIR}"
    cat > "${BLOCK_BIN_DIR}/dotnet" <<'STUB'
#!/usr/bin/env bash
# Test stub standing in for `dotnet`. Version queries succeed (a tool sweep
# must still see the tool as present); a build refuses, so no test can
# rebuild the real mod or write into the repository tree.
case "${1-}" in
    --version|--info|info)
        printf '8.0.100\n'
        exit 0
        ;;
esac
printf 'dotnet stub: the build is disabled in the smoke-validation test suite\n' >&2
exit 1
STUB
    chmod 0755 -- "${BLOCK_BIN_DIR}/dotnet"
}

# install_fake_game <dest> <name> — a valid game fixture whose apphost
# records its execution under <name>.
install_fake_game() {
    local dest="${1-}"
    local name="${2-}"

    make_fake_game_dir "${dest}" || return 1
    install_launch_sentinel "${dest}" "${name}"
}

# build_min_path
#
# Builds a PATH that contains every host tool EXCEPT Xvfb. An EMPTY PATH
# cannot be used for this case: the script's shebang is `#!/usr/bin/env bash`,
# so with an empty PATH the kernel cannot find the interpreter at all and the
# run dies with the shell's own 127 before the script's tool sweep ever runs.
# The interpreter is therefore linked in, and only Xvfb is withheld.
build_min_path() {    local dir
    local name
    local found

    mkdir -p -- "${MIN_PATH_DIR}"

    # One `ln` per directory: the farm holds a symlink for every host tool,
    # so the script finds the same binaries it would find on the real PATH.
    for dir in /usr/local/bin /usr/bin /bin /usr/sbin /sbin; do
        if [[ ! -d "$dir" ]]; then
            continue
        fi
        ln -sf -- "$dir"/* "${MIN_PATH_DIR}/" 2>/dev/null || true
    done

    # Xvfb is deliberately withheld: it is the ONE tool this case removes.
    rm -f -- "${MIN_PATH_DIR}/Xvfb"

    # Tools that live outside the standard directories (dotnet is installed
    # under the user's home on this host) are linked in explicitly, so that
    # Xvfb is the only missing tool.
    for name in bash dotnet; do
        found="$(command -v "$name" 2>/dev/null || true)"
        if [[ -z "$found" ]]; then
            continue
        fi
        ln -sf -- "$found" "${MIN_PATH_DIR}/${name}" 2>/dev/null || true
    done
}

# run_preflight_env <env-assignments...> -- [args...] — run the script under
# test with the launch-free options every case needs (a bounded game timeout,
# a bounded grace period and a bounded build) plus the explicit environment
# overrides (PATH, XDG_STATE_HOME).
run_preflight_env() {
    local -a envs=()
    local -a args=()
    local seen=0
    local item

    for item in "$@"; do
        if (( seen == 1 )); then
            args+=("$item")
        elif [[ "$item" == "--" ]]; then
            seen=1
        else
            envs+=("$item")
        fi
    done

    run_script_env "${envs[@]+"${envs[@]}"}" -- \
        --timeout 5 \
        --grace-period 2 \
        --build-timeout 5 \
        "${args[@]+"${args[@]}"}"
}

# assert_dir_exists <path> <label> — the harness has no directory-existence
# assertion (its assert_file_exists requires a regular file), so this one is
# built on the harness's own pass/fail primitives.
assert_dir_exists() {
    local path="${1-}"
    local label="${2-}"
    if [[ -d "$path" ]]; then
        pass "$label"
    else
        fail "$label" "expected a directory at: $path"
    fi
    return 0
}

# failure_report <text> — the FAILURE portion of the script's stderr.
#
# The script also prints a resolved-configuration block on every run, which
# names the game path, the mods path and the mod project. A bare "the report
# contains the path" check would therefore pass on a run that never failed
# for that reason, so the failure report is isolated first: it starts at the
# "What:" marker of the What → Where → How to fix pattern (FR-007) and, when
# the report does not use that marker, at the "ERROR:" marker. When neither
# marker is present the whole text is returned.
failure_report() {
    local text="${1-}"
    local report=""

    if [[ "$text" == *"ERROR:"* ]]; then
        report="${text#*ERROR:}"
        report="ERROR:${report}"
    elif [[ "$text" == *"What:"* ]]; then
        report="${text#*What:}"
        report="What:${report}"
    else
        report="$text"
    fi

    printf '%s' "$report"
}

# assert_code_not_in <csv> <actual> <label> — the exit code must not be one of
# the listed values.
assert_code_not_in() {
    local unexpected="${1-}"
    local actual="${2-}"
    local label="${3-}"
    local candidate

    for candidate in ${unexpected//,/ }; do
        if [[ "$actual" == "$candidate" ]]; then
            fail "$label" "expected any code except [${unexpected}], got '${actual}'"
            return 0
        fi
    done
    pass "$label"
    return 0
}

# directory_listing <dir> — a stable listing of a directory tree, used to
# prove that a run left a directory untouched.
directory_listing() {
    local dir="${1-}"
    local listing=""

    if [[ ! -d "$dir" ]]; then
        printf '(absent)\n'
        return 0
    fi

    if ! listing="$(find "${dir}" -mindepth 1 -printf '%y %P\n' 2>/dev/null | LC_ALL=C sort)"; then
        # A failed listing is reported as such rather than as a partial one,
        # so a comparison against it fails loudly instead of silently.
        printf '(unreadable)\n'
        return 0
    fi

    printf '%s' "$listing"
}

# ---------------------------------------------------------------------------
# Fixtures
# ---------------------------------------------------------------------------

install_fake_game "${GAME_DIR}" "game"
install_fake_game "${BROKEN_SMAPI_DIR}" "broken-smapi"
install_fake_game "${BAD_STEAM_DIR}" "bad-steam"

# The path table's base and its siblings.
install_fake_game "${BASE_GAME}" "base-game"
install_fake_game "${BASE_EVIL_GAME}" "base-evil-game"
install_fake_game "${BASE_DOTDOT_TARGET}" "base-dotdot-target"
install_fake_game "${BASE_SPACE_GAME}" "space-game"
install_fake_game "${OUTSIDE_GAME}" "outside-game"

# The ".." candidate: <base>/child/../child2 resolves to <base>/child2, and
# the intermediate directory must exist for the resolution to succeed.
mkdir -p -- "${BASE_DOTDOT_MID}"

# The symlink that escapes the base.
ln -s -- "${OUTSIDE_GAME}" "${BASE_ESCAPE_LINK}"

# P2's sentinel: a naive textual prefix test could mistake
# <base>-evil/LivingRoots for a path inside <base>/Mods, so the file must
# survive the run.
mkdir -p -- "${BASE_EVIL_GAME}/LivingRoots"
printf 'keep me\n' > "${BASE_EVIL_GAME}/LivingRoots/keep.txt"

# P5': a game directory whose smapi-internal/ entry is a symlink pointing
# OUTSIDE the game directory. The SMAPI assemblies are reachable only through
# the escaping link, so a correct script refuses the installation.
install_fake_game "${ESCAPE_GAME_DIR}" "escape-game"
rm -rf -- "${ESCAPE_GAME_DIR}/smapi-internal"
mkdir -p -- "${ESCAPE_SMAPI_DIR}"
: > "${ESCAPE_SMAPI_DIR}/SMAPI.Toolkit.dll"
: > "${ESCAPE_SMAPI_DIR}/SMAPI.Toolkit.CoreInterfaces.dll"
ln -s -- "${ESCAPE_SMAPI_DIR}" "${ESCAPE_GAME_DIR}/smapi-internal"

# The failing Xvfb stub, in a directory of its own so that it can be put in
# front of the real PATH.
mkdir -p -- "${STUB_BIN_DIR}"
make_stub_xvfb "${STUB_BIN_DIR}"

# The build blocker.
install_build_blocker

# The tool sweep's PATH: everything except Xvfb.
build_min_path

# The state root that cannot be written to (TC-24).
mkdir -p -- "${READONLY_STATE}"
chmod 0500 -- "${READONLY_STATE}"

# ===========================================================================
# TC-20 — invalid --mods-path rejected before launch
# ===========================================================================

printf '# --- TC-20: invalid --mods-path rejected before launch ---\n'

# FR-012 (0) does not pin whether the mods-path check precedes the display
# phase, so this case runs with a WORKING display (the real Xvfb) and a
# blocked `dotnet`: the run must reach the mods-path check either way, and it
# must never rebuild anything.
run_preflight_env "PATH=${NO_BUILD_PATH}" -- \
    --game-path "${GAME_DIR}" \
    --mods-path "${BAD_MODS_PATH}"

REPORT="$(failure_report "$RUN_STDERR")"

assert_exit 16 "TC-20: a --mods-path that does not exist exits 16"
assert_contains "$REPORT" "${BAD_MODS_PATH}" \
    "TC-20: the failure report names the offending mods path"
assert_contains "$REPORT" "Fix:" \
    "TC-20: the report ends with an actionable Fix: line"
assert_file_absent "${BAD_MODS_PATH}" \
    "TC-20: the rejected mods path was not created"
assert_file_absent "${GAME_DIR}/Mods/LivingRoots" \
    "TC-20: the deploy target was not touched"
assert_file_absent "$(launch_marker "game")" \
    "TC-20: the game was not launched"

# ===========================================================================
# TC-21 — SMAPI not functional
# ===========================================================================

printf '# --- TC-21: SMAPI not functional ---\n'

# The SMAPI marker check is the second step of the pre-flight (FR-012 (0)),
# long before the display, so the failing Xvfb stub is put in front of the
# PATH: it keeps a broken implementation from starting a real X server.
run_preflight_env "PATH=${NO_DISPLAY_PATH}" -- \
    --game-path "${BROKEN_SMAPI_DIR}"

REPORT="$(failure_report "$RUN_STDERR")"

assert_exit 2 "TC-21: a broken SMAPI installation exits 2"
assert_contains "$REPORT" "SMAPI" \
    "TC-21: the failure report names SMAPI as the failed component"
assert_contains "$REPORT" "SMAPI.Toolkit.CoreInterfaces.dll" \
    "TC-21: the failure report names the missing marker file"
assert_contains "$REPORT" "Fix:" \
    "TC-21: the report ends with an actionable Fix: line"
assert_file_absent "$(launch_marker "broken-smapi")" \
    "TC-21: the game was not launched"

# ===========================================================================
# TC-22 — Steam prerequisite missing
# ===========================================================================

printf '# --- TC-22: Steam prerequisite missing ---\n'

# The Steam check is the third step of the pre-flight (FR-012 (0)), long
# before the display — the failing Xvfb stub keeps the run from starting a
# real X server if the check is missing.
run_preflight_env "PATH=${NO_DISPLAY_PATH}" -- \
    --game-path "${BAD_STEAM_DIR}"

REPORT="$(failure_report "$RUN_STDERR")"

assert_exit 11 "TC-22: a wrong steam_appid.txt exits 11"
assert_contains "$REPORT" "steam_appid.txt" \
    "TC-22: the failure report names steam_appid.txt"
assert_contains "$REPORT" "413150" \
    "TC-22: the failure report names the expected Steam app id"
assert_contains "$REPORT" "99999" \
    "TC-22: the failure report names the observed Steam app id"
assert_contains "$REPORT" "Fix:" \
    "TC-22: the report ends with an actionable Fix: line"
assert_file_absent "$(launch_marker "bad-steam")" \
    "TC-22: the game was not launched"

# ===========================================================================
# TC-23 — the virtual display cannot be allocated
# ===========================================================================

printf '# --- TC-23: the virtual display cannot be allocated ---\n'

run_preflight_env "PATH=${NO_DISPLAY_PATH}" -- \
    --game-path "${GAME_DIR}"

REPORT="$(failure_report "$RUN_STDERR")"

assert_exit 10 "TC-23: a failing Xvfb exits 10"
assert_ne 9 "$RUN_CODE" \
    "TC-23: the failure is NOT reported as 9 (missing host tool)"
assert_ne 3 "$RUN_CODE" \
    "TC-23: the failure is NOT reported as 3 (graphics present but unusable)"
assert_contains "$REPORT" "Xvfb" \
    "TC-23: the failure report names Xvfb"
assert_matches "$REPORT" "([Dd]isplay[^[:alnum:][:cntrl:]]*(could not|couldn.t|failed|unable|cannot|can.t)|(could not|couldn.t|failed|unable|cannot|can.t)[^[:alnum:][:cntrl:]]*[Dd]isplay)" \
    "TC-23: the failure report says the virtual display could not be allocated or started"
assert_contains "$REPORT" "Fix:" \
    "TC-23: the report ends with an actionable Fix: line"
assert_file_absent "$(launch_marker "game")" \
    "TC-23: the game was not launched"

# ===========================================================================
# TC-24 — internal script error (the ERR trap)
# ===========================================================================

printf '# --- TC-24: internal script error ---\n'

run_preflight_env "PATH=${NO_DISPLAY_PATH}" "XDG_STATE_HOME=${READONLY_STATE}" -- \
    --game-path "${GAME_DIR}"

REPORT="$(failure_report "$RUN_STDERR")"

assert_exit 13 "TC-24: an unwritable XDG_STATE_HOME exits 13"
assert_ne 1 "$RUN_CODE" \
    "TC-24: the exit code is NOT the raw status of the failing command"
assert_matches "$REPORT" "smoke-validation\.sh line [0-9]+" \
    "TC-24: the ERR trap reports the script name and the failing line number"
assert_matches "$REPORT" "([Ff]unction|[Ii]nside)[^[:alnum:][:cntrl:]]*['\`]?[A-Za-z_][A-Za-z0-9_.]*" \
    "TC-24: the ERR trap reports the enclosing function"
assert_matches "$REPORT" "[Ee]xit[^0-9]{0,12}13" \
    "TC-24: the ERR trap reports the exit code"
assert_contains "$REPORT" "Fix:" \
    "TC-24: the report ends with an actionable Fix: line"

# The state root is restored so that the harness can remove the sandbox.
chmod 0700 -- "${READONLY_STATE}"

# ===========================================================================
# FR-012 item 8 — the host-tool sweep fails closed
# ===========================================================================

printf '# --- FR-012 item 8: a missing host tool fails closed with 9 ---\n'

assert_file_exists "${MIN_PATH_DIR}/bash" \
    "tool sweep: the restricted PATH still contains the interpreter"
assert_file_absent "${MIN_PATH_DIR}/Xvfb" \
    "tool sweep: the restricted PATH withholds Xvfb"

run_preflight_env "PATH=${BLOCK_BIN_DIR}:${MIN_PATH_DIR}" -- \
    --game-path "${GAME_DIR}"

REPORT="$(failure_report "$RUN_STDERR")"

assert_exit 9 "tool sweep: a missing host tool exits 9"
assert_contains "$REPORT" "Xvfb" \
    "tool sweep: the failure report names the missing tool"
assert_contains "$REPORT" "Fix:" \
    "tool sweep: the report ends with an actionable Fix: line"
assert_file_absent "$(launch_marker "game")" \
    "tool sweep: the game was not launched"

# ===========================================================================
# The path-property table (FR-012 item 7)
# ===========================================================================

printf '# --- path-property table: P1 — a genuine child of the base ---\n'

# P1: the derived mods path <base>/Mods is a genuine child of the canonicalized
# game directory. The run must accept it: the pre-flight reaches the display
# phase (exit 10 with the failing Xvfb stub) instead of rejecting the path.
run_preflight_env "PATH=${NO_DISPLAY_PATH}" -- \
    --game-path "${BASE_GAME}"

assert_exit 10 "P1: a genuine child of the base is accepted (the display phase is reached)"
assert_code_not_in "${REJECTION_CODES}" "$RUN_CODE" \
    "P1: a genuine child of the base is not rejected with a path code"
assert_dir_exists "${BASE_GAME}/Mods" \
    "P1: the fixture's Mods directory is a genuine child of the base"
assert_file_absent "$(launch_marker "base-game")" \
    "P1: the game was not launched"

printf '# --- path-property table: P2 — a sibling sharing the textual prefix ---\n'

# P2: <base>-evil shares the base's textual prefix, so a naive
# `case "$real" in "$base"*` would treat it as being inside the base. The
# rejection itself is not observable through the CLI (a user-supplied root is
# its own base, FR-012 item 7), so the consequence is asserted: the sibling
# must be left completely untouched.
EVIL_LISTING_BEFORE="$(directory_listing "${BASE_EVIL_GAME}")"

run_preflight_env "PATH=${NO_DISPLAY_PATH}" -- \
    --game-path "${BASE_GAME}"

assert_exit 10 "P2: the run reached the display phase (the base was accepted)"
assert_file_exists "${BASE_EVIL_GAME}/LivingRoots/keep.txt" \
    "P2: the prefix-sharing sibling's deploy directory was not removed"
assert_eq "${EVIL_LISTING_BEFORE}" "$(directory_listing "${BASE_EVIL_GAME}")" \
    "P2: the prefix-sharing sibling was left untouched"
assert_file_absent "$(launch_marker "base-evil-game")" \
    "P2: the game was not launched"

printf '# --- path-property table: P3 — the base carries a trailing slash ---\n'

# P3: a base that keeps its trailing slash yields the pattern "…/game//*",
# which never matches. The base must therefore be canonicalized first.
run_preflight_env "PATH=${NO_DISPLAY_PATH}" -- \
    --game-path "${BASE_GAME}/"

assert_exit 10 "P3: a base with a trailing slash is accepted (the display phase is reached)"
assert_code_not_in "${REJECTION_CODES}" "$RUN_CODE" \
    "P3: a base with a trailing slash is not rejected with a path code"
assert_file_absent "$(launch_marker "base-game")" \
    "P3: the game was not launched"

printf '# --- path-property table: P4 — the candidate contains ".." ---\n'

# P4: realpath resolves the ".." segment, and the result lands inside the
# base, so the candidate is accepted.
run_preflight_env "PATH=${NO_DISPLAY_PATH}" -- \
    --game-path "${BASE_GAME}/child/../child2"

assert_exit 10 "P4: a candidate containing \"..\" is accepted (the display phase is reached)"
assert_code_not_in "${REJECTION_CODES}" "$RUN_CODE" \
    "P4: a candidate containing \"..\" is not rejected with a path code"
assert_file_absent "$(launch_marker "base-dotdot-target")" \
    "P4: the game was not launched"

printf '# --- path-property table: P5 — a symlink pointing outside the base ---\n'

# P5: the candidate is a symlink to a directory outside the base. The
# rejection is not observable through the CLI (a user-supplied root is its own
# base), so the consequence is asserted: the escape target is not touched.
OUTSIDE_LISTING_BEFORE="$(directory_listing "${OUTSIDE_GAME}")"

run_preflight_env "PATH=${NO_DISPLAY_PATH}" -- \
    --game-path "${BASE_ESCAPE_LINK}"

assert_exit 10 "P5: the symlinked root reached the display phase"
assert_eq "${OUTSIDE_LISTING_BEFORE}" "$(directory_listing "${OUTSIDE_GAME}")" \
    "P5: the escape target was left untouched"
assert_file_absent "$(launch_marker "outside-game")" \
    "P5: the game was not launched"

printf '# --- path-property table: P5-prime — a CONSTRUCTED path escaping the base ---\n'

# P5': the direct manifestation of P2 and P5 for the paths the script
# CONSTRUCTS. The game directory's own smapi-internal/ entry is a symlink
# pointing outside the game directory, so the constructed marker path escapes
# the base: the script must refuse the SMAPI installation (FR-012 item 2,
# exit 2) instead of reading the escaping files.
ESCAPE_LISTING_BEFORE="$(directory_listing "${ESCAPE_SMAPI_DIR}")"

run_preflight_env "PATH=${NO_DISPLAY_PATH}" -- \
    --game-path "${ESCAPE_GAME_DIR}"

assert_exit 2 "P5': a constructed path escaping the base is rejected (exit 2)"
assert_eq "${ESCAPE_LISTING_BEFORE}" "$(directory_listing "${ESCAPE_SMAPI_DIR}")" \
    "P5': the escaping SMAPI directory was left untouched"
assert_file_absent "$(launch_marker "escape-game")" \
    "P5': the game was not launched"

printf '# --- path-property table: P6 — a path containing a space ---\n'

# P6: environment-derived names are not restricted by any character
# allowlist — the round-8 decision withdrew the allowlist, because the game's
# own directory ("Stardew Valley") contains a space.
run_preflight_env "PATH=${NO_DISPLAY_PATH}" -- \
    --game-path "${BASE_SPACE_GAME}"

assert_exit 10 "P6: a path containing a space is accepted (the display phase is reached)"
assert_code_not_in "${REJECTION_CODES}" "$RUN_CODE" \
    "P6: a path containing a space is not rejected with a path code"
assert_file_absent "$(launch_marker "space-game")" \
    "P6: the game was not launched"

# ---------------------------------------------------------------------------

finish_tests
