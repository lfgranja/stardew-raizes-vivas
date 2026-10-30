"""Baseline parsing, validation, atomic writes, and ratchets."""

from __future__ import annotations

import json
import math
import os
import tempfile
from dataclasses import dataclass
from pathlib import Path
from typing import Any


class BaselineError(Exception):
    """Raised when a baseline file is missing, malformed, or out of range."""


@dataclass(frozen=True)
class CoverageBaseline:
    lines: float
    branches: float


@dataclass(frozen=True)
class DuplicationBaseline:
    percentage: float
    duplicates: int = 0
    lines: int = 0
    measured_at: str = ""
    commit: str = ""
    scope: str = ""


def _is_finite_number(value: Any) -> bool:
    return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value)


def _validate_percent(value: Any, name: str) -> float:
    if not _is_finite_number(value):
        raise BaselineError(f"{name} must be a finite number")
    result = float(value)
    if result < 0.0 or result > 100.0:
        raise BaselineError(f"{name} must be between 0 and 100")
    return result


def parse_coverage_baseline(text: str) -> CoverageBaseline:
    values: dict[str, str] = {}
    for raw_line in text.splitlines():
        line = raw_line.strip()
        if not line or line.startswith("#"):
            continue
        if "=" not in line:
            raise BaselineError("baseline line must be key=value")
        key, _, value = line.partition("=")
        key = key.strip()
        value = value.strip()
        if key in values:
            raise BaselineError(f"duplicate key {key!r}")
        values[key] = value
    if set(values) != {"lines", "branches"}:
        raise BaselineError("coverage baseline requires 'lines' and 'branches'")
    try:
        lines = float(values["lines"])
        branches = float(values["branches"])
    except ValueError as exc:
        raise BaselineError("baseline value must be numeric") from exc
    if not math.isfinite(lines) or not math.isfinite(branches):
        raise BaselineError("baseline value must be finite")
    if lines < 0.0 or lines > 100.0 or branches < 0.0 or branches > 100.0:
        raise BaselineError("baseline value must be between 0 and 100")
    return CoverageBaseline(lines=lines, branches=branches)


def format_coverage_baseline(baseline: CoverageBaseline) -> str:
    lines = _validate_percent(baseline.lines, "lines")
    branches = _validate_percent(baseline.branches, "branches")
    return f"lines={lines:.1f}\nbranches={branches:.1f}\n"


def parse_duplication_baseline(text: str) -> DuplicationBaseline:
    try:
        payload = json.loads(text)
    except (json.JSONDecodeError, ValueError) as exc:
        raise BaselineError("duplication baseline must be valid JSON") from exc
    if not isinstance(payload, dict):
        raise BaselineError("duplication baseline must be a JSON object")
    if "percentage" not in payload:
        raise BaselineError("duplication baseline requires 'percentage'")
    allowed = {"percentage", "duplicates", "lines", "measuredAt", "measured_at", "commit", "scope"}
    unknown = set(payload) - allowed
    if unknown:
        raise BaselineError(f"unknown keys: {sorted(unknown)}")
    percentage = _validate_percent(payload["percentage"], "percentage")
    duplicates = payload.get("duplicates", 0)
    lines = payload.get("lines", 0)
    if isinstance(duplicates, bool) or not isinstance(duplicates, int):
        raise BaselineError("duplicates must be an integer")
    if isinstance(lines, bool) or not isinstance(lines, int):
        raise BaselineError("lines must be an integer")
    if duplicates < 0 or lines < 0:
        raise BaselineError("duplicates and lines must be non-negative")
    measured_at = payload.get("measuredAt", payload.get("measured_at", ""))
    if not isinstance(measured_at, str):
        raise BaselineError("measuredAt must be a string")
    commit = payload.get("commit", "")
    if not isinstance(commit, str):
        raise BaselineError("commit must be a string")
    scope = payload.get("scope", "")
    if not isinstance(scope, str):
        raise BaselineError("scope must be a string")
    return DuplicationBaseline(
        percentage=percentage,
        duplicates=duplicates,
        lines=lines,
        measured_at=measured_at,
        commit=commit,
        scope=scope,
    )


