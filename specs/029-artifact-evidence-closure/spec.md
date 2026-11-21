# Feature Specification: Fechar dívida de artefatos e evidência

**Feature Branch**: `029-artifact-evidence-closure`

**Created**: 2026-11-20

**Status**: Draft

**Input**: Handoff do assessment `artifact-evidence-closure` (verdict go, Option A) e decisão 80/20 do Bloco 5 de 2026-11-20.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Diagramas validados (Priority: P1)

Como mantenedor, quero que todos os diagramas de arquitetura candidatos passem na verificação determinística, para poder regenerá-los sem medo de perder conteúdo ou propagar layout inválido.

**Why this priority**: É a única falha reproduzida por comando determinístico neste ciclo (2 de 5 candidatos `ok:false`), e ataca diretamente a dívida registrada desde o ciclo 026.

**Independent Test**: Executar a verificação dos 5 candidatos e observar 5 resultados positivos; nenhuma outra história precisa ser executada para este teste.

**Acceptance Scenarios**:

1. **Given** os cinco diretórios candidatos em `.archify/`, **When** a verificação determinística de cada um é executada, **Then** os cinco retornam sucesso e nenhum erro de sobreposição de fases, corredor ambíguo ou schema é reportado.
2. **Given** o candidato `workflow-heirloom-pipeline` com fases `growth`/`harvest`/`sync` sobrepostas, **When** sua descrição é ajustada, **Then** as fases ficam disjuntas, o corredor ambíguo é removido e o `mainPath` passa a referir arestas existentes.
3. **Given** o candidato `workflow-verification-pipeline` com estrutura de schema antiga, **When** sua descrição é convertida, **Then** ela passa a usar a estrutura aceita pela verificação atual sem perder os nós e conexões existentes.

---

### User Story 2 - Âncoras documentais confiáveis (Priority: P2)

Como leitor ou revisor, quero encontrar um registro de progresso e um índice de pesquisa no repositório, com fatos locais separados de afirmações remotas não confirmadas.

**Why this priority**: Sem essas âncoras, o leitor não consegue distinguir evidência executada de status remoto presumido; o custo é baixo e o benefício é imediato.

**Independent Test**: Listar os arquivos de âncora e verificar que contêm nota datada e marcação explícita de itens não verificados.

**Acceptance Scenarios**:

1. **Given** `docs/PROGRESSO.md` e `docs/research/index.md` existem, **When** um leitor os abre, **Then** encontra data, âncora do repositório, contagens obtidas por comando e marcação `[NV]` nas afirmações remotas.
2. **Given** a documentação existente `OBJETIVO.md` e `ROADMAP.md`, **When** o leitor consulta o estado mais recente, **Then** encontra nota datada de 2026-11-20 sem apagar o histórico anterior.

---

### User Story 3 - Resíduos de evidência revisados sem supervisão humana simulada (Priority: P3)

Como revisor, quero que os resíduos documentais com evidência já registrada sejam revisados explicitamente como qualidade de requisitos, sem transformar evidência de máquina em atestação humana.

**Why this priority**: Fecha a dívida de checkboxes sem inventar implementação ou atestado; depende da História 1 apenas para o registro final.

**Independent Test**: Comparar as notas de revisão com as fontes de evidência citadas e confirmar que nenhuma afirmação `verified`/D1/SC-004 foi promovida.

**Acceptance Scenarios**:

1. **Given** checkboxes abertos em specs antigas, **When** a revisão de qualidade é registrada, **Then** cada item revisado cita a fonte de evidência e é rotulado como revisão de requisitos, não como nova implementação.
2. **Given** itens que dependem de participante humano, sessão multiplayer ao vivo ou aprovação de owner, **When** a revisão é feita, **Then** permanecem pendentes e marcados `[NV]`/human-gated.

---

### Edge Cases

- Se um candidato `.archify/` não puder ser convertido sem perder informação, ele é registrado como dívida pendente com a razão, não alterado artificialmente para passar.
- Se `docs/architecture/` não existir, nenhum HTML é criado apenas para satisfazer a história 1; a verificação incide sobre os candidatos existentes.
- Se uma nota de revisão não tiver fonte de evidência auditável, o item permanece aberto.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema de verificação de diagramas DEVE reportar sucesso para os cinco candidatos em `.archify/` quando executado sobre o estado final do ciclo.
- **FR-002**: A descrição do candidato `workflow-heirloom-pipeline` DEVE definir fases disjuntas, rotas sem corredor ambíguo e `mainPath` cujas arestas existam.
- **FR-003**: A descrição do candidato `workflow-verification-pipeline` DEVE usar a estrutura aceita pela verificação atual, preservando os nós e conexões do conteúdo original.
- **FR-004**: As âncoras `docs/PROGRESSO.md` e `docs/research/index.md` DEVEM existir e conter nota datada de estado verificado com contagens obtidas por comando.
- **FR-005**: Toda afirmação sobre estado remoto, CI, proteção de branch ou aprovação DEVE ser marcada `[NV]` até observação ao vivo.
- **FR-006**: O registro de revisão de resíduos DEVE citar a fonte de evidência de cada item e classificá-lo como revisão de qualidade de requisitos.
- **FR-007**: Nenhum item dependente de participante humano, sessão multiplayer ao vivo, atestado E, segredo de owner ou aprovação de revisor DEVE ser marcado como concluído por este ciclo.
- **FR-008**: O ciclo NÃO DEVE modificar arquivos sob `LivingRoots/`.
- **FR-009**: O gate autoritativo do alvo DEVE permanecer verde após as alterações deste ciclo.

### Key Entities *(include if feature involves data)*

- **Candidato de diagrama**: diretório em `.archify/` com uma descrição verificável; atributos: tipo, fases/rotas ou nós/arestas, resultado da verificação.
- **Nota de estado verificado**: registro datado em documento de âncora; atributos: âncora do repositório, comando executado, resultado, escopo `[NV]`.
- **Resíduo de requisito**: item de checklist em spec antiga; atributos: identificação, fonte de evidência, classificação de revisão.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 5 de 5 candidatos de diagrama passam na verificação determinística (baseline: 3 de 5).
- **SC-002**: 2 de 2 âncoras documentais existem com nota datada e contagens verificadas (baseline: 0 de 2).
- **SC-003**: 100% dos itens de revisão registrados citam uma fonte de evidência e nenhum item human-gated é promovido.
- **SC-004**: Zero arquivos sob `LivingRoots/` modificados e nenhuma atestação `verified` reivindicada.
- **SC-005**: O gate autoritativo local reporta todas as etapas concluídas com sucesso após o ciclo.

## Assumptions

- Os candidatos `.archify/` são artefatos gerados/localmente versionados de forma ignorada; correções não entram no diff do PR.
- A verificação determinística dos diagramas é executada pelo utilitário `archify` já presente no ambiente.
- `docs/architecture/` não faz parte do alvo; nenhum HTML precisa ser criado.
- Checkboxes com evidência em `.specify/memory/STATE.md`, `validate.md` ou `release.md` podem ser revisados como qualidade de requisitos.
- Itens que dependem de pessoas, segredos ou aprovação externa permanecem pendentes.

