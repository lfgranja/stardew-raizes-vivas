#!/usr/bin/env python3
"""Spec 010 remediation consistency check.

This is a repository-level static test. It encodes the acceptance criteria of
the 2026-10-27 read-only analysis remediation. It does not implement the
runner and does not attest runtime behaviour; it fails on contract drift.

Run: python3 scripts/tests/spec-consistency.py
Exit: 0 all checks pass; 1 one or more checks fail.
"""

from __future__ import annotations

import glob
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SPEC = ROOT / "specs" / "010-headless-verification-runner"
REL = "specs/010-headless-verification-runner"

failures: list[str] = []
checks = 0


def check(condition: bool, message: str) -> None:
    global checks
    checks += 1
    if not condition:
        failures.append(message)


def read(path: Path) -> str:
    try:
        return path.read_text(encoding="utf-8")
    except FileNotFoundError:
        failures.append(f"missing file: {path.relative_to(ROOT)}")
        return ""


def load_json(path: Path):
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError:
        failures.append(f"missing file: {path.relative_to(ROOT)}")
        return None
    except json.JSONDecodeError as exc:
        failures.append(f"invalid JSON in {path.relative_to(ROOT)}: {exc}")
        return None


def section(text: str, start: str, end: str) -> str:
    pattern = re.escape(start) + r"(.*?)" + re.escape(end)
    match = re.search(pattern, text, re.S)
    return match.group(0) if match else ""


def backticked(cell: str) -> set[str]:
    return set(re.findall(r"`([^`]+)`", cell))


def walk(node):
    if isinstance(node, dict):
        yield node
        for value in node.values():
            yield from walk(value)
    elif isinstance(node, list):
        for value in node:
            yield from walk(value)


# ---------------------------------------------------------------------------
# 1. Gate-3 transition closure and status identity
# ---------------------------------------------------------------------------
template = read(SPEC / "contracts" / "test-md-template.md")
promotion = section(template, "## Promotion set", "## Claim discipline")
rows = []
for line in promotion.splitlines():
    if not line.startswith("|"):
        continue
    cells = [c.strip() for c in line.strip().strip("|").split("|")]
    if len(cells) < 2 or cells[0] in {"From", "------"} or set(cells[0]) <= {"-", " "}:
        continue
    backticked_cells = [
        re.sub(r"\s*\(.*?\)", "", c).strip().strip("`").strip() for c in cells[:2]
    ]
    rows.append((backticked_cells[0], backticked_cells[1]))
expected_transitions = {
    ("partial", "machine-pass"),
    ("machine-pass", "machine-pass"),
    ("machine-pass|verified", "partial"),
    ("any", "partial"),
    ("partial|machine-pass", "verified"),
    ("verified", "verified"),
}
check(
    set(rows) == expected_transitions,
    "test-md-template.md promotion table must contain exactly the six C-REL-15 transitions; "
    f"found {sorted(rows)}",
)
check(
    ("verified", "machine-pass") not in rows,
    "test-md-template.md still contains the illegal verified -> machine-pass transition",
)
check(
    "same closed value" in template and "gate3Status" in template,
    "test-md-template.md must state that testMdStatus and gate3Status are the same closed value",
)

# ---------------------------------------------------------------------------
# 2. Exit-code contract fully enumerated + helper subcommands
# ---------------------------------------------------------------------------
exit_doc = read(SPEC / "contracts" / "exit-codes.md")
exit_codes = set()
for line in exit_doc.splitlines():
    match = re.match(r"^\|\s*(\d+)\s*\|", line)
    if match:
        exit_codes.add(int(match.group(1)))
check(
    exit_codes >= set(range(0, 23)),
    "exit-codes.md must enumerate every code 0-22 as its own row; missing "
    f"{sorted(set(range(0, 23)) - exit_codes)}",
)
check(
    "EX_MOD_RESOLUTION_ABORTED" in exit_doc and "EX_GAME_NOT_FOUND" in exit_doc,
    "exit-codes.md inheritance rows must name the spec-008 EX_* constants",
)
helper_section = section(exit_doc, "## Helper subcommands", "\n## ")
excluded_helper_section = helper_section
check(
    bool(helper_section),
    "exit-codes.md must contain a '## Helper subcommands' section",
)
for verb in ("validate-scenario", "evaluate", "extract", "build-manifest"):
    check(
        verb in excluded_helper_section,
        f"exit-codes.md helper section must document `{verb}`",
    )
