# Idea Intake: SonarCloud to local gate baseline readiness

- **Slug**: sonarcloud-to-local-gate-baseline
- **Created**: 2026-11-01T07:30:00Z
- **Source**: pasted text
- **Type**: improvement

## Idea (as captured)

> Código mais limpo agora: zeramos todos os issues OPEN/CONFIRMED do SonarCloud no dev. Com menos code smells, vulnerabilidades e problemas de complexidade, o baseline para o quality gate local fica muito mais realista e sustentável.
> Correções já seguem o espírito da feature 011: ao reduzir complexidade cognitiva (S3776), remover checks desnecessários (S2589), corrigir problemas de segurança (S6506), etc., já estamos alinhados com o objetivo de "build-fail com diagnósticos acionáveis".
> Menor volume de remediação: a Fase 3 da feature 011 (US1 - reliability/analyzers com TreatWarningsAsErrors) terá muito menos trabalho para chegar a um build limpo.
> Por que não totalmente
> O gate local tem regras próprias: o quality gate da spec 011 não é um espelho 1:1 do SonarCloud. Ele usa Roslyn analyzers (SonarAnalyzer.CSharp + .NET analyzers), Semgrep, Trivy, Coverlet, jscpd com thresholds baseados em baseline próprio. Ter zero issues no Sonar não significa que vai passar no gate local sem ajustes.
> Precisamos capturar o baseline agora: conforme FR-009/FR-017 da spec, precisamos medir coverage e duplicação no estado atual (após essas correções) para setar thresholds com ratchet. Os valores podem ter mudado com nossas refatorações.
> Fase 2 ainda é bloqueante: os testes TDD do gate precisam estar RED commitados antes de implementar os módulos Python/YAML. Mesmo com código limpo, essa infraestrutura precisa ser construída conforme o plano.
> Análises podem divergir: alguns rulesets diferem entre SonarCloud e os analyzers locais. Podemos encontrar novos warnings com AnalysisLevel=6.0-recommended + TreatWarningsAsErrors=true que o SonarCloud não reportava da mesma forma.
> Recomendação
> Sim, temos um baseline muito mais sólido agora. O ideal é:
> Capturar os baselines frescos (coverage via ReportGenerator, duplicação via jscpd) com o código atual do dev pós-correções
> Seguir rigorosamente o plano da 011: Fase 2 (testes RED) → Fase 3 (analyzers) → etc.
> Tratar qualquer novo warning encontrado pelos analyzers locais como parte do trabalho da feature 011, não como regressão
> Ou seja: zerar o SonarCloud nos dá um ponto de partida limpo para implementar o gate local com thresholds justos baseados no estado atual, ao invés de ter que remediar centenas de issues enquanto também construímos a infraestrutura.

## Restated

With all OPEN/CONFIRMED SonarCloud issues cleared on dev, the codebase is in a much cleaner state for implementing the local OSS quality gate (spec 011). While this reduces remediation work for the analyzer phase, the gate has its own rules and requires fresh coverage/duplication baselines measured now; we should follow the 011 plan (RED tests first, then analyzers) and treat any new analyzer warnings as part of that work.

## Origin & Context

- **Raised by**: maintainer (Luis)
- **Trigger**: Completed zeroing SonarCloud issues on branch dev via bug workflow (PRs merged)

## First-Glance Unknowns

- [NEEDS CLARIFICATION: What are the exact fresh coverage and duplication baselines after the recent refactorings?]
- [NEEDS CLARIFICATION: Will enabling AnalysisLevel=6.0-recommended + TreatWarningsAsErrors introduce new warnings not seen in SonarCloud?]
- [NEEDS CLARIFICATION: Is Phase 2 (TDD RED) already complete on the 011 branch or still pending?]
- [NEEDS CLARIFICATION: Are the baseline files (.coverage-baseline, .jscpd-baseline.json) already committed on dev/main or need to be created?]
