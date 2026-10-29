"""Contract tests for the build-analysis job (US3)."""

from __future__ import annotations

from pathlib import Path
from typing import Any

import yaml

ROOT = Path(__file__).resolve().parents[3]
WORKFLOW = ROOT / ".github" / "workflows" / "quality-gate.yml"


def load_workflow() -> dict[str, Any]:
    assert WORKFLOW.exists(), f"{WORKFLOW} must exist"
    parsed = yaml.safe_load(WORKFLOW.read_text(encoding="utf-8"))
    assert isinstance(parsed, dict)
    return parsed


def jobs(workflow: dict[str, Any]) -> dict[str, Any]:
    parsed = workflow.get("jobs")
    assert isinstance(parsed, dict)
    return parsed


def steps(job: dict[str, Any]) -> list[dict[str, Any]]:
    return list(job.get("steps", []))


def run_text(job: dict[str, Any]) -> str:
    return "\n".join(str(step.get("run", "")) for step in steps(job))


def test_build_analysis_job_exists_on_a_standard_runner():
    job = jobs(load_workflow()).get("build-analysis")
    assert job is not None, "build-analysis job is required"
    assert job.get("runs-on") == "ubuntu-latest"
    assert job.get("timeout-minutes"), "build-analysis must declare a timeout"
    assert int(job["timeout-minutes"]) <= 20


def test_build_analysis_materialises_the_pinned_build_deps_and_fails_closed():
    job = jobs(load_workflow())["build-analysis"]
    text = run_text(job)
    assert "BUILDDEPS_TOKEN" in text
    assert "gamepath.zip" in text
    assert "exit 1" in text, "missing token must fail closed"
    assert "BUILDDEPS_TOKEN" in str(job.get("env", {})) or "BUILDDEPS_TOKEN" in text


def test_build_analysis_restores_locked_and_builds_without_restore():
    text = run_text(jobs(load_workflow())["build-analysis"])
    assert "dotnet restore --locked-mode" in text
    assert "dotnet build --no-restore" in text
    assert "-c Release" in text


def test_build_analysis_collects_coverage_and_runs_the_gate():
    job = jobs(load_workflow())["build-analysis"]
    text = run_text(job)
    assert '--collect:"XPlat Code Coverage"' in text or "--collect:'XPlat Code Coverage'" in text
    assert "dotnet tool restore" in text
    assert "dotnet reportgenerator" in text
    assert "JsonSummary" in text
    assert "python -m quality_gate coverage" in text
    assert ".coverage-baseline" in text
    assert "python -m quality_gate coverage" in text
    assert "PYTHONPATH" in str(job.get("env", {})) or "PYTHONPATH" in text


def test_coverage_artifacts_are_uploaded():
    job = jobs(load_workflow())["build-analysis"]
    uploads = [step for step in steps(job) if "upload-artifact" in str(step.get("uses", ""))]
    assert uploads, "coverage artifacts must be uploaded"
    assert any(
        "coverage" in str(step.get("with", {}).get("path", "")).lower() for step in uploads
    )


def test_aggregate_gate_requires_build_analysis():
    gate = jobs(load_workflow())["gate"]
    assert "build-analysis" in gate.get("needs", [])
    assert "always()" in str(gate.get("if", ""))
