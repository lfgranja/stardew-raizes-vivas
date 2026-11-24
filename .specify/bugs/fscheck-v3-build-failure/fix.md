# Bug Fix: FsCheck 3.6.0 broke the test project build

- **Slug**: fscheck-v3-build-failure
- **Fixed**: 2026-11-01T01:26:15Z
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

Pinned `FsCheck` and `FsCheck.Xunit` back to `2.16.6`, regenerated `LivingRoots.Tests/packages.lock.json`, and added semver-major dependabot ignores so the same breaking bump is not proposed again. Local reproduction on `origin/dev` failed with the reported CS0103/CS0305/CS1061/CS0619 errors; after the fix the test project builds and the full suite passes.

## Changes

| File | Change | Notes |
|------|--------|-------|
| `LivingRoots.Tests/LivingRoots.Tests.csproj` | modified | `FsCheck` and `FsCheck.Xunit` 3.6.0 → 2.16.6 |
| `LivingRoots.Tests/packages.lock.json` | modified | Regenerated with `dotnet restore --force-evaluate`; byte-identical to PR #537's lock (`git diff eb75616 -- LivingRoots.Tests/packages.lock.json` is empty) |
| `.github/dependabot.yml` | modified | `ignore` rules for semver-major `FsCheck`/`FsCheck.Xunit` under `/LivingRoots.Tests`, with a comment explaining the 2.x API dependency |
| `scripts/quality-gate/tests/test_dependency_pins.py` | added | Policy tests for the major-version pin and the dependabot ignore rules |

## Diff Highlights

```xml
<!-- LivingRoots.Tests/LivingRoots.Tests.csproj -->
-    <PackageReference Include="FsCheck" Version="3.6.0" />
-    <PackageReference Include="FsCheck.Xunit" Version="3.6.0" />
+    <PackageReference Include="FsCheck" Version="2.16.6" />
+    <PackageReference Include="FsCheck.Xunit" Version="2.16.6" />
```

```yaml
# .github/dependabot.yml
  - package-ecosystem: "nuget"
    directory: "/LivingRoots.Tests"
    schedule:
      interval: "weekly"
    # The property-based suite is written against the FsCheck 2.x C# API.
    ignore:
      - dependency-name: "FsCheck"
        update-types:
          - "version-update:semver-major"
      - dependency-name: "FsCheck.Xunit"
        update-types:
          - "version-update:semver-major"
```

```python
# scripts/quality-gate/tests/test_dependency_pins.py
def test_fscheck_major_version_is_pinned_to_2():
    references = package_references()
    for package in FSCHECK_PACKAGES:
        version = references.get(package)
        assert version is not None, f"{package} must stay referenced by the test project"
        assert version.split(".", maxsplit=1)[0] == "2"
```

## Tests Added or Updated

- `scripts/quality-gate/tests/test_dependency_pins.py::test_fscheck_major_version_is_pinned_to_2` — fails if `FsCheck` or `FsCheck.Xunit` moves off the 2.x line before the suite is migrated.
- `scripts/quality-gate/tests/test_dependency_pins.py::test_dependabot_ignores_fscheck_major_updates` — fails if the `/LivingRoots.Tests` dependabot entry drops the semver-major ignores.
- No product tests were changed. The existing `dotnet build`/`dotnet test` path is the real compile regression check.

## Local Verification

- Reproduced the bug on a clean `origin/dev` worktree at `e819a34`:
  `dotnet build LivingRoots.Tests/LivingRoots.Tests.csproj -c Release`
  → fails with `CS0103` (`Prop`/`Arb`), `CS0305` (`Gen<T>`), `CS1061` (`Gen<float>.Two`), and `CS0619` (`Record.Exception(Func<Task>)`).
- Restored the downgraded packages:
  `dotnet restore LivingRoots.Tests/LivingRoots.Tests.csproj --force-evaluate` → success; lock contains `FsCheck`/`FsCheck.Xunit` `2.16.6`.
- Verified the generated lock is identical to PR #537's:
  `git diff eb75616 -- LivingRoots.Tests/packages.lock.json` → empty.
- Verified locked-mode restore:
  `dotnet restore LivingRoots.Tests/LivingRoots.Tests.csproj --locked-mode` → success.
- Built the fixed tree:
  `dotnet build LivingRoots.Tests/LivingRoots.Tests.csproj -c Release --no-restore` → **0 errors**, 2 pre-existing `System.Collections.Immutable` net6.0 warnings.
- Ran the full test suite:
  `dotnet test LivingRoots.Tests/LivingRoots.Tests.csproj -c Release --no-build --verbosity minimal` → **1443 passed, 0 failed, 4 skipped**.
- Ran the new policy tests:
  `PYTHONPATH=scripts/quality-gate /tmp/opencode/qg-venv/bin/python -m pytest scripts/quality-gate/tests/test_dependency_pins.py -q` → `2 passed`.
- Ran the full quality-gate suite:
  `PYTHONPATH=scripts/quality-gate /tmp/opencode/qg-venv/bin/python -m pytest scripts/quality-gate/tests -q` → `169 passed, 4 failed`; the four failures are the same pre-existing ones that reproduce on `origin/dev`.
- Manual check: the `CS0619` at `VisualizationServiceTests.cs:142` disappears once FsCheck compiles, confirming it was a cascade from the unresolved `Prop`/`Arb` symbols rather than a second bug.

## Deviations from Assessment

1. **Re-applied rather than merged PR #537.** The assessment allowed cherry-picking #537 or merging it. The branch was cut fresh from `origin/dev` and the same two changes were applied directly, then the lock was regenerated locally. The resulting lock is identical to #537's, so the two PRs will merge as a no-op on the shared files.
2. **Local reproduction was possible after all.** The assessment said the proprietary game assemblies would limit verification to CI. The host has a usable Stardew Valley install, so the failure was reproduced and the full suite was run locally. This strengthens the evidence; no remediation changed.
3. **The `Record.Exception` diagnostic was a cascade.** The assessment flagged it as possibly independent. The local repro shows it only appears alongside the FsCheck errors and disappears after the downgrade, so no separate change was needed.

## Follow-ups

- Tracked in issue #547.
- Migrate the property suite to FsCheck 3 (`FsCheck.Fluent.Prop`, `ArbMap`, `PropertyAttribute.Arbitrary`) as a separate change, then remove the pin and the dependabot ignores.
- Make `Quality Gate / build-analysis` or `Build and Test` a required status check so a red build cannot merge again; PR #528 was merged with the same failing job.
- PR #537 carries the same downgrade plus unrelated `.gitignore` and bug-doc changes. After this branch merges, close or rebase #537 so it does not duplicate the dependency change.
- The remaining red `Quality Gate / gate` fronts are separate: jscpd (PR #546), coverage exclusions (PR #543), and formatting (PR #544).
