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
Targeted suppression requires a rule ID and a documented reason (`.editorconfig` override or `#pragma warning disable <rule> // <rule>: reason`). Blanket `NoWarn` and `nosemgrep`/`.semgrepignore`/`.trivyignore` are forbidden. Root `.editorconfig` must not disable any rule.