#!/usr/bin/env python3
"""Append a MDMV 035.1.0 telemetry event to a JSONL file (stdlib, no network).

Safety: append-only; never rewrites existing lines. Redaction heuristic warns on
obvious secret patterns in provided text (never prints matched secret value).
Cost estimates are labeled estimates (is_billing defaults false); billing NV.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import sys
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

SCHEMA_VERSION = "035.1.0"
CATEGORIES = {"run", "tool", "gate", "policy", "delivery", "state", "metrics", "sdd"}
GATE_RESULTS = {
    "pass", "fail", "error", "escalated", "budget_exceeded",
    "policy_blocked", "needs_human", "no-op",
}
BLAST = {"low", "medium", "high", "critical", "unknown"}
SECRET_PATTERNS = [
    ("private_key_pem", re.compile(r"-----BEGIN [A-Z ]*PRIVATE KEY-----")),
    ("github_token", re.compile(r"\bgh[pousr]_[A-Za-z0-9]{20,}")),
    ("aws_access_key", re.compile(r"\bAKIA[0-9A-Z]{16}\b")),
    ("bearer_token", re.compile(r"\bBearer\s+[A-Za-z0-9\-._~+/]+=*", re.I)),
    ("password_kv", re.compile(r"\b(password|passwd|pwd)\s*[:=]\s*\S+", re.I)),
    ("secret_kv", re.compile(r"\b(secret|token|api[_-]?key|access[_-]?key)\s*[:=]\s*\S+", re.I)),
]


def now_iso() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def scan(obj: Any) -> list[str]:
    hits: list[str] = []
    if isinstance(obj, dict):
        for v in obj.values():
            hits.extend(scan(v))
    elif isinstance(obj, list):
        for v in obj:
            hits.extend(scan(v))
    elif isinstance(obj, str):
        for name, pat in SECRET_PATTERNS:
            if pat.search(obj):
                hits.append(name)
    return sorted(set(hits))


def load_json_arg(raw: str | None) -> dict[str, Any] | None:
    if raw is None:
        return None
    try:
        obj = json.loads(raw)
    except json.JSONDecodeError as e:
        print(f"emit_telemetry: invalid JSON argument: {e}", file=sys.stderr)
        raise SystemExit(3)
    if not isinstance(obj, dict):
        print("emit_telemetry: JSON argument must be an object", file=sys.stderr)
        raise SystemExit(3)
    return obj


def next_seq(path: Path, run_id: str) -> int:
    if not path.is_file():
        return 0
    max_seq = -1
    for line in path.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        try:
            obj = json.loads(line)
        except json.JSONDecodeError:
            continue
        if isinstance(obj, dict) and obj.get("run_id") == run_id and isinstance(obj.get("seq"), int):
            max_seq = max(max_seq, obj["seq"])
    return max_seq + 1


def build_event(args: argparse.Namespace) -> dict[str, Any]:
    if args.category not in CATEGORIES:
        print(f"emit_telemetry: unknown category {args.category}", file=sys.stderr)
        raise SystemExit(3)
    if args.gate_result and args.gate_result not in GATE_RESULTS:
        print(f"emit_telemetry: invalid gate_result {args.gate_result}", file=sys.stderr)
        raise SystemExit(3)
    if args.blast_radius and args.blast_radius not in BLAST:
        print(f"emit_telemetry: invalid blast-radius {args.blast_radius}", file=sys.stderr)
        raise SystemExit(3)
    event: dict[str, Any] = {
        "schema_version": SCHEMA_VERSION,
        "event_id": args.event_id or f"evt-{uuid.uuid4().hex[:12]}",
        "ts": args.ts or now_iso(),
        "run_id": args.run_id,
        "seq": args.seq,
        "actor": args.actor,
        "category": args.category,
        "action": args.action,
    }
    if args.state:
        event["state"] = args.state
    if args.gate_result:
        event["gate_result"] = args.gate_result
    if args.blast_radius:
        event["blast_radius_class"] = args.blast_radius
    if args.corrects_event_id:
        event["corrects_event_id"] = args.corrects_event_id
    if args.target_repo:
        event["target_repo"] = args.target_repo
    if args.target_ref:
        event["target_ref"] = args.target_ref
    step: dict[str, Any] = {}
    if args.step_name:
        step["name"] = args.step_name
    if args.command_redacted:
        step["command_redacted"] = args.command_redacted
    if args.exit_code is not None:
        step["exit_code"] = args.exit_code
    if args.summary_redacted:
        step["summary_redacted"] = args.summary_redacted
    if args.artifact_ref:
        step["artifact_ref"] = args.artifact_ref
    if args.gate_result and "status" not in step:
        step["status"] = args.gate_result
    if step:
        event["step"] = step
    sdd = load_json_arg(args.sdd_json)
    if sdd:
        event["sdd"] = sdd
    metrics = load_json_arg(args.metrics_json) or {}
    if args.cost_source or args.cost_method or args.cost_confidence:
        metrics.setdefault("cost_estimate", {})
        ce = metrics["cost_estimate"]
        ce.setdefault("source", args.cost_source or "unspecified")
        ce.setdefault("method", args.cost_method or "unspecified")
        ce.setdefault("confidence", args.cost_confidence or "unknown")
        ce.setdefault("is_billing", False)
        ce.setdefault("amount", None)
        if args.cost_currency:
            ce.setdefault("currency", args.cost_currency)
        if args.cost_notes_redacted:
            ce.setdefault("notes_redacted", args.cost_notes_redacted)
    if args.gate_result:
        metrics.setdefault("gate", {"name": args.step_name or args.action, "result": args.gate_result})
    if metrics:
        event["metrics"] = metrics
    extra = load_json_arg(args.extra_json)
    if extra:
        for k, v in extra.items():
            event.setdefault(k, v)
    return event


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="Append MDMV 035.1.0 telemetry event (no network).")
    ap.add_argument("path", help="JSONL file to append to (created if missing)")
    ap.add_argument("--run-id", required=True)
    ap.add_argument("--category", required=True, choices=sorted(CATEGORIES))
    ap.add_argument("--action", required=True)
    ap.add_argument("--actor", default="system", choices=["orchestrator", "executor", "checker", "human", "system"])
    ap.add_argument("--event-id")
    ap.add_argument("--ts", help="ISO8601 UTC timestamp; default now")
    ap.add_argument("--seq", type=int, help="default: next seq for run_id in file")
    ap.add_argument("--state")
    ap.add_argument("--gate-result", choices=sorted(GATE_RESULTS))
    ap.add_argument("--blast-radius", choices=sorted(BLAST))
    ap.add_argument("--corrects-event-id")
    ap.add_argument("--target-repo")
    ap.add_argument("--target-ref")
    ap.add_argument("--step-name")
    ap.add_argument("--command-redacted")
    ap.add_argument("--exit-code", type=int)
    ap.add_argument("--summary-redacted")
    ap.add_argument("--artifact-ref")
    ap.add_argument("--sdd-json", help='JSON object, e.g. {"spec_ref":"...","stage":"validate"}')
    ap.add_argument("--metrics-json", help="JSON object merged into metrics")
    ap.add_argument("--extra-json", help="JSON object merged at top level (non-overriding)")
    ap.add_argument("--cost-source")
    ap.add_argument("--cost-method")
    ap.add_argument("--cost-confidence", choices=["low", "medium", "high", "unknown"])
    ap.add_argument("--cost-currency")
    ap.add_argument("--cost-notes-redacted")
    ap.add_argument("--allow-redaction-warning", action="store_true", help="append even if redaction heuristic triggers (warning still printed)")
    ap.add_argument("--print-json", action="store_true", help="print appended event JSON")
    args = ap.parse_args(argv)

    path = Path(args.path)
    if args.seq is None:
        args.seq = next_seq(path, args.run_id)
    event = build_event(args)
    hits = scan(event)
    if hits:
        print(f"emit_telemetry: WARNING redaction heuristic matched patterns: {hits} (value not shown). Use --allow-redaction-warning to append anyway, or fix content.", file=sys.stderr)
        if not args.allow_redaction_warning:
            return 2
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("a", encoding="utf-8") as f:
        f.write(json.dumps(event, ensure_ascii=False, separators=(",", ":")) + "\n")
    if args.print_json:
        print(json.dumps(event, ensure_ascii=False, indent=2))
    else:
        print(f"emit_telemetry: appended event_id={event['event_id']} run_id={event['run_id']} seq={event['seq']} -> {path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
