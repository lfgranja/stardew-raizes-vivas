# Bug Fix: `.gamepath/` gitignored, closing the proprietary-assembly staging hazard

- **Slug**: gamepath-not-gitignored
- **Fixed**: 2026-10-03
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

Added a `.gamepath/` rule to `.gitignore` and corrected the `AGENTS.md` line that advised passing
`GamePath` anywhere without warning. The project's own CI points `GamePath` at `.gamepath/` at the
repository root (`.github/workflows/build_and_test.yml:58`), and with no rule for it, all six
proprietary Stardew Valley assemblies — including the 6.2 MB `Stardew Valley.dll` (ConcernedApe LLC)
— were stageable by `git add -A` into a **public** repository. The hazard is now closed and verified.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `.gitignore` | modified | +7 lines: `.gamepath/` rule with a comment stating why, placed in the project-specific DLL block at `:17-29`. |
| `AGENTS.md` | modified | +11 / −1 lines in the `GamePath` block (`:15-29`): dropped *"and not in `.gitignore` — nothing to edit, just pass it"*, added the in-tree warning and a pointer to the acquisition recipe with the flat→nested gotcha. |
| `.specify/bugs/gamepath-not-gitignored/fix.md` | added | This report. |

No production code, no test code, no workflow file was touched.

> **Scope note on `AGENTS.md`:** `git diff --stat AGENTS.md` reports **245 changed lines**, but that
> is almost entirely a **pre-existing uncommitted rewrite** of the whole file (from
> `# LIVING ROOTS — PROJECT KNOWLEDGE BASE` to `# Living Roots — Agent Notes`). `AGENTS.md` was listed
> as ` M` in `git status` at the start of this session, before any edit here. **This fix's own
> contribution is the ~12 lines in the `GamePath` block quoted below.** Anyone splitting commits must
> not attribute the whole rewrite to this fix, and should consider whether the rewrite should be
> committed separately first.

## Diff Highlights

`.gitignore` — the rule that closes the hazard:

```gitignore
 # Local GamePath staging for the proprietary Stardew Valley assemblies fetched from the
 # builddeps release. build_and_test.yml uses this exact path, so following the project's
 # own CI convention locally lands 6 DLLs (incl. the 6.2 MB proprietary Stardew Valley.dll,
 # ConcernedApe LLC) at the repo root, where they would otherwise be stageable. Redistribution
 # is not permitted — see lfgranja/stardew-raizes-vivas-builddeps.
+.gamepath/
```

`AGENTS.md` — the sentence that was actively dangerous:

```diff
-`GamePath` is **not** in either `.csproj` and not in `.gitignore` — nothing to edit, just pass it.
+`GamePath` is **not** in either `.csproj` — nothing to edit, just pass it.
 The folder must contain `Stardew Valley.dll`, … and `smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll`.
 Same list the CI preflight step checks.
+
+**Point `GamePath` outside the repository, or at `.gamepath/`** — which `.gitignore` now ignores.
+Never at an arbitrary path inside the working tree: these are proprietary Stardew Valley assemblies
+(ConcernedApe LLC) and a bare `git add -A` at any unignored in-tree path would stage them into this
+**public** repo. That is publication, not a mistake you can take back.
+
+No game folder on this machine? `.github/workflows/build_and_test.yml` (see the `GamePath` assembly
+block) has the working recipe: … then **relocate `SMAPI.Toolkit.CoreInterfaces.dll` into a nested
+`smapi-internal/`** — the zip is flat but ModBuildConfig resolves that one assembly from the nested
+path, and the upstream README's two tables contradict each other on this.
```

The `not in .gitignore` clause was removed because it is now false *and* because it was the phrasing
that implied any in-tree path was fine.

## Tests Added or Updated

