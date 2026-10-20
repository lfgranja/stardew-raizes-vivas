#!/usr/bin/env bash
#
# scripts/tests/test-integration.sh — T012: the integration suite for
# scripts/smoke-validation.sh.
#
# The suite automates the full-lifecycle test cases of
# specs/008-smoke-validation/quickstart.md:
#
#   TC-1   the mod loads -> exit 0 + "SUCCESS: Mod loaded without errors."
#          and the run log records the launch contract (FR-001, FR-014)
#   TC-2   a mod-scoped Error record -> exit 5 (FR-003, FR-006)
#   TC-3   a missing game installation -> exit 1 (FR-007, FR-012)
#   TC-4   a game already running -> exit 7; with --force the conflicting
#          process is terminated and the run proceeds (FR-011)
#   TC-5   a second concurrent instance -> exit 6 (FR-002)
#   TC-6   the launch-to-verdict deadline -> exit 4 (FR-009)
#   TC-7   Ctrl+C -> exit 130, cleanup ran, and the death was a GENUINE
#          signal death (WIFSIGNALED), not a plain `exit 130` (FR-005, FR-010)
#   TC-9   an ineffective deploy -> exit 12 naming both digests, before the
#          launch (FR-016)
#   TC-10  a stale log cannot produce a false pass -> exit 5 (FR-002)
#   TC-11  a mod skipped by SMAPI -> exit 5 with SMAPI's reason (FR-003)
#   TC-12  SMAPI exits without producing a log -> exit 15 (FR-002)
#   TC-14  the resolved mods directory is the one SMAPI uses (FR-017)
#   TC-15  the run log, the run id, the VERSION, the launch timestamp and the
#          stdout/stderr split (FR-014)
#   TC-16  --help completeness (FR-015)
#   TC-17  the FR-007 status-translation table, every row
#   TC-25  idempotency: two consecutive runs, same code, same verdict (FR-008)
#   TC-26  the build timeout -> exit 12 (FR-016)
#
# HOW THE CASES ARE DRIVEN
#
#   * launch-free      TC-3, TC-5, TC-16 — no game, no display, no build.
#   * fixture-driven   TC-2, TC-4, TC-6, TC-7, TC-9, TC-10, TC-11, TC-12,
#                      TC-17, TC-26 — a synthetic game directory whose
#                      `StardewModdingAPI` apphost is a controllable stub. The
#                      stub writes the synthetic SMAPI log the case needs and
#                      returns the exit status the case needs, so the real
#                      game is never launched and nothing outside the sandbox
#                      is touched.
#   * real             TC-1, TC-14, TC-15, TC-25 — the real installation, the
#                      real mods directory and the real SMAPI log.
#
# SAFETY
#
#   The real cases mutate the real installation in exactly the way the
#   feature is specified to: the script rebuilds the mod and DELETES AND
#   RECREATES `<game-path>/Mods/LivingRoots/`. Before any real case runs,
#   `guard_real_deploy_target` confirms that the resolved deploy target is
#   the real `<game>/Mods/LivingRoots` and nothing else; if it is not, the
#   real cases are refused rather than run against an unknown directory.
#
#   The script's own state (the run log and the lock) is confined to the
#   harness sandbox: HOME, XDG_STATE_HOME, XDG_RUNTIME_DIR and XDG_CONFIG_HOME
#   all point inside the per-test sandbox, EXCEPT for the real cases, which
#   restore the user's real HOME and XDG_CONFIG_HOME so that the game writes
#   its SMAPI log into the real `~/.config/StardewValley/ErrorLogs/` and so
#   that SMAPI's `~`-anonymisation is reproducible. Nothing is ever deleted
#   from that directory by this suite.
#
# SKIPPING
#
#   SMOKE_SKIP_INTEGRATION=1 (or SKIP_INTEGRATION=1) skips the whole file with
#   a clear summary and exit 0. A host without Xvfb/glxinfo/xdpyinfo/dotnet
#   degrades gracefully: the cases that need them are skipped, the rest run.
#
# Requires bash 4.4+.

set -Eeuo pipefail

# ---------------------------------------------------------------------------
# Cleanup registered BEFORE the harness is sourced.
#
# The harness installs its own EXIT trap and chains to whatever trap was
# already installed (it captures it with `trap -p EXIT` at load time), so a
# trap installed here still runs — after the harness has removed its sandbox.
# It is the safety net for the stub processes a badly behaved run may leave
# behind: a case that fails to clean up must not leak a sleeping stub into
# the rest of the suite.
# ---------------------------------------------------------------------------

INTEGRATION_STUB_MARKERS=()
INTEGRATION_STUB_PIDS=()

integration_cleanup() {
    local marker="" pid=""
    # The PIDs are remembered in a shell variable, not only in a marker file:
    # the harness removes its sandbox BEFORE this trap runs, so a marker file
    # inside the sandbox would already be gone by the time the cleanup looks
    # at it.
    for pid in "${INTEGRATION_STUB_PIDS[@]+"${INTEGRATION_STUB_PIDS[@]}"}"; do
        [[ -n "$pid" ]] || continue
        if kill -0 "$pid" 2>/dev/null; then
            kill -TERM "$pid" 2>/dev/null || true
            sleep 0.2
            kill -KILL "$pid" 2>/dev/null || true
        fi
    done
    for marker in "${INTEGRATION_STUB_MARKERS[@]+"${INTEGRATION_STUB_MARKERS[@]}"}"; do
        [[ -n "$marker" && -f "$marker" ]] || continue
        pid="$(cat -- "$marker" 2>/dev/null || true)"
        if [[ -n "$pid" ]] && kill -0 "$pid" 2>/dev/null; then
            kill -TERM "$pid" 2>/dev/null || true
            sleep 0.2
            kill -KILL "$pid" 2>/dev/null || true
        fi
        rm -f -- "$marker" 2>/dev/null || true
    done
    return 0
}

trap integration_cleanup EXIT

# shellcheck source=lib/harness.sh
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/harness.sh"

# ---------------------------------------------------------------------------
# Suite-level configuration
# ---------------------------------------------------------------------------

# The overall guard: no new case is started once the suite has run for this
# long. Every individual case is bounded as well, so the suite can never hang.
SUITE_BUDGET_SECONDS=2400

# The external bound of a single case. The launch-dependent cases drive the
# real game, which needs ~15 s unloaded and up to ~45 s under load; the
# fixture cases pay one `dotnet build` (~10 s) each.
CASE_LIMIT_SECONDS=300
FIXTURE_CASE_LIMIT_SECONDS=240

SUITE_START="$(date +%s)"

# The user's real home. The harness redirects HOME only for the child it
# spawns; the suite's own $HOME is still the real one.
REAL_HOME="${HOME}"
REAL_GAME_DIR="${REAL_HOME}/.steam/steam/steamapps/common/Stardew Valley"
REAL_MODS_DIR="${REAL_GAME_DIR}/Mods"
REAL_SMAPI_LOG="${REAL_HOME}/.config/StardewValley/ErrorLogs/SMAPI-latest.txt"

# The mod's own project (the default of --mod-project).
REAL_MOD_PROJECT="${HARNESS_REPO_ROOT}/LivingRoots/LivingRoots.csproj"

# ---------------------------------------------------------------------------
# Small helpers
# ---------------------------------------------------------------------------

# announce <id> <title> — a TAP comment that names the case being run.
announce() {
    printf '# --- %s: %s\n' "${1-}" "${2-}"
}

# suite_budget_left — true while the overall guard still allows new cases.
suite_budget_left() {
    local elapsed=$(( $(date +%s) - SUITE_START ))
    (( elapsed < SUITE_BUDGET_SECONDS ))
}

# elapsed_seconds — the wall time since the suite started.
elapsed_seconds() {
    printf '%d' "$(( $(date +%s) - SUITE_START ))"
}

