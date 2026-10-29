# Manual End-to-End Verification Protocol — SMAPI / Modding / Gameplay Loop
# Subagent research deliverable; manual-first + multi-signal evidence preference

---

## 1. Sources retrieved (with classification)

| Source / URL | Classification | What it contributes |
|---|---|---|
| SMAPI docs / GitHub / Wiki (smapi.io, stardewvalleywiki.com/Modding:Console_commands, SMAPI log parser) | Community / practice docs | Standard SMAPI workflow: build → install → SMAPI launch → test → review console/log → release; save profiles; console commands; log levels (TRACE/DEBUG/INFO/WARN/ERROR) |
| Monitoring & Evaluation Studio — “Triangulation: Using Multiple Sources and Methods” (monitoringevaluationstudio.com/resources/reference/triangulation); BetterEvaluation.org | Research methodology (evaluation / social science) | Triangulation = cross-verifying findings against >1 source/method; different methods fail differently; shared conclusion from independent weaknesses is robust |
| Huang, Meyer, Oriol — “Combining Tests and Proofs with Contracts for Better Software Verification”, Communications of the ACM, 2026 (cacm.acm.org/research/combining-tests-and-proofs-with-contracts-for-better-software-verification) | Peer-reviewed (ACM / IEEE-aligned) | Tests + proofs + contracts are complementary; no single method sufficient; contracts specify expected behavior; proofs cover all inputs only within stated assumptions |
| ITEA Journal Vol 47 Iss 2 — “Human Oversight for AI-Generated Test Artifacts” (itea.org/journals/volume-47-2/human-oversight-for-ai-generated-test-artifacts); references NIST 2023 | Peer-reviewed / standards-aligned (ITEA; NIST cited) | Independent judgment required; artifact quality (complete, traceable, fit-for-use) matters more than quantity; human review must assess whether evidence is adequate — not just whether it exists |
| Astaqc — “Why Tests Pass but Software Still Breaks: The Assertion Gap in Automated Testing in 2026” (astaqc.com/software-testing-blog/why-tests-pass-software-still-breaks-assertion-gap-2026) | Industry / practitioner analysis | False confidence from passing tests that miss partial failures; assertion gap allows wrong values / wrong fields / wrong UI text to ship; multi-layer detection needed |
| Astaqc — “Dead Tests in CI/CD 2026” (astaqc.com/software-testing-blog/dead-tests-cicd-2026-detect-remove-tests-never-run-always-pass) | Industry / practitioner analysis | Pipeline reporting large pass counts (e.g., 400 passed, 60 never executed) creates false confidence; safe deletion requires verification; coverage data must be read with intent |
| Tricentis — “Build verification testing: What it is & why it matters” (tricentis.com/learn/build-verification-testing) | Industry / vendor best practice | BVT/smoke testing is a litmus test only — confirms stability for deeper testing, not exhaustive correctness; “goal isn’t exhaustive coverage” |
| Medium / PhoenixArjun — “Why Your AI ‘Works’… But Still Fails: The Missing Layer of Verification Engineering” (medium.com/@phoenixarjun007/why-your-ai-works-but-still-fails-the-missing-layer-of-verification-engineering-bd13f17685af) | Practitioner / opinion (industry) | Explicit “false pass” concept: build passes, metrics in range, output still wrong; manual verification “slower by design” for control; verification layer must check output meets criteria |
| N-iX / Virtuosoqa / PractiTest — 2026 software testing best practices (n-ix.com/software-testing-best-practices, virtuosoqa.com/post/testing-best-practices, practitest.com/resource-center/blog/software-testing-best-practices-checklist) | Industry / survey-based | Automate critical paths with clear ownership; 20–25% of builds pass all tests despite high coverage; behavioral testing requires real execution; evidence-rich failure reporting; manual testing irreplaceable for judgment |
| SMAPI compatibility / troubleshooting guides (gameserverkings.com, youtube guides, Stardrop profile docs) | Community / practice | Save-file comparison via profiles; backup procedures before installing mods; comparing mod lists across players; SMAPI console watch for skipped/rejected mods |

