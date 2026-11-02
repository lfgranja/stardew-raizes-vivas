#!/usr/bin/env bash
#
# scripts/tests/lib/runner-harness.sh — extends harness.sh with fake Xvfb,
# fake SMAPI log, fake game install, fake milestone save, and a
# run_verify_scenario helper that always isolates HOME/XDG_*.
set -Eeuo pipefail
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/harness.sh"

# Fake Xvfb: a no-op executable placed on PATH.
make_fake_xvfb() {
    local dir="$1"
    mkdir -p "$dir"
    cat > "$dir/Xvfb" <<'EOF'
#!/usr/bin/env bash
exit 0
EOF
    chmod +x "$dir/Xvfb"
}

# Fake game install: minimal directory with required assemblies.
make_fake_game_dir() {
    local dir="$1"
    mkdir -p "$dir"
    touch "$dir/Stardew Valley.dll" "$dir/StardewModdingAPI.dll"
}

# Fake milestone save: an empty save directory.
make_fake_milestone_save() {
    local dir="$1"
    mkdir -p "$dir"
}

# run_verify_scenario: invoke scripts/verify-scenario.sh with isolated env.
run_verify_scenario() {
    local sandbox="$1"
    shift
    HOME="$sandbox/home" XDG_CONFIG_HOME="$sandbox/xdg" XDG_STATE_HOME="$sandbox/state" XDG_RUNTIME_DIR="$sandbox/run" \
        bash scripts/verify-scenario.sh "$@"
}