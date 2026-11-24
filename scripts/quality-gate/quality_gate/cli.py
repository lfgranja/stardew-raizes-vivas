"""Command-line interface for the quality gate."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from quality_gate.baseline import (
    BaselineError,
    CoverageBaseline,
    DuplicationBaseline,
    load_coverage_baseline,
    load_duplication_baseline,
    parse_coverage_baseline,
    ratchet_coverage,
    ratchet_duplication,
    save_coverage_baseline,
    save_duplication_baseline,
)
from quality_gate.coverage import GateConfigurationError as CoverageConfigError
from quality_gate.coverage import evaluate_coverage, load_summary
from quality_gate.diagnostics import GateConfigurationError
from quality_gate.duplication import evaluate_duplication, load_report
from quality_gate.security import evaluate_security, parse_report as parse_security_report


def _print(text: str) -> None:
    print(text, flush=True)


def _config_error(message: str) -> int:
    _print(f"::error title=Configuration error::{message}")
    return 2


def cmd_coverage(args: argparse.Namespace) -> int:
    try:
        summary = load_summary(Path(args.summary))
        baseline = load_coverage_baseline(Path(args.baseline))
    except (GateConfigurationError, BaselineError, OSError) as exc:
        return _config_error(str(exc))
    result = evaluate_coverage(summary, baseline)
    for diagnostic in result.diagnostics:
        _print(diagnostic.to_github())
    return result.exit_code


def cmd_duplication(args: argparse.Namespace) -> int:
    try:
        report = load_report(Path(args.report))
        baseline = load_duplication_baseline(Path(args.baseline))
    except (GateConfigurationError, BaselineError, OSError) as exc:
        return _config_error(str(exc))
    result = evaluate_duplication(report, baseline)
    for diagnostic in result.diagnostics:
        _print(diagnostic.to_github())
    return result.exit_code


def cmd_security(args: argparse.Namespace) -> int:
    try:
        vulnerabilities = parse_security_report(
            json.loads(Path(args.report).read_text(encoding="utf-8"))
        )
    except (GateConfigurationError, OSError, ValueError) as exc:
        return _config_error(str(exc))
    result = evaluate_security(vulnerabilities)
    for diagnostic in result.diagnostics:
        _print(diagnostic.to_github())
    return result.exit_code


def cmd_ratchet_coverage(args: argparse.Namespace) -> int:
    summary_path = Path(args.summary)
    baseline_path = Path(args.baseline)
    try:
        summary = load_summary(summary_path)
        old = load_coverage_baseline(baseline_path)
    except (GateConfigurationError, BaselineError, OSError) as exc:
        return _config_error(str(exc))
    measured = CoverageBaseline(lines=summary.lines, branches=summary.branches)
    if measured.lines < old.lines or measured.branches < old.branches:
        _print(
            f"::error title=Coverage ratchet refused::measured {measured.lines}/{measured.branches} "
            f"is below baseline {old.lines}/{old.branches}"
        )
        return 2
    new = ratchet_coverage(old, measured)
    if new.lines == old.lines and new.branches == old.branches:
        return 0
    try:
        save_coverage_baseline(baseline_path, new)
    except (BaselineError, OSError) as exc:
        return _config_error(str(exc))
    return 0


def cmd_ratchet_duplication(args: argparse.Namespace) -> int:
    report_path = Path(args.report)
    baseline_path = Path(args.baseline)
    try:
        report = load_report(report_path)
        old = load_duplication_baseline(baseline_path)
    except (GateConfigurationError, BaselineError, OSError) as exc:
        return _config_error(str(exc))
    measured = DuplicationBaseline(percentage=report.percentage)
    if measured.percentage > old.percentage:
        _print(
            f"::error title=Duplication ratchet refused::measured {measured.percentage} "
            f"is above baseline {old.percentage}"
        )
        return 2
    new = ratchet_duplication(old, measured)
    if new.percentage == old.percentage:
        return 0
    try:
        save_duplication_baseline(baseline_path, new)
    except (BaselineError, OSError) as exc:
        return _config_error(str(exc))
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="quality_gate")
    subparsers = parser.add_subparsers(dest="command", required=True)

    coverage = subparsers.add_parser("coverage")
    coverage.add_argument("--summary", required=True)
    coverage.add_argument("--baseline", required=True)
    coverage.set_defaults(func=cmd_coverage)

    duplication = subparsers.add_parser("duplication")
    duplication.add_argument("--report", required=True)
    duplication.add_argument("--baseline", required=True)
    duplication.set_defaults(func=cmd_duplication)

    security = subparsers.add_parser("security")
    security.add_argument("--report", required=True)
    security.set_defaults(func=cmd_security)

    ratchet_coverage = subparsers.add_parser("ratchet-coverage")
    ratchet_coverage.add_argument("--summary", required=True)
    ratchet_coverage.add_argument("--baseline", required=True)
    ratchet_coverage.set_defaults(func=cmd_ratchet_coverage)

    ratchet_duplication = subparsers.add_parser("ratchet-duplication")
    ratchet_duplication.add_argument("--report", required=True)
    ratchet_duplication.add_argument("--baseline", required=True)
    ratchet_duplication.set_defaults(func=cmd_ratchet_duplication)

    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    return int(args.func(args))


if __name__ == "__main__":
    raise SystemExit(main())