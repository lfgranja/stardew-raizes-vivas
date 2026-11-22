# Feature Specification: Alinhar bandas 31/71 nos cenários ativos 004 e 005

**Feature Branch**: `feature/032-alinhar-bandas-cenario`

**Created**: 2026-11-22

**Status**: Draft

**Input**: Handoff do assessment `band-scale-scenario-drift` (verdict **go**, Option A) e dívida registrada no ciclo 030.

## Clarifications

### Session 2026-11-22

- Q: Renomear/alterar fixtures e schemas 010? → A: Não. São contratos históricos explicitamente excluídos pela guarda `scripts/tests/test-band-scale-31-71.sh`.
- Q: Alterar `exclusiveMax` para `false` em Healthy? → A: Não. Nenhuma leitura real de exatamente 100 foi evidenciada; questão contratual separada.
- Q: Promover `machine-pass` a `verified`? → A: Não. Apenas `machine-pass` host-only, honestidade R.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Contrato de cenário coerente (Priority: P1)

Como revisor de Gate 3, quero que os cenários ativos de crescimento em solo saudável declarem a mesma banda Healthy do domínio, para que a evidência automatizada não descreva uma regra obsoleta.

**Why this priority**: É a dívida ativa apontada pelo ciclo 030 e verificável por comparação direta com `ModConstants`.

**Independent Test**: ler os dois cenários e confirmar `min: 71` e descrição `[71, 100)`; nenhuma outra história necessária.

**Acceptance Scenarios**:

1. **Given** `ModConstants.ModerateHealthyBandEdge = 71f`, **When** leio `compost-growth-healthy` e `soil-health-growth`, **Then** ambos declaram `min: 71` e descrição `[71, 100)`.
2. **Given** o valor observado 80 nos bundles anteriores, **When** o predicado `between` é reavaliado, **Then** o veredito continua `pass`.
3. **Given** a exigência de reexecução, **When** os dois cenários são rodados no runner headless, **Then** produzem `verdict=pass`, 3 sinais concordantes e `--verify-bundle` exit 0.

---

### User Story 2 - Evidência promovida pelo runner, sem reescrita (Priority: P2)

Como responsável pela trilha SDD, quero que os novos runs sejam registrados pelo próprio runner e que os artefatos históricos permaneçam intactos, para preservar proveniência.

**Why this priority**: blocos `GATE3:BEGIN/END` editados à mão já foram causa de falha de docs markers em ciclo anterior.

**Independent Test**: `verify-scenario.sh --check-docs` passa e `git diff --name-only dev...HEAD` não contém `LivingRoots/`, workflows, `scripts/ci/` nem fixtures 010.

**Acceptance Scenarios**:

1. **Given** um novo run `pass`, **When** o runner promove o bloco, **Then** o bloco reflete o novo run ID sem edição manual.
2. **Given** um run `partial`/W, **When** o resultado é registrado, **Then** nenhum bloco é promovido e nenhum `verified` é reivindicado.
3. **Given** o diff do ciclo, **When** revisado, **Then** as únicas mudanças de cenário são 4 linhas nos 2 arquivos ativos.

## Edge Cases

- Runner retorna `partial` por sinal ausente → registrar `partial`, não promover, não repetir indefinidamente (teto de 2 tentativas por cenário).
- `--verify-bundle` falha → não promover; registrar bundle e saída.
- Guarda `test-band-scale-31-71.sh` continua excluindo 004/005 → coerente: o escopo corrige o conteúdo, não a guarda.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: `compost-growth-healthy.scenario.json` DEVE declarar `min: 71` e descrição `[71, 100)`, preservando `max: 100`, `tolerance`, `exclusiveMin`, `exclusiveMax` e `factor`.
- **FR-002**: `soil-health-growth.scenario.json` DEVE declarar `min: 71` e descrição `[71, 100)`, preservando os demais campos.
- **FR-003**: NENHUM outro campo dos dois cenários PODE ser alterado; total máximo de 4 linhas mudadas.
- **FR-004**: Os dois cenários DEVEM ser reexecutados via `verify-scenario.sh` com `--no-acquire --game-path ... --skip-build`.
- **FR-005**: A promoção de blocos `GATE3:BEGIN/END` DEVE ser feita pelo runner; edição manual é proibida.
- **FR-006**: Nenhum arquivo sob `LivingRoots/`, `.github/workflows/`, `scripts/ci/` ou `specs/010-*` PODE ser alterado.
- **FR-007**: Notas append-only DEVEM ser registradas nos `test.md` e na âncora de estado, com run IDs e limite `machine-pass`.
- **FR-008**: O ciclo DEVE fechar a cadeia SDD completa com artefatos persistidos.

### Key Entities

- **ScenarioBandPredicate**: predicado `between` com `min/max/exclusive*` e descrição da banda.
- **Gate3Evidence**: run ID, veredito, honestidade R, 3 sinais, bundle verificado e bloco promovido pelo runner.

## Success Criteria *(mandatory)*

- **SC-001**: 2/2 cenários ativos declaram `[71, 100)` (baseline: 0/2).
- **SC-002**: Exatamente 4 linhas alteradas nos 2 cenários; zero mudanças em campos protegidos (baseline: 0).
- **SC-003**: 2/2 runs Gate 3 com `verdict=pass`, honestidade R, 3 sinais e `--verify-bundle` exit 0.
- **SC-004**: `aggregate-gate.sh` e `release-gate.sh` ALL STEPS PASSED no HEAD do ciclo.
- **SC-005**: `git diff --name-only dev...HEAD` sem `LivingRoots/`, workflows, `scripts/ci/` e `specs/010-*`.

## Assumptions

- Runner headless disponível com Xvfb e `GamePath` apontando para os assemblies do jogo.
- Valor observado 80 permanece estável com as sementes registradas (`123456789`, `987654321`).
- Autorização de merge do prompt cobre este PR, desde que checks obrigatórios estejam verdes.
