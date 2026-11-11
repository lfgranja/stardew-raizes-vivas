# Narrow Lights-Out — Living Roots (Fase 4)

Status: **desenhado / parcialmente implementado** (2026-11-10)
Referência: `mdmv-dark-factory` research 07/11/12; ADR 002 (Fase 4).

## Princípio (políticas 022/025/042)

- **Sem auto-merge cego.** Toda entrega automatizada abre PR; humano aprova o merge.
- **Verificação machine-checkable** antes de qualquer PR (build + test + lint + gate específico do job).
- **Execution record** (035) anexado ao PR.
- **Blast radius baixo** + consequência de erro contida (rollback trivial).
- **Julgamento humano** reservado a intenção, arquitetura e mudanças que quebram (Mission Control).

## Classe de job inicial: paridade i18n (default/en/pt)

Única classe de trabalho narrow habilitada hoje. Motivação: 3 arquivos JSON, verificação determinística (conjunto de chaves idêntico + sem valores vazios), blast radius baixo (só strings), rollback trivial.

### Gates obrigatórios (machine-checkable)

1. `scripts/ci/i18n-parity-check.py` → PASS (0 errors).
2. `dotnet build Stardew-LivingRoots.sln --configuration Release -m:1` → OK.
3. `dotnet test Stardew-LivingRoots.sln --configuration Release -m:1` → OK.
4. `dotnet format Stardew-LivingRoots.sln --verify-no-changes` → OK.
5. `python3 scripts/ci/sdd-gate.py` → PASS.
6. (Se tocar código) Gate 3 via `verify-scenario.sh` — não aplicável a mudanças só de i18n.

### Fluxo do job

1. Agente recebe tarefa (issue/schedule) escopada a i18n.
2. Agente roda os gates acima em worktree isolado.
3. Agente abre PR com execution record (035) anexado.
4. Humano revisa o resultado e aprova o merge.
5. Sem auto-merge. Se os gates falharem, o PR não é aberto (ou é aberto como draft com falha).

### Exemplo executado

- PR #781 (Fase 2) incluiu a correção de paridade i18n: 4 chaves `mulch.*` adicionadas ao `pt.json` (`message.mulch.already`, `message.mulch.invalid-tile`, `mulch.description`, `mulch.name`). Verificação: `i18n-parity-check.py` → PASS.

## Classes habilitadas (narrow lights-out)

### 1. Paridade i18n (default/en/pt) — HABILITADA
- **Gates:** `i18n-parity-check.py` PASS, build, test, format, sdd-gate.
- **Blast radius:** baixo (só strings).
- **Exemplo:** PR #783 (4 chaves mulch.* adicionadas ao pt.json).
- **Workflow:** `.github/workflows/i18n-parity.yml`.

### 2. Correção de lint (`dotnet format`) — HABILITADA
- **Gates:** `dotnet format --verify-no-changes` (deve falhar antes, passar depois), build, test, sdd-gate.
- **Blast radius:** baixo (formatação, sem mudança de lógica).
- **Fluxo:** agente roda `dotnet format`, verifica diff mínimo, abre PR com execution record.
- **Workflow:** `.github/workflows/code-style.yml` (já existente) + sdd-gate.

### 3. Novas chaves i18n (feature strings) — HABILITADA
- **Gates:** `i18n-parity-check.py` PASS (key sets idênticos), build, test, format, sdd-gate.
- **Blast radius:** baixo (só strings).
- **Fluxo:** agente adiciona a chave nos 3 locales (default/en/pt), traduzindo para pt; verifica paridade; abre PR.
- **Nota:** tradução para pt requer revisão humana (qualidade de idioma).

### 4. Bump de dependências patch — HABILITADA (somente patch/minor)
- **Gates:** `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, sdd-gate, i18n-parity (se aplicável).
- **Blast radius:** médio (pode afetar compilação/comportamento).
- **Restrição:** apenas bumps de **patch** (x.y.Z → x.y.Z+1). Bumps de minor/major requerem revisão humana.
- **Verificação:** `scripts/ci/dependency-outdated-check.py` reporta pacotes desatualizados; agente aplica apenas patch bumps.
- **Workflow:** `.github/workflows/dependency-check.yml` (scheduled weekly + dispatch).

## Classes NÃO habilitadas (zona vermelha / alto blast radius)

- Mudanças em `Controllers/`, `Patches/`, segurança, persistência.
- Bumps de minor/major de dependências.
- Mudanças em multiplayer host-authoritative state.
- Auto-merge cego (política 022/025/042).

## O que NÃO está habilitado

- Auto-merge cego (política 022/025/042): proibido.
- Jobs que tocam `Controllers/`, `Patches/`, segurança/persistência (zona vermelha, brownfield).
- Jobs sem verificação machine-checkable.
- Runtime harness de execução de agentes (hub Fase 3, NV) — este repo não roda agentes autonomamente; o fluxo é assistido.

## Referências

- ADR 002 (Fase 4).
- `scripts/ci/i18n-parity-check.py`.
- `docs/governance/execution-record.schema.json`.
- `mdmv-dark-factory` research 07 (8 passos), 11 (brownfield zones), 12 (adoção parcial).
