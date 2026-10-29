"""Tests for the ReportGenerator coverage gate."""

from __future__ import annotations

import json
import math

import pytest
from hypothesis import assume, given

from quality_gate.baseline import CoverageBaseline
from quality_gate.coverage import (
    COVERAGE_EPSILON,
    ClassCoverage,
    CoverageSummary,
    evaluate_coverage,
    load_summary,
    parse_summary,
)
from quality_gate.diagnostics import Diagnostic, GateConfigurationError

from .conftest import percent, reportgenerator_summary


def test_epsilon_is_pinned_to_reportgenerator_precision():
    assert COVERAGE_EPSILON == 0.05


def test_parse_summary_reads_the_real_reportgenerator_schema():
    payload = {
        "summary": {"linecoverage": 79.1, "branchcoverage": 70.4},
        "coverage": {
            "assemblies": [
                {
                    "classesinassembly": [
                        {
                            "name": "LivingRoots.Weak",
                            "coverage": 12.0,
                            "branchcoverage": 4.0,
                        },
                        {
                            "name": "LivingRoots.NoBranches",
                            "coverage": 100.0,
                            "branchcoverage": None,
                        },
                    ]
                }
            ]
        },
    }
    summary = parse_summary(payload)
    assert summary.lines == 79.1
    assert summary.branches == 70.4
    assert summary.classes == (
        ClassCoverage(name="LivingRoots.Weak", line_coverage=12.0, branch_coverage=4.0),
        ClassCoverage(name="LivingRoots.NoBranches", line_coverage=100.0, branch_coverage=None),
    )


@given(reportgenerator_summary())
def test_parse_summary_accepts_generated_reports(payload):
    summary = parse_summary(payload)
    assert 0.0 <= summary.lines <= 100.0
    assert 0.0 <= summary.branches <= 100.0


@pytest.mark.parametrize(
    "payload",
    [
        {},
        {"summary": {}},
        {"summary": {"linecoverage": 79.1}},
        {"summary": {"linecoverage": 79.1, "branchcoverage": 70.4}},
        {"summary": {"linecoverage": "79.1", "branchcoverage": 70.4}},
        {"summary": {"linecoverage": 79.1, "branchcoverage": 70.4}, "coverage": "nope"},
    ],
)
def test_parse_summary_fails_closed_on_missing_or_malformed_data(payload):
    with pytest.raises(GateConfigurationError):
        parse_summary(payload)


@pytest.mark.parametrize("value", [math.nan, math.inf, -math.inf, -0.1, 100.1])
def test_parse_summary_rejects_non_finite_or_out_of_range_values(value):
    payload = {"summary": {"linecoverage": value, "branchcoverage": 70.4}}
    with pytest.raises(GateConfigurationError):
        parse_summary(payload)


def test_load_summary_reads_json(tmp_path):
    path = tmp_path / "Summary.json"
    path.write_text(
        json.dumps(
            {
                "summary": {"linecoverage": 80.0, "branchcoverage": 70.0},
                "coverage": {"assemblies": []},
            }
        ),
        encoding="utf-8",
    )
    assert load_summary(path).lines == 80.0


def test_load_summary_missing_file_is_a_configuration_error(tmp_path):
    with pytest.raises(GateConfigurationError):
        load_summary(tmp_path / "missing.json")


@given(percent(), percent(), percent(), percent())
def test_measured_above_the_floor_passes(lines, branches, extra_lines, extra_branches):
    assume(lines + extra_lines <= 100.0)
    assume(branches + extra_branches <= 100.0)
    summary = CoverageSummary(lines=lines + extra_lines, branches=branches + extra_branches)
    baseline = CoverageBaseline(lines=lines, branches=branches)
    result = evaluate_coverage(summary, baseline)
    assert result.exit_code == 0
    assert result.diagnostics == ()


def test_equality_passes_exactly():
    summary = CoverageSummary(lines=79.1, branches=70.4)
    baseline = CoverageBaseline(lines=79.1, branches=70.4)
    assert evaluate_coverage(summary, baseline).exit_code == 0


def test_just_inside_the_epsilon_passes():
    baseline = CoverageBaseline(lines=80.0, branches=70.0)
    summary = CoverageSummary(lines=79.951, branches=69.951)
    assert evaluate_coverage(summary, baseline).exit_code == 0


def test_just_outside_the_epsilon_fails():
    baseline = CoverageBaseline(lines=80.0, branches=70.0)
    summary = CoverageSummary(lines=79.949, branches=69.949)
    assert evaluate_coverage(summary, baseline).exit_code == 1


@given(percent(), percent())
def test_regression_below_the_epsilon_fails(lines, branches):
    assume(lines >= COVERAGE_EPSILON + 0.01)
    baseline = CoverageBaseline(lines=lines, branches=branches)
    summary = CoverageSummary(
        lines=lines - COVERAGE_EPSILON - 0.01,
        branches=branches,
    )
    result = evaluate_coverage(summary, baseline)
    assert result.exit_code == 1
    assert result.diagnostics
    assert "coverage" in result.diagnostics[0].message.lower()


def test_failure_names_the_lowest_covered_classes():
    summary = CoverageSummary(
        lines=50.0,
        branches=50.0,
        classes=(
            ClassCoverage("LivingRoots.Good", 90.0, 90.0),
            ClassCoverage("LivingRoots.Worst", 10.0, 5.0),
            ClassCoverage("LivingRoots.Middle", 50.0, 40.0),
        ),
    )
    baseline = CoverageBaseline(lines=79.1, branches=70.4)
    result = evaluate_coverage(summary, baseline)
    assert result.exit_code == 1
    text = "\n".join(d.message for d in result.diagnostics)
    assert "LivingRoots.Worst" in text
    assert "LivingRoots.Middle" in text
    assert "LivingRoots.Good" not in text


def test_missing_classes_still_produces_a_useful_diagnostic():
    summary = CoverageSummary(lines=50.0, branches=50.0)
    baseline = CoverageBaseline(lines=79.1, branches=70.4)
    result = evaluate_coverage(summary, baseline)
    assert result.exit_code == 1
    assert result.diagnostics[0].to_github().startswith("::error title=")


def test_annotation_format_for_file_and_title_findings():
    file_finding = Diagnostic(
        title="Analyzer finding",
        message="S1481 unused local",
        file="LivingRoots/Example.cs",
        line=42,
    )
    assert file_finding.to_github() == (
        "::error file=LivingRoots/Example.cs,line=42::S1481 unused local"
    )
    title_finding = Diagnostic(title="Coverage regression", message="lines 50.0% < 79.1%")
    assert title_finding.to_github() == (
        "::error title=Coverage regression::lines 50.0% < 79.1%"
    )
