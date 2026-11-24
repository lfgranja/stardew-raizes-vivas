#!/usr/bin/env python3
"""Deterministic i18n parity check for Living Roots (Fase 4 narrow lights-out).
Verifies default/en/pt locale JSON files have identical key sets and no empty values.
Exit 0 pass, 1 fail. Stdlib only; no network.
"""
from __future__ import annotations
import json, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
I18N = ROOT / "LivingRoots" / "i18n"
LOCALES = ["default", "en", "pt"]
errors: list[str] = []
warnings: list[str] = []

def load(name: str) -> dict | None:
    p = I18N / f"{name}.json"
    if not p.is_file():
        errors.append(f"missing locale file: {p.relative_to(ROOT)}")
        return None
    try:
        data = json.loads(p.read_text(encoding="utf-8"))
    except Exception as e:
        errors.append(f"invalid JSON {p.relative_to(ROOT)}: {e}")
        return None
    if not isinstance(data, dict):
        errors.append(f"{p.relative_to(ROOT)}: top-level must be object")
        return None
    return data

data = {name: load(name) for name in LOCALES}
present = {n: d for n, d in data.items() if d is not None}

if len(present) == len(LOCALES):
    keys = {n: set(d.keys()) for n, d in present.items()}
    base = keys["default"]
    for name in ("en", "pt"):
        missing = base - keys[name]
        extra = keys[name] - base
        if missing:
            errors.append(f"{name}.json missing keys vs default: {sorted(missing)}")
        if extra:
            errors.append(f"{name}.json extra keys vs default: {sorted(extra)}")
    # empty values
    for name, d in present.items():
        for k, v in d.items():
            if not isinstance(v, str) or not v.strip():
                errors.append(f"{name}.json: empty/non-string value for {k!r}")
    # duplicate check across en/default (optional info)
    if keys["default"] == keys["en"] == keys["pt"]:
        print("i18n-parity: key sets identical across default/en/pt")

for w in warnings:
    print(f"WARN: {w}")
for e in errors:
    print(f"ERROR: {e}")
status = "PASS" if not errors else "FAIL"
print(f"i18n-parity: {status} ({len(errors)} errors, {len(warnings)} warnings)")
sys.exit(0 if not errors else 1)
