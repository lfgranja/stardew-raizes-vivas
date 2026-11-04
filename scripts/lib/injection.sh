#!/usr/bin/env bash
if [ -n "${_LR_INJECTION_LOADED:-}" ]; then return 0 2>/dev/null || exit 0; fi
_LR_INJECTION_LOADED=1
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

injection_validate_commands() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local scenario="$1"
    [[ -f "$scenario" ]] || return "$EXIT_USAGE_ERROR"
    python3 - "$scenario" <<'PY'
import json,sys
d=json.load(open(sys.argv[1]))
cmds=d.get("initCommands",[])
for c in cmds:
    if not isinstance(c,dict): sys.exit(14)
    if c.get("kind") not in ("macro","console"): sys.exit(14)
    if "command" not in c and "path" not in c and "macro" not in c and "file" not in c:
        sys.exit(14)
sys.exit(0)
PY
}

injection_validate_macro() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local file="$1"
    [[ -f "$file" ]] || return "$EXIT_USAGE_ERROR"
    [[ "$file" == *.txt ]] || return 14
    python3 - "$file" <<'PY'
import sys
p=sys.argv[1]
raw=open(p,'rb').read()
# BOM optional
if raw.startswith(b'\xef\xbb\xbf'):
    raw=raw[3:]
try:
    text=raw.decode('utf-8')
except Exception:
    sys.exit(14)
lines=text.splitlines()
# check leading whitespace before /
for l in lines:
    if l.strip()=="":
        continue
    if l != l.lstrip():
        # leading whitespace before slash
        if l.lstrip().startswith('/'):
            sys.exit(14)
    if not l.lstrip().startswith('/'):
        # non-empty line must start /
        if l.strip()!="":
            sys.exit(14)
# count sleep
sleeps=[l for l in lines if l.strip().lower().startswith('/sleep')]
if len(sleeps)!=1:
    sys.exit(14)
# sleep must be last non-empty line
nonempty=[l for l in lines if l.strip()!=""]
if nonempty and not nonempty[-1].strip().lower().startswith('/sleep'):
    sys.exit(14)
# nested macro: no runmacro at all
for l in lines:
    if l.strip().lower().startswith('/runmacro'):
        sys.exit(14)
sys.exit(0)
PY
}

# injection_build_queue <scenario> <out> [all|init|steps]
#
# Emit the executable verification-queue commands for one scenario. This is
# deliberately NOT a SMAPI macro: the launcher runs with stdin closed and
# `debug runmacro` cannot call mod commands, so the verification mod consumes
# these lines from a file queue instead. Macro files are read here and their
# `/`-prefixed lines are flattened into the queue; `/sleep` is dropped because
# the runner waits for the queue acknowledgement rather than a fixed sleep.
#
# `init` and `steps` split the two halves so injection_run can capture the
# "before" dumps after setup (soil health, crop, bin) but before the scenario
# mutates the world. The default `all` keeps the historical single-file shape.
injection_build_queue() {
    [[ $# -eq 2 || $# -eq 3 ]] || return "$EXIT_USAGE_ERROR"
    local scenario="$1" out="$2" phase="${3:-all}"
    [[ -f "$scenario" ]] || return "$EXIT_USAGE_ERROR"
    case "$phase" in
        all|init|steps) ;;
        *) return "$EXIT_USAGE_ERROR" ;;
    esac
    python3 - "$scenario" "$out" "$phase" <<'PY'
import json, os, sys
scn, out, phase = sys.argv[1], sys.argv[2], sys.argv[3]
with open(scn, encoding="utf-8") as f:
    d = json.load(f)
base = os.path.dirname(os.path.abspath(scn))
lines = []

def emit(command):
    command = command.strip()
    if not command:
        return
    if command.startswith("/"):
        command = command[1:].strip()
    if not command:
        return
    lowered = command.lower()
    if lowered.startswith("sleep"):
        # Queue execution is acknowledged; a fixed game-thread sleep is not a
        # command and would stall the consumer.
        return
    lines.append(command)

def tile_args(target):
    if not isinstance(target, str):
        return None
    parts = target.replace(" ", "").split(",")
    if len(parts) != 2:
        return None
    try:
        return int(parts[0]), int(parts[1])
    except ValueError:
        return None

for c in (d.get("initCommands", []) or []) if phase in ("all", "init") else []:
    if not isinstance(c, dict):
        continue
    kind = c.get("kind")
    if kind == "macro":
        rel = c.get("file") or c.get("path") or c.get("macro") or ""
        if not rel:
            continue
        path = rel if os.path.isabs(rel) else os.path.join(base, rel)
        if not os.path.isfile(path):
            sys.stderr.write(f"macro file not found: {path}\n")
            sys.exit(19)
        with open(path, encoding="utf-8-sig") as mf:
            for raw in mf:
                emit(raw)
    elif kind == "console":
        emit(str(c.get("command") or c.get("cmd") or ""))

for step in (d.get("steps", []) or []) if phase in ("all", "steps") else []:
    if not isinstance(step, dict):
        continue
    action = step.get("action")
    target = step.get("target")
    xy = tile_args(target) if isinstance(target, str) and "," in target else None
    inputs = step.get("inputs") or {}
    if action == "insertWaste" and xy:
        item_id = str(inputs.get("itemId", "771"))
        count = str(inputs.get("count", "1"))
        lines.append(f"lr_insert_waste {xy[0]} {xy[1]} {item_id} {count}")
    elif action == "sleep":
        days = step.get("days", inputs.get("days", 1))
        try:
            days = int(days)
        except (TypeError, ValueError):
            days = 1
        if days < 0:
            days = 0
        lines.append(f"lr_advance_days {days}")
    elif action == "dumpCropPhase":
        # The runner brackets every scenario with before/after dumps. A second
        # write to the same after file from here would race the bracketing
        # write, so this step stays a contract marker only.
        continue
    elif action == "observe":
        # The runner appends the after dumps; observe is a contract marker.
        continue

with open(out, "w", encoding="utf-8") as f:
    f.write("\n".join(lines))
    if lines:
        f.write("\n")
sys.exit(0)
PY
}