ignore_lines = [line for line in exit_doc.splitlines() if "--ignore-exit-code" in line]
check(
    any("0" in line and "22" in line and "unknown" in line for line in ignore_lines),
    "exit-codes.md must state that --ignore-exit-code rejects 0, 22, and unknown values",
)

# ---------------------------------------------------------------------------
# 3. Manifest required fields and conditional R/E evidence
# ---------------------------------------------------------------------------
evidence = load_json(SPEC / "contracts" / "evidence-bundle.schema.json")
if evidence is not None:
    required = set(evidence.get("required", []))
    expected_required = {
        "schemaVersion",
        "runId",
        "timestamp",
        "toolVersions",
        "gameVersion",
        "modVersion",
        "environment",
        "expectedSignals",
        "subject",
        "verdict",
        "honestyLevel",
        "attempts",
        "claimsFile",
        "artifacts",
    }
    check(
        required == expected_required,
        "evidence-bundle.schema.json required set must equal the published manifest set; "
        f"missing={sorted(expected_required - required)} extra={sorted(required - expected_required)}",
    )
    re_condition = None
    for node in walk(evidence):
        if "if" not in node or "then" not in node:
            continue
        if_node = node["if"]
        then_node = node["then"]
        level = (
            if_node.get("properties", {})
            .get("honestyLevel", {})
            .get("enum")
        )
        then_required = set(then_node.get("required", []))
        if level and set(level) >= {"R", "E"} and {"commands", "materials", "seeds"} <= then_required:
            re_condition = node
            break
    check(
        re_condition is not None,
        "evidence-bundle.schema.json must conditionally require commands, materials, and seeds for honestyLevel R/E",
    )
    attempts = evidence.get("properties", {}).get("attempts", {})
    check(
        attempts.get("minItems") == 0,
        "evidence-bundle.schema.json attempts must allow [] for pre-attempt failures (minItems: 0)",
    )
    override_pattern = (
        evidence.get("$defs", {})
        .get("overrideEntry", {})
        .get("properties", {})
        .get("key", {})
        .get("pattern", "")
    )
    check(
        "gamePath" in override_pattern and "milestoneSave" in override_pattern,
        "evidence-bundle.schema.json environmentOverrides must allow gamePath and milestoneSave",
    )
    serialized = json.dumps(evidence)
    check(
        '"skipBuild"' not in serialized,
        "evidence-bundle.schema.json must use the canonical `buildSkipped`, not `skipBuild`",
    )
    check(
        (SPEC / "contracts" / "claims.schema.json").is_file(),
        "contracts/claims.schema.json is missing",
    )
    claims = None
    if (SPEC / "contracts" / "claims.schema.json").is_file():
        claims = load_json(SPEC / "contracts" / "claims.schema.json")
    if claims is not None:
        claim_required = {"claimId", "evidencePointer", "risk", "honestyLevel", "safeWording", "refusalSite"}
        claim_defs = claims.get("$defs", {})
        claim_obj = claim_defs.get("claim", {})
        check(
            claim_required <= set(claim_obj.get("required", [])),
            "claims.schema.json claim object must require " + ", ".join(sorted(claim_required)),
        )
        check(
            claims.get("required") is not None,
            "claims.schema.json must declare a root required array",
        )

spec = read(SPEC / "spec.md")
cbnd = section(spec, "**C-BND-01", "\n- **C-BND-02")
check(
    all(field in cbnd for field in ("claimsFile", "artifacts", "commands", "materials", "seeds")),
    "spec.md C-BND-01 must list claimsFile, artifacts, and conditional commands/materials/seeds",
)

