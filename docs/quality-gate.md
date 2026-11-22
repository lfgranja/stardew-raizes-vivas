# Quality Gate — Local Run Guide

## Run analyzers locally
```
dotnet build Stardew-LivingRoots.sln -c Release
```
Must report 0 warnings and 0 errors (TreatWarningsAsErrors).

## Run the analyzer probe
```
bash scripts/quality-gate/analyzer-probe.sh
```
Exits 0 with `PASS` when both probe builds fail with file, line, and rule.

## Run Semgrep
```
.venv-quality-gate/bin/semgrep scan --config p/csharp --error --severity ERROR --metrics off LivingRoots/
```

## Measure coverage
```
dotnet test Stardew-LivingRoots.sln -c Release --collect:"XPlat Code Coverage" --results-directory ./coverage
dotnet reportgenerator -reports:'**/coverage.cobertura.xml' -targetdir:coverage-report -reporttypes:'JsonSummary;TextSummary'
PYTHONPATH=scripts/quality-gate python -m quality_gate coverage --summary coverage-report/Summary.json --baseline .coverage-baseline
```

## Measure duplication
```
npx --yes jscpd@5.4.2 LivingRoots/ --format csharp --min-lines 5 --min-tokens 50 --reporters json --output jscpd-report --silent
PYTHONPATH=scripts/quality-gate python -m quality_gate duplication --report jscpd-report/jscpd-report.json --baseline .jscpd-baseline.json
```

## Ratchet a baseline (only in the improving direction)
```
PYTHONPATH=scripts/quality-gate python -m quality_gate ratchet-coverage --summary <Summary.json> --baseline .coverage-baseline
PYTHONPATH=scripts/quality-gate python -m quality_gate ratchet-duplication --report <jscpd-report.json> --baseline .jscpd-baseline.json
```

## Suppression policy
Targeted suppression requires a rule ID and a documented reason (`.editorconfig` override or `#pragma warning disable <rule> // <rule>: reason`). Blanket `NoWarn`, `nosemgrep` and `.semgrepignore` are forbidden. Root `.editorconfig` must not disable any rule. `.trivyignore` is allowed **only** as a narrow, expiring register: every entry must be a `CVE-\d{4}-\d{4,}` ID and the file must carry `Owner:`, `Justification:` and `Expiry:` headers; an expired register fails the gate and must be re-audited (see `.specify/bugs/quality-gate-ci-failures/`).

## CI trust boundary (verified locally 2026-11-18)

- **Local authority**: `scripts/release-gate.sh` is the authoritative production gate. Its seven steps are formatting, locked restore, Release build, full tests, launch-free smoke, docs markers, and protected paths. Gate 3 (real SMAPI session) is deliberately separate and is NOT implied by a green release gate.
- **Hosted authority**: `sdd-gate` is the only branch-protection required check on `dev` (`strict: true`, zero approving reviews required). It runs `scripts/ci/aggregate-gate.sh` with no .NET toolchain and no secrets, so a green `sdd-gate` does NOT prove that build, test, or formatting passed. Hosted `Build and Test` and `Check Formatting` need the private `BUILDDEPS_TOKEN`; `Quality Gate / build-analysis` needs builddeps plus coverage baselines.
- **Credential-dependent diagnostics**: `build_and_test.yml`, `code-style.yml`, and `quality-gate.yml` now emit `::error::BUILDDEPS_TOKEN is not set; cannot download the private build-deps release.` before first use when the secret is absent or empty. A red check with that message means provisioning, not a source regression. `release.yml` is tag-triggered and owner-gated and is intentionally outside this contract; `verify-scenario.yml` is best-effort (`continue-on-error: true`) and intentionally uses `::warning::`.
- **Enforcement**: `PYTHONPATH=scripts/quality-gate python -m pytest scripts/quality-gate/tests/test_ci_secret_guards.py -q` fails if a future non-best-effort workflow step consumes the token without the guard.
- **[NV] resolvido em 2026-11-22 — mecanismo verificado (atribuição restrita em 05:20Z)**: `gh run view --log-failed` nos runs `44641194770`, `44664287245` e `44664266743` (logs de **19–20/11**, branches `dependabot/...`, dois deles correspondentes a #811/#812) exibe `BUILDDEPS_TOKEN is not set; cannot download the private build-deps release.` no step de busca de assemblies (`exit code 1`). `BUILDDEPS_TOKEN` existe como segredo de repositório, mas `gh api repos/lfgranja/stardew-raizes-vivas/dependabot/secrets` retorna lista vazia: PRs do Dependabot não recebem o segredo. **Não foi feita leitura direta dos runs de 18/11**; a extensão do mecanismo aos PRs #790–#796 da observação original é inferência, não verificação. Evidência em `research/2026-11-22-remote-nv-closure.md`. Qualquer outro status remoto/CI/proteção não observado ao vivo continua **[NV]**.