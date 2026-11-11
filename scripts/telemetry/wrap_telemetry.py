#!/usr/bin/env python3
"""Run a command and append MDMV 035.1.0 telemetry (stdlib, no network).

Captures exit code/duration; classifies gate_result (pass/fail/error);
redacts command by accepting an explicit --command-redacted (recommended) or a
coarse argv rendering (never captures stdout/stderr content).
Cost remains labeled estimate (NV billing).
"""
from __future__ import annotations

import argparse
import subprocess
import sys
import time
from pathlib import Path

# local sibling import when run as script
sys.path.insert(0, str(Path(__file__).resolve().parent))
import emit_telemetry as emit  # noqa: E402


def coarse_redact(argv: list[str]) -> str:
    # Keep argv[0] and count only; avoid leaking args that may contain secrets.
    if not argv:
        return "<empty>"
    return f"{argv[0]} <argv:{len(argv)-1} args redacted>"


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="Wrap command and emit 035 telemetry (no network).")
    ap.add_argument("telemetry_jsonl", help="JSONL file to append to")
    ap.add_argument("--run-id", required=True)
    ap.add_argument("--action", required=True)
    ap.add_argument("--category", default="gate", choices=sorted(emit.CATEGORIES))
    ap.add_argument("--actor", default="system", choices=["orchestrator", "executor", "checker", "human", "system"])
    ap.add_argument("--target-repo")
    ap.add_argument("--target-ref")
    ap.add_argument("--step-name")
    ap.add_argument("--command-redacted", help="Required/recommended: redacted command text to record")
    ap.add_argument("--summary-prefix", default="")
    ap.add_argument("--state")
    ap.add_argument("--sdd-json")
    ap.add_argument("--allow-redaction-warning", action="store_true")
    ap.add_argument("--timeout-seconds", type=float)
    ap.add_argument("--cwd")
    # Split on -- so wrapped command is never parsed as tool options.
    raw = list(argv) if argv is not None else sys.argv[1:]
    if "--" in raw:
        idx = raw.index("--")
        tool_argv, cmd = raw[:idx], raw[idx + 1:]
    else:
        tool_argv, cmd = raw, []
    args = ap.parse_args(tool_argv)
    if not cmd:
        print("wrap_telemetry: provide command after --", file=sys.stderr)
        return 3
    command_redacted = args.command_redacted or coarse_redact(cmd)
    step_name = args.step_name or args.action
    t0 = time.monotonic()
    timed_out = False
    try:
        proc = subprocess.run(cmd, cwd=args.cwd, check=False, timeout=args.timeout_seconds)
        exit_code = proc.returncode
    except FileNotFoundError as e:
        duration_ms = int((time.monotonic() - t0) * 1000)
        summary = f"{args.summary_prefix}command not found: {e.filename!r} (redacted context)".strip()
        return _emit_and_propagate(args, command_redacted, step_name, 127, duration_ms, "error", summary)
    except subprocess.TimeoutExpired:
        duration_ms = int((time.monotonic() - t0) * 1000)
        timed_out = True
        summary = f"{args.summary_prefix}timeout after {args.timeout_seconds}s".strip()
        return _emit_and_propagate(args, command_redacted, step_name, 124, duration_ms, "budget_exceeded", summary)
    duration_ms = int((time.monotonic() - t0) * 1000)
    if exit_code == 0:
        gate_result = "pass"
        summary = f"{args.summary_prefix}exit=0 duration_ms={duration_ms}".strip()
    else:
        gate_result = "fail"
        summary = f"{args.summary_prefix}exit={exit_code} duration_ms={duration_ms}".strip()
    return _emit_and_propagate(args, command_redacted, step_name, exit_code, duration_ms, gate_result, summary)


def _emit_and_propagate(args: argparse.Namespace, command_redacted: str, step_name: str,
                        exit_code: int, duration_ms: int, gate_result: str, summary: str) -> int:
    metrics = {"duration_ms": duration_ms, "gate": {"name": step_name, "result": gate_result},
               "cost_estimate": {"source": "local", "method": "maintainer-estimate", "confidence": "unknown",
                                 "is_billing": False, "amount": None, "notes_redacted": "billing NV"}}
    emit_args = argparse.Namespace(
        path=args.telemetry_jsonl, run_id=args.run_id, category=args.category, action=args.action,
        actor=args.actor, event_id=None, ts=None, seq=None, state=args.state, gate_result=gate_result,
        blast_radius="unknown" if gate_result != "pass" else "low", corrects_event_id=None,
        target_repo=args.target_repo, target_ref=args.target_ref, step_name=step_name,
        command_redacted=command_redacted, exit_code=exit_code, summary_redacted=summary,
        artifact_ref=None, sdd_json=args.sdd_json, metrics_json=None, extra_json=None,
        cost_source=None, cost_method=None, cost_confidence=None, cost_currency=None,
        cost_notes_redacted=None, allow_redaction_warning=args.allow_redaction_warning, print_json=False,
    )
    emit_args._metrics_override = metrics
    event = emit.build_event(emit_args)
    event["metrics"] = metrics
    hits = emit.scan(event)
    if hits and not args.allow_redaction_warning:
        print(f"wrap_telemetry: WARNING redaction heuristic matched patterns: {hits}; telemetry not appended", file=sys.stderr)
        return 2 if exit_code == 0 else exit_code
    path = Path(args.telemetry_jsonl)
    if emit_args.seq is None:
        emit_args.seq = emit.next_seq(path, args.run_id)
        event["seq"] = emit_args.seq
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("a", encoding="utf-8") as f:
        import json
        f.write(json.dumps(event, ensure_ascii=False, separators=(",", ":")) + "\n")
    print(f"wrap_telemetry: appended gate_result={gate_result} exit={exit_code} duration_ms={duration_ms} event_id={event['event_id']} -> {path}")
    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
