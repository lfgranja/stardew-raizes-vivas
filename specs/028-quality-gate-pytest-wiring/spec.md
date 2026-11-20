# Feature Specification: Quality Gate pytest wiring

**Feature Branch**: `028-quality-gate-pytest-wiring`
**Created**: 2026-11-20T01:22Z
**Status**: Draft
**Input**: "wire quality gate pytest into CI and remove dead S8969 pragma"

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Gate Python confiável localmente (Priority: P1)

Como maintainer, quero executar a suíte pytest do gate de qualidade e obter resultado verde quando o repositório está consistente, para confiar no sinal antes de abrir um PR.

**Why this priority**: é o único gate local vermelho observado (177/3); sem ele, os demais gates verdes dão falsa segurança.
**Independent Test**: `PYTHONPATH=scripts/quality-gate python3 -m pytest scripts/quality-gate/tests -q` → 0 failures.

**Acceptance Scenarios**:
1. **Given** csprojs pinam `SonarAnalyzer.CSharp 10.36.0.15337` e `Microsoft.CodeAnalysis.NetAnalyzers 11.0.100`, **When** a suíte pytest roda, **Then** os testes de pin leem esses mesmos valores e passam.
2. **Given** nenhuma supressão não justificada em `LivingRoots/`, **When** `test_every_repository_suppression_is_documented` roda, **Then** a lista de violações é vazia.
3. **Given** o pragma S8969 morto removido, **When** o build Release roda, **Then** 0 erros (`TreatWarningsAsErrors` intacto).

### User Story 2 — Suíte pytest no CI fail-closed (Priority: P1)

Como revisor, quero que o CI execute a mesma suíte pytest do local, para que drift de política não passe despercebido.

**Why this priority**: fecha o buraco entre gates locais e CI sem adicionar toolchain ao boundary `sdd-gate`.
**Independent Test**: inspecionar `.github/workflows/quality-gate.yml` job `static` e rodar os testes de contrato de workflow.

**Acceptance Scenarios**:
1. **Given** o job `static` (game-free, Python 3.12, sem secrets), **When** o workflow roda, **Then** instala `requirements-dev.txt` e executa `python -m pytest scripts/quality-gate/tests -q`.
2. **Given** pytest falha, **When** o job `gate` agrega os resultados, **Then** o check é RED sem `continue-on-error`.
3. **Given** os testes de contrato de workflow, **When** executados, **Then** permanecem verdes (nenhuma asserção de step-list exata é violada).

### User Story 3 — Evidência Gate 3 para a mudança em Services (Priority: P2)

Como maintainer, quero evidência `machine-pass` headless para a remoção do pragma em `LivingRoots/Services/`, para cumprir a constituição sem exigir atestação humana que não foi dada.

**Why this priority**: constituição Gate 3 cobre qualquer mudança em runnable code.
**Independent Test**: `scripts/verify-scenario.sh` com cenário compost + `--verify-bundle` exit 0.

**Acceptance Scenarios**:
1. **Given** o cenário `compost-growth-hardening.scenario.json`, **When** o runner headless roda, **Then** os 3 sinais concordam e o bundle é schema-válido.
2. **Given** o bundle, **When** verificado offline, **Then** `machine-pass` host-only é registrado; **`verified` não é reivindicado**.

---

### Edge Cases

- `requirements-ci.txt` isolado não fornece `pytest`/`hypothesis`/`yaml` — o step deve instalar `requirements-dev.txt` (ou ambos).
- Se um novo `!` aparecer no bloco do pragma no futuro, S8969 deve ser corrigido na origem, não suprimido.
- Arquivos `.archify/` e docs ausentes (`docs/PROGRESSO.md`) não são tratados nesta feature.

## Requirements *(mandatory)*

- **FR-001**: Atualizar `SONAR_VERSION` e `NETANALYZERS_VERSION` em `scripts/quality-gate/tests/test_analyzer_config.py` para os valores reais dos csprojs (`10.36.0.15337`, `11.0.100`).
- **FR-002**: Remover as linhas `#pragma warning disable S8969` e `#pragma warning restore S8969` de `LivingRoots/Services/CompostingBinService.cs` sem alterar a lógica do bloco.
- **FR-003**: Adicionar em `.github/workflows/quality-gate.yml` job `static`, após a instalação de `requirements-ci.txt`: step de instalação de `requirements-dev.txt` e step `python -m pytest scripts/quality-gate/tests -q`.
- **FR-004**: Não adicionar `continue-on-error`, secrets, jobs novos, nem mudar `needs` do job `gate`.
- **FR-005**: Registrar Gate 3 `machine-pass` (se aplicável à mudança em `.cs`) via runner headless; não promover a `verified`.
- **FR-006**: Atualizar `.specify/memory/STATE.md` com o estado verificado e dívidas **[NV]** remanescentes.

## Success Criteria *(mandatory)*

1. Suíte pytest local: **0 failures** (baseline 177 passed / 3 failed).
2. Build Release: **0 erros**; format/locked restore/aggregate/sdd/release gates: verdes.
3. CI: step pytest presente em `static`; testes de contrato de workflow verdes.
4. Gate 3: bundle válido, `--verify-bundle` exit 0, `machine-pass` sem `verified`.
5. Nenhuma nova supressão, `NoWarn`, `nosemgrep` ou `.semgrepignore`.

## Key Entities

- `test_analyzer_config.py` — contrato de versões de analyzer e política de supressões.
- `CompostingBinService.cs` — serviço Yellow Zone onde a supressão morta reside.
- `quality-gate.yml` (job `static`, job `gate`) — ponto de inclusão do pytest no CI.
- `scripts/verify-scenario.sh` — produtor de evidência Gate 3.

## Assumptions

- Gate 3 machine-pass é suficiente para mudança comment-only em produção; `verified` E é owner-gated.
- O merge do PR exige aprovador distinto e checks verdes observados ao vivo — permanece **[NV]**.
