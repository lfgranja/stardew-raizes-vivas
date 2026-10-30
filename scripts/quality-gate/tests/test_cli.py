"""Tests for the quality_gate command-line interface."""

from __future__ import annotations

import json
import os
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
PACKAGE_PARENT = ROOT / "scripts" / "quality-gate"


def run_cli(*args: str, cwd: Path | None = None) -> subprocess.CompletedProcess[str]:
    env = dict(os.environ)
    env["PYTHONPATH"] = str(PACKAGE_PARENT)
    return subprocess.run(
        [sys.executable, "-m", "quality_gate", *args],
        cwd=cwd or ROOT,
        env=env,
        text=True,
        capture_output=True,
        check=False,
    )


def write_summary(path: Path, lines: float, branches: float) -> None:
    path.write_text(
        json.dumps(
            {
                "summary": {"linecoverage": lines, "branchcoverage": branches},
                "coverage": {
                    "assemblies": [
                        {
                            "classesinassembly": [
                                {
                                    "name": "LivingRoots.Worst",
                                    "coverage": 5.0,
                                    "branchcoverage": 2.0,
                                }
                            ]
                        }
                    ]
                },
            }
        ),
        encoding="utf-8",
    )


def write_coverage_baseline(path: Path, lines: float, branches: float) -> None:
    path.write_text(f"lines={lines}\nbranches={branches}\n", encoding="utf-8")


def write_jscpd(path: Path, percentage: float) -> None:
    path.write_text(
        json.dumps(
            {
                "statistics": {"total": {"percentage": percentage, "duplicates": 1, "lines": 10}},
                "duplicates": [
                    {
                        "lines": 10,
                        "firstFile": {"name": "LivingRoots/A.cs", "start": 1},
                        "secondFile": {"name": "LivingRoots/B.cs", "start": 2},
                    }
                ],
            }
        ),
        encoding="utf-8",
    )


def write_duplication_baseline(path: Path, percentage: float) -> None:
    path.write_text(json.dumps({"percentage": percentage}), encoding="utf-8")


def write_trivy(path: Path, severity: str) -> None:
    path.write_text(
        json.dumps(
            {
                "Results": [
                    {
                        "Target": "LivingRoots/packages.lock.json",
                        "Vulnerabilities": [
                            {
                                "VulnerabilityID": "CVE-2026-0001",
                                "PkgName": "Example.Package",
                                "InstalledVersion": "1.0.0",
                                "FixedVersion": "1.0.1",
                                "Severity": severity,
                            }
                        ],
                    }
                ]
            }
        ),
        encoding="utf-8",
    )


def test_help_lists_every_subcommand():
    result = run_cli("--help")
    assert result.returncode == 0, result.stderr
    for command in (
        "coverage",
        "duplication",
        "security",
        "ratchet-coverage",
        "ratchet-duplication",
    ):
        assert command in result.stdout


def test_every_subcommand_has_help():
    for command in (
        "coverage",
        "duplication",
        "security",
        "ratchet-coverage",
        "ratchet-duplication",
    ):
        result = run_cli(command, "--help")
        assert result.returncode == 0, f"{command}: {result.stderr}"


def test_missing_input_file_exits_two_with_a_configuration_error(tmp_path):
    result = run_cli(
        "coverage",
        "--summary",
        str(tmp_path / "missing.json"),
        "--baseline",
        str(tmp_path / "missing-baseline"),
    )
    assert result.returncode == 2
    assert "::error title=" in result.stdout + result.stderr


def test_passing_coverage_exits_zero(tmp_path):
    summary = tmp_path / "Summary.json"
    baseline = tmp_path / "baseline"
    write_summary(summary, 79.1, 70.4)
    write_coverage_baseline(baseline, 79.1, 70.4)
    result = run_cli("coverage", "--summary", str(summary), "--baseline", str(baseline))
    assert result.returncode == 0, result.stdout + result.stderr


def test_failing_coverage_exits_one_and_prints_an_annotation(tmp_path):
    summary = tmp_path / "Summary.json"
    baseline = tmp_path / "baseline"
    write_summary(summary, 50.0, 50.0)
    write_coverage_baseline(baseline, 79.1, 70.4)
    result = run_cli("coverage", "--summary", str(summary), "--baseline", str(baseline))
    assert result.returncode == 1
    assert "::error title=Coverage regression::" in result.stdout
    assert "LivingRoots.Worst" in result.stdout
    assert "79.1" in result.stdout


def test_passing_duplication_exits_zero(tmp_path):
    report = tmp_path / "jscpd.json"
    baseline = tmp_path / "baseline.json"
    write_jscpd(report, 0.48)
    write_duplication_baseline(baseline, 0.48)
    result = run_cli("duplication", "--report", str(report), "--baseline", str(baseline))
    assert result.returncode == 0, result.stdout + result.stderr


