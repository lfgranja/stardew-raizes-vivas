"""Contract tests for the fast, game-free CI jobs (US2)."""

from __future__ import annotations

import re
import subprocess
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
    assert any(entry.get("cron") for entry in config["schedule"]), "a weekly cron is required"


def test_pull_requests_and_pushes_are_not_path_filtered():
    config = trigger_config(load_workflow())
    for event in ("pull_request", "push"):
        event_config = config[event] or {}
        assert "paths" not in event_config, f"{event} must not filter paths"
        assert "paths-ignore" not in event_config, f"{event} must not ignore paths"
    pull_request = config["pull_request"] or {}
    assert "branches" not in pull_request, "pull_request must target every base branch"
    assert "branches-ignore" not in pull_request
    assert "types" not in pull_request, "pull_request must run for every activity type"
    push = config["push"] or {}
    assert "main" in push.get("branches", [])
    assert "dev" in push.get("branches", [])


def test_jobs_run_on_a_standard_ubuntu_runner_with_bounded_time():
    parsed = jobs(load_workflow())
    limits = {"static": 10, "sca": 10, "codeql": 15, "gate": 5}
    for name, limit in limits.items():
        assert name in parsed, f"missing job {name}"
        job = parsed[name]
        assert job["runs-on"] == "ubuntu-latest"
        assert job.get("timeout-minutes"), f"{name} must declare a timeout"
        assert int(job["timeout-minutes"]) <= limit


def test_static_and_sca_run_unconditionally():
    parsed = jobs(load_workflow())
    for name in ("static", "sca"):
        assert "if" not in parsed[name], f"{name} must run on every trigger"


def test_permissions_are_least_privilege():
    workflow = load_workflow()
    assert workflow.get("permissions") == {"contents": "read"}
    parsed = jobs(workflow)
    static_permissions = parsed["static"].get("permissions", {})
    assert static_permissions.get("contents") == "read"
    assert static_permissions.get("security-events") == "write"
    sca_permissions = parsed["sca"].get("permissions", {})
    assert "security-events" not in sca_permissions
    assert sca_permissions.get("contents", "read") == "read"
    gate_permissions = parsed["gate"].get("permissions", {})
    assert "security-events" not in gate_permissions
    assert gate_permissions.get("contents", "read") == "read"
    codeql_permissions = parsed["codeql"].get("permissions", {})
    assert codeql_permissions.get("contents") == "read"
    assert codeql_permissions.get("security-events") == "write"
    assert "write-all" not in yaml.safe_dump(workflow)


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
    assert "--severity ERROR" in command
    assert "--metrics off" in command
    assert "--sarif" in command
    assert "--output semgrep.sarif" in command
    assert "LivingRoots/" in command
    ordered = steps(static)
    install_index = next(
        index for index, step in enumerate(ordered) if "requirements-ci.txt" in step_run(step)
    )
    semgrep_index = ordered.index(semgrep_step)
    assert install_index < semgrep_index, "the pinned Semgrep CLI must be installed before it runs"


def test_semgrep_prints_findings_with_file_and_line():
    static = jobs(load_workflow())["static"]
    semgrep_steps = [step for step in steps(static) if "semgrep scan" in step_run(step)]
    sarif_steps = [step for step in semgrep_steps if "--sarif" in step_run(step)]
    text_steps = [step for step in semgrep_steps if "--sarif" not in step_run(step)]
    assert len(sarif_steps) == 1, "exactly one Semgrep step writes SARIF"
    assert len(text_steps) == 1, "exactly one Semgrep step prints file:line findings"
    text = text_steps[0]
    assert "always()" in str(text.get("if", "")), (
        "the text step must run even when the SARIF step fails"
    )
    command = step_run(text)
    assert "--output" not in command, "the text step must not write a report file"
    assert "--severity ERROR" in command
    assert "--error" in command
    assert "LivingRoots/" in command


def evaluate_sarif_guard(condition: str, event_name: str, head_repo: str) -> bool:
    """Evaluate the fork guard for a concrete event without GitHub Actions."""
    expression = re.sub(r"\$\{\{\s*|\s*\}\}", "", condition).strip()
    expression = expression.replace("github.event_name", repr(event_name))
    expression = expression.replace(
        "github.event.pull_request.head.repo.full_name", repr(head_repo)
    )
    expression = expression.replace(
        "github.event.pull_request.head.repo.fork", repr(head_repo != "owner/repo")
    )
    expression = expression.replace("github.repository", repr("owner/repo"))
    expression = expression.replace("||", " or ").replace("&&", " and ")
    expression = re.sub(r"(?<![=!<>])!(?!=)", " not ", expression)
    assert not re.search(r"github\.", expression), expression
    return bool(eval(expression, {"__builtins__": {}}, {}))


