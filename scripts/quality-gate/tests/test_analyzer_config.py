"""Contract tests for the analyzer configuration and the suppression policy."""

from __future__ import annotations

import re
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
                found[element.tag.split("}")[-1]] = element.text.strip()
    return found


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
    assert "NoWarn" not in props, f"{path} must not suppress warnings globally"


def test_build_props_enables_the_required_analyzer_settings():
    props = properties(parse_project(BUILD_PROPS))
    assert props.get("AnalysisLevel") == "latest"
    assert props.get("AnalysisMode") == "Recommended"
    assert props.get("TreatWarningsAsErrors") == "true"
    assert props.get("EnforceCodeStyleInBuild") == "true"
    assert props.get("EnableNETAnalyzers") == "true"


@pytest.mark.parametrize("path", [MOD_PROJECT, TEST_PROJECT])
def test_both_projects_reference_the_pinned_analyzers(path):
    references = package_references(parse_project(path))
    assert "SonarAnalyzer.CSharp" in references, f"{path} must reference SonarAnalyzer.CSharp"
    assert "Microsoft.CodeAnalysis.NetAnalyzers" in references, (
        f"{path} must reference Microsoft.CodeAnalysis.NetAnalyzers"
    )
    assert references["SonarAnalyzer.CSharp"].get("Version") == SONAR_VERSION
    assert references["Microsoft.CodeAnalysis.NetAnalyzers"].get("Version") == NETANALYZERS_VERSION
    for name in ("SonarAnalyzer.CSharp", "Microsoft.CodeAnalysis.NetAnalyzers"):
        private_assets = references[name].get("PrivateAssets", "")
        assert "all" in private_assets.split(";"), f"{name} must not leak to consumers"


def test_the_probe_builds_both_projects():
    assert PROBE.exists(), "the analyzer probe is required"
    text = PROBE.read_text(encoding="utf-8")
    assert "LivingRoots/LivingRoots.csproj" in text
    assert "LivingRoots.Tests/LivingRoots.Tests.csproj" in text


SUPPRESS_MESSAGE = re.compile(
    r"\[SuppressMessage\((?P<body>.*?)\)\]",
    re.DOTALL,
)
JUSTIFICATION = re.compile(r'Justification\s*=\s*"(?P<text>[^"]*)"', re.DOTALL)
PRAGMA_DISABLE = re.compile(r"^(\s*)#pragma\s+warning\s+disable\s+(?P<rules>[A-Za-z0-9_,\s]+)$")


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
        # A justification may be the same-line trailing comment or the line above.
        same_line = line.split("#pragma", 1)[1]
        same_line_comment = "#" in same_line
        previous = lines[index - 1].strip() if index > 0 else ""
        above_comment = (previous.startswith("//") or previous.startswith("/*")) and len(previous) > 3
        if not same_line_comment and not above_comment:
            violations.append(
                f"{path}:{index + 1}: #pragma warning disable {rules} has no justification comment"
            )

    return violations


def repository_sources() -> list[Path]:
    sources: list[Path] = []
    for directory in (ROOT / "LivingRoots", ROOT / "LivingRoots.Tests"):
        sources.extend(sorted(directory.rglob("*.cs")))
    assert sources, "the scan must not be vacuous"
    return sources


def test_every_repository_suppression_is_documented():
    violations: list[str] = []
    for path in repository_sources():
        violations.extend(scan_suppressions(path, path.read_text(encoding="utf-8")))
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
    assert "no justification" in violations[0]


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
