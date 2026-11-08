# Plan: CI / Release Gate (spec 019)

**Feature Branch**: `019-ci-release-gate`

**Lineage:** constitution → intake → research → define(spec) → shape(out, owner actions) → decide(go, local-only) → specify → clarify (none needed; the contract is deterministic) → plan (this file) → tasks → implementation (gate + reruns) → converge (appendix honesty) → release checklist (owner action).

## Design

- One authoritative local entry point: `scripts/release-gate.sh`. Sites the repo already depends on are reused, not rewritten: `dotnet format --verify-no-changes`, `dotnet restore --locked-mode`, `dotnet build/test`, `SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh`, `./scripts/verify-scenario.sh --check-docs`, `./scripts/check-protected-paths.sh`.
- No network, no secrets, no SMAPI/game boot inside the gate. Gate 3 remains a separate, deliberate attestation mechanism.
- Exit codes are informative: 3 dirty tree, 4 missing GamePath, 11 format, 12 restore, 13 build, 14 tests, 15 smoke, 16 docs, 17 protected-paths. 64 unknown argument.
- Hosted CI is an honest *restatement* of this contract (build_and_test exists, dormant by design). The gate does not drift relative to it.

## Sequence

1. [done] 019 scaffold + decision go(local-only).
2. [executes now] Re-certify launch-free smoke on a quiet host (background).
3. [executes now] Full `scripts/release-gate.sh` run; capture output into `verification/`.
4. [done] `release.yml` authored local-only, marked unverified remote.
5. [honesty] `verification/verification.md` records exactly what ran where; anything not run is stated unrun.
6. [owner action] host checklist in `quickstart.md` (secrets, required checks, branch protection, CODEOWNERS discipline, auto-merge, tag/release).

## Rollback

`git revert <gate commit>` or checkout last known-good SHA; the gate is additive (new script + new docs only); no existing file is modified, no behavior removed.

## Convergence note

All claims in this feature are locally evidenced; the only open items require host authority. `converge` appends this statement; no existing evidence is rewritten.
