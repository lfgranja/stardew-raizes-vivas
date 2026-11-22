# Feature Specification: Fechar [NV] de causa-raiz remota dos PRs #790–#796

**Feature Branch**: `feature/031-nv-remote-log-closure`

**Created**: 2026-11-22

**Status**: Draft

**Input**: Handoff do assessment `nv-remote-log-closure` (verdict **go**, Option A) e decisão 80/20 do Bloco 5 de 2026-11-22.

## Clarifications

### Session 2026-11-22

- Q: Reescrever artefatos históricos 025/030 para remover `[NV]`? → A: Não. Append-only; notas datadas em âncoras de estado + documento de pesquisa novo.
- Q: Os demais `[NV]` devem ser promovidos com esta evidência? → A: Não. D1, atestação E, SC-004, tag/release, `RUNNER_AUDIT_STORE` e aprovador distinto permanecem humano/owner-gated e marcados.
- Q: Esta mudança exige Gate 3 real-game? → A: Não. Nenhum arquivo de código executável `LivingRoots/` é alterado; aplicam-se Gate 1 (estático/testes) e gates de documento/SDD do alvo.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Evidência remota persistida (Priority: P1)

Como mantenedor ou agente de orquestração, quero encontrar em um artefato versionado os comandos, run IDs e trechos de log que explicam a causa dos checks vermelhos dos PRs #790–#796, para decidir sem re-leitura repetida de logs.

**Why this priority**: É a condição para converter o `[NV]` em fato verificável e atender ao pedido explícito do prompt.

**Independent Test**: Abrir `research/2026-11-22-remote-nv-closure.md` e conferir três run IDs com trechos literais de log e timestamp de leitura.

**Acceptance Scenarios**:

1. **Given** os logs remotos lidos via `gh run view <id> --log-failed` em 2026-11-22, **When** o leitor consulta o documento de pesquisa, **Then** encontra os IDs 44641194770, 44664287245 e 44664266743, o erro `BUILDDEPS_TOKEN is not set` e o step `Fetch game assemblies`.
2. **Given** a afirmação sobre a causa raiz, **When** o leitor verifica a data/hora da leitura, **Then** a evidência é datada e escopada aos PRs #790–#796, não generalizada.
3. **Given** a possibilidade de os logs expirarem no GitHub, **When** o documento é lido no futuro, **Then** os trechos literais persistidos continuam citáveis sem nova chamada remota.

---

### User Story 2 - Âncoras de estado honestas (Priority: P2)

Como revisor da trilha SDD, quero que as âncoras de estado apontem a evidência resolvida e continuem marcando como `[NV]` o que depende de humano, para que nenhum status seja promovido por inferência.

**Why this priority**: A confusão entre "verificado ao vivo" e "owner-gated" é o risco central deste ciclo.

**Independent Test**: Ler `STATE.md`, `docs/PROGRESSO.md`, `docs/research/index.md` e `docs/quality-gate.md` e checar que a nota de 2026-11-22 distingue os dois conjuntos.

**Acceptance Scenarios**:

1. **Given** `docs/quality-gate.md:48` com `[NV]` sobre a causa step-level, **When** a nota datada é lida, **Then** a causa aparece como verificada em 2026-11-22 com run IDs.
2. **Given** D1, atestação E de Gate 3, SC-004, tag/release, `RUNNER_AUDIT_STORE` e aprovador distinto, **When** as âncoras são lidas, **Then** permanecem `[NV]`/human-gated.
3. **Given** artefatos históricos já mesclados, **When** o ciclo é revisado, **Then** nenhum deles foi reescrito ou renumerado.

---

## Edge Cases

- Log remoto indisponível no futuro → trechos literais e run IDs persistidos no artefato são a evidência de auditoria; nenhuma afirmação nova depende de reexecução.
- PRs encerrados → o ciclo não tenta reabri-los nem reexecutar CI.
- Leitor apressado → a nota nomeia explicitamente o que **não** foi resolvido.
- Gates com artefatos novos → `aggregate-gate.sh` pode exigir `validate.md`/`release.md` e tasks fechadas antes de PASS; o fechamento R4/R7 e o checkbox final fazem parte do ciclo.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O ciclo DEVE persistir um documento de pesquisa datado com os comandos `gh` executados, run IDs, step names e trechos literais de erro.
- **FR-002**: O documento DEVE citar pelo menos três execuções remotas independentes com o mesmo erro de provisão de `BUILDDEPS_TOKEN`.
- **FR-003**: A nota DEVE limitar a promoção do `[NV]` à causa step-level dos PRs #790–#796.
- **FR-004**: As âncoras `STATE.md`, `docs/PROGRESSO.md`, `docs/research/index.md` e `docs/quality-gate.md` DEVEM receber notas append-only datadas apontando o documento de pesquisa.
- **FR-005**: Nenhum artefato histórico de assessments, validations ou releases já mesclado PODE ser reescrito.
- **FR-006**: Os `[NV]` humano/owner-gated DEVEM permanecer marcados como tal em todas as âncoras atualizadas.
- **FR-007**: Nenhum arquivo executável sob `LivingRoots/`, `.github/workflows/` ou `scripts/ci/` PODE ser alterado.
- **FR-008**: O ciclo DEVE fechar a cadeia SDD completa (assess → specify → clarify → plan → checklist → tasks → analyze → implement → converge → validate R4 → release R7) com artefatos persistidos.

### Key Entities

- **RemoteEvidenceNote**: documento de pesquisa com data/hora de leitura, comandos, run IDs, trechos de log e escopo da afirmação.
- **StateAnchor**: arquivo de estado com nota append-only datada que aponta para a evidência e distingue `[NV]` resolvido de humano-gated.

## Success Criteria *(mandatory)*

- **SC-001**: 100% das duas âncoras-alvo (`STATE.md`, `docs/quality-gate.md`) não contêm mais o `[NV]` step-level sem a evidência datada correspondente (baseline: 2 ocorrências).
- **SC-002**: Pelo menos 3 run IDs e 3 trechos de log literais são encontrados no documento de pesquisa (baseline: 0).
- **SC-003**: `bash scripts/ci/aggregate-gate.sh` termina com `ALL STEPS PASSED` no HEAD do ciclo.
- **SC-004**: `GamePath=... scripts/release-gate.sh` termina com `ALL STEPS PASSED` no HEAD do ciclo.
- **SC-005**: `git diff --name-only dev...HEAD` contém apenas caminhos de documentação/SDD — zero arquivos sob `LivingRoots/`, `.github/workflows/` ou `scripts/ci/`.

## Assumptions

- `gh` 2.97.0 está disponível e autenticado para leitura do repositório `lfgranja/stardew-raizes-vivas`.
- Os run IDs citados foram lidos ao vivo em 2026-11-22T03:32–03:34Z; a retenção futura pelo GitHub não é assumida.
- O gate autoritativo do alvo permanece `scripts/release-gate.sh`, com CI GitHub Actions e CLI `gh` (Bloco 0.a).
