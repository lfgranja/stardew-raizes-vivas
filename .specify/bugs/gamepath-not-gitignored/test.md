# Bug Verification: `.gamepath/` gitignored — proprietary assemblies no longer stageable

- **Slug**: gamepath-not-gitignored
- **Tested**: 2026-10-03
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The hazard does not reproduce. The assessment's exact reproduction — `git check-ignore` over the six
proprietary assemblies at the path CI uses — now reports every one of them ignored, attributed to
`.gitignore:29`. `git add -A` stages nothing from `.gamepath/`, `git status` shows no entry, and the
816-test suite is unaffected. No regressions.

One check from `fix.md` could not be re-executed under this command's guardrail and is disclosed
below rather than claimed.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) — rule exists | `rg -n "gamepath" .gitignore` | **pass** | Was *no match*; now `.gitignore:29:.gamepath/` |
| Reproduction (post-fix) — all 6 assemblies | `git check-ignore -q` on each `.gamepath/**` DLL | **pass** | Was **NOT IGNORED ×6**; now ignored ×6 |
| Rule attribution | `git check-ignore -v` | **pass** | Both root and nested DLLs → `.gitignore:29:.gamepath/` |
| `git add -A` stages nothing | `git add -A --dry-run \| grep -E "add '\.gamepath/"` | **pass** | 0 matches |
| `git status` clean | `git status --porcelain \| grep -E "^\?\? \.gamepath/"` | **pass** | No entry |
| No pre-existing exposure | `git ls-files \| grep -c '\.dll$'` | **pass** | `0` |
| Fix target: misleading `.gitignore` claim removed | `rg "not in \`.gitignore\`" AGENTS.md` | **pass** | No match. The false clause is gone. |
| Fix target: warning present | `rg "Point \`GamePath\` outside the repository\|relocate \`SMAPI" AGENTS.md` | **pass** | Present at `:20` and `:27` |
| Regression suite | `dotnet test --no-build -c Release` | **pass** | `Com falha: 0, Aprovado: 816, Ignorado: 0` |
| Lint / type-check | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | **pass** | Exit 0 |
| New / updated tests | — | **n/a** | None exist or were added; a gitignore rule cannot be covered by the test suite. `fix.md` states this. |
| Rule matches a **real** directory | create `.gamepath/` with dummy files, re-run `check-ignore`, remove | **not-run** | **Guardrail:** this command must write only inside `BUG_DIR`. Performed during `/speckit.bug.fix`; result recorded there — directory and all contents ignored. Not independently re-run here. |

## Output Excerpts

Before → after on the exact path CI uses (`build_and_test.yml:58` sets
`GamePath: ${{ github.workspace }}/.gamepath`):

```
before (assessment):                      after (this verification):
.gamepath/Stardew Valley.dll              → NOT IGNORED     IGNORED
.gamepath/StardewModdingAPI.dll           → NOT IGNORED     IGNORED
.gamepath/StardewValley.GameData.dll      → NOT IGNORED     IGNORED
.gamepath/MonoGame.Framework.dll          → NOT IGNORED     IGNORED
.gamepath/xTile.dll                       → NOT IGNORED     IGNORED
.gamepath/smapi-internal/
  SMAPI.Toolkit.CoreInterfaces.dll        → NOT IGNORED     IGNORED

$ rg -n "gamepath" .gitignore
29:.gamepath/

$ git check-ignore -v ".gamepath/Stardew Valley.dll" \
                   ".gamepath/smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll"
.gitignore:29:.gamepath/	.gamepath/Stardew Valley.dll
.gitignore:29:.gamepath/	.gamepath/smapi-internal/SMAPI.Toolkit.CoreInterfaces.dll
```

```
$ git add -A --dry-run | grep -E "add '\.gamepath/"
PASS: git add -A stages nothing from .gamepath/

$ git ls-files | grep -c '\.dll$'
0

$ dotnet test --no-build -c Release
Com falha: 0   Aprovado: 816   Ignorado: 0
```

## Residual Risks

- **One check was not re-executed.** The "rule matches a real directory" check requires creating
  `.gamepath/`, which this command's guardrail forbids. It passed during `/speckit.bug.fix` with
  empty `touch` files, and the directory was removed. `git check-ignore -v` on nested paths matches
  `.gitignore:29` whether or not the directory exists, which is why the pattern is confirmed here —
  but the on-disk behaviour was not independently witnessed in this command. Low risk; disclosed
  rather than claimed.
- **`AGENTS.md:15` still reads "nothing to edit, just pass it".** Precisely: the fix removed
  *"and not in `.gitignore`"* — the false, dangerous clause — and left this clause intact. It is now
  immediately qualified by `:20-23` ("Point `GamePath` outside the repository, or at `.gamepath/` …
  That is publication, not a mistake you can take back"). The assessment's requirement — that the
  line state plainly where to point `GamePath` — is met. But "just pass it" followed by a warning
  reads slightly contradictory in tone, and a future editor trimming the file could drop one without
  the other.
- **The fix is configuration, so nothing guards it.** No test asserts a gitignore rule. If the rule is
  deleted, the suite stays green and CI stays green. Only the preflight described at
  `.gitignore:461-468` — which does not exist (§Residual Risks in the assessment) — would catch it.
  This fix reduces reliance on that preflight; it does not replace it.
- **A deliberate `git add -f` still works.** `.gitignore` prevents accidents, not intent. The
  assessment's own words: on a public repo, publication "is not a mistake you can take back."
- **The `AGENTS.md` change is entangled with a 245-line pre-existing uncommitted rewrite.** It was
  listed as ` M` at session start, before any of this work. The verification confirms the *content*
  is present at `:20` and `:27`, but the commit boundary is unresolved and the rewrite's own accuracy
  was not reviewed here.
- **No in-game or runtime verification.** Not applicable to a gitignore rule, and none was expected.
  Note that the 816 green tests say nothing about whether the mod works in Stardew Valley — the
  visualization layer remains wired-but-dead.
- **Working-tree state remains entangled.** 13 modified files and several untracked directories,
  including `.gitignore`, `AGENTS.md`, `.specify/scripts/*`, and the
  `LivingRoots.Tests/CompostApplicationServiceTests.cs` doc fix from a separate bug slug.

## Recommendation

**Close the bug — verified.** The reported hazard is closed by direct measurement: all six
proprietary assemblies at the project's own CI `GamePath` location are now ignored via
`.gitignore:29`, `git add -A` stages nothing from that directory, and no exposure existed beforehand
(`git ls-files` → 0 tracked DLLs). The 816-test suite and the format gate both pass, as expected for a
configuration change. The one check that could not be re-run under this command's guardrail passed
during the fix and is disclosed above rather than claimed.

Do not read this as broader safety. It closes one specific path; it does not add the missing
staged-path preflight, and it does not defend against `git add -f`. The highest-value follow-up
remains implementing or rewording `.gitignore:461-468`, which would catch the general form of this
bug — any future file appearing at an unignored in-tree path.