def test_failing_duplication_exits_one_and_names_both_clones(tmp_path):
    report = tmp_path / "jscpd.json"
    baseline = tmp_path / "baseline.json"
    write_jscpd(report, 2.0)
    write_duplication_baseline(baseline, 0.48)
    result = run_cli("duplication", "--report", str(report), "--baseline", str(baseline))
    assert result.returncode == 1
    assert "::error file=LivingRoots/A.cs,line=1::" in result.stdout
    assert "::error file=LivingRoots/B.cs,line=2::" in result.stdout


def test_blocking_vulnerability_exits_one(tmp_path):
    report = tmp_path / "trivy.json"
    write_trivy(report, "HIGH")
    result = run_cli("security", "--report", str(report))
    assert result.returncode == 1
    assert "::error title=" in result.stdout
    assert "CVE-2026-0001" in result.stdout
    assert "Example.Package" in result.stdout
    assert "LivingRoots/packages.lock.json" in result.stdout


def test_non_blocking_severity_exits_zero(tmp_path):
    report = tmp_path / "trivy.json"
    write_trivy(report, "MEDIUM")
    result = run_cli("security", "--report", str(report))
    assert result.returncode == 0, result.stdout + result.stderr


def test_ratchet_coverage_refuses_to_lower_a_floor(tmp_path):
    summary = tmp_path / "Summary.json"
    baseline = tmp_path / "baseline"
    write_summary(summary, 10.0, 10.0)
    write_coverage_baseline(baseline, 79.1, 70.4)
    before = baseline.read_bytes()
    before_entries = sorted(path.name for path in tmp_path.iterdir())
    result = run_cli(
        "ratchet-coverage",
        "--summary",
        str(summary),
        "--baseline",
        str(baseline),
    )
    assert result.returncode == 2
    assert baseline.read_bytes() == before, "a weakening measurement must leave the baseline byte-identical"
    assert sorted(path.name for path in tmp_path.iterdir()) == before_entries, (
        "a refused ratchet must not leave a temporary file behind"
    )


def test_ratchet_coverage_raises_a_floor(tmp_path):
    summary = tmp_path / "Summary.json"
    baseline = tmp_path / "baseline"
    write_summary(summary, 90.0, 80.0)
    write_coverage_baseline(baseline, 79.1, 70.4)
    result = run_cli(
        "ratchet-coverage",
        "--summary",
        str(summary),
        "--baseline",
        str(baseline),
    )
    assert result.returncode == 0, result.stdout + result.stderr
    assert "lines=90" in baseline.read_text(encoding="utf-8")


def test_ratchet_coverage_equal_is_a_noop(tmp_path):
    summary = tmp_path / "Summary.json"
    baseline = tmp_path / "baseline"
    write_summary(summary, 79.1, 70.4)
    write_coverage_baseline(baseline, 79.1, 70.4)
    before = baseline.read_text(encoding="utf-8")
    result = run_cli(
        "ratchet-coverage",
        "--summary",
        str(summary),
        "--baseline",
        str(baseline),
    )
    assert result.returncode == 0, result.stdout + result.stderr
    assert baseline.read_text(encoding="utf-8") == before


def test_ratchet_duplication_refuses_to_raise_a_ceiling(tmp_path):
    report = tmp_path / "jscpd.json"
    baseline = tmp_path / "baseline.json"
    write_jscpd(report, 5.0)
    write_duplication_baseline(baseline, 0.48)
    before = baseline.read_bytes()
    before_entries = sorted(path.name for path in tmp_path.iterdir())
    result = run_cli(
        "ratchet-duplication",
        "--report",
        str(report),
        "--baseline",
        str(baseline),
    )
    assert result.returncode == 2
    assert baseline.read_bytes() == before, "a weakening measurement must leave the baseline byte-identical"
    assert sorted(path.name for path in tmp_path.iterdir()) == before_entries, (
        "a refused ratchet must not leave a temporary file behind"
    )


def test_ratchet_duplication_lowers_a_ceiling(tmp_path):
    report = tmp_path / "jscpd.json"
    baseline = tmp_path / "baseline.json"
    write_jscpd(report, 0.1)
    write_duplication_baseline(baseline, 0.48)
    result = run_cli(
        "ratchet-duplication",
        "--report",
        str(report),
        "--baseline",
        str(baseline),
    )
    assert result.returncode == 0, result.stdout + result.stderr
    assert json.loads(baseline.read_text(encoding="utf-8"))["percentage"] == 0.1


def test_ratchet_duplication_equal_is_a_noop(tmp_path):
    report = tmp_path / "jscpd.json"
    baseline = tmp_path / "baseline.json"
    write_jscpd(report, 0.48)
    write_duplication_baseline(baseline, 0.48)
    before = baseline.read_text(encoding="utf-8")
    result = run_cli(
        "ratchet-duplication",
        "--report",
        str(report),
        "--baseline",
        str(baseline),
    )
    assert result.returncode == 0, result.stdout + result.stderr
    assert baseline.read_text(encoding="utf-8") == before
