# scripts/lib/statistics.sh — flaky statistics (spec 010, T061e).

if [ -n "${_LR_STATISTICS_LOADED:-}" ]; then
    return 0 2>/dev/null || exit 0
fi
_LR_STATISTICS_LOADED=1

# shellcheck source=common.sh
source "$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)/common.sh"

wilson_ci_upper() {
    # wilson_ci_upper <successes> <n>  -> upper bound (success proportion)
    local s="${1:-0}"; local n="${2:-0}"
    python3 - "$s" "$n" <<'PY'
import sys, math
s=int(sys.argv[1]); n=int(sys.argv[2])
if n<=0:
    print("1.000000"); sys.exit(0)
p=s/n
z=1.959963984540054
den=1+z*z/n
center=p+z*z/(2*n)
rad=z*math.sqrt(p*(1-p)/n + z*z/(4*n*n))
upper=(center+rad)/den
print(f"{upper:.6f}")
PY
}

wilson_ci_lower() {
    local s="${1:-0}"; local n="${2:-0}"
    python3 - "$s" "$n" <<'PY'
import sys, math
s=int(sys.argv[1]); n=int(sys.argv[2])
if n<=0:
    print("0.000000"); sys.exit(0)
p=s/n
z=1.959963984540054
den=1+z*z/n
center=p+z*z/(2*n)
rad=z*math.sqrt(p*(1-p)/n + z*z/(4*n*n))
lower=(center-rad)/den
print(f"{max(0.0,lower):.6f}")
PY
}

wilson_failure_upper() {
    # upper bound for failure proportion: args <failures> <n>
    wilson_ci_upper "$1" "$2"
}

statistical_status() {
    # statistical_status <failures> <n>  -> not-evaluated|insufficient-data|confirmed-flaky|stable
    python3 - "$@" <<'PY'
import sys, math
failures=int(sys.argv[1]) if len(sys.argv)>1 else 0
n=int(sys.argv[2]) if len(sys.argv)>2 else 0
if n <= 0:
    print("not-evaluated"); sys.exit(0)
if n < 30:
    print("insufficient-data"); sys.exit(0)
p = failures / n
z = 1.959963984540054
den = 1 + z*z/n
center = p + z*z/(2*n)
rad = z*math.sqrt(p*(1-p)/n + z*z/(4*n*n))
upper = (center + rad) / den
if upper < 1.0 and p >= 0.30:
    print("confirmed-flaky")
else:
    print("stable")
PY
}

statistics_wilson_ci() { wilson_ci_gate "$@"; }
wilson_ci_gate() {
    # wilson_ci_gate <failures> <n>  -> 0 if gate passes (N>=30, upper<1.0, p>=0.30), 1 otherwise
    local failures="${1:-0}"; local n="${2:-0}"
    local status
    status="$(statistical_status "$failures" "$n")"
    if [[ "$status" == "confirmed-flaky" ]]; then
        return 0
    fi
    return 1
}

pass_rate_tier() {
    # pass_rate_tier <rate (0..1 or 0..100)>  -> healthy|monitor|alert
    python3 - "$1" <<'PY'
import sys
r=float(sys.argv[1])
if r > 1.0:
    r = r/100.0
if r >= 0.99:
    print("healthy")
elif r >= 0.95:
    print("monitor")
else:
    print("alert")
PY
}

# Benjamini-Hochberg multiple comparison control (q <= 0.05 default).
# Usage: benjamini_hochberg [q] p1 p2 ...   or pipe p-values (one per line) to stdin with optional q as $1.
benjamini_hochberg() {
    local q="0.05"
    local vals=()
    if [[ $# -gt 0 ]]; then
        # if first arg looks like float and rest exist, treat first as q? Heuristic: if $1 contains '.' and $#>1
        if [[ "$1" == *.* && $# -gt 1 ]]; then
            q="$1"; shift
        fi
        vals=("$@")
    fi
    if [[ ${#vals[@]} -eq 0 ]]; then
        vals=($(cat))
    fi
    python3 - "$q" "${vals[@]}" <<'PY'
import sys
q = float(sys.argv[1])
vals = [float(x) for x in sys.argv[2:]]
m = len(vals)
if m == 0:
    sys.exit(0)
order = sorted(range(m), key=lambda i: vals[i])
adj = [0.0]*m
prev = 1.0
for rank in range(m, 0, -1):
    i = order[rank-1]
    raw = vals[i] * m / rank
    prev = min(prev, raw)
    adj[i] = min(1.0, prev)
for i in range(m):
    reject = "true" if adj[i] <= q else "false"
    print(f"{vals[i]:.6f}\t{adj[i]:.6f}\t{reject}")
PY
}

bh_adjust() { benjamini_hochberg "$@"; }
bh_control() { benjamini_hochberg "$@"; }
raft_bh() { benjamini_hochberg "$@"; }
raft_bh_control() { benjamini_hochberg "$@"; }
multiple_comparison_control() { benjamini_hochberg "$@"; }

statistical_status_closed_set() {
    # prints the closed set
    printf '%s\n' "not-evaluated" "insufficient-data" "confirmed-flaky" "stable"
}

export -f wilson_ci_upper wilson_ci_lower wilson_failure_upper statistical_status \
          wilson_ci_gate pass_rate_tier benjamini_hochberg bh_adjust bh_control \
          raft_bh raft_bh_control multiple_comparison_control statistical_status_closed_set
