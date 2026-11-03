#!/usr/bin/env bash
if [ -n "${_LR_SAVE_LOADED:-}" ]; then return 0 2>/dev/null || exit 0; fi
_LR_SAVE_LOADED=1
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

SAVE_WORKING_DIR=""; SAVE_BACKUP_DIR=""; SAVE_SOURCE_DIR=""
SAVE_SOURCE_HASH=""; SAVE_SOURCE_HASH_BEFORE=""; SAVE_SOURCE_HASH_AFTER=""
_SAVE_INSTANCE_DIR=""

# Create a per-instance temporary directory
save_create_instance_dir() {
    local base="${1:-${RUNNER_TMPDIR:-/tmp}}"
    local inst
    inst="$(mktemp -d "${base}/lr-save-XXXXXX" 2>/dev/null)" || return "$EXIT_USAGE_ERROR"
    _SAVE_INSTANCE_DIR="$inst"
    echo "$inst"
    return 0
}

# Setup environment variables to isolate game config/data/cache under instance
save_setup_env() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local inst="$1"
    [[ -n "$inst" && -d "$inst" ]] || return 9
    export HOME="$inst/home"
    export XDG_CONFIG_HOME="$inst/xdg/config"
    export XDG_DATA_HOME="$inst/xdg/data"
    export XDG_CACHE_HOME="$inst/xdg/cache"
    export XDG_STATE_HOME="$inst/xdg/state"
    mkdir -p "$HOME" "$XDG_CONFIG_HOME" "$XDG_DATA_HOME" "$XDG_CACHE_HOME" "$XDG_STATE_HOME"
    _SAVE_INSTANCE_DIR="$inst"
    return 0
}

save_isolate() {
    [[ $# -eq 3 ]] || return "$EXIT_USAGE_ERROR"
    local instance="$1" source="$2" save_name="$3"
    [[ -d "$source" ]] || return 9
    [[ -n "$instance" ]] || return 9

    # Ensure instance exists (create if needed)
    if [[ ! -d "$instance" ]]; then
        instance="$(save_create_instance_dir "$(dirname "$instance")" 2>/dev/null)" || return "$EXIT_USAGE_ERROR"
    fi

    # Setup isolated environment
    save_setup_env "$instance" || return $?

    local working="$instance/xdg/StardewValley/Saves/$save_name"
    local backup="$instance/save-backup"
    mkdir -p "$working" "$backup"

    # Never open player save - we only read from source (milestone copy)
    save_assert_no_player_save_access "$source" || return $?

    # Compute source hash before copy
    SAVE_SOURCE_HASH_BEFORE="$(save_hash_tree "$source")"
    [[ -n "$SAVE_SOURCE_HASH_BEFORE" ]] || return 19

    # Copy milestone save as working copy (read-only source: use cp -a)
    if ! cp -a "$source/." "$working/" 2>/dev/null; then
        return 19
    fi
    # Create sidecar backup
    if ! cp -a "$source/." "$backup/" 2>/dev/null; then
        return 19
    fi

    # Compute source hash after copy and verify unchanged
    SAVE_SOURCE_HASH_AFTER="$(save_hash_tree "$source")"
    if [[ "$SAVE_SOURCE_HASH_BEFORE" != "$SAVE_SOURCE_HASH_AFTER" ]]; then
        return 19
    fi

    SAVE_WORKING_DIR="$working"
    SAVE_BACKUP_DIR="$backup"
    SAVE_SOURCE_DIR="$source"
    SAVE_SOURCE_HASH="$SAVE_SOURCE_HASH_AFTER"
    _SAVE_INSTANCE_DIR="$instance"
    return 0
}

save_hash_tree() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local dir="$1"
    (cd "$dir" 2>/dev/null && find . -type f -print0 | sort -z | xargs -0 sha256sum 2>/dev/null | sha256sum | awk '{print $1}') || true
    return 0
}

save_verify_source_unchanged() {
    [[ -n "${SAVE_SOURCE_DIR:-}" ]] || return 19
    local now
    now="$(save_hash_tree "$SAVE_SOURCE_DIR")"
    if [[ "$now" != "${SAVE_SOURCE_HASH:-}" ]]; then
        return 19
    fi
    return 0
}

save_restore_working_copy() {
    [[ -n "${SAVE_BACKUP_DIR:-}" && -n "${SAVE_WORKING_DIR:-}" ]] || return 19
    # Idempotent atomic restore
    rm -rf "${SAVE_WORKING_DIR:?}"/*
    if ! cp -a "${SAVE_BACKUP_DIR}/." "${SAVE_WORKING_DIR}/" 2>/dev/null; then
        return 19
    fi
    # verify
    local h1 h2
    h1="$(save_hash_tree "$SAVE_WORKING_DIR")"
    h2="$(save_hash_tree "$SAVE_BACKUP_DIR")"
    if [[ "$h1" != "$h2" ]]; then return 19; fi
    return 0
}

# Idempotent restore alias
save_restore() {
    save_restore_working_copy "$@"
}

save_assert_no_player_save_access() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local dir="$1"
    # function itself never reads; check an access log if present
    if [[ -f "${RUNNER_RUN_ROOT:-/tmp}/player-save-access.log" ]]; then
        if grep -q "$dir" "${RUNNER_RUN_ROOT}/player-save-access.log"; then
            return 19
        fi
    fi
    return 0
}