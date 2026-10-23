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
# BOUNDED INVOCATIONS (T128)
# --------------------------
# Every invocation of the script under test goes through the harness's
# `run_script_deadline` helper, so a script that HANGS can neither outlive
# the suite nor leak a child (and the X server it may have started). The
# deadline is deliberately larger than every budget the script enforces
# itself (--timeout, --build-timeout and the display phase's own wait), so a
# healthy run still reports the script's own verdict; the deadline only
# fires when the script under test stops making progress.
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

# The harness is linted IN ITS OWN RIGHT by the same hook step (it matches
# `scripts/tests/**/*.sh`), so it is deliberately not followed from here:
# `-x` would pull it in a second time and report its findings against this
# file.
# shellcheck disable=SC1091  # the harness is linted separately, by the same step
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

# The hard deadline, in seconds, applied to every invocation of the script
# under test. It is deliberately larger than any budget the script enforces
# itself, so the script's own verdict is what a run reports; the deadline
# only fires when the script under test HANGS.
PREFLIGHT_DEADLINE_SECS=120

# The budgets the script under test is given. They bound the phases that
# WAIT, so a case that must not hang cannot spend the suite's time waiting.
# The rebuild budget is deliberately larger than the deadline: the case that
# hangs in the rebuild raises it, so the deadline is the only thing that can
# end that run.
PREFLIGHT_GAME_TIMEOUT_SECS=5
PREFLIGHT_BUILD_TIMEOUT_SECS=5

# run_preflight_env <env-assignments...> -- [args...]
#
# Runs the script under test with the launch-free options every case needs
# (a bounded game timeout, a bounded grace period and a bounded build) plus
# the explicit environment overrides (PATH, XDG_STATE_HOME), under the
# suite's HARD DEADLINE (T128).
#
# The harness's `run_script_deadline` helper takes no environment
# assignments, so the assignments are exported into this shell for the
# duration of the invocation and restored afterwards. The harness honours an
# overridden HOME / XDG_STATE_HOME / XDG_RUNTIME_DIR / XDG_CONFIG_HOME (it
# compares the current value with the one it captured at load time) and it
# does not pin PATH at all, so an exported PATH reaches the script.
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

    if ! declare -F _run_script_impl >/dev/null 2>&1; then
        fail "T128: the suite can bound its invocations" \
            "the harness no longer exposes its invocation path (_run_script_impl), so the suite cannot apply a deadline"
        return 0
    fi

    # The harness's own invocation path is driven directly: RUN_ENV_ASSIGNMENTS
    # carries the assignments — they reach the CHILD's environment only, so
    # the suite's own PATH (and the harness's own `cat`, `mktemp` and `rm`)
    # are untouched — and RUN_DEADLINE_SECS wraps the invocation in
    # `timeout`, which signals the child's whole process group, so a hung
    # script cannot outlive the suite nor leak a child.
    RUN_ENV_ASSIGNMENTS=("${envs[@]+"${envs[@]}"}")
    RUN_DEADLINE_SECS="${PREFLIGHT_DEADLINE_SECS}"
    _run_script_impl \
        --timeout "${PREFLIGHT_GAME_TIMEOUT_SECS}" \
        --grace-period 2 \
        --build-timeout "${PREFLIGHT_BUILD_TIMEOUT_SECS}" \
        "${args[@]+"${args[@]}"}"
    # RUN_ENV_ASSIGNMENTS and RUN_DEADLINE_SECS are the harness's own knobs:
    # the harness READS them when it builds the child's environment and when
    # it wraps the invocation in `timeout`. ShellCheck analyses one file at a
    # time and cannot see that reader, so it reports these two assignments as
    # unused. They are deliberately NOT exported — they steer the harness, not
    # the child.
    # shellcheck disable=SC2034  # read by the harness, which this file drives
    RUN_ENV_ASSIGNMENTS=()
    # shellcheck disable=SC2034  # read by the harness, which this file drives
    RUN_DEADLINE_SECS=0

    return 0
}

