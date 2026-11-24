# Data Model: Baseline Artifacts

## Coverage Baseline (.coverage-baseline)
```text
lines=<float>      # Line coverage percentage (1 decimal)
branches=<float>   # Branch coverage percentage (1 decimal)
# measuredAt: ISO8601
# commit: git short SHA
# scope: description
```

## Duplication Baseline (.jscpd-baseline.json)
```json
{
  "percentage": <float>,  // 2 decimals
  "duplicates": <int>,    // number of clone groups
  "lines": <int>,         // duplicated lines
  "measuredAt": "<date>",
  "commit": "<sha>",
  "scope": "<description>"
}
```

## Evidence Reports (specs/011-local-oss-quality-gate/baseline/)
- coverage/Summary.json - ReportGenerator JsonSummary
- jscpd/jscpd-report.json - jscpd full report
