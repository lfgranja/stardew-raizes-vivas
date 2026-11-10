#!/usr/bin/env python3
"""Deterministic SDD chain gate for stardew-raizes-vivas (R4/R7).
Checks: spec.md present per numbered spec; validate.md present + APPROVED;
release.md present; converge/clarify hygiene; satellite manifest; JSON/YAML parse.
Exit 0 pass, 1 fail. Stdlib only; no network.
"""
from __future__ import annotations
import json, re, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
errors: list[str] = []
warnings: list[str] = []

def req(path: str) -> Path:
    p = ROOT / path
    if not p.exists():
        errors.append(f"missing required path: {path}")
    return p

def warn(msg: str) -> None:
    warnings.append(msg)

# Core SDD paths
req("AGENTS.md")
req(".specify/memory/constitution.md")
req("specs")
req("docs/governance/satellite-manifest.json")

# JSON parse (targeted: governance + .specify + root manifests)
for p in list((ROOT / "docs" / "governance").rglob("*.json")) + list((ROOT / ".specify").rglob("*.json")):
    if any(part in {".git", "node_modules", "target"} for part in p.parts):
        continue
    try:
        json.loads(p.read_text(encoding="utf-8"))
    except Exception as e:
        errors.append(f"invalid JSON {p.relative_to(ROOT)}: {e}")

# YAML parse (best-effort)
try:
    import yaml
except Exception:  # pragma: no cover
    yaml = None
for p in list(ROOT.rglob("*.yml")) + list(ROOT.rglob("*.yaml")):
    if any(part in {".git", "node_modules", "target", ".direnv"} for part in p.parts):
        continue
    txt = p.read_text(encoding="utf-8", errors="replace")
    if yaml:
        try:
            list(yaml.safe_load_all(txt))
        except Exception as e:
            errors.append(f"invalid YAML {p.relative_to(ROOT)}: {e}")
    else:
        if "\x00" in txt:
            errors.append(f"invalid YAML (NUL) {p.relative_to(ROOT)}")

# Satellite manifest sanity
sm = ROOT / "docs" / "governance" / "satellite-manifest.json"
if sm.exists():
    try:
        data = json.loads(sm.read_text(encoding="utf-8"))
        if data.get("status") not in {"alinhado", "parcial", "desalinhado", "desatualizado"}:
            errors.append("satellite-manifest.json: invalid status")
        if not data.get("hub_sha"):
            warnings.append("satellite-manifest.json: hub_sha missing")
        if not data.get("adopted_policies"):
            warnings.append("satellite-manifest.json: adopted_policies empty")
    except Exception as e:
        errors.append(f"satellite-manifest.json parse error: {e}")

# Per-spec chain checks
specs = ROOT / "specs"
if specs.exists():
    for d in sorted(p for p in specs.iterdir() if p.is_dir() and p.name[:3].isdigit()):
        slug = d.name
        if not (d / "spec.md").exists():
            warn(f"{slug}: no spec.md (auxiliary dir) — skipped from chain checks")
            continue

        # tasks hygiene
        tasks = d / "tasks.md"
        unchecked = 0
        if tasks.exists():
            unchecked = len(re.findall(r"^- \[ \]", tasks.read_text(encoding="utf-8"), flags=re.M))
            if unchecked > 0:
                warn(f"{slug}: {unchecked} unchecked task(s) in tasks.md")

        # validate.md (R4)
        vpath = ROOT / ".specify" / "validations" / slug / "validate.md"
        if not vpath.exists():
            errors.append(f"missing validate.md for {slug} (R4)")
        else:
            vtext = vpath.read_text(encoding="utf-8").lower()
            if not any(m in vtext for m in ("approved", "aprovado", "pass")):
                errors.append(f"validate.md not APPROVED for {slug} (R4)")

        # release.md (R7) — required once validated
        rpath = ROOT / ".specify" / "releases" / slug / "release.md"
        if not rpath.exists():
            errors.append(f"missing release.md for {slug} (R7)")

        # converge append-only hygiene (warn only)
        converges = list(d.glob("converge*.md"))
        for c in converges:
            if c.stat().st_size == 0:
                warn(f"{slug}: empty converge file {c.name}")

        # clarify persistence (warn only — repo has none yet)
        if not list(d.glob("clarify*.md")) and not (ROOT / ".specify" / "clarifications" / f"{slug}.md").exists():
            warn(f"{slug}: no clarify artifact persisted (R3)")

# Telemetry JSONL (Fase 2) — if present, each line must parse as JSON
telemetry_file = ROOT / "docs" / "telemetry" / "execution-record.jsonl"
if telemetry_file.exists():
    try:
        for i, raw in enumerate(telemetry_file.read_text(encoding="utf-8").splitlines(), start=1):
            if not raw.strip():
                continue
            json.loads(raw)
    except json.JSONDecodeError as e:
        errors.append(f"invalid JSONL {telemetry_file.relative_to(ROOT)} line {i}: {e.msg}")
    except OSError as e:
        errors.append(f"cannot read {telemetry_file.relative_to(ROOT)}: {e}")
else:
    warn("docs/telemetry/execution-record.jsonl not present (Fase 2 optional)")

# Summary
for w in warnings:
    print(f"WARN: {w}")
for e in errors:
    print(f"ERROR: {e}")
status = "PASS" if not errors else "FAIL"
print(f"sdd-gate: {status} ({len(errors)} errors, {len(warnings)} warnings)")
sys.exit(0 if not errors else 1)
