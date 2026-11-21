# Feature Specification: Roteiro de verificação funcional

**Feature Branch**: `feature/030-roteiro-verificacao-funcional`

**Created**: 2026-11-21

**Status**: Draft

**Input**: Handoff do assessment `roteiro-verificacao-funcional` (verdict go, Option A) e decisão 80/20 do Bloco 5 de 2026-11-21.

## Clarifications

### Session 2026-11-21

- Q: A matriz deve ser persistida como artefato versionado ou apenas relatada na sessão? → A: Persistida como `specs/030-roteiro-verificacao-funcional/verification-matrix.md`.
- Q: Quantos cenários Gate 3 devem ser reexecutados — apenas o afetado ou os sete specs com cenário? → A: Todos os cenários primários com `test.md` são verificados; a reexecução completa é obrigatória apenas para o cenário alterado.
- Q: Qual é a precedência entre autorização explícita de merge e checks vermelhos? → A: A autorização cobre o ato de mesclar, nunca o bypass de check obrigatório vermelho.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Matriz auditável de verificação (Priority: P1)

Como mantenedor ou revisor, quero uma única matriz que relacione cada funcionalidade implementada ao comando determinístico que a verifica e ao resultado observado, para responder "o que está testado" sem reconstruir a resposta de dezenas de arquivos.

**Why this priority**: É o pedido literal do Software Engineer e a condição para qualquer afirmação honesta sobre cobertura.

**Independent Test**: Abrir a matriz persistida e executar um grupo representativo (B1 — saúde do solo) com 0 falhas; nenhuma outra história é necessária.

**Acceptance Scenarios**:

1. **Given** as funcionalidades implementadas listadas no `AGENTS.md`, **When** o leitor consulta a matriz persistida, **Then** encontra para cada uma o comando de teste, o resultado esperado e o limite de honestidade (`machine-pass` ≠ `verified`).
2. **Given** os 11 grupos B1–B11, **When** executados com o ambiente declarado, **Then** todos terminam com 0 falhas e contagem colada.
3. **Given** uma feature sem cenário Gate 3, **When** a matriz é consultada, **Then** a ausência aparece como lacuna de evidência, não como falha.

---

### User Story 2 - Evidência end-to-end coerente (Priority: P2)

Como revisor, quero que o cenário de compostagem em solo pobre use as mesmas faixas do domínio, para que a evidência não descreva uma regra obsoleta.

**Why this priority**: A deriva é concreta e verificável (`"max": 34` vs. `PoorModerateBandEdge = 31f`) e corrói a confiança do Gate 3.

**Independent Test**: Verificar que o threshold do cenário hardening é `[0, 31)` e que o bundle reexecutado tem 3 sinais concordantes.

**Acceptance Scenarios**:

1. **Given** o cenário `compost-growth-hardening` com `"max": 34` e descrição "(0, 34)", **When** a correção é aplicada, **Then** o predicado passa a expressar `[0, 31)` com `exclusiveMin: false` e `exclusiveMax: true`.
2. **Given** o cenário editado, **When** o executor headless é executado sob Xvfb, **Then** o bundle resultante tem `verdict: pass`, honestidade `R` e os três sinais presentes e concordantes.
3. **Given** os demais cenários com limites legados, **When** a varredura é feita, **Then** as ocorrências são listadas como dívida e não são alteradas neste ciclo.

---

### User Story 3 - Estado honesto e dívidas explícitas (Priority: P3)

Como Software Engineer, quero que o estado verificado e o que depende de humano/owner fiquem registrados com data e âncora, para que nenhuma atestação seja inferida de gate verde.

**Why this priority**: A constituição v2.0.0 e o protocolo de verificação exigem a distinção; é o que impede falso `verified`.

**Independent Test**: Comparar as notas datadas em `STATE.md`/`PROGRESSO.md` com a saída dos gates e verificar que D1, atestação E e SC-004 permanecem `[NV]`.

**Acceptance Scenarios**:

1. **Given** os gates executados, **When** as notas são escritas, **Then** contêm data, HEAD, âncora de feature, contagens por script e saída dos gates.
2. **Given** PRs #811/#812 com checks obrigatórios vermelhos, **When** o ciclo é entregue, **Then** o desfecho de merge é registrado como `[NV]` com causa, ou como merge com saída ao vivo do `gh`.
3. **Given** qualquer resultado de máquina, **When** o relatório final é escrito, **Then** nenhuma afirmação `verified`, D1 ou SC-004 é promovida.