# ---------------------------------------------------------------------------
# 4. Scenario required-field mirror and step-input conditionals
# ---------------------------------------------------------------------------
scenario = load_json(SPEC / "contracts" / "scenario.schema.json")
data_model = read(SPEC / "data-model.md")
if scenario is not None:
    mirror_section = section(data_model, "#### Nested required-field mirror", "#### Absence vs. null vs. default")
    mirror_rows: dict[str, str] = {}
    for line in mirror_section.splitlines():
        if not line.startswith("|") or line.startswith("| Object path") or line.startswith("|----"):
            continue
        cells = [c.strip() for c in line.strip().strip("|").split("|")]
        if len(cells) >= 2:
            mirror_rows[cells[0]] = cells[1]
    root_required = set(scenario.get("required", []))
    root_cell = mirror_rows.get("`#` (root)", "")
    check(
        backticked(root_cell) == root_required,
        "data-model.md root required row must equal scenario.schema.json root required; "
        f"missing={sorted(root_required - backticked(root_cell))} extra={sorted(backticked(root_cell) - root_required)}",
    )
    env_required = set(scenario.get("$defs", {}).get("environment", {}).get("required", []))
    # The schema's environment is an inline property, not a $def; fall back to it.
    env_required = set(
        scenario.get("properties", {}).get("environment", {}).get("required", env_required)
    )
    env_cell = mirror_rows.get("`environment`", "")
    check(
        backticked(env_cell) == env_required,
        "data-model.md environment required row must equal scenario.schema.json",
    )
    inputs_cell = mirror_rows.get("`steps[].inputs`", "")
    for token in ("insertWaste", "itemId", "count", "setSoilHealth", "soilHealth", "runMacro", "macroFile"):
        check(
            token in inputs_cell,
            f"data-model.md steps[].inputs row must document the {token} conditional requirement",
        )
    check(
        "`hostGuard` (farmhand only)" in mirror_section,
        "data-model.md must retain the conditional hostGuard required-field row",
    )
    between = None
    predicate = scenario.get("$defs", {}).get("predicate", {})
    for variant in predicate.get("oneOf", []):
        props = variant.get("properties", {})
        if props.get("op", {}).get("const") == "between":
            between = variant
            break
    if between is not None:
        tolerance = between.get("properties", {}).get("tolerance", {})
        check(
            tolerance.get("default") == 0.0001,
            "scenario.schema.json between predicate must default tolerance to 0.0001",
        )
    step = scenario.get("$defs", {}).get("step", {})
    step_allof = json.dumps(step.get("allOf", []))
    for action in ("insertWaste", "setSoilHealth", "runMacro"):
        check(
            action in step_allof,
            f"scenario.schema.json step conditions must cover {action}",
        )

# ---------------------------------------------------------------------------
# 5. Registries resolve and promotion triggers have the right shape
# ---------------------------------------------------------------------------
never = load_json(SPEC / "never-quarantine.json")
never_schema = load_json(SPEC / "contracts" / "never-quarantine.schema.json")
if never is not None and never_schema is not None:
    classes = never.get("classes", [])
    enum = (
        never_schema.get("$defs", {})
        .get("neverQuarantineClass", {})
        .get("enum", [])
    )
    check(
        len(classes) == 10 and set(classes) == set(enum),
        "never-quarantine.json must carry exactly the ten closed classes from its schema",
    )
    scenario_index = {}
    for path in glob.glob(str(ROOT / "specs" / "**" / "*.scenario.json"), recursive=True):
        data = load_json(Path(path))
        if not isinstance(data, dict):
            continue
        key = (data.get("featureSlug"), data.get("scenarioId"))
        scenario_index[key] = path
    for entry in never.get("criticalPathScenarios", []):
        key = (entry.get("featureSlug"), entry.get("scenarioId"))
        check(
            key in scenario_index,
            f"never-quarantine criticalPathScenario {key} does not resolve to a committed scenario JSON",
        )

promotion_triggers = load_json(SPEC / "promotion-triggers.json")
if promotion_triggers is not None:
    check(
        isinstance(promotion_triggers.get("components"), list)
        and isinstance(promotion_triggers.get("transitions"), list),
        "promotion-triggers.json must use {schemaVersion, components[], transitions[]}",
    )
    check(
        promotion_triggers.get("transitions") == [],
        "promotion-triggers.json transitions[] must start empty and be appended by the runner",
    )

# ---------------------------------------------------------------------------
# 6. Task-plan coverage for every remediation gap
# ---------------------------------------------------------------------------
tasks = read(SPEC / "tasks.md")


def task_line(task_id: str) -> str:
    match = re.search(rf"^- \[[ x]\] {re.escape(task_id)}\b.*$", tasks, re.M)
    return match.group(0) if match else ""


required_tasks = {
    "T005": ["build-manifest"],
    "T013": ["claims.schema.json"],
    "T028": ["testMdPath", "marker"],
    "T034b": ["root"],
    "T034c": ["C-SEC-32"],
    "T061b": ["suite.sh"],
    "T061c": ["run-all"],
    "T061d": ["C-SCN-36"],
    "T064": ["deployment", "12"],
    "T067": ["xdotool", "stdin"],
    "T071": ["commands", "materials", "seeds"],
    "T075": ["idempotent", "QUARANTINE_OWNER"],
    "T076": ["Wilson", "RAFT"],
    "T082": ["commands", "materials", "seeds"],
    "T094b": ["quit"],
    "T099": ["--quarantine-owner"],
    "T100b": ["C-REL-25"],
    "T104": ["T103", "T099"],
    "T110": ["zero", "runner-source"],
    "T122b": ["security-aggregation"],
    "T125b": ["doc-link"],
    "T136": ["VS-09b", "VS-09c"],
    "T137": ["gamepath.lock"],
    "T146": ["T141", "T145"],
}
for task_id, tokens in required_tasks.items():
    line = task_line(task_id)
    check(bool(line), f"tasks.md is missing required remediation task {task_id}")
    for token in tokens:
        check(
            token.lower() in line.lower(),
            f"tasks.md {task_id} must mention `{token}`",
        )