# run_script_bounded <limit-seconds> [ENV=VALUE ...] -- [script args ...]
#
# The harness's run_script/run_script_env capture the script's output and its
# exit status, but they place no time bound on it. The launch-dependent cases
# drive the real game, so this wrapper adds one: the child is run under
# `timeout`, whose own 124 status is distinguishable from every code of the
# FR-007 taxonomy. The environment handling mirrors the harness's contract:
# HOME, XDG_STATE_HOME, XDG_RUNTIME_DIR and XDG_CONFIG_HOME point into the
# harness sandbox, and the caller's assignments are appended last so they
# override them.
#
# The result is published through the harness's own globals: RUN_STDOUT,
# RUN_STDERR, RUN_CODE and RUN_PERFORMED.
run_script_bounded() {
    local limit="${1-}"
    shift || true

    local -a env_assignments=()
    local -a script_args=()
    local seen_separator=0
    local arg=""
    for arg in "$@"; do
        if (( seen_separator == 1 )); then
            script_args+=("$arg")
        elif [[ "$arg" == "--" ]]; then
            seen_separator=1
        else
            env_assignments+=("$arg")
        fi
    done

    local out_file=""
    local err_file=""
    out_file="$(mktemp "${TMPDIR:-/tmp}/livingroots-smoke-stdout.XXXXXXXXXX" 2>/dev/null || true)"
    err_file="$(mktemp "${TMPDIR:-/tmp}/livingroots-smoke-stderr.XXXXXXXXXX" 2>/dev/null || true)"

    if [[ -z "$out_file" || -z "$err_file" ]]; then
        RUN_CODE=1
        RUN_STDOUT=""
        RUN_STDERR="test-integration.sh: could not create the capture files"
        RUN_PERFORMED=1
        return 0
    fi

    local -a env_args=(
        "HOME=${HARNESS_HOME}"
        "XDG_STATE_HOME=${HARNESS_XDG_STATE_HOME}"
        "XDG_RUNTIME_DIR=${HARNESS_XDG_RUNTIME_DIR}"
        "XDG_CONFIG_HOME=${HARNESS_XDG_CONFIG_HOME}"
    )
    if (( ${#env_assignments[@]} > 0 )); then
        env_args+=("${env_assignments[@]}")
    fi

    if env "${env_args[@]}" timeout -k 5 "$limit" "$HARNESS_SCRIPT" \
        "${script_args[@]+"${script_args[@]}"}" >"$out_file" 2>"$err_file"; then
        RUN_CODE=0
    else
        RUN_CODE=$?
    fi

    RUN_STDOUT="$(cat -- "$out_file" 2>/dev/null || true)"
    RUN_STDERR="$(cat -- "$err_file" 2>/dev/null || true)"
    rm -f -- "$out_file" "$err_file" 2>/dev/null || true
    RUN_PERFORMED=1
    return 0
}

# write_stub_apphost <path> — install the controllable stand-in for SMAPI's
# native apphost over the harness's inert stub.
#
# The stub honours four environment variables, which the script under test
# passes through to the game:
#
#   SMOKE_STUB_LOG       the file the stub writes its "SMAPI log" to
#   SMOKE_STUB_LOG_SRC   the file whose content is copied there
#   SMOKE_STUB_EXIT      the exit status the stub returns (default 0)
#   SMOKE_STUB_SLEEP     seconds to sleep before returning (default 0)
#   SMOKE_STUB_MARKER    a file the stub records its own PID in
#
# The marker is how a case proves that the game was (or was not) launched.
write_stub_apphost() {
    local path="${1-}"
    if [[ -z "$path" ]]; then
        printf 'write_stub_apphost: a path is required\n' >&2
        return 1
    fi
    cat > "$path" <<'STUB'
#!/usr/bin/env bash
# Controllable stand-in for SMAPI's native apphost.
#
# It launches no game, touches nothing outside the sandbox, and produces the
# SMAPI log and the exit status the test case needs.
set -u

if [[ -n "${SMOKE_STUB_MARKER:-}" ]]; then
    printf '%s\n' "$$" > "${SMOKE_STUB_MARKER}" 2>/dev/null || true
fi

if [[ -n "${SMOKE_STUB_LOG:-}" && -n "${SMOKE_STUB_LOG_SRC:-}" ]]; then
    if [[ -f "${SMOKE_STUB_LOG_SRC}" ]]; then
        cp -- "${SMOKE_STUB_LOG_SRC}" "${SMOKE_STUB_LOG}" 2>/dev/null || true
    fi
fi

if [[ -n "${SMOKE_STUB_SLEEP:-}" && "${SMOKE_STUB_SLEEP}" != "0" ]]; then
    sleep "${SMOKE_STUB_SLEEP}"
fi

exit "${SMOKE_STUB_EXIT:-0}"
STUB
    chmod 0755 -- "$path" 2>/dev/null || true
    return 0
}

# fixture_game <name> — build a synthetic game directory whose apphost is the
# controllable stub. Prints the directory.
fixture_game() {
    local name="${1-}"
    if [[ -z "$name" ]]; then
        printf 'fixture_game: a name is required\n' >&2
        return 1
    fi
    local dir="${FIXTURE_ROOT}/${name}"
    make_fake_game_dir "$dir" || return 1
    write_stub_apphost "${dir}/StardewModdingAPI" || return 1
    printf '%s' "$dir"
}

# refresh_sentinel <log-file> — rewrite the "Log started at ... UTC" sentinel
# of a synthetic log to the current UTC time.
#
# The harness's fixtures carry a fixed sentinel (2026-10-20T00:00:00 UTC).
# The script under test MUST ignore records that precede the sentinel of the
# current run, so a fixture whose sentinel is in the past could be rejected as
# stale and the case would measure the wrong behaviour. The sentinel's
# *format* is left untouched — only the timestamp is refreshed.
refresh_sentinel() {
    local path="${1-}"
    local now=""
    now="$(date -u '+%Y-%m-%dT%H:%M:%S')"
    sed -i "s/Log started at .* UTC/Log started at ${now} UTC/" "$path" 2>/dev/null || true
    return 0
}

# stub_log <path> <kind> — a synthetic SMAPI log of the given harness kind,
# with a current run sentinel.
stub_log() {
    local path="${1-}"
    local kind="${2-}"
    make_log_fixture "$path" "$kind" || return 1
    refresh_sentinel "$path"
    return 0
}

# latest_run_log — the path of the most recent run log the script wrote in
# the sandbox state directory, or the empty string.
latest_run_log() {
    local dir="${HARNESS_XDG_STATE_HOME}/living-roots-smoke/runs"
    local newest=""
    local candidate=""
    [[ -d "$dir" ]] || return 0
    for candidate in "${dir}"/*.log; do
        [[ -f "$candidate" ]] || continue
        if [[ -z "$newest" ]] || [[ "$candidate" -nt "$newest" ]]; then
            newest="$candidate"
        fi
    done
    printf '%s' "$newest"
}

# run_log_lines_stamped <run-log> <run-id> — the number of NON-EMPTY log
# lines that do NOT carry the run id. Zero means every log line is stamped.
run_log_lines_unstamped() {
    local run_log="${1-}"
    local run_id="${2-}"
    local line=""
    local count=0
    [[ -f "$run_log" ]] || { printf '0'; return 0; }
    while IFS= read -r line || [[ -n "$line" ]]; do
        [[ -n "${line//[[:space:]]/}" ]] || continue
        if [[ "$line" != *"$run_id"* ]]; then
            count=$(( count + 1 ))
        fi
    done < "$run_log"
    printf '%d' "$count"
}

# remember_pid <pid> — remember a fixture process so the EXIT trap can
# terminate it if a case is interrupted before it cleans up itself.
remember_pid() {
    local pid="${1-}"
    [[ -n "$pid" ]] || return 0
    INTEGRATION_STUB_PIDS+=("$pid")
    return 0
}

# register_stub <marker-file> — remember a stub marker so the EXIT trap can
# terminate a stub the script under test left behind.
register_stub() {
    local marker="${1-}"
    [[ -n "$marker" ]] || return 0
    INTEGRATION_STUB_MARKERS+=("$marker")
    return 0
}

# kill_stub <marker-file> — terminate a stub the script under test left
# behind, so a case cannot leak a process into the next one.
kill_stub() {
    local marker="${1-}"
    local pid=""
    [[ -n "$marker" && -f "$marker" ]] || return 0
    pid="$(cat -- "$marker" 2>/dev/null || true)"
    if [[ -n "$pid" ]]; then
        remember_pid "$pid"
        if kill -0 "$pid" 2>/dev/null; then
            kill -TERM "$pid" 2>/dev/null || true
            sleep 0.3
            kill -KILL "$pid" 2>/dev/null || true
        fi
    fi
    rm -f -- "$marker" 2>/dev/null || true
    return 0
}

# real_game_available — true when the real installation is present.
real_game_available() {
    [[ -d "$REAL_GAME_DIR" ]] || return 1
    [[ -f "${REAL_GAME_DIR}/Stardew Valley.dll" ]] || return 1
    [[ -x "${REAL_GAME_DIR}/StardewModdingAPI" ]] || return 1
    return 0
}

# guard_real_deploy_target — the safety gate of every real case.
#
# The script DELETES AND RECREATES `<mods-path>/LivingRoots/` on every run.
# This confirms, before anything is run, that the directory the script would
# touch is the real `<game>/Mods/LivingRoots` — resolved through the realpath
# canonicalisation the script itself performs — and not anything else.
guard_real_deploy_target() {
    local canonical_game=""
    local canonical_mods=""
    local target=""

    canonical_game="$(realpath -e -- "$REAL_GAME_DIR" 2>/dev/null || true)"
    canonical_mods="$(realpath -e -- "$REAL_MODS_DIR" 2>/dev/null || true)"
    [[ -n "$canonical_game" && -n "$canonical_mods" ]] || return 1

    target="${canonical_mods}/LivingRoots"
    case "$target" in
        "$canonical_game"/Mods/LivingRoots) return 0 ;;
        *) return 1 ;;
    esac
}

# write_signal_probe <path> — the perl helper that drives a signal case and
# reports the RAW wait status, so a genuine signal death (WIFSIGNALED) can be
# distinguished from a plain `exit 128+N`.
#
# Usage: signal-probe.pl <stdout-file> <marker-file> <SIGNAME> <wait-secs> <cmd...>
# Output: EARLY=<0|1> SIGNALED=<0|1> SIG=<n> EXIT=<n> SHELL=<status>
write_signal_probe() {
    local path="${1-}"
    cat > "$path" <<'PERL'
#!/usr/bin/perl
# Drive a command, signal it once it has reached its marker file, and report
# the raw wait status.
#
# The shell's own `wait` cannot distinguish "died from SIGINT" (WIFSIGNALED)
# from "exited with status 130" — both surface as 130. The raw status does
# distinguish them, and this is the only way to observe it from a test.
use strict;
use warnings;

my ($outfile, $marker, $signame, $wait_secs, @cmd) = @ARGV;

if (@cmd < 1) {
    print "NO_COMMAND\n";
    exit 2;
}

my %signals = (HUP => 1, INT => 2, TERM => 15);
my $sig = $signals{$signame};
if (!defined $sig) {
    print "BAD_SIGNAL\n";
    exit 2;
}

my $pid = fork();
defined $pid or die "fork failed: $!\n";

if ($pid == 0) {
    open(STDOUT, '>', $outfile) or die "cannot open $outfile: $!\n";
    open(STDERR, '>&', \*STDOUT) or die "cannot dup stderr: $!\n";
    open(STDIN, '<', '/dev/null') or die "cannot open /dev/null: $!\n";
    exec { $cmd[0] } @cmd;
    print "EXEC_FAILED\n";
    exit 127;
}

my $exited_early = 0;
my $status = 0;
my $deadline = time + $wait_secs;

while (1) {
    my $reaped = waitpid($pid, 1);
    if ($reaped == $pid) {
        $exited_early = 1;
        $status = $?;
        last;
    }
    last if -e $marker;
    last if time > $deadline;
    select(undef, undef, undef, 0.05);
}

if (!$exited_early) {
    kill($sig, $pid);
    waitpid($pid, 0);
    $status = $?;
}

my $termsig = $status & 127;
my $exitcode = $status >> 8;
my $signaled = $termsig ? 1 : 0;
my $shell = $signaled ? (128 + $termsig) : $exitcode;

print "EARLY=$exited_early SIGNALED=$signaled SIG=$termsig EXIT=$exitcode SHELL=$shell\n";
PERL
    chmod 0755 -- "$path" 2>/dev/null || true
    return 0
}

# probe_field <probe-output> <name> — the value of one field of the signal
# probe's output line.
probe_field() {
    local output="${1-}"
    local name="${2-}"
    local line=""
    local rest=""
    local value=""
    line="$(printf '%s\n' "$output" | grep -E '^(EARLY|SIGNALED|SIG|EXIT|SHELL)=' | head -n 1 || true)"
    [[ -n "$line" ]] || { printf ''; return 0; }
    rest="${line#*${name}=}"
    printf '%s' "${rest%% *}"
}

# ---------------------------------------------------------------------------
# Skip switch
# ---------------------------------------------------------------------------

if integration_skipped; then
    skip "the integration suite (SMOKE_SKIP_INTEGRATION=1)" "the real game installation"
    assert_summary
    exit 0
fi

# ---------------------------------------------------------------------------
# Host tool gate
#
# The script's pre-flight sweeps the host tools FIRST, so on a host without
# Xvfb every case but --help would report the missing tool instead of its own
# code. The suite therefore degrades gracefully as a whole.
# ---------------------------------------------------------------------------

HAVE_HOST_TOOLS=1
for required_tool in Xvfb glxinfo xdpyinfo dotnet; do
    if require_tool "$required_tool" "$required_tool is available"; then
        pass "$required_tool is available on PATH"
    else
        HAVE_HOST_TOOLS=0
    fi
done

# perl drives the signal cases: it is the only way to observe the RAW wait
# status, and therefore to tell a genuine signal death (WIFSIGNALED) from a
# plain `exit 128+N`.
HAVE_PERL=1
if require_tool perl "perl is available"; then
    pass "perl is available on PATH"
else
    HAVE_PERL=0
fi

# ---------------------------------------------------------------------------
# TC-1 — the mod loads successfully (real installation)
# ---------------------------------------------------------------------------

TC1_RUN_LOG=""
TC1_RUN_ID=""
TC1_STDOUT=""
TC1_STDERR=""

announce "TC-1" "the mod loads -> exit 0 and the run log records the launch contract"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-1 (mod loads successfully)" "the host tool sweep"
elif ! real_game_available; then
    skip "TC-1 (mod loads successfully)" "the real game installation"
elif ! guard_real_deploy_target; then
    fail "TC-1: the deploy target resolves to the real Mods/LivingRoots" \
        "the resolved deploy target is not '${REAL_GAME_DIR}/Mods/LivingRoots' — refusing to run against the real installation"
elif ! suite_budget_left; then
    skip "TC-1 (mod loads successfully)" "the suite time budget"
else
    pass "TC-1: the deploy target resolves to the real Mods/LivingRoots"

    run_script_bounded "$CASE_LIMIT_SECONDS" \
        "HOME=${REAL_HOME}" \
        "XDG_CONFIG_HOME=${REAL_HOME}/.config" \
        -- \
        --game-path "$REAL_GAME_DIR" \
        --mods-path "$REAL_MODS_DIR" \
        --smapi-log "$REAL_SMAPI_LOG" \
        --timeout 120

    assert_exit 0 "TC-1: the run exits 0"
    assert_contains_line "$RUN_STDOUT" "SUCCESS: Mod loaded without errors." \
        "TC-1: stdout carries exactly the success verdict line"
    assert_not_contains "$RUN_STDOUT" "ERROR:" \
        "TC-1: the diagnostics are not on stdout"

    # TC-15 inspects the same run; its streams are captured here, because the
    # later cases overwrite the harness's globals.
    TC1_STDOUT="$RUN_STDOUT"
    TC1_STDERR="$RUN_STDERR"

    TC1_RUN_LOG="$(latest_run_log)"
    if [[ -z "$TC1_RUN_LOG" ]]; then
        fail "TC-1: a run log was written" \
            "no run log under '${HARNESS_XDG_STATE_HOME}/living-roots-smoke/runs/'"
    else
        pass "TC-1: a run log was written"

        TC1_RUN_ID="$(basename -- "$TC1_RUN_LOG")"
        TC1_RUN_ID="${TC1_RUN_ID%.log}"

        tc1_log_text="$TC1_RUN_LOG"
        tc1_log_lower="$(tr '[:upper:]' '[:lower:]' < "$tc1_log_text" 2>/dev/null || true)"

        # --- the launch contract (FR-001) ---
        assert_contains "$tc1_log_text" "./StardewModdingAPI --no-terminal" \
            "TC-1: the run log records the apphost invocation"
        assert_contains "$tc1_log_text" "$(realpath -e -- "$REAL_GAME_DIR" 2>/dev/null || printf '%s' "$REAL_GAME_DIR")" \
            "TC-1: the run log records the game directory"
        assert_matches "$tc1_log_lower" '(working directory|cwd|work ?dir)' \
            "TC-1: the run log names the working directory of the launch"

        assert_contains "$tc1_log_text" "SMAPI_NO_TERMINAL=true" \
            "TC-1: the run log records SMAPI_NO_TERMINAL=true"
        assert_contains "$tc1_log_text" "SMAPI_DEVELOPER_MODE=true" \
            "TC-1: the run log records SMAPI_DEVELOPER_MODE=true"
        assert_contains "$tc1_log_text" "LIBGL_ALWAYS_SOFTWARE=1" \
            "TC-1: the run log records LIBGL_ALWAYS_SOFTWARE=1"
        assert_contains "$tc1_log_text" "GALLIUM_DRIVER=llvmpipe" \
            "TC-1: the run log records GALLIUM_DRIVER=llvmpipe"
        assert_contains "$tc1_log_text" "SDL_VIDEODRIVER=x11" \
            "TC-1: the run log records SDL_VIDEODRIVER=x11"
        assert_contains "$tc1_log_text" "SDL_AUDIODRIVER=dummy" \
            "TC-1: the run log records SDL_AUDIODRIVER=dummy"
        assert_contains "$tc1_log_text" "ALSOFT_DRIVERS=null" \
            "TC-1: the run log records ALSOFT_DRIVERS=null"

        assert_contains "$tc1_log_text" "-nolisten tcp" \
            "TC-1: the run log records the Xvfb -nolisten tcp flag"
        assert_contains "$tc1_log_text" "-displayfd" \
            "TC-1: the run log records the Xvfb -displayfd flag"
        assert_matches "$tc1_log_text" 'DISPLAY[=:[:space:]]+:[0-9]+' \
            "TC-1: the run log records DISPLAY set from the reported display number"

        # --- the phase timings (FR-014) ---
        assert_matches "$tc1_log_text" '[0-9]+(\.[0-9]+)?[[:space:]]*(s|sec|second)' \
            "TC-1: the run log records a phase duration"
        assert_matches "$tc1_log_lower" '(total|wall)' \
            "TC-1: the run log records the total wall time"
        assert_matches "$tc1_log_lower" 'pre-?flight' \
            "TC-1: the run log records the pre-flight phase"
        assert_matches "$tc1_log_lower" '(rebuild|build)' \
            "TC-1: the run log records the rebuild-and-verify phase"
        assert_matches "$tc1_log_lower" 'launch' \
            "TC-1: the run log records the launch phase"
        assert_matches "$tc1_log_lower" '(readiness|wait)' \
            "TC-1: the run log records the readiness wait"
        assert_matches "$tc1_log_lower" '(verdict)' \
            "TC-1: the run log records the verdict"
        assert_matches "$tc1_log_lower" '(cleanup|termination)' \
            "TC-1: the run log records the cleanup phase"
    fi
fi

# ---------------------------------------------------------------------------
# TC-2 — a mod-scoped Error record fails the run (fixture)
# ---------------------------------------------------------------------------

announce "TC-2" "a mod-scoped Error record -> exit 5"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-2 (mod-scoped error)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-2 (mod-scoped error)" "the suite time budget"
else
    tc2_game="$(fixture_game tc2 || true)"
    tc2_log="${FIXTURE_ROOT}/tc2/SMAPI-latest.txt"
    tc2_marker="${FIXTURE_ROOT}/tc2/launched.marker"
    register_stub "$tc2_marker"

    stub_log "$tc2_log" continuation_lines

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_LOG=${tc2_log}" \
        "SMOKE_STUB_LOG_SRC=${tc2_log}" \
        "SMOKE_STUB_MARKER=${tc2_marker}" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc2_game" \
        --smapi-log "$tc2_log" \
        --timeout 60

    kill_stub "$tc2_marker"

    assert_exit 5 "TC-2: a mod-scoped Error record exits 5"
    assert_contains "$RUN_STDERR" "NullReferenceException" \
        "TC-2: the report names the matched failure record"
    assert_contains "$RUN_STDERR" "at StardewValley.Game1.DayStarted()" \
        "TC-2: the report carries the continuation lines of the failure record"
    assert_contains "$RUN_STDERR" "$tc2_log" \
        "TC-2: the report names the SMAPI log"
    assert_contains "$RUN_STDERR" "Fix:" \
        "TC-2: the report carries an actionable Fix: line"
fi

# ---------------------------------------------------------------------------
# TC-3 — the game is not installed (launch-free)
# ---------------------------------------------------------------------------

announce "TC-3" "a missing game installation -> exit 1"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-3 (game not installed)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-3 (game not installed)" "the suite time budget"
else
    tc3_missing="${FIXTURE_ROOT}/no-such-game"
    tc3_log="${FIXTURE_ROOT}/tc3/SMAPI-latest.txt"
    mkdir -p -- "$(dirname -- "$tc3_log")" 2>/dev/null || true

    run_script --game-path "$tc3_missing" --smapi-log "$tc3_log"

    assert_exit 1 "TC-3: a missing game installation exits 1"
    assert_contains "$RUN_STDERR" "$tc3_missing" \
        "TC-3: the report names the missing game path"
    assert_contains "$RUN_STDERR" "Fix:" \
        "TC-3: the report carries an actionable Fix: line"
    assert_not_contains "$RUN_STDOUT" "ERROR:" \
        "TC-3: the diagnostics are on stderr, not on stdout"
fi

# ---------------------------------------------------------------------------
# TC-4 — a game is already running (fixture)
# ---------------------------------------------------------------------------

announce "TC-4" "an already-running game -> exit 7; --force terminates it"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-4 (game already running)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-4 (game already running)" "the suite time budget"
else
    tc4_game="$(fixture_game tc4 || true)"
    tc4_log="${FIXTURE_ROOT}/tc4/SMAPI-latest.txt"
    tc4_marker="${FIXTURE_ROOT}/tc4/launched.marker"
    register_stub "$tc4_marker"
    tc4_conflict_marker="${FIXTURE_ROOT}/tc4/conflict.marker"
    tc4_force_marker="${FIXTURE_ROOT}/tc4/force-conflict.marker"

    # A process whose /proc/<pid>/exe really is the fixture's "StardewValley"
    # binary — the identification rule of FR-011 (by executable path, never by
    # process name).
    cp -- "/usr/bin/sleep" "${tc4_game}/StardewValley" 2>/dev/null || true
    chmod 0755 -- "${tc4_game}/StardewValley" 2>/dev/null || true

    "${tc4_game}/StardewValley" 300 >/dev/null 2>&1 &
    tc4_conflict_pid=$!
    register_stub "$tc4_conflict_marker"
    remember_pid "$tc4_conflict_pid"
    printf '%s\n' "$tc4_conflict_pid" > "$tc4_conflict_marker"
    sleep 0.5

    if ! kill -0 "$tc4_conflict_pid" 2>/dev/null; then
        fail "TC-4: a conflicting game process is running" \
            "the fixture process ${tc4_conflict_pid} did not start"
    else
        pass "TC-4: a conflicting game process is running"

        run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
            "SMOKE_STUB_LOG=${tc4_log}" \
            "SMOKE_STUB_LOG_SRC=${tc4_log}" \
            "SMOKE_STUB_MARKER=${tc4_marker}" \
            "SMOKE_STUB_EXIT=0" \
            -- \
            --game-path "$tc4_game" \
            --smapi-log "$tc4_log" \
            --timeout 60

        kill_stub "$tc4_marker"

        assert_exit 7 "TC-4: an already-running game exits 7"
        assert_contains "$RUN_STDERR" "$tc4_conflict_pid" \
            "TC-4: the report lists the conflicting PID"
        assert_contains "$RUN_STDERR" "Fix:" \
            "TC-4: the report carries an actionable Fix: line"

        if kill -0 "$tc4_conflict_pid" 2>/dev/null; then
            fail "TC-4: the conflicting game was not clobbered" \
                "the fixture process ${tc4_conflict_pid} is still alive"
        else
            pass "TC-4: the conflicting game was not clobbered"
        fi

        # --- the same situation, with --force ---
        "${tc4_game}/StardewValley" 300 >/dev/null 2>&1 &
        tc4_force_pid=$!
        register_stub "$tc4_force_marker"
        remember_pid "$tc4_force_pid"
        printf '%s\n' "$tc4_force_pid" > "$tc4_force_marker"
        sleep 0.5

        rm -f -- "$tc4_marker"
        stub_log "$tc4_log" mod_loaded

        run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
            "SMOKE_STUB_LOG=${tc4_log}" \
            "SMOKE_STUB_LOG_SRC=${tc4_log}" \
            "SMOKE_STUB_MARKER=${tc4_marker}" \
            "SMOKE_STUB_EXIT=0" \
            -- \
            --game-path "$tc4_game" \
            --smapi-log "$tc4_log" \
            --timeout 60 \
            --force

        kill_stub "$tc4_marker"

        assert_ne 7 "$RUN_CODE" "TC-4: --force does not abort with 7"
        assert_file_exists "$tc4_marker" \
            "TC-4: --force proceeds to the launch"
        if kill -0 "$tc4_force_pid" 2>/dev/null; then
            fail "TC-4: --force terminated the conflicting process" \
                "the fixture process ${tc4_force_pid} is still alive"
        else
            pass "TC-4: --force terminated the conflicting process"
        fi
    fi

    kill_stub "$tc4_conflict_marker"
    kill_stub "$tc4_force_marker"
fi

# ---------------------------------------------------------------------------
# TC-5 — a second concurrent instance (launch-free)
# ---------------------------------------------------------------------------

announce "TC-5" "a second concurrent instance -> exit 6"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-5 (lock contention)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-5 (lock contention)" "the suite time budget"
else
    tc5_game="$(fixture_game tc5 || true)"
    tc5_log="${FIXTURE_ROOT}/tc5/SMAPI-latest.txt"

    # Hold the lock on BOTH candidate paths of FR-002, so the case holds
    # whichever one the script uses:
    #   ${XDG_RUNTIME_DIR}/livingroots-smoke.lock
    #   ${XDG_STATE_HOME}/living-roots-smoke/livingroots-smoke.lock
    mkdir -p -- "$HARNESS_XDG_RUNTIME_DIR" 2>/dev/null || true
    mkdir -p -- "${HARNESS_XDG_STATE_HOME}/living-roots-smoke" 2>/dev/null || true
    tc5_runtime_lock="${HARNESS_XDG_RUNTIME_DIR}/livingroots-smoke.lock"
    tc5_state_lock="${HARNESS_XDG_STATE_HOME}/living-roots-smoke/livingroots-smoke.lock"

    flock -x "$tc5_runtime_lock" -c 'sleep 300' >/dev/null 2>&1 &
    tc5_lock_a=$!
    flock -x "$tc5_state_lock" -c 'sleep 300' >/dev/null 2>&1 &
    tc5_lock_b=$!

    # Wait until both locks are actually held.
    tc5_lock_ready=0
    for _tc5_try in 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15 16 17 18 19 20; do
        if ! flock -n "$tc5_runtime_lock" -c true >/dev/null 2>&1 \
            && ! flock -n "$tc5_state_lock" -c true >/dev/null 2>&1; then
            tc5_lock_ready=1
            break
        fi
        sleep 0.2
    done

    if (( tc5_lock_ready == 0 )); then
        fail "TC-5: the smoke-test lock is held by another instance" \
            "could not acquire the lock for the competing holder"
    else
        pass "TC-5: the smoke-test lock is held by another instance"

        run_script --game-path "$tc5_game" --smapi-log "$tc5_log"

        assert_exit 6 "TC-5: a second concurrent instance exits 6"
        assert_contains "$RUN_STDERR" "lock" \
            "TC-5: the report names the lock"
        assert_contains "$RUN_STDERR" "Fix:" \
            "TC-5: the report carries an actionable Fix: line"
    fi

    kill -TERM "$tc5_lock_a" 2>/dev/null || true
    kill -TERM "$tc5_lock_b" 2>/dev/null || true
    wait "$tc5_lock_a" 2>/dev/null || true
    wait "$tc5_lock_b" 2>/dev/null || true
fi

# ---------------------------------------------------------------------------
# TC-6 — the launch-to-verdict deadline (fixture)
# ---------------------------------------------------------------------------

announce "TC-6" "no verdict within the deadline -> exit 4"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-6 (timeout)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-6 (timeout)" "the suite time budget"
else
    tc6_game="$(fixture_game tc6 || true)"
    tc6_log="${FIXTURE_ROOT}/tc6/SMAPI-latest.txt"
    tc6_marker="${FIXTURE_ROOT}/tc6/launched.marker"
    register_stub "$tc6_marker"

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_MARKER=${tc6_marker}" \
        "SMOKE_STUB_SLEEP=300" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc6_game" \
        --smapi-log "$tc6_log" \
        --timeout 5

    kill_stub "$tc6_marker"

    assert_exit 4 "TC-6: the launch-to-verdict deadline exits 4"
    assert_contains "$RUN_STDERR" "$tc6_log" \
        "TC-6: the timeout report names the SMAPI log"
    assert_contains "$RUN_STDERR" "Fix:" \
        "TC-6: the timeout report carries an actionable Fix: line"
    assert_matches "$RUN_STDERR" '(timeout|timed out|--timeout)' \
        "TC-6: the timeout report names the timeout"
fi

# ---------------------------------------------------------------------------
# TC-7 — Ctrl+C: exit 130 from a GENUINE signal death (fixture)
# ---------------------------------------------------------------------------

announce "TC-7" "SIGINT -> exit 130, cleanup ran, genuine signal death"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-7 (interruption)" "the host tool sweep"
elif (( HAVE_PERL == 0 )); then
    skip "TC-7 (interruption)" "perl"
elif ! suite_budget_left; then
    skip "TC-7 (interruption)" "the suite time budget"
else
    tc7_game="$(fixture_game tc7 || true)"
    tc7_log="${FIXTURE_ROOT}/tc7/SMAPI-latest.txt"
    tc7_marker="${FIXTURE_ROOT}/tc7/launched.marker"
    tc7_probe="${FIXTURE_ROOT}/tc7/signal-probe.pl"
    tc7_out="${FIXTURE_ROOT}/tc7/probe-output.txt"

    write_signal_probe "$tc7_probe"

    register_stub "$tc7_marker"

    tc7_probe_output="$(perl "$tc7_probe" "$tc7_out" "$tc7_marker" INT 120 \
        env "HOME=${HARNESS_HOME}" \
            "XDG_STATE_HOME=${HARNESS_XDG_STATE_HOME}" \
            "XDG_RUNTIME_DIR=${HARNESS_XDG_RUNTIME_DIR}" \
            "XDG_CONFIG_HOME=${HARNESS_XDG_CONFIG_HOME}" \
            "SMOKE_STUB_MARKER=${tc7_marker}" \
            "SMOKE_STUB_SLEEP=300" \
            "SMOKE_STUB_EXIT=0" \
        "$HARNESS_SCRIPT" \
            --game-path "$tc7_game" \
            --smapi-log "$tc7_log" \
            --timeout 120 2>&1 || true)"

    assert_contains "$tc7_probe_output" "SIGNALED=" \
        "TC-7: the signal probe reported a wait status"

    assert_file_exists "$tc7_marker" \
        "TC-7: the game was launched before the interruption"
    assert_eq "0" "$(probe_field "$tc7_probe_output" EARLY)" \
        "TC-7: the script was still running when the signal was sent"
    assert_eq "1" "$(probe_field "$tc7_probe_output" SIGNALED)" \
        "TC-7: the script died from the signal (WIFSIGNALED), not from a plain exit"
    assert_eq "2" "$(probe_field "$tc7_probe_output" SIG)" \
        "TC-7: the script died from SIGINT"
    assert_eq "130" "$(probe_field "$tc7_probe_output" SHELL)" \
        "TC-7: the caller observes exit status 130"

    tc7_stub_pid="$(cat -- "$tc7_marker" 2>/dev/null || true)"
    if [[ -z "$tc7_stub_pid" ]]; then
        fail "TC-7: the cleanup killed the game process" \
            "the game was never launched, so there was nothing to clean up"
    elif kill -0 "$tc7_stub_pid" 2>/dev/null; then
        fail "TC-7: the cleanup killed the game process" \
            "the stub process ${tc7_stub_pid} is still alive"
    else
        pass "TC-7: the cleanup killed the game process"
    fi

    kill_stub "$tc7_marker"
fi

# ---------------------------------------------------------------------------
# TC-9 — an ineffective deploy (fixture)
# ---------------------------------------------------------------------------

announce "TC-9" "the deployed assembly is not the one this run built -> exit 12"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-9 (deploy ineffective)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-9 (deploy ineffective)" "the suite time budget"
else
    tc9_game="$(fixture_game tc9 || true)"
    tc9_log="${FIXTURE_ROOT}/tc9/SMAPI-latest.txt"
    tc9_marker="${FIXTURE_ROOT}/tc9/launched.marker"
    register_stub "$tc9_marker"
    tc9_target="${tc9_game}/Mods/LivingRoots"

    # Seed a stale deployment and make the deploy target unwritable, so the
    # deployed assembly cannot become the one this run built.
    mkdir -p -- "$tc9_target" 2>/dev/null || true
    printf 'stale deployment payload\n' > "${tc9_target}/LivingRoots.dll" 2>/dev/null || true
    chmod 0555 -- "$tc9_target" 2>/dev/null || true

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_LOG=${tc9_log}" \
        "SMOKE_STUB_LOG_SRC=${tc9_log}" \
        "SMOKE_STUB_MARKER=${tc9_marker}" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc9_game" \
        --smapi-log "$tc9_log" \
        --timeout 60

    chmod 0755 -- "$tc9_target" 2>/dev/null || true
    kill_stub "$tc9_marker"

    assert_exit 12 "TC-9: an ineffective deploy exits 12"
    assert_file_absent "$tc9_marker" \
        "TC-9: the game was never launched"
    assert_matches "$RUN_STDERR" '[0-9a-f]{64}' \
        "TC-9: the report names a SHA-256 digest"
    tc9_digest_count="$(printf '%s\n' "$RUN_STDERR" | grep -oE '[0-9a-f]{64}' | sort -u | wc -l | tr -d ' ' || true)"
    assert_eq "2" "$tc9_digest_count" \
        "TC-9: the report names BOTH SHA-256 digests (built and deployed)"
fi

# ---------------------------------------------------------------------------
# TC-10 — a stale log cannot produce a false pass (fixture)
# ---------------------------------------------------------------------------

announce "TC-10" "a stale success marker cannot make the run pass -> exit 5"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-10 (stale log)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-10 (stale log)" "the suite time budget"
else
    tc10_game="$(fixture_game tc10 || true)"
    tc10_log="${FIXTURE_ROOT}/tc10/SMAPI-latest.txt"
    tc10_marker="${FIXTURE_ROOT}/tc10/launched.marker"
    register_stub "$tc10_marker"

    # The fixture carries the mod's success marker BEFORE the run sentinel.
    stub_log "$tc10_log" stale_marker

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_LOG=${tc10_log}" \
        "SMOKE_STUB_LOG_SRC=${tc10_log}" \
        "SMOKE_STUB_MARKER=${tc10_marker}" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc10_game" \
        --smapi-log "$tc10_log" \
        --timeout 60

    kill_stub "$tc10_marker"

    assert_exit 5 "TC-10: a stale success marker exits 5, not 0"
    assert_not_contains "$RUN_STDOUT" "SUCCESS: Mod loaded without errors." \
        "TC-10: the stale marker did not produce a success verdict"
fi

# ---------------------------------------------------------------------------
# TC-11 — the mod is skipped by SMAPI (fixture)
# ---------------------------------------------------------------------------

announce "TC-11" "a mod skipped by SMAPI -> exit 5 with SMAPI's reason"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-11 (mod skipped by SMAPI)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-11 (mod skipped by SMAPI)" "the suite time budget"
else
    tc11_game="$(fixture_game tc11 || true)"
    tc11_log="${FIXTURE_ROOT}/tc11/SMAPI-latest.txt"
    tc11_marker="${FIXTURE_ROOT}/tc11/launched.marker"
    register_stub "$tc11_marker"

    stub_log "$tc11_log" mod_skipped

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_LOG=${tc11_log}" \
        "SMOKE_STUB_LOG_SRC=${tc11_log}" \
        "SMOKE_STUB_MARKER=${tc11_marker}" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc11_game" \
        --smapi-log "$tc11_log" \
        --timeout 60

    kill_stub "$tc11_marker"

    assert_exit 5 "TC-11: a mod skipped by SMAPI exits 5"
    assert_contains "$RUN_STDERR" "needs SMAPI 4.6.0 or later" \
        "TC-11: the report states SMAPI's skip reason"
    assert_contains "$RUN_STDERR" "Fix:" \
        "TC-11: the report carries an actionable Fix: line"
fi

# ---------------------------------------------------------------------------
# TC-12 — SMAPI exits without producing a log (fixture)
# ---------------------------------------------------------------------------

announce "TC-12" "SMAPI exits without producing a log -> exit 15"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-12 (no log produced)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-12 (no log produced)" "the suite time budget"
else
    tc12_game="$(fixture_game tc12 || true)"
    tc12_log="${FIXTURE_ROOT}/tc12/SMAPI-latest.txt"
    tc12_marker="${FIXTURE_ROOT}/tc12/launched.marker"
    register_stub "$tc12_marker"

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_MARKER=${tc12_marker}" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc12_game" \
        --smapi-log "$tc12_log" \
        --timeout 60

    kill_stub "$tc12_marker"

    assert_exit 15 "TC-12: a run that produced no log exits 15"
    assert_contains "$RUN_STDERR" "$tc12_log" \
        "TC-12: the report names the SMAPI log"
    assert_contains "$RUN_STDERR" "Fix:" \
        "TC-12: the report carries an actionable Fix: line"
    assert_not_contains "$RUN_STDOUT" "SUCCESS: Mod loaded without errors." \
        "TC-12: a log-less run is never reported as a success"
fi

# ---------------------------------------------------------------------------
# TC-14 — the resolved mods directory is the one SMAPI uses (real installation)
# ---------------------------------------------------------------------------

announce "TC-14" "the resolved mods directory is the one SMAPI uses"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-14 (mods directory override)" "the host tool sweep"
elif ! real_game_available; then
    skip "TC-14 (mods directory override)" "the real game installation"
elif ! guard_real_deploy_target; then
    fail "TC-14: the deploy target resolves to the real Mods/LivingRoots" \
        "the resolved deploy target is not '${REAL_GAME_DIR}/Mods/LivingRoots' — refusing to run against the real installation"
elif ! suite_budget_left; then
    skip "TC-14 (mods directory override)" "the suite time budget"
else
    pass "TC-14: the deploy target resolves to the real Mods/LivingRoots"

    tc14_mods="${FIXTURE_ROOT}/tc14-mods"
    mkdir -p -- "$tc14_mods" 2>/dev/null || true
    tc14_expected="$(realpath -m -- "$tc14_mods" 2>/dev/null || printf '%s' "$tc14_mods")"
    tc14_run_start="$(date +%s)"

    run_script_bounded "$CASE_LIMIT_SECONDS" \
        "HOME=${REAL_HOME}" \
        "XDG_CONFIG_HOME=${REAL_HOME}/.config" \
        -- \
        --game-path "$REAL_GAME_DIR" \
        --mods-path "$tc14_mods" \
        --smapi-log "$REAL_SMAPI_LOG" \
        --timeout 120

    assert_exit 0 "TC-14: the run against the overridden mods directory exits 0"

    if [[ -f "$REAL_SMAPI_LOG" ]]; then
        pass "TC-14: the game wrote its SMAPI log"
        tc14_log_text="$(tr -d '\r' < "$REAL_SMAPI_LOG" 2>/dev/null || true)"
        tc14_log_mtime="$(stat -c %Y -- "$REAL_SMAPI_LOG" 2>/dev/null || printf '0')"
        if (( tc14_log_mtime >= tc14_run_start )); then
            pass "TC-14: the SMAPI log belongs to this run"
        else
            fail "TC-14: the SMAPI log belongs to this run" \
                "the log's mtime (${tc14_log_mtime}) predates the run (${tc14_run_start}) — it is a stale log"
        fi
        assert_contains "$tc14_log_text" "Log started at" \
            "TC-14: the SMAPI log carries the run sentinel"
        # SMAPI prints the resolved mods directory itself, canonicalised and
        # `~`-anonymised. The fixture path lives outside the home directory,
        # so no anonymisation applies to it.
        assert_contains "$tc14_log_text" "Mods go here: ${tc14_expected}" \
            "TC-14: SMAPI's 'Mods go here:' line names the resolved mods directory"
    else
        fail "TC-14: the game wrote its SMAPI log" \
            "no SMAPI log at '${REAL_SMAPI_LOG}'"
    fi
fi

# ---------------------------------------------------------------------------
# TC-15 — the run log, the run id, the VERSION and the stream split
# ---------------------------------------------------------------------------

announce "TC-15" "the run log exists with the run id, the VERSION and the launch timestamp"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-15 (run log and stream split)" "the host tool sweep"
elif ! real_game_available; then
    skip "TC-15 (run log and stream split)" "the real game installation"
elif [[ -z "$TC1_RUN_LOG" ]]; then
    skip "TC-15 (run log and stream split)" "the TC-1 run log"
else
    tc15_log="$TC1_RUN_LOG"
    tc15_stdout="$TC1_STDOUT"
    tc15_stderr="$TC1_STDERR"
    tc15_runs_dir="${HARNESS_XDG_STATE_HOME}/living-roots-smoke/runs"

    assert_file_exists "$tc15_log" \
        "TC-15: the run log exists at the FR-014 path"
    assert_contains "$tc15_log" "${HARNESS_XDG_STATE_HOME}/living-roots-smoke/runs/" \
        "TC-15: the run log lives under the XDG state base"
    assert_file_mode "$tc15_log" 600 \
        "TC-15: the run log has mode 0600"
    assert_dir_mode "${HARNESS_XDG_STATE_HOME}/living-roots-smoke" 700 \
        "TC-15: the living-roots-smoke directory has mode 0700"
    assert_dir_mode "$tc15_runs_dir" 700 \
        "TC-15: the runs directory has mode 0700"

    tc15_run_id="$(basename -- "$tc15_log")"
    tc15_run_id="${tc15_run_id%.log}"

    assert_matches "$tc15_run_id" '^[0-9]{8}T[0-9]{6}Z-[0-9]+$' \
        "TC-15: the run id has the <UTC timestamp>-<PID> form"
    assert_contains "$tc15_log" "$tc15_run_id" \
        "TC-15: the run log contains the run id"

    # The script's own VERSION, taken from --version (the same source the
    # run-log header and the stdout summary must carry).
    run_script --version
    tc15_version="$(printf '%s\n' "$RUN_STDOUT" | head -n 1 | awk '{print $NF}' || true)"
    if [[ -z "$tc15_version" ]]; then
        fail "TC-15: the run log contains the script VERSION" \
            "--version printed no parsable version"
    else
        assert_contains "$tc15_log" "$tc15_version" \
            "TC-15: the run log contains the script VERSION"
        assert_contains "$tc15_stdout" "$tc15_version" \
            "TC-15: the stdout summary contains the script VERSION"
    fi

    assert_contains "$tc15_stdout" "$tc15_run_id" \
        "TC-15: the stdout summary carries the run id"
    assert_contains "$tc15_stdout" "$tc15_log" \
        "TC-15: the stdout summary carries the run-log path"

    assert_matches "$tc15_log" '[0-9]{4}-[0-9]{2}-[0-9]{2}[T ][0-9]{2}:[0-9]{2}:[0-9]{2}' \
        "TC-15: the run log records the launch timestamp"

    assert_eq "0" "$(run_log_lines_unstamped "$tc15_log" "$tc15_run_id")" \
        "TC-15: every log line is stamped with the run id"

    # The verdict is on stdout, the diagnostics on stderr.
    assert_contains_line "$tc15_stdout" "SUCCESS: Mod loaded without errors." \
        "TC-15: the verdict is on stdout"
    assert_not_contains "$tc15_stdout" "What:" \
        "TC-15: the failure report is not on stdout"
    assert_not_contains "$tc15_stdout" "ERROR:" \
        "TC-15: the diagnostics are not on stdout"
    assert_not_contains "$tc15_stderr" "SUCCESS: Mod loaded without errors." \
        "TC-15: the verdict is not on stderr"
fi

# ---------------------------------------------------------------------------
# TC-16 — --help completeness (launch-free)
# ---------------------------------------------------------------------------

announce "TC-16" "--help documents every contract the spec requires"

if ! suite_budget_left; then
    skip "TC-16 (--help completeness)" "the suite time budget"
else
    run_script --help

    tc16_help="$RUN_STDOUT"

    assert_exit 0 "TC-16: --help exits 0"
    assert_contains "$tc16_help" "Usage:" \
        "TC-16: --help prints a Usage: line"
    assert_contains "$tc16_help" "smoke-validation.sh" \
        "TC-16: --help names the script"

    # Every row of the normative Command-Line Options table: the spelling, the
    # argument placeholder and the default.
    assert_contains "$tc16_help" "--game-path DIR" \
        "TC-16: --help documents --game-path DIR"
    assert_contains "$tc16_help" "~/.steam/steam/steamapps/common/Stardew Valley" \
        "TC-16: --help documents the default of --game-path"
    assert_contains "$tc16_help" "--mods-path DIR" \
        "TC-16: --help documents --mods-path DIR"
    assert_contains "$tc16_help" "<game-path>/Mods" \
        "TC-16: --help documents the default of --mods-path"
    assert_contains "$tc16_help" "--mod-project FILE" \
        "TC-16: --help documents --mod-project FILE"
    assert_contains "$tc16_help" "LivingRoots/LivingRoots.csproj" \
        "TC-16: --help documents the default of --mod-project"
    assert_contains "$tc16_help" "--timeout SECONDS" \
        "TC-16: --help documents --timeout SECONDS"
    assert_contains "$tc16_help" "120" \
        "TC-16: --help documents the default of --timeout"
    assert_contains "$tc16_help" "--build-timeout SECONDS" \
        "TC-16: --help documents --build-timeout SECONDS"
    assert_contains "$tc16_help" "300" \
        "TC-16: --help documents the default of --build-timeout"
    assert_contains "$tc16_help" "--grace-period SECONDS" \
        "TC-16: --help documents --grace-period SECONDS"
    assert_contains "$tc16_help" "15" \
        "TC-16: --help documents the default of --grace-period"
    assert_contains "$tc16_help" "--smapi-log FILE" \
        "TC-16: --help documents --smapi-log FILE"
    assert_contains "$tc16_help" "SMAPI-latest.txt" \
        "TC-16: --help documents the default of --smapi-log"
    assert_contains "$tc16_help" "--force" \
        "TC-16: --help documents --force"
    assert_contains "$tc16_help" "--help" \
        "TC-16: --help documents --help"
    assert_contains "$tc16_help" "-h" \
        "TC-16: --help documents the -h alias"
    assert_contains "$tc16_help" "--version" \
        "TC-16: --help documents --version"

    # The "Environment (set automatically)" section.
    assert_contains "$tc16_help" "Environment (set automatically)" \
        "TC-16: --help has the 'Environment (set automatically)' section"
    assert_contains "$tc16_help" "SMAPI_NO_TERMINAL=true" \
        "TC-16: --help documents SMAPI_NO_TERMINAL=true"
    assert_contains "$tc16_help" "SMAPI_DEVELOPER_MODE=true" \
        "TC-16: --help documents SMAPI_DEVELOPER_MODE=true"
    assert_contains "$tc16_help" "LIBGL_ALWAYS_SOFTWARE=1" \
        "TC-16: --help documents LIBGL_ALWAYS_SOFTWARE=1"
    assert_contains "$tc16_help" "GALLIUM_DRIVER=llvmpipe" \
        "TC-16: --help documents GALLIUM_DRIVER=llvmpipe"
    assert_contains "$tc16_help" "SDL_VIDEODRIVER=x11" \
        "TC-16: --help documents SDL_VIDEODRIVER=x11"
    assert_contains "$tc16_help" "SDL_AUDIODRIVER=dummy" \
        "TC-16: --help documents SDL_AUDIODRIVER=dummy"
    assert_contains "$tc16_help" "ALSOFT_DRIVERS=null" \
        "TC-16: --help documents ALSOFT_DRIVERS=null"
    assert_contains "$tc16_help" "DISPLAY" \
        "TC-16: --help documents DISPLAY"
    assert_contains "$tc16_help" "not user-configurable" \
        "TC-16: --help states that the environment is not user-configurable"

    # The run-log location, the run-id format and the VERSION.
    assert_contains "$tc16_help" "living-roots-smoke/runs/" \
        "TC-16: --help documents the run-log location"
    assert_contains "$tc16_help" "XDG_STATE_HOME" \
        "TC-16: --help documents the XDG state base of the run log"
    assert_contains "$tc16_help" "<UTC timestamp>-<PID>" \
        "TC-16: --help documents the run-ID format"

    run_script --version
    tc16_version_line="$(printf '%s\n' "$RUN_STDOUT" | head -n 1 || true)"
    tc16_version="$(printf '%s\n' "$tc16_version_line" | awk '{print $NF}' || true)"
    if [[ -n "$tc16_version" ]]; then
        assert_contains "$tc16_help" "$tc16_version" \
            "TC-16: --help documents the VERSION"
    else
        fail "TC-16: --help documents the VERSION" \
            "--version printed no parsable version"
    fi

    # At least one usage example (quickstart TC-16).
    assert_matches "$tc16_help" '^[[:space:]]*\$?[[:space:]]*\.?/?scripts/smoke-validation\.sh' \
        "TC-16: --help contains at least one usage example"

    # Every exit code of the FR-007 table.
    for tc16_code in 0 1 2 3 4 5 6 7 9 10 11 12 13 14 15 16 17 129 130 143; do
        if printf '%s\n' "$tc16_help" | grep -qE "^[[:space:]]*${tc16_code}[[:space:]]"; then
            pass "TC-16: --help documents the exit code ${tc16_code}"
        else
            fail "TC-16: --help documents the exit code ${tc16_code}" \
                "no line in the exit-code table starts with '${tc16_code}'"
        fi
    done
fi

# ---------------------------------------------------------------------------
# TC-17 — the FR-007 status-translation table, every row (fixture)
#
# Each raw child status is fed to the script through a stub apphost that
# returns exactly that status and writes no log. The script's exit code must
# be the documented translation.
#
# Interpretation note: FR-002's "readiness vs. exit" rule (a clean exit with
# no log is 15, never 0) is read as applying to a CLEAN exit — the clause
# names "an exit status of 0" explicitly — while a non-zero child status is
# classified by the FR-007 translation table, which is the only reading under
# which the table's 131/134/154 rows are reachable at all (a broken SMAPI
# installation never writes a log).
# ---------------------------------------------------------------------------

announce "TC-17" "every raw child status maps to its documented exit code"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-17 (status translation)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-17 (status translation)" "the suite time budget"
else
    tc17_game="$(fixture_game tc17 || true)"
    tc17_log="${FIXTURE_ROOT}/tc17/SMAPI-latest.txt"
    tc17_marker="${FIXTURE_ROOT}/tc17/launched.marker"
    register_stub "$tc17_marker"

    # The table: <raw child status>:<documented script exit code>
    tc17_rows=(
        "124:4"
        "125:13"
        "126:9"
        "127:9"
        "131:2"
        "134:2"
        "135:13"
        "154:2"
        "42:13"
    )

    for tc17_row in "${tc17_rows[@]}"; do
        tc17_raw="${tc17_row%%:*}"
        tc17_expected="${tc17_row##*:}"

        rm -f -- "$tc17_marker"
        run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
            "SMOKE_STUB_MARKER=${tc17_marker}" \
            "SMOKE_STUB_EXIT=${tc17_raw}" \
            -- \
            --game-path "$tc17_game" \
            --smapi-log "$tc17_log" \
            --timeout 60

        kill_stub "$tc17_marker"

        assert_eq "$tc17_expected" "$RUN_CODE" \
            "TC-17: the raw status ${tc17_raw} maps to ${tc17_expected}"
    done

    # 137 (SIGKILL) is context-dependent by the table itself: 5 for the game
    # process, 2 for the SMAPI apphost, 13 otherwise. The contract the table
    # states is that the raw status never escapes as the script's own code.
    rm -f -- "$tc17_marker"
    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_MARKER=${tc17_marker}" \
        "SMOKE_STUB_EXIT=137" \
        -- \
        --game-path "$tc17_game" \
        --smapi-log "$tc17_log" \
        --timeout 60

    kill_stub "$tc17_marker"

    assert_ne "137" "$RUN_CODE" \
        "TC-17: the raw status 137 never becomes the script's own exit code"
    case "$RUN_CODE" in
        5|2|13)
            pass "TC-17: the raw status 137 maps to one of 5 / 2 / 13"
            ;;
        *)
            fail "TC-17: the raw status 137 maps to one of 5 / 2 / 13" \
                "expected 5, 2 or 13, got ${RUN_CODE}"
            ;;
    esac

    # 0 -> 0: the child exits cleanly AND the log shows a loaded mod.
    rm -f -- "$tc17_marker"
    stub_log "$tc17_log" mod_loaded
    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_LOG=${tc17_log}" \
        "SMOKE_STUB_LOG_SRC=${tc17_log}" \
        "SMOKE_STUB_MARKER=${tc17_marker}" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc17_game" \
        --smapi-log "$tc17_log" \
        --timeout 60

    kill_stub "$tc17_marker"

    assert_eq "0" "$RUN_CODE" \
        "TC-17: the raw status 0 maps to 0 when the log shows a loaded mod"

    # 128+N for the signals delivered to the script itself. SIGINT is TC-7;
    # SIGTERM and SIGHUP are checked here.
    for tc17_signal in TERM HUP; do
        if (( HAVE_PERL == 0 )); then
            skip "TC-17: SIG${tc17_signal} maps to the documented status" "perl"
            continue
        fi
        tc17_probe="${FIXTURE_ROOT}/tc17/signal-probe.pl"
        write_signal_probe "$tc17_probe"

        rm -f -- "$tc17_marker"
        tc17_probe_output="$(perl "$tc17_probe" "${FIXTURE_ROOT}/tc17/probe-${tc17_signal}.txt" \
            "$tc17_marker" "$tc17_signal" 120 \
            env "HOME=${HARNESS_HOME}" \
                "XDG_STATE_HOME=${HARNESS_XDG_STATE_HOME}" \
                "XDG_RUNTIME_DIR=${HARNESS_XDG_RUNTIME_DIR}" \
                "XDG_CONFIG_HOME=${HARNESS_XDG_CONFIG_HOME}" \
                "SMOKE_STUB_MARKER=${tc17_marker}" \
                "SMOKE_STUB_SLEEP=300" \
                "SMOKE_STUB_EXIT=0" \
            "$HARNESS_SCRIPT" \
                --game-path "$tc17_game" \
                --smapi-log "$tc17_log" \
                --timeout 120 2>&1 || true)"

        kill_stub "$tc17_marker"

        case "$tc17_signal" in
            TERM) tc17_expected_signal=143 ;;
            HUP)  tc17_expected_signal=129 ;;
        esac

        assert_eq "1" "$(probe_field "$tc17_probe_output" SIGNALED)" \
            "TC-17: SIG${tc17_signal} is a genuine signal death"
        assert_eq "${tc17_expected_signal}" "$(probe_field "$tc17_probe_output" SHELL)" \
            "TC-17: SIG${tc17_signal} maps to ${tc17_expected_signal}"
    done
fi

# ---------------------------------------------------------------------------
# TC-25 — idempotency (real installation)
# ---------------------------------------------------------------------------

announce "TC-25" "two consecutive runs produce the same code and the same verdict"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-25 (idempotency)" "the host tool sweep"
elif ! real_game_available; then
    skip "TC-25 (idempotency)" "the real game installation"
elif ! guard_real_deploy_target; then
    fail "TC-25: the deploy target resolves to the real Mods/LivingRoots" \
        "the resolved deploy target is not '${REAL_GAME_DIR}/Mods/LivingRoots' — refusing to run against the real installation"
elif ! suite_budget_left; then
    skip "TC-25 (idempotency)" "the suite time budget"
else
    pass "TC-25: the deploy target resolves to the real Mods/LivingRoots"

    run_script_bounded "$CASE_LIMIT_SECONDS" \
        "HOME=${REAL_HOME}" \
        "XDG_CONFIG_HOME=${REAL_HOME}/.config" \
        -- \
        --game-path "$REAL_GAME_DIR" \
        --mods-path "$REAL_MODS_DIR" \
        --smapi-log "$REAL_SMAPI_LOG" \
        --timeout 120

    tc25_code_1="$RUN_CODE"
    tc25_verdict_1="$(printf '%s\n' "$RUN_STDOUT" | grep -E '^(SUCCESS|FAILURE)' | head -n 1 || true)"

    run_script_bounded "$CASE_LIMIT_SECONDS" \
        "HOME=${REAL_HOME}" \
        "XDG_CONFIG_HOME=${REAL_HOME}/.config" \
        -- \
        --game-path "$REAL_GAME_DIR" \
        --mods-path "$REAL_MODS_DIR" \
        --smapi-log "$REAL_SMAPI_LOG" \
        --timeout 120

    tc25_code_2="$RUN_CODE"
    tc25_verdict_2="$(printf '%s\n' "$RUN_STDOUT" | grep -E '^(SUCCESS|FAILURE)' | head -n 1 || true)"

    assert_eq "$tc25_code_1" "$tc25_code_2" \
        "TC-25: both runs exit with the same code"
    assert_eq "$tc25_verdict_1" "$tc25_verdict_2" \
        "TC-25: both runs print the same verdict"
    assert_ne "" "$tc25_verdict_1" \
        "TC-25: the runs print a verdict line"
    assert_ne "6" "$tc25_code_2" \
        "TC-25: the second run re-acquires the lock"
fi

# ---------------------------------------------------------------------------
# TC-26 — the build timeout (fixture)
# ---------------------------------------------------------------------------

announce "TC-26" "the rebuild-and-verify phase is bounded -> exit 12"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-26 (build timeout)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-26 (build timeout)" "the suite time budget"
else
    tc26_game="$(fixture_game tc26 || true)"
    tc26_log="${FIXTURE_ROOT}/tc26/SMAPI-latest.txt"
    tc26_marker="${FIXTURE_ROOT}/tc26/launched.marker"
    register_stub "$tc26_marker"

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_MARKER=${tc26_marker}" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc26_game" \
        --smapi-log "$tc26_log" \
        --build-timeout 1 \
        --timeout 60

    kill_stub "$tc26_marker"

    assert_exit 12 "TC-26: an expired build timeout exits 12"
    assert_file_absent "$tc26_marker" \
        "TC-26: the game was never launched"
    assert_matches "$RUN_STDERR" '[0-9]+(\.[0-9]+)?[[:space:]]*(s|sec|second)' \
        "TC-26: the report names the elapsed time"
    assert_matches "$RUN_STDERR" '1[[:space:]]*(s|sec|second)' \
        "TC-26: the report names the expired bound"
    assert_contains "$RUN_STDERR" "Fix:" \
        "TC-26: the report carries an actionable Fix: line"
fi

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

printf '# integration suite wall time: %s seconds\n' "$(elapsed_seconds)"

finish_tests
