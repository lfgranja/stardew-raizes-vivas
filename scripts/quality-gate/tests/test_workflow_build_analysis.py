"""Contract tests for the build-analysis job and US3 tooling (US3)."""

from __future__ import annotations

import json
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Any

import yaml

ROOT = Path(__file__).resolve().parents[3]
WORKFLOW = ROOT / ".github" / "workflows" / "quality-gate.yml"
TOOLS = ROOT / "dotnet-tools.json"
TEST_PROJECT = ROOT / "LivingRoots.Tests" / "LivingRoots.Tests.csproj"


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
    dumped = yaml.safe_dump(job)
    assert "BUILDDEPS_TOKEN" in dumped
    assert "GH_TOKEN" in dumped
    assert "gamepath-v1" in dumped
    assert "gamepath.zip" in dumped
    assert "gh release download" in dumped
    assert "GamePath" in dumped
    assert "BUILDDEPS_TOKEN is required" in dumped or "Missing build dependencies token" in dumped
    assert "exit 1" in dumped, "missing token must fail closed"


def test_build_analysis_restores_locked_and_builds_without_restore():
    text = run_text(jobs(load_workflow())["build-analysis"])
    assert "dotnet restore --locked-mode" in text
    assert "dotnet build --no-restore" in text
    assert "-c Release" in text or "--configuration Release" in text


def test_build_analysis_collects_coverage_and_runs_the_gate():
    job = jobs(load_workflow())["build-analysis"]
    text = run_text(job)
    assert '--collect:"XPlat Code Coverage"' in text or "--collect:'XPlat Code Coverage'" in text
    assert "dotnet tool restore" in text
    assert "dotnet reportgenerator" in text
    assert "-reporttypes:'JsonSummary;TextSummary'" in text or '"-reporttypes:JsonSummary;TextSummary"' in text
    assert "python -m quality_gate coverage" in text
    assert ".coverage-baseline" in text
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


def test_jscpd_is_pinned_csharp_only_and_gated():
    workflow = load_workflow()
    static = jobs(workflow)["static"]
    jscpd_steps = [step for step in steps(static) if "jscpd@" in str(step.get("run", ""))]
    assert jscpd_steps, "jscpd step is required in static"
    command = str(jscpd_steps[0]["run"])
    assert "npx --yes jscpd@5.4.2" in command
    assert "--format csharp" in command
    assert "--min-lines 5" in command
    assert "--min-tokens 50" in command
    assert "--reporters json" in command
    assert "--output jscpd-report" in command

    gate_steps = [step for step in steps(static) if "quality_gate duplication" in str(step.get("run", ""))]
    assert gate_steps, "the jscpd report must be gated"
    assert "--report jscpd-report/jscpd-report.json" in str(gate_steps[0]["run"])
    assert ".jscpd-baseline.json" in str(gate_steps[0]["run"])

    uploads = [step for step in steps(static) if "upload-artifact" in str(step.get("uses", ""))]
    assert any("jscpd" in str(step.get("with", {})).lower() for step in uploads)


def test_reportgenerator_and_coverlet_are_pinned():
    tools = json.loads(TOOLS.read_text(encoding="utf-8"))
    reportgenerator = tools["tools"]["dotnet-reportgenerator-globaltool"]
    assert reportgenerator["version"] == "5.5.12"

    tree = ET.parse(TEST_PROJECT)
    references = {
        element.get("Include"): element.get("Version")
        for element in tree.getroot().iter()
        if element.tag.endswith("PackageReference")
    }
    assert references.get("coverlet.collector") == "6.0.0"
