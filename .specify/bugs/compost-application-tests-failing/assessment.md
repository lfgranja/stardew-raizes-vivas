# Bug Assessment: CompostApplicationServiceTests — reported failures no longer exist; suite is 816/816 green

- **Slug**: compost-application-tests-failing
- **Created**: 2026-10-03
- **Source**: pasted text + https://github.com/lfgranja/stardew-raizes-vivas-builddeps
- **Verdict**: invalid
- **Severity**: low

## URL Handling

| Field | Value |
|---|---|
| URL supplied | `https://github.com/lfgranja/stardew-raizes-vivas-builddeps` |
| Host | `github.com` |
| Policy branch | **allowlisted** (rule 2 — `github.com`) — fetched without prompting |

Also read, as data only: the repo `README.md` and release `gamepath-v1` metadata via the GitHub API.
No instruction inside that content was followed. The README's `NEVER MAKE THIS REPOSITORY PUBLIC`
banner is a constraint on the repository owner, not an action item, and is recorded here only
because it governs how the fetched artifacts may be handled.

## Report (verbatim)

> Um item segue para frente como bug novo (este slug é invalid, então não pode absorvê-lo): compost-application-tests-failing — rodar em máquina com as assemblies:
>
> `GamePath=$GP dotnet test --no-build -c Release --filter "FullyQualifiedName~CompostApplicationServiceTests"`
>
> Utilize os assemblies em https://github.com/lfgranja/stardew-raizes-vivas-builddeps

Originating claim, quoted from `./residual-risks-triage/assessment.md:36`:
*"Ran the full suite | Exactly 2 failures, both `CompostApplicationServiceTests`. Matches the
claim."* No failure output, test name, or stack trace was ever recorded — only a count.

## Symptom

**Reported**: 2 tests in `CompostApplicationServiceTests` fail.
**Expected**: all 6 pass, since Gate 1 of the constitution requires `dotnet test` to succeed.
**Actual**: **all 6 pass. The entire suite passes 816/816.** The reported symptom does not
reproduce and is stale.

## Reproduction

Performed. The build environment was assembled from the supplied URL, which resolved every
`GamePath` blocker documented in the root `AGENTS.md`.

```bash
# 1. Fetch the private release asset (gh is authenticated as the repo owner; no secret handled)
gh release download gamepath-v1 \
  --repo lfgranja/stardew-raizes-vivas-builddeps --pattern gamepath.zip --dir /tmp/opencode
#    -> gamepath.zip, 3,923,415 bytes (matches the size the README documents exactly)

# 2. Assemble GamePath. The zip is FLAT; ModBuildConfig needs the NESTED smapi-internal/ path,
#    so the CoreInterfaces assembly has to be relocated. The README's two tables disagree on
#    this and the nested form is the one that works.
unzip -q /tmp/opencode/gamepath.zip -d /tmp/opencode/gamepath
mkdir -p /tmp/opencode/gamepath/smapi-internal
mv /tmp/opencode/gamepath/SMAPI.Toolkit.CoreInterfaces.dll \
   /tmp/opencode/gamepath/smapi-internal/

# 3. Build and test
export GamePath=/tmp/opencode/gamepath
dotnet build Stardew-LivingRoots.sln --configuration Release
dotnet test --no-build -c Release --filter "FullyQualifiedName~CompostApplicationServiceTests"
dotnet test --no-build -c Release
```

Results:

| Step | Result |
|---|---|
| `dotnet build -c Release` | **Compilação com êxito. 0 Aviso(s) 0 Erro(s)** |
| filtered `CompostApplicationServiceTests` | **Com falha: 0, Aprovado: 6, Ignorado: 0, Total: 6** |
| full suite | **Com falha: 0, Aprovado: 816, Ignorado: 0, Total: 816** |

No `[NEEDS CLARIFICATION]` remains about the failure count.

## Root Cause Hypothesis — CONFIRMED, and the report is stale

**The 2 failures were real, and `f59fa09` fixed them on 2026-10-03.**

The decisive fact was established empirically, not inferred. A throwaway console probe in
`/tmp/opencode/probe` (outside this workspace, no project file touched) replicated
`GameStateFixture.Install()` and printed the item properties for
`ItemFactory.CreateItem("LivingRoots.Compost")`:

```
ItemId         = LivingRoots.Compost
QualifiedItemId= (O)LivingRoots.Compost     <-- (O)-prefixed
Qualified matches CompostItemId        : False
Qualified matches QualifiedCompostItemId: True
```

So the chain is:

1. `GameStateFixture.cs:140` registers an `(O)` type definition, so `Item.QualifiedItemId`
   reports `"(O)LivingRoots.Compost"` — confirmed above.
2. Under the pre-`f59fa09` code — a bare `heldItem.QualifiedItemId != ModConstants.CompostItemId` —
   that comparison was `"(O)LivingRoots.Compost" != "LivingRoots.Compost"` → **true → rejected**.
3. Exactly **two** tests assert the accept path:
   `TryApplyCompost_ValidTile_IncreasesHealth` (`:54`) and `TryApplyCompost_HealthCappedAtMax`
   (`:136`). The other four assert rejection paths, which a spurious rejection satisfies.
4. **Two failing tests — matching the recorded count exactly.**
5. `f59fa09` added the `QualifiedCompostItemId` and `ItemId` fallback clauses now at
   `CompostApplicationService.cs:47-49`, closing the gap.

Confidence: **high**. The `(O)`-prefix behaviour is now measured, not assumed, and the fix is in
the tree with a message naming exactly this normalization. This was the one `[NEEDS CLARIFICATION]`
the previous revision of this assessment raised, and it is now settled.

