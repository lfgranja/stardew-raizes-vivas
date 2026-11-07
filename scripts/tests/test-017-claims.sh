#!/usr/bin/env bash
#
# scripts/tests/test-017-claims.sh — claim-register validator for spec 017.
#
# Validates specs/017-joja-agrochemical-tradeoff/verification/claim-records.md:
#
#   (a) every claim-register row's evidence_class is one of:
#       automated | host-only-machine-pass | human-verified |
#       d1-farmhand-convergence | qualitative-follow-up | planned-run-not-executed
#   (b) status is one of: pending | complete | blocked | infeasible
#   (c) for rows with status `complete`, the source_artifact_path column's
#       first path-like token EXISTS (URLs skipped; paths after commas skipped;
#       heuristic: first backtick-quoted token ending in .md/.json/.log/.cs, or
#       containing an evidence-bundle-* directory, or containing a Tests/ dir)
#   (d) rows whose claim mentions D1/farmhand OR "Human `verified`" (human +
#       verified) OR "SC-004" MUST have status != complete
#   (e) prints PASS/FAIL with counts; exits non-zero on failure
#
# Self-contained: python3 does the markdown parsing. No dotnet, no game, no
# network. Read-only on the register.

set -euo pipefail

REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd -P)"
FEATURE_DIR="$REPO_ROOT/specs/017-joja-agrochemical-tradeoff"
RECORDS="$FEATURE_DIR/verification/claim-records.md"

if [[ ! -f "$RECORDS" ]]; then
  printf 'FAIL: claim register not found at %s\n' "$RECORDS" >&2
  exit 2
fi

python3 - "$RECORDS" "$REPO_ROOT" "$FEATURE_DIR" <<'PY'
import re
import sys
from pathlib import Path

records_path, repo_root, feature_dir = sys.argv[1], Path(sys.argv[2]), Path(sys.argv[3])
text = Path(records_path).read_text(encoding="utf-8")

VALID_CLASSES = {
    "automated",
    "host-only-machine-pass",
    "human-verified",
    "d1-farmhand-convergence",
    "qualitative-follow-up",
    "planned-run-not-executed",
}
VALID_STATUSES = {"pending", "complete", "blocked", "infeasible"}

# --- isolate the "Claim register" table section ---------------------------
m = re.search(r"^##\s+Claim register\s*$", text, re.MULTILINE)
if not m:
    print("FAIL: '## Claim register' section not found")
    sys.exit(1)
section = text[m.end():]
nxt = re.search(r"^##\s+", section, re.MULTILINE)
if nxt:
    section = section[: nxt.start()]

rows = []
for raw in section.splitlines():
    line = raw.strip()
    if not line.startswith("|"):
        continue
    cells = [c.strip() for c in line.split("|")]
    # leading/trailing pipes produce empty first/last cells
    if cells and cells[0] == "":
        cells = cells[1:]
    if cells and cells[-1] == "":
        cells = cells[:-1]
    if len(cells) < 5:
        continue
    # separator row (---|---|...)
    if all(re.fullmatch(r":?-{3,}:?", c or "---") for c in cells):
        continue
    # header row
    if cells[0].lower() == "claim" and "evidence" in cells[1].lower():
        continue
    rows.append(cells[:5])

if not rows:
    print("FAIL: no claim-register rows parsed")
    sys.exit(1)

def first_backtick_token(cell):
    m2 = re.search(r"`([^`]+)`", cell)
    return m2.group(1).strip() if m2 else cell.strip()

PATH_RE = re.compile(r"^[A-Za-z0-9_.\-][A-Za-z0-9_./\-]*\.(md|json|log|cs|txt|yml|yaml)$")

def path_candidates(source_cell):
    for tok in re.findall(r"`([^`]+)`", source_cell):
        tok = tok.strip()
        if "://" in tok:
            continue  # skip URLs
        if PATH_RE.match(tok):
            yield tok
        elif "evidence-bundle-" in tok:
            yield tok.rstrip("/")
        elif "/Tests/" in tok or tok.startswith("LivingRoots.Tests"):
            yield tok
        # anything else is skipped (commas, prose, commands)

def exists(path_tok):
    if (repo_root / path_tok).exists():
        return True
    if (feature_dir / path_tok).exists():
        return True
    return False

failures = []
class_counts = {}
status_counts = {}
checked_paths = 0

for i, (claim, evidence, source, status, scope) in enumerate(rows, 1):
    ev = first_backtick_token(evidence).lower()
    st = first_backtick_token(status).lower()
    class_counts[ev] = class_counts.get(ev, 0) + 1
    status_counts[st] = status_counts.get(st, 0) + 1

    # (a) evidence_class membership
    if ev not in VALID_CLASSES:
        failures.append(f"row {i}: evidence_class {ev!r} not in the six normalized classes")

    # (b) status membership
    if st not in VALID_STATUSES:
        failures.append(f"row {i}: status {st!r} not in pending|complete|blocked|infeasible")

    # (c) complete rows: first path-like source token must exist
    if st == "complete":
        cand = next(path_candidates(source), None)
        if cand is None:
            failures.append(f"row {i}: status complete but no path-like token found in source_artifact_path")
        else:
            checked_paths += 1
            if not exists(cand):
                failures.append(f"row {i}: complete row's source path {cand!r} does not exist (repo root or feature dir)")

    # (d) D1/human-verified/SC-004 rows must not be complete
    cl = claim.lower()
    mentions_d1 = ("d1" in cl) or ("farmhand" in cl)
    mentions_human_verified = ("human" in cl) and ("verified" in cl)
    mentions_sc004 = "sc-004" in cl
    if (mentions_d1 or mentions_human_verified or mentions_sc004) and st == "complete":
        failures.append(f"row {i}: claim mentions D1/farmhand/Human-verified/SC-004 but status is complete")

print(f"claim-records validator: {records_path}")
print(f"rows parsed: {len(rows)}")
print(f"evidence_class counts: {dict(sorted(class_counts.items()))}")
print(f"status counts: {dict(sorted(status_counts.items()))}")
print(f"complete-row source paths checked: {checked_paths}")

if failures:
    print(f"FAIL: {len(failures)} problem(s) found")
    for f in failures:
        print(f"  - {f}")
    sys.exit(1)

print("PASS: all claim-register rows satisfy the normalized schema and honesty rules")
PY
