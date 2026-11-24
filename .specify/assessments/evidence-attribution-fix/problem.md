# Problem Definition: Correção de atribuição de evidência e SemVer

- **Slug**: evidence-attribution-fix
- **Created**: 2026-11-22T05:19:30Z
- **Inputs used**: intake.md | research.md

## Problem Statement

O release 032 mesclado perdeu sua linha SemVer e a nota de fechamento do `[NV]` atribui aos PRs #790–#796 logs que foram lidos de runs de 19–20/11, alguns de PRs diferentes; leitores podem superinterpretar a evidência.

## Goals

- Restaurar `0.1.7` no release 032.
- Explicitar que os logs verificados são de 19–20/11 e que a extensão a 18/11 é inferência do mesmo mecanismo.

## Non-Goals

- Reabrir PRs, reescrever código, cenários, workflows, contratos ou artefatos históricos além das âncoras nomeadas.

## Success Metrics

- SemVer presente no release 032 (baseline: ausente).
- 0 afirmações de verificação direta dos runs de 18/11 (baseline: implícita).

## Cost of Inaction

Trilha SDD com metadado incompleto e afirmação remota superatribuída.

## Open Questions

- Nenhuma.
