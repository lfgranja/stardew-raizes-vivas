"""Contract tests for the analyzer configuration and the suppression policy."""

from __future__ import annotations

import os
import re
import subprocess
import xml.etree.ElementTree as ET
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[3]
BUILD_PROPS = ROOT / "Directory.Build.props"
MOD_PROJECT = ROOT / "LivingRoots" / "LivingRoots.csproj"
TEST_PROJECT = ROOT / "LivingRoots.Tests" / "LivingRoots.Tests.csproj"
PROBE = ROOT / "scripts" / "quality-gate" / "analyzer-probe.sh"

SONAR_VERSION = "10.35.0.4138"
NETANALYZERS_VERSION = "10.0.403"


def parse_project(path: Path) -> ET.Element:
    return ET.parse(path).getroot()


def properties(root: ET.Element) -> dict[str, str]:
    found: dict[str, str] = {}
    for group in root.iter():
        if not group.tag.endswith("PropertyGroup"):
            continue
        for element in group:
            if element.text is not None:
                found[element.tag.split("}")[-1].lower()] = element.text.strip()
    return found


def element_value(element: ET.Element, name: str) -> str | None:
    """Read an MSBuild attribute or child element (both are legal)."""
    value = element.get(name)
    if value is not None:
        return value
    for child in element:
        if child.tag.split("}")[-1].lower() == name.lower():
            return (child.text or "").strip()
    return None


def package_references(root: ET.Element) -> dict[str, ET.Element]:
    found: dict[str, ET.Element] = {}
    for element in root.iter():
        if element.tag.endswith("PackageReference"):
            include = element.get("Include")
            if include:
                found[include] = element
    return found


@pytest.mark.parametrize("path", [BUILD_PROPS, MOD_PROJECT, TEST_PROJECT])
def test_project_files_exist(path):
    assert path.exists(), f"{path} must exist"


@pytest.mark.parametrize("path", [BUILD_PROPS, MOD_PROJECT, TEST_PROJECT])
def test_no_global_nowarn_is_used(path):
    props = properties(parse_project(path))
    assert "nowarn" not in props, f"{path} must not suppress warnings globally"


def test_build_props_enables_the_required_analyzer_settings():
    props = properties(parse_project(BUILD_PROPS))
    assert props.get("analysislevel") == "latest"
    assert props.get("analysismode") == "Recommended"
    assert props.get("treatwarningsaserrors") == "true"
    assert props.get("enforcecodestyleinbuild") == "true"
    assert props.get("enablenetanalyzers") == "true"


@pytest.mark.parametrize("path", [MOD_PROJECT, TEST_PROJECT])
def test_both_projects_reference_the_pinned_analyzers(path):
    references = package_references(parse_project(path))
    assert "SonarAnalyzer.CSharp" in references, f"{path} must reference SonarAnalyzer.CSharp"
    assert "Microsoft.CodeAnalysis.NetAnalyzers" in references, (
        f"{path} must reference Microsoft.CodeAnalysis.NetAnalyzers"
    )
    assert element_value(references["SonarAnalyzer.CSharp"], "Version") == SONAR_VERSION
    assert (
        element_value(references["Microsoft.CodeAnalysis.NetAnalyzers"], "Version")
        == NETANALYZERS_VERSION
    )
    for name in ("SonarAnalyzer.CSharp", "Microsoft.CodeAnalysis.NetAnalyzers"):
        private_assets = element_value(references[name], "PrivateAssets") or ""
        assert "all" in private_assets.split(";"), f"{name} must not leak to consumers"


def test_the_probe_builds_both_projects(tmp_path):
    assert PROBE.exists(), "the analyzer probe is required"
    bin_dir = tmp_path / "bin"
    bin_dir.mkdir()
    log = tmp_path / "dotnet.log"
    dotnet = bin_dir / "dotnet"
    dotnet.write_text(
        "#!/usr/bin/env bash\n"
        "printf '%s\\n' \"$*\" >> \"${DOTNET_LOG}\"\n"
        "echo '__AnalyzerGateProbe.cs(8,17): error S1481: probe' >&2\n"
        "exit 1\n",
        encoding="utf-8",
    )
    dotnet.chmod(0o755)

    env = dict(os.environ)
    env["PATH"] = f"{bin_dir}{os.pathsep}{env['PATH']}"
    env["DOTNET_LOG"] = str(log)
    result = subprocess.run(
        ["bash", str(PROBE)],
        cwd=ROOT,
        env=env,
        text=True,
        capture_output=True,
        check=False,
    )
    assert result.returncode == 0, result.stdout + result.stderr
    assert "PASS" in result.stdout
    invocations = log.read_text(encoding="utf-8").splitlines()
    assert len(invocations) == 2, "the probe must build both projects exactly once"
    assert any("LivingRoots/LivingRoots.csproj" in line for line in invocations)
    assert any("LivingRoots.Tests/LivingRoots.Tests.csproj" in line for line in invocations)
    for line in invocations:
        assert "build" in line and "--no-restore" in line and "-c Release" in line
    assert not (ROOT / "LivingRoots" / "__AnalyzerGateProbe.cs").exists()
    assert not (ROOT / "LivingRoots.Tests" / "__AnalyzerGateProbe.cs").exists()


