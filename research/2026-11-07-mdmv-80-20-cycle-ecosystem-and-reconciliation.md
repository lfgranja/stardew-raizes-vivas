# MDMV 80/20 Cycle — Ecosystem Research & 014↔015 Reconciliation (2026-11-07)

**Scope**: High-reliability web research (official/primary sources prioritized) + autonomous 80/20 decision for `stardew-raizes-vivas` (Living Roots), adapted from the MDMV Dark Factory flow (`/home/luis/development/MDMV/projetos/mdmv-dark-factory`).  
**Honesty**: External facts are cited; local repo facts are verified from files/commands in this session. `machine-pass` ≠ `verified`/human E. No fabricated runs.

## 1) Ecosystem facts (verified 2026-11-07, web)

| Topic | Finding | Source (primary/official where possible) | Confidence |
|---|---|---|---|
| SMAPI current | **SMAPI 4.5.2** released **14 March 2026** for Stardew Valley **1.6.14+** (Linux/macOS/Windows). 4.5.1 fixed Linux/macOS installer; 4.5.2 improved performance, fixed color-scheme save on Linux/macOS, updated translations. | https://smapi.io/ ; https://github.com/Pathoschild/SMAPI/blob/develop/docs/release-notes.md | High (official) |
| Mod compatibility list | smapi.io states the mod compatibility list is **no longer comprehensively updated** and its future is in doubt (“sunset” caveat). | https://smapi.io/mods | High (official) |
| Headless / dedicated multiplayer hosting | **JunimoServer** (Docker, open-source) runs Stardew Valley as an always-online/headless-style dedicated server (no host player owns the save in the same way as normal co-op); described as alpha/unstable in some contexts and requires Steam-owned copy + Docker/Linux comfort. **SMAPIDedicatedServerMod / “Always On Server”** style mods automate host behavior (not the same as farmhand convergence testing for a specific mod). These are hosting references, **not proof** of Living Roots D1 farmhand convergence. | https://github.com/stardew-valley-dedicated-server/server ; https://hub.docker.com/r/sdvd/server ; https://github.com/ObjectManagerManager/SMAPIDedicatedServerMod ; https://hostmygame.eu/en/guides/stardew-valley/stardew-valley-dedicated-server-setup ; https://space-node.net/blog/stardew-valley-dedicated-server-multiplayer-guide-2026 | Medium–High (official repos/docs; stability claims vary) |
| Competitive landscape | **ReGen Valley** (Nexus #39740, 2025) remains a close soil-health competitor (per-tile soil health, cover crops, grazing, farm-wide quality/yield signals) but differs from Living Roots’ per-tile harvest seam + compost/mulch/growth coherence + host-authoritative multiplayer design intent + educational framing. Nexus page access may be blocked in some automated contexts (403 observed historically in repo notes); treat Nexus download counts/details as **not revalidated live** here. | https://www.nexusmods.com/stardewvalley/mods/39740 (access context-limited); https://thecozygamer.com/blog/best-stardew-valley-mods ; https://technwz.com/best-stardew-valley-mods | Medium (mixed: primary mod page + secondary roundups) |

## 2) Repo state (verified 2026-11-07, local)

- Branch: `dev` (218 commits ahead of `origin/dev` at session start; 1 local commit added by this cycle before this note: Gate 3 marker promotion). Modified files at start: `COMPLETION_SUMMARY.md`, `ROADMAP.md`, `specs/010-headless-verification-runner/promotion-triggers.json` — all coherent runner-produced marker updates for `run-20261107T210830Z-67f1` (verdict pass, honesty R, commit `e969bb5c04757db35367cdc0499a6330bd99767b`). Bundle dir exists locally but is gitignored (`evidence-bundle-run-*/`).
- Spec 017: Phase 11/12 closed per repo annotations (2026-11-07); Gate 3 machine-pass host-only (bundle `589a`, honesty R); `machine-pass` ≠ `verified`; D1/SC-004/human E pending.
- Spec 014: 131/133; T109 D1 blocked; T110 deferred (historical runner-contract reasons noted). 014 Phase 9 includes classifier parity work (31/71) in code.
- Spec 015: Harmony harvest seam largely implemented/tested; Phase 11 reconciliation (T061–T065) completed. After this cycle’s honest bookkeeping, T059 marked `[x]` (impl+tests; runtime spot-check residual). Remaining open: T047, T049, T050, T052, T057, T058, T060 (blocked/deferred/human-E/runtime-residual mix). No fabricated runs.

## 3) Autonomous 80/20 decision (executed)

**Decision**: (A) Commit coherent, runner-produced Gate 3 marker updates (honest housekeeping, reduces dirty state). (B) Execute 014↔015 reconciliation as append-only, evidence-based bookkeeping: resolve T059 with explicit residual honesty and append a Phase 12 mapping table for remaining open 015 tasks. (C) Record ecosystem research with primary-source citations.

**Why 80/20**: High clarity/low risk; no gameplay behavior change; no fabricated evidence; no remote/destructive actions; avoids blocked items (real D1 farmhand requires environment/human/explicit capability not available here).

**Not done (requires explicit authorization/capability/human)**: push to `origin` (remote action; 200+ commits ahead — decide intentionally), real D1 farmhand convergence (T109/015 T049/T057), human E UX/verification attestations (T052/T060 and Gate 3 `verified`), pest clamp (T050 dependency).

## 4) Next 80/20 candidates (ranked)

1. **D1 environment prep/run** (when a second player or supported MP harness is available): closes 014 T109 + 015 T049/T057 cluster; requires explicit environment/human.
2. **Human E spot-checks** (audio/sprite UX, extra-harvest/regrow runtime edges): closes 015 T047/T052/T058/T060 residuals with honest evidence.
3. **Spec 018 Wave 0 follow-ups** (where feasible): place-bin macro already exists; `debug split` via queue remains blocked (T002 evidence recorded) — needs runner/queue design decision; mulch verification seam (T005–T008) is feasible but larger code work.
4. **Publish/sync docs**: decide intentionally about pushing `dev` to `origin/dev` (no history rewrite). Not executed autonomously.

## Sources

- SMAPI: https://smapi.io/ ; https://github.com/Pathoschild/SMAPI/blob/develop/docs/release-notes.md ; https://smapi.io/mods
- JunimoServer / headless hosting: https://github.com/stardew-valley-dedicated-server/server ; https://hub.docker.com/r/sdvd/server ; https://github.com/ObjectManagerManager/SMAPIDedicatedServerMod
- Hosting guides (secondary, descriptive): https://hostmygame.eu/en/guides/stardew-valley/stardew-valley-dedicated-server-setup ; https://space-node.net/blog/stardew-valley-dedicated-server-multiplayer-guide-2026 ; https://www.survivalservers.com/news/stardew-valley-dedicated-server
- Mod landscape (secondary/primary mix): https://www.nexusmods.com/stardewvalley/mods/39740 ; https://thecozygamer.com/blog/best-stardew-valley-mods ; https://technwz.com/best-stardew-valley-mods
- Local repo truth: `ROADMAP.md`, `COMPLETION_SUMMARY.md`, `specs/014-soil-health-crop-quality/tasks.md`, `specs/015-harmony-harvest-seam/tasks.md`, `specs/018-machine-pass-full-coverage/tasks.md`, `evidence-bundle-run-20261107T210830Z-67f1/verdict.json` (local, gitignored bundle), `specs/010-headless-verification-runner/promotion-triggers.json`.