# injection_write_session <injection_dir> <run_id> <save_id> <dump_dir> <smapi_log>
# Atomic: temp file + rename, so the mod never reads a half-written session.
injection_write_session() {
    [[ $# -eq 5 ]] || return "$EXIT_USAGE_ERROR"
    local dir="$1" run_id="$2" save_id="$3" dump_dir="$4" smapi_log="$5"
    mkdir -p "$dir" || return "$EXIT_INTERNAL_ERROR"
    python3 - "$dir/session.json" "$run_id" "$save_id" "$dump_dir" "$smapi_log" <<'PY'
import json, os, sys, tempfile
path, run_id, save_id, dump_dir, smapi_log = sys.argv[1:6]
payload = {
    "schemaVersion": 1,
    "runId": run_id,
    "saveId": save_id,
    "dumpDir": dump_dir,
    "smapiLogPath": smapi_log,
}
directory = os.path.dirname(path)
fd, tmp = tempfile.mkstemp(prefix=".session-", dir=directory, text=True)
try:
    with os.fdopen(fd, "w", encoding="utf-8") as f:
        json.dump(payload, f, indent=2)
    os.replace(tmp, path)
except Exception:
    try:
        os.unlink(tmp)
    except OSError:
        pass
    raise
PY
}

# injection_write_queue <injection_dir> <generation> <command_file>
injection_write_queue() {
    [[ $# -eq 3 ]] || return "$EXIT_USAGE_ERROR"
    local dir="$1" generation="$2" commands="$3"
    [[ -f "$commands" ]] || return "$EXIT_USAGE_ERROR"
    [[ "$generation" =~ ^[0-9]+$ ]] || return "$EXIT_USAGE_ERROR"
    local target="$dir/queue-$generation.txt"
    local tmp="$target.tmp.$$"
    mkdir -p "$dir" || return "$EXIT_INTERNAL_ERROR"
    cp -f "$commands" "$tmp" || return "$EXIT_INTERNAL_ERROR"
    mv -f "$tmp" "$target" || return "$EXIT_INTERNAL_ERROR"
    return 0
}

# injection_wait_ack <injection_dir> <generation> <timeout_seconds>
# 0 = acknowledged ok; 19 = consumer reported failure; 4 = timeout.
injection_wait_ack() {
    [[ $# -eq 3 ]] || return "$EXIT_USAGE_ERROR"
    local dir="$1" generation="$2" timeout="$3"
    [[ "$generation" =~ ^[0-9]+$ ]] || return "$EXIT_USAGE_ERROR"
    [[ "$timeout" =~ ^[0-9]+$ ]] || return "$EXIT_USAGE_ERROR"
    local ack="$dir/ack-$generation.json"
    local err="$dir/error-$generation.json"
    local waited=0
    while (( waited < timeout * 2 )); do
        if [[ -f "$ack" ]]; then
            python3 - "$ack" <<'PY'
import json, sys
try:
    d = json.load(open(sys.argv[1], encoding="utf-8"))
except Exception:
    sys.exit(19)
sys.exit(0 if d.get("ok") is True else 19)
PY
            return $?
        fi
        if [[ -f "$err" ]]; then
            return "$EXIT_ERRORED"
        fi
        sleep 0.5
        waited=$((waited + 1))
    done
    return "$EXIT_TIMEOUT"
}

# injection_run <scenario...>
#
# Write the session + generation-1 queue and wait for the verification mod to
# acknowledge it. The queue always brackets the scenario commands with
# before/after dumps so the three Gate 3 signals are produced even when a
# scenario omits explicit dump steps.
injection_run() {
    [[ $# -ge 1 ]] || return "$EXIT_USAGE_ERROR"
    local dir="${RUNNER_INJECTION_DIR:-${RUNNER_RUN_ROOT:-/tmp}/injection}"
    local run_id="${RUN_ID:-${RUNNER_RUN_ID:-unknown}}"
    local save_id="${RUNNER_SAVE_ID:-}"
    local dump_dir="$dir/dumps"
    local smapi_log="${RUNNER_SMAPI_LOG:-}"
    local timeout="${RUNNER_INJECTION_TIMEOUT:-300}"
    if [[ -z "$save_id" ]]; then
        log_error "injection_run: RUNNER_SAVE_ID is empty; save_prepare must run first"
        return "$EXIT_ERRORED"
    fi
    mkdir -p "$dir" "$dump_dir" || return "$EXIT_INTERNAL_ERROR"
    export RUNNER_INJECTION_DIR="$dir"
    injection_write_session "$dir" "$run_id" "$save_id" "$dump_dir" "$smapi_log" || return "$EXIT_INTERNAL_ERROR"

    local combined="$dir/commands-1.txt"
    : > "$combined" || return "$EXIT_INTERNAL_ERROR"
    local scenario out rc
    # Setup first: the scenario init commands plant the fixture crop and seed
    # soil health. The "before" capture must observe that prepared world, so it
    # cannot precede the init commands.
    for scenario in "$@"; do
        [[ -f "$scenario" ]] || return "$EXIT_USAGE_ERROR"
        out="$(mktemp "${TMPDIR:-/tmp}/lr-queue.XXXXXXXXXX")" || return "$EXIT_INTERNAL_ERROR"
        rc=0
        injection_build_queue "$scenario" "$out" init || rc=$?
        if (( rc != 0 )); then
            rm -f "$out"
            return "$rc"
        fi
        cat "$out" >> "$combined"
        rm -f "$out"
    done
    printf 'lr_dump_soil_health before\n' >> "$combined"
    printf 'lr_dump_crop_phase before\n' >> "$combined"
    # Then the scenario's mutating steps.
    for scenario in "$@"; do
        [[ -f "$scenario" ]] || return "$EXIT_USAGE_ERROR"
        out="$(mktemp "${TMPDIR:-/tmp}/lr-queue.XXXXXXXXXX")" || return "$EXIT_INTERNAL_ERROR"
        rc=0
        injection_build_queue "$scenario" "$out" steps || rc=$?
        if (( rc != 0 )); then
            rm -f "$out"
            return "$rc"
        fi
        cat "$out" >> "$combined"
        rm -f "$out"
    done
    printf 'lr_dump_soil_health after\n' >> "$combined"
    printf 'lr_dump_crop_phase after\n' >> "$combined"

    injection_write_queue "$dir" 1 "$combined" || return "$EXIT_INTERNAL_ERROR"
    injection_wait_ack "$dir" 1 "$timeout"
    return $?
}

# injection_quit <injection_dir> <generation> [timeout]
injection_quit() {
    [[ $# -ge 2 ]] || return "$EXIT_USAGE_ERROR"
    local dir="$1" generation="$2" timeout="${3:-60}"
    local commands="$dir/commands-$generation.txt"
    mkdir -p "$dir" || return "$EXIT_INTERNAL_ERROR"
    printf 'lr_quit\n' > "$commands" || return "$EXIT_INTERNAL_ERROR"
    injection_write_queue "$dir" "$generation" "$commands" || return "$EXIT_INTERNAL_ERROR"
    injection_wait_ack "$dir" "$generation" "$timeout"
    return $?
}

injection_seed() {
    # seeds through host-authoritative lock; refuse farmhand
    if [[ "${PLAYER_ROLE:-host}" == "farmhand" ]]; then
        return 21
    fi
    # claim_acquire
    if declare -F claim_acquire >/dev/null 2>&1; then
        claim_acquire "seed" 2>/dev/null || return 6
    fi
    return 0
}
