"""Tests for baseline parsing, validation, atomic writes, and ratchets."""

from __future__ import annotations

import math

import pytest
from hypothesis import given

from quality_gate import baseline as baseline_module
from quality_gate.baseline import (
    BaselineError,
    CoverageBaseline,
    DuplicationBaseline,
    format_coverage_baseline,
    format_duplication_baseline,
    load_coverage_baseline,
    load_duplication_baseline,
    parse_coverage_baseline,
    parse_duplication_baseline,
    ratchet_coverage,
    ratchet_duplication,
    save_coverage_baseline,
    save_duplication_baseline,
)

from .conftest import coverage_baseline_dict, percent


def test_parse_coverage_baseline_accepts_comments_and_whitespace():
    text = """
    # measuredAt: 2026-10-29
    # commit: abc123
    # scope: LivingRoots/
    lines = 79.1
    branches=70.4
    """
    parsed = parse_coverage_baseline(text)
    assert parsed == CoverageBaseline(lines=79.1, branches=70.4)


@given(coverage_baseline_dict())
def test_coverage_baseline_round_trips(payload):
    parsed = CoverageBaseline(**payload)
    text = format_coverage_baseline(parsed)
    again = parse_coverage_baseline(text)
    assert again.lines == pytest.approx(parsed.lines, abs=0.05)
    assert again.branches == pytest.approx(parsed.branches, abs=0.05)


@pytest.mark.parametrize("text", ["lines=79.1", "branches=70.4", "", "# only a comment"])
def test_coverage_baseline_requires_both_keys(text):
    with pytest.raises(BaselineError):
        parse_coverage_baseline(text)


def test_coverage_baseline_rejects_unknown_keys():
    with pytest.raises(BaselineError):
        parse_coverage_baseline("lines=79.1\nbranches=70.4\ntokens=10")


@pytest.mark.parametrize("value", ["-0.1", "100.1", "nan", "inf", "-inf", "abc", ""])
def test_coverage_baseline_rejects_invalid_percentages(value):
    with pytest.raises(BaselineError):
        parse_coverage_baseline(f"lines={value}\nbranches=70.4")


@given(coverage_baseline_dict())
def test_coverage_baseline_serialisation_is_stable(payload):
    parsed = CoverageBaseline(**payload)
    once = format_coverage_baseline(parsed)
    twice = format_coverage_baseline(parse_coverage_baseline(once))
    assert once == twice


def test_duplication_baseline_round_trips():
    parsed = DuplicationBaseline(
        percentage=0.48,
        duplicates=6,
        lines=64,
        measured_at="2026-10-29",
        commit="abc123",
        scope="LivingRoots/ (csharp)",
    )
    again = parse_duplication_baseline(format_duplication_baseline(parsed))
    assert again == parsed


@given(percent())
def test_duplication_baseline_round_trips_percentage(value):
    parsed = DuplicationBaseline(percentage=value)
    again = parse_duplication_baseline(format_duplication_baseline(parsed))
    assert again.percentage == pytest.approx(value, abs=0.005)


def test_duplication_baseline_rejects_unknown_keys():
    with pytest.raises(BaselineError):
        parse_duplication_baseline('{"percentage": 0.48, "unexpected": 1}')


@pytest.mark.parametrize(
    "payload",
    [
        "{}",
        '{"percentage": -1}',
        '{"percentage": 101}',
        '{"percentage": "0.5"}',
        '{"percentage": 0.5, "duplicates": -1}',
        '{"percentage": 0.5, "duplicates": true}',
        '{"percentage": 0.5, "lines": -3}',
        "not json",
    ],
)
def test_duplication_baseline_rejects_invalid_payloads(payload):
    with pytest.raises(BaselineError):
        parse_duplication_baseline(payload)


def test_load_missing_baseline_is_a_configuration_error(tmp_path):
    with pytest.raises(BaselineError):
        load_coverage_baseline(tmp_path / "missing")
    with pytest.raises(BaselineError):
        load_duplication_baseline(tmp_path / "missing.json")


def test_save_coverage_baseline_is_atomic_on_replace_failure(tmp_path, monkeypatch):
    target = tmp_path / ".coverage-baseline"
    target.write_text("lines=10\nbranches=20\n", encoding="utf-8")

    def boom(*_args, **_kwargs):
        raise OSError("disk full")

    monkeypatch.setattr(baseline_module.os, "replace", boom)

    with pytest.raises(BaselineError):
        save_coverage_baseline(target, CoverageBaseline(lines=50.0, branches=60.0))

    assert target.read_text(encoding="utf-8") == "lines=10\nbranches=20\n"
    assert [path.name for path in tmp_path.iterdir() if path.name != target.name] == []


def test_save_duplication_baseline_is_atomic_on_replace_failure(tmp_path, monkeypatch):
    target = tmp_path / ".jscpd-baseline.json"
    original = '{"percentage": 1.0, "duplicates": 1, "lines": 10}'
    target.write_text(original, encoding="utf-8")

    def boom(*_args, **_kwargs):
        raise OSError("disk full")

    monkeypatch.setattr(baseline_module.os, "replace", boom)

    with pytest.raises(BaselineError):
        save_duplication_baseline(target, DuplicationBaseline(percentage=0.1))

    assert target.read_text(encoding="utf-8") == original
    assert [path.name for path in tmp_path.iterdir() if path.name != target.name] == []


@given(coverage_baseline_dict(), coverage_baseline_dict())
def test_coverage_ratchet_never_lowers_a_floor(old, measured):
    old_baseline = CoverageBaseline(**old)
    measured_baseline = CoverageBaseline(**measured)
    ratcheted = ratchet_coverage(old_baseline, measured_baseline)
    assert ratcheted.lines == max(old_baseline.lines, measured_baseline.lines)
    assert ratcheted.branches == max(old_baseline.branches, measured_baseline.branches)


@given(coverage_baseline_dict(), coverage_baseline_dict())
def test_coverage_ratchet_is_idempotent(old, measured):
    old_baseline = CoverageBaseline(**old)
    measured_baseline = CoverageBaseline(**measured)
    once = ratchet_coverage(old_baseline, measured_baseline)
    twice = ratchet_coverage(once, measured_baseline)
    assert twice == once


@given(percent(), percent())
def test_duplication_ratchet_never_raises_a_ceiling(old, measured):
    old_baseline = DuplicationBaseline(percentage=old, duplicates=1, lines=10)
    measured_baseline = DuplicationBaseline(percentage=measured, duplicates=2, lines=20)
    ratcheted = ratchet_duplication(old_baseline, measured_baseline)
    assert ratcheted.percentage == min(old, measured)
    assert ratcheted.percentage <= old


@given(percent(), percent())
def test_duplication_ratchet_is_idempotent(old, measured):
    old_baseline = DuplicationBaseline(percentage=old, duplicates=1, lines=10)
    measured_baseline = DuplicationBaseline(percentage=measured, duplicates=2, lines=20)
    once = ratchet_duplication(old_baseline, measured_baseline)
    twice = ratchet_duplication(once, measured_baseline)
    assert twice == once


@given(percent())
def test_ratchet_rejects_non_finite_measurements(value):
    old_baseline = CoverageBaseline(lines=10.0, branches=20.0)
    with pytest.raises(BaselineError):
        ratchet_coverage(
            old_baseline,
            CoverageBaseline(lines=math.nan, branches=value),
        )
