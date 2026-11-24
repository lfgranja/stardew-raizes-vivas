# Bug Assessment: Triage of residual risks — not a defect report

- **Slug**: residual-risks-triage
- **Created**: 2026-09-30
- **Source**: pasted text
- **Verdict**: invalid
- **Severity**: low

> Note: this is a triage of the report as submitted. The command's verdict vocabulary does not
> include `partial`; of the permitted values, `invalid` is the correct one — see §Verdict Reasoning.
> The single actionable item found during triage is recorded separately in §Actionable Finding.

## Report (verbatim)

> Riscos residuais (no relatório)
> 2 falhas restantes são de asset (A001), não regressão · cue Ship não verificável · inventário cheio
> consome o bin (preservado deliberadamente) · co-op sem cobertura · proveniência da mudança
> concorrente continua inexplicada — agora com comportamento verificado, mas o fato de ter quebrado
> ICompostingBinService para consumidores externos deve ser confirmado antes do commit.

This is the residual-risk list from the verification report of
[`compostingbin-collectcompost-untestable`](./) — not a defect report. Four of the five items declare
themselves non-defects in their own text. Each was checked against the codebase before judging.

## Symptom

No symptom. The submission is a set of known limitations and accepted trade-offs, three of which are
phrased as explicit non-issues ("não regressão", "não verificável", "preservado deliberadamente").

## Reproduction

Not applicable — there is no failing behaviour. Each item was instead verified against the codebase:

| Item | Verification performed | Finding |
|------|------------------------|---------|
| 2 failures are asset-scoped | Ran the full suite | Exactly 2 failures, both `CompostApplicationServiceTests`. Matches the claim. |
| `Ship` cue unverifiable | `grep '"Ship"'` across src + tests | 2 call sites (`CompostingBinService.cs:91,126`), 0 test assertions. Matches the claim. |
| Full inventory still consumes bin | Read `CompostingBinService.cs:75-84` | `TryAddItem` result is logged at `Warn`; bin is emptied unconditionally. Matches the claim, and is pinned by a passing test. |
| No co-op coverage | `grep` for `multiplayer`/`farms`/`getPlayers` across `LivingRoots/` | Zero matches. Co-op is entirely unexercised. Matches the claim. |
| API broke external consumers | Checked mod version, tags, releases, all call sites | **Premise is false.** See §Root Cause. |

## Suspected Code Paths

- `LivingRoots/Services/CompostingBinService.cs:91,126` — the two `Game1.playSound("Ship")` sites.
  Reachable only through the `Game1.sounds` static; nothing asserts they fire.
- `LivingRoots/Services/CompostingBinService.cs:75-84` — refused-grant path. Logs a warning, then
  consumes the bin regardless.
- `LivingRoots/Services/PlayerInventory.cs:16` — `Game1.player.addItemToInventoryBool`. In SMAPI
  multiplayer `Game1.player` is the local player, so the grant reaches whoever collected. Behaviour is
  correct; no test covers it.
- `LivingRoots/Domain/Interfaces/ICompostingBinService.cs:26` — the changed signature. This is the item
  whose stated impact does not hold up.

## Root Cause Hypothesis

This is not a defect. It is the residual-risk section of a verification report, re-submitted as a bug
report. Three items are explicit non-issues, one is a coverage gap, and the fifth rests on a premise
that the evidence contradicts.

That fifth item claims the signature change "quebrou `ICompostingBinService` para consumidores
externos". Checked directly:

| Check | Result |
|-------|--------|
| `manifest.json` Version | `0.0.1` |
| `git tag` | none |
| `gh release list` | empty |
| Production consumers of `CollectCompost` | 1 — `ModController.cs:870`, already updated |
| Test consumers | all updated; suite compiles and passes |

The mod has never been released, so there is no external consumer to break. The blast radius is zero.
Confidence: **high** — this is a fact about the repository, not an inference.

## Verdict Reasoning

The report contains no failing behaviour. Assigning `valid` would record a phantom defect and spend a
`/speckit.bug.fix` cycle on it. Assigning `likely valid, needs reproduction` would imply something
might still be wrong with the code, which the verification and the two negative controls already ruled
out. `invalid / not a bug` is the accurate label, and the per-item table in §Reproduction records what
was checked so the reasoning is auditable rather than dismissive.

