#!/usr/bin/env python3
"""Deterministic dependency-outdated report for Living Roots (Fase 4 narrow lights-out).
Runs `dotnet list package --outdated` and reports outdated packages, classifying
bump type (patch/minor/major). Advisory only: exit 0 (report). Stdlib only.
"""
from __future__ import annotations
import re, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CSPROJ = ROOT / "LivingRoots" / "LivingRoots.csproj"

def parse_version(v: str) -> tuple[int, ...]:
    parts = v.strip().split(".")
    out = []
    for p in parts:
        m = re.match(r"(\d+)", p)
        out.append(int(m.group(1)) if m else 0)
    while len(out) < 3:
        out.append(0)
    return tuple(out[:3])

def bump_type(current: str, latest: str) -> str:
    c = parse_version(current); l = parse_version(latest)
    if l[0] != c[0]: return "major"
    if l[1] != c[1]: return "minor"
    if l[2] != c[2]: return "patch"
    return "none"

def main() -> int:
    if not CSPROJ.is_file():
        print("dependency-outdated: SKIP (csproj not found)")
        return 0
    try:
        proc = subprocess.run(
            ["dotnet", "list", str(CSPROJ), "package", "--outdated"],
            capture_output=True, text=True, timeout=300, cwd=str(ROOT),
        )
    except FileNotFoundError:
        print("dependency-outdated: SKIP (dotnet not available)")
        return 0
    except subprocess.TimeoutExpired:
        print("dependency-outdated: SKIP (timeout)")
        return 0

    text = proc.stdout + "\n" + proc.stderr
    # Parse lines like: "> Microsoft.CodeAnalysis.NetAnalyzers      10.0.403     10.0.403    11.0.100"
    rows = []
    for line in text.splitlines():
        m = re.match(r"\s*>\s+(\S+)\s+(\S+)\s+(\S+)\s+(\S+)\s*$", line)
        if m:
            name, requested, resolved, latest = m.groups()
            rows.append((name, requested, resolved, latest, bump_type(resolved, latest)))

    if not rows:
        print("dependency-outdated: PASS (no outdated packages)")
        return 0

    print("dependency-outdated: report")
    for name, req, res, lat, bt in rows:
        print(f"  {name}: {res} -> {lat} ({bt})")
    patches = [r for r in rows if r[4] == "patch"]
    minors = [r for r in rows if r[4] == "minor"]
    majors = [r for r in rows if r[4] == "major"]
    print(f"summary: patch={len(patches)} minor={len(minors)} major={len(majors)} total={len(rows)}")
    if majors:
        print("NOTE: major bumps require human review (not eligible for narrow lights-out).")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
