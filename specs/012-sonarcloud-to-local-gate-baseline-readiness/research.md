# Research: SonarCloud to Local Gate Baseline Readiness

## Decisions

### Decision 1: Use ReportGenerator 5.5.12 to generate coverage summary
- **Decision**: Install and use ReportGenerator 5.5.12 (global tool via mise/dotnet 6) to convert cobertura.xml to JsonSummary format expected by quality gate
- **Rationale**: Gate expects ReportGenerator schema exactly; lowest risk, matches pinned version in dotnet-tools.json
- **Alternatives considered**: Hand-roll Cobertura→JsonSummary converter (feasible but more work, need to match schema exactly)

### Decision 2: Follow TDD evidence from red-phase.md
- **Decision**: Accept existing red-phase.md as evidence of RED→GREEN progression
- **Rationale**: Tests exist and implementation complete; red-phase.md documents intentional RED state at 4111bb0
- **Alternatives considered**: Recreate RED state on separate branch (unnecessary)

### Decision 3: Fresh baselines captured
- **Decision**: Capture baselines with current tool versions against current HEAD
- **Rationale**: Post-fix state different from previous baselines; need fair thresholds per FR-009/FR-017
- **Results**: Coverage 76.9% lines / 67.6% branches, Duplication 0.49% (6 clones, 67 lines)
