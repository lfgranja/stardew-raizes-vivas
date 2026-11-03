#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
ROOT="$(cd -- "${SCRIPT_DIR}/../.." && pwd -P)"
cd "$ROOT"

# Run the python consistency checker.
python3 scripts/tests/spec-consistency.py

# C-SCN-36 mirror check: data-model.md required-field table == schema required arrays.
python3 - <<'PY'
import json, re, sys
root = "."
spec = "specs/010-headless-verification-runner"
dm_path = f"{spec}/data-model.md"
schema_path = f"{spec}/contracts/scenario.schema.json"
dm = open(dm_path, encoding="utf-8").read()
schema = json.load(open(schema_path, encoding="utf-8"))

def section(text, start, end):
    m = re.search(re.escape(start) + r"(.*?)" + re.escape(end), text, re.S)
    return m.group(0) if m else ""

def backticked(cell):
    return set(re.findall(r"`([^`]+)`", cell))

mirror = section(dm, "#### Nested required-field mirror", "#### Absence vs. null vs. default")
rows = {}
for line in mirror.splitlines():
    if not line.startswith("|") or line.startswith("| Object path") or line.startswith("|----"):
        continue
    cells = [c.strip() for c in line.strip().strip("|").split("|")]
    if len(cells) >= 2:
        rows[cells[0]] = cells[1]

# root
root_required = set(schema.get("required", []))
root_cell = rows.get("`#` (root)", "")
if backticked(root_cell) != root_required:
    print("C-SCN-36 FAIL: root required mismatch", file=sys.stderr)
    print("missing", sorted(root_required - backticked(root_cell)), file=sys.stderr)
    print("extra", sorted(backticked(root_cell) - root_required), file=sys.stderr)
    sys.exit(1)

# environment
env_required = set(schema.get("properties", {}).get("environment", {}).get("required", []))
env_cell = rows.get("`environment`", "")
if backticked(env_cell) != env_required:
    print("C-SCN-36 FAIL: environment required mismatch", file=sys.stderr)
    sys.exit(1)

print("C-SCN-36: OK")
PY
exit 0
