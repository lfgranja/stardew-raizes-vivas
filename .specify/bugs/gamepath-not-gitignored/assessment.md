# Bug Assessment: `.gamepath/` is not gitignored while CI points GamePath there — 6 proprietary DLLs are stageable into a public repo

- **Slug**: gamepath-not-gitignored
- **Created**: 2026-10-03
- **Source**: pasted text
- **Verdict**: valid
- **Severity**: high

## Report (verbatim)

> 1. AGENTS.md não documenta a receita do GamePath — release download + relocação para smapi-internal/ aninhado. Sem isso, ninguém mais consegue reproduzir esta verificação.
> 2. BUILDDEPS_ENABLED ainda presumivelmente false — os 816 verdes não têm contraparte em CI.

Submitted as a two-item report. Both items were checked against the repository before judging, and
the outcome is not what the report assumes:

| Reported item | Verdict | Evidence |
|---|---|---|
| 1 — `AGENTS.md` lacks the GamePath recipe | **partially true, narrow** | `AGENTS.md:17` already documents the destination layout *including* `smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll`; `:123` already names the repo and the `gamepath-v1` tag. What is missing is only the copy-pasteable acquisition commands — and a canonical version already exists at `.github/workflows/build_and_test.yml:73-85`. |
| 2 — `BUILDDEPS_ENABLED` presumably `false` | **invalid — factually wrong** | `gh variable list` → `BUILDDEPS_ENABLED  true  2026-10-03T16:22:19Z`. `gh secret list` → `BUILDDEPS_TOKEN` present. The report was ~3 hours stale. |

While verifying item 2, a **third defect of materially higher severity** surfaced that the report
does not mention. It is the subject of this assessment, per the user's scoping decision.

## Symptom

`.gitignore` contains no rule for the `.gamepath/` directory, and `.github/workflows/build_and_test.yml:58`
sets `GamePath: ${{ github.workspace }}/.gamepath` — **at the repository root, inside the working
tree**. A developer who mirrors the project's own CI convention locally and then runs `git add -A`
stages six proprietary binaries, including the 6.2 MB `Stardew Valley.dll` (ConcernedApe LLC),
into `lfgranja/stardew-raizes-vivas`, which is **PUBLIC**. The builddeps repository's README states
`NEVER MAKE THIS REPOSITORY PUBLIC` and `Redistribution is not permitted`.

Expected: proprietary game assemblies are unreachable to git regardless of where `GamePath` points.
Actual: they are fully stageable at the exact path the project's own workflow uses.

## Reproduction

Deterministic and non-destructive. No repository state was modified.

```bash
$ rg -n "gamepath" .gitignore
(no match — no rule exists)

$ git check-ignore -v .gamepath
(no match — not ignored)

$ git check-ignore -q ".gamepath/Stardew Valley.dll"; echo $?
1        # 0 = ignored, 1 = NOT ignored

$ git check-ignore -v "LivingRoots.Tests/bin/Release/net6.0/Stardew Valley.dll"
.gitignore:12:**/bin/      LivingRoots.Tests/bin/Release/net6.0/Stardew Valley.dll
```

All six assemblies are stageable:

```
.gamepath/Stardew Valley.dll                               → NOT IGNORED
.gamepath/StardewModdingAPI.dll                            → NOT IGNORED
.gamepath/MonoGame.Framework.dll                           → NOT IGNORED
.gamepath/xTile.dll                                        → NOT IGNORED
.gamepath/StardewValley.GameData.dll                       → NOT IGNORED
.gamepath/smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll  → NOT IGNORED
```

Target repository visibility:

```
$ gh repo view lfgranja/stardew-raizes-vivas --json visibility,isPrivate
{"isPrivate":false,"visibility":"PUBLIC"}
```

**No leak has occurred.** Verified: `git ls-files | grep -c '\.dll$'` → `0`, and `git add -A --dry-run`
stages zero DLL files. This assessment describes a live hazard, not an existing breach.

## Suspected Code Paths

