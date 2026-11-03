#!/usr/bin/env bash
#
# scripts/lib/logging.sh — JSON Lines audit/application logging with redaction.
#

if [[ -n "${_LR_LOGGING_SH_LOADED:-}" ]]; then
    return 0 2>/dev/null || exit 0
fi
_LR_LOGGING_SH_LOADED=1

source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

_LR_LOG_DIR="${RUNNER_LOG_DIR:-}"

_redact_python() {
    local data="${1:-}"
    python3 - "$data" <<'PY'
import re, sys
text = sys.argv[1]
patterns = [
    r'(?i)bearer\s+[A-Za-z0-9._~+/=-]+',
    r'(?i)(token|secret|password|passwd|api[_-]?key)\s*[:=]\s*\S+',
    r'\bgh[pousr]_[A-Za-z0-9]{20,}\b',
    r'\bgithub_pat_[A-Za-z0-9_]{20,}\b',
    r'\bAKIA[0-9A-Z]{16}\b',
    r'\beyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\b',
    r'\b[0-9a-fA-F]{32,}\b',
    r'\b[A-Za-z0-9+/]{40,}={0,2}\b',
    r'https?://[^\s]*[?&](token|access_token|api_key|key|secret)=[^\s&]+',
]
for p in patterns:
    text = re.sub(p, "[REDACTED]", text)
sys.stdout.write(text)
PY
}

redact() {
    # Redact a string passed as args or stdin.
    if [[ $# -gt 0 ]]; then
        _redact_python "$*"
    else
        local data
        data="$(cat)"
        _redact_python "$data"
    fi
}

redact_secret() { redact "$@"; }

_lr_ts() {
    date -u +%Y-%m-%dT%H:%M:%S.%3NZ
}

# Emit a JSON line. Args: sink level message [extra json fields as key=value]
_emit_json_line() {
    local sink="$1"; shift
    local level="$1"; shift
    local message="$1"; shift
    local ts
    ts="$(_lr_ts)"
    # redact message (base64, url-encoded, hex variants handled)
    local redacted_msg
    redacted_msg="$(_redact_python "$message")"
    python3 - "$sink" "$level" "$ts" "$redacted_msg" "$*" <<'PY'
import json, sys
sink, level, ts, msg, extra = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4], sys.argv[5]
obj = {"ts": ts, "sink": sink, "level": level, "message": msg}
# extra: space-separated key=value pairs
for pair in extra.split():
    if "=" in pair:
        k, v = pair.split("=", 1)
        # redact value
        obj[k] = v
# redact all string values recursively? do shallow
def redact_val(x):
    if isinstance(x, str):
        return x  # already redacted at bash side for msg; values redacted below
    return x
# Apply redaction patterns to every string value
import re
pats = [r'(?i)bearer\s+[A-Za-z0-9._~+/=-]+', r'\bgh[pousr]_[A-Za-z0-9]{20,}\b',
        r'\b[0-9a-fA-F]{32,}\b', r'\b[A-Za-z0-9+/]{40,}={0,2}\b']
def redact_str(s):
    for p in pats:
        s = re.sub(p, "[REDACTED]", s)
    return s
for k, v in list(obj.items()):
    if isinstance(v, str):
        obj[k] = redact_str(v)
print(json.dumps(obj, separators=(",", ":")))
PY
}

log_audit_event() {
    local event="${1:-audit}"; shift || true
    local line
    line="$(_emit_json_line "audit" "info" "$event" "$*")"
    printf '%s\n' "$line"
    _append_sink "audit" "$line"
}

audit_event() { log_audit_event "$@"; }

log_application() {
    local level="${1:-info}"; shift || true
    local message="${*:-}"
    _emit_json_line "application" "$level" "$message" ""
}

app_log() { log_application "$@"; }

log_success() {
    local message="${*:-}"
    _emit_json_line "success" "info" "$message" ""
}

log_failure() {
    local message="${*:-}"
    _emit_json_line "failure" "error" "$message" ""
}

log_verbose() {
    if [[ "${RUNNER_VERBOSE:-0}" == "1" ]]; then
        local message="${*:-}"
        _emit_json_line "verbose" "debug" "$message" ""
    fi
}

log_ci() {
    local message="${*:-}"
    _emit_json_line "ci" "info" "$message" ""
}

log_junit() {
    local message="${*:-}"
    _emit_json_line "junit" "info" "$message" ""
}

log_allure() {
    local message="${*:-}"
    _emit_json_line "allure" "info" "$message" ""
}

log_error_sink() {
    local message="${*:-}"
    _emit_json_line "error" "error" "$message" ""
}

# Alias used by other libs.
log_error_json() { log_error_sink "$@"; }

# Append to sink files if RUNNER_LOG_DIR is set (best-effort).
_append_sink() {
    local sink="$1"; local line="$2"
    if [[ -n "$_LR_LOG_DIR" && -d "$_LR_LOG_DIR" ]]; then
        printf '%s\n' "$line" >> "${_LR_LOG_DIR}/${sink}.jsonl" 2>/dev/null || true
    fi
}
