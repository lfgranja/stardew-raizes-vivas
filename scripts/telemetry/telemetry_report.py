#!/usr/bin/env python3
"""Summarize MDMV 035/039 telemetry JSONL (stdlib, no network).

Reports errors, warnings/avisos, failures, cost estimates (labeled), and bottlenecks.
Costs are NEVER treated as billing unless explicitly evidenced outside this tool (NV).
"""
from __future__ import annotations

import argparse
import json
import sys
from collections import Counter, defaultdict
from pathlib import Path
from typing import Any

FAIL_RESULTS = {"fail", "error"}
WARN_RESULTS = {"escalated", "budget_exceeded", "policy_blocked", "needs_human"}


def load_events(path: Path) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    events: list[dict[str, Any]] = []
    parse_errors: list[dict[str, Any]] = []
    for i, raw in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        if not raw.strip():
            continue
        try:
            obj = json.loads(raw)
        except json.JSONDecodeError as e:
            parse_errors.append({"line": i, "error": f"{e.msg} col {e.colno}"})
            continue
        if isinstance(obj, dict):
            events.append({"_line": i, **obj})
        else:
            parse_errors.append({"line": i, "error": "line is not a JSON object"})
    return events, parse_errors


def summarize(events: list[dict[str, Any]], parse_errors: list[dict[str, Any]]) -> dict[str, Any]:
    by_cat = Counter(e.get("category", "unknown") for e in events)
    by_gate = Counter(e.get("gate_result") for e in events if e.get("gate_result") is not None)
    by_repo = Counter(e.get("target_repo", "(unspecified)") for e in events)
    by_run = Counter(e.get("run_id", "(unspecified)") for e in events)

    failures: list[dict[str, Any]] = []
    warnings: list[dict[str, Any]] = []
    bottlenecks: list[dict[str, Any]] = []
    costs: list[dict[str, Any]] = []

    for e in events:
        line = e.get("_line")
        rid = e.get("run_id")
        gr = e.get("gate_result")
        step = e.get("step") if isinstance(e.get("step"), dict) else {}
        ec = step.get("exit_code")
        if gr in FAIL_RESULTS or (isinstance(ec, int) and ec != 0):
            failures.append({"line": line, "run_id": rid, "event_id": e.get("event_id"), "gate_result": gr, "exit_code": ec, "action": e.get("action"), "summary_redacted": step.get("summary_redacted")})
        if gr in WARN_RESULTS:
            warnings.append({"line": line, "run_id": rid, "event_id": e.get("event_id"), "gate_result": gr, "action": e.get("action")})
            bottlenecks.append({"kind": "gate", "gate_result": gr, "run_id": rid, "action": e.get("action"), "line": line})
        metrics = e.get("metrics") if isinstance(e.get("metrics"), dict) else {}
        cost = metrics.get("cost_estimate") if isinstance(metrics.get("cost_estimate"), dict) else None
        if cost:
            billing = cost.get("is_billing")
            costs.append({
                "line": line, "run_id": rid, "event_id": e.get("event_id"),
                "source": cost.get("source"), "method": cost.get("method"), "confidence": cost.get("confidence"),
                "is_billing": billing, "amount": cost.get("amount"), "currency": cost.get("currency"),
                "notes_redacted": cost.get("notes_redacted"),
                "nv": "billing/evidence NV" if billing is not False else "labeled estimate (not billing)",
            })
            if billing is True:
                warnings.append({"line": line, "run_id": rid, "event_id": e.get("event_id"), "gate_result": "needs_human", "action": "cost_is_billing_requires_evidence"})
        if "corrects_event_id" in e:
            warnings.append({"line": line, "run_id": rid, "event_id": e.get("event_id"), "gate_result": "no-op", "action": f"correction_of={e.get('corrects_event_id')}"})

    bottleneck_counts = Counter((b["gate_result"], b.get("action")) for b in bottlenecks)
    return {
        "events": len(events),
        "parse_errors": parse_errors,
        "errors": [{"kind": "parse", **x} for x in parse_errors],
        "warnings_avisos": warnings,
        "failures": failures,
        "costs_labeled_only": costs,
        "bottlenecks": [{"gate_result": gr, "action": act, "count": n} for (gr, act), n in bottleneck_counts.most_common()],
        "by_category": dict(by_cat),
        "by_gate_result": {k: v for k, v in by_gate.items() if k is not None},
        "by_target_repo": dict(by_repo),
        "by_run_id": dict(by_run),
        "interpretation": {
            "costs": "NV/faturamento não comprovado por este relatório; apenas cost_estimate rotulado.",
            "remote_ci": "NV salvo registrado nos eventos.",
            "redaction": "use validate_telemetry.py para heurística de segredos.",
        },
    }


