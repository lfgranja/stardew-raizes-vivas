#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
REPO_ROOT="${RUNNER_REPO_ROOT:-$(cd -- "$SCRIPT_DIR/.." && pwd -P)}"
cd "$REPO_ROOT"

source "$SCRIPT_DIR/lib/common.sh"
source "$SCRIPT_DIR/lib/preflight.sh"
source "$SCRIPT_DIR/lib/acquisition.sh"
source "$SCRIPT_DIR/lib/display.sh"
source "$SCRIPT_DIR/lib/save.sh"
source "$SCRIPT_DIR/lib/launch.sh"
source "$SCRIPT_DIR/lib/injection.sh"
source "$SCRIPT_DIR/lib/signals.sh"
source "$SCRIPT_DIR/lib/verdict.sh"
source "$SCRIPT_DIR/lib/evidence.sh"
source "$SCRIPT_DIR/lib/promotion.sh"
source "$SCRIPT_DIR/lib/cleanup.sh"
source "$SCRIPT_DIR/lib/quit.sh"
source "$SCRIPT_DIR/lib/retry.sh"
source "$SCRIPT_DIR/lib/quarantine.sh"
source "$SCRIPT_DIR/lib/performance.sh"
source "$SCRIPT_DIR/lib/atomic.sh"
source "$SCRIPT_DIR/lib/logging.sh"
source "$SCRIPT_DIR/lib/sanitize.sh"
source "$SCRIPT_DIR/lib/version-gate.sh"
source "$SCRIPT_DIR/lib/scenario-validate.sh"
source "$SCRIPT_DIR/lib/retention.sh"
source "$SCRIPT_DIR/lib/ci.sh"
source "$SCRIPT_DIR/lib/reporters.sh"
source "$SCRIPT_DIR/lib/suite.sh"
source "$SCRIPT_DIR/lib/statistics.sh"

# --- defaults ---
SCENARIO=""
SCENARIO_GLOB=""
GAME_PATH="${RUNNER_GAME_PATH:-${STARDEW_GAME_PATH:-}}"
GAME_PATH_SET=0
SKIP_BUILD=0
OVERRIDES=()
MILESTONE_SAVE=""
QUARANTINE_OWNER_CLI=""
NO_ACQUIRE=0
FORCE_REFRESH=0
IGNORE_EXIT_CODES=""
TIMEOUT=300
VERBOSE=0
CI_MODE=0
VERIFY_BUNDLE=""
CHECK_DOCS=0

usage() { cat <<'EOF'
Usage: verify-scenario.sh --scenario <file|dir> [options]
       verify-scenario.sh --verify-bundle <dir>
       verify-scenario.sh --check-docs [--scenario <file>]
Options: --scenario <file|dir> --scenario-glob <glob> --game-path <dir>
  --skip-build --override <key>=<value> (repeatable) --milestone-save <name>
  --quarantine-owner <name> --no-acquire --force-refresh
  --ignore-exit-code <list> --verify-bundle <dir> --check-docs
  --timeout <seconds> --verbose --ci --help
EOF
}