SUPPRESS_MESSAGE = re.compile(
    r"\[\s*(?:[A-Za-z_][\w.]*\.)?SuppressMessage\s*\((?P<body>.*?)\)\s*\]",
    re.DOTALL,
)
JUSTIFICATION = re.compile(r'Justification\s*=\s*"(?P<text>[^"]*)"', re.DOTALL)
PRAGMA_DISABLE = re.compile(
    r"^(\s*)#pragma\s+warning\s+disable\b\s*(?P<rules>[A-Za-z0-9_,\s]*?)"
    r"(?:\s*(?P<comment>//.*|/\*.*?\*/))?\s*$"
)
SEVERITY_NONE = re.compile(
    r"^\s*dotnet_diagnostic\.(?P<rule>[A-Za-z0-9_]+)\.severity\s*=\s*none\s*$"
)


def comment_justifies(comment: str, rules: list[str]) -> bool:
    """A justification names every disabled rule and adds a non-empty reason."""
    body = comment.strip()
    if not body:
        return False
    body = re.sub(r"^\s*(?://|#|/\*)", "", body).strip()
    body = re.sub(r"\*/\s*$", "", body).strip()
    if not all(rule in body for rule in rules):
        return False
    for rule in rules:
        body = body.replace(rule, "")
    body = body.strip(" :,-")
    return len(body) >= 3


def scan_suppressions(path: Path, text: str) -> list[str]:
    """Return human-readable policy violations for one source file."""
    violations: list[str] = []
    lines = text.splitlines()

    for match in SUPPRESS_MESSAGE.finditer(text):
        justification = JUSTIFICATION.search(match.group("body"))
        if justification is None or not justification.group("text").strip():
            line = text[: match.start()].count("\n") + 1
            violations.append(f"{path}:{line}: [SuppressMessage] without a non-empty Justification")

    for index, line in enumerate(lines):
        match = PRAGMA_DISABLE.match(line)
        if not match:
            continue
        rules = match.group("rules").strip()
        rule_ids = [token.strip() for token in rules.split(",") if token.strip()]
        if not rule_ids or not all(re.fullmatch(r"[A-Za-z][A-Za-z0-9_]*", rule) for rule in rule_ids):
            violations.append(f"{path}:{index + 1}: #pragma warning disable without a specific rule id")
            continue
        # A justification may be the same-line trailing comment or the line above,
        # and it must name every disabled rule plus a reason.
        comment = match.group("comment") or ""
        previous = lines[index - 1].strip() if index > 0 else ""
        if not comment_justifies(comment, rule_ids) and not comment_justifies(previous, rule_ids):
            violations.append(
                f"{path}:{index + 1}: #pragma warning disable {rules} has no justified reason naming the rule"
            )

    return violations


def repository_sources() -> list[Path]:
    sources: list[Path] = []
    for directory in (ROOT / "LivingRoots", ROOT / "LivingRoots.Tests"):
        sources.extend(sorted(directory.rglob("*.cs")))
    assert sources, "the scan must not be vacuous"
    return sources


def scan_editorconfig(path: Path, text: str, root: bool = False) -> list[str]:
    """Reject repository-wide or unjustified rule disabling in .editorconfig."""
    violations: list[str] = []
    lines = text.splitlines()
    for index, line in enumerate(lines):
        match = SEVERITY_NONE.match(line)
        if not match:
            continue
        rule = match.group("rule")
        if root:
            violations.append(
                f"{path}:{index + 1}: the root .editorconfig must not disable {rule}; "
                "scope the override to a folder or file pattern"
            )
            continue
        previous = lines[index - 1].strip() if index > 0 else ""
        trailing = line.split("#", 1)[1] if "#" in line else ""
        justified = comment_justifies(previous, [rule]) or comment_justifies(trailing, [rule])
        if not justified:
            violations.append(
                f"{path}:{index + 1}: {rule} is disabled without a comment naming the rule and reason"
            )
    return violations


FORBIDDEN_SUPPRESSION_ARTIFACTS = (".semgrepignore", ".trivyignore")