def format_duplication_baseline(baseline: DuplicationBaseline) -> str:
    percentage = _validate_percent(baseline.percentage, "percentage")
    if isinstance(baseline.duplicates, bool) or not isinstance(baseline.duplicates, int):
        raise BaselineError("duplicates must be an integer")
    if isinstance(baseline.lines, bool) or not isinstance(baseline.lines, int):
        raise BaselineError("lines must be an integer")
    if baseline.duplicates < 0 or baseline.lines < 0:
        raise BaselineError("duplicates and lines must be non-negative")
    payload: dict[str, Any] = {
        "percentage": round(percentage, 2),
        "duplicates": baseline.duplicates,
        "lines": baseline.lines,
    }
    if baseline.measured_at:
        payload["measuredAt"] = baseline.measured_at
    if baseline.commit:
        payload["commit"] = baseline.commit
    if baseline.scope:
        payload["scope"] = baseline.scope
    return json.dumps(payload)


def _atomic_write(path: Path, content: str) -> None:
    path = Path(path)
    directory = path.parent
    directory.mkdir(parents=True, exist_ok=True)
    fd, tmp_name = tempfile.mkstemp(prefix=f".{path.name}.", dir=str(directory))
    tmp_path = Path(tmp_name)
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as handle:
            handle.write(content)
            handle.flush()
            os.fsync(handle.fileno())
        os.replace(tmp_path, path)
    except BaseException:
        try:
            tmp_path.unlink(missing_ok=True)
        except OSError:
            pass
        raise
    finally:
        if tmp_path.exists():
            try:
                tmp_path.unlink()
            except OSError:
                pass


def load_coverage_baseline(path: Path) -> CoverageBaseline:
    path = Path(path)
    if not path.is_file():
        raise BaselineError(f"baseline not found: {path}")
    return parse_coverage_baseline(path.read_text(encoding="utf-8"))


def load_duplication_baseline(path: Path) -> DuplicationBaseline:
    path = Path(path)
    if not path.is_file():
        raise BaselineError(f"baseline not found: {path}")
    return parse_duplication_baseline(path.read_text(encoding="utf-8"))


def save_coverage_baseline(path: Path, baseline: CoverageBaseline) -> None:
    try:
        _atomic_write(path, format_coverage_baseline(baseline) + "")
    except (OSError, BaselineError) as exc:
        raise BaselineError(str(exc)) from exc


def save_duplication_baseline(path: Path, baseline: DuplicationBaseline) -> None:
    try:
        _atomic_write(path, format_duplication_baseline(baseline))
    except (OSError, BaselineError) as exc:
        raise BaselineError(str(exc)) from exc


def ratchet_coverage(old: CoverageBaseline, measured: CoverageBaseline) -> CoverageBaseline:
    old_lines = _validate_percent(old.lines, "lines")
    old_branches = _validate_percent(old.branches, "branches")
    new_lines = _validate_percent(measured.lines, "lines")
    new_branches = _validate_percent(measured.branches, "branches")
    return CoverageBaseline(lines=max(old_lines, new_lines), branches=max(old_branches, new_branches))


def ratchet_duplication(old: DuplicationBaseline, measured: DuplicationBaseline) -> DuplicationBaseline:
    old_pct = _validate_percent(old.percentage, "percentage")
    new_pct = _validate_percent(measured.percentage, "percentage")
    return DuplicationBaseline(
        percentage=min(old_pct, new_pct),
        duplicates=measured.duplicates,
        lines=measured.lines,
        measured_at=measured.measured_at or old.measured_at,
        commit=measured.commit or old.commit,
        scope=measured.scope or old.scope,
    )