def test_sarif_upload_is_skipped_for_fork_pull_requests():
    static = jobs(load_workflow())["static"]
    uploads = [step for step in steps(static) if "upload-sarif" in str(step.get("uses", ""))]
    assert uploads, "static must upload Semgrep SARIF"
    upload = uploads[0]
    condition = str(upload.get("if", ""))
    assert condition, "SARIF upload needs a fork guard"
    assert evaluate_sarif_guard(condition, "pull_request", "someone/fork") is False
    assert evaluate_sarif_guard(condition, "pull_request", "owner/repo") is True
    assert evaluate_sarif_guard(condition, "push", "") is True
    assert evaluate_sarif_guard(condition, "schedule", "") is True
    assert upload.get("with", {}).get("sarif_file") == "semgrep.sarif"


def test_sarif_guard_evaluator_accepts_the_fork_boolean():
    guard = "${{ github.event_name != 'pull_request' || !github.event.pull_request.head.repo.fork }}"
    assert evaluate_sarif_guard(guard, "pull_request", "someone/fork") is False
    assert evaluate_sarif_guard(guard, "pull_request", "owner/repo") is True
    assert evaluate_sarif_guard(guard, "push", "") is True


def test_codeql_is_scheduled_only_and_buildless():
    workflow = load_workflow()
    codeql = jobs(workflow)["codeql"]
    condition = str(codeql.get("if", ""))
    assert "schedule" in condition
    assert "workflow_dispatch" in condition
    assert "push" in condition, "CodeQL must also run on push to main"
    assert codeql["runs-on"] == "ubuntu-latest"
    assert any("actions/checkout@" in str(step.get("uses", "")) for step in steps(codeql))
    init_steps = [step for step in steps(codeql) if "codeql-action/init@" in str(step.get("uses", ""))]
    assert init_steps, "CodeQL init step is required"
    init_uses = str(init_steps[0]["uses"])
    assert init_uses.endswith("@v4"), "first-party actions use a major tag"
    init_with = init_steps[0].get("with", {})
    assert init_with.get("languages") == "csharp"
    assert init_with.get("build-mode") == "none"
    analyze_steps = [step for step in steps(codeql) if "codeql-action/analyze@" in str(step.get("uses", ""))]
    assert analyze_steps, "CodeQL analyze step is required"
    assert str(analyze_steps[0]["uses"]).endswith("@v4")
    analyze_with = analyze_steps[0].get("with", {})
    assert analyze_with.get("output") == "codeql-results.sarif", (
        "the analyze step must write a named SARIF file for the annotation step"
    )
    uploads = [step for step in steps(codeql) if "upload-sarif" in str(step.get("uses", ""))]
    assert uploads, "CodeQL SARIF must be uploaded"
    assert uploads[0].get("with", {}).get("sarif_file") == "codeql-results.sarif"
    annotation_steps = [
        step
        for step in steps(codeql)
        if "codeql-results.sarif" in step_run(step) and "::error" in step_run(step)
    ]
    assert annotation_steps, "CodeQL results must be surfaced as file:line annotations"
    annotation = step_run(annotation_steps[0])
    assert "file=" in annotation and "line=" in annotation, (
        "the annotation step must emit ::error file=<uri>,line=<startLine>::<message>"
    )
    ordered = steps(codeql)
    assert ordered.index(init_steps[0]) < ordered.index(analyze_steps[0]), (
        "CodeQL init must run before analyze"
    )
    assert ordered.index(analyze_steps[0]) < ordered.index(annotation_steps[0]), (
        "CodeQL must analyze before rendering annotations"
    )
    dumped = yaml.safe_dump(codeql)
    assert "BUILDDEPS_TOKEN" not in dumped
    assert "STARDEW_GAME_PATH" not in dumped
    assert "gamepath.zip" not in dumped
    assert "codeql" not in jobs(workflow)["gate"].get("needs", [])


