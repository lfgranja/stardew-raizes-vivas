# Contract: CI credential guard and deterministic check

## Guard message contract

Every non-best-effort workflow step whose `env.GH_TOKEN` references `secrets.BUILDDEPS_TOKEN` MUST begin with a fail-fast block equivalent to:

```bash
set -euo pipefail
if [ -z "${GH_TOKEN:-}" ]; then
  echo "::error::BUILDDEPS_TOKEN is not set; cannot download the private build-deps release."
  exit 1
fi
```

- The message MUST contain the literal token name and the word `not set`.
- Empty string and unset are both treated as missing.
- The block MUST appear before any `gh release download` or `mkdir` of the game-assembly path.
- Best-effort jobs (`continue-on-error: true`, or the documented `::warning::` verification path) are exempt and MUST NOT be converted into hard failures.

## Deterministic check contract

The local check derives a list of `CredentialDependentStep` records from all files in `.github/workflows/*.yml`:

- **pass** — every non-best-effort consumer has a guard message and an `exit 1`, and the guard precedes first use.
- **fail** — any non-best-effort consumer lacks the guard; the failure output names the file, job, and step.
- **exclusions** — jobs with `continue-on-error: true`; steps using `::warning::` by design.
- **no mutation** — the check reads workflow files only and never edits them.

## Trust-boundary contract (documentation)

The local quality-gate guide MUST state, in one dedicated section:

1. `scripts/release-gate.sh` is the local authoritative gate (seven steps).
2. `sdd-gate` is the only hosted required check for `dev` and cannot detect build/test failures.
3. Which job families depend on the private credential.
4. Which claims remain [NV] in this session (remote step-level root causes).
