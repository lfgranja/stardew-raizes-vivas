"""Tests for the Trivy dependency-vulnerability gate."""

from __future__ import annotations

import pytest
from hypothesis import given
from hypothesis import strategies as st

from quality_gate.diagnostics import GateConfigurationError
from quality_gate.security import (
    SEVERITY_ORDER,
    Vulnerability,
    evaluate_security,
    is_blocking,
    parse_report,
)

from .conftest import trivy_report


def test_parse_report_collects_vulnerabilities_with_targets():
    payload = {
        "Results": [
            {
                "Target": "LivingRoots/packages.lock.json",
                "Vulnerabilities": [
                    {
                        "VulnerabilityID": "CVE-2026-0001",
                        "PkgName": "Example.Package",
                        "InstalledVersion": "1.0.0",
                        "FixedVersion": "1.0.1",
                        "Severity": "HIGH",
                    }
                ],
            }
        ]
    }
    vulnerabilities = parse_report(payload)
    assert vulnerabilities == (
        Vulnerability(
            cve="CVE-2026-0001",
            package="Example.Package",
            installed_version="1.0.0",
            fixed_version="1.0.1",
            severity="HIGH",
            target="LivingRoots/packages.lock.json",
        ),
    )


@given(trivy_report())
def test_parse_report_preserves_every_generated_vulnerability(payload):
    expected = []
    for result in payload["Results"]:
        for item in result["Vulnerabilities"]:
            expected.append(
                (
                    item["VulnerabilityID"],
                    item["PkgName"],
                    item["InstalledVersion"],
                    item["FixedVersion"],
                    item["Severity"],
                    result["Target"],
                )
            )
    parsed = parse_report(payload)
    assert len(parsed) == len(expected)
    assert [
        (
            vulnerability.cve,
            vulnerability.package,
            vulnerability.installed_version,
            vulnerability.fixed_version,
            vulnerability.severity,
            vulnerability.target,
        )
        for vulnerability in parsed
    ] == expected


@pytest.mark.parametrize(
    "payload",
    [
        {},
        {"Results": None},
        {"Results": "nope"},
        {"Results": [{"Vulnerabilities": "nope"}]},
        {
            "Results": [
                {
                    "Target": "x",
                    "Vulnerabilities": [{"PkgName": "p", "Severity": "HIGH"}],
                }
            ]
        },
        {
            "Results": [
                {
                    "Target": "x",
                    "Vulnerabilities": [
                        {"VulnerabilityID": "CVE-1", "PkgName": "p", "Severity": "SEVERE"}
                    ],
                }
            ]
        },
    ],
)
def test_parse_report_fails_closed_on_malformed_data(payload):
    with pytest.raises(GateConfigurationError):
        parse_report(payload)


def test_fixed_version_is_optional_for_unfixed_vulnerabilities():
    payload = {
        "Results": [
            {
                "Target": "LivingRoots/packages.lock.json",
                "Vulnerabilities": [
                    {
                        "VulnerabilityID": "CVE-2026-9999",
                        "PkgName": "Example.Package",
                        "InstalledVersion": "1.0.0",
                        "Severity": "CRITICAL",
                    }
                ],
            }
        ]
    }
    vulnerabilities = parse_report(payload)
    assert vulnerabilities == (
        Vulnerability(
            cve="CVE-2026-9999",
            package="Example.Package",
            installed_version="1.0.0",
            fixed_version="",
            severity="CRITICAL",
            target="LivingRoots/packages.lock.json",
        ),
    )
    result = evaluate_security(vulnerabilities)
    assert result.exit_code == 1
    assert "CRITICAL" in result.diagnostics[0].message
    assert "CVE-2026-9999" in result.diagnostics[0].message


@pytest.mark.parametrize("severity", ["HIGH", "CRITICAL"])
def test_high_and_critical_are_blocking(severity):
    vulnerability = Vulnerability(
        cve="CVE-2026-0001",
        package="Example.Package",
        installed_version="1.0.0",
        fixed_version="1.0.1",
        severity=severity,
        target="LivingRoots/packages.lock.json",
    )
    result = evaluate_security((vulnerability,))
    assert result.exit_code == 1
    message = result.diagnostics[0].message
    assert "CVE-2026-0001" in message
    assert "Example.Package" in message
    assert "1.0.0" in message
    assert "1.0.1" in message
    assert severity in message
    assert "LivingRoots/packages.lock.json" in message


@pytest.mark.parametrize("severity", ["LOW", "MEDIUM"])
def test_low_and_medium_are_not_blocking(severity):
    vulnerability = Vulnerability(
        cve="CVE-2026-0001",
        package="Example.Package",
        installed_version="1.0.0",
        fixed_version="1.0.1",
        severity=severity,
        target="LivingRoots/packages.lock.json",
    )
    assert evaluate_security((vulnerability,)).exit_code == 0


def test_unknown_severity_fails_closed():
    vulnerability = Vulnerability(
        cve="CVE-2026-0001",
        package="Example.Package",
        installed_version="1.0.0",
        fixed_version="1.0.1",
        severity="UNKNOWN",
        target="LivingRoots/packages.lock.json",
    )
    result = evaluate_security((vulnerability,))
    assert result.exit_code == 1


def test_empty_scan_passes():
    assert evaluate_security(()).exit_code == 0


def test_severity_order_is_total_and_strict():
    order = ["UNKNOWN", "LOW", "MEDIUM", "HIGH", "CRITICAL"]
    ranks = [SEVERITY_ORDER[severity] for severity in order]
    assert ranks == sorted(ranks)
    assert len(set(ranks)) == len(ranks)


@given(st.sampled_from(list(SEVERITY_ORDER)))
def test_is_blocking_matches_the_high_threshold(severity):
    assert is_blocking(severity) == (SEVERITY_ORDER[severity] >= SEVERITY_ORDER["HIGH"])
