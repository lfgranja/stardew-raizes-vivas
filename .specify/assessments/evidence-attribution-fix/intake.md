# Idea Intake: Correção de atribuição de evidência e SemVer

- **Slug**: evidence-attribution-fix
- **Created**: 2026-11-22T05:19:30Z
- **Source**: revisão crítica pós-entrega + leitura dos artefatos mesclados
- **Type**: fix

## Idea (as captured)

> "SemVer ausente no release 032; os logs puxados são de 19–20/11 em branches #811/#812, não necessariamente os PRs #790–#796 de 18/11."

## Restated

Restaurar o SemVer `0.1.7` no release 032 e restringir a afirmação do `[NV]` ao que os logs realmente provam: o mecanismo `BUILDDEPS_TOKEN` ausente no escopo Dependabot, observado nos runs de 19–20/11; a extensão aos PRs de 18/11 é inferência do mesmo mecanismo.

## Origin & Context

- **Raised by**: revisão de evidências após merges #817/#818/#819.
- **Trigger**: leitura direta de `.specify/releases/032-*/release.md` (sem SemVer) e `gh run list` (sem runs de 18/11 no limite consultado).

## First-Glance Unknowns

- Nenhuma bloqueante.
