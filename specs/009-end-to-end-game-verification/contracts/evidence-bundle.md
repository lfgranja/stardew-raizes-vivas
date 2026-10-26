# Evidence Bundle Contract (FR-001c)

**Feature**: 009-end-to-end-game-verification
**Canonical structure**: MIRROR-style `evidence-bundle-<run-id>/` directory.

## Layout

```
evidence-bundle-<run-id>/
├── manifest.json          # schema version, run id, timestamp ISO 8601, tool/game/mod versions, environment, SHA-256 per file
├── claims.json            # claim → evidence pointer, risk, safe wording
├── artifacts/             # screenshots, SMAPI logs, save-deltas before/after, reflection outputs
├── junit.xml              # CI integration (optional)
└── allure.json            # rich report (optional)
```

## manifest.json schema

```json
{
  "schemaVersion": "1.0",
  "runId": "<uuid>",
  "timestamp": "2026-10-25T00:00:00Z",
  "toolVersions": { "dotnet": "6.0", "smaapi": "4.5" },
  "gameVersion": "1.6.x",
  "modVersion": "1.0.0",
  "environment": "local-dev",
  "files": [ { "path": "artifacts/...", "sha256": "..." } ]
}
```

## claims.json structure

```json
[
  { "claim": "verified", "evidence": "artifacts/save-diff.json", "risk": "low", "wording": "maintainer-verified with multi-signal evidence" }
]
```

## Honesty Levels (W/R/E)

- **W (launch/connect)**: tooling/launch verified (SMAPI loads, mod connects); no runtime evidence collected.
- **R (runtime evidence collected)**: evidence artifacts present and valid (save delta, log excerpt, observation notes).
- **E (human judgment)**: assigned only by the maintainer. Never auto-assigned. Requires W + R as prerequisites.

Assignment rule: W → R → E (sequential). E cannot be assigned without both W and R. Auto-promotion to E is prohibited.

## Failure handling

4-state model: `pass` / `fail` / `errored` / `partial` / `skipped`.
- Crash → `errored` with attached artifacts.
- Incomplete signal set → `partial` (never auto-promoted to pass).
- Retry only transient/environmental failures with bounded attempts and per-attempt logging.
- Confirmed flakies quarantined with owner and time-box.

## Save preservation

Verification runs against a disposable save copy. Original save must be byte-identical after verification. Backup/restore of `Saves/<Name>/` and sidecars `soil_health_data_{saveId}.json` / `composting_bins_{saveId}.json`; sandbox via `XDG_CONFIG_HOME` temp dir.