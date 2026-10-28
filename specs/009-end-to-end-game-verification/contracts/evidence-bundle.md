# Evidence Bundle Contract (FR-001c)

**Feature**: 009-end-to-end-game-verification
**Canonical structure**: MIRROR-style `evidence-bundle-<run-id>/` directory.
**Extended by**: spec 010 (`specs/010-headless-verification-runner/spec.md` FR-014/FR-015/FR-024/FR-027, C-BND-01, C-SEC-29).

## Layout

```
evidence-bundle-<run-id>/
├── manifest.json              # schema version, run id, timestamp, tool/game/mod versions, environment, subject digests
├── manifest.sha256            # detached SHA-256 anchor of manifest.json (spec 010)
├── claims.json                # claim -> evidence pointer, risk, honesty level, safe wording, refusal site
├── artifacts/                 # screenshots, SMAPI logs, save-deltas before/after, reflection outputs
├── attempts/<n>/attempt.json  # per-attempt record (spec 010 FR-011)
├── attempts/<n>/manifest.json # per-attempt digest manifest (spec 010 C-BND-01)
├── integrity-failure.json     # tamper-evident record on integrity failure (optional)
├── junit.xml                  # CI integration (optional)
└── allure.json                # rich report (optional)
```

`runId` format is `run-<YYYYMMDDTHHMMSSZ>-<4 lowercase hex>` (C-BND-02), not a UUID. The directory name is `evidence-bundle-<runId>/` and the manifest `runId` MUST equal the directory component.

## manifest.json schema

```json
{
  "schemaVersion": 1,
  "runId": "run-20261027T120000Z-a1b2",
  "timestamp": "2026-10-27T12:00:00Z",
  "toolVersions": { "dotnet": "6.0", "smapi": "4.4.0" },
  "gameVersion": "1.6.14",
  "modVersion": "0.4.0",
  "environment": {
    "gameVersion": "1.6.14",
    "smapiVersion": "4.4.0",
    "modVersion": "0.4.0",
    "apiVersion": "1.0.0",
    "saveId": "farm-year1-day5",
    "mode": "ci"
  },
  "subject": [
    { "name": "artifacts/soil-health-before-after.json", "digest": { "sha256": "..." } },
    { "name": "artifacts/crop-phase-dump.json", "digest": { "sha256": "..." } },
    { "name": "artifacts/smapi-log.txt", "digest": { "sha256": "..." } }
  ],
  "files": [
    { "name": "artifacts/soil-health-before-after.json", "digest": { "sha256": "..." } }
  ],
  "verdict": "pass",
  "honestyLevel": "R",
  "attempts": ["attempts/1/attempt.json", "attempts/2/attempt.json"],
  "compatibility": { "tier": "INFO", "deviation": null },
  "commands": [],
  "materials": [],
  "seeds": [],
  "determinism": { "idempotent": true, "declaredSources": [] },
  "commandsCompleteness": false,
  "materialsCompleteness": false,
  "seedsCompleteness": false,
  "integrityFailureRef": null
}
```

- `subject` is canonical (SLSA-aligned). `files` is a deprecated alias retained for one major version; producers SHOULD emit both during the transition, and the verifier prefers `subject` when both are present.
- `manifest.sha256` is detached and written after `manifest.json` is finalized. It is the trust root the offline verifier compares against; a manifest that does not match its anchor is rejected.
- Mandatory evidence for honesty level R/E: `commands`, `materials`, at least one seed material, `claims.json`, and recomputed digests (C-SEC-29). A bundle missing any mandatory item for its claimed level fails closed in the offline verifier.
- `integrity-failure.json` is append-only and is listed in `subject`; it is never overwritten or deleted on retry.

## claims.json structure

```json
[
  {
    "claimId": "C-001",
    "claim": "poor soil slows crop growth by 1.15x",
    "evidencePointer": "artifacts/crop-phase-dump.json#/crops/0/phaseDays",
    "risk": "medium",
    "honestyLevel": "R",
    "safeWording": "The automated run observed a 1.15x phase-day multiplier on the target tile.",
    "refusalSite": "Refuse the claim if the three signals disagree or the run is flaky."
  }
]
```

`risk` is `low`/`medium`/`high`; `honestyLevel` is `W`/`R`/`E`; `refusalSite` names the exact condition under which the claim must be refused. The offline verifier tests refusal-site liveness with negative fixtures.

## Honesty Levels (W/R/E)

- **W (launch/connect)**: tooling/launch verified (SMAPI loads, mod connects); no runtime evidence collected.
- **R (runtime evidence collected)**: evidence artifacts present and valid (save delta, log excerpt, observation notes), plus commands, materials, at least one seed, claims, and recomputed digests.
- **E (human judgment)**: assigned only by the maintainer. Never auto-assigned. Requires W + R as prerequisites.

Assignment rule: W → R → E (sequential). E cannot be assigned without both W and R. Auto-promotion to E is prohibited. A clean non-flaky host `pass` with three agreeing signals and honesty ≥ R is `machine-pass`; `verified` is a human-only E attestation over that machine evidence and is never auto-assigned.

## Failure handling

Five base verdicts — `pass`, `fail`, `errored`, `partial`, `skipped` — plus the derived metadata mark `flaky` (never a sixth base verdict).

- Crash → `errored` with attached artifacts.
- Incomplete signal set → `partial` (never auto-promoted to pass).
- Signal disagreement (dump vs. save) → `partial`.
- Retry only transient/environmental failures with bounded attempts and per-attempt logging.
- A run is `flaky` iff attempt 1 fails with a retryable class and a later attempt passes on the same commit/seed/environment. A flaky pass exits `0` but never promotes `test.md` to `verified`.
- Two flaky runs in the rolling 14-day window make the scenario a **quarantine candidate**; automation proposes, a human approves. **Confirmed-flaky status** is a separate statistical status requiring N ≥ 30 runs and a Wilson 95% CI excluding 100% with failure rate ≥ 30%. Pass-rate tiers (`healthy ≥ 99%`, `monitor 95–99%`, `< 95%` alert) are monitoring signals, not quarantine triggers.
- Quarantine time-box is 14 days with one 14-day extension (28-day hard cap). At the cap the scenario is auto-disabled and an issue is opened; auto-disable is the only automatic terminal action. Deletion requires human approval.
- Every attempt writes `attempts/<n>/attempt.json` plus its own `attempts/<n>/manifest.json`; the top-level `attempts[]` lists those paths and is append-only.

## Save preservation

Verification runs execute against a disposable save copy. The source milestone save and the restored working copy must be byte-identical to their pre-run SHA-256 digests; a mismatch is `errored` (exit 19). Backup/restore covers `Saves/<Name>/` and sidecars `soil_health_data_{saveId}.json` / `composting_bins_{saveId}.json`; sandbox via `XDG_CONFIG_HOME` temp dir. The player's real save directory is never opened, copied, or modified.
