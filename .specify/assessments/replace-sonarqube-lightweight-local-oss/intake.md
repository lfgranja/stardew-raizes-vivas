# Idea Intake: Substituir avaliações do SonarQube/SonarCloud por soluções leves, locais, nativas, open-source e gratuitas

- **Slug**: replace-sonarqube-lightweight-local-oss
- **Created**: 2026-10-29
- **Source**: pasted text (research provided by the user) + run https://github.com/lfgranja/stardew-raizes-vivas/runs/123455233388 (host: github.com — allowlisted)
- **Type**: exploration

## Idea (as captured)

> "É possível, viável, interessante, desejável substituir as avaliações do SonarQube por soluções LEVES, locais, nativas, opensource e gratuitas?"
>
> Contexto de origem: run do SonarCloud Code Analysis em `lfgranja/stardew-raizes-vivas` (https://github.com/lfgranja/stardew-raizes-vivas/runs/123455233388) com falha de Quality Gate — "C Reliability Rating on New Code (required ≥ A)" — após o merge da PR #518 em `dev`.
>
> A pesquisa capturada cobre, de forma aprofundada e multilíngue: (1) o que o SonarQube/SonarCloud entrega (Reliability, Security, Maintainability, Duplicação, Cobertura, Quality Gate, PR decoration); (2) uma stack OSS leve/local/nativa/gratuita por dimensão — analisadores Roslyn (`Microsoft.CodeAnalysis.NetAnalyzers`, `SonarAnalyzer.CSharp`, StyleCop, Roslynator, Meziantou) com `TreatWarningsAsErrors`; SAST (Semgrep CE, CodeQL, OWASP Dependency-Check, Trivy, Gitleaks); duplicação (jscpd, PMD CPD); cobertura (Coverlet + ReportGenerator); mutation testing (Stryker.NET); formatação (`dotnet format`, CSharpier); (3) comparação SonarCloud (SaaS, gratuito para repos públicos) vs. stack local OSS; (4) desejabilidade para este projeto; (5) plano de migração em 4 fases; (6) veredito final (possível/viável/interessante/desejável, com ressalvas) e referências.

## Restated

Avaliar se as análises de qualidade/código atualmente feitas pelo SonarCloud no repositório `stardew-raizes-vivas` podem ser substituídas por um conjunto de ferramentas leves, locais, nativas, open-source e gratuitas (principalmente analisadores Roslyn para .NET, mais SAST, cobertura e duplicação), e sob quais condições isso é viável e desejável.

## Origin & Context

- **Raised by**: maintainer do repositório (`lfgranja`), via sessão de pesquisa/assessment.
- **Trigger**: falha recorrente do Quality Gate do SonarCloud por "C Reliability Rating on New Code (required ≥ A)" em PRs/merges recentes (ex.: run 123455233388 após merge da PR #518), somada à dependência de um serviço de nuvem e à natureza "dormant" do workflow Build and Test (que exige `STARDEW_GAME_PATH`).

## First-Glance Unknowns

- [NEEDS CLARIFICATION: quantos warnings dos analisadores Roslyn virariam erros ao habilitar `TreatWarningsAsErrors` no `LivingRoots.csproj`/`LivingRoots.Tests.csproj`?]
- [NEEDS CLARIFICATION: o gate de Reliability do SonarCloud é a única barreira de qualidade hoje, ou há outros checks obrigatórios (protected paths, formatação)?]
- [NEEDS CLARIFICATION: a equipe está disposta a manter uma stack composta (Semgrep + CodeQL + Coverlet + jscpd) em vez de um único SaaS?]
- [NEEDS CLARIFICATION: é necessário manter histórico/tendências de qualidade e PR decoration, ou basta falhar o build?]
- [NEEDS CLARIFICATION: qual threshold de cobertura e de duplicação seria aceitável para este projeto?]
- [NEEDS CLARIFICATION: a remoção do SonarCloud deve ser total ou manter SonarCloud em paralelo como fonte de verdade secundária?]