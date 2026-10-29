"""Tests for the jscpd duplication gate."""

from __future__ import annotations

import json
import math

import pytest
from hypothesis import assume, given

from quality_gate.baseline import DuplicationBaseline
from quality_gate.diagnostics import GateConfigurationError
from quality_gate.duplication import (
    Clone,
    DuplicationReport,
    evaluate_duplication,
    load_report,
    parse_report,
)

from .conftest import jscpd_report, percent


def test_parse_report_reads_total_and_clone_locations():
    payload = {
        "statistics": {"total": {"percentage": 0.48, "duplicates": 1, "lines": 64}},
        "duplicates": [
            {
                "lines": 64,
                "firstFile": {"name": "LivingRoots/A.cs", "start": 10},
                "secondFile": {"name": "LivingRoots/B.cs", "start": 42},
            }
        ],
    }
    report = parse_report(payload)
    assert report.percentage == 0.48
    assert report.duplicates == (
        Clone(
            first_file="LivingRoots/A.cs",
            first_line=10,
            second_file="LivingRoots/B.cs",
            second_line=42,
            lines=64,
        ),
    )


@given(jscpd_report())
def test_parse_report_accepts_generated_reports(payload):
    report = parse_report(payload)
    assert 0.0 <= report.percentage <= 100.0
    assert len(report.duplicates) == payload["statistics"]["total"]["duplicates"]


@pytest.mark.parametrize(
    "payload",
    [
        {},
        {"statistics": {}},
        {"statistics": {"total": {}}},
        {"statistics": {"total": {"percentage": "0.48"}}},
        {"statistics": {"total": {"percentage": 0.48}}, "duplicates": "nope"},
    ],
)
def test_parse_report_fails_closed_on_malformed_data(payload):
    with pytest.raises(GateConfigurationError):
        parse_report(payload)


@pytest.mark.parametrize("value", [math.nan, math.inf, -0.1, 100.1])
def test_parse_report_rejects_invalid_percentages(value):
    with pytest.raises(GateConfigurationError):
        parse_report({"statistics": {"total": {"percentage": value}}})


def test_load_report_missing_file_is_a_configuration_error(tmp_path):
    with pytest.raises(GateConfigurationError):
        load_report(tmp_path / "missing.json")


def test_load_report_reads_json(tmp_path):
    path = tmp_path / "jscpd-report.json"
    path.write_text(
        json.dumps({"statistics": {"total": {"percentage": 0.1}}}),
        encoding="utf-8",
    )
    assert load_report(path).percentage == 0.1


@given(percent(), percent())
def test_measured_below_the_ceiling_passes(ceiling, headroom):
    assume(ceiling + headroom <= 100.0)
    report = DuplicationReport(percentage=ceiling + headroom)
    baseline = DuplicationBaseline(percentage=ceiling)
    assert evaluate_duplication(report, baseline).exit_code == 0


@given(percent())
def test_measured_at_the_ceiling_passes(ceiling):
    report = DuplicationReport(percentage=ceiling)
    baseline = DuplicationBaseline(percentage=ceiling)
    assert evaluate_duplication(report, baseline).exit_code == 0


@given(percent(), percent())
def test_measured_above_the_ceiling_fails(measured, ceiling):
    assume(measured > ceiling + 0.0001)
    report = DuplicationReport(percentage=measured)
    baseline = DuplicationBaseline(percentage=ceiling)
    result = evaluate_duplication(report, baseline)
    assert result.exit_code == 1
    assert "duplication" in result.diagnostics[0].message.lower()


def test_failure_reports_both_clone_sides_with_file_and_line():
    report = DuplicationReport(
        percentage=2.0,
        duplicates=(
            Clone(
                first_file="LivingRoots/A.cs",
                first_line=10,
                second_file="LivingRoots/B.cs",
                second_line=42,
                lines=64,
            ),
        ),
    )
    baseline = DuplicationBaseline(percentage=0.48)
    result = evaluate_duplication(report, baseline)
    assert result.exit_code == 1
    text = "\n".join(d.message for d in result.diagnostics)
    assert "LivingRoots/A.cs:10" in text
    assert "LivingRoots/B.cs:42" in text


def test_failure_without_clone_data_still_produces_a_diagnostic():
    report = DuplicationReport(percentage=2.0)
    baseline = DuplicationBaseline(percentage=0.48)
    result = evaluate_duplication(report, baseline)
    assert result.exit_code == 1
    assert result.diagnostics
    assert result.diagnostics[0].to_github().startswith("::error title=")
