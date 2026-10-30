"""ReportGenerator coverage gate."""

from __future__ import annotations

import json
import math
from dataclasses import dataclass
from pathlib import Path
from typing import Optional

from quality_gate.baseline import CoverageBaseline
from quality_gate.diagnostics import Diagnostic, GateConfigurationError

COVERAGE_EPSILON = 0.05


@dataclass(frozen=True)
class ClassCoverage:
    name: str
    line_coverage: float
    branch_coverage: Optional[float]


@dataclass(frozen=True)
class CoverageSummary:
    lines: float
    branches: float
    classes: tuple[ClassCoverage, ...] = ()


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


def parse_summary(payload: object) -> CoverageSummary:
    if not isinstance(payload, dict):
        raise GateConfigurationError("summary payload must be a JSON object")
    summary = payload.get("summary")
    if not isinstance(summary, dict):
        raise GateConfigurationError("summary.summary must be an object")
    if "linecoverage" not in summary or "branchcoverage" not in summary:
        raise GateConfigurationError("summary.linecoverage and summary.branchcoverage are required")
    lines = _as_number(summary["linecoverage"], "linecoverage")
    branches = _as_number(summary["branchcoverage"], "branchcoverage")

    coverage = payload.get("coverage")
    if not isinstance(coverage, dict):
        raise GateConfigurationError("summary.coverage must be an object")
    assemblies = coverage.get("assemblies")
    if not isinstance(assemblies, list) or not assemblies:
        raise GateConfigurationError("coverage.assemblies must be a non-empty array")

    classes: list[ClassCoverage] = []
    for assembly in assemblies:
        if not isinstance(assembly, dict):
            raise GateConfigurationError("assembly must be an object")
        class_list = assembly.get("classesinassembly")
        if not isinstance(class_list, list):
            raise GateConfigurationError("classesinassembly must be an array")
        for entry in class_list:
            if not isinstance(entry, dict):
                raise GateConfigurationError("class entry must be an object")
            if "name" not in entry or "coverage" not in entry or "branchcoverage" not in entry:
                raise GateConfigurationError("class entry requires name, coverage, and branchcoverage")
            name = entry["name"]
            if not isinstance(name, str):
                raise GateConfigurationError("class name must be a string")
            line_cov = _as_number(entry["coverage"], "coverage")
            branch_raw = entry["branchcoverage"]
            if branch_raw is None:
                branch_cov = None
            else:
                branch_cov = _as_number(branch_raw, "branchcoverage")
            classes.append(ClassCoverage(name=name, line_coverage=line_cov, branch_coverage=branch_cov))

    return CoverageSummary(lines=lines, branches=branches, classes=tuple(classes))


def load_summary(path: Path) -> CoverageSummary:
    path = Path(path)
    if not path.is_file():
        raise GateConfigurationError(f"summary not found: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError) as exc:
        raise GateConfigurationError(f"cannot read summary: {exc}") from exc
    return parse_summary(payload)


def evaluate_coverage(summary: CoverageSummary, baseline: CoverageBaseline) -> GateResult:
    lines_ok = summary.lines + COVERAGE_EPSILON >= baseline.lines
    branches_ok = summary.branches + COVERAGE_EPSILON >= baseline.branches
    if lines_ok and branches_ok:
        return GateResult(exit_code=0, diagnostics=())

    diagnostics: list[Diagnostic] = []
    low_classes = sorted(
        (cls for cls in summary.classes if cls.line_coverage + COVERAGE_EPSILON < baseline.lines),
        key=lambda cls: cls.line_coverage,
    )
    if low_classes:
        for cls in low_classes:
            diagnostics.append(
                Diagnostic(
                    title="Coverage regression",
                    message=f"{cls.name} {cls.line_coverage}% < {baseline.lines}%",
                )
            )
    else:
        diagnostics.append(
            Diagnostic(
                title="Coverage regression",
                message=f"lines {summary.lines}% < {baseline.lines}%",
            )
        )
    return GateResult(exit_code=1, diagnostics=tuple(diagnostics))