def test_trivy_is_pinned_and_lets_the_python_gate_own_the_exit_code():
    sca = jobs(load_workflow())["sca"]
    trivy_steps = [step for step in steps(sca) if "aquasecurity/trivy-action@" in str(step.get("uses", ""))]
    assert trivy_steps, "Trivy step is required"
    trivy = trivy_steps[0]
    uses = str(trivy["uses"])
    pin = uses.split("@", 1)[1]
    assert pin == "ed142fd0673e97e23eac54620cfb913e5ce36c25", "the v0.36.0 action must be SHA-pinned"
    assert "0.36.0" in str(trivy.get("name", "")), "the pinned action version must be named in the step"
    with_config = trivy.get("with", {})
    assert str(with_config.get("version")) == "v0.76.1", "the Trivy CLI binary must be pinned"
    assert str(with_config.get("scan-type")) == "fs"
    assert str(with_config.get("scan-ref")) == "."
    assert str(with_config.get("severity")) == "HIGH,CRITICAL"
    assert str(with_config.get("scanners")) == "vuln"
    assert str(with_config.get("exit-code")) == "0"
    assert str(with_config.get("format")) == "json"
    assert str(with_config.get("output")) == "trivy.json"
    skip_dirs = str(with_config.get("skip-dirs", ""))
    assert "bin" in skip_dirs and "obj" in skip_dirs and ".git" in skip_dirs

    ordered = steps(sca)
    trivy_index = ordered.index(trivy)
    gate_index = next(
        index for index, step in enumerate(ordered) if "python -m quality_gate security" in step_run(step)
    )
    assert trivy_index < gate_index
    assert "--report trivy.json" in step_run(ordered[gate_index])


def test_trivy_targets_the_committed_lock_files():
    for relative in ("LivingRoots/packages.lock.json", "LivingRoots.Tests/packages.lock.json"):
        assert (ROOT / relative).exists(), f"{relative} must exist"
        tracked = subprocess.run(
            ["git", "ls-files", "--error-unmatch", relative],
            cwd=ROOT,
            capture_output=True,
            text=True,
            check=False,
        )
        assert tracked.returncode == 0, f"{relative} must be committed"


def test_no_blocking_step_is_allowed_to_continue_on_error():
    blocking_markers = (
        "semgrep scan",
        "trivy",
        "quality_gate",
        "jscpd",
        "dotnet build",
        "dotnet test",
        "reportgenerator",
        "needs.",
        "codeql-action",
    )
    for step in all_steps(load_workflow()):
        run = step_run(step)
        uses = str(step.get("uses", ""))
        if any(marker in run or marker in uses for marker in blocking_markers):
            value = step.get("continue-on-error")
            assert value not in (True, "true"), f"continue-on-error is forbidden on blocking steps: {step}"
    for name in ("static", "sca", "codeql", "gate"):
        value = jobs(load_workflow())[name].get("continue-on-error")
        assert value not in (True, "true"), f"continue-on-error is forbidden on the {name} job"


def test_aggregate_gate_fails_closed_on_skipped_or_failed_jobs():
    gate = jobs(load_workflow())["gate"]
    assert "always()" in str(gate.get("if", ""))
    needs = gate.get("needs", [])
    for required in ("static", "sca"):
        assert required in needs
    run = "\n".join(step_run(step) for step in steps(gate))
    for required in ("static", "sca"):
        assert f"needs.{required}.result" in run
    assert "always()" in str([step for step in steps(gate) if "needs." in step_run(step)][0].get("if", ""))


def render_gate_script(run: str, results: dict[str, str]) -> str:
    script = run
    for job, value in results.items():
        escaped = re.escape(job)
        # Accept both the dot form and the bracket form GitHub Actions allows.
        expression = (
            r"\$\{\{\s*needs(?:\.%s|\[\s*['\"]%s['\"]\s*\])\.result\s*\}\}"
            % (escaped, escaped)
        )
        script = re.sub(expression, value, script)
    # Jobs outside this story's matrix (for example the US3 job) are rendered
    # as success; their behavior is asserted in the US3 test file.
    script = re.sub(
        r"\$\{\{\s*needs(?:\.\w[\w-]*|\[\s*['\"][\w-]+['\"]\s*\])\.result\s*\}\}",
        "success",
        script,
    )
    assert "${{" not in script, "the gate script must only reference rendered results"
    return script


def run_gate(results: dict[str, str]) -> subprocess.CompletedProcess[str]:
    gate = jobs(load_workflow())["gate"]
    step = next(step for step in steps(gate) if "needs." in step_run(step))
    script = render_gate_script(step_run(step), results)
    return subprocess.run(
        ["bash", "-c", script],
        capture_output=True,
        text=True,
        check=False,
    )


def test_aggregate_gate_behavior_for_every_job_result():
    success = {"static": "success", "sca": "success"}
    assert run_gate(success).returncode == 0
    for job in success:
        for value in ("failure", "cancelled", "skipped"):
            results = dict(success)
            results[job] = value
            outcome = run_gate(results)
            assert outcome.returncode == 1, f"{job}={value} must fail the gate"
            assert "::error" in outcome.stdout + outcome.stderr


def test_static_and_sca_do_not_require_the_game():
    workflow = load_workflow()
    for name in ("static", "sca"):
        dumped = yaml.safe_dump(jobs(workflow)[name])
        assert "STARDEW_GAME_PATH" not in dumped
        assert "gamepath.zip" not in dumped
        assert "BUILDDEPS_TOKEN" not in dumped
