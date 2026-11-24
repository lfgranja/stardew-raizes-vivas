#!/usr/bin/env python3
"""Deterministic policy drift check (guardrail 4 / G10 / spec 026).
Compares hub policy manifest (canonical) against satellite-reported manifests.
No network. Exit 0=aligned/no-satellites, 1=drift detected, 2=usage/input error.
"""
from __future__ import annotations
import json, sys
from pathlib import Path

def load(p: Path):
    try: return json.loads(p.read_text(encoding='utf-8'))
    except Exception as e: print(f"DRIFT_ERROR read {p}: {e}"); return None

def check(hub: dict, sat: dict) -> list[str]:
    issues = []
    for k in hub.get('required_policies', []):
        if k not in sat.get('policies_affected', []) and k not in sat.get('adopted_policies', []):
            issues.append(f"missing_policy:{k}")
    if sat.get('hub_sha') and hub.get('hub_sha') and sat['hub_sha'] != hub['hub_sha']:
        issues.append(f"hub_sha_mismatch:{sat['hub_sha']}!={hub['hub_sha']}")
    for f in hub.get('required_files', []):
        if f not in sat.get('files_present', []):
            issues.append(f"missing_file:{f}")
    return issues

def main(argv):
    if len(argv) < 2:
        print("usage: check_policy_drift.py <hub_manifest.json> [satellite_manifest.json ...]")
        return 2
    hub = load(Path(argv[1]))
    if hub is None: return 2
    if len(argv) == 2:
        print("policy-drift: PASS (no satellite manifests provided; NV)")
        return 0
    drift = 0
    for sp in argv[2:]:
        sat = load(Path(sp))
        if sat is None: drift = 1; continue
        issues = check(hub, sat)
        target = sat.get('target', sp)
        if issues:
            drift = 1
            print(f"policy-drift: DRIFT target={target} issues={issues}")
        else:
            print(f"policy-drift: ALIGNED target={target}")
    if drift == 0:
        print("policy-drift: PASS")
    return 1 if drift else 0

if __name__ == '__main__':
    raise SystemExit(main(sys.argv))
