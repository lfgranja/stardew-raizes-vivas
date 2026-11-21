# Data Model: Fechar dívida de artefatos e evidência

## CandidateDiagram

- **Representação**: diretório em `.archify/` contendo `candidate.json`.
- **Atributos**: `tipo` (architecture/lifecycle/workflow), `fases` (intervalo colunas), `nós`, `arestas`, `mainPath`, resultado da verificação (`ok:true|false`).
- **Regras de validação**: fases disjuntas; arestas do `mainPath` devem existir; corredores de arestas não ambíguos; estrutura conforme o schema atual (`lanes`/`nodes`/`edges` para workflow).
- **Transição de estado**: `inválido` → `válido` somente após verificação determinística com sucesso.

## VerifiedStateNote

- **Representação**: bloco datado em `docs/PROGRESSO.md`, `docs/research/index.md`, `OBJETIVO.md` e `ROADMAP.md`.
- **Atributos**: data, âncora do repositório, comando executado, resultado, lista de `[NV]`.
- **Regras**: append-only; nunca reescreve histórico; toda afirmação remota marcada `[NV]`; nenhuma promoção a `verified` humano.

## RequirementReviewResidue

- **Representação**: item de checklist em `specs/*/tasks.md` com registro em `.specify/memory/STATE.md` ou artefatos de validação/release.
- **Atributos**: identificação do item, fonte de evidência, classificação (`revisão de qualidade` | `pendente human-gated`).
- **Regras**: sem fonte auditável → permanece pendente; dependente de participante humano/D1/atestado E/owner → permanece `[NV]`.

## Relações

- `VerifiedStateNote` referencia `CandidateDiagram` (resultado da verificação) e `RequirementReviewResidue` (evidência citada).
- Nenhuma entidade persiste estado de farm; nenhuma relação com domínio do jogo.

