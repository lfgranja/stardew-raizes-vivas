#!/usr/bin/env bash
#
# scripts/lib/concurrency.sh — advisory locks, process-conflict claims, orphan
# registry (C-CON-08/09/22/23).
#

if [[ -n "${_LR_CONCURRENCY_SH_LOADED:-}" ]]; then
    return 0 2>/dev/null || exit 0
fi
_LR_CONCURRENCY_SH_LOADED=1

source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

# ---------------------------------------------------------------------------
# State
# ---------------------------------------------------------------------------

declare -A _LR_LOCK_FDS=()
declare -A _LR_LOCK_PATHS=()

_LR_LOCK_ROOT="${RUNNER_LOCK_ROOT:-/tmp/livingroots-locks-${UID:-$(id -u)}}"
_LR_CLAIM_ROOT_OVERRIDE="${RUNNER_CLAIM_ROOT:-}"

# ---------------------------------------------------------------------------
# pid_identity PID -> prints "STARTTIME CMDLINEHASH" (or empty + rc 1)
# ---------------------------------------------------------------------------

pid_identity() {
    if [[ $# -ne 1 ]]; then
        log_error "pid_identity: usage: pid_identity PID"
        return "$EXIT_USAGE_ERROR"
    fi
    local pid="$1"
    if ! [[ "$pid" =~ ^[0-9]+$ ]]; then
        return "$EXIT_USAGE_ERROR"
    fi
    local stat_path="/proc/${pid}/stat"
    local cmdline_path="/proc/${pid}/cmdline"
    if [[ ! -r "$stat_path" || ! -r "$cmdline_path" ]]; then
        return 1
    fi
    # starttime is field 22 (1-indexed) in /proc/PID/stat; comm may contain spaces
    # and parens, so extract after the last ')'.
    local rest starttime cmdline_hash
    rest="$(cat -- "$stat_path" 2>/dev/null)" || return 1
    # strip everything up to and including the last ')'
    rest="${rest##*) }"
    # now fields: state ppid pgrp session tty_nr tpgid flags minflt cminflt majflt cmajflt utime stime cutime cstime priority nice num_threads itrealvalue starttime ...
    # starttime is 20th field in rest (0-index: 19)
    starttime="$(printf '%s' "$rest" | awk '{print $20}')"
    if [[ -z "$starttime" ]]; then
        return 1
    fi
    cmdline_hash="$(sha256sum < "$cmdline_path" 2>/dev/null | awk '{print $1}')"
    if [[ -z "$cmdline_hash" ]]; then
        return 1
    fi
    printf '%s %s\n' "$starttime" "$cmdline_hash"
    return 0
}

# ---------------------------------------------------------------------------
# lock_acquire RESOURCE [TIMEOUT_MS]
# Stores fd in _LR_LOCK_FDS[RESOURCE]. Returns 0 on success.
# ---------------------------------------------------------------------------

_lock_resource_path() {
    local resource="$1"
    local safe="${resource//[^A-Za-z0-9._-]/_}"
    printf '%s/%s.lock' "$_LR_LOCK_ROOT" "$safe"
}

lock_acquire() {
    if [[ $# -lt 1 || $# -gt 2 ]]; then
        log_error "lock_acquire: usage: lock_acquire RESOURCE [TIMEOUT_MS]"
        return "$EXIT_USAGE_ERROR"
    fi
    local resource="$1"
    local timeout_ms="${2:-$RunnerLockTotalTimeoutMs}"
    local lock_path
    lock_path="$(_lock_resource_path "$resource")"

    mkdir -p -- "$_LR_LOCK_ROOT" 2>/dev/null
    chmod 0700 -- "$_LR_LOCK_ROOT" 2>/dev/null || true

    # Open lock file.
    local fd
    exec {fd}>"$lock_path" 2>/dev/null || {
        log_error "lock_acquire: cannot open lock file"
        return "$EXIT_INTERNAL_ERROR"
    }
    chmod 0600 -- "$lock_path" 2>/dev/null || true

    # Bounded backoff.
    local attempt=0
    local backoff=$RunnerLockInitialBackoffMs
    local deadline=$(( $(date +%s%3N) + timeout_ms ))
    while :; do
        if flock -n "$fd" 2>/dev/null; then
            _LR_LOCK_FDS["$resource"]="$fd"
            _LR_LOCK_PATHS["$resource"]="$lock_path"
            return 0
        fi
        attempt=$((attempt + 1))
        if (( attempt >= RunnerLockMaxAttempts )); then
            break
        fi
        local now
        now=$(date +%s%3N)
        if (( now >= deadline )); then
            break
        fi
        # jitter ±25%
        local jitter=$(( backoff * RunnerLockJitterPercent / 100 ))
        local sleep_ms=$(( backoff + (RANDOM % (2 * jitter + 1)) - jitter ))
        (( sleep_ms < 1 )) && sleep_ms=1
        # cap
        (( sleep_ms > RunnerLockMaxBackoffMs )) && sleep_ms=$RunnerLockMaxBackoffMs
        sleep "$(awk -v ms="$sleep_ms" 'BEGIN{printf "%.3f", ms/1000}')"
        backoff=$(( backoff * RunnerLockBackoffMultiplier ))
        (( backoff > RunnerLockMaxBackoffMs )) && backoff=$RunnerLockMaxBackoffMs
    done

    exec {fd}>&- 2>/dev/null || true
    log_error "lock_acquire: contention on $resource"
    return "$EXIT_LOCK_CONTENTION"
}

lock_release() {
    if [[ $# -ne 1 ]]; then
        log_error "lock_release: usage: lock_release RESOURCE"
        return "$EXIT_USAGE_ERROR"
    fi
    local resource="$1"
    local fd="${_LR_LOCK_FDS[$resource]:-}"
    if [[ -n "$fd" ]]; then
        flock -u "$fd" 2>/dev/null || true
        exec {fd}>&- 2>/dev/null || true
        unset '_LR_LOCK_FDS[$resource]'
        unset '_LR_LOCK_PATHS[$resource]'
    fi
    return 0
}

# ---------------------------------------------------------------------------
# Claim root resolution + validation.
# ---------------------------------------------------------------------------

_claim_root_resolve() {
    # Prints root path on stdout; returns 0 on success.
    if [[ -n "$_LR_CLAIM_ROOT_OVERRIDE" ]]; then
        # Validate absolute.
        case "$_LR_CLAIM_ROOT_OVERRIDE" in
            /*) ;;
            *) log_error "claim root: RUNNER_CLAIM_ROOT must be absolute"; return "$EXIT_ERRORED" ;;
        esac
        printf '%s' "$_LR_CLAIM_ROOT_OVERRIDE"
        return 0
    fi
    local uid="${UID:-$(id -u)}"
    local run_user="/run/user/${uid}"
    if [[ -d "$run_user" ]]; then
        printf '%s/livingroots-claims' "$run_user"
        return 0
    fi
    printf '/tmp/livingroots-claims-%s' "$uid"
    return 0
}

_claim_root_validate_or_create() {
    local root="$1"
    # Must not be symlink.
    if [[ -L "$root" ]]; then
        log_error "claim root: symlink not allowed: $root"
        return "$EXIT_ERRORED"
    fi
    if [[ -e "$root" ]]; then
        if [[ ! -d "$root" ]]; then
            log_error "claim root: not a directory: $root"
            return "$EXIT_ERRORED"
        fi
        # owner check
        local owner mode
        owner="$(stat -c '%u' -- "$root" 2>/dev/null)" || owner=""
        mode="$(stat -c '%a' -- "$root" 2>/dev/null)" || mode=""
        local euid
        euid="$(id -u)"
        if [[ "$owner" != "$euid" ]]; then
            log_error "claim root: owner mismatch: $root"
            return "$EXIT_ERRORED"
        fi
        # mode must be exactly 0700 (no group/world bits)
        if [[ "$mode" != "700" ]]; then
            log_error "claim root: bad mode $mode on $root"
            return "$EXIT_ERRORED"
        fi
    else
        # Create with 0700 under umask 077.
        ( umask 077; mkdir -m 0700 -- "$root" ) 2>/dev/null || {
            log_error "claim root: cannot create $root"
            return "$EXIT_ERRORED"
        }
        # Re-validate
        if [[ -L "$root" ]]; then
            log_error "claim root: symlink race on $root"
            return "$EXIT_ERRORED"
        fi
    fi
    return 0
}

# ---------------------------------------------------------------------------
# claim_acquire RESOURCE_ID [PID]
# ---------------------------------------------------------------------------

claim_acquire() {
    if [[ $# -lt 1 || $# -gt 2 ]]; then
        log_error "claim_acquire: usage: claim_acquire RESOURCE_ID [PID]"
        return "$EXIT_USAGE_ERROR"
    fi
    local resource_id="$1"
    local pid="${2:-$$}"

    local root
    root="$(_claim_root_resolve)" || return "$EXIT_ERRORED"
    _claim_root_validate_or_create "$root" || return "$EXIT_ERRORED"

    local claim_path="${root}/${resource_id}.claim"

    # If claim exists, evaluate.
    if [[ -e "$claim_path" || -L "$claim_path" ]]; then
        if [[ -L "$claim_path" ]]; then
            log_error "claim_acquire: claim is symlink"
            return "$EXIT_ERRORED"
        fi
        # Read token.
        local old_pid old_start old_hash
        old_pid="$(sed -n 's/^pid=//p' "$claim_path" 2>/dev/null | head -1)"
        old_start="$(sed -n 's/^start_time=//p' "$claim_path" 2>/dev/null | head -1)"
        old_hash="$(sed -n 's/^cmdline_hash=//p' "$claim_path" 2>/dev/null | head -1)"

        # Liveness check.
        local live=0
        if [[ "$old_pid" =~ ^[0-9]+$ ]] && [[ -r "/proc/${old_pid}/stat" ]]; then
            local ident
            ident="$(pid_identity "$old_pid" 2>/dev/null)" || ident=""
            if [[ -n "$ident" ]]; then
                local cur_start cur_hash
                cur_start="${ident%% *}"
                cur_hash="${ident##* }"
                if [[ "$cur_start" == "$old_start" && "$cur_hash" == "$old_hash" ]]; then
                    live=1
                fi
            fi
        fi

        if (( live == 1 )); then
            log_error "claim_acquire: conflict on $resource_id"
            return "$EXIT_PROCESS_CONFLICT"
        fi

        # Stale: reclaim guard flow.
        local guard_path="${claim_path}.reclaim"
        # Try create guard with O_EXCL.
        local created=0
        if ( set -o noclobber; : > "$guard_path" ) 2>/dev/null; then
            created=1
        fi
        if (( created == 0 )); then
            # Guard exists; check staleness.
            local guard_age_ok=0
            if [[ -f "$guard_path" ]]; then
                local now mtime age
                now=$(date +%s%3N)
                mtime=$(stat -c '%Y' -- "$guard_path" 2>/dev/null || echo 0)
                age=$(( (now / 1000 - mtime) * 1000 ))
                if (( age >= RunnerReclaimGuardStaleMs )); then
                    # check recorded owner token dead
                    guard_age_ok=1
                fi
            fi
            if (( guard_age_ok == 0 )); then
                log_error "claim_acquire: reclaim guard contention"
                return "$EXIT_LOCK_CONTENTION"
            fi
            # Remove stale guard.
            rm -f -- "$guard_path" 2>/dev/null || true
            if ( set -o noclobber; : > "$guard_path" ) 2>/dev/null; then
                created=1
            else
                log_error "claim_acquire: cannot create reclaim guard"
                return "$EXIT_LOCK_CONTENTION"
            fi
        fi

        # Re-check token while holding guard.
        local recheck_live=0
        if [[ "$old_pid" =~ ^[0-9]+$ ]] && [[ -r "/proc/${old_pid}/stat" ]]; then
            local ident2
            ident2="$(pid_identity "$old_pid" 2>/dev/null)" || ident2=""
            if [[ -n "$ident2" ]]; then
                local c_start c_hash
                c_start="${ident2%% *}"
                c_hash="${ident2##* }"
                if [[ "$c_start" == "$old_start" && "$c_hash" == "$old_hash" ]]; then
                    recheck_live=1
                fi
            fi
        fi
        if (( recheck_live == 1 )); then
            rm -f -- "$guard_path" 2>/dev/null || true
            log_error "claim_acquire: conflict on recheck"
            return "$EXIT_PROCESS_CONFLICT"
        fi

        # Remove stale claim.
        rm -f -- "$claim_path" 2>/dev/null || true
        rm -f -- "$guard_path" 2>/dev/null || true
    fi

    # Create claim.
    local instance_id="${RUNNER_INSTANCE_ID:-instance-$$-$(date +%s%N)}"
    local ident
    ident="$(pid_identity "$pid" 2>/dev/null)" || ident="0 0"
    local start_time="${ident%% *}"
    local cmdline_hash="${ident##* }"
    local payload
    payload="instance_id=${instance_id}
pid=${pid}
start_time=${start_time}
cmdline_hash=${cmdline_hash}
created_at=$(date -u +%Y-%m-%dT%H:%M:%S.%3NZ)"

    if ( set -o noclobber; printf '%s\n' "$payload" > "$claim_path" ) 2>/dev/null; then
        chmod 0600 -- "$claim_path" 2>/dev/null || true
        printf '%s\n' "$claim_path"
        return 0
    fi

    # Race: someone else created it.
    log_error "claim_acquire: race creating claim"
    return "$EXIT_PROCESS_CONFLICT"
}

claim_release() {
    if [[ $# -ne 1 ]]; then
        log_error "claim_release: usage: claim_release RESOURCE_ID"
        return "$EXIT_USAGE_ERROR"
    fi
    local resource_id="$1"
    local root
    root="$(_claim_root_resolve)" || return "$EXIT_ERRORED"
    rm -f -- "${root}/${resource_id}.claim" 2>/dev/null || true
    return 0
}

# ---------------------------------------------------------------------------
# orphan_registry BUNDLE_DIR DISPLAY SOCKET_PATH [OWNER_PID] [OWNER_START] [MATCH] [CLASSIFICATION]
# Appends an entry to orphaned-displays.json.
# ---------------------------------------------------------------------------

orphan_registry() {
    if [[ $# -lt 3 ]]; then
        log_error "orphan_registry: usage: orphan_registry BUNDLE_DIR DISPLAY SOCKET_PATH [OWNER_PID] [OWNER_START] [MATCH] [CLASSIFICATION]"
        return "$EXIT_USAGE_ERROR"
    fi
    local bundle_dir="$1"
    local display="$2"
    local socket_path="$3"
    local owner_pid="${4:-null}"
    local owner_start="${5:-null}"
    local match="${6:-false}"
    local classification="${7:-unresponsive}"

    mkdir -p -- "$bundle_dir" 2>/dev/null || {
        log_error "orphan_registry: cannot create bundle dir"
        return "$EXIT_ERRORED"
    }
    local registry="${bundle_dir}/orphaned-displays.json"

    python3 - "$registry" "$display" "$socket_path" "$owner_pid" "$owner_start" "$match" "$classification" <<'PY'
import json, os, sys, datetime
registry, display, socket_path, owner_pid, owner_start, match, classification = sys.argv[1:8]
now = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
entry = {
    "display": display,
    "socketPath": socket_path,
    "observedAtUtc": now,
    "ownerPid": None if owner_pid == "null" else owner_pid,
    "ownerStartTime": None if owner_start == "null" else owner_start,
    "ownerTokenMatch": (match.lower() == "true"),
    "classification": classification,
    "action": "report-only",
}
data = []
if os.path.exists(registry):
    try:
        with open(registry, "r", encoding="utf-8") as f:
            data = json.load(f)
        if not isinstance(data, list):
            data = []
    except Exception:
        data = []
data.append(entry)
tmp = registry + ".tmp"
with open(tmp, "w", encoding="utf-8") as f:
    json.dump(data, f, indent=2)
    f.write("\n")
    f.flush()
    os.fsync(f.fileno())
os.replace(tmp, registry)
PY
    return $?
}
