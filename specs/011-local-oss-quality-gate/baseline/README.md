# Baseline Measurements (T043/T044)

## Coverage (ReportGenerator 5.5.12)
- lines: 79.5%
- branches: 70.7%
- Source: `specs/011-local-oss-quality-gate/baseline/coverage/Summary.json`

## Duplication (jscpd 5.4.2, C# only)
- percentage: 0.41% (5 clones, 54 duplicated lines)
- Source: `specs/011-local-oss-quality-gate/baseline/jscpd/jscpd-report.json`

## Commands
```
dotnet test Stardew-LivingRoots.sln -c Release --collect:"XPlat Code Coverage" --results-directory ./coverage
dotnet reportgenerator -reports:'**/coverage.cobertura.xml' -targetdir:coverage-report -reporttypes:'JsonSummary;TextSummary'
npx --yes jscpd@5.4.2 LivingRoots/ --format csharp --min-lines 5 --min-tokens 50 --reporters json --output jscpd-report --silent
```

## Ratchet
- Coverage floor: 79.5 lines / 70.7 branches (measured baseline).
- Duplication ceiling: 0.42% (rounded up from 0.4104 for pass-at-baseline).