# --- parse ---
while [[ $# -gt 0 ]]; do
    case "$1" in
        --scenario) [[ $# -ge 2 ]] || { echo "runner: error: code=14 class=USAGE_ERROR message=missing value for --scenario" >&2; exit 14; }; SCENARIO="$2"; shift 2 ;;
        --scenario-glob) [[ $# -ge 2 ]] || { echo "runner: error: code=14 class=USAGE_ERROR message=missing value" >&2; exit 14; }; SCENARIO_GLOB="$2"; shift 2 ;;
        --game-path) [[ $# -ge 2 ]] || { echo "runner: error: code=14 class=USAGE_ERROR message=missing value for --game-path" >&2; exit 14; }; GAME_PATH="$2"; GAME_PATH_SET=1; shift 2 ;;        --skip-build) SKIP_BUILD=1; shift ;;
        --override) [[ $# -ge 2 ]] || { echo "runner: error: code=14 class=USAGE_ERROR message=missing value for --override" >&2; exit 14; }; OVERRIDES+=("$2"); shift 2 ;;
        --milestone-save) [[ $# -ge 2 ]] || { echo "runner: error: code=14 class=USAGE_ERROR message=missing value" >&2; exit 14; }; MILESTONE_SAVE="$2"; shift 2 ;;
        --quarantine-owner) [[ $# -ge 2 ]] || { echo "runner: error: code=14 class=USAGE_ERROR message=missing value" >&2; exit 14; }; QUARANTINE_OWNER_CLI="$2"; shift 2 ;;
        --no-acquire) NO_ACQUIRE=1; shift ;;
        --force-refresh) FORCE_REFRESH=1; shift ;;
        --ignore-exit-code) [[ $# -ge 2 ]] || { echo "runner: error: code=14 class=USAGE_ERROR message=missing value" >&2; exit 14; }; IGNORE_EXIT_CODES="$2"; shift 2 ;;
        --verify-bundle) [[ $# -ge 2 ]] || { echo "runner: error: code=14 class=USAGE_ERROR message=missing value" >&2; exit 14; }; VERIFY_BUNDLE="$2"; shift 2 ;;
        --check-docs) CHECK_DOCS=1; shift ;;
        --timeout) [[ $# -ge 2 ]] || { echo "runner: error: code=14 class=USAGE_ERROR message=missing value" >&2; exit 14; }; TIMEOUT="$2"; shift 2 ;;
        --verbose) VERBOSE=1; shift ;;
        --ci) CI_MODE=1; export CI_MODE; shift ;;
        --help|-h) usage; exit 0 ;;
        --) shift; break ;;
        -*) echo "runner: error: code=14 class=USAGE_ERROR message=unknown flag $1" >&2; exit 14 ;;
        *) echo "runner: error: code=14 class=USAGE_ERROR message=unexpected positional $1" >&2; exit 14 ;;
    esac
done

# CI mode
if (( CI_MODE == 1 )); then
    ci_enable 2>/dev/null || true
    ci_noninteractive 2>/dev/null || true
fi

# --no-acquire + --force-refresh
if (( NO_ACQUIRE == 1 && FORCE_REFRESH == 1 )); then
    echo "runner: error: code=14 class=USAGE_ERROR message=incompatible acquire flags" >&2
    exit 14
fi

# validate --ignore-exit-code
if [[ -n "$IGNORE_EXIT_CODES" ]]; then
    IFS=',' read -ra _codes <<< "$IGNORE_EXIT_CODES"
    for c in "${_codes[@]}"; do
        c="${c// /}"
        case "$c" in
            4|6|7|9|10|13|14|18|19|20|21) ;;
            0|22) echo "runner: error: code=14 class=USAGE_ERROR message=forbidden ignore code $c" >&2; exit 14 ;;
            *) echo "runner: error: code=14 class=USAGE_ERROR message=unknown ignore code $c" >&2; exit 14 ;;
        esac
    done
fi

# validate overrides
ALLOWED_OVERRIDE_RE='^(environment\.gameVersion|environment\.smapiVersion|environment\.modVersion|environment\.saveId|environment\.config\..*)$'
for ov in "${OVERRIDES[@]}"; do
    key="${ov%%=*}"
    if [[ ! "$key" =~ $ALLOWED_OVERRIDE_RE ]]; then
        echo "runner: error: code=14 class=USAGE_ERROR message=forbidden override key $key [field=$key]" >&2
        exit 14
    fi
    if [[ "$ov" != *=* ]]; then
        echo "runner: error: code=14 class=USAGE_ERROR message=malformed override" >&2
        exit 14
    fi
done

# --game-path empty validation
if (( GAME_PATH_SET == 1 )) && [[ -z "$GAME_PATH" ]]; then
    echo "runner: error: code=14 class=USAGE_ERROR message=empty game path" >&2
    exit 14
