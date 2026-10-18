**Question:** Should the deferred rollback verification gate (`SC-008` from spec 004 — `CompostingBinService` manual play-through + `StillConsumesBin` automated test) be treated as an absolute prerequisite that blocks all feature 005 yield-activation (`FR-001`, `FR-002`, `FR-003`), allowing concurrent work on growth/harvest mechanism design only after it passes? (FR-005 / Dependencies / SC-008 / Q5)

Why it matters: This is the dominant risk (`problem.md` #1; `FR-009` line 82). Without `SC-008` green, any `Crop.phaseDays` touch (`FR-001`) or harvest-quality/extra-yield activation (`FR-002`/`FR-003`) activates unverified code — the hidden duplication bug (`CompostingBinService.cs` partial-delivery loop at `outputCount > 1` with no rollback) could corrupt farm state.

**Recommended:** Option A — `SC-008` must pass first; aligns with the explicit verification gate (`Dependencies` 136-137; `FR-005` 78; `Open Questions` 156), protects against the documented duplication bug, and preserves the manual observation requirement (`FR-009` 82; `SC-008` 114) that no automated test can replace. Concurrent design work on hook mechanism (`Q1` Option A) is permitted; activation is not.

| Option | Description |
|--------|-------------|
| A | Treat `SC-008` as absolute blocker: no `FR-002`/`FR-003` activation until rollback verified; design work on `Crop.newDay` / `Crop.harvest` hooks can proceed in parallel, but no yield-increase activation. Matches `Dependencies` 136-137 and `FR-005` exactly. |
| B | Relax `SC-008` to concurrent prerequisite: activate yield features as `SC-008` verification proceeds in parallel; accept duplication risk during verification window. Reduces schedule dependency but violates `FR-005` safety gate. |
| C | Defer `SC-008` to post-plan verification only: design/spec complete without rollback confirmation; rely solely on automated tests (`StillConsumesBin`) and skip manual `GamePath` verification. Contradicts `FR-009` (manual play-through required) and `SC-008` (manual is the dominant verification path). |

You can reply with the option letter (e.g., "A"), accept the recommendation by saying "yes" or "recommended", or provide your own short answer (<=5 words).
