#!/usr/bin/env python3
"""Validate MDMV 035/039 telemetry JSONL (stdlib, no network).

Exit codes:
  0 = valid (or warnings only, unless --strict/--strict-redaction escalates)
  2 = structural validation errors
  3 = usage/file error
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

REQUIRED = {"schema_version", "event_id", "ts", "run_id", "category", "action"}
CATEGORIES = {"run", "tool", "gate", "policy", "delivery", "state", "metrics", "sdd"}
GATE_RESULTS = {
    "pass", "fail", "error", "escalated", "budget_exceeded",
    "policy_blocked", "needs_human", "no-op",
}
BLAST = {"low", "medium", "high", "critical", "unknown"}
SCHEMA_RE = re.compile(r"^035\.[0-9]+\.[0-9]+$")
# Heuristic only; never print matched secret value.
SECRET_PATTERNS = [
    ("private_key_pem", re.compile(r"-----BEGIN [A-Z ]*PRIVATE KEY-----")),
    ("github_token", re.compile(r"\bgh[pousr]_[A-Za-z0-9]{20,}")),
    ("aws_access_key", re.compile(r"\bAKIA[0-9A-Z]{16}\b")),
    ("bearer_token", re.compile(r"\bBearer\s+[A-Za-z0-9\-._~+/]+=*", re.I)),
    ("password_kv", re.compile(r"\b(password|passwd|pwd)\s*[:=]\s*\S+", re.I)),
    ("secret_kv", re.compile(r"\b(secret|token|api[_-]?key|access[_-]?key)\s*[:=]\s*\S+", re.I)),
]


@dataclass
class Finding:
    level: str  # error|warning
    code: str
    line: int
    message: str


@dataclass
class Report:
    findings: list[Finding] = field(default_factory=list)
    events: int = 0

    def add(self, level: str, code: str, line: int, message: str) -> None:
        self.findings.append(Finding(level, code, line, message))

    @property
    def errors(self) -> list[Finding]:
        return [f for f in self.findings if f.level == "error"]

    @property
    def warnings(self) -> list[Finding]:
        return [f for f in self.findings if f.level == "warning"]


def scan_strings(obj: Any, path: str = "$") -> list[tuple[str, str]]:
    out: list[tuple[str, str]] = []
    if isinstance(obj, dict):
        for k, v in obj.items():
            out.extend(scan_strings(v, f"{path}.{k}"))
    elif isinstance(obj, list):
        for i, v in enumerate(obj):
            out.extend(scan_strings(v, f"{path}[{i}]"))
    elif isinstance(obj, str):
        out.append((path, obj))
    return out


def check_redaction(obj: dict[str, Any], line: int, rep: Report, strict: bool) -> None:
    level = "error" if strict else "warning"
    for path, text in scan_strings(obj):
        if len(text) > 4000:
            rep.add("warning", "REDACT_LONG_STRING", line, f"{path}: very long string ({len(text)} chars) — verify redaction")
        for name, pat in SECRET_PATTERNS:
            if pat.search(text):
                rep.add(level, f"REDACT_{name.upper()}", line, f"{path}: possible secret pattern '{name}' detected (value not shown)")


def validate_event(obj: Any, line: int, rep: Report, strict: bool, strict_redaction: bool) -> None:
    if not isinstance(obj, dict):
        rep.add("error", "JSON_OBJECT", line, "each JSONL line must be a JSON object")
        return
    missing = REQUIRED - set(obj)
    if missing:
        rep.add("error", "REQUIRED_FIELDS", line, f"missing required fields: {sorted(missing)}")
    sv = obj.get("schema_version")
    if isinstance(sv, str) and not SCHEMA_RE.match(sv):
        rep.add("error", "SCHEMA_VERSION", line, f"schema_version must match 035.x.y, got {sv!r}")
    elif sv is not None and not isinstance(sv, str):
        rep.add("error", "SCHEMA_VERSION_TYPE", line, "schema_version must be string")
    cat = obj.get("category")
    if cat is not None and cat not in CATEGORIES:
        rep.add("warning", "CATEGORY_UNKNOWN", line, f"unknown category {cat!r} (expected 035 categories)")
    gr = obj.get("gate_result")
    if gr is not None and gr not in GATE_RESULTS:
        rep.add("error", "GATE_RESULT", line, f"invalid gate_result {gr!r}")
    br = obj.get("blast_radius_class")
    if br is not None and br not in BLAST:
        rep.add("warning", "BLAST_RADIUS", line, f"unknown blast_radius_class {br!r}")
    ce = obj.get("corrects_event_id")
    if ce is not None and not isinstance(ce, str):
        rep.add("error", "CORRECTS_EVENT_ID", line, "corrects_event_id must be string when present")
    metrics = obj.get("metrics")
    if metrics is not None and not isinstance(metrics, dict):
        rep.add("error", "METRICS_TYPE", line, "metrics must be object")
    elif isinstance(metrics, dict):
        cost = metrics.get("cost_estimate")
        if isinstance(cost, dict):
            if cost.get("is_billing") is True:
                rep.add("warning", "COST_BILLING", line, "cost_estimate.is_billing=true requires explicit billing evidence; treat as NV unless proven")
            if not {"source", "method", "confidence"}.issubset(cost):
                rep.add("warning", "COST_FIELDS", line, "cost_estimate should include source, method, confidence, is_billing")
    step = obj.get("step")
    if isinstance(step, dict):
        ec = step.get("exit_code")
        if ec is not None and not isinstance(ec, int):
            rep.add("warning", "EXIT_CODE_TYPE", line, "step.exit_code should be integer or null")
    check_redaction(obj, line, rep, strict=strict_redaction)
    if strict and isinstance(metrics, dict) and "cost_estimate" not in metrics and cat in {"metrics", "gate"}:
        rep.add("warning", "COST_MISSING", line, "gate/metrics event without cost_estimate (optional, but recommended in 039)")


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="Validate MDMV 035/039 telemetry JSONL (no network).")
    ap.add_argument("path", help="JSONL file path")
    ap.add_argument("--strict", action="store_true", help="extra warnings for optional 039 fields")
    ap.add_argument("--strict-redaction", action="store_true", help="treat redaction heuristics as errors")
    ap.add_argument("--json", action="store_true", help="machine-readable JSON report to stdout")
    args = ap.parse_args(argv)

    p = Path(args.path)
    if not p.is_file():
        print(f"telemetry-validate: file not found: {p}", file=sys.stderr)
        return 3
    rep = Report()
    try:
        lines = p.read_text(encoding="utf-8").splitlines()
    except OSError as e:
        print(f"telemetry-validate: read error: {e}", file=sys.stderr)
        return 3
    seen_ids: set[str] = set()
    for i, raw in enumerate(lines, start=1):
        if not raw.strip():
            continue
        rep.events += 1
        try:
            obj = json.loads(raw)
        except json.JSONDecodeError as e:
            rep.add("error", "JSON_PARSE", i, f"invalid JSON: {e.msg} at col {e.colno}")
            continue
        validate_event(obj, i, rep, strict=args.strict, strict_redaction=args.strict_redaction)
        if isinstance(obj, dict):
            eid = obj.get("event_id")
            if isinstance(eid, str):
                if eid in seen_ids:
                    rep.add("warning", "EVENT_ID_DUP", i, f"duplicate event_id {eid!r} (append-only streams should keep unique ids)")
                seen_ids.add(eid)

    out = {
        "path": str(p),
        "events": rep.events,
        "errors": [f.__dict__ for f in rep.errors],
        "warnings": [f.__dict__ for f in rep.warnings],
        "ok": len(rep.errors) == 0,
    }
    if args.json:
        print(json.dumps(out, ensure_ascii=False, indent=2))
    else:
        status = "PASS" if out["ok"] else "FAIL"
        print(f"telemetry-validate: {status} events={rep.events} errors={len(rep.errors)} warnings={len(rep.warnings)} path={p}")
        for f in rep.errors[:80]:
            print(f"  ERROR L{f.line} [{f.code}] {f.message}")
        for f in rep.warnings[:80]:
            print(f"  WARN  L{f.line} [{f.code}] {f.message}")
        if len(rep.errors) > 80 or len(rep.warnings) > 80:
            print("  ... truncated; use --json for full report")
    return 0 if out["ok"] else 2


if __name__ == "__main__":
    raise SystemExit(main())