None, per the assessment: *"This is configuration. The verification is the negative check… `git
check-ignore -q ".gamepath/Stardew Valley.dll"` must return `0`."* No automated test can guard a
gitignore rule, and `.gitignore:461-474` already documents why a real preflight is the missing piece
(see §Follow-ups).

## Local Verification

The decisive checks, all run against the real gitignore engine:

| Check | Command | Result |
|---|---|---|
| All 6 assemblies ignored | `git check-ignore -q` on each `.gamepath/**` DLL | **pass** — all 6 ignored |
| Nested path ignored | `git check-ignore -q ".gamepath/smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll"` | **pass** — `.gitignore:29` |
| Zip also ignored | `git check-ignore -q ".gamepath/gamepath.zip"` | **pass** |
| Rule matches a **real** directory | created `.gamepath/` with dummy files, re-ran `check-ignore` | **pass** — directory and contents all ignored |
| `git status` stays clean | `git status --porcelain \| grep -E "^\?\? \.gamepath/"` | **pass** — no entry |
| `git add -A` stages nothing | `git add -A --dry-run \| grep -E "add '\.gamepath/"` | **pass** — 0 matches |
| Rule survives directory absence | `git check-ignore -v ".gamepath/Stardew Valley.dll"` after `rm -rf .gamepath` | **pass** — still `.gitignore:29` |
| Format gate | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | **pass** |
| Regression suite | `dotnet test --no-build -c Release` | **pass** — `Com falha: 0, Aprovado: 816` |
| No pre-existing exposure | `git ls-files \| grep -c '\.dll$'` | **pass** — `0` |

Before/after on the exact path CI uses:

```
before:  git check-ignore -q ".gamepath/Stardew Valley.dll"   → 1  (NOT ignored)
after:   git check-ignore -q ".gamepath/Stardew Valley.dll"   → 0  (ignored, .gitignore:29)
```

Two things worth recording honestly about the verification itself:

- **`check-ignore` on the bare name `.gamepath` fails even with the rule in place**, because the
  pattern ends in `/` and git will not classify a non-existent path as a directory. It only matches
  once the directory exists. The rule is correct; the naive one-liner is not. Verified by creating
  the directory.
- **Two of my intermediate checks produced false positives** because they grepped for the substring
  `gamepath`, which also matches `.specify/bugs/gamepath-not-gitignored/`. Re-run with `^\?\? \.gamepath/`
  and `add '\.gamepath/` anchors, both pass. Recorded because the same loose grep would be easy to
  trust in a future audit.

All staged-path checks used `--dry-run`. No proprietary binary was ever staged. The dummy files used
for the real-directory test were named after the assemblies but were empty `touch` files; the
directory was removed afterwards.

## Deviations from Assessment

1. **Assessment item 3 was partially applied, by linking rather than embedding.** The assessment listed
   *"mirror the acquisition recipe into `AGENTS.md`"* as optional, while an Open Question asked
   *"should `AGENTS.md` embed the full recipe, or link to `build_and_test.yml:73-85`?"* — unresolved.
   I resolved it by **linking with the critical detail inline**: a pointer to the workflow plus the
   flat-vs-nested relocation, which is the one step the assessment identified as easy to miss and the
   one that cost this session two failed builds. The full recipe was not duplicated, because a copy
   inside `AGENTS.md` would drift from the workflow silently. The embed-vs-link question is therefore
   answered in practice, not left open.

2. **A verification technique deviated from the assessment's suggestion.** The assessment proposed
   `git check-ignore -q ".gamepath/Stardew Valley.dll"` returning `0` as the check. That check passes
   but is weak on its own — it does not prove the rule matches a real directory. A stronger check was
   added: create the directory, re-test, then remove it. Both are reported above.

No other departures. The change stayed inside the two files the assessment named.

## Follow-ups

- **`.gitignore:461-468` still describes a preflight that does not exist.** It asserts *"THE
  PRE-FLIGHT REFUSES TO START while any of these is unignored"*; `rg -l "check-ignore"` finds only
  `.gitignore` itself, `lefthook.yml` runs only `dotnet format` and `dotnet test`, and `.git/hooks/`
  holds only `.sample` files. This fix reduces the need for that check (the safe path is now the
  default) but does not replace it. Deliberately not actioned here — the assessment scoped it out as
  a separate defect with its own blast radius. It is the highest-value follow-up in this repo,
  because it would catch the next file that appears at an unignored in-tree path, which is the general
  form of the bug just fixed.
- **Commit the `AGENTS.md` rewrite separately, or not at all.** See §Scope note. Right now the
  `.gitignore` fix and a 245-line `AGENTS.md` rewrite are entangled in the same working tree, along
  with 10 other modified files (`.specify/scripts/*`, `.specify/integrations/*`) and untracked
  tooling directories.
- **Local cleanup.** The build copied `Stardew Valley.dll` and 5 other proprietary assemblies into
  `LivingRoots.Tests/bin/{Debug,Release}/net6.0/`. Git-ignored and untracked (`git ls-files` → 0), but
  present on disk. `dotnet clean` if that should not persist.
- **Consider promoting the CI checks.** `AGENTS.md:126-128` forbids promoting either check to
  `required_status_checks` until `Build and Test` has one genuinely green run. Runs `37137581500` and
  `37137502126` are `success` with tests executing, so that gate is satisfied. Same paragraph's warning
  applies: remove the `paths-ignore` filter first. Policy decision, not a bug fix.
- **Do not action reported item 2.** `BUILDDEPS_ENABLED` is already `true`.