Severity is `low`: no user-visible malfunction, no data risk, no regression. The one item with a
defensible claim is a process concern, not a code defect.

## Actionable Finding

While triaging, one **genuine latent defect** surfaced that was not in the submitted list.
`LivingRoots/ModConstants.cs:63-65` declares item ids in two different conventions:

```csharp
public const string CompostItemId         = "LivingRoots.Compost";        // dot
public const string CompostingBinItemId   = "LivingRoots.CompostingBin";  // dot
public const string CompostingBinRecipeId = "LivingRoots_CompostingBin";  // underscore
```

Consumer counts today: `CompostItemId` 8, `CompostingBinItemId` 0, `CompostingBinRecipeId` 0.

The hazard is that `CompostApplicationService.cs:46` compares `heldItem.QualifiedItemId` against
`ModConstants.CompostItemId` with a bare `!=`. Per
`specs/003-composting-tests/briefings/compost-item.md`, the `Data/Objects` key must match the constant
exactly or that comparison fails **silently — no exception, no log**. So when task `A001` implements
`content.json`, any constant left on the wrong convention becomes a compost item that can be produced
but never applied.

This is latent, not active: no consumer reads the two unused constants, and `content.json` does not
exist yet. It is also not caused by the fix under verification. It deserves its own assessment once
`A001` is scheduled, since that is the moment it becomes capable of biting.

## Proposed Remediation

**Preferred**: no code change. Close this as not-a-defect and carry the two live items into existing
tracking.

1. Reconcile the `ModConstants` id convention to a single form (`LivingRoots.` — the form
   `CompostItemId` already uses and the form 8 call sites depend on), or delete
   `CompostingBinRecipeId` and `CompostingBinItemId` as unused (Principle V). Gate this on `A001` so the
   convention is settled before `content.json` is authored against it.
2. Commit the verified `CollectCompost` change as its own commit, separate from the other ~90 unrelated
   working-tree entries. That removes the provenance ambiguity without needing to reconstruct who
   authored it — provenance is only load-bearing while the change is uncommitted and mixed in.

**Alternatives**:

- *If you want the `Ship` cue assertable*, that is a new `ISoundPlayer` seam — a deliberate reversal of
  a decision you already made, and it should be its own assessment, not a residue of this one.
- *If you want co-op coverage*, that is new test work against a feature that has no implementation.
  There is no multiplayer code anywhere in `LivingRoots/`; testing it would mean first writing it.

**Files likely to change** (for item 1 only, when `A001` is scheduled):

- `LivingRoots/ModConstants.cs` — the two unused constants

**Tests to add or update**: none for this assessment. The `CollectCompost_WhenInventoryRefuses_
StillConsumesBin` test already pins item 3 as intentional behaviour.

## Risks & Considerations

- **Declaring this `invalid` could look like dismissing a real problem.** The mitigation is the itemized
  evidence table in §Reproduction — every claim was checked, and the one whose premise failed is
  documented rather than ignored.
- **The `ModConstants` inconsistency is the item most likely to cause future harm**, precisely because
  it fails silently. It should not be lost because this assessment is filed as `invalid`.
- **Do not batch-commit the working tree.** 96 entries are uncommitted, spanning an unreleased API
  change and unrelated in-flight work. Whatever provenance concern motivated item 5 disappears once
  the change is committed on its own.

## Open Questions

- **[NEEDS CLARIFICATION: should `CompostingBinItemId` and `CompostingBinRecipeId` be reconciled or
  deleted?** Both have zero consumers. Deleting is the YAGNI-consistent option; reconciling preserves
  intent for the placeable composter described in
  `specs/003-composting-tests/briefings/composting-bin-item.md`.
- **[NEEDS CLARIFICATION: is full-inventory-loses-compost intended game design, or an oversight?]** It
  was preserved deliberately and is now pinned by a test, so changing it later requires updating that
  test. Worth confirming it matches player expectation rather than merely matching current code.
- **[NEEDS CLARIFICATION: do you want the `ModConstants` inconsistency assessed separately now, or
  deferred to `A001`?** It is latent today and becomes live at `A001`.
- **[NEEDS CLARIFICATION: was the concurrent authoring process expected?** If another agent or tool is
  expected to edit this working tree, the provenance concern is a workflow question rather than a
  one-off, and worth addressing at the process level.