check(
    "Implement `scripts/lib/suite.sh`" not in task_line("T110"),
    "T110 must not implement scripts/lib/suite.sh; suite discovery moved to Phase 2 T061b",
)
check(
    "--mods-path" not in tasks,
    "tasks.md still contains the removed --mods-path flag",
)
check(
    "CODEOWNERS" in tasks and "security-aggregation" in tasks,
    "tasks.md must cover .github/CODEOWNERS and security-aggregation",
)
check(
    "FR-021" in tasks and "doc-link" in tasks,
    "tasks.md must cover the FR-021 doc-link check",
)
check(
    "C-SEC-32" in tasks and "xdotool" in tasks and "C-REL-25" in tasks,
    "tasks.md must cover C-SEC-32, xdotool/stdin, and C-REL-25 research capture",
)

# ---------------------------------------------------------------------------
# 7. Spec consistency: constants path, helper verbs, flag semantics, FR-007
# ---------------------------------------------------------------------------
for path in (SPEC / "spec.md", SPEC / "tasks.md", SPEC / "data-model.md", SPEC / "plan.md"):
    text = read(path)
    check(
        "LivingRoots/ModConstants.cs" not in text,
        f"{path.relative_to(ROOT)} still names the non-existent LivingRoots/ModConstants.cs",
    )
check(
    "LivingRoots/Constants.cs" in spec,
    "spec.md FR-025 must name LivingRoots/Constants.cs as the constants owner",
)
a16 = section(spec, "**A16**", "\n- **A17**")
check(
    "build-manifest" in a16,
    "spec.md A16 must list the four helper verbs including build-manifest",
)
check(
    "build-manifest" in task_line("T005"),
    "tasks.md T005 must scaffold four helper verbs including build-manifest",
)
check(
    "build-manifest" in read(SPEC / "plan.md"),
    "plan.md must list build-manifest",
)
nfr003 = section(spec, "**NFR-003**", "\n- **NFR-004**")
check(
    "--quarantine-owner" in nfr003,
    "spec.md NFR-003 flag semantics must document --quarantine-owner",
)
check(
    "buildSkipped" in nfr003,
    "spec.md NFR-003 must use the canonical buildSkipped field",
)
fr007 = section(spec, "**FR-007**", "\n- **FR-008**")
for token in ("rngSeed", "`time`", "determinism", "scenarioKind", "hostGuard", "schema `required` arrays"):
    check(
        token in fr007,
        f"spec.md FR-007 must document the authoritative required-field set member {token}",
    )

# ---------------------------------------------------------------------------
# 8. Status vocabularies and canonical field names
# ---------------------------------------------------------------------------
status_paragraph = ""
for paragraph in spec.split("\n\n"):
    if "`scenarioStatus`" in paragraph and "`statisticalStatus`" in paragraph:
        status_paragraph = paragraph
        break
for token in ("active", "retired", "stable", "not-evaluated"):
    check(
        token in status_paragraph,
        f"spec.md status vocabulary paragraph must include {token}",
    )
check(
    "stable" in data_model,
    "data-model.md statisticalStatus vocabulary must include stable",
)
quickstart = read(SPEC / "quickstart.md")
check(
    "T115" not in quickstart,
    "quickstart.md still assigns the twelve VS cases to T115; use T136",
)
check(
    "T136" in quickstart,
    "quickstart.md must assign the twelve VS cases to T136",
)
for vs in ("VS-01", "VS-02", "VS-03", "VS-04", "VS-05", "VS-06", "VS-07", "VS-08", "VS-09", "VS-09b", "VS-09c", "VS-10"):
    check(vs in quickstart, f"quickstart.md is missing {vs}")
check(
    "skipBuild" not in quickstart,
    "quickstart.md must use buildSkipped, not skipBuild",
)