def to_markdown(s: dict[str, Any], path: Path) -> str:
    lines = [f"# Telemetry report — `{path}`", "", f"- events: **{s['events']}**", f"- parse errors: **{len(s['parse_errors'])}**", f"- failures: **{len(s['failures'])}**", f"- warnings/avisos: **{len(s['warnings_avisos'])}**", f"- cost estimates (labeled): **{len(s['costs_labeled_only'])}**", ""]
    lines += ["## Failures", ""]
    if s["failures"]:
        lines.append("| line | run_id | gate/exit | action | summary |")
        lines.append("|---:|---|---|---|---|")
        for f in s["failures"][:50]:
            summ = (f.get("summary_redacted") or "").replace("|", "\\|")[:120]
            lines.append(f"| {f['line']} | {f.get('run_id','')} | {f.get('gate_result') or ''}/{f.get('exit_code') if f.get('exit_code') is not None else ''} | {f.get('action','')} | {summ} |")
    else:
        lines.append("- (none)")
    lines += ["", "## Warnings / avisos", ""]
    if s["warnings_avisos"]:
        for w in s["warnings_avisos"][:80]:
            lines.append(f"- L{w['line']} run={w.get('run_id')} {w.get('gate_result')} action={w.get('action')}")
    else:
        lines.append("- (none)")
    lines += ["", "## Costs (labeled estimates only; billing NV)", ""]
    if s["costs_labeled_only"]:
        for c in s["costs_labeled_only"][:50]:
            lines.append(f"- L{c['line']} run={c.get('run_id')} source={c.get('source')} method={c.get('method')} conf={c.get('confidence')} is_billing={c.get('is_billing')} amount={c.get('amount')} {c.get('currency') or ''} — {c.get('nv')}")
    else:
        lines.append("- (none) — opacidade de custo se não instrumentado; usar 035 cost_estimate rotulado.")
    lines += ["", "## Bottlenecks", ""]
    if s["bottlenecks"]:
        for b in s["bottlenecks"][:30]:
            lines.append(f"- {b['gate_result']} / {b['action']}: {b['count']}")
    else:
        lines.append("- (none observed)")
    lines += ["", "## Counts", "", f"- by_category: `{json.dumps(s['by_category'], ensure_ascii=False)}`", f"- by_gate_result: `{json.dumps(s['by_gate_result'], ensure_ascii=False)}`", f"- by_target_repo: `{json.dumps(s['by_target_repo'], ensure_ascii=False)}`", ""]
    lines += ["## Interpretation / NV", "", f"- {s['interpretation']['costs']}", f"- {s['interpretation']['remote_ci']}", f"- {s['interpretation']['redaction']}", ""]
    return "\n".join(lines)


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="Summarize MDMV 035/039 telemetry JSONL (no network).")
    ap.add_argument("path")
    ap.add_argument("--format", choices=["markdown", "json"], default="markdown")
    ap.add_argument("-o", "--output")
    args = ap.parse_args(argv)
    p = Path(args.path)
    if not p.is_file():
        print(f"telemetry-report: file not found: {p}", file=sys.stderr)
        return 3
    events, parse_errors = load_events(p)
    s = summarize(events, parse_errors)
    out = json.dumps(s, ensure_ascii=False, indent=2) if args.format == "json" else to_markdown(s, p)
    if args.output:
        Path(args.output).write_text(out + "\n", encoding="utf-8")
    else:
        print(out)
    return 2 if parse_errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