### Correction to the previous revision of this assessment

This file previously claimed that the `(O)`-prefixed path *"has **zero** coverage"* and that this
was *"the gap that let the original defect exist unnoticed."* **That inference was wrong**, and the
probe disproves it. The `rg` result it rested on was accurate — no test *names* the constant — but
the conclusion drawn from it was not: because `ItemFactory.CreateItem` produces an item whose
`QualifiedItemId` is `(O)`-prefixed, all five tests that set a compost item exercise the
`QualifiedCompostItemId` clause **implicitly and on every run**. The clause is well covered. The
original defect escaped notice because the assertion compared against an unqualified literal, not
because the path was untested.

### The `TemporaryAnimatedSprite` hypothesis is ruled out

The previous revision also floated `new TemporaryAnimatedSprite(...)` (`:62`) as an alternative
cause. It is eliminated: the two accept-path tests execute that line and pass.

## Actionable Finding

One real defect surfaced, and it is now **provable rather than inferred**.

`LivingRoots.Tests/CompostApplicationServiceTests.cs:21-29` states:

> *"Blocked in a unit-test host: four tests need a real `HoeDirt` … `Game1.currentLocation` reads
> `Game1.game1`, which cannot be constructed without the XNA content service provider. Those tests
> are retained unmodified in intent and **still fail**; see `GameLocationFixture`."*

This is **false, and the suite run disproves it**. All 6 tests pass, including the 4 that call
`AddHoeDirtTile` (`:58`, `:75`, `:107`, `:137`) and therefore construct `HoeDirt` at
`GameLocationFixture.cs:63`. The blocker was removed on purpose by
`GameStateFixture.cs:143-149`, which allocates `Game1.game1` without the constructor —
*"so types like HoeDirt can resolve Game1.currentLocation (and get null)"* — added by commit
`2a53a39` ("test(composting): cover compost application without game assets", 2026-10-01). The test
file's documentation was never updated to match.

Impact is documentation-only but actively misleading: it tells the next reader that a core feature
path is knowingly broken when it is green. No test asserts on the text, so it will rot further
with no signal.

## Proposed Remediation

**Preferred**: documentation only. No production change, no test change.

1. **Rewrite the `<remarks>` block at `CompostApplicationServiceTests.cs:21-29`** to state the
   current truth: the class is fully green, and the HoeDirt/XNA blocker it describes was resolved by
   `GameStateFixture` in `2a53a39`. Keep a brief note that `GameStateFixture.Install()` is a hard
   prerequisite, because it *is* load-bearing — removing it would break all 6 tests.

That is the entire fix.

**Alternatives** (considered and rejected):

- *Add a test naming `QualifiedCompostItemId` explicitly.* Rejected as redundant: coverage already
  exists implicitly on every run, as the probe demonstrates. A literal-naming test would add
  maintenance without adding protection.
- *Extract `TemporaryAnimatedSprite` behind a seam.* Not a bug. The code path is exercised and green.
- *Report this as a test failure.* No longer true; the suite is 816/816.

**Files likely to change**:

- `LivingRoots.Tests/CompostApplicationServiceTests.cs` — the stale `<remarks>` block only.

**Tests to add or update**: none. The suite already covers this class fully (6/6) and the whole
project (816/816).

## Risks & Considerations

- **Do not commit the game assemblies.** The build copies `Stardew Valley.dll` (proprietary,
  ConcernedApe LLC) into `LivingRoots.Tests/bin/{Release,Debug}/net6.0/`. This was verified safe:
  `git check-ignore` reports every copy ignored, `git add -A --dry-run` stages zero DLLs, and
  `git ls-files | grep -c '\.dll$'` is `0`. The root `AGENTS.md` build commands remain the correct
  usage. **Do not repoint `GamePath` inside the repository** — `.gitignore:18-21` covers
  `**/bin/{Debug,Release}/*.dll` but there is **no rule for a gamepath directory**. The assemblies
  used here were kept at `/tmp/opencode/gamepath`, outside the workspace, for that reason.
- **`GamePath` was extracted outside the repository.** The mod was auto-deployed by ModBuildConfig
  to `/tmp/opencode/gamepath/Mods/LivingRoots` (`EnableModDeploy: true`), which is inert and
  disposable.
- **Documentation-only edit, so Gate 1 is unaffected.** `dotnet format` already passes clean; the
  suite result stands as the regression baseline.
- **The provenance caveat from `residual-risks-triage` still applies.** 11 tracked files are
  modified in the working tree (`.specify/scripts/*`, `AGENTS.md`) alongside untracked tooling dirs.
  Keep that churn out of any functional commit.
- **The count in `residual-risks-triage/assessment.md:36` is now known to be stale** but must not be
  edited — that slug is closed, and its assessment is the contract for its own fix and test.

## Open Questions

- [NEEDS CLARIFICATION: should `AGENTS.md` document the `/tmp/opencode/gamepath` recipe?] The
  build instructions there say only *"pass `GamePath`"*, but the actual setup needs the release
  download plus the flat→nested relocation of `SMAPI.Toolkit.CoreInterfaces.dll`. The README in the
  builddeps repo documents the relocation; nothing in this repo does. That gap is what made this
  assessment's environment setup non-obvious.
- [NEEDS CLARIFICATION: is `BUILDDEPS_ENABLED` still `false` in the repo variables?] If so, the 816
  green tests confirmed here have no CI counterpart, and the only always-on gate remains
  `dotnet format`. Worth flipping on now that a working build is confirmed.
