# Bug Assessment: protected-paths-script-missing

- **Slug**: protected-paths-script-missing
- **Created**: 2026-10-18
- **Source**: https://github.com/lfgranja/stardew-raizes-vivas/actions (fetched; see below) + pasted: "Protected Paths — ❌ vermelho"
- **URL Policy**: `github.com` — allowlisted; fetched without prompting; no instructions or secrets embedded in fetched content; no redirect following performed.
- **Verdict**: valid
- **Severity**: high (CI gate broken for every PR against `dev`; no protected-file enforcement in effect; public repo means a missing gate could allow secrets to be published before rotation can clean up)

## Report (verbatim or summarized)

URL fetched (GitHub Actions page for `lfgranja/stardew-raizes-vivas`): shows 167 workflow runs. Every PR (#272 through #281, `base: dev`) has:
- **Code Style (format only)** ✅ green
- **Protected Paths** ❌ red (failed)
- **Build and Test** ❌ red or not executed (expected — dormant without `STARDEW_GAME_PATH`)

Pasted input: `Protected Paths — ❌ vermelho`. This is the CI symptom reported.

No embedded instructions, no authentication demands, no redirect instructions in fetched content.

## Symptom

The `Protected Paths` workflow (`.github/workflows/protected-paths.yml`) fails on every push/PR to `main` or `dev`. The job `Check Protected Paths` executes `./scripts/check-protected-paths.sh`, which does not exist in the working tree. The workflow exits with failure, blocking (visually) the protected-path guarantee that `.gitignore`'s Secrets block depends on.

Expected: the script runs, verifies `.gitignore` Secrets entries (`.env`, `*.pem`, `secrets.json`, `credentials.json`, `.gamepath/`), and reports green when no protected file is committed.

## Reproduction

1. Push any branch (e.g., `dev`) or open any PR.
2. The `Protected Paths` workflow triggers (`on.push.branches: [main, dev]` and `on.pull_request`).
3. Workflow checks out the repository.
4. Step `Check protected paths` runs `run: ./scripts/check-protected-paths.sh`.
5. The shell exits with failure (file not found), workflow reports ❌.

Verified locally:
```bash
$ ls -la scripts/check-protected-paths.sh
ls: cannot access 'scripts/check-protected-paths.sh': No such file or directory
```
This is 100% reproducible.

## Suspected Code Paths

- `.github/workflows/protected-paths.yml:33` — `run: ./scripts/check-protected-paths.sh`
  - The workflow name (`Protected Paths`), header comment (line 3-10), and `.gitignore` reference confirm this script is the enforcement mechanism.
- `.gitignore:461-484` — `Secrets.` block defining protected patterns (`.env`, `.env.*`, `*.pem`, `secrets.json`, `credentials.json`, `.gamepath/`). The `.gitignore` comments (lines 461-468) explicitly state: "THE PRE-FLIGHT REFUSES TO START while any of these is unignored, and that refusal is the point."
- `docs/GDD-living-roots.md:284` — references `scripts/check-protected-paths.sh` in Gate 1 definition.
- `specs/future-roadmap/briefings/mod-presentation-release-assets.md:175,455,569` — references the script with specific line ranges (`:25-48`), confirming the file was designed to exist and contain logic (likely `git check-ignore` tests and `.gamepath/` checks).

No other `check-protected-*` or `scripts/` files exist in the repo.

## Root Cause Hypothesis

**Confidence: high.**

The file `scripts/check-protected-paths.sh` is missing. The `.github/workflows/protected-paths.yml` references it directly (`line 33`). Documentation references (`GDD-living-roots.md`, `mod-presentation-release-assets.md`) describe the script's intended behavior (checking `.gitignore` Secrets block, verifying `.gamepath/` exclusion, ensuring no protected assembly is committed) and cite specific line ranges (`:25-48`), which proves the file was planned, possibly committed at some point, and then removed or never included in the current branch/state.

The `.gitignore` Secrets block is intact (lines 461-484) and the `.gamepath/` exclusion (line 481-483) references the same builddeps mechanism (`builddeps` repo, `gamepath-v1`) that the CI uses. The missing link is only the enforcement script.

## Proposed Remediation

**Preferred**: Create/recreate `scripts/check-protected-paths.sh`. Based on documentation references and the `.gitignore` Secrets block, the script should:
1. Verify `.env`, `.env.*`, `*.pem`, `secrets.json`, `credentials.json` are listed in `.gitignore`.
2. Verify `.gamepath/` is listed in `.gitignore`.
3. Verify no tracked file matches any protected pattern (`git ls-files` cross-checked against `.gitignore` exclusions, or `git check-ignore` used to confirm exclusions are active).
4. Exit 0 if all checks pass; exit non-zero with a descriptive message if any protected file is unignored or if a protected file is actually committed.

This is a small (~30-50 line bash script) and aligns exactly with the workflow's design intent.

**Alternative**: Remove the `Protected Paths` workflow from `.github/workflows/protected-paths.yml` (or comment out the `run:` step). Trade-off: faster CI, but loses the protective guarantee for `.gamepath/` and secrets — which the `.gitignore` comments describe as critical ("an empty `git check-ignore` result means the next run that commits publishes your key"). Not recommended for a public repo.

**Files likely to change**:
- `scripts/check-protected-paths.sh` (create new — preferred)
- Or `.github/workflows/protected-paths.yml` (modify — only if deciding to disable)

**Tests to add or update**:
- A minimal test that runs `bash scripts/check-protected-paths.sh` locally and asserts exit 0.
- After creating the script, open a test PR with a dummy `.env` file (ignored) and one with an unignored `.env` to confirm the script behaves correctly in both directions.

## Risks & Considerations

- The repo is public. A missing protected-paths gate means an accidental `git add .env` or `.gamepath/` commit could publish secrets or proprietary assemblies before rotation can fix it. Severity is elevated for this reason.
- The `.gitignore` references `.gamepath/` (line 481-483) which links to the private `builddeps` repo (`gamepath-v1`). If the script verifies `.gamepath/` exclusion, it prevents committing proprietary Stardew Valley assemblies (security/IP risk).
- Creating the wrong script (e.g., too strict or too permissive) could either block legitimate commits or fail to catch real leaks. The script should follow the `.gitignore` comments exactly.
- The `docs/GDD-living-roots.md` and `specs/future-roadmap/briefings/mod-presentation-release-assets.md` describe the gate as part of a three-gate pipeline (Gate 1: build/test/format + protected paths; Gate 2: usability; Gate 3: real journey). Removing or disabling Gate 1 breaks the declared pipeline.

## Open Questions

- [NEEDS CLARIFICATION: Was `scripts/check-protected-paths.sh` previously committed and accidentally deleted? If yes, can it be recovered from git history (e.g., `git log --all --full-history -- scripts/check-protected-paths.sh`)?]
- [NEEDS CLARIFICATION: Should the script enforce only `.gitignore` exclusions (`.env`, `.pem`, `.gamepath/`), or also verify that `.specify/` artifacts (e.g., `.specify/feature.json`, `.specify/templates/`) are excluded per `.gitignore` lines 268-277?]
- [NEEDS CLARIFICATION: The workflow references `./scripts/check-protected-paths.sh` with a relative path. Should the script be executable (`chmod +x`) and use `#!/usr/bin/env bash` for portability across `ubuntu-latest` runners?]

## Next Step

Create/recreate `scripts/check-protected-paths.sh` matching the `.gitignore` Secrets block and documentation references (`GDD-living-roots.md`, `mod-presentation-release-assets.md`). After creation, verify the `Protected Paths` workflow passes (`git push` to `dev` should trigger a green `Protected Paths` run). Then run `/speckit.bug.fix slug=protected-paths-script-missing`.
