# State correction — 2026-11-18 (append-only)

This note corrects claims in the inherited 0.2.0-beta prep artifacts without rewriting their history. The 10 commits in PR #800 were authored on 2026-11-12/14 and describe a repository state that no longer holds after PRs #797 and #798 merged into `dev`.

## What changed since those commits

- `origin/dev` advanced from `af12158` to `8015129` (PR #797 `a3711dc`, PR #798 `8015129`).
- PR #797 added explicit `BUILDDEPS_TOKEN` diagnostics, a workflow-contract test and the CI trust-boundary section in `docs/quality-gate.md`.
- PR #798 recorded the 025 delivery in `.specify/memory/STATE.md`.
- The 10 commits from local `dev` were inherited and published as PR #800; they are no longer "unpushed".
- No tag and no public release were created. `0.1.0` remains the shipped mod version; `0.2.0` is still a draft/pre-release decision awaiting the maintainer.

## Superseded claims (keep originals verbatim)

| File / line | Original claim | Correct as of 2026-11-18 |
|---|---|---|
| `OBJETIVO.md:4`, `:62`, `:128`, `:153` | `dev = origin/dev`, 0 commits ahead, no PRs open | `dev` local and `origin/dev` had diverged; PRs #797–#800 were opened/merged; check `gh pr list` live before repeating any count |
| `ROADMAP.md:17`, `:20`, `:211` | `dev = origin/dev`, tree clean, 0 open PRs | same correction; remote state is [NV] unless checked live |
| `docs/0.2.0-beta-prep.md:60` | "Commits on `dev` (no push, ahead of `origin/dev`)" | those commits are now published via PR #800 |
| `docs/0.2.0-beta-prep.md:4`, `CHANGELOG.md:5` | "0 tags / 0 public releases" | still true, but must be re-dated: it describes 2026-11-12/14, not a fresh 2026-11-18 verification |

## Still accurate

- `.specify/assessments/0.2.0-beta/decision.md`, `.specify/validations/0.2.0-beta/validate.md`, `.specify/releases/0.2.0-beta/release.md`: "conditional / not bumped / manifest 0.1.0" remains valid. Only the provenance anchor needs the new date — no status promotion.
- The `release-gate.sh ALL STEPS PASSED` evidence at `27ed81b` (2026-11-14T21:22:43Z) remains a historical record; a fresh run on `30ce5c9` also passed on 2026-11-18T11:26:36Z.

## [NV] rule

GitHub PR/branch/CI status changes continuously. Every count above is a snapshot observed on 2026-11-18; re-check with `gh` before relying on it. Nothing here promotes a remote claim to verified.