- `.github/workflows/build_and_test.yml:58` — `GamePath: ${{ github.workspace }}/.gamepath`. This is
  the origin of the convention. In CI it is safe by luck: the runner is ephemeral. Copied to a
  developer machine it is not.
- `.github/workflows/build_and_test.yml:73-85` — the canonical acquisition recipe, including the
  flat→nested relocation at `:83-85` that the builddeps README's two tables contradict.
- `.gitignore` — no `gamepath` rule anywhere. The DLL rules that do exist (`:12` `**/bin/`, `:18-22`
  `**/bin/{Debug,Release}/*.dll`) only match paths under a `bin/` directory, so they do not cover
  assemblies at the root of `.gamepath/`.
- `.gitignore:461-474` — a `Secrets` block whose comment claims *"THE PRE-FLIGHT REFUSES TO START
  while any of these is unignored"*. See §Actionable Finding 2.
- `AGENTS.md:5-20` — the build instructions. `:15` states *"GamePath is not in either `.csproj` and
  not in `.gitignore` — nothing to edit, just pass it"*, which is actively misleading advice: passing
  it is exactly what creates the exposure, because the natural place to pass it is the path CI uses.
- `lefthook.yml` — pre-commit runs only `dotnet format`; pre-push runs only `dotnet test`. Neither
  inspects staged paths. Consistent with `AGENTS.md`: no git hooks are installed in `.git/hooks`.

## Root Cause Hypothesis

`.gitignore` was written for a C# mod project and never anticipated that the *game installation*
would be materialised inside the repository tree. ModBuildConfig's copy step
(`LivingRoots.Tests/bin/**`) lands under `**/bin/`, which was already covered at `.gitignore:12`,
so the original exposure was invisible. The builddeps convention introduced a second location —
`.gamepath/` at the root — and no rule was added for it. The gap is invisible to review because
`.gitignore` has no `gamepath` entry to notice is missing, and `git status` shows nothing until
someone actually runs the acquisition recipe.

Confidence: **high**. Every claim above is a direct read of committed files plus `git check-ignore`
behaviour, not an inference. The one inference — that a developer would point `GamePath` at
`.gamepath` — is supported by that path being the project's own documented convention.

## Proposed Remediation

**Preferred**: one `.gitignore` rule, plus a documentation correction.

1. **Add `.gamepath/` to `.gitignore`**, in the project-specific block near `:12-22`, with a comment
   stating *why*: it holds proprietary Stardew Valley assemblies that must never reach git. Suggested:

   ```gitignore
   # Local GamePath staging for the proprietary Stardew Valley assemblies fetched from the
   # builddeps release. build_and_test.yml uses this exact path; these DLLs must never be
   # committed (ConcernedApe LLC, redistribution not permitted).
   .gamepath/
   ```

   This alone closes the hazard, and it is the minimum change that does so.

2. **Correct the misleading sentence at `AGENTS.md:15`.** *"nothing to edit, just pass it"* should
   say plainly: point `GamePath` **outside** the repository, or at `.gamepath/` once rule 1 exists —
   never at an arbitrary path inside the tree. A reader currently has no way to learn this except by
   nearly making the mistake.

3. **Optional but cheap: mirror the acquisition recipe into `AGENTS.md`.** Reported item 1. The
   canonical form is `build_and_test.yml:73-85`; it needs the `gh release download`, the `unzip`,
   and the `mkdir -p smapi-internal && mv` relocation. Without at least a pointer to it, the flat vs
   nested contradiction in the upstream README will be rediscovered by every new developer — it cost
   this session two failed build attempts to work out.

**Alternatives** (considered and rejected):

- *Move the CI `GamePath` outside the workspace.* Rejected: it removes the hazard at the source but
  touches a workflow that is currently green and does not help developers who already use
  `.gamepath/` locally. Rule 1 is sufficient and lower risk.
- *Rely on the `.gitignore` secrets block at `:461-474`.* Rejected: that block covers `.env`, `*.pem`,
  `secrets.json`, `credentials.json`. It does not cover `.dll`, and adding the rule there rather than
  in the project block would obscure its purpose.
