"""jscpd duplication gate."""

from __future__ import annotations

import json
import math
from dataclasses import dataclass
from pathlib import Path
from typing import Optional

from quality_gate.baseline import DuplicationBaseline
from quality_gate.diagnostics import Diagnostic, GateConfigurationError

DUPLICATION_EPSILON = 0.0


@dataclass(frozen=True)
class Clone:
    first_file: str
    first_line: int
    second_file: str
    second_line: int
    lines: int


@dataclass(frozen=True)
class DuplicationReport:
    percentage: float
    total_lines: int = 0
    duplicates: tuple[Clone, ...] = ()


@dataclass(frozen=True)
class GateResult:
    exit_code: int
    diagnostics: tuple[Diagnostic, ...] = ()


def _as_number(value: object, name: str) -> float:
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise GateConfigurationError(f"{name} must be a number")
    result = float(value)
    if not math.isfinite(result):
        raise GateConfigurationError(f"{name} must be finite")
    if result < 0.0 or result > 100.0:
        raise GateConfigurationError(f"{name} must be between 0 and 100")
    return result


def _as_int(value: object, name: str) -> int:
    if isinstance(value, bool) or not isinstance(value, int):
        raise GateConfigurationError(f"{name} must be an integer")
    if value < 0:
        raise GateConfigurationError(f"{name} must be non-negative")
    return value


def parse_report(payload: object) -> DuplicationReport:
    if not isinstance(payload, dict):
        raise GateConfigurationError("report payload must be a JSON object")
    statistics = payload.get("statistics")
    if not isinstance(statistics, dict):
        raise GateConfigurationError("statistics must be an object")
    total = statistics.get("total")
    if not isinstance(total, dict):
        raise GateConfigurationError("statistics.total must be an object")
    if "percentage" not in total or "duplicates" not in total or "lines" not in total:
        raise GateConfigurationError("statistics.total requires percentage, duplicates, and lines")
    percentage = _as_number(total["percentage"], "percentage")
    duplicates_count = _as_int(total["duplicates"], "duplicates")
    total_lines = _as_int(total["lines"], "lines")

    duplicates_raw = payload.get("duplicates", [])
    if not isinstance(duplicates_raw, list):
        raise GateConfigurationError("duplicates must be an array")
    if len(duplicates_raw) != duplicates_count:
        raise GateConfigurationError("duplicates count mismatch")
    clones: list[Clone] = []
    for entry in duplicates_raw:
        if not isinstance(entry, dict):
            raise GateConfigurationError("clone entry must be an object")
        if "lines" not in entry:
            raise GateConfigurationError("clone requires lines")
        clone_lines = _as_int(entry["lines"], "lines")
        first = entry.get("firstFile")
        second = entry.get("secondFile")
        if not isinstance(first, dict) or not isinstance(second, dict):
            raise GateConfigurationError("clone requires firstFile and secondFile")
        if "name" not in first or "start" not in first or "name" not in second or "start" not in second:
            raise GateConfigurationError("clone files require name and start")
        if not isinstance(first["name"], str) or not isinstance(second["name"], str):
            raise GateConfigurationError("file names must be strings")
        first_line = _as_int(first["start"], "start")
        second_line = _as_int(second["start"], "start")
        clones.append(
            Clone(
                first_file=first["name"],
                first_line=first_line,
                second_file=second["name"],
                second_line=second_line,
                lines=clone_lines,
            )
        )

    # total_lines is available but not stored on the report; keep parity.
    return DuplicationReport(percentage=percentage, total_lines=total_lines, duplicates=tuple(clones))


def load_report(path: Path) -> DuplicationReport:
    path = Path(path)
    if not path.is_file():
        raise GateConfigurationError(f"report not found: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError) as exc:
        raise GateConfigurationError(f"cannot read report: {exc}") from exc
    return parse_report(payload)


def evaluate_duplication(report: DuplicationReport, baseline: DuplicationBaseline) -> GateResult:
    ceiling = baseline.percentage
    if report.percentage <= ceiling:
        return GateResult(exit_code=0, diagnostics=())
    diagnostics: list[Diagnostic] = []
    if report.duplicates:
        for clone in report.duplicates:
            message = (
                f"{clone.first_file}:{clone.first_line} / {clone.second_file}:{clone.second_line} "
                f"({clone.lines} duplicated lines)"
            )
            diagnostics.append(
                Diagnostic(
                    title="Duplication regression",
                    message=message,
                    file=clone.first_file,
                    line=clone.first_line,
                )
            )
            diagnostics.append(
                Diagnostic(
                    title="Duplication regression",
                    message=message,
                    file=clone.second_file,
                    line=clone.second_line,
                )
            )
    else:
        diagnostics.append(
            Diagnostic(
                title="Duplication regression",
                message=f"duplication {report.percentage}% > {ceiling}%",
            )
        )
    return GateResult(exit_code=1, diagnostics=tuple(diagnostics))