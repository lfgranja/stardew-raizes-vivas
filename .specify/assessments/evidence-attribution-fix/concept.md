# Concept: Correção de atribuição de evidência e SemVer

- **Slug**: evidence-attribution-fix
- **Created**: 2026-11-22T05:19:30Z
- **Recommended option**: Option A

## Options

### Option A — Correção append-only nas âncoras + SemVer

- **Sketch**: restaurar a linha SemVer e acrescentar nota datada corrigindo a atribuição em `research/2026-11-22-remote-nv-closure.md`, `STATE.md`, `PROGRESSO.md` e `docs/quality-gate.md`.
- **Appetite**: small
- **Trade-offs**: honestidade preservada; nenhuma reescrita de histórico além da linha ausente.
- **Rabbit holes**: apagar a resolução do `[NV]` — não fazer.

### Option B — Reescrever os artefatos mesclados apagando a resolução

- **Sketch**: remover a nota de resolução até logs de 18/11 serem obtidos.
- **Appetite**: small
- **Trade-offs**: perde evidência válida do mecanismo; rejeitado.

## Recommendation

**Option A**.

## Out of Scope

Código, cenários, workflows, contratos, PRs antigos, owner/human-gated.

## Assumptions to Validate

`aggregate-gate.sh` aceita R4/R7 do ciclo 034.
