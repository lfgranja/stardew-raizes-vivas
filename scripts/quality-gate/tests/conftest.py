"""Shared Hypothesis settings and generators for the quality-gate tests."""

from __future__ import annotations

import json
from typing import Any

from hypothesis import HealthCheck, settings
from hypothesis import strategies as st

settings.register_profile(
    "quality-gate",
    max_examples=100,
    deadline=None,
    suppress_health_check=[HealthCheck.too_slow],
)
settings.load_profile("quality-gate")


def percent() -> st.SearchStrategy[float]:
    """A finite percentage in [0, 100]."""
    return st.floats(
        min_value=0.0,
        max_value=100.0,
        allow_nan=False,
        allow_infinity=False,
        width=64,
    )


def coverage_pair() -> st.SearchStrategy[tuple[float, float]]:
    return st.tuples(percent(), percent())


@st.composite
def coverage_baseline_dict(draw: st.DrawFn) -> dict[str, float]:
    lines, branches = draw(coverage_pair())
    return {"lines": lines, "branches": branches}


@st.composite
def reportgenerator_summary(draw: st.DrawFn) -> dict[str, Any]:
    lines, branches = draw(coverage_pair())
    classes = []
    for index in range(draw(st.integers(min_value=0, max_value=4))):
        classes.append(
            {
                "name": f"LivingRoots.Class{index}",
                "filename": f"LivingRoots/Class{index}.cs",
                "linecoverage": draw(percent()),
                "branchcoverage": draw(percent()),
            }
        )
    return {
        "summary": {"linecoverage": lines, "branchcoverage": branches},
        "coverage": {
            "assemblies": [
                {
                    "name": "LivingRoots",
                    "classesinassembly": classes,
                }
            ]
        },
    }


@st.composite
def jscpd_report(draw: st.DrawFn) -> dict[str, Any]:
    clones = []
    for index in range(draw(st.integers(min_value=0, max_value=4))):
        clones.append(
            {
                "format": "csharp",
                "lines": draw(st.integers(min_value=5, max_value=200)),
                "firstFile": {
                    "name": f"LivingRoots/First{index}.cs",
                    "start": draw(st.integers(min_value=1, max_value=500)),
                },
                "secondFile": {
                    "name": f"LivingRoots/Second{index}.cs",
                    "start": draw(st.integers(min_value=1, max_value=500)),
                },
            }
        )
    return {
        "statistics": {
            "total": {
                "percentage": draw(percent()),
                "duplicates": len(clones),
                "lines": sum(clone["lines"] for clone in clones),
            }
        },
        "duplicates": clones,
    }


@st.composite
def trivy_report(draw: st.DrawFn) -> dict[str, Any]:
    severities = ["LOW", "MEDIUM", "HIGH", "CRITICAL", "UNKNOWN"]
    vulnerabilities = []
    for index in range(draw(st.integers(min_value=0, max_value=5))):
        vulnerabilities.append(
            {
                "VulnerabilityID": f"CVE-2026-{index:04d}",
                "PkgName": f"Package{index}",
                "InstalledVersion": f"1.0.{index}",
                "FixedVersion": f"1.0.{index + 1}",
                "Severity": draw(st.sampled_from(severities)),
            }
        )
    return {
        "Results": [
            {
                "Target": "LivingRoots/packages.lock.json",
                "Vulnerabilities": vulnerabilities,
            }
        ]
    }


def write_json(path, payload: Any) -> None:
    path.write_text(json.dumps(payload), encoding="utf-8")
