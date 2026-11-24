# Quickstart — CI / Release Gate (spec 019, local-only)

## Run the gate (this host)

```bash
GamePath=/home/luis/stardew-assemblies scripts/release-gate.sh
# or the real game install:
GamePath="/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" scripts/release-gate.sh
```

Expected: `[release-gate] ALL STEPS PASSED`, exit 0, commit SHA printed. Anything else fails closed with the step named (`11` format, `12` locked restore, `13` build, `14` tests, `15` smoke, `16` docs, `17` protected paths, `3` dirty tree, `4` missing GamePath).

Gate-3 attestation (real game) is intentionally **not** in this command. See `VERIFICATION_PROTOCOL.md` — runs are deliberate and produce bundles (`evidence-bundle-run-*`).

## Host enablement checklist (owner action; the only human part)

1. Builddeps: `BUILDDEPS_ENABLED=true`, `BUILDDEPS_TOKEN` = private `read` credential for `lfgranja/stardew-raizes-vivas-builddeps` (release `gamepath-v1`).
2. Run `Build and Test` once; confirm green; drop the workflow's `paths-ignore` before marking required.
3. Branch protection / rulesets on `main` and `dev`:
   - required: Build and Test, Check Protected Paths, Quality Gate / static, Check Formatting (after first green);
   - require branches up to date; disallow skipping; required approvals ≥1 per repo policy (CODEOWNERS discipline for @lfgranja paths);
   - stale approval dismissal on push; conversation resolution where supported.
4. Enable provider-native auto-merge **only after 1–3 are green** and policy is in place (MDMV 022 hub-first minimum).
5. Release: push tag `v*.*.*`; `release.yml` builds the ReleaseZip and uploads it as a release asset; verify the asset digests before publishing to SMAPI/mod sites; record the tag + SHA in the release notes (Gate R7: SemVer, changelog with FR/T IDs, rollback plan, comms).
6. Rollback: `git revert <bad-commit>` (or reset to the recorded known-good SHA) and re-release; no improvised rollback steps on production.

## What this proves and what it cannot prove

- **Proves (locally):** clean tree, locked restore, format, build, full tests, launch-free smoke, docs markers, no tracked assemblies/secrets.
- **Cannot prove (user decides):** the hosted runner actually executes (only host-side knobs), D1/SC-004/human-verified, game-play balance — all owned by the user (SC-004/D1 planned for tonight).

## MDMV status

`local-only` / `proposta-pendente` (hub-first 022). Not an MDMV default. Do not edit the hub from this repo.