fi

# --verify-bundle dispatch (launch-free) - T104 territory but keep here as required
if [[ -n "$VERIFY_BUNDLE" ]]; then
    exec "$SCRIPT_DIR/verify-evidence-bundle.sh" "$VERIFY_BUNDLE"
fi

# --check-docs dispatch (launch-free)
if (( CHECK_DOCS == 1 )); then
    if declare -F promotion_check_docs >/dev/null 2>&1 && promotion_check_docs "$REPO_ROOT" "${SCENARIO:-}"; then
        exit 0
    else
        rc=${?:-0}
        if (( rc == 14 )); then exit 14; fi
        exit 21
    fi
fi

# run requires scenario
if [[ -z "$SCENARIO" ]]; then
    echo "runner: error: code=14 class=USAGE_ERROR message=missing --scenario" >&2
    exit 14
fi

# expand suite if directory
SCENARIO_LIST=()
if [[ -d "$SCENARIO" ]]; then
    # wire to suite.sh (T061b)
    if declare -F suite_discover >/dev/null 2>&1; then
        while IFS= read -r s; do
            [[ -n "$s" ]] && SCENARIO_LIST+=("$s")
        done < <(suite_discover "$SCENARIO")
    else
        while IFS= read -r s; do
            [[ -n "$s" ]] && SCENARIO_LIST+=("$s")
        done < <(find "$SCENARIO" -maxdepth 1 -type f \( -name '*.scenario.json' -o -name '*.json' \) | LC_ALL=C sort)
    fi
    if (( ${#SCENARIO_LIST[@]} == 0 )); then
        exit "$EXIT_NO_TESTS"
    fi
else
    SCENARIO_LIST=("$SCENARIO")
fi

# scenario validation
for sc in "${SCENARIO_LIST[@]}"; do
    if [[ -e "$sc" && ! -d "$sc" ]]; then
        if declare -F scenario_validate >/dev/null 2>&1; then
            if ! scenario_validate "$sc" >/dev/null 2>&1; then
                exit 14
            fi
        fi
    fi
done

# Resolve the milestone save from the scenario contract when --milestone-save
# is absent. Every scenario declares one, and the runner previously ignored it,
# so save_prepare received an empty string even when the scenario was complete.
# An explicit --milestone-save wins over the scenario value (the CLI is the
# operator override); a suite uses the first declared milestone.
if [[ -z "$MILESTONE_SAVE" ]]; then
    for _sc in "${SCENARIO_LIST[@]}"; do
        [[ -f "$_sc" ]] || continue
        _ms="$(python3 - "$_sc" <<'PY'
import json, sys
try:
    with open(sys.argv[1], encoding="utf-8") as f:
        data = json.load(f)
except Exception:
    sys.exit(0)
value = data.get("milestoneSave") if isinstance(data, dict) else None
if isinstance(value, str) and value.strip():
    print(value.strip())
PY
)"
        if [[ -n "$_ms" ]]; then
            MILESTONE_SAVE="$_ms"
            break
        fi
    done
fi

# export seams
export RUNNER_REPO_ROOT="$REPO_ROOT"
export RUNNER_GAME_PATH="$GAME_PATH"
export RUNNER_MODS_DIR="${GAME_PATH:+${GAME_PATH}/Mods}"
export RUNNER_LAUNCHER="${RUNNER_LAUNCHER:-${GAME_PATH:+${GAME_PATH}/StardewModdingAPI}}"
export RUNNER_XVFB="${RUNNER_XVFB:-Xvfb}"
RUNNER_SMAPI_LOG_PINNED=0
if [[ -n "${RUNNER_SMAPI_LOG:-}" ]]; then
    RUNNER_SMAPI_LOG_PINNED=1
fi
export RUNNER_SMAPI_LOG_PINNED
export RUNNER_SMAPI_LOG="${RUNNER_SMAPI_LOG:-${XDG_CONFIG_HOME:-$HOME/.config}/StardewValley/ErrorLogs/SMAPI-latest.txt}"
export RUNNER_CACHE_DIR="${RUNNER_CACHE_DIR:-${XDG_CACHE_HOME:-$HOME/.cache}/livingroots/game}"
export RUNNER_EVIDENCE_ROOT="${RUNNER_EVIDENCE_ROOT:-$REPO_ROOT}"
export RUNNER_RUN_ROOT="${RUNNER_RUN_ROOT:-$(mktemp -d)}"
export RUNNER_CLAIM_ROOT="${RUNNER_CLAIM_ROOT:-${XDG_RUNTIME_DIR:-/tmp}/livingroots-runner}"
export RUNNER_RETRY_COOLDOWN_MS="${RUNNER_RETRY_COOLDOWN_MS:-${RunnerRetryCooldownMs:-5000}}"
export RUNNER_QUARANTINE_OWNER="${QUARANTINE_OWNER_CLI:-${RUNNER_QUARANTINE_OWNER:-}}"
export RUNNER_MILESTONE_SAVE="$MILESTONE_SAVE"
export RUNNER_SKIP_BUILD=$SKIP_BUILD
export RUNNER_NO_ACQUIRE=$NO_ACQUIRE
export RUNNER_FORCE_REFRESH=$FORCE_REFRESH
export RUNNER_OVERRIDES="${OVERRIDES[*]:-}"

mkdir -p "$RUNNER_RUN_ROOT" "$RUNNER_CLAIM_ROOT" 2>/dev/null || true

# RUNNER_QUIET
export RUNNER_QUIET="${RUNNER_QUIET:-0}"

# --- run pipeline (T084–T098 order: preflight → acquisition → version gate → isolate → launch → inject → capture → evaluate → evidence → promote → cleanup) ---
RUN_ID="$(runner_run_id 2>/dev/null || date +%s%N)"
BUNDLE_DIR="$RUNNER_EVIDENCE_ROOT/evidence-bundle-$RUN_ID"
if declare -F evidence_init >/dev/null 2>&1; then
    evidence_init "$RUNNER_EVIDENCE_ROOT" "$RUN_ID" 2>/dev/null || true
fi

# preflight
if declare -F preflight_run >/dev/null 2>&1; then
    preflight_run "$GAME_PATH" "${GAME_PATH:+$GAME_PATH/Mods}" "$BUNDLE_DIR" "$SKIP_BUILD" || rc=$?
    if [[ -n "${rc:-}" ]]; then
        case "$rc" in
            9|12|14|19|21) exit "$rc" ;;
        esac
    fi
else
    # fallback preflight
    if [[ -n "$GAME_PATH" ]]; then
        if ! preflight_resolve_game_path "$GAME_PATH" 2>/dev/null; then
            if [[ -z "$GAME_PATH" ]]; then echo "runner: error: code=14 class=USAGE_ERROR message=empty game path" >&2; exit 14; fi
            (( NO_ACQUIRE == 1 )) && exit 9
        fi
    fi
    if [[ -z "$GAME_PATH" ]] && (( NO_ACQUIRE == 1 )); then exit 9; fi
    if [[ -n "$GAME_PATH" && -d "$GAME_PATH" ]]; then
        preflight_check_assemblies "$GAME_PATH" 2>/dev/null || exit 9
        preflight_check_deployment "${GAME_PATH}/Mods" 2>/dev/null || exit 12
    fi
fi

# version gate (before Xvfb)
if declare -F version_gate_classify >/dev/null 2>&1 && [[ -n "$GAME_PATH" ]]; then
    local_man="$GAME_PATH/Mods/LivingRoots/manifest.json"
    [[ -f "$local_man" ]] || local_man="$REPO_ROOT/LivingRoots/manifest.json"
    vg_rc=0
    version_gate_classify "$GAME_PATH" "$local_man" >/dev/null 2>&1 || vg_rc=$?
    if (( vg_rc == 19 )); then
        exit 19
    fi
fi

# acquisition
if (( NO_ACQUIRE == 0 )); then
    if declare -F acquisition_run >/dev/null 2>&1; then
        acquisition_run $([[ $FORCE_REFRESH == 1 ]] && echo --force-refresh) >/dev/null 2>&1 || rc=$?
        if (( ${rc:-0} == 9 || ${rc:-0} == 19 )); then exit $rc; fi
    fi
fi

# isolate (save). This is a direct call, not a `declare -F` probe: a missing
# milestone used to skip the whole stage silently, which is how a launched run
# with no save looked healthy. Missing milestone => 19; malformed invocation => 14.
save_rc=0
save_prepare "$MILESTONE_SAVE" "$RUNNER_RUN_ROOT" >/dev/null 2>&1 || save_rc=$?
if (( save_rc != 0 )); then
    if (( save_rc == EXIT_USAGE_ERROR )); then
        echo "runner: error: code=14 class=USAGE_ERROR message=save_prepare invocation invalid" >&2
        exit "$EXIT_USAGE_ERROR"
    fi
    echo "runner: error: code=19 class=ERRORED message=milestone save unavailable (${MILESTONE_SAVE:-<none>})" >&2
    exit "$EXIT_ERRORED"
fi

# display
if ! display_start 2>/dev/null; then
    exit 10
fi
trap 'display_stop 2>/dev/null || true; cleanup_run 2>/dev/null || true' EXIT

# launch
# The verification mod reads this from the game process environment at Entry.
# injection_run also exports it, but that happens after the process started, so
# the value must be in the launcher's environment before launch_start.
export RUNNER_INJECTION_DIR="${RUNNER_INJECTION_DIR:-$RUNNER_RUN_ROOT/injection}"
mkdir -p "$RUNNER_INJECTION_DIR" 2>/dev/null || true
LAUNCHER="${RUNNER_LAUNCHER:-${GAME_PATH:+$GAME_PATH/StardewModdingAPI}}"
LOG_PATH="$RUNNER_SMAPI_LOG"
mkdir -p "$(dirname "$LOG_PATH")" 2>/dev/null || true
if [[ -z "$LAUNCHER" || ! -f "$LAUNCHER" ]]; then
    display_stop 2>/dev/null || true
    exit 9
fi
if declare -F launch_start >/dev/null 2>&1; then
    launch_start "$LAUNCHER" "$LOG_PATH" "$TIMEOUT" || { display_stop 2>/dev/null || true; exit 9; }
    if declare -F launch_wait_ready >/dev/null 2>&1; then
        rc=0
        launch_wait_ready "$LOG_PATH" "${LAUNCH_PID:-}" "$TIMEOUT" || rc=$?
        if (( rc != 0 )); then
            display_stop 2>/dev/null || true
            exit "$rc"
        fi
    fi
fi

# inject: direct call, no `declare -F` escape hatch. A queue that the mod never
# acknowledges is an errored run, not a launch that quietly did nothing.
RUN_EXIT=0
inj_rc=0
injection_run "${SCENARIO_LIST[@]}" >/dev/null 2>&1 || inj_rc=$?
if (( inj_rc != 0 )); then
    RUN_EXIT="$EXIT_ERRORED"
fi

# capture/signals
sigcap_rc=0
signals_capture "$BUNDLE_DIR" "${SCENARIO_LIST[@]}" >/dev/null 2>&1 || sigcap_rc=$?
if (( sigcap_rc != 0 )); then
    RUN_EXIT="$EXIT_ERRORED"
fi

# verdict gate: the three mandatory signals ARE the evidence. When any is
# missing the verdict is `partial` (exit 19) and is never promoted to pass
# (FR-011). This is what stops a launched-but-uncaptured run exiting 0 with an
# empty artifacts/ directory. `--ignore-exit-code` is deliberately not applied:
# demoting an incomplete signal set would recreate the false pass.
sig_rc=0
signals_require_expected_signals "$BUNDLE_DIR" "$RUN_ID" "${RUNNER_QUARANTINE_OWNER:-runner}" >/dev/null 2>&1 || sig_rc=$?
if (( sig_rc != 0 )); then
    RUN_EXIT="$EXIT_ERRORED"
fi

# evaluate: thresholds are judged against the captured artifacts. The wrapper
# keeps the pure 5-argument verdict_evaluate unchanged.
verdict_rc=0
verdict_out="$(verdict_evaluate_bundle "$BUNDLE_DIR" "${SCENARIO_LIST[@]}" 2>/dev/null)" || verdict_rc=$?
if (( verdict_rc != 0 )); then
    RUN_EXIT="$verdict_rc"
fi

# evidence
printf '{"runId":"%s"}\n' "$RUN_ID" > "$RUNNER_RUN_ROOT/manifest-input.json" 2>/dev/null || true
if declare -F evidence_finalize >/dev/null 2>&1; then
    evidence_finalize "$BUNDLE_DIR" "$RUNNER_RUN_ROOT/manifest-input.json" >/dev/null 2>&1 || true
fi
if declare -F evidence_write_claims >/dev/null 2>&1; then
    # W = launch/connect only; R requires runtime evidence. A run whose signal
    # set is incomplete must not claim R (honesty level is sequential).
    if (( RUN_EXIT == 0 )); then _honesty="R"; else _honesty="W"; fi
    evidence_write_claims "$BUNDLE_DIR" "$RUN_ID" "$_honesty" >/dev/null 2>&1 || true
fi

# promote — read testMdPath from each scenario definition (FR-016)
declare -A _PROMOTED_TEST_MDS=()
for _sc in "${SCENARIO_LIST[@]}"; do
    [[ -f "$_sc" ]] || continue
    _tm="$(python3 - "$_sc" "$REPO_ROOT" <<'PY'
import json, os, sys
sc, root = sys.argv[1], sys.argv[2]
try:
    with open(sc, encoding="utf-8") as f:
        data = json.load(f)
except Exception:
    sys.exit(0)
tm = data.get("testMdPath") if isinstance(data, dict) else None
if not isinstance(tm, str) or not tm.strip():
    sys.exit(0)
cands = []
if os.path.isabs(tm):
    cands.append(tm)
else:
    cands.append(os.path.join(root, tm))
    cands.append(os.path.join(os.path.dirname(os.path.abspath(sc)), tm))
for c in cands:
    if os.path.isfile(c):
        print(os.path.abspath(c))
        sys.exit(0)
print(os.path.abspath(os.path.join(root, tm)))
PY
)"
    [[ -n "$_tm" ]] || continue
    if [[ -n "${_PROMOTED_TEST_MDS[$_tm]:-}" ]]; then
        continue
    fi
    _PROMOTED_TEST_MDS[$_tm]=1
    if (( RUN_EXIT == 0 )) && [[ -f "$_tm" ]] && grep -q 'GATE3:BEGIN' "$_tm" 2>/dev/null; then
        if declare -F promotion_apply >/dev/null 2>&1; then
            promotion_apply "$REPO_ROOT" "$_tm" "$BUNDLE_DIR" >/dev/null 2>&1 || true
        fi
    fi
done

# clean quit: direct call. The backstop path records cleanQuit=false instead of
# pretending the run was clean.
quit_rc=0
quit_run >/dev/null 2>&1 || quit_rc=$?
if (( quit_rc != 0 && RUN_EXIT == 0 )); then
    RUN_EXIT="$quit_rc"
fi
display_stop 2>/dev/null || true
trap - EXIT
if declare -F cleanup_run >/dev/null 2>&1; then
    cleanup_run >/dev/null 2>&1 || true
fi

exit "$RUN_EXIT"

exit 0