def scan_forbidden_suppressions(path: Path, text: str) -> list[str]:
    """Reject blanket scanner suppressions: findings must be fixed, not hidden."""
    violations: list[str] = []
    if path.name in FORBIDDEN_SUPPRESSION_ARTIFACTS:
        violations.append(
            f"{path}: {path.name} is forbidden; fix the finding instead of ignoring it"
        )
    for index, line in enumerate(text.splitlines(), 1):
        if "nosemgrep" in line:
            violations.append(
                f"{path}:{index}: nosemgrep is forbidden; fix the finding instead of ignoring it"
            )
    return violations


def test_every_repository_suppression_is_documented():
    violations: list[str] = []
    for path in repository_sources():
        violations.extend(scan_suppressions(path, path.read_text(encoding="utf-8")))
    assert violations == [], "\n".join(violations)


def test_every_repository_editorconfig_override_is_documented():
    configs = [
        path
        for path in ROOT.rglob(".editorconfig")
        if ".git" not in path.parts and ".venv-quality-gate" not in path.parts
    ]
    assert configs, "the scan must not be vacuous"
    violations: list[str] = []
    for path in configs:
        violations.extend(
            scan_editorconfig(
                path,
                path.read_text(encoding="utf-8"),
                root=path.resolve() == (ROOT / ".editorconfig").resolve(),
            )
        )
    assert violations == [], "\n".join(violations)


def test_a_justified_pragma_passes_the_policy(tmp_path):
    source = tmp_path / "Justified.cs"
    source.write_text(
        "// S1481: the local exists only to pin the overload under test.\n"
        "#pragma warning disable S1481\n"
        "int unused = 1;\n"
        "#pragma warning restore S1481\n",
        encoding="utf-8",
    )
    assert scan_suppressions(source, source.read_text(encoding="utf-8")) == []


def test_an_unjustified_pragma_fails_the_policy(tmp_path):
    source = tmp_path / "Unjustified.cs"
    source.write_text(
        "#pragma warning disable S1481\nint unused = 1;\n",
        encoding="utf-8",
    )
    violations = scan_suppressions(source, source.read_text(encoding="utf-8"))
    assert len(violations) == 1
    assert "no justified reason" in violations[0]


def test_a_pragma_with_a_trailing_justification_passes_the_policy(tmp_path):
    source = tmp_path / "Justified.cs"
    source.write_text(
        "#pragma warning disable S1481 // S1481: the local exists only to pin the overload under test.\n"
        "int unused = 1;\n",
        encoding="utf-8",
    )
    assert scan_suppressions(source, source.read_text(encoding="utf-8")) == []


def test_a_pragma_with_an_empty_trailing_comment_fails_the_policy(tmp_path):
    source = tmp_path / "Unjustified.cs"
    source.write_text(
        "#pragma warning disable S1481 //\nint unused = 1;\n",
        encoding="utf-8",
    )
    violations = scan_suppressions(source, source.read_text(encoding="utf-8"))
    assert len(violations) == 1
    assert "no justified reason" in violations[0]


def test_a_pragma_with_an_unrelated_above_comment_fails_the_policy(tmp_path):
    source = tmp_path / "Unjustified.cs"
    source.write_text(
        "// This comment is about something else entirely.\n"
        "#pragma warning disable S1481\n"
        "int unused = 1;\n",
        encoding="utf-8",
    )
    violations = scan_suppressions(source, source.read_text(encoding="utf-8"))
    assert len(violations) == 1
    assert "no justified reason" in violations[0]


def test_a_bare_pragma_fails_the_policy(tmp_path):
    source = tmp_path / "Bare.cs"
    source.write_text(
        "#pragma warning disable\nint unused = 1;\n",
        encoding="utf-8",
    )
    violations = scan_suppressions(source, source.read_text(encoding="utf-8"))
    assert len(violations) == 1
    assert "without a specific rule id" in violations[0]


def test_a_pragma_with_an_inline_block_justification_passes_the_policy(tmp_path):
    source = tmp_path / "Justified.cs"
    source.write_text(
        "#pragma warning disable S1481 /* S1481: pins the overload under test */\n"
        "int unused = 1;\n",
        encoding="utf-8",
    )
    assert scan_suppressions(source, source.read_text(encoding="utf-8")) == []


def test_a_suppress_message_without_justification_fails_the_policy(tmp_path):
    source = tmp_path / "Suppressed.cs"
    source.write_text(
        '[System.Diagnostics.CodeAnalysis.SuppressMessage("csharpsquid", "S3966")]\n'
        "public void Dispose() { }\n",
        encoding="utf-8",
    )
    violations = scan_suppressions(source, source.read_text(encoding="utf-8"))
    assert len(violations) == 1
    assert "Justification" in violations[0]