---

### Edge Cases

- Ambiente sem `GamePath` exportado: o gate é bloqueado por provisionamento, não classificado como RED de código; todo comando usa `GamePath=/home/luis/stardew-assemblies`.
- Smoke launch-free exceder o teto do harness: executar as 7 etapas isoladamente na ordem; timeout de harness não é RED.
- Runner rejeitar o cenário editado por schema: restaurar apenas essa edição própria a partir de `HEAD`, manter a matriz e registrar dívida `[NV]`.
- Repo mutado por outra sessão durante o ciclo: reancorar no início do bloco seguinte; não reverter trabalho alheio.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O ciclo DEVE persistir uma matriz com 11 grupos (B1–B11) cobrindo solo, visualização, composto, mulch/decaimento, agroquímico, crioulo, colheita, crescimento, multiplayer, arquitetura/segurança e ferramental de verificação.
- **FR-002**: A matriz DEVE registrar para cada grupo o filtro `FullyQualifiedName~` exato, o resultado esperado e a data/hora da execução.
- **FR-003**: O ciclo DEVE executar os gates autoritativos do alvo (`release-gate.sh`, `aggregate-gate.sh`, `i18n-parity-check.py`, pytest de quality-gate) no início e no fechamento.
- **FR-004**: O ciclo DEVE corrigir o threshold de saúde do solo do cenário `compost-growth-hardening` de `(0, 34)` para `[0, 31)`.
- **FR-005**: O ciclo DEVE reexecutar o Gate 3 do cenário alterado e anexar o novo `runId` a `specs/004-playable-compost-loop/test.md`.
- **FR-006**: O ciclo DEVE varrer os demais cenários quanto a limites legados e listar as ocorrências sem alterá-las.
- **FR-007**: O ciclo DEVE registrar em notas datadas e append-only o estado verificado, as contagens por script e os itens `[NV]`.
- **FR-008**: O ciclo NÃO DEVE produzir diff em `LivingRoots/**` nem alterar save/schema/i18n/código de jogo.
- **FR-009**: O ciclo NÃO DEVE promover nenhum resultado a `verified` humano nem simular D1/SC-004/aprovação de revisor.
- **FR-010**: PRs abertos DEVEM ser conduzidos para `dev` somente com checks obrigatórios verdes observados ao vivo; casos owner-gated DEVEM ser registrados como `[NV]`.

### Key Entities *(include if feature involves data)*

- **Grupo de verificação**: identificador B1–B11, feature associada, filtro de teste, expectativa, resultado observado.
- **Cenário Gate 3**: arquivo JSON versionado, `thresholds`, `expectedSignals`, bundle com `runId` e nível de honestidade.
- **Nota de estado**: data, HEAD, âncora de feature, contagens, gates e dívidas `[NV]`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 11 de 11 grupos B1–B11 executados com 0 falhas e contagem registrada (baseline: grupos não agregados).
- **SC-002**: Gates autoritativos `ALL STEPS PASSED` no início e no fechamento do ciclo (baseline: aggregate PASS; release em execução no início).
- **SC-003**: 1 de 1 deriva corrigida: `"max": 31`, `exclusiveMin: false`, `exclusiveMax: true`, descrição coerente.
- **SC-004**: Bundle reexecutado com `verdict: pass`, honestidade `R` e 3 sinais concordantes; `runId` registrado.
- **SC-005**: Zero diff em `LivingRoots/**` e zero afirmações `verified`/D1/SC-004 promovidas.
- **SC-006**: Notas datadas presentes em `STATE.md` e `docs/PROGRESSO.md` com âncora, contagens e `[NV]`.

## Assumptions

- O runner aceita a edição do threshold sem mudança de schema; se rejeitar, a correção é revertida apenas localmente e a dívida registrada.
- O diretório de assemblies e o jogo real permanecem disponíveis neste host.
- A autorização explícita do prompt cobre o ato de mesclar e não exige aprovador distinto pela política do alvo (proteção de `dev` com 0 reviews), mas não autoriza bypass de check vermelho.
- Nenhuma afirmação remota não observada via `gh` é promovida a verificada.
