# Bug Fix: CompostApplicationServiceTests — stale "still fail" documentation corrected

- **Slug**: compost-application-tests-failing
- **Fixed**: 2026-10-03
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

The reported symptom — 2 failing tests in `CompostApplicationServiceTests` — does not exist and was
already fixed by commit `f59fa09`; the suite is 816/816 green. What remained was the defect the
assessment isolated as its Actionable Finding: the test class's XML documentation asserted that
"four tests … still fail" because of a `HoeDirt`/XNA blocker that `GameStateFixture` had already
removed. That claim is now replaced with the verified truth.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `LivingRoots.Tests/CompostApplicationServiceTests.cs` | modified (docs only) | Replaced the false `<para>` at the old `:21-29` with two accurate paragraphs. 13 insertions, 7 deletions. |
| `.specify/bugs/compost-application-tests-failing/fix.md` | added | This report. |

No production file, no test method, and no assertion was touched. Verified mechanically:

```
$ git diff -U0 LivingRoots.Tests/CompostApplicationServiceTests.cs \
    | grep -E "^[+-]" | grep -vE "^(\+\+\+|---)" \
    | grep -vE "^[+-]\s*///" | grep -vE "^[+-]\s*$"
NONE — every changed line is an XML doc comment
```

## Diff Highlights

The false claim, removed:

```
-/// <b>Blocked in a unit-test host:</b> four tests need a real <c>HoeDirt</c> in the target
-/// location's terrain features, … <c>Game1.currentLocation</c> reads <c>Game1.game1</c>, which
-/// cannot be constructed without the XNA content service provider. Those tests are retained
-/// unmodified in intent and still fail; see <see cref="GameLocationFixture"/>.
```

Replaced with the prerequisite, and the reason it is load-bearing:

```
+/// <b>Prerequisite:</b> <see cref="GameStateFixture"/> is installed by this class's constructor and
+/// is load-bearing for all six tests. Four of them need a real <c>HoeDirt</c>, because the service
+/// checks the tile type before it looks at soil health or the held item, and
+/// <c>HoeDirt</c>'s constructors all funnel into <c>initialize(Game1.currentLocation)</c>.
+/// <c>Game1.currentLocation</c> reads <c>Game1.game1</c>, whose real constructor dereferences the
+/// XNA content service provider that only MonoGame populates while booting. The fixture allocates
+/// that static without the constructor, which is what lets <c>HoeDirt</c> resolve a (null)
+/// location. Removing it breaks every test here.
```

Plus a second paragraph recording the measured `(O)`-prefix behaviour, which is the fact that made
the original defect possible:

```
+/// The same fixture registers the <c>(O)</c> item type definition, so an item created by
+/// <see cref="ItemFactory"/> reports <c>QualifiedItemId</c> as <c>"(O)LivingRoots.Compost"</c>
+/// rather than the bare id it assigns. The service's held-item check accepts both forms.
```

## Tests Added or Updated

None, per the assessment — *"The suite already covers this class fully (6/6) and the whole project
(816/816)."* No coverage gap existed: the `(O)`-prefixed clause is exercised implicitly by all five
tests that set a compost item, because `ItemFactory.CreateItem` yields an `(O)`-prefixed
`QualifiedItemId`. The earlier revision of the assessment wrongly claimed otherwise; that claim is
retracted there and was not carried into this fix.

## Local Verification

All commands run with `GamePath=/tmp/opencode/gamepath`, assembled from the builddeps release per
the assessment.

| Command | Result |
|---|---|
| `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | **pass** — exit 0, no output |
| `dotnet build Stardew-LivingRoots.sln -c Release` | **pass** — `0 Aviso(s) 0 Erro(s)` |
| `dotnet test --no-build -c Release --filter "FullyQualifiedName~CompostApplicationServiceTests"` | **pass** — `Com falha: 0, Aprovado: 6, Total: 6` |
| `dotnet test --no-build -c Release` | **pass** — `Com falha: 0, Aprovado: 816, Total: 816` |
| diff is comment-only | **pass** — no code line changed |

Gate 1 of the constitution (`build` + `test` + `format` no changes) passes in full.

Manual check: re-read the edited `<remarks>` block at `:15-36` and confirmed the pre-existing
paragraph about `NoOpSoundsHelperStub` (`:16-20`) was preserved verbatim — it was accurate and out
of scope.

## Deviations from Assessment

1. **The fix was applied despite an `invalid` verdict, on explicit user instruction.** The verdict
   covers the *reported symptom* (the 2 stale test failures), which required no change. The
   Actionable Finding was a separately recorded, non-gated documentation defect with a concrete
   remediation. Step 1 of the command says to stop on `invalid`; the user was asked and chose to
   apply the documentation fix. Scope did not expand beyond what the assessment's preferred
   remediation specified.

2. **A third paragraph was added beyond the assessment's brief.** The assessment asked to state that
   the class is green and that `GameStateFixture` is a hard prerequisite. It also said *"keep a
   brief note that `GameStateFixture.Install()` is a hard prerequisite"* — but not to document the
   `(O)`-prefix behaviour. That paragraph was added because it is the measured fact that explains
   why `f59fa09` was needed, and a future maintainer editing the item-id check is exactly the reader
   who needs it. Two sentences, same file, no new dependency.

## Follow-ups

- **`AGENTS.md` does not document the `GamePath` recipe.** It says only *"pass `GamePath`"*, which is
  not sufficient: the build needs the `gamepath.zip` release download **and** the relocation of
  `SMAPI.Toolkit.CoreInterfaces.dll` into a nested `smapi-internal/`. The builddeps README documents
  the relocation; nothing in this repo does, and that gap is what made the setup non-obvious during
  this assessment. This was the assessment's first `[NEEDS CLARIFICATION]` and it remains open.
- **Consider setting `BUILDDEPS_ENABLED=true`.** The 816 green tests confirmed here still have no CI
  counterpart; the only always-on gate remains `dotnet format`, which is whitespace-only. A working
  build is now confirmed, so the condition that blocked it is gone. Second open
  `[NEEDS CLARIFICATION]` from the assessment.
- **Do not repoint `GamePath` inside the repository.** `.gitignore:18-21` covers
  `**/bin/{Debug,Release}/*.dll`, but there is no rule for a gamepath directory. The assemblies used
  here were kept at `/tmp/opencode/gamepath`, outside the workspace. Verified safe even after the
  build copied them in: `git check-ignore` reports every copy ignored, `git add -A --dry-run` stages
  zero DLLs, `git ls-files | grep -c '\.dll$'` is `0`.
- **Keep the working-tree churn out of the functional commit.** 11 tracked files remain modified
  (`.specify/scripts/*`, `AGENTS.md`) alongside untracked tooling directories. The change in this
  fix should be committed on its own.
- **`LivingRoots.Tests/bin/{Debug,Release}/net6.0/` now contains proprietary Stardew Valley
  binaries** copied there by ModBuildConfig. They are git-ignored and were not committed. Consider
  `dotnet clean` if the local build should be left pristine.

## Next

`/speckit.bug.test slug=compost-application-tests-failing` — Gate 1 already passes, so the test
stage should confirm the suite result and record that the stale claim is gone. Note that the
verification cannot re-confirm the *negative* claim in the old documentation without a reader
re-deriving it; the passing 6/6 is the evidence that retires it.
