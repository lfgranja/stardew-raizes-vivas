"""Trivy dependency-vulnerability gate."""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import Optional

from quality_gate.diagnostics import Diagnostic, GateConfigurationError

SEVERITY_ORDER = {
    "UNKNOWN": 0,
    "LOW": 1,
    "MEDIUM": 2,
    "HIGH": 3,
    "CRITICAL": 4,
}


@dataclass(frozen=True)
class Vulnerability:
    cve: str
    package: str
    installed_version: str
    fixed_version: str
    severity: str
    target: str


@dataclass(frozen=True)
class GateResult:
    exit_code: int
    diagnostics: tuple[Diagnostic, ...] = ()


def is_blocking(severity: str) -> bool:
    if severity not in SEVERITY_ORDER:
        raise GateConfigurationError(f"unknown severity: {severity}")
    return SEVERITY_ORDER[severity] >= SEVERITY_ORDER["HIGH"]


def parse_report(payload: object) -> tuple[Vulnerability, ...]:
    if not isinstance(payload, dict):
        raise GateConfigurationError("report payload must be a JSON object")
    results = payload.get("Results")
    if not isinstance(results, list):
        raise GateConfigurationError("Results must be an array")
    vulnerabilities: list[Vulnerability] = []
    for result in results:
        if not isinstance(result, dict):
            raise GateConfigurationError("result must be an object")
        target = result.get("Target")
        if not isinstance(target, str):
            raise GateConfigurationError("Target must be a string")
        items = result.get("Vulnerabilities", [])
        if not isinstance(items, list):
            raise GateConfigurationError("Vulnerabilities must be an array")
        for item in items:
            if not isinstance(item, dict):
                raise GateConfigurationError("vulnerability must be an object")
            cve = item.get("VulnerabilityID")
            package = item.get("PkgName")
            installed = item.get("InstalledVersion")
            severity = item.get("Severity")
            if not isinstance(cve, str):
                raise GateConfigurationError("VulnerabilityID must be a string")
            if not isinstance(package, str):
                raise GateConfigurationError("PkgName must be a string")
            if not isinstance(installed, str):
                raise GateConfigurationError("InstalledVersion must be a string")
            if severity not in SEVERITY_ORDER:
                raise GateConfigurationError(f"unknown severity: {severity}")
            fixed = item.get("FixedVersion", "")
            if fixed is None:
                fixed = ""
            if not isinstance(fixed, str):
                raise GateConfigurationError("FixedVersion must be a string")
            vulnerabilities.append(
                Vulnerability(
                    cve=cve,
                    package=package,
                    installed_version=installed,
                    fixed_version=fixed,
                    severity=severity,
                    target=target,
                )
            )
    return tuple(vulnerabilities)


def load_report(path: Path) -> tuple[Vulnerability, ...]:
    path = Path(path)
    if not path.is_file():
        raise GateConfigurationError(f"report not found: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError) as exc:
        raise GateConfigurationError(f"cannot read report: {exc}") from exc
    return parse_report(payload)


def evaluate_security(vulnerabilities: tuple[Vulnerability, ...]) -> GateResult:
    diagnostics: list[Diagnostic] = []
    blocking_found = False
    for vuln in vulnerabilities:
        if vuln.severity not in ("LOW", "MEDIUM", "HIGH", "CRITICAL"):
            diagnostics.append(
                Diagnostic(
                    title="Security configuration error",
                    message=f"unknown severity {vuln.severity} for {vuln.cve}",
                )
            )
            blocking_found = True
            continue
        if is_blocking(vuln.severity):
            blocking_found = True
            fixed_part = vuln.fixed_version if vuln.fixed_version else "none"
            diagnostics.append(
                Diagnostic(
                    title="Security vulnerability",
                    message=(
                        f"{vuln.cve} {vuln.package} {vuln.installed_version} -> {fixed_part} "
                        f"{vuln.severity} {vuln.target}"
                    ),
                )
            )
    if blocking_found:
        return GateResult(exit_code=1, diagnostics=tuple(diagnostics))
    return GateResult(exit_code=0, diagnostics=())