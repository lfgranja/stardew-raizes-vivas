# Concept: Explicit secret-dependent CI failures

- **Slug**: 025-ci-secret-guard-explicitness
- **Created**: 2026-11-18T10:56:00Z
- **Recommended option**: Option A — Guard + contract test + trust-boundary docs

## Options

### Option A — Guard + contract test + trust-boundary docs
- **Sketch**: The one token-consuming job that still fails opaquely (`build_and_test.yml`) gains the same fail-fast message its two peers already use. A deterministic test scans the workflows and asserts that every non-best-effort step consuming `BUILDDEPS_TOKEN` carries that guard, so the invariant is enforced rather than merely written. The local quality-gate guide gains a short, honest section on what hosted CI proves, what the local `release-gate.sh` proves, and which remote claims stay [NV].
- **Appetite**: small (a workflow step, one test module, one docs section).
- **Trade-offs**: wins immediate diagnosability and prevents regression; sacrifices nothing in gameplay or owner authority. Cost is a third copy of a one-line guard plus a YAML-parsing test dependency already used by the existing test suite. Risk: a malformed `if`/`exit` could alter CI semantics, mitigated by adding only a pre-flight `if [ -z ... ]; then ::error::; exit 1; fi` before existing commands.
- **Rabbit holes**: expanding into `release.yml` (owner-gated), "fixing" `verify-scenario.yml`'s intentional warning/continue-on-error, or trying to change hosted required checks.

### Option B — Document only, change no workflow
- **Sketch**: Add an explanation to `docs/quality-gate.md` that a red `Build and Test` may mean a missing private credential, and leave workflows untouched.
- **Appetite**: small (docs only).
- **Trade-offs**: zero operational risk, but the check output itself stays ambiguous and nothing prevents the next unguarded job. Fails success metric 1 and 2.
- **Rabbit holes**: turning the doc into a stale claim the moment workflow topology changes.

### Option C — Full CI trust elevation (required checks, all workflows, baseline ratchet)
- **Sketch**: Make build/test/format hosted required checks, guard every token consumer including `release.yml`, and fix the 0% jscpd / coverage-baseline fragility at the same time.
- **Appetite**: large.
- **Trade-offs**: addresses merge confidence most thoroughly, but branch protection, secrets and release controls are explicitly owner-only in this cycle (spec 019 T006/T007, STATE.md blockers). Cannot be executed autonomously.
- **Rabbit holes**: host-side settings not present in the repo; cross-file conflicts with the owner's unpushed commits (`sdd-gate.yml`, `aggregate-gate.sh`).

## Recommendation

**Option A.** It satisfies all four success metrics from `problem.md` with the smallest reviewable change, keeps the guard fail-closed (per `ci-skipped-job-false-green`), touches only paths disjoint from the owner's unpushed work, and requires no owner authorization. Option B leaves the core ambiguity intact; Option C is not executable in this cycle.

## Out of Scope (for the recommended option)

- Branch protection, required checks, secrets, auto-merge, tags, releases (owner-only).
- `verify-scenario.yml` warning path / `continue-on-error: true` (documented best-effort).
- `release.yml` guard (tag-triggered, owner-gated).
- Dependabot PR triage, jscpd/coverage baselines, any runnable C# change, any new locale strings.

## Assumptions to Validate

- PyYAML is already a dependency of the quality-gate tests (`test_workflow_contract.py` imports it) — verify before writing the new test.
- The exact guard wording from `code-style.yml:49` can be reused verbatim without tripping the 0% jscpd ceiling.
- `build_and_test.yml` is the only non-best-effort token consumer missing the guard — re-verify with `grep` at implementation time, since other sessions may mutate the repo.
