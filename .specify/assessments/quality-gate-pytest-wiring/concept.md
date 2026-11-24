# Concept: Quality Gate pytest wiring and dead suppression cleanup

- **Slug**: quality-gate-pytest-wiring
- **Date**: 2026-11-20T01:20Z

## Options

### Option A — Smallest thing that could work (recommended)

Corrigir os dois pins em `test_analyzer_config.py` para `10.36.0.15337` / `11.0.100`, remover as duas linhas `#pragma warning disable/restore S8969` de `CompostingBinService.cs:115,120`, e adicionar dois steps em `.github/workflows/quality-gate.yml` dentro do job `static` (install de `requirements-dev.txt` + `python -m pytest scripts/quality-gate/tests -q`).

- **Appetite**: small (horas).
- **Trade-offs**: corrige a causa (pins desatualizados, supressão morta) e fecha o buraco do CI; toca `LivingRoots/Services/` (Yellow Zone) → deve Gate 3 machine-pass.
- **Rabbit holes**: reintroduzir warning S8969 se algum `!` existir no bloco (verificado: não existe); quebrar contrato de workflow se step list for asserção exata (verificado: não é).

### Option B — Documentation-only

Deixar o pragma no lugar, adicionar `// S8969: ...` justificativa, corrigir só os pins e documentar o pytest em `docs/quality-gate.md`.

- **Appetite**: small.
- **Trade-offs**: evita Gate 3 (nenhum `.cs` executável mudaria), mas preserva uma supressão inútil e não executa pytest no CI — deixa o risco de drift intacto.
- **Rabbit holes**: a justificativa falsa ("necessária") é desonesta e a política de Suppressions exige "fix the finding".

### Option C — Do nothing / wait for owner

Manter a dívida P1 registrada até decisão humana.

- **Appetite**: zero.
- **Trade-offs**: nenhum risco de regressão, mas nenhum gate novo e dívida indefinida.
- **Rabbit holes**: confundir "owner-gated" com "bloqueado" — os itens owner-gated reais (D1, E, tag) são distintos destes.

## Recommendation

**Option A**. É o único que resolve as três causas raiz com superfície pequena e evidência determinística. O Gate 3 é automatizável via `verify-scenario.sh` (`machine-pass` host-only), portanto não depende de intervention humana; a atestação E permanece **[NV]** por desenho.

## Bounded scope

- Inclui: 4 arquivos — `scripts/quality-gate/tests/test_analyzer_config.py`, `LivingRoots/Services/CompostingBinService.cs`, `.github/workflows/quality-gate.yml`, artefatos SDD/STATE.
- Exclui: csprojs,别的 arquivos de produção, mudanças de CI provider/CLI, merge do PR, tags/releases, D1.

## Assumptions

- `dotnet build` Release permanece 0 erros após remoção do pragma (o gatilho S8969 — `!` — não existe no bloco; verificado por leitura).
- `actions/setup-python@v7` com Python 3.12 continua disponível no runner (já usado no job `static`).
- `requirements-dev.txt` (`pytest==9.2.0`, `hypothesis==6.170.13`, `PyYAML==6.0.3`) satisfaz a suíte local (verificado: 180 testes coletados).
