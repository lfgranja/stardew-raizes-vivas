"""Contract tests for the fast, game-free CI jobs (US2)."""

from __future__ import annotations

import re
from pathlib import Path
from typing import Any

import yaml

ROOT = Path(__file__).resolve().parents[3]
WORKFLOW = ROOT / ".github" / "workflows" / "quality-gate.yml"
REQUIREMENTS_CI = ROOT / "scripts" / "quality-gate" / "requirements-ci.txt"


def load_workflow() -> dict[str, Any]:
    assert WORKFLOW.exists(), f"{WORKFLOW} must exist"
    parsed = yaml.safe_load(WORKFLOW.read_text(encoding="utf-8"))
    assert isinstance(parsed, dict), "workflow must parse to a mapping"
    return parsed


def jobs(workflow: dict[str, Any]) -> dict[str, Any]:
    parsed_jobs = workflow.get("jobs")
    assert isinstance(parsed_jobs, dict), "workflow must declare jobs"
    return parsed_jobs


def steps(job: dict[str, Any]) -> list[dict[str, Any]]:
    parsed_steps = job.get("steps", [])
    assert isinstance(parsed_steps, list), "job steps must be a list"
    return parsed_steps


def all_steps(workflow: dict[str, Any]) -> list[dict[str, Any]]:
    return [step for job in jobs(workflow).values() for step in steps(job)]


def trigger_config(workflow: dict[str, Any]) -> dict[str, Any]:
    # PyYAML resolves the unquoted `on:` key to boolean True (YAML 1.1).
    config = workflow.get("on", workflow.get(True))
    assert isinstance(config, dict), "workflow must declare an on: mapping"
    return config


def step_run(step: dict[str, Any]) -> str:
    return str(step.get("run", ""))


def find_run_step(job: dict[str, Any], needle: str) -> dict[str, Any]:
    for step in steps(job):
        if needle in step_run(step):
            return step
    raise AssertionError(f"no step in job runs {needle!r}")


def test_workflow_is_triggered_on_pr_push_schedule_and_dispatch():
    config = trigger_config(load_workflow())
    assert "pull_request" in config
    assert "push" in config
    assert "schedule" in config
    assert "workflow_dispatch" in config
    push = config["push"] or {}
    branches = push.get("branches", [])
    assert "main" in branches
    assert "dev" in branches


def test_jobs_run_on_a_standard_ubuntu_runner():
    parsed = jobs(load_workflow())
    for name in ("static", "sca"):
        assert name in parsed, f"missing job {name}"
        assert parsed[name]["runs-on"] == "ubuntu-latest"


def test_permissions_are_least_privilege():
    workflow = load_workflow()
    assert workflow.get("permissions") == {"contents": "read"}
    parsed = jobs(workflow)
    assert parsed["static"].get("permissions", {}).get("security-events") == "write"
    assert parsed["sca"].get("permissions", {"contents": "read"}).get("security-events") is None
    assert parsed["codeql"].get("permissions", {}).get("security-events") == "write"
    dumped = yaml.safe_dump(workflow)
    assert "write-all" not in dumped


def test_semgrep_uses_the_pinned_cli_not_the_archived_action():
    workflow = load_workflow()
    dumped = yaml.safe_dump(workflow)
    assert "semgrep/semgrep-action" not in dumped
    assert REQUIREMENTS_CI.exists()
    assert "semgrep==1.182.1" in REQUIREMENTS_CI.read_text(encoding="utf-8")
    static = jobs(workflow)["static"]
    semgrep_step = find_run_step(static, "semgrep scan")
    command = step_run(semgrep_step)
    assert "--config p/csharp" in command
    assert "--error" in command
    assert "LivingRoots/" in command
    assert any("requirements-ci.txt" in step_run(step) for step in steps(static))


def test_sarif_upload_is_skipped_for_fork_pull_requests():
    static = jobs(load_workflow())["static"]
    uploads = [step for step in steps(static) if "upload-sarif" in str(step.get("uses", ""))]
    assert uploads, "static must upload Semgrep SARIF"
    assert uploads[0].get("if"), "SARIF upload needs a fork guard"


def test_codeql_is_scheduled_only_and_buildless():
    workflow = load_workflow()
    codeql = jobs(workflow)["codeql"]
    condition = str(codeql.get("if", ""))
    assert "schedule" in condition
    assert "workflow_dispatch" in condition
    assert "pull_request" not in condition
    init_steps = [step for step in steps(codeql) if "codeql-action/init@" in str(step.get("uses", ""))]
    assert init_steps, "CodeQL init step is required"
    init_with = init_steps[0].get("with", {})
    assert init_with.get("languages") == "csharp"
    assert init_with.get("build-mode") == "none"
    assert any("codeql-action/analyze@" in str(step.get("uses", "")) for step in steps(codeql))
    dumped = yaml.safe_dump(codeql)
    assert "BUILDDEPS_TOKEN" not in dumped
    assert "codeql" not in jobs(workflow)["gate"].get("needs", [])


def test_trivy_is_pinned_and_lets_the_python_gate_own_the_exit_code():
    sca = jobs(load_workflow())["sca"]
    trivy_steps = [step for step in steps(sca) if "aquasecurity/trivy-action@" in str(step.get("uses", ""))]
    assert trivy_steps, "Trivy step is required"
    trivy = trivy_steps[0]
    uses = str(trivy["uses"])
    pin = uses.split("@", 1)[1]
    assert re.fullmatch(r"[0-9a-f]{40}", pin), "third-party actions must be pinned to a commit SHA"
    assert "0.36.0" in str(trivy.get("name", "")) + uses
    with_config = trivy.get("with", {})
    assert str(with_config.get("severity")) == "HIGH,CRITICAL"
    assert str(with_config.get("scanners")) == "vuln"
    assert str(with_config.get("exit-code")) == "0"
    assert str(with_config.get("format")) == "json"

    ordered = steps(sca)
    trivy_index = ordered.index(trivy)
    gate_index = next(
        index for index, step in enumerate(ordered) if "python -m quality_gate security" in step_run(step)
    )
    assert trivy_index < gate_index


def test_no_blocking_step_is_allowed_to_continue_on_error():
    for step in all_steps(load_workflow()):
        value = step.get("continue-on-error")
        assert value not in (True, "true"), f"continue-on-error is forbidden: {step}"


def test_aggregate_gate_fails_closed_on_skipped_or_failed_jobs():
    gate = jobs(load_workflow())["gate"]
    assert "always()" in str(gate.get("if", ""))
    needs = gate.get("needs", [])
    assert "static" in needs
    assert "sca" in needs
    run = "\n".join(step_run(step) for step in steps(gate))
    assert "needs." in run
    assert "exit 1" in run


def test_static_and_sca_do_not_require_the_game():
    workflow = load_workflow()
    for name in ("static", "sca"):
        dumped = yaml.safe_dump(jobs(workflow)[name])
        assert "STARDEW_GAME_PATH" not in dumped
        assert "gamepath.zip" not in dumped
        assert "BUILDDEPS_TOKEN" not in dumped
