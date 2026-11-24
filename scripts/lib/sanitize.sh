#!/usr/bin/env bash
#
# scripts/lib/sanitize.sh — path sanitization matching ModDataService.SanitizePathSegments.
#

if [[ -n "${_LR_SANITIZE_SH_LOADED:-}" ]]; then
    return 0 2>/dev/null || exit 0
fi
_LR_SANITIZE_SH_LOADED=1

source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

sanitize_path() {
    if [[ $# -ne 1 ]]; then
        log_error "sanitize_path: usage: sanitize_path PATH"
        return "$EXIT_USAGE_ERROR"
    fi
    local input="$1"
    local tmp_py out rc
    tmp_py="$(mktemp)" || return "$EXIT_ERRORED"
    cat > "$tmp_py" <<'PYEOF'
import sys, unicodedata, os

raw = sys.argv[1]

def fail(code, msg):
    sys.stderr.write(msg + "\n")
    sys.exit(code)

if raw == "":
    fail(14, "empty path")

for ch in raw:
    if ord(ch) < 0x20 or ord(ch) == 0x7f:
        fail(14, "control character")

if "\\" in raw:
    fail(14, "backslash")

nfc = unicodedata.normalize("NFC", raw)
if unicodedata.normalize("NFC", nfc) != nfc:
    fail(14, "normalization")

if nfc.startswith("/"):
    fail(14, "absolute")

if len(nfc) >= 2 and nfc[0].isascii() and nfc[0].isalpha() and nfc[1] == ":":
    fail(14, "drive letter")

if "://" in nfc:
    fail(14, "uri")

for ch in nfc:
    if ord(ch) < 128:
        continue
    if 0x00C0 <= ord(ch) <= 0x024F:
        continue
    if unicodedata.combining(ch):
        continue
    if ch in ("/", ".", "-", "_"):
        continue
    fail(14, "homoglyph")

segments = [s for s in nfc.split("/") if s != ""]

max_consec = 0
for seg in segments:
    consec = 0
    for c in seg:
        if c == ".":
            consec += 1
            if consec > max_consec:
                max_consec = consec
        else:
            consec = 0
    if max_consec > 2:
        fail(14, "traversal")
    if seg == "..":
        fail(14, "traversal")

try:
    if os.path.exists(nfc):
        real = os.path.realpath(nfc)
        root = os.path.realpath(os.getcwd())
        if real != root and not real.startswith(root + os.sep):
            fail(14, "symlink escape")
except Exception:
    fail(19, "internal")

valid = []
for seg in segments:
    if seg == ".":
        continue
    if seg == "..":
        fail(14, "traversal")
    if seg.strip() == "":
        continue
    valid.append(seg)

if not valid:
    fail(14, "empty result")

out = "/".join(valid)
out = unicodedata.normalize("NFC", out)
sys.stdout.write(out)
PYEOF
    out="$(python3 "$tmp_py" "$input")"
    rc=$?
    rm -f "$tmp_py"
    if (( rc != 0 )); then
        return $rc
    fi
    printf '%s\n' "$out"
    return 0
}

validate_path() {
    sanitize_path "$@" >/dev/null
}

export -f sanitize_path validate_path
