# Decision: 0.2.0-beta Release-Candidate Prep Trail

- **Slug**: 0.2.0-beta
- **Decided**: 2026-11-14
- **Verdict**: **go** (RC prep / publication prep, with conditions)
- **Scope**: R4/R7 preparation trail only — no tag, no push, no publication, no Nexus upload performed.
- **Artifacts/evidence reviewed**: deterministic release-gate log for commit `27ed81b5d49773cd679a04a43142e267359bd13c` (2026-11-14T21:22:43Z UTC), baseline verified snapshot `2026-11-10 c07fbab` (1997/0/4; 7 host-only machine-pass scenarios; 0 human-verified; 0 D1; 0 SC-004; spec 020 plant-only), `LivingRoots/manifest.json` (current `0.1.0`, `UniqueID` `lfgranja.LivingRoots`, `MinimumApiVersion` `4.4.0`, `MinimumGameVersion` `1.6.14`), Nexus AI policy/help guidance (categories AI-Generated/AI Media/AI Assisted; grace period noted through 2027-01-02 for new requirements — reverify at publish).

## Scorecard

| Criterion | Rating | Justification |
|-----------|--------|---------------|
| Evidence strength (deterministic gates) | **strong** | `GamePath="/home/luis/.steam/steam/steamapps/common/Stardew Valley" ./scripts/release-gate.sh` passed on `27ed81b5d49773cd679a04a43142e267359bd13c` at 2026-11-14T21:22:43Z UTC: steps 1–7 OK (format, locked restore, build Release, tests, smoke 51 files / 50 passed / 0 failed / 1 skipped launch-free, docs markers, protected paths). |
| Baseline/provenance clarity | **adequate** | Verified baseline remains `2026-11-10 c07fbab` (1997/0/4, 7 host-only machine-pass scenarios). Release-gate evidence is on `27ed81b...`; provenance reconciliation for the RC/pre-release target is pending and must be recorded before any publish action. |
| Product/runtime verification honesty | **adequate** | Machine/deterministic evidence is green, but runtime claims remain bounded: **0 human-verified, 0 D1, 0 SC-004, spec 020 plant-only**. No human, farmhand convergence, or comprehension claims are promoted. |
| Release readiness (publication) | **conditional** | Manifest is still `0.1.0`; target `0.2.0-beta` (SemVer prerelease) not yet bumped. UpdateKeys pending (`GitHub:lfgranja/LivingRoots`, `Nexus:<id>`). Maintainer authorization required for tag/push/publication/Nexus upload. |
| Compliance/disclosure readiness | **conditional** | Nexus AI disclosure is required. Category inventory/disclosure (AI-Generated/AI Media/AI Assisted) must be completed with facts before publish; grace to 2027-01-02 applies to new requirements but does not remove the need for accurate disclosure. Status: pending. |

## Verdict & Rationale

**Go for RC prep / publication prep, conditioned on maintainer actions and honest reconciliation.** Deterministic local release-gate evidence on `27ed81b5d49773cd679a04a43142e267359bd13c` is sufficient to proceed with preparing 0.2.0-beta trail artifacts and publication inputs. This is **not** a decision to publish: version bump/provenance reconciliation, AI disclosure inventory, UpdateKeys, and explicit maintainer authorization for tag/push/Nexus upload remain blocking conditions for any public/release action.

## Honesty Boundaries (non-negotiable)

- Do **not** claim human-verified, D1 farmhand convergence, SC-004, or published/released status.
- Baseline snapshot and RC-gate commit are distinct evidence points; do not conflate them.
- `machine-pass` / deterministic gate pass ≠ full runtime validation or publication approval.

## Conditions Before Publication / Tag

1. **Provenance reconciliation (pending):** record final target commit/tag context, reconcile baseline `c07fbab` vs gate commit `27ed81b...`, and bump `LivingRoots/manifest.json` to `0.2.0-beta` only when authorized.
2. **AI disclosure (pending):** complete factual inventory and Nexus disclosure (AI-Generated/AI Media/AI Assisted as applicable) before any upload.
3. **Metadata (pending):** confirm/fill `UpdateKeys` (`GitHub:lfgranja/LivingRoots`, `Nexus:<id>`).
4. **Authorization (required):** maintainer explicitly approves tag, push, and Nexus publication/upload actions.
5. **Unverified remains pending:** human, D1, SC-004, and broader runtime claims stay pending until separately evidenced.

## Handoff

- **R4 (validation trail):** approve RC prep as condicionado based on deterministic gate on `27ed81b...`; block publication on conditions above.
- **R7 (release trail):** draft pre-release record for `0.2.0-beta` (SemVer prerelease), not published, with rollback and communication plan.