# run_preflight_in_fake_repo <env-assignments...> -- [args...]
#
# Like run_preflight_env, but the script under test is invoked through the
# SANDBOX repository's symlink: the repository root the script derives from
# its OWN location is then the sandbox, so the fixture project — and
# everything the build stub writes — stays inside the harness sandbox and
# NOTHING is written into the real repository tree.
#
# The script path is restored even when the invocation failed, so a later
# case never runs against the sandbox repository by accident.
run_preflight_in_fake_repo() {
    local saved_script="${HARNESS_SCRIPT}"

    HARNESS_SCRIPT="${FAKE_REPO_SCRIPT}"
    run_preflight_env "$@" || true
    HARNESS_SCRIPT="${saved_script}"

    return 0
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

# assert_is_direct_child <base> <candidate> <label>
#
# The candidate must be a DIRECT child of the base: it lies under the base
# and the remainder of the path is a single component. A sibling that shares
# the base's textual prefix (<base>-evil) is NOT a child.
assert_is_direct_child() {
    local base="${1-}"
    local candidate="${2-}"
    local label="${3-}"
    local rest=""

    if [[ "$candidate" == "${base}/"* ]]; then
        rest="${candidate#"${base}/"}"
    fi

    if [[ -n "$rest" && "$rest" != */* ]]; then
        pass "$label"
    else
        fail "$label" "expected '${candidate}' to be a direct child of '${base}'"
    fi
    return 0
}

# resolved_mods_path <text>
#
# Prints the mods path the script under test RESOLVED, as it reported it at
# run start. The script logs every run's resolved configuration, so the value
# is read back from the run's own output rather than recomputed here.
#
# The line is "<timestamp> [INFO] [<run-id>] mods-path: <path>"; the run id
# is variable, so the marker is the level-and-run-id prefix that precedes
# the key.
resolved_mods_path() {
    local text="${1-}"
    local line

    while IFS= read -r line || [[ -n "$line" ]]; do
        case "$line" in
            *" mods-path: "*)
                printf '%s' "${line##* mods-path: }"
                return 0
                ;;
        esac
    done <<< "$text"

    return 1
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

# assert_le <actual> <limit> <label> — the actual number must not exceed the
# limit. Both arguments must be plain non-negative integers; anything else
# fails the assertion rather than passing it vacuously.
assert_le() {
    local actual="${1-}"
    local limit="${2-}"
    local label="${3-}"

    if [[ ! "$actual" =~ ^[0-9]+$ ]] || [[ ! "$limit" =~ ^[0-9]+$ ]]; then
        fail "$label" "expected a number <= ${limit}, got '${actual}'"
        return 0
    fi

    if (( actual <= limit )); then
        pass "$label"
    else
        fail "$label" "expected a number <= ${limit}, got '${actual}'"
    fi
    return 0
}

# reap_hanging_profile <profile-dir>
#
# Kills the processes a deadline-killed run left behind. The script under
# test starts its rebuild in its OWN process group (FR-005 requires it), so
# the deadline's group signal never reaches that child and it survives the
# run. The suite must not leak it either.
#
# The reap is scoped to the suite's OWN fixture directory — a path that
# exists only for this run — so no process outside this suite is signalled.
# The TERM is tried first and the KILL is the escalation for a child that
# ignores it.
reap_hanging_profile() {
    local profile_dir="${1-}"
    local pid

    [[ -n "$profile_dir" ]] || return 0

    for pid in $(pgrep -f "${profile_dir}" 2>/dev/null || true); do
        kill -TERM "$pid" 2>/dev/null || true
    done

    sleep 1

    for pid in $(pgrep -f "${profile_dir}" 2>/dev/null || true); do
        kill -KILL "$pid" 2>/dev/null || true
    done

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
# A profile for runs that get PAST the pre-flight
# ---------------------------------------------------------------------------
#
# Almost every case in this file aborts inside the pre-flight, so the suite
# never starts an X server and never builds. Two cases need a run that goes
# FURTHER:
#
#   * T108 observes the LOCK FILE, and the lock is acquired AFTER the
#     rebuild-and-verify step (FR-012 (0));
#   * T128 observes the suite's hard deadline, and a run that HANGS is a run
#     that reached the rebuild (the build is the first phase that waits).
#
# Both need a display phase that SUCCEEDS and a rebuild that SUCCEEDS, so
# both are SIMULATED rather than performed:
#
#   * Xvfb is stubbed with a stub that REPORTS a display number on the
#     descriptor the script passes it (-displayfd 3) and stays alive until
#     the cleanup signals it; xdpyinfo and glxinfo are stubbed with a
#     software-renderer report. No real X server is ever started.
#   * `dotnet` is stubbed with a stub that simulates ModBuildConfig's
#     deploy: it writes the built assembly, the deployed assembly (with the
#     SAME content, so the digest comparison passes) and the deployed
#     manifest (a copy of the project's, so the UniqueID matches).
#
# The mod project MUST live inside the repository (FR-016 (a)), and the
# script derives the repository root from its OWN location. The script under
# test is therefore invoked through a SYMLINK inside a sandbox "repo"
# directory: the sandbox repository becomes the base every path is
# prefix-tested against, so the fixture project — and everything the stub
# writes — stays inside the harness sandbox and NOTHING is written into the
# real repository tree.

# The sandbox repository, the fixture project inside it and the symlink to
# the script under test.
FAKE_REPO="${FIXTURE_ROOT}/fakerepo"
FAKE_REPO_SCRIPT="${FAKE_REPO}/scripts/smoke-validation.sh"
FAKE_PROJECT="${FAKE_REPO}/LivingRoots/LivingRoots.csproj"

# The PATH profile whose display phase and rebuild both SUCCEED.
PASS_DISPLAY_BIN_DIR="${FIXTURE_ROOT}/pass-display"
HANGING_BUILD_BIN_DIR="${FIXTURE_ROOT}/hanging-build"

# The game fixture and the state root the T108 case uses. The lock lives in
# the state root (XDG_STATE_HOME), not in XDG_RUNTIME_DIR: the state root is
# the branch in which the script CREATES the lock's own directory and
# enforces its mode, so "the lock's directory has mode 0700" is a property
# of the script rather than of the harness.
LOCK_GAME_DIR="${FIXTURE_ROOT}/lock/game"
LOCK_STATE_DIR="${FIXTURE_ROOT}/lock/state"
LOCK_DIR="${LOCK_STATE_DIR}/livingroots-smoke"
LOCK_FILE="${LOCK_DIR}/livingroots-smoke.lock"

# make_stub_xvfb_ok <path>
#
# A stub Xvfb that behaves like a working X server: it reports the display
# number it was given on file descriptor 3 (the script passes -displayfd 3)
# and then stays alive until the script's cleanup signals its process group.
make_stub_xvfb_ok() {
    local path="${1-}"

    if [[ -z "$path" ]]; then
        printf 'make_stub_xvfb_ok: a path is required\n' >&2
        return 1
    fi

    mkdir -p -- "$(dirname -- "$path")" || return 1
    # The PATH profile links every host tool into its directory, so the
    # stub's own name is ALREADY a symlink to the real binary. The link is
    # removed first: writing the stub through it would overwrite the real
    # Xvfb on this host.
    rm -f -- "$path" || return 1
    cat > "$path" <<'STUB'
#!/usr/bin/env bash
# Stub Xvfb: answers the runnability probe, reports a display number on the
# descriptor the script passes (-displayfd 3) and then stays alive until the
# cleanup signals it.
set -u

case "${1-}" in
    -version|--version)
        # The pre-flight's runnability probe EXECUTES `Xvfb -version` inside a
        # command substitution, so the probe must answer and RETURN — a stub
        # that only knows how to serve a display would hang the probe.
        printf 'Xvfb version 21.1.24\n'
        exit 0
        ;;
esac

printf '99\n' >&3 2>/dev/null || true
while true; do
    sleep 1
done
STUB
    chmod 0755 -- "$path" 2>/dev/null || true
    return 0
}

# make_stub_xdpyinfo <path> — a stub that reports the GLX and RANDR
# extensions the display probe requires (FR-012 item 4a).
make_stub_xdpyinfo() {
    local path="${1-}"

    if [[ -z "$path" ]]; then
        printf 'make_stub_xdpyinfo: a path is required\n' >&2
        return 1
    fi

    mkdir -p -- "$(dirname -- "$path")" || return 1
    rm -f -- "$path" || return 1
    cat > "$path" <<'STUB'
#!/usr/bin/env bash
# Stub xdpyinfo: reports the GLX and RANDR extensions the display probe
# requires. The script reads the extension list twice (once for the exit
# status, once for the extension names), so both are answered here.
printf 'name of display: :99\n'
printf 'number of extensions: 2\n'
printf 'GLX\n'
printf 'RANDR\n'
exit 0
STUB
    chmod 0755 -- "$path" 2>/dev/null || true
    return 0
}

# make_stub_glxinfo <path> — a stub that reports a software rasteriser and a
# framebuffer-object extension (FR-012 item 4b/4c/4d).
make_stub_glxinfo() {
    local path="${1-}"

    if [[ -z "$path" ]]; then
        printf 'make_stub_glxinfo: a path is required\n' >&2
        return 1
    fi

    mkdir -p -- "$(dirname -- "$path")" || return 1
    rm -f -- "$path" || return 1
    cat > "$path" <<'STUB'
#!/usr/bin/env bash
# Stub glxinfo: reports llvmpipe and a framebuffer-object extension, which
# is what the software-OpenGL probe requires.
printf 'OpenGL vendor string: Mesa\n'
printf 'OpenGL renderer string: llvmpipe (LLVM 15.0, 256 bits)\n'
printf 'OpenGL version string: 3.3 (Core Profile) Mesa 23.0.0\n'
printf ' GL_ARB_framebuffer_object\n'
exit 0
STUB
    chmod 0755 -- "$path" 2>/dev/null || true
    return 0
}

# make_stub_dotnet_build <path> [hang]
#
# A stub `dotnet` that simulates `dotnet build`:
#
#   * it reads the deploy target out of the build's -p:GameModsPath= option;
#   * it writes the built assembly into the project's bin/ directory and the
#     deployed assembly into <mods-path>/<ModFolderName>/, both with the SAME
#     content, so the digest comparison of FR-016 (d) passes;
#   * it copies the project's manifest into the deployed folder, so the
#     deployed UniqueID matches the project's.
#
# With the second argument "hang" it never returns, so the script under test
# waits in the rebuild phase until the suite's deadline kills it.
make_stub_dotnet_build() {
    local path="${1-}"
    local hang="${2-}"

    if [[ -z "$path" ]]; then
        printf 'make_stub_dotnet_build: a path is required\n' >&2
        return 1
    fi

    mkdir -p -- "$(dirname -- "$path")" || return 1
    rm -f -- "$path" || return 1
    cat > "$path" <<'STUB'
#!/usr/bin/env bash
# Stub dotnet: simulates `dotnet build` and the deploy ModBuildConfig
# performs, so the rebuild-and-verify phase of the script under test can
# succeed without a real build and without writing into the repository.
set -u

mods_path=""
project=""
for arg in "$@"; do
    case "$arg" in
        -p:GameModsPath=*) mods_path="${arg#-p:GameModsPath=}" ;;
        *.csproj) project="$arg" ;;
    esac
done

if [[ "@@HANG@@" == "1" ]]; then
    # A build that never returns: the script under test waits for it until
    # the suite's deadline fires.
    while true; do
        sleep 1
    done
fi

if [[ -z "$project" || -z "$mods_path" ]]; then
    printf 'dotnet stub: no project or no mods path\n' >&2
    exit 1
fi

project_dir="$(dirname -- "$project")"
folder="$(basename -- "$project")"
folder="${folder%.csproj}"
manifest="${project_dir}/manifest.json"

field() {
    sed -n "s/.*\"$1\"[[:space:]]*:[[:space:]]*\"\([^\"]*\)\".*/\1/p" -- "$2" 2>/dev/null | head -1
}

entry_dll="$(field EntryDll "$manifest")"
if [[ -z "$entry_dll" ]]; then
    printf 'dotnet stub: the project manifest has no EntryDll\n' >&2
    exit 1
fi

mkdir -p -- "${project_dir}/bin" || exit 1
printf 'fake built assembly\n' > "${project_dir}/bin/${entry_dll}" || exit 1

mkdir -p -- "${mods_path}/${folder}" || exit 1
printf 'fake built assembly\n' > "${mods_path}/${folder}/${entry_dll}" || exit 1
cp -- "$manifest" "${mods_path}/${folder}/manifest.json" || exit 1

exit 0
STUB
    # The hang flag is baked into the stub: the second argument decides
    # whether the build returns or never does.
    if [[ "$hang" == "hang" ]]; then
        sed -i 's|@@HANG@@|1|' -- "$path" 2>/dev/null || true
    else
        sed -i 's|@@HANG@@|0|' -- "$path" 2>/dev/null || true
    fi
    chmod 0755 -- "$path" 2>/dev/null || true
    return 0
}

# build_pass_display_path
#
# Prints the path of a PATH profile in which the display phase and the
# rebuild both SUCCEED: the display stubs come first, then the build stub,
# then every host tool the suite needs.
build_pass_display_path() {
    local dir="${PASS_DISPLAY_BIN_DIR}"
    local d name found

    rm -rf -- "$dir"
    mkdir -p -- "$dir" || return 1

    for d in /usr/local/bin /usr/bin /bin /usr/sbin /sbin; do
        [[ -d "$d" ]] || continue
        ln -sf -- "$d"/* "$dir/" 2>/dev/null || true
    done

    for name in bash dotnet; do
        found="$(command -v "$name" 2>/dev/null || true)"
        [[ -n "$found" ]] || continue
        ln -sf -- "$found" "${dir}/${name}" 2>/dev/null || true
    done

    # The display stubs, in front of the real X server.
    make_stub_xvfb_ok "${dir}/Xvfb" || return 1
    make_stub_xdpyinfo "${dir}/xdpyinfo" || return 1
    make_stub_glxinfo "${dir}/glxinfo" || return 1

    # The build stub, in front of the real `dotnet`.
    make_stub_dotnet_build "${dir}/dotnet" || return 1

    printf '%s' "$dir"
}

# build_hanging_build_path
#
# Prints the path of a PATH profile in which the display phase SUCCEEDS but
# the rebuild NEVER RETURNS. A run with that profile hangs in the rebuild
# phase until the suite's deadline kills it.
build_hanging_build_path() {
    local dir="${HANGING_BUILD_BIN_DIR}"
    local d name found

    rm -rf -- "$dir"
    mkdir -p -- "$dir" || return 1

    for d in /usr/local/bin /usr/bin /bin /usr/sbin /sbin; do
        [[ -d "$d" ]] || continue
        ln -sf -- "$d"/* "$dir/" 2>/dev/null || true
    done

    for name in bash dotnet; do
        found="$(command -v "$name" 2>/dev/null || true)"
        [[ -n "$found" ]] || continue
        ln -sf -- "$found" "${dir}/${name}" 2>/dev/null || true
    done

    make_stub_xvfb_ok "${dir}/Xvfb" || return 1
    make_stub_xdpyinfo "${dir}/xdpyinfo" || return 1
    make_stub_glxinfo "${dir}/glxinfo" || return 1
    make_stub_dotnet_build "${dir}/dotnet" "hang" || return 1

    printf '%s' "$dir"
}

# ---------------------------------------------------------------------------
# Fixtures
# ---------------------------------------------------------------------------

install_fake_game "${GAME_DIR}" "game"
# TC-21's fixture must actually be broken: the harness's own
# make_broken_smapi_dir omits smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll,
# which is the file TC-21 asserts the report names. (A complete fake game dir
# cannot serve this case: the path-table rows use the same fixture and expect
# the run to get PAST the SMAPI check.)
make_broken_smapi_dir "${BROKEN_SMAPI_DIR}" || return 1
install_launch_sentinel "${BROKEN_SMAPI_DIR}" "broken-smapi"
# TC-22's fixture must actually carry the wrong app id: the harness's own
# make_bad_steam_dir writes 99999, which is what TC-22 asserts the report
# names. (A complete fake game dir cannot serve this case — it would carry
# the correct 413150 and the run would get past the Steam check.)
make_bad_steam_dir "${BAD_STEAM_DIR}" || return 1
install_launch_sentinel "${BAD_STEAM_DIR}" "bad-steam"

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

# The sandbox repository the "past the pre-flight" runs use: the script
# under test is reached through a SYMLINK inside it, so the repository root
# it derives from its own location is the sandbox and the fixture project
# never touches the real repository tree.
mkdir -p -- "${FAKE_REPO}/scripts" "${FAKE_REPO}/LivingRoots"
ln -sf -- "${HARNESS_SCRIPT}" "${FAKE_REPO_SCRIPT}"
printf '<Project Sdk="Microsoft.NET.Sdk" />\n' > "${FAKE_PROJECT}"
cat > "${FAKE_REPO}/LivingRoots/manifest.json" <<'MANIFEST'
{
  "Name": "Stardew Valley - Living Roots",
  "Author": "lfgranja",
  "Version": "0.0.1",
  "UniqueID": "lfgranja.LivingRoots",
  "EntryDll": "LivingRoots.dll",
  "MinimumApiVersion": "4.0.0"
}
MANIFEST

# The game fixture the lock case uses. It carries the launch sentinel, so a
# launch would be observable.
install_fake_game "${LOCK_GAME_DIR}" "lock-game"

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
# TC-8's package clause: the report must name the PACKAGE that provides the
# missing tool, not just the tool (quickstart TC-8, FR-012 item 8).
#
# The package is asserted in its PAIRED form — the report names the missing
# tool TOGETHER WITH the package that provides it — and not as a bare
# package name: the report's remedy line names every package of the
# distribution as a general hint, so a bare containment check would pass on
# a report that never named the package of the missing tool at all.
assert_matches "$REPORT" "Xvfb\(xorg-x11-server-Xvfb\)" \
    "tool sweep (TC-8): the failure report names the package that provides the missing tool"
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

# The row's evidence is the MODS PATH THE SCRIPT RESOLVED, not the fixture's
# own directory: the fixture creates <base>/Mods itself, so asserting that
# the directory exists proves nothing about the script. The script reports
# the resolved mods path at run start, so the value under test is the one
# the script produced.
P1_MODS_PATH="$(resolved_mods_path "$RUN_STDERR")"

assert_ne "" "$P1_MODS_PATH" \
    "P1: the script reports the mods path it resolved"
assert_eq "${BASE_GAME}/Mods" "$P1_MODS_PATH" \
    "P1: the script resolved the mods path to the base's own Mods child"
assert_is_direct_child "${BASE_GAME}" "$P1_MODS_PATH" \
    "P1: the script's resolved mods path is a genuine child of the base"
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
# Convergence fixtures and helpers (T060, T069, T078, T081)
# ---------------------------------------------------------------------------

# One PATH profile per withheld tool. Each profile holds every host tool the
# suite needs — the failing Xvfb stub first, so a run that gets past the tool
# sweep never starts a real X server, and the `dotnet` stub after the
# standard directories, so a run that gets as far as the build refuses to
# build the real mod.
PATH_WITHOUT_DIR="${FIXTURE_ROOT}/path-without"

# build_path_without <tool>
#
# Prints the path of a directory that holds every host tool EXCEPT the one
# named. The interpreter is linked in explicitly: the script's shebang is
# `#!/usr/bin/env bash`, so with an empty PATH the kernel cannot find the
# interpreter at all and the run dies with the shell's own 127 before the
# tool sweep ever runs.
# build_path_without <tool> [<tool>...]
#
# Prints the path of a directory that holds every host tool EXCEPT the ones
# named. The interpreter is linked in explicitly: the script's shebang is
# `#!/usr/bin/env bash`, so with an empty PATH the kernel cannot find the
# interpreter at all and the run dies with the shell's own 127 before the
# tool sweep ever runs.
build_path_without() {
    local dir
    local d name found withheld
    local -a withheld_tools=()

    for withheld in "$@"; do
        [[ -n "$withheld" ]] || continue
        withheld_tools+=("$withheld")
    done

    if (( ${#withheld_tools[@]} == 0 )); then
        printf 'build_path_without: at least one tool must be named\n' >&2
        return 1
    fi

    dir="${PATH_WITHOUT_DIR}/$(printf '%s' "${withheld_tools[*]}" | tr ' ' '-')"
    rm -rf -- "$dir"
    mkdir -p -- "$dir" || return 1

    for d in /usr/local/bin /usr/bin /bin /usr/sbin /sbin; do
        [[ -d "$d" ]] || continue
        ln -sf -- "$d"/* "$dir/" 2>/dev/null || true
    done

    for name in bash dotnet; do
        found="$(command -v "$name" 2>/dev/null || true)"
        [[ -n "$found" ]] || continue
        ln -sf -- "$found" "${dir}/${name}" 2>/dev/null || true
    done

    # The failing Xvfb stub, in front of the real X server.
    if ! _is_withheld "Xvfb" "${withheld_tools[@]}"; then
        ln -sf -- "${STUB_BIN_DIR}/Xvfb" "${dir}/Xvfb" 2>/dev/null || true
    fi

    # The build blocker, in front of the real `dotnet`.
    if ! _is_withheld "dotnet" "${withheld_tools[@]}"; then
        ln -sf -- "${BLOCK_BIN_DIR}/dotnet" "${dir}/dotnet" 2>/dev/null || true
    fi

    # The tools this profile withholds.
    for withheld in "${withheld_tools[@]}"; do
        rm -f -- "${dir}/${withheld}"
    done

    printf '%s' "$dir"
}

# _is_withheld <tool> <withheld...> — true when the tool is among the
# withheld ones.
_is_withheld() {
    local tool="${1-}"
    shift
    local candidate

    for candidate in "$@"; do
        if [[ "$candidate" == "$tool" ]]; then
            return 0
        fi
    done

    return 1
}

# build_pidof_path
#
# Prints the path of a PATH profile that has `pidof` but NOT `pgrep`.
# FR-012 item 8 accepts either tool, so a host with only `pidof` is a
# supported host.
build_pidof_path() {
    local dir
    local found

    dir="$(build_path_without "pgrep")"

    found="$(command -v pidof 2>/dev/null || true)"
    if [[ -n "$found" ]]; then
        ln -sf -- "$found" "${dir}/pidof" 2>/dev/null || true
    fi

    printf '%s' "$dir"
}

# assert_contains_any <haystack> <csv-needles> <label>
#
# The haystack must contain at least ONE of the needles.
assert_contains_any() {
    local haystack="${1-}"
    local needles="${2-}"
    local label="${3-}"
    local needle

    for needle in ${needles//|/ }; do
        if [[ "$haystack" == *"$needle"* ]]; then
            pass "$label"
            return 0
        fi
    done

    fail "$label" "expected to contain one of [${needles}]"
    return 0
}

# report_what <report> — the "What:" line of the failure report.
#
# FR-007's report pattern is "What → Where → How to fix": the "What:" line
# names the FAILED COMPONENT, so the missing tool has to appear there and
# not merely somewhere in the shell's own diagnostics.
report_what() {
    local report="${1-}"
    local line

    while IFS= read -r line || [[ -n "$line" ]]; do
        case "$line" in
            *"What: "*)
                printf '%s' "${line##*What: }"
                return 0
                ;;
        esac
    done <<< "$report"

    return 1
}

# ===========================================================================
# T069 — the host-tool sweep covers every external the script invokes
# ===========================================================================
#
# The script's REQUIRED_TOOLS array must name every external command the
# script actually invokes. A tool that is used but NOT verified is a tool
# whose absence is silently misclassified: a host without `sort`, for
# example, reports a healthy installation as "SMAPI not functional" (exit
# 2) instead of as a missing host tool (exit 9).
#
# The property is a table over the tools: each row withholds exactly ONE
# tool from the PATH and asserts that the run fails closed with exit 9 and
# names the missing tool. The sweep runs FIRST (FR-012 (0)), so the removal
# must abort the run before any build or launch.
#
# The first group is the set the sweep omits today; the second group is the
# set FR-012 item 8 already names, and it is the CONTROL group — those rows
# already hold, so a green row proves the sweep really is reached.
#
# NOTE: the tools whose absence breaks the run BEFORE the sweep (the run log
# is created before the pre-flight) are part of the table on purpose: a
# missing host tool must be reported as a missing host tool, never as an
# internal error (13) or as the shell's own 127.

printf '# --- T069: the host-tool sweep covers every external the script invokes ---\n'

PREFLIGHT_SWEEP_TOOLS=(
    cp cat sort touch mkdir chmod rm dirname basename tail
    Xvfb xdpyinfo glxinfo flock timeout mktemp stat realpath
    sha256sum date grep dotnet ps readlink sed awk cut head tr
    sleep mkfifo
)

for sweep_tool in "${PREFLIGHT_SWEEP_TOOLS[@]}"; do
    sweep_path="$(build_path_without "$sweep_tool")"

    if [[ "$sweep_tool" == "timeout" ]]; then
        # The ONE row that cannot be wrapped in the suite's deadline.
        #
        # The deadline is applied by the harness with `env <assignments>
        # timeout ... <script>`, and `env` resolves the program it is asked
        # to run in the PATH it is GIVEN — so a PATH that withholds
        # `timeout` cannot launch the wrapper at all, and the run would die
        # with the shell's own 127 before the script under test even
        # started. The row therefore runs without the wrapper. That is
        # safe: the script's own tool sweep aborts the run in milliseconds,
        # so the run cannot outlive the suite.
        PREFLIGHT_DEADLINE_SECS=0
    fi

    run_preflight_env "PATH=${sweep_path}" -- --game-path "${GAME_DIR}"

    PREFLIGHT_DEADLINE_SECS=120

    REPORT="$(failure_report "$RUN_STDERR")"
    WHAT="$(report_what "$REPORT" || printf '')"

    assert_exit 9 \
        "tool sweep: a host without '${sweep_tool}' fails closed with 9"
    assert_matches "$WHAT" "(^|[^A-Za-z0-9_])${sweep_tool}([^A-Za-z0-9_]|$)" \
        "tool sweep: the report names the missing tool '${sweep_tool}'"
done

# ===========================================================================
# T143 — the `pgrep` OR `pidof` alternative, asserted as the rule it is
# ===========================================================================
#
# FR-012 item 8 names `pgrep` (or `pidof)` explicitly: the rule is an
# ALTERNATIVE, so either tool satisfies it and only the absence of BOTH is
# a missing host tool. T081 covers the positive half for `pidof`; the two
# cases below cover the other half of the rule:
#
#   * `pgrep` alone satisfies it — the sweep must let the run through; and
#   * NEITHER tool satisfies it — the run must fail closed with exit 9 and
#     name the missing capability.
#
# The second case is the one the spec's wording makes MANDATORY: a host
# without both tools cannot run the orphan-Xvfb detection at all, and it
# must be reported as a missing host tool rather than as an internal error.

printf '# --- T143: the pgrep-or-pidof alternative, asserted as a rule ---\n'

# `pgrep` present, `pidof` absent: EITHER tool satisfies the requirement.
PGREP_ONLY_PATH="$(build_path_without "pidof")"

assert_file_exists "${PGREP_ONLY_PATH}/pgrep" \
    "T143: the restricted PATH provides pgrep"
assert_file_absent "${PGREP_ONLY_PATH}/pidof" \
    "T143: the restricted PATH withholds pidof"

run_preflight_env "PATH=${PGREP_ONLY_PATH}" -- --game-path "${GAME_DIR}"

# The sweep must be satisfied: the run gets past it and reaches the display
# phase, where the failing Xvfb stub ends it.
assert_exit 10 \
    "T143: pgrep alone satisfies the pgrep-or-pidof requirement (the sweep lets the run through)"
assert_ne 9 "$RUN_CODE" \
    "T143: pgrep alone is NOT reported as a missing host tool"

# NEITHER tool: the run must fail closed with exit 9 and name the missing
# capability.
NEITHER_PATH="$(build_path_without "pgrep" "pidof")"

assert_file_absent "${NEITHER_PATH}/pgrep" \
    "T143: the restricted PATH withholds pgrep"
assert_file_absent "${NEITHER_PATH}/pidof" \
    "T143: the restricted PATH withholds pidof"

run_preflight_env "PATH=${NEITHER_PATH}" -- --game-path "${GAME_DIR}"

REPORT="$(failure_report "$RUN_STDERR")"
WHAT="$(report_what "$REPORT" || printf '')"

assert_exit 9 \
    "T143: a host without pgrep AND without pidof fails closed with 9"
assert_matches "$WHAT" "pgrep" \
    "T143: the report names pgrep as part of the missing capability"
assert_matches "$WHAT" "pidof" \
    "T143: the report names pidof as part of the missing capability"
assert_contains "$REPORT" "Fix:" \
    "T143: the report ends with an actionable Fix: line"
assert_file_absent "$(launch_marker "game")" \
    "T143: the game was not launched"

# ===========================================================================
# T137 — every row of the sweep has a REMEDIATION PACKAGE
# ===========================================================================
#
# FR-012 item 8: a missing tool must be reported with the PACKAGE that
# provides it, so the operator can install it, and quickstart TC-8 asserts
# the clause for Xvfb. A wrong package — or an `unknown-package` fallback
# — must not be able to ship unnoticed.
#
# THE CLAUSE IS ASSERTED ON THE REPORT, NOT ON THE SCRIPT'S SOURCE.
#
# The table below is the EXPECTATION: for every tool of the host-tool
# sweep, the package the failure report must name. Every row is checked
# the same way, and the check is BEHAVIOURAL: the row's own tool is
# withheld from the PATH, the script is run, and the report the run printed
# is read. How the script STORES the mapping — an associative array, a
# `case` function, a lookup table in a file of its own — is therefore
# irrelevant to this case: only the package the report NAMES is under
# test.
#
# A source-shape assertion would be a change detector, not a
# specification: it pins the test to ONE representation of the table, so a
# refactor that changes nothing an operator can observe (the same tool
# names, the same packages, the same `unknown-package` fallback, the same
# report line) would still fail the suite. The report is the contract, and
# the report is what is asserted here.
#
# The table is DRIVEN: one row per tool of the sweep, and every row runs
# the same steps. A tool that is added to the sweep without a row here is
# reported by the completeness assertion below, so the two tables cannot
# drift apart silently.
#
# The clause is asserted for every tool the script REPORTS as missing. A
# tool the script does not report at all is a DIFFERENT defect — the tool is
# missing from the script's own sweep — and the sweep's case (T069) is where
# that gap is reported, so it is counted once and not twice.

printf '# --- T137: every sweep row carries its remediation package ---\n'

# | tool | the package that provides it |
TOOL_PACKAGE_TABLE=(
    "Xvfb|xorg-x11-server-Xvfb"
    "xdpyinfo|mesa-utils"
    "glxinfo|mesa-utils"
    "flock|util-linux"
    "timeout|coreutils"
    "mktemp|coreutils"
    "stat|coreutils"
    "realpath|coreutils"
    "sha256sum|coreutils"
    "date|coreutils"
    "grep|grep"
    "dotnet|dotnet-sdk"
    "ps|procps-ng"
    "readlink|coreutils"
    "sed|sed"
    "awk|gawk"
    "cut|coreutils"
    "head|coreutils"
    "tr|coreutils"
    "cp|coreutils"
    "cat|coreutils"
    "sort|coreutils"
    "touch|coreutils"
    "mkdir|coreutils"
    "chmod|coreutils"
    "rm|coreutils"
    "dirname|coreutils"
    "basename|coreutils"
    "tail|coreutils"
    "sleep|coreutils"
    "mkfifo|coreutils"
)

# The script's declared required tools, read out of its source.
#
# This parse does NOT decide the package clause: the clause is asserted on
# the report (see below). The parse only yields the set of tools the script
# DECLARES, which T144's "no dead rows" direction needs in order to tell a
# sweep row that names a real requirement from one that names nothing.
read_required_tools() {
    local inside=0
    local line tool

    while IFS= read -r line || [[ -n "$line" ]]; do
        case "$line" in
            "REQUIRED_TOOLS=("*|"readonly REQUIRED_TOOLS=("*|"REQUIRED_TOOLS = ("*)
                inside=1
                line=""
                ;;
        esac

        if (( inside == 1 )); then
            case "$line" in
                ")"*)
                    inside=0
                    continue
                    ;;
            esac

            for tool in $line; do
                case "$tool" in
                    "#"*) break ;;
                    ")"*) inside=0; break ;;
                    "") ;;
                    *) printf '%s\n' "$tool" ;;
                esac
            done
        fi
    done < "${HARNESS_SCRIPT}"
}

# The table's own tools, one per line.
table_tools() {
    local row tool

    for row in "${TOOL_PACKAGE_TABLE[@]}"; do
        tool="${row%%|*}"
        printf '%s\n' "$tool"
    done
}

# The derived tables are written to files: the suite runs with `pipefail`,
# and a `grep -q` that exits early would SIGPIPE the writer and turn a
# successful match into a failed pipeline.
TOOL_PACKAGE_TABLE_FILE="$(mktemp "${HARNESS_SANDBOX}/tool-package-table.XXXXXXXXXX")"
REQUIRED_TOOLS_FILE="$(mktemp "${HARNESS_SANDBOX}/required-tools.XXXXXXXXXX")"
PREFLIGHT_SWEEP_TOOLS_FILE="$(mktemp "${HARNESS_SANDBOX}/sweep-tools.XXXXXXXXXX")"

table_tools > "$TOOL_PACKAGE_TABLE_FILE"
read_required_tools > "$REQUIRED_TOOLS_FILE"
printf '%s\n' "${PREFLIGHT_SWEEP_TOOLS[@]}" > "$PREFLIGHT_SWEEP_TOOLS_FILE"

# A. Every row of the host-tool SWEEP table must have a row in the package
#    table: a tool that is swept without a recorded package would be
#    reported as `unknown-package`.
for sweep_row_tool in "${PREFLIGHT_SWEEP_TOOLS[@]}"; do
    if grep -qx -- "$sweep_row_tool" "$TOOL_PACKAGE_TABLE_FILE"; then
        pass "T137 [${sweep_row_tool}]: the package table carries a row"
    else
        fail "T137 [${sweep_row_tool}]: the package table carries a row" \
            "the sweep covers '${sweep_row_tool}', but the package table does not — add the row"
    fi
done

# B. The REPORT names the package, row by row. Every row withholds its own
#    tool from the PATH, runs the script and reads the report the run
#    printed. Nothing about the script's internals is inspected: the
#    package is read out of what the operator is shown.
for package_row in "${TOOL_PACKAGE_TABLE[@]}"; do
    package_tool="${package_row%%|*}"
    package_name="${package_row##*|}"

    package_path="$(build_path_without "$package_tool")"

    if [[ "$package_tool" == "timeout" ]]; then
        # The ONE row that cannot be wrapped in the suite's deadline.
        #
        # The deadline is applied by the harness with `env <assignments>
        # timeout ... <script>`, and `env` resolves the program it is asked
        # to run in the PATH it is GIVEN — so a PATH that withholds
        # `timeout` cannot launch the wrapper at all, and the run would die
        # with the shell's own 127 before the script under test even
        # started. The row therefore runs without the wrapper. That is
        # safe: the script's own tool sweep aborts the run in milliseconds,
        # so the run cannot outlive the suite.
        PREFLIGHT_DEADLINE_SECS=0
    fi

    run_preflight_env "PATH=${package_path}" -- --game-path "${GAME_DIR}"

    PREFLIGHT_DEADLINE_SECS=120

    package_report="$(failure_report "$RUN_STDERR")"
    package_what="$(report_what "$package_report" || printf '')"

    if [[ "$package_what" =~ (^|[^A-Za-z0-9_])${package_tool}([^A-Za-z0-9_]|$) ]]; then
        # The script reports this tool as missing, so the report must ALSO
        # name the package that provides it — and it must be the RIGHT one.
        #
        # The pairing is asserted in its `tool(package)` form and not as a
        # bare package name: the report's remedy line names EVERY package
        # of the distribution as a general hint, so a bare containment
        # check would pass on a report that never named the package of the
        # missing tool at all.
        assert_matches "$package_report" "${package_tool}\(${package_name}\)" \
            "T137 [${package_tool}]: the failure report names the package '${package_name}' that provides the missing tool"
    else
        # The script does not report this tool as missing at all, so it
        # names no package for it. That is a DIFFERENT defect — the tool is
        # missing from the script's own sweep — and it is reported by the
        # sweep's case (T069), which fails for exactly those tools. The
        # package clause is vacuous for such a row, so no assertion is
        # made here: the gap is counted once, where it belongs.
        printf '# T137 [%s]: not asserted — the script does not report this tool as missing, so it names no package for it (the sweep case owns that gap)\n' \
            "$package_tool"
    fi
done

# C. Every tool the script DECLARES must be covered by the sweep table, so
#    the two cannot drift apart silently.
while IFS= read -r declared_tool; do
    [[ -n "$declared_tool" ]] || continue

    if grep -qx -- "$declared_tool" "$PREFLIGHT_SWEEP_TOOLS_FILE"; then
        pass "T137 [${declared_tool}]: the sweep table covers the declared tool"
    else
        fail "T137 [${declared_tool}]: the sweep table covers the declared tool" \
            "the script's REQUIRED_TOOLS declares '${declared_tool}', which the sweep table does not cover — add the row"
    fi
done < "$REQUIRED_TOOLS_FILE"

# ===========================================================================
# T144 — the sweep table covers EVERY external the script actually invokes
# ===========================================================================
#
# The section header of the sweep claims that it covers "every external the
# script invokes". The claim is asserted here as a PROPERTY, and the set of
# externals is DERIVED FROM THE SCRIPT'S OWN SOURCE rather than written out
# by hand: a table that is maintained by hand drifts from the script the
# moment the script changes.
#
# The derivation is a small, quote-aware tokenizer:
#
#   * a word is a command only in a COMMAND POSITION — the first word of a
#     line or of a segment, or the word after a pipe, an operator or a
#     compound-command keyword;
#   * the text inside a quoted string is NEVER a command, so a message that
#     happens to name a program does not become a dependency;
#   * a command substitution, a process substitution and a backquote ARE
#     scanned, because those are where the script keeps most of its calls;
#   * a redirection's target is not a command;
#   * a `#` outside quotes starts a comment.
#
# The words the scan finds are then filtered to the EXTERNALS: a name is an
# external when it is not one of bash's own builtins and the host can
# execute it as a program. The second condition keeps the set honest on a
# host that lacks a tool: a tool the host does not have cannot be a
# dependency of a run on that host.
#
# The property has two directions:
#
#   * COVERAGE — every external the script invokes is covered by the sweep
#     table or by one of the ALTERNATIVE rules (a requirement that EITHER
#     tool satisfies). A new external that nobody sweeps is the failure
#     this direction catches.
#   * NO DEAD ROWS — every row of the sweep table names something real: an
#     external the scan found, or a tool the script DECLARES as required
#     (the spec's required set), or a tool this host does not have at all
#     (the row is then about a host that lacks it).
#
# The ALTERNATIVE rules are part of the table because FR-012 item 8 names
# `pgrep` (or `pidof`): the requirement is satisfied by EITHER tool, so the
# two are ONE row of the sweep, not two. T143 asserts the rule itself.

printf '# --- T144: the sweep table covers every external the script invokes ---\n'

# The scanner. It is written to the sandbox so the suite needs no extra
# file in the repository.
SCAN_PROGRAM="${HARNESS_SANDBOX}/scan-commands.awk"

cat > "$SCAN_PROGRAM" <<'SCAN_EOF'
# scan-commands.awk — print the command words a bash script invokes.
#
# One word per line, in the order the words were found. The scanner is
# quote-aware: the text inside a quoted string is never a command, except
# for a command substitution inside it, whose contents ARE scanned.
#
# The quote state PERSISTS across lines, because a quoted string may span
# lines — the script keeps its two awk programs in single-quoted variables,
# and scanning their bodies as bash would report the awk program's own
# identifiers as commands.

function is_op(c) {
    return (c == "|" || c == ";" || c == "&" || c == "(" || c == ")")
}

# match_paren(text, open) — the index of the ")" that closes the "(" at
# `open`. Nested parentheses are counted.
function match_paren(text, open,   i, n, depth, c) {
    n = length(text)
    depth = 0
    for (i = open; i <= n; i++) {
        c = substr(text, i, 1)
        if (c == "(") depth++
        else if (c == ")") {
            depth--
            if (depth == 0) return i
        }
    }
    return n + 1
}

# handle_word(w, out) — classify a completed word.
function handle_word(w, out) {
    if (w == "") return
    if (g_redir == 1) {
        g_redir = 0
        return
    }
    if (g_expect != 1) return
    if (w ~ /^[A-Za-z_][A-Za-z0-9_]*=/) return           # an assignment
    if (w ~ /^(if|then|elif|else|while|until|do|!|time|exec)$/) return
    if (w ~ /^[A-Za-z_][A-Za-z0-9._+-]*$/) {
        out[++g_out_n] = w
        g_expect = 0
        return
    }
    g_expect = 0                                         # a dynamic command
}

# scan(text, out) — tokenize `text` and record the command words in `out`.
function scan(text, out,   i, n, c, j, save_e, save_r, save_mode, save_buf) {
    n = length(text)
    i = 1
    g_expect = 1
    g_redir = 0
    while (i <= n) {
        c = substr(text, i, 1)
        if (g_mode == "w") {
            if (c == " " || c == "\t") { handle_word(g_buf, out); g_buf = ""; i++; continue }
            if (c == "#" && g_buf == "") return               # a comment ends the line
            if (c == "'") { handle_word(g_buf, out); g_buf = ""; g_mode = "s"; i++; continue }
            if (c == "\"") { handle_word(g_buf, out); g_buf = ""; g_mode = "d"; i++; continue }
            if (c == "\\") { g_buf = g_buf substr(text, i + 1, 1); i += 2; continue }
            if (c == "$" && substr(text, i + 1, 1) == "(") {
                handle_word(g_buf, out); g_buf = ""
                j = match_paren(text, i + 1)
                save_e = g_expect; save_r = g_redir; save_mode = g_mode; save_buf = g_buf
                g_mode = "w"; g_buf = ""
                scan(substr(text, i + 2, j - i - 2), out)
                g_expect = save_e; g_redir = save_r; g_mode = save_mode; g_buf = save_buf
                i = j + 1
                continue
            }
            if (c == "`") {
                handle_word(g_buf, out); g_buf = ""
                j = index(substr(text, i + 1), "`")
                if (j == 0) { i = n + 1; continue }
                save_e = g_expect; save_r = g_redir; save_mode = g_mode; save_buf = g_buf
                g_mode = "w"; g_buf = ""
                scan(substr(text, i + 1, j - 1), out)
                g_expect = save_e; g_redir = save_r; g_mode = save_mode; g_buf = save_buf
                i = i + j + 1
                continue
            }
            if (c == "<" && substr(text, i + 1, 1) == "(") {
                handle_word(g_buf, out); g_buf = ""
                j = match_paren(text, i + 1)
                save_e = g_expect; save_r = g_redir; save_mode = g_mode; save_buf = g_buf
                g_mode = "w"; g_buf = ""
                scan(substr(text, i + 2, j - i - 2), out)
                g_expect = save_e; g_redir = save_r; g_mode = save_mode; g_buf = save_buf
                i = j + 1
                continue
            }
            if (is_op(c)) {
                handle_word(g_buf, out); g_buf = ""
                g_expect = 1
                g_redir = 0
                i++
                continue
            }
            if (c == ">" || c == "<") {
                handle_word(g_buf, out); g_buf = ""
                g_redir = 1
                i++
                continue
            }
            g_buf = g_buf c
            i++
            continue
        }
        if (g_mode == "s") {
            if (c == "'") { handle_word(g_buf, out); g_buf = ""; g_mode = "w"; i++; continue }
            g_buf = g_buf c
            i++
            continue
        }
        # g_mode == "d"
        if (c == "\\") { g_buf = g_buf substr(text, i + 1, 1); i += 2; continue }
        if (c == "\"") { handle_word(g_buf, out); g_buf = ""; g_mode = "w"; i++; continue }
        if (c == "$" && substr(text, i + 1, 1) == "(") {
            handle_word(g_buf, out); g_buf = ""
            j = match_paren(text, i + 1)
            save_e = g_expect; save_r = g_redir; save_mode = g_mode; save_buf = g_buf
            g_mode = "w"; g_buf = ""
            scan(substr(text, i + 2, j - i - 2), out)
            g_expect = save_e; g_redir = save_r; g_mode = save_mode; g_buf = save_buf
            i = j + 1
            continue
        }
        g_buf = g_buf c
        i++
    }
    handle_word(g_buf, out)
    g_buf = ""
}

BEGIN {
    g_mode = "w"
    g_buf = ""
}

{
    line = $0

    if (g_delim != "") {
        if (line ~ "^[[:space:]]*" g_delim "[[:space:]]*$") g_delim = ""
        next
    }

    if (match(line, /<<-?'"?[A-Za-z_][A-Za-z0-9_]*"?([^<]|$)/)) {
        g_delim = substr(line, RSTART, RLENGTH)
        sub(/^<<-?'?"?/, "", g_delim)
        sub(/"?([^<]|$)$/, "", g_delim)
    }

    scan(line, g_words)
}

END {
    for (i = 1; i <= g_out_n; i++) print g_words[i]
}
SCAN_EOF

# Bash's own builtins and keywords. A name on this list is never an
# external, even when a file of the same name also exists on the PATH
# (`printf`, `echo`, `test` and `kill` are in both).
SHELL_BUILTINS=": [ alias bg bind break builtin caller cd command compgen complete compopt continue declare dirs disown echo enable eval exec exit export false fc fg getopts hash help history jobs kill let local logout mapfile popd printf pushd pwd read readarray readonly return set shift shopt source suspend test times trap true type typeset ulimit umask unalias unset wait"

# is_shell_builtin <name> — true when the name is one of bash's own
# builtins or keywords.
is_shell_builtin() {
    local candidate="${1-}"
    local builtin

    for builtin in ${SHELL_BUILTINS}; do
        if [[ "$candidate" == "$builtin" ]]; then
            return 0
        fi
    done

    return 1
}

# script_externals — the external commands the script under test invokes,
# derived from its own source, one per line and sorted.
script_externals() {
    local name

    while IFS= read -r name; do
        [[ -n "$name" ]] || continue
        if is_shell_builtin "$name"; then
            continue
        fi
        type -P -- "$name" >/dev/null 2>&1 || continue
        printf '%s\n' "$name"
    done <<< "$1" | LC_ALL=C sort -u
}

# The ALTERNATIVE rules of the sweep: a requirement that EITHER of the named
# tools satisfies. Each entry is a pipe-separated list; the absence of ALL
# of them is a missing host tool.
PREFLIGHT_SWEEP_ALTERNATIVES=(
    "pgrep|pidof"
)

# The tools the scan MUST find. They are the ones the script calls in its
# ordinary phases and that every host able to run the sweep has. The list
# is an ANCHOR: without it a scanner that silently found nothing would make
# the coverage property vacuously true.
PREFLIGHT_SCAN_ANCHORS=(
    sleep mkfifo flock sed awk grep head cut tr sort tail
    stat realpath readlink sha256sum date ps mktemp
    dirname basename mkdir chmod rm cp cat touch
)

# The command words the scan found.
SCANNED_COMMANDS="$(awk -f "$SCAN_PROGRAM" -- "${HARNESS_SCRIPT}" 2>/dev/null || printf '')"

# The externals among them.
SCRIPT_EXTERNALS="$(script_externals "$SCANNED_COMMANDS")"

# The anchor first: a scan that found nothing would make every property
# below pass for the wrong reason.
for scan_anchor in "${PREFLIGHT_SCAN_ANCHORS[@]}"; do
    if printf '%s\n' "$SCRIPT_EXTERNALS" | grep -qx -- "$scan_anchor"; then
        pass "T144 [${scan_anchor}]: the source scan finds the external the script invokes"
    else
        fail "T144 [${scan_anchor}]: the source scan finds the external the script invokes" \
            "the scan of the script's own source did not find '${scan_anchor}' — the scanner is broken, so the coverage property below would be vacuous"
    fi
done

# The alternative rule is part of the sweep: FR-012 item 8 names
# `pgrep` (or `pidof`), so the two tools are ONE requirement.
for alt_row in "${PREFLIGHT_SWEEP_ALTERNATIVES[@]}"; do
    IFS='|' read -r -a alt_tools <<< "$alt_row"

    for alt_tool in "${alt_tools[@]}"; do
        if printf '%s\n' "$SCRIPT_EXTERNALS" | grep -qx -- "$alt_tool"; then
            pass "T144 [${alt_row}]: the script invokes '${alt_tool}', one of the tools that satisfy the alternative"
        else
            fail "T144 [${alt_row}]: the script invokes '${alt_tool}', one of the tools that satisfy the alternative" \
                "the scan did not find '${alt_tool}' — the alternative rule names a tool the script never calls"
        fi
    done
done

# COVERAGE: every external the script invokes is covered by the sweep table
# or by an alternative rule.
while IFS= read -r derived_tool; do
    [[ -n "$derived_tool" ]] || continue

    covered=0

    for sweep_row_tool in "${PREFLIGHT_SWEEP_TOOLS[@]}"; do
        if [[ "$derived_tool" == "$sweep_row_tool" ]]; then
            covered=1
            break
        fi
    done

    if (( covered == 0 )); then
        for alt_row in "${PREFLIGHT_SWEEP_ALTERNATIVES[@]}"; do
            IFS='|' read -r -a alt_tools <<< "$alt_row"
            for alt_tool in "${alt_tools[@]}"; do
                if [[ "$derived_tool" == "$alt_tool" ]]; then
                    covered=1
                    break
                fi
            done
        done
    fi

    if (( covered == 1 )); then
        pass "T144 [${derived_tool}]: the sweep table covers the external the script invokes"
    else
        fail "T144 [${derived_tool}]: the sweep table covers the external the script invokes" \
            "the script invokes '${derived_tool}', but no row of the sweep table (nor an alternative rule) covers it — add the row"
    fi
done <<< "$SCRIPT_EXTERNALS"

# NO DEAD ROWS: every row of the sweep table names something real.
while IFS= read -r table_row_tool; do
    [[ -n "$table_row_tool" ]] || continue

    if printf '%s\n' "$SCRIPT_EXTERNALS" | grep -qx -- "$table_row_tool"; then
        pass "T144 [${table_row_tool}]: the sweep row names an external the script invokes"
        continue
    fi

    if grep -qx -- "$table_row_tool" "$REQUIRED_TOOLS_FILE"; then
        pass "T144 [${table_row_tool}]: the sweep row names a tool the script declares as required"
        continue
    fi

    if type -P -- "$table_row_tool" >/dev/null 2>&1; then
        fail "T144 [${table_row_tool}]: the sweep row names something real" \
            "the sweep table sweeps '${table_row_tool}', but the script neither invokes it nor declares it as required — the row would assert a dependency that does not exist"
    else
        pass "T144 [${table_row_tool}]: the sweep row names a tool this host does not have"
    fi
done < "$PREFLIGHT_SWEEP_TOOLS_FILE"

# ===========================================================================
# T108 — the lock file: private, in a private directory, and it SURVIVES
# ===========================================================================
#
# FR-002: the flock is taken on a lock file that is deliberately LEFT IN
# PLACE — removing the name detaches it from the locked inode and re-opens
# the race window the lock exists to close. The lock's own properties are
# therefore observable only AFTER a run: the file must exist, it must be
# readable by its owner alone (0600) and the directory that holds it must
# be traversable by its owner alone (0700).
#
# The lock is acquired AFTER the rebuild-and-verify phase, so the case
# needs a run that gets PAST the pre-flight: the display phase and the
# rebuild are SIMULATED (see the "profile for runs that get PAST the
# pre-flight" section above), and the script under test is invoked through
# the sandbox repository's symlink so that nothing is written into the real
# repository tree.
#
# XDG_RUNTIME_DIR is BLANKED for this run on purpose. With it set, the
# lock lives directly in the runtime directory, whose mode the HARNESS
# chooses — so "the lock's directory has mode 0700" would be a property of
# the harness and not of the script. With it blanked, the script creates
# the lock's own directory under the state root and enforces the mode
# itself, which is the property FR-002 is about.

printf '# --- T108: the lock file is private and survives the run ---\n'

mkdir -p -- "${LOCK_STATE_DIR}"

T108_DISPLAY_PATH="$(build_pass_display_path)"

run_preflight_in_fake_repo \
    "PATH=${T108_DISPLAY_PATH}" \
    "XDG_STATE_HOME=${LOCK_STATE_DIR}" \
    "XDG_RUNTIME_DIR=" \
    -- \
    --game-path "${LOCK_GAME_DIR}"

# The run must have reached the lock phase — otherwise the file below was
# never created by the script and the assertions would be vacuous.
assert_contains "$RUN_STDERR" "lock acquired" \
    "T108: the run reached the lock phase"

assert_file_exists "${LOCK_FILE}" \
    "T108: the lock file survives the run (it is deliberately never removed)"
assert_file_mode "${LOCK_FILE}" 600 \
    "T108: the lock file's mode is 0600"
assert_dir_mode "${LOCK_DIR}" 700 \
    "T108: the lock file's directory has mode 0700"

# The lock's own directory is the script's: it is created and chmod'ed by
# the script, so the run must have left it there.
assert_dir_exists "${LOCK_DIR}" \
    "T108: the script created the lock's own directory"

# ===========================================================================
# T128 — the suite's HARD DEADLINE bounds a hung script under test
# ===========================================================================
#
# Every invocation in this suite goes through the harness's
# `run_script_deadline` helper (see run_preflight_env above), so a script
# under test that HANGS can neither outlive the suite nor leak a child —
# and the X server it may have started.
#
# The case below proves it: the rebuild stub never returns, so the only
# thing that can end the run is the deadline. `timeout` reports its OWN 124
# when its deadline fires, so that exit code says the deadline ended the
# run rather than the script reporting a verdict of its own.

printf '# --- T128: the suite bounds a hung script under test ---\n'

T128_HANGING_PATH="$(build_hanging_build_path)"

# The deadline is deliberately short: the run is SUPPOSED to hang, so the
# deadline is the only thing that can end it. The rebuild budget is raised
# above the deadline for the same reason — with the script's own budget
# expiring first, the run would report the script's verdict (12) instead of
# the deadline's.
T128_SAVED_DEADLINE="${PREFLIGHT_DEADLINE_SECS}"
T128_SAVED_BUILD_TIMEOUT="${PREFLIGHT_BUILD_TIMEOUT_SECS}"
PREFLIGHT_DEADLINE_SECS=20
PREFLIGHT_BUILD_TIMEOUT_SECS=300

T128_START="$(date +%s)"
run_preflight_in_fake_repo "PATH=${T128_HANGING_PATH}" -- \
    --game-path "${LOCK_GAME_DIR}"
T128_ELAPSED=$(( $(date +%s) - T128_START ))

PREFLIGHT_DEADLINE_SECS="${T128_SAVED_DEADLINE}"
PREFLIGHT_BUILD_TIMEOUT_SECS="${T128_SAVED_BUILD_TIMEOUT}"

assert_exit 124 \
    "T128: a hung run is ended by the suite's deadline (timeout's own 124)"

# The run must have REACHED the rebuild: a run that failed earlier would
# have returned the script's own verdict and the deadline would never have
# fired.
assert_contains "$RUN_STDERR" "building" \
    "T128: the hung run reached the rebuild phase"

assert_le "$T128_ELAPSED" 45 \
    "T128: the hung run was ended within the deadline's bound"

# The X server the run started is gone: the script's own cleanup ran when
# the deadline's signal reached it.
assert_eq "" "$(pgrep -f "${HANGING_BUILD_BIN_DIR}/Xvfb" 2>/dev/null || printf '')" \
    "T128: the X server the hung run started did not outlive the suite"

# The rebuild runs in its OWN process group (FR-005 requires it), so the
# deadline's group signal never reaches it and the child survives the run.
# The SUITE must not leak it: the reap below is scoped to the suite's own
# fixture directory, so nothing outside this run is touched.
reap_hanging_profile "${HANGING_BUILD_BIN_DIR}"

assert_eq "" "$(pgrep -f "${HANGING_BUILD_BIN_DIR}" 2>/dev/null || printf '')" \
    "T128: no child of the hung run outlived the suite"




# ===========================================================================
# T060 — the --mod-project path is validated (FR-016 (a), FR-012 item 7)
# ===========================================================================
#
# FR-016 (a): "The path MUST be resolved with `realpath` and MUST satisfy
# the component-boundary prefix test of FR-012 item 7 against the
# canonicalized repository root." A project OUTSIDE the repository must
# therefore be rejected — the run must not build it and must not launch the
# game. A project that does not exist must be rejected too: FR-012 item 7
# resolves a user-supplied root with `realpath -e`, which requires it to
# exist.
#
# The spec does NOT pin an exit code for this rejection — FR-007's taxonomy
# has no entry for "the mod project is outside the repository" — so the
# expectation is the one the requirement states: a non-zero exit that is
# not 0, no build and no launch.
#
# "The build never ran" is observed through the `dotnet` stub: it refuses
# to build and says so on stderr, so the ABSENCE of that message is the
# evidence. "The game was never launched" is observed through the launch
# sentinel the apphost stub writes.
#
# The accepted rows need the run to REACH the build, so they use the real
# Xvfb (the failing stub would stop the run in the display phase, before the
# build, and the "the build ran" evidence would be vacuous).

printf '# --- T060: the --mod-project path is validated ---\n'

T060_GAME_DIR="${FIXTURE_ROOT}/t060/game"
T060_OUTSIDE_DIR="${FIXTURE_ROOT}/t060/outside"

install_fake_game "${T060_GAME_DIR}" "t060-game"

# A project OUTSIDE the repository, with a manifest — the manifest is what
# the current implementation checks before it builds, so the ONLY reason to
# reject this project is the containment test.
mkdir -p -- "${T060_OUTSIDE_DIR}"
cp -- "${HARNESS_REPO_ROOT}/LivingRoots/manifest.json" \
    "${T060_OUTSIDE_DIR}/manifest.json"
printf '<Project Sdk="Microsoft.NET.Sdk" />\n' \
    > "${T060_OUTSIDE_DIR}/LivingRoots.csproj"

# | case | --mod-project | expectation |
T060_PROJECT_TABLE=(
    "default|(default)|accepted"
    "outside|${T060_OUTSIDE_DIR}/LivingRoots.csproj|rejected"
    "dotdot|${HARNESS_REPO_ROOT}/LivingRoots/../LivingRoots/LivingRoots.csproj|accepted"
    "missing|${HARNESS_REPO_ROOT}/LivingRoots/NoSuchProject.csproj|rejected"
)

for t060_row in "${T060_PROJECT_TABLE[@]}"; do
    IFS='|' read -r t060_case t060_project t060_expect <<< "$t060_row"

    if [[ "$t060_project" == "(default)" ]]; then
        run_preflight_env "PATH=${NO_BUILD_PATH}" -- \
            --game-path "${T060_GAME_DIR}"
    else
        run_preflight_env "PATH=${NO_BUILD_PATH}" -- \
            --game-path "${T060_GAME_DIR}" --mod-project "$t060_project"
    fi

    REPORT="$(failure_report "$RUN_STDERR")"

    if [[ "$t060_expect" == "accepted" ]]; then
        assert_contains "$RUN_STDERR" "the build is disabled" \
            "T060 [${t060_case}]: the mod project is accepted — the run reaches the build"
    else
        assert_ne 0 "$RUN_CODE" \
            "T060 [${t060_case}]: the run is rejected with a non-zero exit code"
        assert_not_contains "$RUN_STDERR" "the build is disabled" \
            "T060 [${t060_case}]: the build never ran"
        assert_contains_any "$REPORT" "${t060_project}|repositor" \
            "T060 [${t060_case}]: the report identifies the rejected project"
    fi

    assert_file_absent "$(launch_marker "t060-game")" \
        "T060 [${t060_case}]: the game was never launched"
done

# ===========================================================================
# T081 — the orphan-Xvfb detection works when only `pidof` is available
# ===========================================================================
#
# FR-012 item 8 accepts `pgrep` OR `pidof`, and item 10 requires the
# pre-flight to DETECT and REPORT foreign Xvfb instances. A scan that uses
# `pgrep` alone therefore reports nothing at all on a `pidof`-only host.
#
# The double is a copy of a long-running binary under the name "Xvfb": its
# /proc/<pid>/exe points at that copy and its kernel process name is
# "Xvfb", which is exactly what the detection has to recognise. It is NOT on
# the PATH (the PATH's Xvfb is the failing stub), so neither the tool sweep
# nor the display phase is affected.

printf '# --- T081: the orphan-Xvfb detection works when only pidof is available ---\n'

ORPHAN_STUB_DIR="${FIXTURE_ROOT}/orphan"
ORPHAN_STUB="${ORPHAN_STUB_DIR}/Xvfb"

if ! require_tool sleep "sleep is available for the orphan-Xvfb double"; then
    printf '# (the orphan-Xvfb case needs a long-running binary for the double)\n'
else
    mkdir -p -- "${ORPHAN_STUB_DIR}"
    cp -- "$(command -v sleep)" "${ORPHAN_STUB}" 2>/dev/null || true
    chmod 0755 -- "${ORPHAN_STUB}" 2>/dev/null || true

    "${ORPHAN_STUB}" 300 >/dev/null 2>&1 &
    ORPHAN_PID=$!

    assert_file_exists "${ORPHAN_STUB}" \
        "T081: the orphan Xvfb double exists"
    assert_eq "${ORPHAN_STUB}" \
        "$(readlink -f "/proc/${ORPHAN_PID}/exe" 2>/dev/null || printf '')" \
        "T081: the double's /proc/<pid>/exe is the Xvfb copy"

    PIDOF_PATH="$(build_pidof_path)"

    assert_file_absent "${PIDOF_PATH}/pgrep" \
        "T081: the restricted PATH withholds pgrep"
    assert_file_exists "${PIDOF_PATH}/pidof" \
        "T081: the restricted PATH provides pidof"

    run_preflight_env "PATH=${PIDOF_PATH}" -- --game-path "${GAME_DIR}"

    # The pre-flight does not abort on a foreign Xvfb (FR-012 item 10), so
    # the run reaches the display phase, where the failing stub ends it.
    assert_exit 10 \
        "T081: the run reaches the display phase and fails there"

    assert_matches "$RUN_STDERR" "(pid|PID)[ =:]+${ORPHAN_PID}([^0-9]|$)" \
        "T081: the report names the orphan Xvfb's PID"
    assert_contains "$RUN_STDERR" "${ORPHAN_STUB}" \
        "T081: the report names the orphan Xvfb's executable path"
    assert_matches "$RUN_STDERR" "[Ss]tart[ _]?[Tt]ime" \
        "T081: the report carries the process start time"
    assert_matches "$RUN_STDERR" "(^|[^A-Za-z0-9_])[Uu][Ii][Dd]([^A-Za-z0-9_]|$)" \
        "T081: the report carries the owning UID"
    assert_matches "$RUN_STDERR" "[Rr]eport(ed|ing)" \
        "T081: the report states that the instance is only reported"
    assert_matches "$RUN_STDERR" "never[^[:alnum:]]+signall?" \
        "T081: the report states that the instance is never signalled"

    if kill -0 "$ORPHAN_PID" 2>/dev/null; then
        pass "T081: the orphan Xvfb was NOT signalled (it is still alive after the run)"
    else
        fail "T081: the orphan Xvfb was NOT signalled (it is still alive after the run)" \
            "the process ${ORPHAN_PID} is gone — the pre-flight signalled it"
    fi

    # The double is the test's own process: it is cleaned up here so it
    # never outlives the suite. The `wait` reaps it explicitly, so the
    # shell does not print a job-status notification for the killed double.
    kill -9 "$ORPHAN_PID" 2>/dev/null || true
    wait "$ORPHAN_PID" 2>/dev/null || true
fi

# ===========================================================================
# T078 — every path argument reaches an external command after `--`
# ===========================================================================
#
# FR-012 item 7: "every path argument MUST be passed after `--`, because an
# environment-derived path may contain whitespace or glob metacharacters."
#
# There is no behavioural trigger for a path argument that begins with a
# dash, so the invariant is asserted on the script's own SOURCE. The check
# is CALL-SITE SPECIFIC: the table names the external command and the
# variable that holds the path, and the scan asserts that a `--` terminator
# precedes the path on every line that invokes the command with it. A
# rename of one of the variables therefore needs the table below updated —
# the scan reports a missing call site loudly rather than passing
# vacuously.
#
# The scan skips the comment lines and only counts a command that appears in
# a COMMAND position (the start of a line, or after whitespace, a command
# substitution, a pipe or an operator), so a command name that merely
# appears inside a log message is not mistaken for an invocation.

printf '# --- T078: every path argument reaches an external command after -- ---\n'

# | command | the variable that holds the path argument |
PREFLIGHT_PATH_ARG_TABLE=(
    "awk|normalized"
    "head|out_file"
    "sed|out_file"
    "dotnet|mod_project"
    "cat|marker_real"
    "rm|target"
    "cp|built"
    "tail|crash_file"
    "grep|dll"
    "stat|smapi_log"
    "mkdir|run_log_dir"
    "dirname|mod_project"
    "basename|mod_project"
)

for pr_row in "${PREFLIGHT_PATH_ARG_TABLE[@]}"; do
    IFS='|' read -r pr_cmd pr_var <<< "$pr_row"

    # The path argument, exactly as the script spells it: "$out_file".
    pr_path="\"\$${pr_var}\""
    # The glob used to cut the line in front of the path argument. The
    # closing quote is part of the pattern, so "$out_file" does not match
    # "$out_file.bak".
    #
    # The pattern is assembled INLINE, at the point of use, with the path
    # argument quoted SEPARATELY from the trailing "*": only the "*" is a
    # wildcard, the path argument itself is matched LITERALLY. An unquoted
    # expansion of a pattern variable inside `${..}` would let every glob
    # metacharacter that appears in the path argument act as a wildcard
    # (SC2295) — and quoting the whole pattern would make the "*" literal and
    # silently stop the cut.

    pr_found=0
    pr_offenders=""

    while IFS= read -r pr_line; do
        case "$pr_line" in
            \#*) continue ;;
        esac

        [[ "$pr_line" == *"$pr_path"* ]] || continue

        pr_re="(^|[[:space:]]|\$\(|[|;&({])${pr_cmd}([[:space:]]|$)"
        [[ "$pr_line" =~ $pr_re ]] || continue

        pr_found=1

        pr_prefix="${pr_line%%"${pr_path}"*}"
        if [[ ! "$pr_prefix" =~ (^|[[:space:]])--([[:space:]]|$) ]]; then
            pr_offenders="${pr_offenders}"$'\n'"    ${pr_line}"
        fi
    done < "$HARNESS_SCRIPT"

    if (( pr_found > 0 )); then
        pass "T078 [${pr_cmd} \"\$${pr_var}\"]: the call site is present"
    else
        fail "T078 [${pr_cmd} \"\$${pr_var}\"]: the call site is present" \
            "no invocation of '${pr_cmd}' with the path argument \"\$${pr_var}\" was found — the scan is call-site specific, so this table must be updated if the variable was renamed"
        pr_offenders="(the call site was not found)"
    fi

    if [[ -z "$pr_offenders" ]]; then
        pass "T078 [${pr_cmd} \"\$${pr_var}\"]: the path argument is passed after '--'"
    else
        fail "T078 [${pr_cmd} \"\$${pr_var}\"]: the path argument is passed after '--'" \
            "the path argument is not preceded by a '--' terminator on:${pr_offenders}"
    fi
done

# ---------------------------------------------------------------------------

finish_tests
