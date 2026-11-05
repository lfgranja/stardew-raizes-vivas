# SMAPI Log Capture Contract

**Feature**: 016-gate3-runner-hardening
**Scope**: Capture of the SMAPI log into the evidence bundle (`scripts/lib/signals.sh`: `signals_smapi_log_excerpt`, `signals_capture`, `signals_process`).
**Related**: `specs/010-headless-verification-runner/contracts/evidence-bundle.md`, `specs/010-headless-verification-runner/contracts/exit-codes.md` (code 19).

## Default: full copy

The default capture level is **full**. The bundle artifact `artifacts/smapi-log.txt` MUST be a byte-for-byte copy of the source SMAPI log (`SMAPI-latest.txt` or the path resolved by `RUNNER_SMAPI_LOG`). No filtering, truncation, or line selection is applied.

`signals_smapi_log_excerpt` retains its name for contract compatibility but performs a full copy (`cp -f`). The function name is historical; its behavior is full capture. Any reintroduction of the previous three-pattern filter (elapsed/DayEnding/host-guard only) is a regression and must fail the automated test.

## Opt-in excerpting

Excerpting is permitted only as an explicit, documented, opt-in capture level selected by a documented option (for example a scenario field or CLI flag). It is never a silent filter:

- The default remains `full`; an excerpt is produced only when the opt-in is explicitly requested.
- The opt-in level and its parameters MUST be recorded in the run context / scenario so the bundle is reproducible.
- The full log is ALWAYS retained as a forensic secondary artifact even when an excerpt is produced. The excerpt is additive, not a replacement.
- A bundle whose `artifacts/smapi-log.txt` is a filtered excerpt rather than the full log is rejected or flagged by the evidence verifier.

## Size caps for LLM diagnosis

Size caps (via density ranking) apply **only** to LLM diagnosis artifacts, never to the forensic log:

- The forensic artifact `artifacts/smapi-log.txt` is uncapped and complete.
- A derived, capped excerpt may be produced for LLM diagnosis (for example `artifacts/smapi-log.llm.txt` or a documented equivalent) using density ranking to select the most informative lines.
- The full log remains the secondary forensic artifact referenced by the diagnosis artifact.
- Caps are applied only to the derived artifact; they never alter or replace `artifacts/smapi-log.txt`.

## Artifact names

- `artifacts/smapi-log.txt` — mandatory signal artifact; full copy; must be non-empty for a complete signal set.
- `artifacts/smapi-log.excerpt.txt` (or documented equivalent) — optional, opt-in derived excerpt.
- `artifacts/smapi-log.llm.txt` (or documented equivalent) — optional, capped LLM-diagnosis derivative.

The signal-presence checks (`signals_all_present`, `signals_require_expected_signals`) require `artifacts/smapi-log.txt` to be non-empty. Anomaly reports (`anomaly-report*.json`) never satisfy this requirement.

## Exit codes

- Missing or unreadable source SMAPI log: the capture function returns `19` (`EXIT_ERRORED`); a bundle missing `artifacts/smapi-log.txt` is rejected by the verifier with `19`.
- A bundle presenting a filtered excerpt as `artifacts/smapi-log.txt` is rejected or flagged (verifier `19` or policy `21`, per the documented honesty level).

## Test contract

The automated test must fail when the excerpt filter is reintroduced: `signals_smapi_log_excerpt` over a multi-line log containing non-target lines must reproduce those lines byte-for-byte in the output.