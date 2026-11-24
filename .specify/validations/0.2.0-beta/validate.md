# Validation — 0.2.0-beta (RC Prep / Publication Prep)

- **Slug**: 0.2.0-beta
- **Trail**: `.specify/assessments/0.2.0-beta/decision.md` (R4/R7 release-candidate prep)
- **Target**: Living Roots `0.2.0-beta` (SemVer prerelease) — preparation only
- **Gate**: R4 (validate before release)
- **Validated**: 2026-11-14
- **Veredito**: **APROVADO CONDICIONADO** — RC prep validated by deterministic gate on `27ed81b...`; publication blocked on maintainer actions/provenance/disclosure.

## Evidência

- **Deterministic release gate: PASSED**
  - Commit: `27ed81b5d49773cd679a04a43142e267359bd13c`
  - Timestamp: `2026-11-14T21:22:43Z` (UTC, from log)
  - Command: `GamePath="/home/luis/.steam/steam/steamapps/common/Stardew Valley" ./scripts/release-gate.sh`
  - Steps 1–7 OK: format, locked restore, build Release, tests, smoke **51 files / 50 passed / 0 failed / 1 skipped (launch-free)**, docs markers, protected paths.
- **Baseline verified snapshot (unchanged reference):** `2026-11-10 c07fbab`, 1997/0/4, **7 host-only machine-pass scenarios**, **0 human-verified**, **0 D1**, **0 SC-004**, spec 020 plant-only.
- **Manifest/provenance state:** `LivingRoots/manifest.json` currently `0.1.0` (`lfgranja.LivingRoots`, `MinimumApiVersion>=4.4.0`, `MinimumGameVersion>=1.6.14`); target `0.2.0-beta` not yet bumped — provenance reconciliation pending.
- **Release/publication actions:** no tag, no push, no publication, no Nexus upload performed (maintainer authorization required).

## Honesty Boundaries

- This validation covers deterministic RC-prep evidence on `27ed81b...`; it does **not** promote human, D1, SC-004, or published claims.
- Baseline `c07fbab` and gate commit `27ed81b...` are separate evidence points; provenance must be reconciled before publish.
- `machine-pass` / gate pass is not equivalent to full runtime validation or release approval.

## Condições / Bloqueios para Publicação

| Item | Status | Notes |
|------|--------|-------|
| Version bump to `0.2.0-beta` | pending | Manifest currently `0.1.0`; bump only with reconciled provenance and authorization. |
| Provenance reconciliation | pending | Align final target commit/tag narrative across baseline, gate evidence, and release record. |
| Nexus AI disclosure inventory | pending | Required before publish; complete factual categorization (AI-Generated/AI Media/AI Assisted as applicable). Grace noted to 2027-01-02 for new requirements, but accurate disclosure is mandatory. |
| UpdateKeys (`GitHub:lfgranja/LivingRoots`, `Nexus:<id>`) | pending | Nexus ID/keys must be confirmed before upload metadata is finalized. |
| Maintainer authorization (tag/push/upload) | required | No tag/push/publication/Nexus upload without explicit maintainer decision. |
| Human / D1 / SC-004 verification | pending | Out of scope for this RC-prep validation; do not claim. |

## Riscos / Pendências

- Publication metadata and disclosure are incomplete; treating this as releasable would be inaccurate.
- Runtime evidence remains bounded (host-only machine-pass history; spec 020 plant-only context in baseline record); broader verification remains pending.
- Evidence is valid for RC prep on the stated commit/time; any subsequent changes require revalidation as appropriate.

## Veredito

**APPROVED (condicionado) for RC preparation only.** Not approved for publication/release until provenance, version bump, AI disclosure, UpdateKeys, and maintainer-authorized tag/push/upload steps are completed.
