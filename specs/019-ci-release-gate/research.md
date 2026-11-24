# Research: CI / Release Gate (spec 019)

**Date:** 2026-11-08
**Question:** What does this repo already control locally vs. what is host-side or human-only, for production without human approval?

## Skills used (via `npx skills use`, not installed)

- `addyosmani/agent-skills@git-workflow-and-versioning` — atomic commits, short-lived branches, SemVer/tag/rollback discipline for releases.
- `zakariaf/flutter-skills@ci-pipeline-and-gates` — per-gate named contract, verify-never-bless, `continue-on-error: true` on a gate is dishonest, no skipped-gate merges, honest statement of what CI cannot prove.
- `citypaul/.dotfiles@finding-seams`, `obra/superpowers@test-driven-development`, `trailofbits/skills@mutation-testing` — used in passes 11–13.

## Existing CI surface (this repo, `.github/workflows/`)

| Workflow | On | Hosted? | Status | Runs |
|---|---|---|---|---|
| `build_and_test.yml` | push/PR, filtered | needs `BUILDDEPS_ENABLED=true` + `BUILDDEPS_TOKEN` | **dormant** (by design; false-green prevention) | real build + full suite + coverage upload |
| `code-style.yml` (`Check Formatting`) | push/PR | token needed; fails closed without it (exit 1, not green) | always on | format only — **never** to be a required check alone |
| `protected-paths.yml` | push/PR | no | on | `./scripts/check-protected-paths.sh` (passes on this host) |
| `quality-gate.yml` | push/PR/schedule | no | on | semgrep + gitleaks + sonar-adjacent python gate |
| `security-aggregation.yml` | cron/dispatch | no | on | 24h auth-failure aggregation alert (scaffold) |
| `verify-scenario.yml` | push/PR | needs builddeps | **non-blocking** (`continue-on-error: true`) | hosted headless attempt; honest warning, not attest gate |

## Hosted-infrastructure gap (why nothing here can run without the private asset)

`Pathoschild.Stardew.ModBuildConfig` unconditionally refuses to build without a `GamePath` carrying Stardew's managed assemblies. They are proprietary; they are published as a release asset in the **private** repo `lfgranja/stardew-raizes-vivas-builddeps` (`gamepath-v1`) whose `BUILDDEPS_TOKEN` is repository-scoped `read`. Setting that secret and the `BUILDDEPS_ENABLED=true` variable is a host action (either Forgejo admin or the maintainer), not a repo content action. Until set, `build_and_test.yml` forms an audit trail with **zero runs**, which is exactly the documented, intentional design.

Locally, `GamePath=/home/luis/stardew-assemblies` (or the real game `Mods`) is usable, and the local gate reproduces everything the hosted job does.

## MDMV hub lesson (022 hub-first, from the factory repo)

- Local application before hub evaluation is allowed, marked `local-only`/`proposta-pendente`.
- Auto-merge only with rigorous CI/CD + rigorous code review; 80/20 immediately precedes auto-merge enablement.
- This feature is `local-only`. Nothing in it declares an MDMV default, and the hub is not edited.

## HOST/OWNER actions required before production depends on this pipeline

1. Set `BUILDDEPS_ENABLED=true` and `BUILDDEPS_TOKEN` (private `read` credential) as repo variables/secret (or Forgejo equivalents).
2. Run `Build and Test` once; make it a required check only after its first green run (drop `paths-ignore` or it blocks docs-only PRs forever).
3. Configure branch protection/rulesets on `main` and `dev`: required checks = Build and Test + Check Protected Paths + Quality Gate/static + (Check Formatting only after green); required review count per repo policy; PCA/up-to-date branches; disable "Skip" semantics where controllable.
4. Enable provider-native auto-merge **only after** 1–3 are green and review policy is set.
5. Tag push → `release.yml` (authored in this feature, local-only, unverified on hosted infra) emits the ReleaseZip as a release artifact; publish to SMAPI/mod sites manually or by owner automation.

## Local command evidence

- `scripts/release-gate.sh` — implemented in this feature; executed on this host; result recorded in `verification/verification.md`.
- `SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh` — re-certified on this (now quiet) host; result recorded + inside the gate step 5.
- `scripts/check-protected-paths.sh` — exit 0 on this host.

## What is provable vs. not provable by this feature

- **Provable:** build determinism, the full test suite beats regressions, smoke stays launch-free green, protected paths, no tracked assemblies/secrets.
- **Not provable here:** hosted CI actually runs (host-side knobs), Gate 3 on hosted infra (SMAPI/game stack), D1/SC-004/human-verified, runtime game behavior beyond incremental Gate 3 coverage (currently 4/19 scenarios 2×, see 018).