---

## 2. Core principle: manual-first + multi-signal evidence (NOT build-only)

Recommendation (strong): Every significant gameplay-loop change is verified through **three independent signal classes**, not one.

- **Observation signal** — human-run game session, eye/motor confirmation of behavior (e.g., composting bin state transition Empty→Processing→Ready; soil overlay renders at correct tile; tooltip text matches expected category).
- **Artifact signal** — save-file / data-file comparison (before vs after), config state, persistence DTO content (e.g., SoilHealthState mapping, CompostingBinData), screenshot/recording of screen state.
- **Log signal** — SMAPI console / SMAPI-latest.txt / SMAPI-crash.txt review for WARN/ERROR, mod load confirmations, skipped mods, exception traces, context flags (IsPlayerFree, draw-loop events).

Rationale (evidence-based):
- Triangulation methodology: “Every data source carries a bias... When sources with unrelated weaknesses point the same way, the shared conclusion is unlikely to be an artifact of any one of them” (Monitoring & Evaluation Studio; BetterEvaluation). Applied to mod verification: observation may miss a silent data corruption; save-file comparison may miss a rendering glitch; console log may miss a gameplay logic error. Only when all three converge is the claim “verified” defensible.
- CACM 2026 (Huang, Meyer, Oriol): “Moving past the traditional opposition between tests and proofs, current work treats them as complementary.” Translate: automated unit tests (proofs over limited inputs) complement manual observation (tests over real environment) and artifacts (contracts/contracts on saved state). No single source stands alone.
- ITEA / NIST-aligned principle: “Artificial intelligence may help draft a test case, but it cannot remove the need for independent judgment about whether that test case is adequate, complete, traceable, and fit for use as evidence.” For manual verification: a checkbox “I ran it” is insufficient; the judgment must assess whether the observation actually covered the intended gameplay loop and whether artifacts/logs corroborate it.

Preference ordering: **Manual end-to-end session → multi-signal capture → scripted automation only for stable, reused paths.** Build-only (compile + unit pass) is a prerequisite, never a verification claim.

---

## 3. Recommended documented manual checklist (pre-state / steps / evidence / post-state comparison)

### 3.0 SC-009 label mapping (project-specific identifier)

`SC-009` is a **project-specific Living Roots identifier** — it is NOT an IEEE/ISO/SMAPI standard. It is overloaded across four specs and must be disambiguated by context:

| Spec | Meaning of `SC-009` |
|------|---------------------|
| spec 004 (playable-compost-loop) | compost conservation audit |
| spec 005 | per-criterion manual verification |
| spec 008 | pre-flight prerequisite detection |
| spec 009 (this feature) | **manual verification protocol for the Phase 2 growth hook** (this document §3) |

In spec 009 specifically, `SC-009` refers to the **manual verification protocol** defined in this §3 — a documented checklist for a real SMAPI play-through of the Phase 2 growth hook. It is distinct from the scripted pipeline (save comparator only, per FR-001a/FR-003); SC-009 is the manual-protocol label, not the automation component. Formal definition: `specs/009-end-to-end-game-verification/spec.md` Success Criteria (SC-009). Per-criterion checklist: `specs/009-end-to-end-game-verification/checklists/gate3-verification-contract.md`. Status: unverified (requires `GamePath` + real SMAPI session).

Use a reproducible template per feature / gameplay loop segment. Example template for a composting-bin / soil-health mod loop:

### 3.1 Pre-state (baseline — capture BEFORE touching feature)
- [ ] Save-file backup created (copy entire StardewValley/Save folder to dated backup directory; note: SMAPI profile management allows profile-specific saves — use dedicated test profile per docs).
- [ ] Mod list / version pinned (record manifest.json versions of all loaded mods; compare with compatibility list if needed; SMAPI console lists loaded mods and skipped mods at start).
- [ ] Pre-state screenshot / observed state recorded (e.g., tile at target location shows default soil health / bin state Empty; no overlay present; no tooltip active).
- [ ] Console cleared / SMAPI log rotated (start clean SMAPI session; note start time / log file path; ensure SMAPI-latest.txt will be readable post-run).
- [ ] Game started via SMAPI (StardewModdingAPI.exe / launcher), not vanilla executable; confirm SMAPI header line in console.
- [ ] State variables noted: player location, day/time, weather/season if relevant (seasonal decay multiplier is domain-relevant per project docs).

### 3.2 Steps (executed in sequence, not skipped)
1. Trigger action (e.g., interact with composting bin; apply organic waste; use hoe on tile; save and exit to title / return to title if testing persistence).
2. Advance time/state minimally required (e.g., advance to next day, sleep, or perform specific in-game action that triggers state-machine transition Empty→Processing→Ready; per CompostingBinService state machine docs).
3. Re-enter / observe result at same world tile / same save session; avoid testing only in a fresh session if persistence is part of the claim.
4. If multi-player / co-op context applies: compare state across clients (per SMAPI co-op docs: server must load same mod versions; compare console outputs).

### 3.3 Evidence capture (multi-signal — ALL three required for a claim of “verified correct”)
- [ ] Observation evidence — handwritten / typed notes or video of screen state at each transition point; describe specifically what was seen (not “it looked fine” — “bin UI showed Processing and timer had incremented”). If using overlay rendering, confirm overlay is visible, color category matches expected category (from Visualization/ DTO if applicable), tooltip text readable.
- [ ] Artifact evidence — save file diff / exported state captured post-run; compare CompostingBinData / SoilHealthState persistence DTO; specifically check that non-default values are stored (project convention: “sparse cache — don’t store default 0 health values” — verify absence of 0 entries where expected non-zero; verify presence of expected entries where state changed); config/config.json reviewed if settings affected result.
- [ ] Log evidence — SMAPI console / SMAPI-latest.txt inspected for WARN/ERROR; confirm no “Skipping mod” message for the module under test; confirm no exception in draw/update events; if using ModController concurrency guard / Interlocked flags, confirm no deadlock warnings; if custom console command used, confirm command registered and executed (per RegisterConsoleCommand() pattern).

### 3.4 Post-state comparison (quantified comparison, not “seems OK”)
- [ ] Compare pre-state screenshot to post-state screenshot (same camera/zoom if overlay involved; note overlay type from Visualization/ DTO if applicable).
- [ ] Compare pre-state save artifacts to post-state artifacts (diff of relevant persistence fields; state-machine status field must match expected value; do NOT rely only on “game loaded without crash”).
- [ ] Confirm no unexpected secondary effects (nearby tiles unchanged; other mods not skipped; no new ERROR entries in log after the interaction).
- [ ] If claim is “works on fresh save”: repeat on clean profile; if claim is “works on existing save / mid-playthrough”: test on established save (some mods explicitly recommend new save — document which case applies, per community notes on expanded-mod-style recommendations).

---

## 4. Avoiding false-positive claims from build-only verification (the “1447 passed / 0 failed” trap)

Concrete problem: A CI pipeline can report “1447 passed / 0 failed” (or 400 passed / 60 never executed) while the gameplay loop is entirely unverified. This is a documented failure mode, not hypothetical.

Evidence of failure mode:
- Astaqc (2026): “A test suite that detects complete failures — the endpoint is down — but misses the partial failures that are more common in practice… a function that returns the wrong value, an endpoint that returns the right structure with wrong field values, a UI that renders the wrong text — these are the defects that the assertion gap allows to reach production undetected.” Your “1447 passed” is likely catching compile errors and null-reference crashes, not “composting bin shows Ready when waste was never added.”
- Astaqc / Dead Tests: “A CI pipeline reporting 400 passing tests provides false confidence if 60 of those tests have not executed their target code paths in months.” Replace “tests” with “unit assertions” — many pass without exercising the game loop.
- Tricentis / BVT: Build verification is “not exhaustive coverage” — “think of it as a litmus test.” A green build verifies the assembly loads; it verifies nothing about state-machine correctness, persistence, or overlay rendering in a live SMAPI session.
- Medium / Verification Engineering: “The first failure is the false pass. Everything looks green. Tests pass. Metrics are within range. The system appears correct, but the output is still wrong.” Explicit recommendation: “Manual verification alone does not scale — but it is the only verification that checks output against real usage.”

