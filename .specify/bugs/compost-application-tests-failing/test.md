# Bug Verification: CompostApplicationServiceTests — stale "still fail" documentation retired

- **Slug**: compost-application-tests-failing
- **Tested**: 2026-10-03
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

The reported symptom — 2 failing tests in `CompostApplicationServiceTests` — does not reproduce:
the class passes 6/6 and the whole suite passes 816/816. The documentation defect the fix targeted
is confirmed gone, and the replacement claim is confirmed **true by negative control** rather than
merely plausible. No regressions; Gate 1 passes in full.

This is the first slug in `.specify/bugs/` whose verification exercises a negative control rather
than only confirming that a green suite is green.

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) — build | `dotnet build Stardew-LivingRoots.sln -c Release` | **pass** | `0 Aviso(s) 0 Erro(s)` |
| Reproduction (post-fix) — the class reported failing | `dotnet test --no-build -c Release --filter "FullyQualifiedName~CompostApplicationServiceTests"` | **pass** | `Com falha: 0, Aprovado: 6, Total: 6` |
| Reproduction (post-fix) — full suite | `dotnet test --no-build -c Release` | **pass** | `Com falha: 0, Aprovado: 816, Total: 816` |
| **Fix target: stale claim removed** | `rg "still fail\|Blocked in a unit-test host\|cannot be constructed without the XNA" CompostApplicationServiceTests.cs` | **pass** | Zero matches. The claim the assessment disproved is gone. |
| **Fix target: replacement present** | `rg "Prerequisite:\|load-bearing for all six tests\|Removing it breaks every test here"` | **pass** | Present at `:22,23,29`. |
| **Negative control — is the new claim true?** | Probe in `/tmp/opencode/probe`: call `new HoeDirt()` while deliberately **not** allocating `Game1.game1` | **pass** | Threw `System.NullReferenceException`. The doc's *"Removing it breaks every test here"* is factually correct. |
| New / updated tests | — | **n/a** | The fix added none, per the assessment: no coverage gap existed. `fix.md` records this and the retraction of the earlier "zero coverage" claim. |
| Regression suite | `dotnet test --no-build -c Release` | **pass** | 816/816, unchanged from the pre-fix baseline. |
| Lint / type-check | `dotnet format Stardew-LivingRoots.sln --verify-no-changes` | **pass** | Exit 0, no output. |
| Fix stayed documentation-only | `git diff -U0 … \| grep -vE "^[+-]\s*///"` | **pass** | Zero non-comment lines changed. |
| Proprietary binaries not staged | `git ls-files \| grep -c '\.dll$'` | **pass** | `0`. Game assemblies remain git-ignored. |

## Output Excerpts

Reproduction, all three steps from the assessment, re-run post-fix:

```
--- build ---
Compilação com êxito.
    0 Aviso(s)
    0 Erro(s)
--- filtered ---
Aprovado!  – Com falha: 0, Aprovado: 6, Ignorado: 0, Total: 6
--- full suite ---
Aprovado!  – Com falha: 0, Aprovado: 816, Ignorado: 0, Total: 816
```

Fix target retired:

```
$ rg "still fail|Blocked in a unit-test host|cannot be constructed without the XNA" \
     LivingRoots.Tests/CompostApplicationServiceTests.cs
PASS: no stale claim remains in the file
```

Negative control — omitting `GameStateFixture`'s key step breaks `HoeDirt`, which is precisely what
the replacement text now documents:

```
game1 allocated? False
RESULT: new HoeDirt() THREW without game1 -> doc claim CONFIRMED
        System.NullReferenceException
```

## Residual Risks

- **The documentation fix has no automated guard, by nature.** No test asserts on comment text, so
  this class of rot can recur silently. The mitigation is indirect but real, and now demonstrated:
  if someone removes `GameStateFixture.Install()`, the suite fails loudly with a
  `NullReferenceException` — the precondition the new text documents is enforced by the tests
  themselves, not by the prose.
- **`dotnet format` is a whitespace gate only.** Per project `AGENTS.md` it does not enforce
  `csharp_style_expression_bodied_methods = false`. Its clean pass is weak evidence.
- **No runtime or in-game verification.** The mod was never launched. Everything here is
  build-and-test evidence. Static and suite checks cannot detect a wiring defect — the exact class
  of bug that still affects the visualization layer, where `Initialize()` is never called and
  `SetTileHealthData` / `TriggerHoeFeedback` have no call site. **This verification says nothing
  about whether the mod works in Stardew Valley.**
- **CI cannot corroborate.** `build_and_test.yml:53` is gated behind `vars.BUILDDEPS_ENABLED == 'true'`,
  so these 816 green tests have no automated counterpart. The 816/816 result exists only because the
  build environment was assembled manually from the builddeps release.
- **Two `[NEEDS CLARIFICATION]` items from the assessment remain open**, both carried from `fix.md`:
  `AGENTS.md` still does not document the `GamePath` recipe (release download **plus** the nested
  `smapi-internal/` relocation), and `BUILDDEPS_ENABLED` is presumably still `false`. Neither affects
  this fix; both are prerequisites for making this verification repeatable by anyone else.
- **The game assemblies now sit in `LivingRoots.Tests/bin/{Debug,Release}/net6.0/`.** Verified
  git-ignored and untracked, but they are proprietary Stardew Valley binaries (ConcernedApe LLC) on
  local disk. Run `dotnet clean` if that should not persist.
- **The stale count lives on in a closed slug.** `residual-risks-triage/assessment.md:36` still
  records "Exactly 2 failures". That is correct as a historical record and must not be edited — its
  slug is closed — but a future reader searching for "2 failures" will find it.

## Recommendation

**Close the bug — verified end-to-end, with a negative control.** The reported symptom is absent
(6/6, 816/816), the stale documentation the assessment identified is confirmed removed by direct
search, and the replacement text is confirmed *true* rather than merely plausible: deliberately
omitting `GameStateFixture`'s allocation makes `new HoeDirt()` throw
`NullReferenceException`, which is exactly the prerequisite the new text claims is load-bearing. No
regressions, Gate 1 green, and the change provably touched no executable line.

The one thing this verification deliberately does **not** claim: that the mod functions in-game.
That is Gate 3 of the constitution, it requires a human with a Stardew Valley installation, and
nothing in this session touched it. The visualization layer remains wired-but-dead, which unit tests
by construction cannot detect.