- *Add a staged-path check to `lefthook.yml`.* Not now — `AGENTS.md` records that no hooks are
  installed, so a hook would be dead configuration. Worth revisiting if hooks are ever adopted.

**Files likely to change**:

- `.gitignore` — one rule plus comment.
- `AGENTS.md` — correct `:15`, optionally add the acquisition recipe.

**Tests to add or update**: none. This is configuration. The verification is the negative check in
§Reproduction: after the fix, `git check-ignore -q ".gamepath/Stardew Valley.dll"` must return `0`.

## Risks & Considerations

- **The fix must not touch git history.** No `.dll` is currently tracked, so there is nothing to
  rewrite. A `git rm --cached` cleanup is not required and would be a no-op.
- **Adding `.gamepath/` is additive and cannot unignore anything.** It narrows staging; it cannot
  widen it. There is no `!`-negation to get wrong.
- **Verify with `git add -A --dry-run`, not `git add`.** The assessment deliberately used dry-run
  throughout and never staged a proprietary binary.
- **The real remediation is process, not configuration.** Rule 1 makes the safe path the default but
  does not stop a deliberate `git add -f`. `.gitignore:461-464` already articulates why the check
  matters: *"On a public repo that is publication, not a mistake you can take back — rotating
  afterwards is the cleanup, not the fix."*
- **Reported item 2 must not be actioned.** `BUILDDEPS_ENABLED` is already `true` and CI has two green
  runs. Any work planned against that premise is obsolete.
- **A consequence worth surfacing separately:** `AGENTS.md:126-128` instructs not to promote either
  check to `required_status_checks` until `Build and Test` has one genuinely green run. Runs
  `37137581500` and `37137502126` are both `success` with tests executing. **That gate condition is
  now met.** Whether to act on it is a policy decision, not a bug fix, and it is out of scope here.

## Actionable Finding 2 — a documented protection that does not exist

`.gitignore:461-468` asserts:

> *"Secrets. THE PRE-FLIGHT REFUSES TO START while any of these is unignored, and that refusal is the
> point: an empty `git check-ignore` result means the next run that commits publishes your key."*

No such preflight exists in this repository. `rg -l "check-ignore"` returns only `.gitignore` itself
and this bug's own artifacts; `lefthook.yml` runs `dotnet format` and `dotnet test` and nothing
else; `.git/hooks/` contains only `.sample` files. Confidence **medium** — the mechanism may live
outside the repo or in an unmerged branch, but it is not present here.

This matters more than a normal doc/code drift because it manufactures false confidence in exactly
the area where `.gamepath` is a gap. Recommend either implementing the check or rewording the comment
to describe the intended state. **Not included in the preferred remediation above** — it is a
distinct defect with its own blast radius, and the user's scoping decision was to keep this slug on
`.gamepath`.

## Open Questions

- [NEEDS CLARIFICATION: does the secret preflight described at `.gitignore:461-468` exist in another
  branch, another clone, or an external tool?] If not, should it be implemented or the comment
  reworded? Medium confidence that it is absent, not certain.
- [NEEDS CLARIFICATION: should `.gamepath/` be gitignored, or should the CI convention be changed to
  point outside the workspace?] Both close the hazard; rule 1 is lower risk, but the workflow is
  currently green and maintainers may prefer not to touch it.
- [NEEDS CLARIFICATION: is `required_status_checks` promotion now wanted?] The precondition at
  `AGENTS.md:126-128` is satisfied by two green runs. Note the same paragraph's warning still
  applies: the `paths-ignore` filter must be removed first or documentation-only PRs will block on a
  check that cannot report.
- [NEEDS CLARIFICATION: should `AGENTS.md` embed the full acquisition recipe, or link to
  `build_and_test.yml:73-85`?] Embedding duplicates a workflow that can drift; linking is terse but
  less discoverable. Recorded as reported item 1, deliberately not actioned in this slug.
