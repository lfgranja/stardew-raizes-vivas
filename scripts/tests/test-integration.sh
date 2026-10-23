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
# The cases this wave added (they are fixture-driven, except where noted):
#
#   TC-34  a --timeout shorter than the rebuild still reaches the launch and
#          exits 4 — the deadline is anchored at the launch instant, not at
#          the script's start (FR-009, SC-001)
#   TC-35  the allocated display's socket is gone and the run's temporary
#          directory does not survive the run (FR-010, SC-010)
#   TC-36  the run log's mode, its directory modes, the run-id form, the
#          VERSION, the per-line stamping and the stdout/stderr split, on a
#          FIXTURE run (FR-014, SC-014)
#   TC-37  idempotency on a FIXTURE run: two runs, the same code and the
#          same verdict (FR-008, SC-015)
#   TC-38  the deploy effectiveness is asserted against the folder
#          ModBuildConfig actually deploys into — the project's declared
#          <ModFolderName>, which differs from the project file's base name
#          (FR-016 (b) (d), SC-012)
#   TC-39  the phase in which the run failed appears in the stdout
#          `phase durations:` block AND in the run log, with its end
#          timestamp and its elapsed seconds (FR-014, SC-001)
#   TC-40  a deploy target that cannot be removed aborts the run with exit
#          12, naming the target and the removal's error (FR-016 (b))
#   TC-41  the run log carries the launch epoch the sentinel-freshness
#          comparison used, and the header's `launch timestamp` is not the
#          run-start instant (FR-002, FR-014, SC-014)
#   TC-42  the mod's own success marker is reported on stdout as
#          `mod-success-marker:` when it appears inside the window between
#          the mod-resolution header and the termination (FR-003, FR-006)
#   TC-43  the rebuild restores from the PINNED NuGet cache: the script's
#          children inherit the harness's NUGET_PACKAGES and DOTNET_*
#          settings (suite)
#   TC-44  a child's last line reaches the run log even when the child ends
#          WITHOUT a trailing newline, and so does the build output's last
#          line (FR-005, FR-014)
#
# The convergence cases (they share ONE fixture run, the way TC-1's run is
# shared by TC-14, TC-15 and TC-25):
#
#   TC-27  the process-group invariant: every child's recorded process group
#          equals the child's recorded PID (FR-005)
#   TC-28  the deferred save check is labelled `SKIPPED:` on stderr and in the
#          run log, exactly once, and does not change the verdict (FR-004)
#   TC-29  the stdout summary prints EVERY phase's duration, and the durations
#          agree with the ones the run log records (FR-014)
#   TC-30  the run log holds every line the script emits (FR-014)
#   TC-31  every signal is logged with the signal name and the target, the
#          grace period is logged, and the SIGKILL escalation is logged
#          (FR-014)
#   TC-32  the stdout verdict and the exit code always agree, and the verdict
#          is printed only after the cleanup (FR-006, FR-010)
#   TC-33  a containment failure produces EXACTLY ONE failure report (FR-007)
#
# HOW THE CASES ARE DRIVEN
#
#   * launch-free      TC-3, TC-5, TC-16, TC-33 — no game, no display, no
#                      build.
#   * fixture-driven   TC-2, TC-4, TC-6, TC-7, TC-9, TC-10, TC-11, TC-12,
#                      TC-17, TC-26, TC-27 … TC-32 — a synthetic game directory
#                      whose `StardewModdingAPI` apphost is a controllable stub.
#                      The stub writes the synthetic SMAPI log the case needs
#                      and returns the exit status the case needs, so the real
#                      game is never launched and nothing outside the sandbox
#                      is touched. The shared run of TC-27 … TC-32 uses a stub
#                      that IGNORES SIGTERM, so the script's SIGTERM -> grace
#                      period -> SIGKILL escalation is exercised.
#   * fixture-project TC-34 … TC-44 — the cases that need a mod project of
#                      their own use a THROWAWAY one, generated with the real
#                      generator (`dotnet new classlib`) and carrying the same
#                      ModBuildConfig reference the real mod carries, so the
#                      rebuild really deploys. The throwaway project lives in
#                      a gitignored directory under the repository root (the
#                      script REQUIRES `--mod-project` to live inside the
#                      repository — FR-016 (a)) and the suite removes it.
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
#   THE REPOSITORY TREE IS WRITTEN TO. This is the one documented difference
#   between this suite and the pre-flight suite, which writes nothing into
#   the repository:
#
#     * The rebuild is a REAL `dotnet build` of a REAL project, so it writes
#       the build output into the project's own tree. The cases that use the
#       repository's own mod project (`--mod-project` left at its default)
#       therefore write `LivingRoots/obj/` and `LivingRoots/bin/`. Both
#       directories are gitignored build outputs; no tracked file is touched
#       and no source file is modified.
#     * The cases that need a project of their own (TC-34 … TC-44) build a
#       THROWAWAY project under `temp_test/smoke-fixture/` — a gitignored
#       directory at the repository root, because FR-016 (a) prefix-tests
#       `--mod-project` against the repository root and rejects a project
#       outside it. The suite removes that directory when it finishes, and
#       removes a stale one left behind by an interrupted run when it
#       starts.
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
#   SMOKE_SKIP_INTEGRATION=1 skips the whole file with
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
    # The throwaway fixture projects live in the repository (FR-016 (a)
    # rejects a mod project outside it), so they are removed here — a
    # project left behind by an interrupted run would otherwise show up as
    # an untracked directory.
    if declare -F remove_fixture_projects >/dev/null 2>&1; then
        remove_fixture_projects
    fi
    return 0
}

trap integration_cleanup EXIT

# The harness is linted IN ITS OWN RIGHT by the same hook step (it matches
# `scripts/tests/**/*.sh`), so it is deliberately not followed from here:
# `-x` would pull it in a second time and report its findings against this
# file.
# shellcheck disable=SC1091  # the harness is linted separately, by the same step
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/lib/harness.sh"

# ---------------------------------------------------------------------------
# Suite-level configuration
# ---------------------------------------------------------------------------

# The overall guard: no new case is started once the suite has run for this
# long. Every individual case is bounded as well, so the suite can never hang.
#
# The budget covers the real-installation cases (a real rebuild plus a real
# game launch), the status-translation table (ten rebuilds) and the fixture
# cases, each of which pays a rebuild of its own.
SUITE_BUDGET_SECONDS=3000

# The external bound of a single case. The launch-dependent cases drive the
# real game, which needs ~15 s unloaded and up to ~45 s under load; the
# fixture cases pay one `dotnet build` (~10 s) each.
CASE_LIMIT_SECONDS=300
FIXTURE_CASE_LIMIT_SECONDS=240

SUITE_START="$(date +%s)"

# The fixture tree is created up front: the cases write their scratch files
# (the run-log snapshots, the temporary-directory listings) into it.
fixture_root >/dev/null 2>&1 || true

# The throwaway fixture projects of a PREVIOUS run are removed before the
# suite starts: a run the host interrupted leaves them behind, and they
# would otherwise pile up in the repository tree.
rm -rf -- "${HARNESS_REPO_ROOT}/temp_test/smoke-fixture" 2>/dev/null || true

# The user's real home. The harness redirects HOME only for the child it
# spawns; the suite's own $HOME is still the real one.
REAL_HOME="${HOME}"
REAL_GAME_DIR="${REAL_HOME}/.steam/steam/steamapps/common/Stardew Valley"
REAL_MODS_DIR="${REAL_GAME_DIR}/Mods"
REAL_SMAPI_LOG="${REAL_HOME}/.config/StardewValley/ErrorLogs/SMAPI-latest.txt"

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
        # shellcheck disable=SC2034  # read by the harness, which this file drives
        RUN_PERFORMED=1
        return 0
    fi

    local -a env_args=(
        "HOME=${HARNESS_HOME}"
        "XDG_STATE_HOME=${HARNESS_XDG_STATE_HOME}"
        "XDG_RUNTIME_DIR=${HARNESS_XDG_RUNTIME_DIR}"
        "XDG_CONFIG_HOME=${HARNESS_XDG_CONFIG_HOME}"
    )
    # The .NET SDK is used by the rebuild phase (FR-016), and a sandboxed
    # HOME has no NuGet cache: without the pinned cache the rebuild would
    # restore from nuget.org on every run, which needs the network and makes
    # the fixture cases slow and flaky. The harness pins the same values in
    # its own invocation path (`_run_script_impl`); this wrapper carries
    # them too, so both paths hand the script the same environment.
    if [[ -d "${HARNESS_ORIGINAL_HOME}/.nuget/packages" ]]; then
        env_args+=("NUGET_PACKAGES=${HARNESS_ORIGINAL_HOME}/.nuget/packages")
    fi
    env_args+=("DOTNET_CLI_TELEMETRY_OPTOUT=1")
    env_args+=("DOTNET_NOLOGO=1")
    env_args+=("DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1")
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
    # RUN_PERFORMED is the harness's own flag: assert_exit reads it to tell
    # "the run failed" from "no run happened yet". This wrapper does not go
    # through the harness's run_script, so it publishes the flag itself.
    # shellcheck disable=SC2034  # read by the harness, which this file drives
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
#   SMOKE_STUB_ENV_DUMP  a file the stub records its ENVIRONMENT in
#
# The marker is how a case proves that the game was (or was not) launched.
#
# The environment dump is how a case proves that the environment the script
# under test hands to its children carries the pinned NuGet cache and the
# DOTNET_* settings the rebuild needs (the stub is a grandchild of the
# script, so it inherits the script's environment).
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

if [[ -n "${SMOKE_STUB_ENV_DUMP:-}" ]]; then
    {
        printf 'NUGET_PACKAGES=%s\n' "${NUGET_PACKAGES-}"
        printf 'DOTNET_CLI_TELEMETRY_OPTOUT=%s\n' "${DOTNET_CLI_TELEMETRY_OPTOUT-}"
        printf 'DOTNET_NOLOGO=%s\n' "${DOTNET_NOLOGO-}"
        printf 'DOTNET_SKIP_FIRST_TIME_EXPERIENCE=%s\n' "${DOTNET_SKIP_FIRST_TIME_EXPERIENCE-}"
    } > "${SMOKE_STUB_ENV_DUMP}" 2>/dev/null || true
fi

if [[ -n "${SMOKE_STUB_LOG:-}" && -n "${SMOKE_STUB_LOG_SRC:-}" ]]; then
    if [[ -f "${SMOKE_STUB_LOG_SRC}" ]]; then
        cp -- "${SMOKE_STUB_LOG_SRC}" "${SMOKE_STUB_LOG}" 2>/dev/null || true
        # The run sentinel must be stamped AT LAUNCH: the script rejects a
        # sentinel older than the launch timestamp (FR-002's freshness rule),
        # and the template was written before the run started.
        if [[ -f "${SMOKE_STUB_LOG}" ]]; then
            now="$(date -u +%Y-%m-%dT%H:%M:%S)"
            tmp="${SMOKE_STUB_LOG}.tmp.$$"
            if sed "s/Log started at [^ ]* UTC/Log started at ${now} UTC/" \
                -- "${SMOKE_STUB_LOG}" > "$tmp" 2>/dev/null; then
                mv -f -- "$tmp" "${SMOKE_STUB_LOG}" 2>/dev/null || true
            fi
            rm -f -- "$tmp" 2>/dev/null || true
        fi
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

# write_stub_apphost_ignore_sigterm <path> — install a stand-in for SMAPI's
# native apphost that IGNORES SIGTERM.
#
# It is the same stub as write_stub_apphost, with one difference: SIGTERM is
# ignored (by the stub AND by every child it starts, because the disposition
# is inherited across fork/exec). The stub therefore stays alive until the
# script escalates to SIGKILL, which is what exercises the escalation path
# FR-014 requires the run log to record — without driving the real game.
#
# The stub honours the same environment variables as write_stub_apphost:
# SMOKE_STUB_LOG, SMOKE_STUB_LOG_SRC and SMOKE_STUB_MARKER.
write_stub_apphost_ignore_sigterm() {
    local path="${1-}"
    if [[ -z "$path" ]]; then
        printf 'write_stub_apphost_ignore_sigterm: a path is required\n' >&2
        return 1
    fi
    cat > "$path" <<'STUB'
#!/usr/bin/env bash
# Controllable stand-in for SMAPI's native apphost that IGNORES SIGTERM.
#
# It launches no game and touches nothing outside the sandbox. It produces the
# synthetic SMAPI log the script polls and then stays alive, so the script's
# SIGTERM -> grace period -> SIGKILL escalation is exercised.
set -u

# SIGTERM is ignored for this shell AND for every command it starts (the
# disposition is inherited across fork/exec), so the script's grace period
# expires and the escalation path is taken.
trap '' TERM

if [[ -n "${SMOKE_STUB_MARKER:-}" ]]; then
    printf '%s\n' "$$" > "${SMOKE_STUB_MARKER}" 2>/dev/null || true
fi

if [[ -n "${SMOKE_STUB_LOG:-}" && -n "${SMOKE_STUB_LOG_SRC:-}" ]]; then
    if [[ -f "${SMOKE_STUB_LOG_SRC}" ]]; then
        cp -- "${SMOKE_STUB_LOG_SRC}" "${SMOKE_STUB_LOG}" 2>/dev/null || true
        # The run sentinel must be stamped AT LAUNCH: the script rejects a
        # sentinel older than the launch timestamp (FR-002's freshness rule).
        if [[ -f "${SMOKE_STUB_LOG}" ]]; then
            now="$(date -u +%Y-%m-%dT%H:%M:%S)"
            tmp="${SMOKE_STUB_LOG}.tmp.$$"
            if sed "s/Log started at [^ ]* UTC/Log started at ${now} UTC/" \
                -- "${SMOKE_STUB_LOG}" > "$tmp" 2>/dev/null; then
                mv -f -- "$tmp" "${SMOKE_STUB_LOG}" 2>/dev/null || true
            fi
            rm -f -- "$tmp" 2>/dev/null || true
        fi
    fi
fi

# Stay alive until the script escalates to SIGKILL.
while :; do
    sleep 1
done
STUB
    chmod 0755 -- "$path" 2>/dev/null || true
    return 0
}

