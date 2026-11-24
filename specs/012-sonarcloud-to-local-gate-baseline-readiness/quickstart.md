# Quickstart: Baseline Capture

## Prerequisites
- .NET SDK 6.0.428
- ReportGenerator 5.5.12
- Node.js (for jscpd via npx)
- Python venv for quality gate

## Steps

### 1. Run tests with coverage
```bash
dotnet test Stardew-LivingRoots.sln --no-restore --configuration Release --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

### 2. Generate coverage summary
```bash
export PATH="$HOME/.dotnet/tools:$PATH"
DOTNET_ROOT=$(mise where dotnet@6.0.428) PATH=$DOTNET_ROOT:$PATH \
  reportgenerator -reports:'TestResults/**/coverage.cobertura.xml' -targetdir:coverage-report -reporttypes:'JsonSummary;TextSummary'
```

### 3. Generate duplication report
```bash
npx --yes jscpd@5.4.2 LivingRoots/ --format csharp --min-lines 5 --min-tokens 50 --reporters json --output jscpd-report --silent
```

### 4. Update baselines
```bash
# Extract values and update .coverage-baseline and .jscpd-baseline.json
# Copy reports to specs/011-local-oss-quality-gate/baseline/
```

### 5. Verify
```bash
PYTHONPATH=scripts/quality-gate .venv-quality-gate/bin/python -m quality_gate coverage --summary coverage-report/Summary.json --baseline .coverage-baseline
PYTHONPATH=scripts/quality-gate .venv-quality-gate/bin/python -m quality_gate duplication --report jscpd-report/jscpd-report.json --baseline .jscpd-baseline.json
```
