# Concept: Fechar [NV] de causa-raiz remota dos PRs #790–#796

- **Slug**: nv-remote-log-closure
- **Created**: 2026-11-22T03:34:24Z
- **Recommended option**: Option A

## Options

### Option A — Âncora datada de evidência em artefatos novos

- **Sketch**: um documento de pesquisa novo registra os comandos, run IDs, timestamps e trechos de log lidos ao vivo; as âncoras de estado (`STATE.md`, `docs/PROGRESSO.md`, `docs/research/index.md`, `docs/quality-gate.md`) ganham notas datadas apontando para ele. Nada histórico é reescrito.
- **Appetite**: small
- **Trade-offs**: converte um `[NV]` específico em fato verificável e mantém o princípio de que todo status remoto não observado ao vivo continua `[NV]`; não desfaz a natureza humano-gated dos demais. A evidência depende da retenção dos logs pelo GitHub, mitigada pela citação literal no artefato.
- **Rabbit holes**: tentar atualizar todos os 66 arquivos com `[NV]`; reescrever assessments históricos; promover `verified` humano a partir de log de CI.

### Option B — Reescrever os artefatos históricos 025/030

- **Sketch**: editar `decision.md`, `intake.md`, `research.md`, `validate.md` e `release.md` antigos para remover `[NV]` onde a causa agora é conhecida.
- **Appetite**: medium
- **Trade-offs**: deixa o histórico internamente consistente, mas viola append-only e reescreve trabalho já mesclado; perde o registro honesto do que era conhecido na data.
- **Rabbit holes**: reconciliações infinitas entre versões dos artefatos; risco de parecer revisão de 025/030 sem novo ciclo.

### Option C — Não fazer nada

- **Sketch**: manter o `[NV]` e re-leitura dos logs em cada sessão.
- **Appetite**: small
- **Trade-offs**: custo zero agora, mas a dívida informativa se repete e o pedido explícito do prompt fica incompleto.
- **Rabbit holes**: nenhum.

## Recommendation

**Option A**. É a única que resolve o pedido com evidência determinística, sem reescrita histórica e sem tocar em segredos, workflows ou paths protegidos. Os critérios de sucesso são observáveis por leitura de arquivo e por gates locais já existentes.

## Out of Scope (for the recommended option)

- D1, atestação humana E de Gate 3, SC-004, tag/release pública, `RUNNER_AUDIT_STORE`, segredos do Dependabot, aprovação de revisor distinto.
- Mudanças em `.github/workflows/**`, `scripts/ci/**` ou contratos.
- Reabertura/reexecução de PRs encerrados.

## Assumptions to Validate

- Os run IDs citados continuam acessíveis via `gh` (validado em 2026-11-22T03:32–03:34Z).
- `aggregate-gate.sh` aceita novos artefatos SDD sem exigir campos ausentes (a validar no implement).
