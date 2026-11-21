# Concept: Fechar dívida de artefatos e evidência

- **Slug**: artifact-evidence-closure
- **Created**: 2026-11-20
- **Recommended option**: Option A — Validar e reconciliar artefatos existentes

## Options

### Option A — Validar e reconciliar artefatos existentes

- **Sketch**: corrigir os dois candidatos archify até `validate` passar, manter as âncoras documentais criadas com fatos locais e registrar em `STATE.md`/`specs` uma revisão de qualidade de requisitos auditável. Nenhum comportamento do mod muda.
- **Appetite**: small (horas). Confiança: medium — a conversão de schema do candidato antigo pode exigir mais trabalho do que o estimado.
- **Trade-offs**: win — 5/5 candidatos validam e a dívida é fechada com evidência; sacrifice — impacto baixo, nenhum PR desbloqueado. Risco: perda de detalhes do layout ao converter schema.
- **Rabbit holes**: reconstruir o `candidate.json` de `workflow-verification-pipeline` a partir do HTML antigo; auditar todos os resíduos de checkboxes de specs antigas.

### Option B — Apenas documentar a dívida e não corrigir

- **Sketch**: registrar em `STATE.md` que os dois candidatos permanecem inválidos por serem artefatos gerados/gitignored, sem alterá-los.
- **Appetite**: small (minutos). Confiança: high.
- **Trade-offs**: win — custo quase zero; sacrifice — não atinge a métrica 5/5 e deixa a dívida aberta indefinidamente.
- **Rabbit holes**: nenhum; risco de parecer "concluído" sem evidência.

### Option C — Regenerar todos os 5 artefatos do zero

- **Sketch**: recriar candidate.json de todos os diagramas com o schema atual e regenerar HTMLs em `docs/architecture/`.
- **Appetite**: medium (dias). Confiança: low — `docs/architecture/` não existe no alvo e a fonte de verdade dos diagramas não está clara.
- **Trade-offs**: win — padronização completa; sacrifice — alto risco de perder conteúdo e de introduzir HTMLs não solicitados.
- **Rabbit holes**: decisão de layout, aprovação visual humana, políticas de paths protegidos.

## Recommendation

Option A. É a única que atinge as métricas de sucesso (5/5 `ok:true`, âncoras presentes, zero diff em `LivingRoots/**`) com risco contido e sem autorização owner/humana; Option B não resolve a dívida e Option C amplia escopo sem necessidade.

## Out of Scope (for the recommended option)

- Código de jogo, Gate 3 de jogo real, SC-004, D1, atestação E, tag/release, segredos, aprovação de revisor, merge de PR com check vermelho.
- Regenerar HTMLs em `docs/architecture/` (o diretório não existe no alvo e nenhum HTML está stale em relação ao HEAD).

## Assumptions to Validate

- Os candidatos `.archify/` são locais/gitignored; correções não entram no diff versionado.
- A conversão do schema antigo preserva informação suficiente para `validate` passar sem revisão visual humana.
- Checkboxes de specs com evidência em `STATE.md`/`validate.md` podem ser marcados como revisão de qualidade, não como implementação.

