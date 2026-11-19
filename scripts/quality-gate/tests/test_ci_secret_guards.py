"""Contract tests for secret-dependent CI jobs (spec 025).

Every non-best-effort workflow step that consumes the private build credential
must fail fast with an explicit diagnostic before first use. Best-effort jobs
(``continue-on-error: true`` or an intentional ``::warning::`` path) are exempt.
"""

from __future__ import annotations

from pathlib import Path
from typing import Any

import yaml

ROOT = Path(__file__).resolve().parents[3]
WORKFLOW_DIR = ROOT / ".github" / "workflows"
TOKEN_REF = "secrets.BUILDDEPS_TOKEN"
ERROR_MARKER = "BUILDDEPS_TOKEN is not set"


def load_workflow(path: Path) -> dict[str, Any]:
    parsed = yaml.safe_load(path.read_text(encoding="utf-8"))
    assert isinstance(parsed, dict), f"{path} must parse to a mapping"
    return parsed


def step_body(step: dict[str, Any]) -> str:
    return str(step.get("run", ""))


def is_owner_gated(workflow: dict[str, Any]) -> bool:
    """Tag-only release automation is owner-gated and out of scope (spec §Assumptions)."""
    trigger = workflow.get("on", workflow.get(True, {}))
    if not isinstance(trigger, dict):
        return False
    push = trigger.get("push", {})
    return (
        "pull_request" not in trigger
        and isinstance(push, dict)
        and bool(push.get("tags"))
        and not push.get("branches")
    )


def token_consumers(workflows: dict[str, dict[str, Any]]):
    """Yield (file, job_id, step, best_effort) for each in-scope credential consumer."""
    for filename, workflow in workflows.items():
        if is_owner_gated(workflow):
            continue
        jobs = workflow.get("jobs", {})
        assert isinstance(jobs, dict), f"{filename} must declare jobs"
        for job_id, job in jobs.items():
            assert isinstance(job, dict), f"{filename}:{job_id} must be a mapping"
            best_effort = bool(job.get("continue-on-error", False))
            for step in job.get("steps", []):
                env = step.get("env", {})
                if isinstance(env, dict) and TOKEN_REF in str(env.get("GH_TOKEN", "")):
                    yield filename, job_id, step, best_effort


def find_offenders(workflows: dict[str, dict[str, Any]]) -> list[str]:
    offenders: list[str] = []
    for filename, job_id, step, best_effort in token_consumers(workflows):
        body = step_body(step)
        if best_effort or "::warning::" in body:
            continue
        has_guard = ERROR_MARKER in body and "exit 1" in body
        guard_before_use = has_guard and body.index(ERROR_MARKER) < body.index("gh release download") if "gh release download" in body else has_guard
        if not guard_before_use:
            offenders.append(f"{filename}:{job_id}:{step.get('name', '<unnamed>')}")
    return offenders


def real_workflows() -> dict[str, dict[str, Any]]:
    return {path.name: load_workflow(path) for path in sorted(WORKFLOW_DIR.glob("*.yml"))}


def synthetic_workflow(guarded: bool, warning: bool = False, job_best_effort: bool = False) -> dict[str, Any]:
    guard = (
        'set -euo pipefail\nif [ -z "${GH_TOKEN:-}" ]; then\n'
        '  echo "::error::BUILDDEPS_TOKEN is not set; cannot download the private build-deps release."\n'
        "  exit 1\nfi\n"
        if guarded
        else "set -euo pipefail\n"
    )
    if warning:
        guard = 'set -euo pipefail\necho "::warning::BUILDDEPS_TOKEN is not set; headless verification is best-effort."\n'
    job: dict[str, Any] = {
        "runs-on": "ubuntu-latest",
        "steps": [
            {
                "name": "Fetch game assemblies",
                "env": {"GH_TOKEN": "${{ secrets.BUILDDEPS_TOKEN }}"},
                "run": guard + 'gh release download gamepath-v1 --dir "${GamePath}"\n./build.sh\n',
            }
        ],
    }
    if job_best_effort:
        job["continue-on-error"] = True
    return {"jobs": {"build-test": job}}


def test_every_non_best_effort_token_consumer_is_guarded():
    offenders = find_offenders(real_workflows())
    assert offenders == [], "unguarded credential consumers: " + ", ".join(offenders)


def test_synthetic_unguarded_consumer_is_detected():
    offenders = find_offenders({"synthetic.yml": synthetic_workflow(guarded=False)})
    assert offenders == ["synthetic.yml:build-test:Fetch game assemblies"]


def test_best_effort_warning_consumer_is_exempt():
    assert find_offenders({"synthetic.yml": synthetic_workflow(guarded=False, warning=True)}) == []


def test_best_effort_job_is_exempt():
    assert find_offenders({"synthetic.yml": synthetic_workflow(guarded=False, job_best_effort=True)}) == []


EXISTING_COMMANDS = 'gh release download gamepath-v1 --dir "${GamePath}"\n./build.sh\n'


def test_credential_present_path_leaves_existing_commands_unchanged():
    """FR-003: the guard is additive; the original step body survives intact."""
    unguarded = synthetic_workflow(guarded=False)
    guarded = synthetic_workflow(guarded=True)
    original = step_body(next(token_consumers({"synthetic.yml": unguarded}))[2])
    after = step_body(next(token_consumers({"synthetic.yml": guarded}))[2])
    assert original.endswith(EXISTING_COMMANDS)
    assert after.endswith(EXISTING_COMMANDS)
    assert "::error::" not in original
    assert "::error::" in after
