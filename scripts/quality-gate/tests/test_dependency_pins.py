"""Dependency-pin policy tests that protect the test project build.

FsCheck 3 removed the C# API the property-based suite uses (`Arb`, `Prop`,
`Gen<T>.Two`, `WeightAndValue<>`). See
`.specify/bugs/fscheck-v3-build-failure/assessment.md` for the incident.
"""

from __future__ import annotations

import xml.etree.ElementTree as ET
from pathlib import Path
from typing import Any

import yaml

ROOT = Path(__file__).resolve().parents[3]
TEST_PROJECT = ROOT / "LivingRoots.Tests" / "LivingRoots.Tests.csproj"
DEPENDABOT = ROOT / ".github" / "dependabot.yml"
FSCHECK_PACKAGES = ("FsCheck", "FsCheck.Xunit")


def package_references() -> dict[str, str]:
    tree = ET.parse(TEST_PROJECT)
    return {
        element.get("Include"): element.get("Version")
        for element in tree.getroot().iter()
        if element.tag.endswith("PackageReference") and element.get("Include")
    }


def test_fscheck_major_version_is_pinned_to_2():
    references = package_references()
    for package in FSCHECK_PACKAGES:
        version = references.get(package)
        assert version is not None, f"{package} must stay referenced by the test project"
        assert version.split(".", maxsplit=1)[0] == "2", (
            f"{package} {version} breaks the FsCheck 2.x API used by the suite; "
            "migrate the tests before moving to FsCheck 3"
        )


def test_dependabot_ignores_fscheck_major_updates():
    config: dict[str, Any] = yaml.safe_load(DEPENDABOT.read_text(encoding="utf-8"))
    test_updates = [
        update
        for update in config.get("updates", [])
        if update.get("package-ecosystem") == "nuget"
        and update.get("directory") == "/LivingRoots.Tests"
    ]
    assert test_updates, "dependabot must track /LivingRoots.Tests"

    ignores = test_updates[0].get("ignore", [])
    ignored = {entry.get("dependency-name"): entry for entry in ignores}
    for package in FSCHECK_PACKAGES:
        assert package in ignored, f"dependabot must ignore {package} major updates"
        update_types = ignored[package].get("update-types", [])
        assert "version-update:semver-major" in update_types, (
            f"dependabot must ignore semver-major {package} updates"
        )