# write_stub_apphost_partial_line <path> <text> — a stand-in for SMAPI's
# native apphost whose LAST line has NO trailing newline.
#
# The child's output reaches the run log through a reader that loops over
# `read`. `read` returns non-zero at EOF with a PARTIAL buffer, and the loop
# body then never runs for it — so a child that was killed mid-write loses
# its last line silently. The stub writes the text without a newline and
# then stays alive, so the script has to terminate it: that is exactly the
# "child killed mid-write" shape the case needs.
write_stub_apphost_partial_line() {
    local path="${1-}"
    local text="${2-}"
    if [[ -z "$path" ]]; then
        printf 'write_stub_apphost_partial_line: a path is required\n' >&2
        return 1
    fi
    cat > "$path" <<'STUB'
#!/usr/bin/env bash
# Stand-in for SMAPI's native apphost whose last line has no newline.
set -u

if [[ -n "${SMOKE_STUB_MARKER:-}" ]]; then
    printf '%s\n' "$$" > "${SMOKE_STUB_MARKER}" 2>/dev/null || true
fi

if [[ -n "${SMOKE_STUB_LOG:-}" && -n "${SMOKE_STUB_LOG_SRC:-}" ]]; then
    if [[ -f "${SMOKE_STUB_LOG_SRC}" ]]; then
        cp -- "${SMOKE_STUB_LOG_SRC}" "${SMOKE_STUB_LOG}" 2>/dev/null || true
        now="$(date -u +%Y-%m-%dT%H:%M:%S)"
        tmp="${SMOKE_STUB_LOG}.tmp.$$"
        if sed "s/Log started at [^ ]* UTC/Log started at ${now} UTC/" \
            -- "${SMOKE_STUB_LOG}" > "$tmp" 2>/dev/null; then
            mv -f -- "$tmp" "${SMOKE_STUB_LOG}" 2>/dev/null || true
        fi
        rm -f -- "$tmp" 2>/dev/null || true
    fi
fi

# The last line: NO trailing newline. The script terminates this child, so
# the reader sees the partial buffer at EOF.
printf '%s' "${SMOKE_STUB_PARTIAL_TEXT:-PARTIAL-LINE-WITHOUT-NEWLINE}"

# Stay alive until the script terminates the process group.
while :; do
    sleep 1
done
STUB
    chmod 0755 -- "$path" 2>/dev/null || true
    return 0
}

# write_stub_apphost_delayed_marker <path> — a stand-in for
# SMAPI's native apphost that writes the mod's OWN success marker into the
# SMAPI log a couple of seconds AFTER the launch.
#
# The mod emits its marker on `GameLaunched`, which is AFTER SMAPI resolved
# the mods: the marker therefore lands inside the window between the
# mod-resolution header and the termination, and never before the log is
# read. A static fixture cannot express that — the marker would already be
# in the file when the gate reads it — so the stub appends it later.
#
# The delay is carried by the run's SMOKE_STUB_MARKER_DELAY environment
# variable (seconds, default 2), which the stub itself reads: the stub and
# the case that drives it therefore cannot disagree about how long the
# marker is held back.
write_stub_apphost_delayed_marker() {
    local path="${1-}"
    if [[ -z "$path" ]]; then
        printf 'write_stub_apphost_delayed_marker: a path is required\n' >&2
        return 1
    fi
    cat > "$path" <<'STUB'
#!/usr/bin/env bash
# Stand-in for SMAPI's native apphost that appends the mod's own success
# marker to the SMAPI log a couple of seconds after the launch.
set -u

if [[ -n "${SMOKE_STUB_MARKER:-}" ]]; then
    printf '%s\n' "$$" > "${SMOKE_STUB_MARKER}" 2>/dev/null || true
fi

if [[ -n "${SMOKE_STUB_LOG:-}" && -n "${SMOKE_STUB_LOG_SRC:-}" ]]; then
    if [[ -f "${SMOKE_STUB_LOG_SRC}" ]]; then
        cp -- "${SMOKE_STUB_LOG_SRC}" "${SMOKE_STUB_LOG}" 2>/dev/null || true
        now="$(date -u +%Y-%m-%dT%H:%M:%S)"
        tmp="${SMOKE_STUB_LOG}.tmp.$$"
        if sed "s/Log started at [^ ]* UTC/Log started at ${now} UTC/" \
            -- "${SMOKE_STUB_LOG}" > "$tmp" 2>/dev/null; then
            mv -f -- "$tmp" "${SMOKE_STUB_LOG}" 2>/dev/null || true
        fi
        rm -f -- "$tmp" 2>/dev/null || true
    fi
fi

sleep "${SMOKE_STUB_MARKER_DELAY:-2}"

# The mod's own success marker, as a SMAPI record whose source is the mod.
if [[ -n "${SMOKE_STUB_LOG:-}" ]]; then
    printf '[%s INFO  %s] The %s mod was loaded successfully!\n' \
        "$(date -u +%H:%M:%S)" \
        "${SMOKE_STUB_MOD_NAME:-Stardew Valley - Living Roots}" \
        "'${SMOKE_STUB_MOD_NAME:-Stardew Valley - Living Roots}'" \
        >> "${SMOKE_STUB_LOG}" 2>/dev/null || true
fi

# Stay alive until the script terminates the process group.
while :; do
    sleep 1
done
STUB
    chmod 0755 -- "$path" 2>/dev/null || true
    return 0
}