How to prevent in documentation / reports:
- State explicitly: “Verification status = build-stable + manual session verified (date) + artifacts compared + log reviewed.” Not “tests pass = done.”
- Separate sections: (A) Build / compile / format check (the CI gate), (B) Manual end-to-end verification results, (C) Multi-signal evidence index (observation notes file, artifact diff file, log excerpt file, with paths).
- If automation is used: distinguish “automated regression of stable path” (reusable) from “feature verification” (requires manual session at least once per state-machine change or per rendering change).

---

## 5. When scripted automation is justified (reuse / cost-benefit threshold — NOT the default)

Recommendation (threshold-based, conservative): Automate only when ALL of the following hold:

1. Reuse threshold met — The path will be executed ≥3 times (regression after cap changes, after dependency updates, across save profiles, or across SMAPI version bumps). One-time exploratory verification does NOT justify automation cost; the “build once, run once” case is manual.
2. Interface stability — The gameplay loop or persistence schema has been stable through ≥2 manual verifications without change; if CompostingBinService state machine or ModConstants tile limits are actively changing, automation will break and require maintenance that exceeds manual repetition cost.
3. Observable / checkable outcome — The verification can be expressed as a concrete check (save-file value equals expected; overlay pixel / category checked; log contains/doesn’t contain specific substring; console command returns expected response). “Looks right to a human” is not automatable; “SoilHealthState[LocationName][TileKey] == 3” is.
4. Cost of manual repetition > cost of script maintenance — Include human time to run, observe, compare artifacts, document. For SMAPI modding, a 10-minute manual session × 5 repetitions = ~50 min; a script that takes 30 min to write + 10 min/test maintenance per SMAPI update may or may not win — use the threshold explicitly.
5. Evidence preservation — The script produces an evidence artifact (diff, screenshot, log snippet) that can be reviewed independently, not just a pass/fail bit. Per ITEA/NIST: “independent judgment about whether the test case is adequate” requires visible evidence, not hidden automation.

When NOT to automate (manual-first applies):
- New gameplay loop / new state machine transition (Empty→Processing→Ready first-time verification must be observed, not scripted).
- New rendering / overlay / color category (visual verification requires human observation; automated pixel comparison is fragile to resolution, mod load order, and XNA draw-loop timing).
- First-time persistence verification (first save/load cycle after data-structure change — must observe artifact corruption risks directly).
- Any claim that depends on player interaction timing (e.g., “after 3 days” requires real-time or simulated-time verification that is easier to observe manually with save backups than to script correctly).

Automation restructuring (if used):
- Use as regression guard on stable paths, not as primary verification of new behavior.
- Combine with manual session: script checks artifacts/log; human confirms observation on first run; subsequent runs use script + spot-check observation.

---

## 6. SMAPI-specific verification practices (from community/docs)

- Launch exclusively via SMAPI; verify SMAPI header in console (not vanilla); confirm mod folder placement (Mods/<ModName>/manifest.json) per install guide.
- Watch console at load: “SMAPI lists what loaded, and anything it rejected under Skipped mods.” A skipped dependency can silently disable your feature; log evidence must confirm the module is loaded, not just “game started.”
- Use console commands (help, custom registered commands) to trigger states deterministically for evidence, not just for cheating — document command used and result.
- Back up saves before mod changes; use profiles (SMAPI/Stardrop-style) to keep vanilla and modded saves separate; compare profile saves side-by-side when verifying persistence.
- Read log at correct level; SMAPI-latest.txt / SMAPI-crash.txt paths vary by OS (Windows: %AppData%/StardewValley/ErrorLogs or game folder; Linux: ~/.config/StardewValley/ErrorLogs). For verification claims, include log excerpt path in evidence index.
- If using ModController patterns: confirm ExecuteWithConcurrencyGuard, atomic flags, Interlocked state; do not suppress overflow checking (project-wide CheckForOverflowUnderflow=true). These are structural verification points, not gameplay points — observe via log/concurrency tests separately.

