# scripts/lib/retention.sh — durable-store retention / WORM (spec 010, T061).

if [ -n "${_LR_RETENTION_LOADED:-}" ]; then
    return 0 2>/dev/null || exit 0
fi
_LR_RETENTION_LOADED=1

# shellcheck source=common.sh
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

_RETENTION_MIN_DAYS_DEFAULT=365
_RETENTION_APP_LOG_DAYS=30

retention_now_iso() { lr_now_iso; }

# retention_receipt <bundle_dir>  -> prints receipt id, writes receipt file (append-only).
retention_receipt() {
    if [[ $# -lt 1 ]]; then
        log_error "retention_receipt: usage: retention_receipt <bundle_dir>"
        return "$EXIT_USAGE_ERROR"
    fi
    local bundle="$1"
    mkdir -p "$bundle" 2>/dev/null || true
    local ts id receipt
    ts="$(lr_now_iso)"
    id="receipt-$(date -u +%s)-$$"
    receipt="$bundle/retention-receipt.json"
    python3 - "$receipt" "$id" "$ts" "$bundle" <<'PY'
import json, os, sys
path, rid, ts, bundle = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
entry = {"receiptId": rid, "createdAt": ts, "bundle": bundle, "retentionDays": 365}
# append-only: read existing list
existing = []
if os.path.isfile(path):
    try:
        with open(path) as f:
            existing = json.load(f)
        if not isinstance(existing, list):
            existing = [existing]
    except Exception:
        existing = []
existing.append(entry)
tmp = path + ".tmp"
with open(tmp, "w") as f:
    json.dump(existing, f, indent=2)
    f.write("\n")
os.replace(tmp, path)
print(rid)
PY
    return 0
}

# retention_record_timestamp <artifact_path> [store_dir] -> writes per-artifact retention timestamp.
retention_record_timestamp() {
    local artifact="$1"
    local store="${2:-${RUNNER_RETENTION_STORE:-}}"
    if [[ -z "$store" ]]; then
        store="${RUNNER_LOG_DIR:-/tmp}/retention"
    fi
    mkdir -p "$store" 2>/dev/null || true
    local ts
    ts="$(lr_now_iso)"
    python3 - "$store/retention-timestamps.json" "$artifact" "$ts" <<'PY'
import json, os, sys
path, art, ts = sys.argv[1], sys.argv[2], sys.argv[3]
data = {}
if os.path.isfile(path):
    try:
        with open(path) as f:
            data = json.load(f)
    except Exception:
        data = {}
data[art] = {"retentionUntil": None, "recordedAt": ts}
# default retention 365 days
import datetime
try:
    dt = datetime.datetime.fromisoformat(ts.replace("Z","+00:00"))
    until = dt + datetime.timedelta(days=365)
    data[art]["retentionUntil"] = until.strftime("%Y-%m-%dT%H:%M:%S.%f")[:-3] + "Z"
except Exception:
    pass
tmp = path + ".tmp"
with open(tmp, "w") as f:
    json.dump(data, f, indent=2)
    f.write("\n")
os.replace(tmp, path)
PY
    return 0
}

# retention_check_expiry <created_iso> <min_days>  -> 0 if expired (now >= created+min_days), 1 otherwise
retention_check_expiry() {
    if [[ $# -lt 2 ]]; then
        log_error "retention_check_expiry: usage: <created_iso> <min_days>"
        return "$EXIT_USAGE_ERROR"
    fi
    python3 - "$1" "$2" <<'PY'
import sys, datetime
created = sys.argv[1]
min_days = int(sys.argv[2])
try:
    dt = datetime.datetime.fromisoformat(created.replace("Z","+00:00"))
except Exception:
    sys.exit(2)
now = datetime.datetime.now(datetime.timezone.utc)
expiry = dt + datetime.timedelta(days=min_days)
sys.exit(0 if now >= expiry else 1)
PY
}

# retention_export <src> <dest>  -> append-only export (WORM): copy src to dest if not present; never overwrite.
retention_export() {
    if [[ $# -lt 2 ]]; then
        log_error "retention_export: usage: <src> <dest>"
        return "$EXIT_USAGE_ERROR"
    fi
    local src="$1" dest="$2"
    if [[ ! -e "$src" ]]; then
        log_error "retention_export: source missing: $src"
        return "$EXIT_ERRORED"
    fi
    mkdir -p "$(dirname "$dest")" 2>/dev/null || true
    if [[ -e "$dest" ]]; then
        # append-only: do not overwrite
        log_warn "retention_export: destination exists, skipping (WORM): $dest"
        return 0
    fi
    if [[ -d "$src" ]]; then
        cp -a "$src" "$dest"
    else
        cp -p "$src" "$dest"
    fi
    return 0
}

# retention_delete_guarded <path> <created_iso> [min_days] [approval_env_var]
# Refuses deletion before expiry; requires approval after expiry; writes audit event.
retention_delete_guarded() {
    if [[ $# -lt 2 ]]; then
        log_error "retention_delete_guarded: usage: <path> <created_iso> [min_days]"
        return "$EXIT_USAGE_ERROR"
    fi
    local path="$1" created="$2" min_days="${3:-365}"
    # Check expiry
    if ! retention_check_expiry "$created" "$min_days"; then
        log_error "retention_delete_guarded: refusal: before expiry ($min_days days)"
        # audit event
        audit_event "retention_delete_refused" "path=$path" "reason=before-expiry" "created=$created"
        return "$EXIT_POLICY"
    fi
    # Approval gate
    if [[ "${RUNNER_DELETION_APPROVED:-0}" != "1" && "${RETENTION_DELETE_APPROVED:-0}" != "1" ]]; then
        log_error "retention_delete_guarded: refusal: approval required"
        audit_event "retention_delete_refused" "path=$path" "reason=no-approval" "created=$created"
        return "$EXIT_POLICY"
    fi
    # Perform deletion
    rm -rf -- "$path"
    audit_event "retention_delete_approved" "path=$path" "created=$created"
    return 0
}

export -f retention_receipt retention_record_timestamp retention_check_expiry \
          retention_export retention_delete_guarded