# write_stub_dotnet <path> — a stand-in for the `dotnet` CLI whose build
# output ends WITHOUT a trailing newline.
#
# The script dumps the build output into the run log with a `read` loop, so
# the build output's last line is lost the same way a child's last line is:
# `read` returns non-zero at EOF with a partial buffer and the loop body
# never runs for it. The stub stands in for `dotnet` on the PATH, so the
# rebuild phase runs against it without touching the repository's own
# project.
write_stub_dotnet() {
    local path="${1-}"
    if [[ -z "$path" ]]; then
        printf 'write_stub_dotnet: a path is required\n' >&2
        return 1
    fi
    cat > "$path" <<'STUB'
#!/usr/bin/env bash
# Stand-in for the `dotnet` CLI: the build output's LAST line has no
# trailing newline.
set -u
printf '%s\n' "stub dotnet: build output, first line"
printf '%s' "${SMOKE_STUB_BUILD_LAST_LINE:-BUILD-OUTPUT-LAST-LINE-WITHOUT-NEWLINE}"
exit "${SMOKE_STUB_BUILD_EXIT:-0}"
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

# ---------------------------------------------------------------------------
# The throwaway fixture project
#
# The cases that need a mod project of their own use one that is generated
# with the REAL generator (`dotnet new classlib`): a hand-written project
# file would be a scaffold, and a scaffold does not prove that the script's
# rebuild works against a real project.
#
# The project is a ModBuildConfig project — the same one the real mod uses —
# so the rebuild really deploys into the resolved mods path, and the
# script's deploy-effectiveness assertion has a real artifact to compare.
#
# The project lives UNDER THE REPOSITORY ROOT, because FR-016 (a)
# prefix-tests `--mod-project` against the repository root and rejects a
# project outside it. It lives in `temp_test/`, a gitignored directory, and
# the suite removes it when it finishes.
# ---------------------------------------------------------------------------

FIXTURE_PROJECT_ROOT="${HARNESS_REPO_ROOT}/temp_test/smoke-fixture"
MOD_BUILDCONFIG_VERSION="4.4.0"

# remove_fixture_projects — remove the throwaway projects. Idempotent, and
# safe to call from the EXIT trap.
remove_fixture_projects() {
    if [[ -n "$FIXTURE_PROJECT_ROOT" && -d "$FIXTURE_PROJECT_ROOT" ]]; then
        rm -rf -- "$FIXTURE_PROJECT_ROOT" 2>/dev/null || true
    fi
    return 0
}

# fixture_project <case-id> <project-name> [mod-folder-name]
#
# Generate the throwaway project and print the path of its project file.
#
# The optional third argument is the `<ModFolderName>` the project declares.
# It defaults to the project's own name — the folder ModBuildConfig deploys
# into then matches the project file's base name. TC-38 needs a project
# whose declared folder name DIFFERS from the project file's base name.
fixture_project() {
    local case_id="${1-}"
    local project_name="${2-}"
    local mod_folder="${3-}"
    local dir=""
    local csproj=""

    if [[ -z "$case_id" || -z "$project_name" ]]; then
        printf 'fixture_project: a case id and a project name are required\n' >&2
        return 1
    fi
    [[ -n "$mod_folder" ]] || mod_folder="$project_name"

    dir="${FIXTURE_PROJECT_ROOT}/${case_id}/${project_name}"
    csproj="${dir}/${project_name}.csproj"

    rm -rf -- "$dir" 2>/dev/null || true
    mkdir -p -- "$dir" || return 1

    # The generator. `--no-restore` keeps it off the network: the rebuild
    # restores from the pinned NuGet cache.
    if ! (cd -- "$dir" && DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 \
        DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
        dotnet new classlib --name "$project_name" --output "$dir" \
            --framework net6.0 --no-restore) >/dev/null 2>&1; then
        printf 'fixture_project: dotnet new classlib failed\n' >&2
        return 1
    fi

    if [[ ! -f "$csproj" ]]; then
        printf 'fixture_project: the generator produced no project file at %s\n' "$csproj" >&2
        return 1
    fi

    # The GENERATED project file is edited, not replaced: the scaffold rule
    # requires the real generator, and the properties below are exactly the
    # ones the cases need.
    sed -i \
        -e "s#  </PropertyGroup>#\n    <ModFolderName>${mod_folder}</ModFolderName>\n    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>\n    <OutputPath>bin/</OutputPath>\n    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>\n  </PropertyGroup>#" \
        "$csproj" || return 1
    sed -i \
        -e "s#</Project>#  <ItemGroup>\n    <PackageReference Include=\"Pathoschild.Stardew.ModBuildConfig\" Version=\"${MOD_BUILDCONFIG_VERSION}\" />\n  </ItemGroup>\n\n</Project>#" \
        "$csproj" || return 1

    # The manifest the script reads the UniqueID and the entry assembly
    # from (FR-016 (d)). Its NAME is the mod's display name: the script
    # binds the log gate to that name, and the synthetic SMAPI log names
    # the mod by it.
    cat > "${dir}/manifest.json" <<'JSON'
{
  "Name": "Stardew Valley - Living Roots",
  "Author": "lfgranja",
  "Version": "0.0.1",
  "Description": "throwaway fixture project of the smoke-validation suite",
  "UniqueID": "lfgranja.LivingRoots.SmokeFixture",
  "EntryDll": "PROJECT_ENTRY_DLL",
  "MinimumApiVersion": "4.0.0"
}
JSON
    sed -i "s#PROJECT_ENTRY_DLL#${project_name}.dll#" "${dir}/manifest.json" || return 1

    printf '%s' "$csproj"
    return 0
}

# make_unremovable_dir <path> — a directory that `rm -rf` CANNOT remove.
#
# The directory holds a subdirectory whose mode denies writing, so the
# entries inside it cannot be unlinked. The removal of the tree therefore
# fails with the operating system's own error, which is what the deploy
# target's removal must report.
make_unremovable_dir() {
    local path="${1-}"
    if [[ -z "$path" ]]; then
        printf 'make_unremovable_dir: a path is required\n' >&2
        return 1
    fi
    rm -rf -- "$path" 2>/dev/null || true
    mkdir -p -- "${path}/locked" || return 1
    printf 'a file that must not survive\n' > "${path}/locked/payload" || return 1
    chmod 0500 -- "${path}/locked" || return 1
    chmod 0500 -- "$path" || return 1
    return 0
}

# unlock_dir <path> — restore the permissions of a directory made
# unremovable, so the suite can clean it up.
unlock_dir() {
    local path="${1-}"
    [[ -n "$path" ]] || return 0
    chmod 0755 -- "$path" 2>/dev/null || true
    chmod 0755 -- "${path}/locked" 2>/dev/null || true
    return 0
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

# stub_log_mod_loaded_with_error <path> — a synthetic SMAPI log that lists
# the mod in the loaded-mods block AND carries a mod-scoped ERROR record.
#
# The harness's own fixture kinds cannot express the combination: `mod_loaded`
# carries no failure record, and `continuation_lines` carries a failure record
# but NO loaded-mods block — so the gate's verdict for the latter is 17
# ("SMAPI never resolved any mods") and not 5 ("the mod did not load
# cleanly"). The combination is what makes a run fail IN THE VERDICT PHASE
# with the documented code, which is the phase TC-39 asserts on.
#
# The record is appended AFTER the loaded-mods block, so the gate's first leg
# (the mod IS listed) passes and its third leg (no mod-scoped Error/Alert
# record) fails.
stub_log_mod_loaded_with_error() {
    local path="${1-}"
    local now=""

    if [[ -z "$path" ]]; then
        printf 'stub_log_mod_loaded_with_error: a path is required\n' >&2
        return 1
    fi

    make_log_fixture "$path" mod_loaded || return 1
    now="$(date -u '+%H:%M:%S')"
    _log_line "$now" "ERROR" "$HARNESS_MOD_NAME" \
        "NullReferenceException: Object reference not set to an instance of an object." \
        >> "$path"
    refresh_sentinel "$path"
    return 0
}

# kill_lock_holders <path> — terminate every process holding the file open.
#
# A process that holds a flock keeps holding it until its LAST descriptor
# closes, and a child of the holder inherits that descriptor. Killing the
# holder is therefore not enough.
kill_lock_holders() {
    local path="${1-}"
    local pid_dir pid fd target

    [[ -n "$path" ]] || return 0

    for pid_dir in /proc/[0-9]*; do
        pid="${pid_dir#/proc/}"
        for fd in "$pid_dir"/fd/*; do
            target="$(readlink "$fd" 2>/dev/null || printf '')"
            if [[ "$target" == "$path" ]]; then
                kill -TERM -- "$pid" 2>/dev/null || true
                break
            fi
        done
    done

    sleep 0.3
}

# stub_template_for <log-path> — a copy of the fixture that survives the
# script's pre-launch cleanup of the log path.
#
# The script REMOVES the stale log before it launches the game, so the
# content the stub has to write cannot live at the log path itself.
stub_template_for() {
    local log="${1-}"
    local template="${log}.template"

    if [[ -f "$log" ]]; then
        cp -f -- "$log" "$template" 2>/dev/null || true
    fi
    printf '%s' "$template"
}

# ---------------------------------------------------------------------------
# Identifying the log of the run that just finished
#
# The newest log in the sandbox is NOT necessarily the log of the run that
# just finished: an earlier case's log is still there, and a case that
# asserted against it would be asserting against a DIFFERENT run. The log
# of the run is therefore identified by the run id the script reports on
# stdout (`run-id: <id>`), and — for a run that failed before its summary
# was printed — by the log that did not exist when the run started.
#
# A run that wrote NO log is reported as such, so the case fails instead of
# silently asserting against an earlier case's log.
#
# (There is deliberately no "the newest log in the sandbox" helper: it is
# the trap this rule exists to close.)
# ---------------------------------------------------------------------------

# The directory the script writes its run logs into (FR-014).
RUN_LOGS_DIR="${HARNESS_XDG_STATE_HOME}/living-roots-smoke/runs"

# snapshot_run_logs <file> — record the run logs that exist right now, so
# the logs a later run creates can be told apart from the ones it inherited.
snapshot_run_logs() {
    local out="${1-}"
    local candidate=""
    [[ -n "$out" ]] || return 0
    mkdir -p -- "$(dirname -- "$out")" 2>/dev/null || true
    : >"$out" 2>/dev/null || return 0
    for candidate in "${RUN_LOGS_DIR}"/*.log; do
        [[ -f "$candidate" ]] || continue
        printf '%s\n' "$candidate" >>"$out"
    done
    return 0
}

# resolve_run_log <snapshot-file> <stdout-text> — the log of the run that
# just finished, or the empty string when the run wrote none.
#
# The run id on stdout identifies the run exactly. When the run failed
# before its summary was printed (no `run-id:` line), the log that did not
# exist when the snapshot was taken is the run's log — provided there is
# exactly one such log.
resolve_run_log() {
    local snapshot="${1-}"
    local stdout="${2-}"
    local run_id=""
    local candidate=""
    local new_count=0
    local new_log=""

    run_id="$(printf '%s\n' "$stdout" | sed -n 's/^run-id:[[:space:]]*//p' | head -n 1)"
    if [[ -n "$run_id" ]]; then
        candidate="${RUN_LOGS_DIR}/${run_id}.log"
        if [[ -f "$candidate" ]]; then
            printf '%s' "$candidate"
            return 0
        fi
    fi

    for candidate in "${RUN_LOGS_DIR}"/*.log; do
        [[ -f "$candidate" ]] || continue
        if [[ -n "$snapshot" && -f "$snapshot" ]] \
            && grep -qxF -- "$candidate" "$snapshot" 2>/dev/null; then
            continue
        fi
        new_count=$(( new_count + 1 ))
        new_log="$candidate"
    done

    if (( new_count == 1 )); then
        printf '%s' "$new_log"
    fi
    return 0
}

# run_id_epoch <run-id> — the run id's UTC stamp as an epoch.
#
# The run id is `<UTC timestamp>-<PID>`, so the run's START instant is
# recoverable from it alone: no separate record of the start is needed.
run_id_epoch() {
    local run_id="${1-}"
    local stamp="${run_id%%-*}"
    local iso=""

    [[ "$stamp" =~ ^[0-9]{8}T[0-9]{6}Z$ ]] || { printf ''; return 0; }
    iso="${stamp:0:4}-${stamp:4:2}-${stamp:6:2}T${stamp:9:2}:${stamp:11:2}:${stamp:13:2}Z"
    date -u -d "$iso" +%s 2>/dev/null || printf ''
}

# iso_epoch <timestamp> — a `YYYY-MM-DDTHH:MM:SSZ` timestamp as an epoch.
iso_epoch() {
    local stamp="${1-}"
    [[ "$stamp" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z$ ]] || {
        printf ''
        return 0
    }
    date -u -d "$stamp" +%s 2>/dev/null || printf ''
}

# line_epoch <line> — the epoch of a run-log line's leading UTC timestamp.
line_epoch() {
    local line="${1-}"
    local stamp="${line%% *}"
    [[ "$stamp" =~ ^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z$ ]] || {
        printf ''
        return 0
    }
    date -u -d "$stamp" +%s 2>/dev/null || printf ''
}

# first_line_containing <text> <needle> — the first line of <text> that
# contains <needle>, or the empty string.
first_line_containing() {
    local text="${1-}"
    local needle="${2-}"
    local line=""

    while IFS= read -r line || [[ -n "$line" ]]; do
        if [[ "$line" == *"$needle"* ]]; then
            printf '%s' "$line"
            return 0
        fi
    done <<< "$text"

    printf ''
}

# last_timestamped_line <text> — the LAST line of <text> that carries a
# leading UTC timestamp, or the empty string.
#
# The run log's last lines are the phase-duration block, whose rows have no
# timestamp of their own; the last TIMESTAMPED line is the one that ends the
# run's own record.
last_timestamped_line() {
    local text="${1-}"
    local line=""
    local last=""

    while IFS= read -r line || [[ -n "$line" ]]; do
        [[ -n "$(line_epoch "$line")" ]] || continue
        last="$line"
    done <<< "$text"

    printf '%s' "$last"
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
    # The pattern is assembled INLINE, at the point of use, with the field
    # name quoted SEPARATELY from the surrounding "*": only the "*" is a
    # wildcard, the field name itself is matched LITERALLY. An unquoted
    # expansion of a pattern variable inside `${..}` would let every glob
    # metacharacter that appears in the field name act as a wildcard
    # (SC2295).
    rest="${line#*"${name}"=}"
    printf '%s' "${rest%% *}"
}

# ---------------------------------------------------------------------------
# Analysis helpers for the convergence cases (TC-27 … TC-33)
#
# The harness has no assertion for "how many times does this text occur" and
# no reader for the run log's structured records, so the two are built here,
# locally — the shared library is not modified.
# ---------------------------------------------------------------------------

# count_occurrences <text> <needle> — the number of times <needle> occurs in
# <text>.
#
# The scan is non-overlapping: the text after each hit is what the next
# search starts from, so "aa" in "aaa" counts once.
count_occurrences() {
    local text="${1-}"
    local needle="${2-}"
    local count=0
    local rest="$text"

    if [[ -z "$needle" ]]; then
        printf '0'
        return 0
    fi

    while [[ "$rest" == *"$needle"* ]]; do
        count=$(( count + 1 ))
        rest="${rest#*"$needle"}"
    done

    printf '%d' "$count"
}

# assert_contains_id <text> <id> <label>
#
# Like assert_contains, but the number must appear as a WHOLE TOKEN (bounded
# by non-digits). A plain substring match would be wrong for a process id: an
# id can be a substring of a longer number elsewhere in the same line — the
# run id carries the script's own PID and a UTC stamp.
assert_contains_id() {
    local text="${1-}"
    local id="${2-}"
    local label="${3-}"
    local id_re="(^|[^0-9])${id}([^0-9]|$)"

    if [[ "$text" =~ $id_re ]]; then
        pass "$label"
    else
        fail "$label" "expected to contain the number '${id}' as a whole token"
    fi
    return 0
}

# run_log_phase_rows <run-log-text> — one "<phase>|<seconds>" row per phase the
# run log records, in the order it records them.
#
# The phase events are the script's own stamped lines:
#
#   2026-10-21T12:34:56Z [INFO] [20261021T123456Z-4711] phase pre-flight finished in 3s
#
# The row is read off that exact shape and not off a looser "phase … finished
# in …s" match, because the run log also carries the replayed build output,
# whose lines are not the script's own events.
run_log_phase_rows() {
    local text="${1-}"
    local line=""
    local phase_re='^[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}Z[[:space:]]+\[[A-Z]+\][[:space:]]+\[[^]]*\][[:space:]]+phase[[:space:]]+(.*[^[:space:]])[[:space:]]+finished[[:space:]]+in[[:space:]]+([0-9]+(\.[0-9]+)?)s'

    while IFS= read -r line || [[ -n "$line" ]]; do
        if [[ "$line" =~ $phase_re ]]; then
            printf '%s|%s\n' "${BASH_REMATCH[1]}" "${BASH_REMATCH[2]}"
        fi
    done <<< "$text"
}

# stdout_phase_duration <stdout-text> <phase> — the duration the stdout summary
# reports for a phase, or the empty string when it reports none.
#
# A line qualifies when it names the phase AND carries a duration. The first
# qualifying line wins; a line that names the phase but carries no duration
# is skipped, so a section header cannot hide the duration line below it.
#
# The duration must be preceded by whitespace (or start the line), so a value
# that merely looks numeric — a display number in "display: :99" — is not
# mistaken for a duration.
stdout_phase_duration() {
    local text="${1-}"
    local phase="${2-}"
    local line=""
    local duration_re='(^|[[:space:]])([0-9]+(\.[0-9]+)?)[[:space:]]*(s|secs|sec|second|seconds)?([[:space:]]|$)'

    while IFS= read -r line || [[ -n "$line" ]]; do
        [[ "$line" == *"$phase"* ]] || continue
        if [[ "$line" =~ $duration_re ]]; then
            printf '%s' "${BASH_REMATCH[2]}"
            return 0
        fi
    done <<< "$text"

    printf ''
}

# normalize_duration <value> — the duration reduced to whole seconds, so the
# run log's "3" and a stdout summary's "3.000" compare equal.
normalize_duration() {
    local value="${1-}"

    if [[ "$value" == *.* ]]; then
        value="${value%%.*}"
    fi

    printf '%s' "$value"
}

# run_log_child_ids <run-log-text> <child> — the "<pid>|<group>" pair the run
# log recorded for a child ("game" or "Xvfb"), or the empty string.
#
#   game launched: pid <P>, process group <G>
#   virtual display allocated: :<N> (Xvfb pid <P>, group <G>)
run_log_child_ids() {
    local text="${1-}"
    local child="${2-}"
    local line=""
    local re=""

    case "$child" in
        game)
            re='game launched: pid ([0-9]+), process group ([0-9]+)'
            ;;
        Xvfb)
            re='virtual display allocated: :[0-9]+ \(Xvfb pid ([0-9]+), group ([0-9]+)\)'
            ;;
        *)
            printf ''
            return 0
            ;;
    esac

    while IFS= read -r line || [[ -n "$line" ]]; do
        if [[ "$line" =~ $re ]]; then
            printf '%s|%s' "${BASH_REMATCH[1]}" "${BASH_REMATCH[2]}"
            return 0
        fi
    done <<< "$text"

    printf ''
}

# run_log_line_with_ids <run-log-text> <needle> <id> — the first line of the
# run log that contains <needle> AND the numeric token <id>.
#
# The id is matched as a WHOLE TOKEN (bounded by non-digits). A plain
# substring match would be wrong: a process group's id can be a substring of
# the run id (which carries the script's own PID and a UTC stamp), so a line
# that merely echoes the run id would qualify.
run_log_line_with_ids() {
    local text="${1-}"
    local needle="${2-}"
    local id="${3-}"
    local line=""
    local id_re="(^|[^0-9])${id}([^0-9]|$)"

    while IFS= read -r line || [[ -n "$line" ]]; do
        [[ "$line" == *"$needle"* ]] || continue
        [[ "$line" =~ $id_re ]] || continue
        printf '%s' "$line"
        return 0
    done <<< "$text"

    printf ''
}

# run_log_group_grace_line <run-log-text> <id> — the first line of the run log
# that names the given process group AND mentions the grace period — by the
# phrase, or by the number of seconds that was applied.
#
# The number of seconds MUST carry a unit: without one, the process group's
# own id would match ("process group 1234") and every line that names the
# group would qualify.
run_log_group_grace_line() {
    local text="${1-}"
    local id="${2-}"
    local line=""
    local id_re="(^|[^0-9])${id}([^0-9]|$)"
    local grace_re='grace[ -]period|[0-9]+(\.[0-9]+)?[[:space:]]*(s|secs|sec|second|seconds)([^[:alnum:]]|$)'

    while IFS= read -r line || [[ -n "$line" ]]; do
        [[ "$line" =~ $id_re ]] || continue
        [[ "$line" =~ $grace_re ]] || continue
        printf '%s' "$line"
        return 0
    done <<< "$text"

    printf ''
}

# first_line_number_with <text> <needle> — the 1-based number of the first
# line of <text> that contains <needle>, or the empty string.
first_line_number_with() {
    local text="${1-}"
    local needle="${2-}"
    local line=""
    local number=0

    while IFS= read -r line || [[ -n "$line" ]]; do
        number=$(( number + 1 ))
        if [[ "$line" == *"$needle"* ]]; then
            printf '%d' "$number"
            return 0
        fi
    done <<< "$text"

    printf ''
}

# last_line_number_with <text> <needle> — the 1-based number of the LAST line
# of <text> that contains <needle>, or the empty string.
last_line_number_with() {
    local text="${1-}"
    local needle="${2-}"
    local line=""
    local number=0
    local found=""

    while IFS= read -r line || [[ -n "$line" ]]; do
        number=$(( number + 1 ))
        if [[ "$line" == *"$needle"* ]]; then
            found="$number"
        fi
    done <<< "$text"

    printf '%s' "$found"
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

    tc1_snapshot="${FIXTURE_ROOT}/tc1-run-log-snapshot.txt"
    snapshot_run_logs "$tc1_snapshot"

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

    # The log of THIS run, identified by the run id the run reported on
    # stdout — never by "the newest log in the sandbox", which may belong
    # to an earlier case.
    TC1_RUN_LOG="$(resolve_run_log "$tc1_snapshot" "$RUN_STDOUT")"
    if [[ -z "$TC1_RUN_LOG" ]]; then
        fail "TC-1: the run wrote a run log" \
            "this run reported no run id and wrote no new run log under '${RUN_LOGS_DIR}/'"
    else
        pass "TC-1: the run wrote a run log"

        TC1_RUN_ID="$(basename -- "$TC1_RUN_LOG")"
        TC1_RUN_ID="${TC1_RUN_ID%.log}"

        # The CONTENT of the run log, not its path: every assertion below
        # checks what the log says.
        tc1_log_text="$(cat -- "$TC1_RUN_LOG" 2>/dev/null || true)"
        tc1_log_lower="$(printf '%s' "$tc1_log_text" | tr '[:upper:]' '[:lower:]')"

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
        "SMOKE_STUB_LOG_SRC=$(stub_template_for "${tc2_log}")" \
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
            "SMOKE_STUB_LOG_SRC=$(stub_template_for "${tc4_log}")" \
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

        # A run WITHOUT --force must leave the conflicting game alone, so
        # the process must STILL be alive here. (The check was inverted: it
        # failed exactly when the script had behaved correctly.)
        if kill -0 "$tc4_conflict_pid" 2>/dev/null; then
            pass "TC-4: the conflicting game was not clobbered"
        else
            fail "TC-4: the conflicting game was not clobbered" \
                "the fixture process ${tc4_conflict_pid} is gone — a run without --force must not touch it"
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
            "SMOKE_STUB_LOG_SRC=$(stub_template_for "${tc4_log}")" \
            "SMOKE_STUB_MARKER=${tc4_marker}" \
            "SMOKE_STUB_EXIT=0" \
            -- \
            --game-path "$tc4_game" \
            --smapi-log "$tc4_log" \
            --timeout 60 \
            --force

        # The marker is asserted BEFORE kill_stub: kill_stub removes it.
        assert_ne 7 "$RUN_CODE" "TC-4: --force does not abort with 7"
        assert_file_exists "$tc4_marker" \
            "TC-4: --force proceeds to the launch (exit ${RUN_CODE})"

        kill_stub "$tc4_marker"
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

    # The lock holder's CHILD inherits the lock descriptor, so killing the
    # flock process alone does NOT release the lock — every process holding
    # the lock file open has to go, or every later case in the suite would
    # see the lock as held and exit 6.
    kill_lock_holders "$tc5_runtime_lock"
    kill_lock_holders "$tc5_state_lock"

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

    # The marker is read BEFORE kill_stub: kill_stub removes it.
    tc6_launched=0
    if [[ -f "$tc6_marker" ]]; then
        tc6_launched=1
    fi

    kill_stub "$tc6_marker"

    assert_exit 4 "TC-6: the launch-to-verdict deadline exits 4"
    assert_eq "1" "$tc6_launched" \
        "TC-6: the game was launched before the timeout fired"
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

    # T184: the removal of the deploy target MUST SUCCEED, and the DEPLOY
    # must be the ineffective part.
    #
    # T160 made the removal's failure FATAL, so the previous fixture — an
    # unwritable deploy target — aborted the run AT THE REMOVAL (exit 12,
    # which is right) and the digest comparison was never reached: the case
    # stopped exercising the path it means to test.
    #
    # The script resolves the DEPLOYED assembly's path BEFORE it removes the
    # deploy target, and it then compares the digest of the file at that
    # path with the digest of the assembly this run built. The fixture
    # therefore makes the deployed assembly a SYMLINK to a stale assembly
    # that lives OUTSIDE the deploy target — a sibling directory of the
    # resolved mods path:
    #
    #   * the removal SUCCEEDS: the symlink is an entry of a WRITABLE
    #     directory, and unlinking it needs write permission on the
    #     DIRECTORY, not on the file the symlink points at;
    #   * the stale assembly SURVIVES the removal, because it is not inside
    #     the removed tree;
    #   * the build's deploy recreates the deploy target and lands the
    #     freshly built assembly in it, but the file the script compares
    #     against is still the stale one, so the digests DIFFER and the run
    #     aborts with exit 12 naming BOTH of them (FR-016 (d)).
    #
    # The stale assembly must live INSIDE the resolved mods path: the script
    # prefix-tests the deployed path against the mods path, and a path that
    # resolves outside that base is refused as an internal error.
    tc9_stale_dir="${tc9_game}/Mods/.tc9-stale-deployment"
    mkdir -p -- "$tc9_target" "$tc9_stale_dir" 2>/dev/null || true
    printf 'stale deployment payload\n' > "${tc9_stale_dir}/LivingRoots.dll" 2>/dev/null || true
    rm -f -- "${tc9_target}/LivingRoots.dll" 2>/dev/null || true
    ln -s -- "../.tc9-stale-deployment/LivingRoots.dll" "${tc9_target}/LivingRoots.dll" 2>/dev/null || true

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_LOG=${tc9_log}" \
        "SMOKE_STUB_LOG_SRC=$(stub_template_for "${tc9_log}")" \
        "SMOKE_STUB_MARKER=${tc9_marker}" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc9_game" \
        --smapi-log "$tc9_log" \
        --timeout 60

    kill_stub "$tc9_marker"

    assert_exit 12 "TC-9: an ineffective deploy exits 12"
    assert_file_absent "$tc9_marker" \
        "TC-9: the game was never launched"
    # The run reached FR-016 (d)'s verdict, so the removal of the deploy
    # target SUCCEEDED and the two digests were computed and compared.
    assert_contains "$RUN_STDERR" "the deployed assembly is not the one this run built" \
        "TC-9: the report names the deploy that did not take effect"
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
        "SMOKE_STUB_LOG_SRC=$(stub_template_for "${tc10_log}")" \
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
        "SMOKE_STUB_LOG_SRC=$(stub_template_for "${tc11_log}")" \
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
    # Both are needed: the path assertions check where the log lives, the
    # content assertions check what it says.
    tc15_log="$TC1_RUN_LOG"
    tc15_log_text="$(cat -- "$TC1_RUN_LOG" 2>/dev/null || true)"
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
        assert_contains "$tc15_log_text" "$tc15_version" \
            "TC-15: the run log contains the script VERSION"
        assert_contains "$tc15_stdout" "$tc15_version" \
            "TC-15: the stdout summary contains the script VERSION"
    fi

    assert_contains "$tc15_stdout" "$tc15_run_id" \
        "TC-15: the stdout summary carries the run id"
    assert_contains "$tc15_stdout" "$tc15_log" \
        "TC-15: the stdout summary carries the run-log path"

    assert_matches "$tc15_log_text" '[0-9]{4}-[0-9]{2}-[0-9]{2}[T ][0-9]{2}:[0-9]{2}:[0-9]{2}' \
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
    # The tilde is quoted ON PURPOSE: the needle is the LITERAL default the
    # help text prints, so the shell must not expand it here — the assertion
    # has to compare the help's own spelling, byte for byte.
    # shellcheck disable=SC2088  # the tilde is quoted ON PURPOSE: it is the literal default the help text prints
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

    # At least one usage example (quickstart TC-16). The match is made
    # LINE by LINE: bash's `=~` anchors `^` at the whole string, so a
    # whole-output match could never find a line in the middle.
    if printf '%s\n' "$tc16_help" \
        | grep -qE '^[[:space:]]*\$?[[:space:]]*\.?/?scripts/smoke-validation\.sh'; then
        pass "TC-16: --help contains at least one usage example"
    else
        fail "TC-16: --help contains at least one usage example" \
            "no line shows the script's invocation"
    fi

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
        "SMOKE_STUB_LOG_SRC=$(stub_template_for "${tc17_log}")" \
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
# TC-27 … TC-33 — the convergence properties
#
# The seven cases below share ONE fixture run, exactly as TC-1's run is shared
# by TC-14, TC-15 and TC-25: the artefacts are captured HERE, because the
# later cases overwrite the harness's globals.
#
# The shared run's child IGNORES SIGTERM, so the script's termination path has
# to escalate to SIGKILL. That is what makes the escalation observable
# without driving the real game, and it exercises the whole termination path
# (SIGTERM -> grace period -> SIGKILL -> observed exit) in a single run.
#
# The run is a SUCCESSFUL one: the synthetic SMAPI log names the mod in the
# loaded-mods block, so the gate passes and the script reaches the
# termination.
#
# TC-33 does NOT share this run: it needs a containment failure, which is a
# launch-free pre-flight abort.
# ---------------------------------------------------------------------------

TC27_SHARED_OK=0
TC27_RUN_LOG=""
TC27_RUN_ID=""
TC27_STDOUT=""
TC27_STDERR=""
TC27_CODE=0
TC27_RUN_LOG_TEXT=""

announce "TC-27 … TC-31" "the shared fixture run: a successful run whose child ignores SIGTERM"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "the shared fixture run (TC-27 … TC-31)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "the shared fixture run (TC-27 … TC-31)" "the suite time budget"
else
    tc27_game="$(fixture_game tc27 || true)"
    tc27_log="${FIXTURE_ROOT}/tc27/SMAPI-latest.txt"
    tc27_marker="${FIXTURE_ROOT}/tc27/launched.marker"
    register_stub "$tc27_marker"

    # The child that ignores SIGTERM: the script's grace period expires and
    # the escalation to SIGKILL is exercised.
    write_stub_apphost_ignore_sigterm "${tc27_game}/StardewModdingAPI"

    # The synthetic SMAPI log names the mod in the loaded-mods block, so the
    # gate passes and the run reaches the termination phase.
    stub_log "$tc27_log" mod_loaded

    tc27_snapshot="${FIXTURE_ROOT}/tc27-run-log-snapshot.txt"
    snapshot_run_logs "$tc27_snapshot"

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_LOG=${tc27_log}" \
        "SMOKE_STUB_LOG_SRC=$(stub_template_for "${tc27_log}")" \
        "SMOKE_STUB_MARKER=${tc27_marker}" \
        -- \
        --game-path "$tc27_game" \
        --smapi-log "$tc27_log" \
        --timeout 60 \
        --grace-period 2

    kill_stub "$tc27_marker"

    TC27_CODE="$RUN_CODE"
    TC27_STDOUT="$RUN_STDOUT"
    TC27_STDERR="$RUN_STDERR"
    # The log of THIS run, never the newest log in the sandbox: an earlier
    # case's log would make the assertions below pass for the wrong reason.
    TC27_RUN_LOG="$(resolve_run_log "$tc27_snapshot" "$RUN_STDOUT")"
    if [[ -z "$TC27_RUN_LOG" ]]; then
        fail "the shared fixture run wrote a run log" \
            "this run reported no run id and wrote no new run log under '${RUN_LOGS_DIR}/'"
    else
        TC27_RUN_LOG_TEXT="$(cat -- "$TC27_RUN_LOG" 2>/dev/null || true)"
        TC27_RUN_ID="$(basename -- "$TC27_RUN_LOG")"
        TC27_RUN_ID="${TC27_RUN_ID%.log}"
        TC27_SHARED_OK=1
        pass "the shared fixture run wrote a run log"
    fi
fi

# ---------------------------------------------------------------------------
# TC-27 — the process-group invariant the script relies on (FR-005)
#
# FR-005: "job control gives the child a process group whose ID equals the
# child's PID, so `$!` is simultaneously the child's PID and its process-group
# ID". The script's `child_pgid` helper however accepts whatever group `ps`
# reports, so a child that lands in the script's OWN group is recorded as a
# target the cleanup then refuses to signal — which ends a run with exit `13`
# AFTER the success verdict was already printed.
#
# The property: for EVERY child the script launches, the process group the
# run log records equals the PID the run log records. One row per child.
#
# The run log records the two children as:
#
#   game launched: pid <P>, process group <G>
#   virtual display allocated: :<N> (Xvfb pid <P>, group <G>)
# ---------------------------------------------------------------------------

announce "TC-27" "every child's recorded process group equals the child's PID"

if (( TC27_SHARED_OK == 0 )); then
    skip "TC-27 (the process-group invariant)" "the shared fixture run"
else
    tc27_game_ids="$(run_log_child_ids "$TC27_RUN_LOG_TEXT" game)"
    tc27_xvfb_ids="$(run_log_child_ids "$TC27_RUN_LOG_TEXT" Xvfb)"

    if [[ -z "$tc27_game_ids" ]]; then
        fail "TC-27: the run log records the game child's PID and process group" \
            "no 'game launched: pid <P>, process group <G>' line in the run log"
    else
        tc27_game_pid="${tc27_game_ids%%|*}"
        tc27_game_group="${tc27_game_ids##*|}"
        assert_eq "$tc27_game_pid" "$tc27_game_group" \
            "TC-27: the game's recorded process group equals its PID"
    fi

    if [[ -z "$tc27_xvfb_ids" ]]; then
        fail "TC-27: the run log records the Xvfb child's PID and process group" \
            "no 'virtual display allocated: :<N> (Xvfb pid <P>, group <G>)' line in the run log"
    else
        tc27_xvfb_pid="${tc27_xvfb_ids%%|*}"
        tc27_xvfb_group="${tc27_xvfb_ids##*|}"
        assert_eq "$tc27_xvfb_pid" "$tc27_xvfb_group" \
            "TC-27: the Xvfb's recorded process group equals its PID"
    fi

    # The same invariant, read off the REAL run's log when TC-1 ran. It is the
    # run whose retained log showed the incident, so the invariant is asserted
    # there too.
    if [[ -n "$TC1_RUN_LOG" && -f "$TC1_RUN_LOG" ]]; then
        tc27_real_text="$(cat -- "$TC1_RUN_LOG" 2>/dev/null || true)"
        tc27_real_game_ids="$(run_log_child_ids "$tc27_real_text" game)"
        tc27_real_xvfb_ids="$(run_log_child_ids "$tc27_real_text" Xvfb)"

        if [[ -z "$tc27_real_game_ids" ]]; then
            fail "TC-27: the real run's log records the game child's PID and process group" \
                "no 'game launched: pid <P>, process group <G>' line in the real run's log"
        else
            assert_eq "${tc27_real_game_ids%%|*}" "${tc27_real_game_ids##*|}" \
                "TC-27: the real run's game process group equals its PID"
        fi

        if [[ -z "$tc27_real_xvfb_ids" ]]; then
            fail "TC-27: the real run's log records the Xvfb child's PID and process group" \
                "no 'virtual display allocated: :<N> (Xvfb pid <P>, group <G>)' line in the real run's log"
        else
            assert_eq "${tc27_real_xvfb_ids%%|*}" "${tc27_real_xvfb_ids##*|}" \
                "TC-27: the real run's Xvfb process group equals its PID"
        fi
    fi
fi

# ---------------------------------------------------------------------------
# TC-28 — the deferred save check is labelled `SKIPPED:` (FR-004)
#
# FR-004: "the save check is always skipped (with a `SKIPPED:` warning)", and
# the operational-readiness checklist requires a skipped check to be "clearly
# labeled `SKIPPED:` and counted separately from passes".
#
# The property: the run's output carries a `SKIPPED:` line that names the
# save / mod-data persistence check, the label appears EXACTLY ONCE per run,
# it is on stderr AND in the run log (the run log must hold every line the
# script emits), and it does not change the exit code or the verdict.
# ---------------------------------------------------------------------------

announce "TC-28" "the deferred save check is labelled SKIPPED: and does not fail the run"

if (( TC27_SHARED_OK == 0 )); then
    skip "TC-28 (the SKIPPED: label)" "the shared fixture run"
else
    tc28_skipped_count=0
    tc28_skipped_line=""
    while IFS= read -r line || [[ -n "$line" ]]; do
        [[ "$line" == *"SKIPPED:"* ]] || continue
        tc28_skipped_count=$(( tc28_skipped_count + 1 ))
        [[ -n "$tc28_skipped_line" ]] || tc28_skipped_line="$line"
    done <<< "$TC27_STDERR"

    assert_eq "1" "$tc28_skipped_count" \
        "TC-28: the SKIPPED: label appears exactly once on stderr"

    if [[ -z "$tc28_skipped_line" ]]; then
        fail "TC-28: the SKIPPED: line names the save / mod-data persistence check" \
            "no stderr line carries the 'SKIPPED:' label"
    else
        tc28_skipped_lower="$(printf '%s' "$tc28_skipped_line" | tr '[:upper:]' '[:lower:]')"
        assert_matches "$tc28_skipped_lower" '(save|mod[ -]?data|persist)' \
            "TC-28: the SKIPPED: line names the save / mod-data persistence check"
    fi

    # The run log must hold every line the script emits (FR-014).
    tc28_log_skipped_count=0
    while IFS= read -r line || [[ -n "$line" ]]; do
        [[ "$line" == *"SKIPPED:"* ]] || continue
        tc28_log_skipped_count=$(( tc28_log_skipped_count + 1 ))
    done <<< "$TC27_RUN_LOG_TEXT"

    assert_eq "1" "$tc28_log_skipped_count" \
        "TC-28: the SKIPPED: label appears exactly once in the run log"

    if [[ -n "$tc28_skipped_line" ]]; then
        assert_contains_line "$TC27_RUN_LOG_TEXT" "$tc28_skipped_line" \
            "TC-28: the run log holds the SKIPPED: line the script wrote to stderr"
    fi

    # The skipped check is counted separately from the passes: it neither
    # fails the run nor changes the verdict.
    assert_eq "0" "$TC27_CODE" \
        "TC-28: the skipped save check does not change the exit code"
    assert_contains_line "$TC27_STDOUT" "SUCCESS: Mod loaded without errors." \
        "TC-28: the skipped save check does not change the verdict"
fi

# ---------------------------------------------------------------------------
# TC-29 — the stdout summary prints every phase's duration (FR-014)
#
# FR-014: "the stdout summary MUST print the total wall time AND EACH PHASE'S
# DURATION". Today the per-phase durations reach the RUN LOG only.
#
# The property: the stdout summary and the run log report the SAME set of
# phases with the SAME durations. The phase list is read off the run log
# (which records "phase <name> finished in <N>s" for every phase), and every
# one of them must have a duration line in the stdout summary.
# ---------------------------------------------------------------------------

announce "TC-29" "the stdout summary prints every phase's duration"

if (( TC27_SHARED_OK == 0 )); then
    skip "TC-29 (the stdout phase durations)" "the shared fixture run"
else
    tc29_expected_phases="pre-flight|display|rebuild-and-verify|lock|process-conflict-check|log-baseline|launch|readiness wait|verdict|termination|cleanup"
    tc29_rows="$(run_log_phase_rows "$TC27_RUN_LOG_TEXT")"

    tc29_derived=""
    while IFS='|' read -r tc29_phase tc29_secs; do
        [[ -n "$tc29_phase" ]] || continue
        if [[ -z "$tc29_derived" ]]; then
            tc29_derived="$tc29_phase"
        else
            tc29_derived="${tc29_derived}|${tc29_phase}"
        fi
    done <<< "$tc29_rows"

    assert_eq "$tc29_expected_phases" "$tc29_derived" \
        "TC-29: the run log records every phase of the run"

    while IFS='|' read -r tc29_phase tc29_secs; do
        [[ -n "$tc29_phase" ]] || continue

        tc29_stdout_secs="$(stdout_phase_duration "$TC27_STDOUT" "$tc29_phase")"
        if [[ -z "$tc29_stdout_secs" ]]; then
            fail "TC-29: the stdout summary prints the duration of the '${tc29_phase}' phase" \
                "no line of the stdout summary names '${tc29_phase}' together with a duration"
        else
            pass "TC-29: the stdout summary prints the duration of the '${tc29_phase}' phase"
        fi

        assert_eq "$(normalize_duration "$tc29_secs")" "$(normalize_duration "$tc29_stdout_secs")" \
            "TC-29: the stdout duration of the '${tc29_phase}' phase matches the run log's"
    done <<< "$tc29_rows"
fi

# ---------------------------------------------------------------------------
# TC-30 — the run log holds every line the script emits (FR-014)
#
# FR-014: "The run log MUST contain every line the script emits at any
# level". Today the cleanup's failure report — the block the EXIT trap prints
# when the cleanup cannot finish — reaches stderr only.
#
# The property, as an invariant over the line set: EVERY non-empty line the
# script wrote to stderr is present in that run's run log. One assertion per
# stderr line, so a failure names the line that is missing.
#
# The converse direction is asserted as well: every line of the stdout
# summary is present in the run log (the summary is emitted at the INFO
# level, so it belongs in the log too).
# ---------------------------------------------------------------------------

announce "TC-30" "the run log holds every line the script emits"

if (( TC27_SHARED_OK == 0 )); then
    skip "TC-30 (the run log holds every emitted line)" "the shared fixture run"
else
    tc30_index=0
    while IFS= read -r line || [[ -n "$line" ]]; do
        [[ -n "${line//[[:space:]]/}" ]] || continue
        tc30_index=$(( tc30_index + 1 ))
        assert_contains_line "$TC27_RUN_LOG_TEXT" "$line" \
            "TC-30: the run log holds stderr line ${tc30_index}"
    done <<< "$TC27_STDERR"

    tc30_stdout_missing=0
    tc30_stdout_first_missing=""
    tc30_index=0
    while IFS= read -r line || [[ -n "$line" ]]; do
        [[ -n "${line//[[:space:]]/}" ]] || continue
        tc30_index=$(( tc30_index + 1 ))
        if [[ "$TC27_RUN_LOG_TEXT" != *"$line"* ]]; then
            tc30_stdout_missing=$(( tc30_stdout_missing + 1 ))
            [[ -n "$tc30_stdout_first_missing" ]] || tc30_stdout_first_missing="$line"
        fi
    done <<< "$TC27_STDOUT"

    if (( tc30_stdout_missing > 0 )); then
        fail "TC-30: the run log holds every line the stdout summary printed" \
            "${tc30_stdout_missing} stdout line(s) are missing from the run log — the first is: ${tc30_stdout_first_missing}"
    else
        pass "TC-30: the run log holds every line the stdout summary printed"
    fi
fi

# ---------------------------------------------------------------------------
# TC-31 — every signal is logged with its PID and its signal name (FR-014)
#
# FR-014: the script "MUST log every process lifecycle event with a
# timestamp and level prefix — spawn (game, Xvfb), every signal sent
# (INCLUDING THE PID AND THE SIGNAL), the grace period, the escalation to
# SIGKILL, and the observed exit".
#
# The current log says "terminating the game process group <G>" — the signal
# NAME is missing, and so is the PID.
#
# The property, as a table over the lifecycle events: every row must name the
# target AND the thing that happened.
#
#   row        | the run log must contain a line that names …
#   -----------+------------------------------------------------------------
#   SIGTERM    | the signal name and the game's process group
#   grace      | the grace period applied to the game's process group
#   SIGKILL    | the escalation and the game's process group
#
# The escalation row is exercised by the shared run's child, which ignores
# SIGTERM: the grace period expires and the script escalates to SIGKILL.
# ---------------------------------------------------------------------------

announce "TC-31" "every signal is logged with the signal name and the target"

if (( TC27_SHARED_OK == 0 )); then
    skip "TC-31 (the signal lifecycle events)" "the shared fixture run"
else
    tc31_game_ids="$(run_log_child_ids "$TC27_RUN_LOG_TEXT" game)"
    tc31_game_pid="${tc31_game_ids%%|*}"
    tc31_game_group="${tc31_game_ids##*|}"

    if [[ -z "$tc31_game_group" ]]; then
        fail "TC-31: the run log names the game's process group" \
            "no 'game launched: pid <P>, process group <G>' line in the run log"
    else
        tc31_sigterm_line="$(run_log_line_with_ids "$TC27_RUN_LOG_TEXT" "SIGTERM" "$tc31_game_group")"
        if [[ -z "$tc31_sigterm_line" ]]; then
            fail "TC-31: the run log names the SIGTERM signal and the game's process group" \
                "no line names both 'SIGTERM' and the game process group '${tc31_game_group}' — the signal name and the target must be on the same line"
        else
            pass "TC-31: the run log names the SIGTERM signal and the game's process group"
        fi

        if [[ -n "$tc31_sigterm_line" ]]; then
            assert_contains_id "$tc31_sigterm_line" "$tc31_game_pid" \
                "TC-31: the SIGTERM line names the game's PID"
        fi

        tc31_grace_line="$(run_log_group_grace_line "$TC27_RUN_LOG_TEXT" "$tc31_game_group")"
        if [[ -z "$tc31_grace_line" ]]; then
            fail "TC-31: the run log names the grace period applied to the game" \
                "no line names both the grace period and the game process group '${tc31_game_group}'"
        else
            pass "TC-31: the run log names the grace period applied to the game"
        fi

        tc31_sigkill_line="$(run_log_line_with_ids "$TC27_RUN_LOG_TEXT" "SIGKILL" "$tc31_game_group")"
        if [[ -z "$tc31_sigkill_line" ]]; then
            fail "TC-31: the run log names the SIGKILL escalation and the game's process group" \
                "no line names both 'SIGKILL' and the game process group '${tc31_game_group}' — the escalation and its target must be on the same line"
        else
            pass "TC-31: the run log names the SIGKILL escalation and the game's process group"
        fi

        # The observed exit is part of the same contract (FR-014).
        assert_contains "$TC27_RUN_LOG_TEXT" "the game exited with status" \
            "TC-31: the run log records the observed exit of the game"
    fi
fi

# ---------------------------------------------------------------------------
# TC-32 — the failure verdict is decided only after the cleanup (FR-006, FR-010)
#
# FR-006 reports success when the log check passes; FR-010's cleanup runs
# afterwards in the EXIT trap. Today the verdict is printed BEFORE the
# cleanup, so a cleanup failure changes the exit code AFTER
# "SUCCESS: Mod loaded without errors." was already on stdout.
#
# The properties:
#
#   1. the stdout verdict and the exit code always agree — a non-zero exit
#      code means stdout does NOT carry the success verdict, and stdout
#      carrying the success verdict means the exit code IS 0;
#   2. the verdict is printed only after the cleanup — the run log records
#      the cleanup's completion BEFORE the success verdict, and no cleanup
#      event follows the verdict.
#
# The first property is asserted over TWO runs: the successful shared run and
# a failing one, so it is not vacuous.
# ---------------------------------------------------------------------------

announce "TC-32" "the stdout verdict and the exit code always agree, and the verdict comes last"

if (( TC27_SHARED_OK == 0 )); then
    skip "TC-32 (the verdict and the exit code agree)" "the shared fixture run"
else
    # --- property 1, over the successful run ---
    if [[ "$TC27_CODE" == "0" ]]; then
        assert_contains_line "$TC27_STDOUT" "SUCCESS: Mod loaded without errors." \
            "TC-32: an exit code of 0 carries the success verdict on stdout"
    else
        assert_not_contains "$TC27_STDOUT" "SUCCESS: Mod loaded without errors." \
            "TC-32: a non-zero exit code does NOT carry the success verdict on stdout"
    fi

    if [[ "$TC27_STDOUT" == *"SUCCESS: Mod loaded without errors."* ]]; then
        assert_eq "0" "$TC27_CODE" \
            "TC-32: the success verdict on stdout implies an exit code of 0"
    else
        pass "TC-32: the success verdict on stdout implies an exit code of 0 (the run carries no success verdict)"
    fi

    # --- property 1, over a failing run (launch-free: a missing game) ---
    tc32_missing="${FIXTURE_ROOT}/tc32-no-such-game"
    run_script_bounded 60 \
        -- \
        --game-path "$tc32_missing" \
        --smapi-log "${FIXTURE_ROOT}/tc32/SMAPI-latest.txt"

    tc32_fail_code="$RUN_CODE"
    tc32_fail_stdout="$RUN_STDOUT"

    assert_ne "0" "$tc32_fail_code" \
        "TC-32: the failing run exits with a non-zero code"
    assert_not_contains "$tc32_fail_stdout" "SUCCESS: Mod loaded without errors." \
        "TC-32: the failing run does NOT carry the success verdict on stdout"

    # --- property 2: the verdict is printed only after the cleanup ---
    tc32_verdict_line="$(first_line_number_with "$TC27_RUN_LOG_TEXT" "SUCCESS: Mod loaded without errors.")"
    tc32_cleanup_line="$(first_line_number_with "$TC27_RUN_LOG_TEXT" "phase cleanup finished in")"

    if [[ -z "$tc32_cleanup_line" ]]; then
        fail "TC-32: the run log records the cleanup's completion" \
            "no 'phase cleanup finished in <N>s' line in the run log"
    else
        pass "TC-32: the run log records the cleanup's completion"

        if [[ -z "$tc32_verdict_line" ]]; then
            fail "TC-32: the run log records the success verdict" \
                "no 'SUCCESS: Mod loaded without errors.' line in the run log"
        else
            if (( tc32_cleanup_line < tc32_verdict_line )); then
                pass "TC-32: the cleanup completed BEFORE the verdict was printed"
            else
                fail "TC-32: the cleanup completed BEFORE the verdict was printed" \
                    "the cleanup's completion is on line ${tc32_cleanup_line} and the verdict on line ${tc32_verdict_line} — the verdict must be decided only after the cleanup"
            fi
        fi
    fi

    # No cleanup event may follow the verdict: a later failure must not be
    # able to contradict what the script already reported.
    #
    # The cleanup's LIFECYCLE events are the ones the cleanup itself logs
    # ("cleanup starting" and "phase cleanup finished in <N>s"). The phase
    # name alone is not a lifecycle event — the phase-duration block names
    # "cleanup" too, and it legitimately follows the verdict.
    tc32_last_cleanup_line="$(last_line_number_with "$TC27_RUN_LOG_TEXT" "cleanup starting")"
    tc32_last_cleanup_end="$(last_line_number_with "$TC27_RUN_LOG_TEXT" "phase cleanup finished in")"
    if [[ -n "$tc32_last_cleanup_end" ]] \
        && [[ -z "$tc32_last_cleanup_line" || "$tc32_last_cleanup_end" -gt "$tc32_last_cleanup_line" ]]; then
        tc32_last_cleanup_line="$tc32_last_cleanup_end"
    fi

    if [[ -z "$tc32_verdict_line" ]]; then
        pass "TC-32: no cleanup event follows the verdict (the run log records no verdict)"
    elif [[ -z "$tc32_last_cleanup_line" ]]; then
        pass "TC-32: no cleanup event follows the verdict (the run log records no cleanup event)"
    elif (( tc32_last_cleanup_line < tc32_verdict_line )); then
        pass "TC-32: no cleanup event follows the verdict"
    else
        fail "TC-32: no cleanup event follows the verdict" \
            "the last cleanup event is on line ${tc32_last_cleanup_line} and the verdict on line ${tc32_verdict_line}"
    fi
fi

# ---------------------------------------------------------------------------
# TC-33 — a containment failure produces EXACTLY ONE report (FR-007)
#
# The script's own `die` contract says: "never call this from a command
# substitution — `exit` inside `$(...)` only terminates the subshell". The
# path-containment helper violates it, so a containment failure produces the
# primary report AND a second, contradictory ERR-trap report.
#
# The property: a failing run's stderr contains EXACTLY ONE occurrence of the
# failure-report marker ("ERROR: smoke validation failed"). One assertion over
# the report markers.
#
# The fixture: the game directory's own `smapi-internal/` entry holds a
# crash-marker SYMLINK that points OUTSIDE the game directory, so the path the
# script CONSTRUCTS escapes its base and the containment test fails. The
# pre-flight aborts there — before the display and before the build — so the
# case is launch-free.
# ---------------------------------------------------------------------------

announce "TC-33" "a containment failure produces exactly ONE failure report"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-33 (the containment failure report)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-33 (the containment failure report)" "the suite time budget"
else
    tc33_game="${FIXTURE_ROOT}/tc33/game"
    tc33_outside="${FIXTURE_ROOT}/tc33/outside"
    make_fake_game_dir "$tc33_game" 2>/dev/null || true
    mkdir -p -- "$tc33_outside" 2>/dev/null || true
    printf 'a leftover SMAPI marker\n' > "${tc33_outside}/crash.marker" 2>/dev/null || true
    # The escaping marker: the constructed path resolves outside the game
    # directory, so the containment test fails.
    rm -f -- "${tc33_game}/smapi-internal/StardewModdingAPI.crash.marker" 2>/dev/null || true
    ln -sfn -- "${tc33_outside}/crash.marker" \
        "${tc33_game}/smapi-internal/StardewModdingAPI.crash.marker" 2>/dev/null || true

    tc33_snapshot="${FIXTURE_ROOT}/tc33-run-log-snapshot.txt"
    snapshot_run_logs "$tc33_snapshot"

    run_script_bounded 120 \
        -- \
        --game-path "$tc33_game" \
        --smapi-log "${FIXTURE_ROOT}/tc33/SMAPI-latest.txt" \
        --timeout 5 \
        --grace-period 2 \
        --build-timeout 5

    tc33_reports="$(count_occurrences "$RUN_STDERR" "ERROR: smoke validation failed")"

    assert_ne "0" "$RUN_CODE" \
        "TC-33: the containment failure fails the run"
    assert_eq "1" "$tc33_reports" \
        "TC-33: the failing run's stderr carries EXACTLY ONE failure report"

    # The run log holds the same reports, so the same invariant applies there.
    # The log of THIS run, never the newest log in the sandbox.
    tc33_run_log="$(resolve_run_log "$tc33_snapshot" "$RUN_STDOUT")"
    if [[ -z "$tc33_run_log" ]]; then
        fail "TC-33: the containment failure wrote a run log" \
            "this run reported no run id and wrote no new run log under '${RUN_LOGS_DIR}/'"
    else
        tc33_run_log_text="$(cat -- "$tc33_run_log" 2>/dev/null || true)"
        assert_eq "1" "$(count_occurrences "$tc33_run_log_text" "ERROR: smoke validation failed")" \
            "TC-33: the run log carries EXACTLY ONE failure report"
    fi
fi

# ---------------------------------------------------------------------------
# TC-34 — the timeout is measured from the LAUNCH, not from the script's
# start (FR-009, SC-001)
#
# FR-009: "The timeout MUST be measured from the instant the game process is
# launched — the timestamp recorded at launch, not the script's start time —
# and it MUST NOT count the pre-flight or the rebuild-and-verify phase
# against this budget."
#
# The case uses a --timeout SHORTER than the rebuild, so an implementation
# that anchored the deadline at the script's start would have burned the
# whole budget before the launch. The properties:
#
#   1. the run exits 4 (the timeout's own code);
#   2. the game WAS launched — the deadline did not stop the run before the
#      launch;
#   3. the run waited for the timeout AFTER the launch — the run log's own
#      timestamps show the gap between the launch and the end of the run.
# ---------------------------------------------------------------------------

announce "TC-34" "a --timeout shorter than the rebuild still launches the game"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-34 (the timeout is anchored at the launch)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-34 (the timeout is anchored at the launch)" "the suite time budget"
else
    tc34_game="$(fixture_game tc34 || true)"
    tc34_log="${FIXTURE_ROOT}/tc34/SMAPI-latest.txt"
    tc34_marker="${FIXTURE_ROOT}/tc34/launched.marker"
    tc34_project="$(fixture_project tc34 FixtureMod || true)"
    register_stub "$tc34_marker"

    tc34_snapshot="${FIXTURE_ROOT}/tc34-run-log-snapshot.txt"
    snapshot_run_logs "$tc34_snapshot"

    # A --timeout of 5 s, and a rebuild that takes longer than that.
    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_MARKER=${tc34_marker}" \
        "SMOKE_STUB_SLEEP=300" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc34_game" \
        --mod-project "$tc34_project" \
        --smapi-log "$tc34_log" \
        --timeout 5

    # The marker is read BEFORE kill_stub: kill_stub removes it.
    tc34_launched=0
    if [[ -f "$tc34_marker" ]]; then
        tc34_launched=1
    fi
    kill_stub "$tc34_marker"

    assert_exit 4 "TC-34: a --timeout shorter than the rebuild exits 4"
    assert_eq "1" "$tc34_launched" \
        "TC-34: the game was launched before the timeout fired"

    # The run log's own timestamps: the gap between the launch and the end
    # of the run must be about the timeout. A deadline anchored at the
    # script's start would leave no time to wait at all: the run would end
    # in the same second as the launch.
    tc34_run_log="$(resolve_run_log "$tc34_snapshot" "$RUN_STDOUT")"
    if [[ -z "$tc34_run_log" ]]; then
        fail "TC-34: the timeout was measured from the launch" \
            "the run wrote no run log, so the launch instant cannot be reconstructed"
    else
        tc34_log_text="$(cat -- "$tc34_run_log" 2>/dev/null || true)"
        tc34_launch_line="$(first_line_containing "$tc34_log_text" "game launched:")"
        tc34_launch_epoch="$(line_epoch "$tc34_launch_line")"
        tc34_end_line="$(last_timestamped_line "$tc34_log_text")"
        tc34_end_epoch="$(line_epoch "$tc34_end_line")"

        if [[ -z "$tc34_launch_epoch" || -z "$tc34_end_epoch" ]]; then
            fail "TC-34: the timeout was measured from the launch" \
                "the run log carries no timestamped launch record"
        else
            tc34_waited=$(( tc34_end_epoch - tc34_launch_epoch ))
            if (( tc34_waited >= 3 )); then
                pass "TC-34: the timeout was measured from the launch (the run waited ${tc34_waited}s after it)"
            else
                fail "TC-34: the timeout was measured from the launch" \
                    "the run ended ${tc34_waited}s after the launch — the deadline was consumed by the rebuild, so it was anchored at the script's start"
            fi
        fi
    fi
fi

# ---------------------------------------------------------------------------
# TC-35, TC-36, TC-41, TC-43 — the shared fixture run
#
# ONE successful fixture run carries four properties, the way TC-27's run is
# shared by TC-27 … TC-32:
#
#   TC-35  the allocated display's socket is gone and the run's temporary
#          directory does not survive the run (FR-010, SC-010)
#   TC-36  the run log's mode, its directory modes, the run-id form, the
#          VERSION, the per-line stamping and the stdout/stderr split
#          (FR-014, SC-014)
#   TC-41  the run log carries the launch epoch the sentinel-freshness
#          comparison used, and the header's `launch timestamp` is not the
#          run-start instant (FR-002, FR-014)
#   TC-43  the script's children inherit the PINNED NuGet cache and the
#          DOTNET_* settings, so the rebuild restores from the cache and
#          not from nuget.org (suite)
#
# TC-36 and TC-41 used to be observed on the REAL installation only, so
# they were skipped on every host without the game.
# ---------------------------------------------------------------------------

TC36_RUN_LOG=""
TC36_RUN_ID=""
TC36_RUN_LOG_TEXT=""
TC36_STDOUT=""
TC36_STDERR=""
TC36_SHARED_OK=0

announce "TC-35, TC-36, TC-41, TC-43" "the shared fixture run: a successful run against a throwaway project"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "the shared fixture run (TC-35, TC-36, TC-41, TC-43)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "the shared fixture run (TC-35, TC-36, TC-41, TC-43)" "the suite time budget"
else
    tc36_game="$(fixture_game tc36 || true)"
    tc36_log="${FIXTURE_ROOT}/tc36/SMAPI-latest.txt"
    tc36_marker="${FIXTURE_ROOT}/tc36/launched.marker"
    tc36_env_dump="${FIXTURE_ROOT}/tc36/env-dump.txt"
    tc36_project="$(fixture_project tc36 FixtureMod || true)"
    register_stub "$tc36_marker"

    stub_log "$tc36_log" mod_loaded

    # The script's VERSION, read from --version — the same source the
    # run-log header and the stdout summary carry.
    run_script --version
    tc36_version="$(printf '%s\n' "$RUN_STDOUT" | head -n 1 | awk '{print $NF}' || true)"

    tc36_snapshot="${FIXTURE_ROOT}/tc36-run-log-snapshot.txt"
    snapshot_run_logs "$tc36_snapshot"

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_LOG=${tc36_log}" \
        "SMOKE_STUB_LOG_SRC=$(stub_template_for "${tc36_log}")" \
        "SMOKE_STUB_MARKER=${tc36_marker}" \
        "SMOKE_STUB_ENV_DUMP=${tc36_env_dump}" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc36_game" \
        --mod-project "$tc36_project" \
        --smapi-log "$tc36_log" \
        --timeout 60

    kill_stub "$tc36_marker"

    TC36_STDOUT="$RUN_STDOUT"
    TC36_STDERR="$RUN_STDERR"
    TC36_RUN_LOG="$(resolve_run_log "$tc36_snapshot" "$RUN_STDOUT")"
    if [[ -z "$TC36_RUN_LOG" ]]; then
        fail "the shared fixture run wrote a run log" \
            "this run reported no run id and wrote no new run log under '${RUN_LOGS_DIR}/'"
    else
        TC36_RUN_LOG_TEXT="$(cat -- "$TC36_RUN_LOG" 2>/dev/null || true)"
        TC36_RUN_ID="$(basename -- "$TC36_RUN_LOG")"
        TC36_RUN_ID="${TC36_RUN_ID%.log}"
        TC36_SHARED_OK=1
        pass "the shared fixture run wrote a run log"
    fi
fi

# ---------------------------------------------------------------------------
# TC-35 — the cleanup removed the display's socket and the temporary
# directory (FR-010, SC-010)
#
# FR-010: the cleanup removes "the virtual display's socket
# (/tmp/.X11-unix/X{n}) — but ONLY for the display this script allocated" and
# "temp files/directories".
#
# The properties are OBSERVED, not inferred from the implementation: the
# socket of the display the run log names must be gone, and no temporary
# directory the run created may survive it.
# ---------------------------------------------------------------------------

announce "TC-35" "the allocated display's socket and the run's temporary directory are gone"

if (( TC36_SHARED_OK == 0 )); then
    skip "TC-35 (the cleanup's socket and temp-dir removals)" "the shared fixture run"
else
    tc35_display_line="$(first_line_containing "$TC36_RUN_LOG_TEXT" "virtual display allocated:")"
    tc35_display="$(printf '%s' "$tc35_display_line" \
        | sed -n 's/.*virtual display allocated: :\([0-9][0-9]*\).*/\1/p' || true)"

    if [[ -z "$tc35_display" ]]; then
        fail "TC-35: the run log records the display the run allocated" \
            "no 'virtual display allocated: :<N>' line in the run log"
    else
        pass "TC-35: the run log records the display the run allocated"

        if [[ ! -e "/tmp/.X11-unix/X${tc35_display}" ]] \
            && [[ ! -e "${HARNESS_XDG_RUNTIME_DIR}/X${tc35_display}" ]]; then
            pass "TC-35: the allocated display's socket is gone"
        else
            fail "TC-35: the allocated display's socket is gone" \
                "the socket of the display :${tc35_display} still exists"
        fi
    fi

    # The run's OWN temporary directory, identified by the display number it
    # holds: the script writes the number the X server reported into
    # `<temp-dir>/display-number`. Identifying the directory that way — and
    # not by "any directory that appeared" — keeps the assertion honest when
    # another suite runs the script on the same host at the same time.
    tc35_surviving=""
    for tc35_tmp in "${TMPDIR:-/tmp}"/livingroots-smoke.*; do
        [[ -d "$tc35_tmp" ]] || continue
        [[ -n "$tc35_display" ]] || continue
        [[ -f "${tc35_tmp}/display-number" ]] || continue
        if tr -d '[:space:]' < "${tc35_tmp}/display-number" 2>/dev/null \
            | grep -qx "$tc35_display"; then
            tc35_surviving="$tc35_tmp"
            break
        fi
    done

    if [[ -z "$tc35_surviving" ]]; then
        pass "TC-35: the run's temporary directory does not survive the run"
    else
        fail "TC-35: the run's temporary directory does not survive the run" \
            "the temporary directory ${tc35_surviving} (the one that held the display number ${tc35_display}) survived the run"
    fi
fi

# ---------------------------------------------------------------------------
# TC-36 — the run log's own properties, on a FIXTURE run (FR-014, SC-014)
#
# SC-014: "Every run produces a retained run log under
# ${XDG_STATE_HOME:-$HOME/.local/state}/living-roots-smoke/runs/ containing
# the run ID, the script VERSION, the launch timestamp, and every lifecycle
# event, such that the run can be reconstructed from that file alone."
#
# The properties: the log's MODE (0600), the modes of BOTH directory levels
# (0700), the run-id FORM, the VERSION, the per-line stamping with the run
# id, and the stdout/stderr split.
# ---------------------------------------------------------------------------

announce "TC-36" "the run log's mode, its directory modes, the run id, the version and the stream split"

if (( TC36_SHARED_OK == 0 )); then
    skip "TC-36 (the run log's properties)" "the shared fixture run"
else
    assert_file_exists "$TC36_RUN_LOG" \
        "TC-36: the run log exists at the FR-014 path"
    assert_contains "$TC36_RUN_LOG" "${HARNESS_XDG_STATE_HOME}/living-roots-smoke/runs/" \
        "TC-36: the run log lives under the XDG state base"
    assert_file_mode "$TC36_RUN_LOG" 600 \
        "TC-36: the run log has mode 0600"
    assert_dir_mode "${HARNESS_XDG_STATE_HOME}/living-roots-smoke" 700 \
        "TC-36: the living-roots-smoke directory has mode 0700"
    assert_dir_mode "$RUN_LOGS_DIR" 700 \
        "TC-36: the runs directory has mode 0700"

    assert_matches "$TC36_RUN_ID" '^[0-9]{8}T[0-9]{6}Z-[0-9]+$' \
        "TC-36: the run id has the <UTC timestamp>-<PID> form"
    assert_contains "$TC36_RUN_LOG" "$TC36_RUN_ID" \
        "TC-36: the run log contains the run id"

    if [[ -z "$tc36_version" ]]; then
        fail "TC-36: the run log contains the script VERSION" \
            "--version printed no parsable version"
    else
        assert_contains "$TC36_RUN_LOG_TEXT" "$tc36_version" \
            "TC-36: the run log contains the script VERSION"
        assert_contains "$TC36_STDOUT" "$tc36_version" \
            "TC-36: the stdout summary contains the script VERSION"
    fi

    assert_contains "$TC36_STDOUT" "$TC36_RUN_ID" \
        "TC-36: the stdout summary carries the run id"
    assert_contains "$TC36_STDOUT" "$TC36_RUN_LOG" \
        "TC-36: the stdout summary carries the run-log path"

    assert_eq "0" "$(run_log_lines_unstamped "$TC36_RUN_LOG" "$TC36_RUN_ID")" \
        "TC-36: every log line is stamped with the run id"

    # The verdict is on stdout, the diagnostics on stderr.
    assert_contains_line "$TC36_STDOUT" "SUCCESS: Mod loaded without errors." \
        "TC-36: the verdict is on stdout"
    assert_not_contains "$TC36_STDOUT" "ERROR:" \
        "TC-36: the diagnostics are not on stdout"
    assert_not_contains "$TC36_STDERR" "SUCCESS: Mod loaded without errors." \
        "TC-36: the verdict is not on stderr"
fi

# ---------------------------------------------------------------------------
# TC-41 — the run log carries the launch epoch (FR-002, FR-014, SC-014)
#
# FR-002: the sentinel's freshness is judged against "the timestamp recorded
# at launch". FR-014: "a failed run can be reconstructed from the run log
# alone".
#
# The properties:
#
#   1. the run log records a launch-labelled instant that is LATER than the
#      run's start — the launch epoch is a distinct moment;
#   2. the header's field labelled `launch timestamp` is NOT the run-start
#      instant — the run id's own stamp is the run's start, so the two can
#      be compared without any other record.
# ---------------------------------------------------------------------------

announce "TC-41" "the run log carries the launch epoch the freshness comparison used"

if (( TC36_SHARED_OK == 0 )); then
    skip "TC-41 (the launch epoch in the run log)" "the shared fixture run"
else
    tc41_run_start="$(run_id_epoch "$TC36_RUN_ID")"

    if [[ -z "$tc41_run_start" ]]; then
        fail "TC-41: the run's start instant is recoverable from the run id" \
            "the run id '${TC36_RUN_ID}' carries no parsable UTC stamp"
    else
        tc41_launch_epoch=""
        while IFS= read -r tc41_line || [[ -n "$tc41_line" ]]; do
            case "${tc41_line,,}" in
                *launch*) ;;
                *) continue ;;
            esac
            tc41_line_epoch="$(line_epoch "$tc41_line")"
            [[ -n "$tc41_line_epoch" ]] || continue
            if (( tc41_line_epoch > tc41_run_start )); then
                tc41_launch_epoch="$tc41_line_epoch"
                break
            fi
        done <<< "$TC36_RUN_LOG_TEXT"

        if [[ -n "$tc41_launch_epoch" ]]; then
            pass "TC-41: the run log records a launch instant later than the run's start"
        else
            fail "TC-41: the run log records a launch instant later than the run's start" \
                "no launch-labelled record in the run log carries a timestamp later than the run's start instant"
        fi

        # The header's `launch timestamp` field: the run's START instant
        # would make the field a lie — the launch happens later.
        tc41_header_line="$(first_line_containing "$TC36_RUN_LOG_TEXT" "launch timestamp:")"
        tc41_header_value=""
        if [[ -n "$tc41_header_line" ]]; then
            tc41_header_value="${tc41_header_line##*launch timestamp:}"
            tc41_header_value="${tc41_header_value# }"
        fi
        tc41_header_epoch="$(iso_epoch "$tc41_header_value")"

        if [[ -z "$tc41_header_epoch" ]]; then
            fail "TC-41: the header's launch timestamp is not the run-start instant" \
                "the run log carries no parsable 'launch timestamp:' field"
        elif [[ "$tc41_header_epoch" == "$tc41_run_start" ]]; then
            fail "TC-41: the header's launch timestamp is not the run-start instant" \
                "the header's launch timestamp (${tc41_header_value}) IS the run's start instant — the launch epoch is a later moment"
        else
            pass "TC-41: the header's launch timestamp is not the run-start instant"
        fi
    fi
fi

# ---------------------------------------------------------------------------
# TC-43 — the rebuild restores from the PINNED cache (suite)
#
# The suite's own `run_script_bounded` hands the script under test the
# environment the rebuild needs: the NuGet cache is pinned to the real one
# and the .NET first-run ceremony is switched off. Without that, every
# fixture case that reaches the rebuild would restore from nuget.org.
#
# The property is OBSERVED on the script's own child (the apphost stub is a
# grandchild of the script, so it inherits the script's environment).
# ---------------------------------------------------------------------------

announce "TC-43" "the script's children inherit the pinned NuGet cache and the DOTNET_* settings"

if (( TC36_SHARED_OK == 0 )); then
    skip "TC-43 (the pinned NuGet cache)" "the shared fixture run"
elif [[ ! -f "$tc36_env_dump" ]]; then
    fail "TC-43: the script's child recorded its environment" \
        "no environment dump at '${tc36_env_dump}' — the game was never launched"
else
    pass "TC-43: the script's child recorded its environment"
    tc43_dump="$(cat -- "$tc36_env_dump" 2>/dev/null || true)"

    if [[ -d "${HARNESS_ORIGINAL_HOME}/.nuget/packages" ]]; then
        assert_contains "$tc43_dump" "NUGET_PACKAGES=${HARNESS_ORIGINAL_HOME}/.nuget/packages" \
            "TC-43: the rebuild's environment carries the PINNED NuGet cache"
    else
        skip "TC-43: the rebuild's environment carries the PINNED NuGet cache" \
            "the NuGet cache at ~/.nuget/packages"
    fi
    assert_contains "$tc43_dump" "DOTNET_CLI_TELEMETRY_OPTOUT=1" \
        "TC-43: the rebuild's environment carries DOTNET_CLI_TELEMETRY_OPTOUT=1"
    assert_contains "$tc43_dump" "DOTNET_NOLOGO=1" \
        "TC-43: the rebuild's environment carries DOTNET_NOLOGO=1"
    assert_contains "$tc43_dump" "DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1" \
        "TC-43: the rebuild's environment carries DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1"
fi

# ---------------------------------------------------------------------------
# TC-37 — idempotency on a FIXTURE run (FR-008, SC-015)
#
# SC-015: "executed twice in sequence from the same starting state, it
# produces the same exit code and the same verdict". The property used to be
# observed on the real installation only.
# ---------------------------------------------------------------------------

announce "TC-37" "two consecutive fixture runs produce the same code and the same verdict"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-37 (the fixture idempotency)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-37 (the fixture idempotency)" "the suite time budget"
else
    tc37_game="$(fixture_game tc37 || true)"
    tc37_log="${FIXTURE_ROOT}/tc37/SMAPI-latest.txt"
    tc37_marker="${FIXTURE_ROOT}/tc37/launched.marker"
    tc37_project="$(fixture_project tc37 FixtureMod || true)"
    register_stub "$tc37_marker"

    stub_log "$tc37_log" mod_loaded

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_LOG=${tc37_log}" \
        "SMOKE_STUB_LOG_SRC=$(stub_template_for "${tc37_log}")" \
        "SMOKE_STUB_MARKER=${tc37_marker}" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc37_game" \
        --mod-project "$tc37_project" \
        --smapi-log "$tc37_log" \
        --timeout 60

    tc37_code_1="$RUN_CODE"
    tc37_verdict_1="$(printf '%s\n' "$RUN_STDOUT" | grep -E '^(SUCCESS|FAILURE)' | head -n 1 || true)"
    kill_stub "$tc37_marker"

    rm -f -- "$tc37_marker"
    stub_log "$tc37_log" mod_loaded

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_LOG=${tc37_log}" \
        "SMOKE_STUB_LOG_SRC=$(stub_template_for "${tc37_log}")" \
        "SMOKE_STUB_MARKER=${tc37_marker}" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc37_game" \
        --mod-project "$tc37_project" \
        --smapi-log "$tc37_log" \
        --timeout 60

    tc37_code_2="$RUN_CODE"
    tc37_verdict_2="$(printf '%s\n' "$RUN_STDOUT" | grep -E '^(SUCCESS|FAILURE)' | head -n 1 || true)"
    kill_stub "$tc37_marker"

    assert_eq "$tc37_code_1" "$tc37_code_2" \
        "TC-37: both runs exit with the same code"
    assert_eq "$tc37_verdict_1" "$tc37_verdict_2" \
        "TC-37: both runs print the same verdict"
    assert_ne "" "$tc37_verdict_1" \
        "TC-37: the runs print a verdict line"
    assert_ne "6" "$tc37_code_2" \
        "TC-37: the second run re-acquires the lock"
fi

# ---------------------------------------------------------------------------
# TC-38 — the deploy is verified against the folder ModBuildConfig really
# deploys into (FR-016 (b) (d), SC-012)
#
# The fixture project declares a <ModFolderName> that DIFFERS from the
# project file's base name, so the folder the build deploys into and the
# folder the script derives from the project file's NAME are two different
# directories. The properties:
#
#   1. the run succeeds — the deploy-effectiveness assertion is made
#      against the folder the build really used;
#   2. a stale file planted in THAT folder does not survive — the removal
#      targeted it;
#   3. the run log names THAT folder as the deploy target it removed.
# ---------------------------------------------------------------------------

announce "TC-38" "the deploy is removed, digest-compared and reported against the declared <ModFolderName>"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-38 (the declared <ModFolderName>)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-38 (the declared <ModFolderName>)" "the suite time budget"
else
    tc38_game="$(fixture_game tc38 || true)"
    tc38_log="${FIXTURE_ROOT}/tc38/SMAPI-latest.txt"
    tc38_marker="${FIXTURE_ROOT}/tc38/launched.marker"
    # The declared folder name DIFFERS from the project file's base name
    # ("FixtureMod"), exactly like a mod whose <ModFolderName> was chosen.
    tc38_project="$(fixture_project tc38 FixtureMod DifferentFolderName || true)"
    tc38_target="${tc38_game}/Mods/DifferentFolderName"
    register_stub "$tc38_marker"

    stub_log "$tc38_log" mod_loaded

    # A stale file from a PREVIOUS build, in the folder the build deploys
    # into. ModBuildConfig copies but never deletes, so only the removal
    # can make it disappear.
    mkdir -p -- "$tc38_target" 2>/dev/null || true
    printf 'a stale deployment payload\n' > "${tc38_target}/stale-from-a-previous-build.dll" 2>/dev/null || true

    tc38_snapshot="${FIXTURE_ROOT}/tc38-run-log-snapshot.txt"
    snapshot_run_logs "$tc38_snapshot"

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_LOG=${tc38_log}" \
        "SMOKE_STUB_LOG_SRC=$(stub_template_for "${tc38_log}")" \
        "SMOKE_STUB_MARKER=${tc38_marker}" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc38_game" \
        --mod-project "$tc38_project" \
        --smapi-log "$tc38_log" \
        --timeout 60

    kill_stub "$tc38_marker"

    assert_exit 0 "TC-38: the run against the declared <ModFolderName> succeeds"
    assert_file_absent "${tc38_target}/stale-from-a-previous-build.dll" \
        "TC-38: the deploy target the build really uses is the one that was removed"

    tc38_run_log="$(resolve_run_log "$tc38_snapshot" "$RUN_STDOUT")"
    if [[ -z "$tc38_run_log" ]]; then
        fail "TC-38: the run log names the deploy target that was removed" \
            "the run wrote no run log"
    else
        tc38_log_text="$(cat -- "$tc38_run_log" 2>/dev/null || true)"
        if printf '%s\n' "$tc38_log_text" \
            | grep -qE 'removing the deploy target: .*/Mods/DifferentFolderName( |$)'; then
            pass "TC-38: the run log names the deploy target that was removed"
        else
            fail "TC-38: the run log names the deploy target that was removed" \
                "no 'removing the deploy target: .../Mods/DifferentFolderName' line in the run log"
        fi
        assert_contains "$tc38_log_text" "deploy verified: sha256" \
            "TC-38: the run log records the deploy's digest verification"
    fi
fi

# ---------------------------------------------------------------------------
# TC-39 — the phase in which the run failed is reported (FR-014, SC-001)
#
# FR-014: "The script MUST log every process lifecycle event with a
# timestamp and level prefix ... so that a failed run can be reconstructed
# from the run log alone", and the stdout summary MUST print "EACH PHASE'S
# DURATION".
#
# The properties, for the two phases that matter most:
#
#   1. the READINESS-WAIT timeout: the phase the run failed in appears in
#      the stdout `phase durations:` block AND in the run log, with the
#      phase's end timestamp and its elapsed seconds;
#   2. the VERDICT phase: the same, for a run that failed in the gate.
# ---------------------------------------------------------------------------

announce "TC-39" "the phase in which the run failed appears in the durations and in the run log"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-39 (the failed phase is reported)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-39 (the failed phase is reported)" "the suite time budget"
else
    # --- the readiness-wait timeout ---
    tc39a_game="$(fixture_game tc39a || true)"
    tc39a_log="${FIXTURE_ROOT}/tc39a/SMAPI-latest.txt"
    tc39a_marker="${FIXTURE_ROOT}/tc39a/launched.marker"
    tc39a_project="$(fixture_project tc39a FixtureMod || true)"
    register_stub "$tc39a_marker"

    tc39a_snapshot="${FIXTURE_ROOT}/tc39a-run-log-snapshot.txt"
    snapshot_run_logs "$tc39a_snapshot"

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_MARKER=${tc39a_marker}" \
        "SMOKE_STUB_SLEEP=300" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc39a_game" \
        --mod-project "$tc39a_project" \
        --smapi-log "$tc39a_log" \
        --timeout 5

    kill_stub "$tc39a_marker"

    assert_exit 4 "TC-39: the readiness-wait timeout exits 4"

    tc39a_stdout_secs="$(stdout_phase_duration "$RUN_STDOUT" "readiness wait")"
    if [[ -z "$tc39a_stdout_secs" ]]; then
        fail "TC-39: the stdout durations report the phase the readiness-wait timeout failed in" \
            "no line of the stdout summary names 'readiness wait' together with a duration"
    else
        pass "TC-39: the stdout durations report the phase the readiness-wait timeout failed in"
    fi

    tc39a_run_log="$(resolve_run_log "$tc39a_snapshot" "$RUN_STDOUT")"
    if [[ -z "$tc39a_run_log" ]]; then
        fail "TC-39: the run log records the phase the readiness-wait timeout failed in" \
            "the run wrote no run log"
    else
        tc39a_log_text="$(cat -- "$tc39a_run_log" 2>/dev/null || true)"
        if printf '%s\n' "$(run_log_phase_rows "$tc39a_log_text")" | grep -q '^readiness wait|'; then
            pass "TC-39: the run log records the phase the readiness-wait timeout failed in"
        else
            fail "TC-39: the run log records the phase the readiness-wait timeout failed in" \
                "no 'phase readiness wait finished in <N>s' record in the run log"
        fi
    fi

    # --- the verdict phase ---
    tc39b_game="$(fixture_game tc39b || true)"
    tc39b_log="${FIXTURE_ROOT}/tc39b/SMAPI-latest.txt"
    tc39b_marker="${FIXTURE_ROOT}/tc39b/launched.marker"
    tc39b_project="$(fixture_project tc39b FixtureMod || true)"
    register_stub "$tc39b_marker"

    # A mod-scoped ERROR in a log that ALSO lists the mod: the gate's first
    # leg passes and its third leg fails, so the run fails IN THE VERDICT
    # PHASE with the documented code 5. A log without the loaded-mods block
    # would fail the gate with 17 instead, and the case would measure the
    # wrong verdict.
    stub_log_mod_loaded_with_error "$tc39b_log"

    tc39b_snapshot="${FIXTURE_ROOT}/tc39b-run-log-snapshot.txt"
    snapshot_run_logs "$tc39b_snapshot"

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_LOG=${tc39b_log}" \
        "SMOKE_STUB_LOG_SRC=$(stub_template_for "${tc39b_log}")" \
        "SMOKE_STUB_MARKER=${tc39b_marker}" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc39b_game" \
        --mod-project "$tc39b_project" \
        --smapi-log "$tc39b_log" \
        --timeout 60

    kill_stub "$tc39b_marker"

    assert_exit 5 "TC-39: the failing gate exits 5"

    tc39b_stdout_secs="$(stdout_phase_duration "$RUN_STDOUT" "verdict")"
    if [[ -z "$tc39b_stdout_secs" ]]; then
        fail "TC-39: the stdout durations report the phase the gate failed in" \
            "no line of the stdout summary names 'verdict' together with a duration"
    else
        pass "TC-39: the stdout durations report the phase the gate failed in"
    fi

    tc39b_run_log="$(resolve_run_log "$tc39b_snapshot" "$RUN_STDOUT")"
    if [[ -z "$tc39b_run_log" ]]; then
        fail "TC-39: the run log records the phase the gate failed in" \
            "the run wrote no run log"
    else
        tc39b_log_text="$(cat -- "$tc39b_run_log" 2>/dev/null || true)"
        if printf '%s\n' "$(run_log_phase_rows "$tc39b_log_text")" | grep -q '^verdict|'; then
            pass "TC-39: the run log records the phase the gate failed in"
        else
            fail "TC-39: the run log records the phase the gate failed in" \
                "no 'phase verdict finished in <N>s' record in the run log"
        fi
    fi
fi

# ---------------------------------------------------------------------------
# TC-40 — a deploy target that cannot be removed aborts the run (FR-016 (b))
#
# FR-016 (b): the deploy target MUST be removed before the build, "because
# Pathoschild.Stardew.ModBuildConfig's deploy task copies but never deletes,
# so a stale file from a previous build could otherwise survive".
#
# The property: a target that CANNOT be removed aborts the run with exit 12,
# naming the target and the removal's own error — a stale file from a
# previous build can no longer survive a run that reports success.
#
# The fixture makes BOTH candidate deploy folders unremovable (the folder
# the build deploys into and the folder the project file's name names), so
# the case measures the REPORT and not which folder the script derives.
# ---------------------------------------------------------------------------

announce "TC-40" "a deploy target that cannot be removed aborts the run with exit 12"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-40 (the unremovable deploy target)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-40 (the unremovable deploy target)" "the suite time budget"
else
    tc40_game="$(fixture_game tc40 || true)"
    tc40_log="${FIXTURE_ROOT}/tc40/SMAPI-latest.txt"
    tc40_marker="${FIXTURE_ROOT}/tc40/launched.marker"
    tc40_project="$(fixture_project tc40 FixtureMod DifferentFolderName || true)"
    tc40_mods="${tc40_game}/Mods"
    register_stub "$tc40_marker"

    stub_log "$tc40_log" mod_loaded

    # Neither candidate deploy folder can be removed: each holds a
    # subdirectory whose mode denies the unlink.
    make_unremovable_dir "${tc40_mods}/DifferentFolderName" || true
    make_unremovable_dir "${tc40_mods}/FixtureMod" || true

    tc40_snapshot="${FIXTURE_ROOT}/tc40-run-log-snapshot.txt"
    snapshot_run_logs "$tc40_snapshot"

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_LOG=${tc40_log}" \
        "SMOKE_STUB_LOG_SRC=$(stub_template_for "${tc40_log}")" \
        "SMOKE_STUB_MARKER=${tc40_marker}" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc40_game" \
        --mod-project "$tc40_project" \
        --smapi-log "$tc40_log" \
        --timeout 60

    unlock_dir "${tc40_mods}/DifferentFolderName"
    unlock_dir "${tc40_mods}/FixtureMod"

    # The marker is read BEFORE kill_stub: kill_stub removes it.
    tc40_launched=0
    if [[ -f "$tc40_marker" ]]; then
        tc40_launched=1
    fi
    kill_stub "$tc40_marker"

    assert_exit 12 "TC-40: a deploy target that cannot be removed exits 12"
    assert_eq "0" "$tc40_launched" \
        "TC-40: the game was never launched"

    if printf '%s\n' "$RUN_STDERR" | grep -q 'Mods/DifferentFolderName\|Mods/FixtureMod'; then
        pass "TC-40: the report names the deploy target it could not remove"
    else
        fail "TC-40: the report names the deploy target it could not remove" \
            "the report names no deploy target under the resolved mods path"
    fi

    # The removal's own diagnostic. `rm` prefixes every message with its own
    # name, and that prefix is the locale-independent part of it.
    if printf '%s\n' "$RUN_STDERR" | grep -q 'rm: '; then
        pass "TC-40: the report carries the removal's error"
    else
        fail "TC-40: the report carries the removal's error" \
            "the report carries no diagnostic from the removal itself"
    fi
fi

# ---------------------------------------------------------------------------
# TC-42 — the mod's own success marker is reported (FR-003, FR-006, SC-002)
#
# FR-006: "The mod's own success marker, when present, MUST be reported in
# the result output as a secondary signal but does not gate the verdict."
#
# The mod emits the marker on `GameLaunched`, which is AFTER SMAPI resolved
# the mods: the marker therefore lands inside the window between the
# mod-resolution header and the termination. The stub appends it to the log
# a couple of seconds after the launch, so the marker is inside that
# window and NOT in the file when the mod-resolution header is observed.
# ---------------------------------------------------------------------------

announce "TC-42" "the mod's own success marker is reported on stdout"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-42 (the mod's success marker)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-42 (the mod's success marker)" "the suite time budget"
else
    tc42_game="$(fixture_game tc42 || true)"
    tc42_log="${FIXTURE_ROOT}/tc42/SMAPI-latest.txt"
    tc42_marker="${FIXTURE_ROOT}/tc42/launched.marker"
    tc42_project="$(fixture_project tc42 FixtureMod || true)"
    register_stub "$tc42_marker"

    # The stub that appends the marker a couple of seconds after the
    # launch, and stays alive until the script terminates it.
    write_stub_apphost_delayed_marker "${tc42_game}/StardewModdingAPI" || true

    stub_log "$tc42_log" mod_loaded

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_LOG=${tc42_log}" \
        "SMOKE_STUB_LOG_SRC=$(stub_template_for "${tc42_log}")" \
        "SMOKE_STUB_MARKER=${tc42_marker}" \
        "SMOKE_STUB_MARKER_DELAY=2" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc42_game" \
        --mod-project "$tc42_project" \
        --smapi-log "$tc42_log" \
        --timeout 60

    kill_stub "$tc42_marker"

    assert_exit 0 "TC-42: a run whose log carries the mod's success marker exits 0"
    assert_contains "$RUN_STDOUT" "mod-success-marker:" \
        "TC-42: the stdout reports the mod's success marker"
    assert_contains "$RUN_STDOUT" "mod was loaded successfully!" \
        "TC-42: the reported marker is the mod's own success marker"
fi

# ---------------------------------------------------------------------------
# TC-44 — the last line of a child reaches the run log (FR-005, FR-014)
#
# FR-005: the child's output MUST reach the run log. A reader that loops
# over `read` loses the last line when the child ends WITHOUT a trailing
# newline: `read` returns non-zero at EOF with a partial buffer and the loop
# body never runs for it. A child that was killed mid-write is exactly that
# shape.
#
# The properties:
#
#   1. the game's last line reaches the run log even though the child ends
#      without a newline;
#   2. the build output's last line reaches the run log too — the build
#      output is dumped with the same kind of read loop.
# ---------------------------------------------------------------------------

announce "TC-44" "a child's last line reaches the run log even without a trailing newline"

if (( HAVE_HOST_TOOLS == 0 )); then
    skip "TC-44 (the child's last line)" "the host tool sweep"
elif ! suite_budget_left; then
    skip "TC-44 (the child's last line)" "the suite time budget"
else
    # --- the game's own output ---
    tc44_game="$(fixture_game tc44 || true)"
    tc44_log="${FIXTURE_ROOT}/tc44/SMAPI-latest.txt"
    tc44_marker="${FIXTURE_ROOT}/tc44/launched.marker"
    tc44_project="$(fixture_project tc44 FixtureMod || true)"
    register_stub "$tc44_marker"

    write_stub_apphost_partial_line "${tc44_game}/StardewModdingAPI" || true

    stub_log "$tc44_log" mod_loaded

    tc44_snapshot="${FIXTURE_ROOT}/tc44-run-log-snapshot.txt"
    snapshot_run_logs "$tc44_snapshot"

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "SMOKE_STUB_LOG=${tc44_log}" \
        "SMOKE_STUB_LOG_SRC=$(stub_template_for "${tc44_log}")" \
        "SMOKE_STUB_MARKER=${tc44_marker}" \
        "SMOKE_STUB_PARTIAL_TEXT=GAME-LAST-LINE-WITHOUT-A-NEWLINE" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc44_game" \
        --mod-project "$tc44_project" \
        --smapi-log "$tc44_log" \
        --timeout 60

    kill_stub "$tc44_marker"

    tc44_run_log="$(resolve_run_log "$tc44_snapshot" "$RUN_STDOUT")"
    if [[ -z "$tc44_run_log" ]]; then
        fail "TC-44: the game's last line reaches the run log" \
            "the run wrote no run log"
    else
        tc44_log_text="$(cat -- "$tc44_run_log" 2>/dev/null || true)"
        assert_contains "$tc44_log_text" "game: GAME-LAST-LINE-WITHOUT-A-NEWLINE" \
            "TC-44: the game's last line reaches the run log"
    fi

    # --- the build output ---
    tc44b_game="$(fixture_game tc44b || true)"
    tc44b_log="${FIXTURE_ROOT}/tc44b/SMAPI-latest.txt"
    tc44b_stub_dir="${FIXTURE_ROOT}/tc44b/stub-bin"
    tc44b_marker="${FIXTURE_ROOT}/tc44b/launched.marker"
    register_stub "$tc44b_marker"

    mkdir -p -- "$tc44b_stub_dir" 2>/dev/null || true
    write_stub_dotnet "${tc44b_stub_dir}/dotnet" || true

    stub_log "$tc44b_log" mod_loaded

    tc44b_snapshot="${FIXTURE_ROOT}/tc44b-run-log-snapshot.txt"
    snapshot_run_logs "$tc44b_snapshot"

    run_script_bounded "$FIXTURE_CASE_LIMIT_SECONDS" \
        "PATH=${tc44b_stub_dir}:${PATH}" \
        "SMOKE_STUB_LOG=${tc44b_log}" \
        "SMOKE_STUB_LOG_SRC=$(stub_template_for "${tc44b_log}")" \
        "SMOKE_STUB_MARKER=${tc44b_marker}" \
        "SMOKE_STUB_BUILD_LAST_LINE=BUILD-LAST-LINE-WITHOUT-A-NEWLINE" \
        "SMOKE_STUB_EXIT=0" \
        -- \
        --game-path "$tc44b_game" \
        --smapi-log "$tc44b_log" \
        --timeout 60

    kill_stub "$tc44b_marker"

    tc44b_run_log="$(resolve_run_log "$tc44b_snapshot" "$RUN_STDOUT")"
    if [[ -z "$tc44b_run_log" ]]; then
        fail "TC-44: the build output's last line reaches the run log" \
            "the run wrote no run log"
    else
        tc44b_log_text="$(cat -- "$tc44b_run_log" 2>/dev/null || true)"
        assert_contains "$tc44b_log_text" "build: BUILD-LAST-LINE-WITHOUT-A-NEWLINE" \
            "TC-44: the build output's last line reaches the run log"
    fi
fi

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------

printf '# integration suite wall time: %s seconds\n' "$(elapsed_seconds)"

finish_tests
