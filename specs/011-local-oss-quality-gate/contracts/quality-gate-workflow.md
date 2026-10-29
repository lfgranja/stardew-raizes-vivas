# Contract: Quality Gate CI Workflow

**Feature**: 011-local-oss-quality-gate
**Date**: 2026-10-29

## Workflow: `.github/workflows/quality-gate.yml`

**Trigger**: `pull_request` (all), `push` to `dev`/`main`, weekly `schedule`, `workflow_dispatch`.

**Runner**: `ubuntu-latest` (standard).

**Game dependency**:
- `static` and `sca`: none. No Stardew Valley, SMAPI, `STARDEW_GAME_PATH`, or game assemblies.
- `build-analysis`: materialises the pinned build-deps release (`lfgranja/stardew-raizes-vivas-builddeps`, tag `gamepath-v1`, asset `gamepath.zip`) with `BUILDDEPS_TOKEN`, exactly as `build_and_test.yml`. Fails closed with an explicit message when the token is unavailable.
- `codeql`: none; `build-mode: none`.

## Jobs and steps (contract)

| # | Job | Step | Tool | Blocking? | Notes |
|---|---|---|---|---|---|
| 1 | static | Checkout | actions/checkout | yes | |
| 2 | static | Setup Python | actions/setup-python | yes | 3.12 |
| 3 | static | Install CI deps | pip | yes | `scripts/quality-gate/requirements-ci.txt` pins `semgrep==1.182.1` |
| 4 | static | Semgrep | Semgrep CLI | yes | `semgrep scan --config p/csharp --error --severity ERROR --metrics off --sarif --output semgrep.sarif LivingRoots/`; ~10s |
| 5 | static | Upload SARIF | codeql-action/upload-sarif | no | Only when the PR head is in this repository, or on push/schedule; fork PRs have a read-only `GITHUB_TOKEN` |
| 6 | static | jscpd | `npx jscpd@5.4.2` | yes | `LivingRoots/ --format csharp --min-lines 5 --min-tokens 50`; JSON report |
| 7 | static | Duplication gate | `python -m quality_gate duplication` | yes | Fails if total C# duplication > ceiling; diagnostics include both clone sides |
| 8 | sca | Checkout | actions/checkout | yes | |
| 9 | sca | Setup Python | actions/setup-python | yes | 3.12 |
| 10 | sca | Install CI deps | pip | yes | Uses the same pinned requirements |
| 11 | sca | Trivy | `aquasecurity/trivy-action` v0.36.0 | yes | `scan-type: fs`, `scanners: vuln`, `severity: HIGH,CRITICAL`, `exit-code: 0`, JSON output; the Python gate owns the exit code |
| 12 | sca | Security gate | `python -m quality_gate security` | yes | Fails on HIGH/CRITICAL with package/CVE/target diagnostics |
| 13 | build-analysis | Fetch build deps | `gh release download` | yes | `BUILDDEPS_TOKEN`; same preflight as `build_and_test.yml` |
| 14 | build-analysis | Restore | `dotnet restore --locked-mode` | yes | Lock files committed |
| 15 | build-analysis | Build | `dotnet build --no-restore -c Release` | yes | Analyzers run; warnings-as-errors |
| 16 | build-analysis | Test + coverage | `dotnet test --no-build -c Release --collect:"XPlat Code Coverage"` | yes | Coverlet |
| 17 | build-analysis | ReportGenerator | `dotnet reportgenerator` | yes | `-reporttypes:'JsonSummary;TextSummary'`; quoted glob `'**/coverage.cobertura.xml'` |
| 18 | build-analysis | Coverage gate | `python -m quality_gate coverage` | yes | `Summary.json` at `summary.linecoverage` / `summary.branchcoverage`; 0.05 epsilon |
| 19 | codeql | Init | `github/codeql-action/init@v4` | scheduled only | `languages: csharp`, `build-mode: none` |
| 20 | codeql | Analyze | `github/codeql-action/analyze@v4` | scheduled only | SARIF to Security tab; no token/game assemblies |
| 21 | gate | Aggregate | shell | yes | `needs: [static, sca, build-analysis]`, `if: always()`, fails closed if any needed job is not `success`; CodeQL is not in `needs` |

## Thresholds (initial, from the measured baseline)

| Metric | Direction | Initial threshold | Source | Ratchet |
|---|---|---|---|---|
| Line coverage | min | Measured in T043 (current estimate 79.1%) | ReportGenerator `summary.linecoverage` | up |
| Branch coverage | min | Measured in T043 (current estimate 70.4%) | ReportGenerator `summary.branchcoverage` | up |
| Duplication (C# lines) | max | Measured in T043 (current estimate 0.48%) | jscpd `statistics.total.percentage` with `--format csharp` | down |
| Security findings (HIGH/CRITICAL) | max | 0 | Trivy against committed lock files | n/a |
| Reliability (analyzer warnings) | max | 0 | `TreatWarningsAsErrors` | n/a |

The committed `.coverage-baseline` / `.jscpd-baseline.json` values are authoritative. The estimates above are context only and MUST be replaced by the T043 measurements.

## Exit codes / failure behavior

- Gate CLI: `0` pass, `1` regression/finding, `2` configuration error.
- Any failing blocking job fails `gate`; branch protection can require the `Quality Gate / gate` check.
- Code-level findings include file/line; SCA findings include package/CVE/target; coverage findings name the lowest-covered classes; duplication findings include both clone sides; configuration errors include an explicit message.
- Suppressions must be documented (FR-012).

## Permissions

- Workflow default: `contents: read`.
- `static` (SARIF upload): `security-events: write` on same-repo runs; the upload step is skipped on fork PRs.
- `codeql`: `security-events: write`.
- All other jobs: `contents: read`.

## Inputs / secrets

| Job | Secret | Required? |
|---|---|---|
| static | none | — |
| sca | none | — |
| build-analysis | `BUILDDEPS_TOKEN` | yes; fail closed without it |
| codeql | none | — (uses `security-events: write` for SARIF upload) |

Fork PRs: `static` and `sca` run normally. `build-analysis` cannot run without `BUILDDEPS_TOKEN`; the aggregate gate treats that non-success as a failure, and the maintainer must re-run on a same-repo branch before merge. SARIF upload is skipped on fork PRs because the token is read-only.

## Out of scope for this contract

- Mutation testing (Stryker.NET) — optional, separate.
- Inline PR comment decoration — not required (build-fail sufficient).
- New-code-only scoping — whole-repo scans are used and the parity gap is documented.