# ---------------------------------------------------------------------------
# 9. Traceability link status and canonical C-* citations
# ---------------------------------------------------------------------------
trace = read(SPEC / "traceability.md")
check(
    "Link status" in trace,
    "traceability.md must add a Link status column",
)
check(
    trace.count("| mapped |") + trace.count("| demonstrated |") >= 220,
    "traceability.md must mark every CHK row as mapped or demonstrated",
)
check(
    not re.search(r"C-[A-Z]+-0\d\d", trace),
    "traceability.md still contains zero-padded C-* citations (e.g. C-REL-029)",
)

# ---------------------------------------------------------------------------
# 10. Governance artifacts, doc links, checklists, scenarios
# ---------------------------------------------------------------------------
codeowners = ROOT / ".github" / "CODEOWNERS"
check(codeowners.is_file(), ".github/CODEOWNERS is missing")
if codeowners.is_file():
    codeowners_text = read(codeowners)
    for path_fragment in (
        "specs/010-headless-verification-runner/",
        "scripts/verify-scenario.sh",
        "scripts/verify-evidence-bundle.sh",
        "LivingRoots.Verification/",
        "LivingRoots/Domain/Interfaces/ILivingRootsModApi.cs",
    ):
        check(
            path_fragment in codeowners_text,
            f".github/CODEOWNERS must cover {path_fragment}",
        )

workflow = ROOT / ".github" / "workflows" / "security-aggregation.yml"
check(workflow.is_file(), ".github/workflows/security-aggregation.yml is missing")
if workflow.is_file():
    workflow_text = read(workflow)
    for token in ("schedule", "401", "403", "issues: write", "RUNNER_AUDIT_STORE"):
        check(
            token in workflow_text,
            f"security-aggregation.yml must contain {token}",
        )

for doc in (
    ROOT / "VERIFICATION_PROTOCOL.md",
    ROOT / "specs" / "009-end-to-end-game-verification" / "checklists" / "gate3-verification-contract.md",
):
    text = read(doc)
    check(
        "010-headless-verification-runner" in text,
        f"{doc.relative_to(ROOT)} must cross-link spec 010 FR-021",
    )

for checklist in sorted((SPEC / "checklists").glob("*.md")):
    text = read(checklist)
    check(
        "requirements-text review only" in text.lower(),
        f"{checklist.relative_to(ROOT)} must carry the requirements-text-only banner",
    )
    check(
        "T149" in text,
        f"{checklist.relative_to(ROOT)} banner must point at T149 for implementation attestation",
    )

scenario_004 = SPEC.parent / "004-playable-compost-loop" / "scenarios" / "compost-growth-hardening.scenario.json"
check(scenario_004.is_file(), "spec 004 scenario compost-growth-hardening.scenario.json is missing")
if scenario_004.is_file():
    scenario_data = load_json(scenario_004)
    check(
        isinstance(scenario_data, dict)
        and scenario_data.get("scenarioId") == "compost-growth-poor-soil"
        and scenario_data.get("featureSlug") == "004-playable-compost-loop",
        "004 scenario must keep scenarioId compost-growth-poor-soil and featureSlug 004-playable-compost-loop",
    )

# Every committed scenario's testMdPath must resolve to exactly one GATE3 block.
for path in glob.glob(str(ROOT / "specs" / "**" / "*.scenario.json"), recursive=True):
    data = load_json(Path(path))
    if not isinstance(data, dict):
        continue
    test_md_value = data.get("testMdPath")
    if not isinstance(test_md_value, str) or not test_md_value:
        # Invalid fixtures intentionally omit required fields; schema tests own that.
        continue
    test_md = ROOT / test_md_value
    check(
        test_md.is_file(),
        f"{Path(path).relative_to(ROOT)} testMdPath does not resolve: {data.get('testMdPath')}",
    )
    if test_md.is_file():
        text = read(test_md)
        check(
            text.count("<!-- GATE3:BEGIN -->") == 1 and text.count("<!-- GATE3:END -->") == 1,
            f"{test_md.relative_to(ROOT)} must contain exactly one GATE3 block",
        )

# Governance deferred table must include the two missing spec out-of-scope rows.
governance = read(SPEC / "GOVERNANCE-ANNEX.md")
for token in ("Interactive game-store authentication", "Public GitHub-hosted CI execution with game files"):
    check(
        token in governance,
        f"GOVERNANCE-ANNEX.md deferred-items table is missing: {token}",
    )

# ---------------------------------------------------------------------------
# Report
# ---------------------------------------------------------------------------
if failures:
    print(f"spec-consistency: FAIL ({len(failures)} of {checks} checks)")
    for index, failure in enumerate(failures, 1):
        print(f"{index:02d}. {failure}")
    sys.exit(1)

print(f"spec-consistency: PASS ({checks} checks)")