def test_a_suppress_message_with_a_reason_passes_the_policy(tmp_path):
    source = tmp_path / "Suppressed.cs"
    source.write_text(
        '[System.Diagnostics.CodeAnalysis.SuppressMessage(\n'
        '    "csharpsquid",\n'
        '    "S3966",\n'
        '    Justification = "Testing idempotency requires repeated disposal.")]\n'
        "public void Dispose() { }\n",
        encoding="utf-8",
    )
    assert scan_suppressions(source, source.read_text(encoding="utf-8")) == []


def test_an_unqualified_suppress_message_is_recognised(tmp_path):
    source = tmp_path / "Suppressed.cs"
    source.write_text(
        '[SuppressMessage("csharpsquid", "S3966")]\n'
        "public void Dispose() { }\n",
        encoding="utf-8",
    )
    violations = scan_suppressions(source, source.read_text(encoding="utf-8"))
    assert len(violations) == 1
    assert "Justification" in violations[0]


def test_an_editorconfig_override_with_a_reason_passes(tmp_path):
    config = tmp_path / ".editorconfig"
    config.write_text(
        "[*.g.cs]\n"
        "# S1481: generated code is not editable.\n"
        "dotnet_diagnostic.S1481.severity = none\n",
        encoding="utf-8",
    )
    assert scan_editorconfig(config, config.read_text(encoding="utf-8")) == []


def test_an_editorconfig_override_without_a_reason_fails(tmp_path):
    config = tmp_path / ".editorconfig"
    config.write_text(
        "[*.cs]\ndotnet_diagnostic.S1481.severity = none\n",
        encoding="utf-8",
    )
    violations = scan_editorconfig(config, config.read_text(encoding="utf-8"))
    assert len(violations) == 1
    assert "S1481" in violations[0]


def test_an_editorconfig_comment_without_a_reason_fails(tmp_path):
    config = tmp_path / ".editorconfig"
    config.write_text(
        "[*.g.cs]\n"
        "# S1481\n"
        "dotnet_diagnostic.S1481.severity = none\n",
        encoding="utf-8",
    )
    violations = scan_editorconfig(config, config.read_text(encoding="utf-8"))
    assert len(violations) == 1
    assert "without a comment naming the rule and reason" in violations[0]


def test_a_root_blanket_editorconfig_disable_fails_even_with_a_comment(tmp_path):
    config = tmp_path / ".editorconfig"
    config.write_text(
        "[*.cs]\n"
        "# S1481: generated code is not editable.\n"
        "dotnet_diagnostic.S1481.severity = none\n",
        encoding="utf-8",
    )
    violations = scan_editorconfig(config, config.read_text(encoding="utf-8"), root=True)
    assert len(violations) == 1
    assert "root .editorconfig must not disable S1481" in violations[0]


def test_a_scoped_editorconfig_disable_in_a_subdirectory_still_passes(tmp_path):
    config = tmp_path / ".editorconfig"
    text = (
        "[*.g.cs]\n"
        "# S1481: generated code is not editable.\n"
        "dotnet_diagnostic.S1481.severity = none\n"
    )
    assert scan_editorconfig(config, text, root=False) == []


@pytest.mark.parametrize("comment", ["//", "#", "/*"])
def test_nosemgrep_directives_are_forbidden(tmp_path, comment):
    source = tmp_path / "Example.cs"
    text = f"{comment} nosemgrep: this finding is inconvenient\nint unused = 1;\n"
    violations = scan_forbidden_suppressions(source, text)
    assert len(violations) == 1
    assert "nosemgrep is forbidden" in violations[0]


def test_semgrepignore_is_forbidden(tmp_path):
    artifact = tmp_path / ".semgrepignore"
    violations = scan_forbidden_suppressions(artifact, "LivingRoots/\n")
    assert len(violations) == 1
    assert ".semgrepignore is forbidden" in violations[0]


def test_trivyignore_is_forbidden(tmp_path):
    artifact = tmp_path / ".trivyignore"
    violations = scan_forbidden_suppressions(artifact, "CVE-2026-0001\n")
    assert len(violations) == 1
    assert ".trivyignore is forbidden" in violations[0]


def test_the_repository_has_no_blanket_scanner_suppressions():
    violations: list[str] = []
    for name in FORBIDDEN_SUPPRESSION_ARTIFACTS:
        artifact = ROOT / name
        if artifact.exists():
            violations.append(f"{artifact}: {name} is forbidden; fix the finding instead of ignoring it")
    for path in repository_sources():
        violations.extend(scan_forbidden_suppressions(path, path.read_text(encoding="utf-8")))
    assert violations == [], "\n".join(violations)
