#!/usr/bin/env bash
#
# scripts/lib/atomic.sh — atomic file writes (C-CON-04/13/14).
#
# Sourced, never executed. Provides atomic_write and fsync_dir.
#

if [[ -n "${_LR_ATOMIC_SH_LOADED:-}" ]]; then
    return 0 2>/dev/null || exit 0
fi
_LR_ATOMIC_SH_LOADED=1

# shellcheck source=common.sh
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

# fsync a single file's data to disk.
fsync_file() {
    local target="$1"
    python3 - "$target" <<'PY'
import os, sys
path = sys.argv[1]
fd = os.open(path, os.O_RDONLY)
try:
    os.fsync(fd)
finally:
    os.close(fd)
PY
}

# fsync a directory (parent) to disk.
fsync_dir() {
    local target="$1"
    if [[ ! -d "$target" ]]; then
        log_error "fsync_dir: not a directory: $target"
        return "$EXIT_INTERNAL_ERROR"
    fi
    python3 - "$target" <<'PY'
import os, sys
path = sys.argv[1]
fd = os.open(path, os.O_RDONLY | getattr(os, "O_DIRECTORY", 0))
try:
    os.fsync(fd)
finally:
    os.close(fd)
PY
}

# atomic_write TARGET [SOURCE|-]
# Writes content to TARGET atomically: temp file in same directory, fsync data,
# rename, fsync parent. On any failure the target is left untouched and the
# temp file is removed. EXDEV (cross-device rename) is an internal error (13).
atomic_write() {
    if [[ $# -lt 1 || $# -gt 2 ]]; then
        log_error "atomic_write: usage: atomic_write TARGET [SOURCE|-]"
        return "$EXIT_USAGE_ERROR"
    fi
    local target="$1"
    local source="${2:--}"
    local dir tmp rc

    dir="$(dirname -- "$target")"
    if [[ ! -d "$dir" ]]; then
        log_error "atomic_write: target directory missing: $dir"
        return "$EXIT_ERRORED"
    fi

    tmp="$(mktemp "${dir}/.atomic.XXXXXX")" || {
        log_error "atomic_write: mktemp failed"
        return "$EXIT_ERRORED"
    }
    chmod 0600 -- "$tmp" 2>/dev/null || true

    # Write payload.
    if [[ "$source" == "-" ]]; then
        if ! cat > "$tmp"; then
            rm -f -- "$tmp"
            log_error "atomic_write: write failed"
            return "$EXIT_ERRORED"
        fi
    else
        if [[ ! -f "$source" ]]; then
            rm -f -- "$tmp"
            log_error "atomic_write: source not a file: $source"
            return "$EXIT_USAGE_ERROR"
        fi
        if ! cat -- "$source" > "$tmp"; then
            rm -f -- "$tmp"
            log_error "atomic_write: write failed"
            return "$EXIT_ERRORED"
        fi
    fi

    # fsync data.
    if ! fsync_file "$tmp"; then
        rm -f -- "$tmp"
        log_error "atomic_write: fsync data failed"
        return "$EXIT_ERRORED"
    fi

    # Atomic rename.
    if ! mv -f -- "$tmp" "$target" 2>/tmp/atomic.err; then
        rc=$?
        # Detect EXDEV from mv stderr.
        if grep -qi "cross-device\|EXDEV\|Invalid cross-device" /tmp/atomic.err 2>/dev/null; then
            rm -f -- "$tmp"
            rm -f /tmp/atomic.err
            log_error "atomic_write: EXDEV cross-device rename"
            return "$EXIT_INTERNAL_ERROR"
        fi
        rm -f -- "$tmp"
        rm -f /tmp/atomic.err
        log_error "atomic_write: rename failed"
        return "$EXIT_ERRORED"
    fi
    rm -f /tmp/atomic.err

    # fsync parent directory.
    if ! fsync_dir "$dir"; then
        log_error "atomic_write: fsync dir failed"
        return "$EXIT_ERRORED"
    fi

    return 0
}
