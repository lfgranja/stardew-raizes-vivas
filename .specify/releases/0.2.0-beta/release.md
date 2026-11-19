# Release — 0.2.0-beta (Draft / Pre-release Record)

- **Slug**: 0.2.0-beta
- **Gate**: R7 (SemVer, changelog/notes, rollback, comunicação) — preparation trail
- **Versão alvo**: `0.2.0-beta` (SemVer prerelease; **not yet bumped** in `LivingRoots/manifest.json`, currently `0.1.0`)
- **Mod IDs/compat (current manifest)**: `lfgranja.LivingRoots`, `MinimumApiVersion>=4.4.0`, `MinimumGameVersion>=1.6.14`
- **Data do registro**: 2026-11-14
- **Tipo**: draft / pre-release record (**not published**)
- **Veredito**: **DRAFT — NOT RELEASED / NOT PUBLISHED**

## Provenance / Evidence

- **Deterministic release-gate evidence (RC prep):** commit `27ed81b5d49773cd679a04a43142e267359bd13c`, `2026-11-14T21:22:43Z` UTC, `GamePath="/home/luis/.steam/steam/steamapps/common/Stardew Valley" ./scripts/release-gate.sh` — steps 1–7 OK (format, locked restore, build Release, tests, smoke 51 files / 50 passed / 0 failed / 1 skipped launch-free, docs markers, protected paths).
- **Baseline verified snapshot (reference, unchanged):** `2026-11-10 c07fbab`, 1997/0/4, 7 host-only machine-pass scenarios, 0 human-verified, 0 D1, 0 SC-004, spec 020 plant-only.
- **Provenance reconciliation:** pending (must align final target commit/tag, manifest version, and evidence trail before any publish/tag action).
- **Publication actions:** none performed — no tag, no push, no publication, no Nexus upload.

## Release Notes (Draft / Pending Finalization)

- Target: prepare `0.2.0-beta` prerelease artifacts and metadata based on deterministic gate evidence on `27ed81b...`.
- Scope/claims boundary: deterministic and host-only machine evidence only as recorded; **no human-verified, no D1, no SC-004 claims**.
- Final user-facing changelog/release notes content: **pending** maintainer curation and provenance reconciliation.

## AI Disclosure Status (Nexus)

- **Required before publish:** Nexus AI disclosure must be completed with facts.
- **Categories to inventory/disclose as applicable:** AI-Generated / AI Media / AI Assisted (per 2026 Nexus help guidance; grace period noted to 2027-01-02 for new requirements).
- **Status:** pending factual inventory and disclosure selection — do not publish without completed disclosure.

## Metadata / Publication Preconditions (Pending)

- Bump `LivingRoots/manifest.json` `Version` to `0.2.0-beta` (and reconcile provenance) — pending, maintainer decision.
- Confirm/fill `UpdateKeys`: `GitHub:lfgranja/LivingRoots`, `Nexus:<id>` — pending (Nexus ID unknown/unset).
- Maintainer authorization required for: git tag, push, GitHub/Nexus publication, and any upload.
- Reconfirm honesty boundaries in public notes: no human/D1/SC-004/published claims.

## Rollback

- **Pre-publication:** revert RC-prep commits (manifest bump, release-note/disclosure metadata edits, trail docs) if conditions are not met; ensure `dotnet build`/`dotnet test` and deterministic release gate remain green.
- **If a prerelease is later published (future, authorized):** unlist/yank prerelease channel artifacts as appropriate, publish corrective notes, and point users to the last known-good release; no save-schema migration is implied by this draft trail.
- **Data safety note:** no schema migration introduced by this prep record; rollback is documentation/versioning/metadata focused unless future code changes state otherwise.

## Comunicação (Draft)

- Internal/maintainer: share this draft trail, gate evidence (`27ed81b...`, 2026-11-14T21:22:43Z UTC), and pending blockers (provenance, disclosure, UpdateKeys, authorization).
- Public/community/Nexus: **no communication or publication** until maintainer authorizes and all preconditions are satisfied; any future announcement must state prerelease status and exclude unverified human/D1/SC-004 claims.

## Honesty Boundaries

- This file is a **draft pre-release record**, not proof of release.
- Do not claim human-verified, D1 convergence, SC-004, or published status.
- Baseline `c07fbab` and gate commit `27ed81b...` are distinct evidence points pending reconciliation.
