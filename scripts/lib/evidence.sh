#!/usr/bin/env bash
if [ -n "${_LR_EVIDENCE_LOADED:-}" ]; then return 0 2>/dev/null || exit 0; fi
_LR_EVIDENCE_LOADED=1
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/retention.sh"

evidence_run_id() { runner_run_id; }

evidence_bundle_dir() {
    [[ $# -eq 2 ]] || return "$EXIT_USAGE_ERROR"
    printf '%s/evidence-bundle-%s\n' "$1" "$2"
    return 0
}

evidence_init() {
    [[ $# -eq 2 ]] || return "$EXIT_USAGE_ERROR"
    local root="$1" runId="$2"
    local bdir
    bdir="$(evidence_bundle_dir "$root" "$runId")"
    mkdir -p "$bdir/artifacts" "$bdir/attempts"
    return 0
}

evidence_add_artifact() {
    [[ $# -eq 2 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1" path="$2"
    [[ -f "$path" ]] || return 19
    local name
    name="$(basename "$path")"
    cp -f "$path" "$bundle/artifacts/$name"
    return 0
}

evidence_attempt_dir() {
    [[ $# -eq 2 ]] || return "$EXIT_USAGE_ERROR"
    printf '%s/attempts/%s\n' "$1" "$2"
    return 0
}

evidence_record_attempt() {
    [[ $# -ge 2 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1" attemptNum="$2"
    local verdict="${3:-errored}"
    local failureClass="${4:-}"
    local signals="${5:-}"
    local exitCode="${6:-19}"
    local attempt_dir
    attempt_dir="$(evidence_attempt_dir "$bundle" "$attemptNum")"
    mkdir -p "$attempt_dir"
    python3 - "$attempt_dir/attempt.json" "$attemptNum" "$verdict" "$failureClass" "$signals" "$exitCode" <<'PY'
import json,sys
path,attempt,verdict,fc,signals,ec=sys.argv[1:7]
d={"attempt":int(attempt),"verdict":verdict,"failureClass":fc,"signals":signals,"exitCode":int(ec)}
json.dump(d,open(path,"w"),indent=2)
PY
    # append to attempts[] in a tracking file for manifest input (append-only)
    local attempts_list="$bundle/.attempts_list"
    local entry="attempts/${attemptNum}/attempt.json"
    if [[ -f "$attempts_list" ]]; then
        if grep -Fxq "$entry" "$attempts_list"; then
            return 0
        fi
    fi
    printf '%s\n' "$entry" >> "$attempts_list"
    return 0
}

# evidence_write_manifest_input <bundle> <context.json> <output.json>
#
# Assemble the FULL manifest payload the schema requires (FR-014) from the
# scenario contract, the run context and the files the bundle actually holds.
# ManifestBuilder remains the only writer of manifest.json / manifest.sha256;
# this function produces its input, never the manifest itself.
#
# Before this existed the orchestrator handed ManifestBuilder a bare
# {"runId": ...}, so every produced manifest failed
# contracts/evidence-bundle.schema.json with 13 missing required fields and the
# offline verifier (which never validated the schema) still accepted it.
evidence_write_manifest_input() {
    [[ $# -eq 3 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1" context="$2" output="$3"
    [[ -d "$bundle" ]] || return 19
    [[ -f "$context" ]] || return 14
    python3 - "$bundle" "$context" "$output" <<'PY'
import hashlib
import json
import os
import shlex
import sys

bundle, context_path, output = sys.argv[1:4]

with open(context_path, encoding="utf-8") as f:
    ctx = json.load(f)

scenario = {}
scenario_path = ctx.get("scenario") or ""
if scenario_path and os.path.isfile(scenario_path):
    try:
        with open(scenario_path, encoding="utf-8") as f:
            scenario = json.load(f)
    except Exception:
        scenario = {}

def scenario_env(key, default=""):
    env = scenario.get("environment")
    if isinstance(env, dict):
        value = env.get(key)
        if isinstance(value, str) and value:
            return value
    value = ctx.get(key)
    if isinstance(value, str) and value:
        return value
    return default

def sha256(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b""):
            h.update(chunk)
    return h.hexdigest()

# Bundle-relative regular files, excluding the manifest, its detached anchor and
# the builder's own work files. Sorted so two runs over the same bundle agree.
SKIP = {"manifest.json", "manifest.sha256", ".manifest-input.json", ".attempts_list"}
subject = []
artifacts = []
for root, dirs, files in os.walk(bundle):
    dirs.sort()
    for name in sorted(files):
        if name.startswith("."):
            continue
        full = os.path.join(root, name)
        rel = os.path.relpath(full, bundle).replace(os.sep, "/")
        if rel in SKIP or not os.path.isfile(full):
            continue
        subject.append({"name": rel, "digest": {"sha256": sha256(full)}})
        if rel.startswith("artifacts/"):
            artifacts.append(rel)

attempts = []
list_path = os.path.join(bundle, ".attempts_list")
if os.path.isfile(list_path):
    with open(list_path, encoding="utf-8") as f:
        for line in f:
            entry = line.strip()
            if not entry:
                continue
            if entry.startswith("attempts/") and entry.endswith("/attempt.json") and os.path.isfile(os.path.join(bundle, entry)):
                if entry not in attempts:
                    attempts.append(entry)

expected_signals = scenario.get("expectedSignals")
if not isinstance(expected_signals, list) or len(expected_signals) != 3:
    expected_signals = ["soilHealthStateDelta", "cropPhaseDelta", "smapiLog"]

env_cfg = {}
scenario_config = (scenario.get("environment") or {}).get("config") if isinstance(scenario.get("environment"), dict) else None
if isinstance(scenario_config, dict):
    if scenario_config.get("saveMode") in ("save", "nosave"):
        env_cfg["saveMode"] = scenario_config["saveMode"]
    if isinstance(scenario_config.get("headless"), bool):
        env_cfg["headless"] = scenario_config["headless"]
if isinstance(ctx.get("buildSkipped"), bool):
    env_cfg["buildSkipped"] = ctx["buildSkipped"]

environment = {
    "gameVersion": scenario_env("gameVersion", "1.6.14"),
    "smapiVersion": scenario_env("smapiVersion", "4.4.0"),
    "modVersion": scenario_env("modVersion", "0.4.0"),
    "apiVersion": scenario_env("apiVersion", "1.0.0"),
    "saveId": scenario_env("saveId", "unknown"),
}
if ctx.get("mode") in ("ci", "local"):
    environment["mode"] = ctx["mode"]
if env_cfg:
    environment["config"] = env_cfg

# Commands: the scenario's declared init commands and steps, as argv arrays.
commands = []
def add_argv(argv):
    argv = [str(part) for part in argv if str(part)]
    if argv:
        commands.append({"argv": argv})

for entry in scenario.get("initCommands") or []:
    if not isinstance(entry, dict):
        continue
    if isinstance(entry.get("command"), str):
        add_argv(shlex.split(entry["command"]))
    elif isinstance(entry.get("file"), str):
        add_argv(["debug", "runmacro", entry["file"]])

for step in scenario.get("steps") or []:
    if not isinstance(step, dict):
        continue
    argv = ["step", str(step.get("action", "unknown"))]
    if isinstance(step.get("target"), str):
        argv.append(step["target"])
    if isinstance(step.get("inputs"), dict) and step["inputs"]:
        argv.append(json.dumps(step["inputs"], sort_keys=True, separators=(",", ":")))
    add_argv(argv)

# Materials: real files the run depended on, each with its own digest.
materials = []
def add_material(path, name, role):
    if isinstance(path, str) and os.path.isfile(path):
        materials.append({
            "uri": "file://" + os.path.abspath(path),
            "sha256": sha256(path),
            "name": name,
            "size": os.path.getsize(path),
            "annotations": {"role": role},
        })

game_path = ctx.get("gamePath") or ""
if game_path:
    add_material(os.path.join(game_path, "Stardew Valley.dll"), "Stardew Valley.dll", "game-file")
    add_material(os.path.join(game_path, "StardewModdingAPI.dll"), "StardewModdingAPI.dll", "game-file")
if scenario_path:
    add_material(scenario_path, os.path.basename(scenario_path), "dataset-instance")
add_material(ctx.get("modManifest") or "", "manifest.json", "mod-manifest")
if not materials:
    # A material list is mandatory only for R/E; keep it honest with the file we
    # always have rather than inventing an entry.
    add_material(scenario_path, os.path.basename(scenario_path) or "scenario.json", "dataset-instance")

seed_id = ctx.get("milestoneSave") or environment["saveId"]
seeds = [{
    "seedId": str(seed_id),
    "source": "milestoneSave",
    "baseCommit": str(ctx.get("commit") or "0000000000000000000000000000000000000000"),
}]

tool_versions = {"runner": "0.1.0"}
for name, version in (ctx.get("toolVersions") or {}).items():
    if isinstance(name, str) and isinstance(version, str) and version:
        tool_versions[name] = version

manifest = {
    "schemaVersion": 1,
    "runId": ctx.get("runId") or "",
    "timestamp": ctx.get("timestamp") or "",
    "toolVersions": tool_versions,
    "gameVersion": environment["gameVersion"],
    "modVersion": environment["modVersion"],
    "environment": environment,
    "expectedSignals": expected_signals,
    "subject": subject,
    "verdict": ctx.get("verdict") or "errored",
    "honestyLevel": ctx.get("honestyLevel") or "W",
    "attempts": attempts,
    "claimsFile": "claims.json",
    "artifacts": artifacts,
}
if isinstance(ctx.get("environmentOverrides"), list):
    manifest["environmentOverrides"] = ctx["environmentOverrides"]
if manifest["honestyLevel"] in ("R", "E"):
    manifest["commands"] = commands
    manifest["materials"] = materials
    manifest["seeds"] = seeds

with open(output, "w", encoding="utf-8") as f:
    json.dump(manifest, f, indent=2)
    f.write("\n")
PY
}

evidence_finalize() {
    [[ $# -eq 2 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1" input="$2"
    mkdir -p "$bundle"
    local manifest_input="$bundle/.manifest-input.json"
    # Build manifest input from provided input or synthesize
    if [[ -f "$input" ]]; then
        cp -f "$input" "$manifest_input"
    else
        printf '{"runId":""}\n' > "$manifest_input"
    fi
    # Ensure attempts[] is populated if tracked
    if [[ -f "$bundle/.attempts_list" ]]; then
        python3 - "$manifest_input" "$bundle/.attempts_list" <<'PY'
import json,sys
mip=sys.argv[1]
lst=sys.argv[2]
try:
    d=json.load(open(mip))
except Exception:
    d={}
attempts=[]
try:
    with open(lst) as f:
        for line in f:
            line=line.strip()
            if line:
                attempts.append(line)
except Exception:
    attempts=[]
# append-only semantics
if "attempts" not in d or not isinstance(d.get("attempts"), list):
    d["attempts"]=attempts
else:
    # ensure append-only (only add missing)
    existing=set(d["attempts"])
    for a in attempts:
        if a not in existing:
            d["attempts"].append(a)
json.dump(d,open(mip,"w"),indent=2)
PY
    fi
    # invoke ManifestBuilder (the single producer)
    if command -v dotnet >/dev/null 2>&1; then
        local dll
        local _lib_dir
        _lib_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
        for cand in "LivingRoots.Verification.Runner/bin/Release/net6.0/LivingRoots.Verification.Runner.dll" "LivingRoots.Verification.Runner/bin/Debug/net6.0/LivingRoots.Verification.Runner.dll" "$(dirname "$_lib_dir")/LivingRoots.Verification.Runner/bin/Release/net6.0/LivingRoots.Verification.Runner.dll"; do
            [[ -f "$cand" ]] && dll="$cand" && break
        done
        if [[ -n "${dll:-}" ]]; then
            dotnet "$dll" build-manifest "$bundle" "$manifest_input" >/dev/null 2>&1
            local rc=$?
            if (( rc == 0 )); then
                # manifest.sha256 must be written only after finalization by ManifestBuilder
                # wire retention receipt/export
                retention_receipt "$bundle" >/dev/null 2>&1 || true
                retention_export "$bundle" "$bundle" >/dev/null 2>&1 || true
                return 0
            fi
        fi
    fi
    return 13
}

evidence_write_claims() {
    [[ $# -eq 3 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1" runId="$2" honesty="$3"
    python3 - "$bundle/claims.json" "$runId" "$honesty" <<'PY'
import json,sys
path,runId,honesty=sys.argv[1],sys.argv[2],sys.argv[3]
d={"schemaVersion":1,"runId":runId,"honestyLevel":honesty,"claims":[]}
json.dump(d,open(path,"w"),indent=2)
PY
    return 0
}

evidence_write_integrity_failure() {
    [[ $# -ge 3 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1" class="$2" artifact="$3" detail="${4:-}"
    mkdir -p "$bundle"
    python3 - "$bundle/integrity-failure.json" "$class" "$artifact" "$detail" <<'PY'
import json,sys,datetime
path,cls,art,detail=sys.argv[1],sys.argv[2],sys.argv[3],sys.argv[4]
d={"schemaVersion":1,"runId":"","class":cls,"artifact":art,"recordedAt":datetime.datetime.now(datetime.timezone.utc).isoformat().replace("+00:00","Z")}
if detail: d["detail"]=detail
json.dump(d,open(path,"w"),indent=2)
PY
    return 0
}

evidence_validate() {
    [[ $# -eq 1 ]] || return "$EXIT_USAGE_ERROR"
    local bundle="$1"
    [[ -f "$bundle/manifest.json" ]] || return 19
    [[ -f "$bundle/manifest.sha256" ]] || return 19
    [[ -f "$bundle/claims.json" ]] || return 19
    return 0
}

export -f evidence_run_id evidence_bundle_dir evidence_init evidence_add_artifact \
       evidence_attempt_dir evidence_record_attempt evidence_finalize \
       evidence_write_manifest_input evidence_write_claims evidence_write_integrity_failure evidence_validate