---

## 7. Summary — recommendation for this session/task context

Given this project’s conventions (SMAPI mod, DDD layered architecture, ModController atomic state machine, sparse persistence, no hardcoded constants, VisualizationService overlays, CompostingBinService state machine, SoilHealthService persistence, security-first sanitization, thread-safe design):

- Preferred verification order: (1) Manual SMAPI session with pre-state record → (2) State-machine transition observed → (3) Save artifact compared (before vs after; check sparse-cache rule) → (4) SMAPI console/log reviewed for WARN/ERROR/skip messages → (5) If feature is stable and reused, add scripted regression that checks the artifact + log; do NOT replace (1)–(4) with (5).
- Evidence documentation: Maintain per-task folder (verification/<feature>/) containing observation notes, artifact diff file, log excerpt file, pre/post screenshots, checklist markdown. This satisfies the “evidence artifacts” expectation (IEEE/ACM artifact badges require reproducible materials; ITEA requires traceable evidence).
- Claim discipline: Do not write “verified by build / 1447 passed” in release notes. Write “verified by manual end-to-end session + multi-signal evidence: observation notes [filename], artifact comparison [filename], SMAPI log [filename], session date [date].” The build check lives in a separate “build verification” section.
- Automation justification: Only after 2+ manual passes with stable interface; only for paths that can be expressed as concrete artifact/log checks; always with visible evidence output.

---

## 8. Source links (selected — all retrieved this session)

- SMAPI workflow / console / log parser / install: https://smapi.io ; https://github.com/Pathoschild/SMAPI ; https://stardewvalleywiki.com/Modding:Console_commands ; https://stardewvalleywiki.com/Modding:Installing_SMAPI_on_Windows ; https://www.gameserverkings.com/knowledge-base/stardew-valley/how-to-mod-stardew-valley
- Multi-source / triangulation verification methodology: https://www.monitoringevaluationstudio.com/resources/reference/triangulation ; https://www.betterevaluation.org/methods-approaches/methods/triangulation
- Tests + proofs + contracts (peer-reviewed / CACM 2026): https://cacm.acm.org/research/combining-tests-and-proofs-with-contracts-for-better-software-verification
- Independent judgment / evidence quality (ITEA / NIST-aligned): https://itea.org/journals/volume-47-2/human-oversight-for-ai-generated-test-artifacts
- False confidence / assertion gap / dead tests: https://www.astaqc.com/software-testing-blog/why-tests-pass-software-still-breaks-assertion-gap-2026 ; https://www.astaqc.com/software-testing-blog/dead-tests-cicd-2026-detect-remove-tests-never-run-always-pass
- Build verification (smoke / litmus, not exhaustive): https://www.tricentis.com/learn/build-verification-testing
- False pass / verification engineering (practitioner): https://medium.com/@phoenixarjun007/why-your-ai-works-but-still-fails-the-missing-layer-of-verification-engineering-bd13f17685af
- Best practices / automation thresholds / evidence-rich reporting: https://www.virtuosoqa.com/post/testing-best-practices ; https://www.n-ix.com/software-testing-best-practices ; https://www.practitest.com/resource-center/blog/software-testing-best-practices-checklist
- SMAPI log reading / save backup / profiles: https://jenna-juffuffles.gitbook.io/svve/troubleshooting/your-smapi-log ; https://www.youtube.com/watch?v=LwLF2vIQEfA ; https://www.youtube.com/watch?v=3OYK8y0TPuo (Stardrop profiles)
