# Idea Intake: Alinhar bandas 31/71 nos cenários ativos 004 e 005

- **Slug**: band-scale-scenario-drift
- **Created**: 2026-11-22T03:49:24Z
- **Source**: pasted text (prompt de orquestração 80/20) + repo path `.specify/memory/STATE.md`
- **Type**: fix

## Idea (as captured)

> "Dívida registrada (não alterada): limites legados em `compost-growth-healthy`/`soil-health-growth` (`min: 67`) e fixtures de `010-*` (`34/67`); escopo deste ciclo corrigiu apenas a deriva confirmada."

## Restated

Corrigir a deriva entre as bandas de saúde do solo implementadas (`[0,31)`, `[31,71)`, `[71,100]`) e os dois cenários Gate 3 ativos que ainda descrevem Healthy como `[67,100)`, sem reescrever fixtures contratuais históricas nem código de jogo.

## Origin & Context

- **Raised by**: ciclo 030 (dívida registrada em `STATE.md` e `specs/030-roteiro-verificacao-funcional/research.md`); retomado pelo prompt de 2026-11-21.
- **Trigger**: investigação read-only de 2026-11-22 (`BandDebtScout`) confirmou 4 linhas em 2 cenários ativos e valor observado 80 (`specs/004/test.md:24`, `specs/005/test.md:24`).

## First-Glance Unknowns

- [NEEDS CLARIFICATION: alterar o cenário exige reexecução Gate 3?] — respondido na research: sim, para ambos.
- [NEEDS CLARIFICATION: renomear fixtures `boundary-33-poor`/`boundary-67-healthy`?] — respondido na research: não; histórico contratual.
