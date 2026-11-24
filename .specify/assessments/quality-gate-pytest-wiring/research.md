# Research: Quality Gate pytest wiring and dead suppression cleanup

- **Slug**: quality-gate-pytest-wiring
- **Date**: 2026-11-20T01:20Z
- **Method**: leitura direta de arquivos do alvo + execução de gates locais. Nenhuma fonte web externa necessária — a afirmação é inteiramente local e reproduzível.

## Evidence (deterministic, local)

1. `PYTHONPATH=scripts/quality-gate python3 -m pytest scripts/quality-gate/tests -q` → **177 passed / 3 failed** (2026-11-20T01:19Z). Falhas:
   - `test_both_projects_reference_the_pinned_analyzers[path0]` e `[path1]` — `scripts/quality-gate/tests/test_analyzer_config.py:87` espera `SONAR_VERSION=10.35.0.4138` / `NETANALYZERS_VERSION=10.0.403` (`test_analyzer_config.py:18-19`), enquanto `LivingRoots/LivingRoots.csproj:20-21` e `LivingRoots.Tests/LivingRoots.Tests.csproj:57-58` pinam `10.36.0.15337` / `11.0.100` (bumps #791/#792/#794/#795, mesclados 2026-11-19/20).
   - `test_every_repository_suppression_is_documented` (`test_analyzer_config.py:275,279`) — `#pragma warning disable S8969` em `LivingRoots/Services/CompostingBinService.cs:115` sem justificativa; `:114` está em branco e `:120` tem o `restore`.
2. O pragma é **morto**: o bloco `:116-119` contém apenas `if (result.Accepted) { ApplyAcceptedAddWaste(...) }` — sem operador `!` (gatilho do S8969). Histórico: `dadf3f98` tinha `item!` + justificativa; o refactor `fec8b4b2` removeu ambos e deixou o pragma nu. Todos os outros 9 pragmas em `LivingRoots/` têm justificativa `// RULE: reason` e passam na política.
3. `scripts/ci/aggregate-gate.sh` (4 etapas: sdd-gate, policy-drift, protected-paths, docs markers) e `python3 scripts/ci/sdd-gate.py --check` (0 erros / 29 warnings) **não executam pytest**. Repo-wide grep em `.github/`, `scripts/ci/`, `scripts/release-gate.sh`, `lefthook.yml` → nenhuma invocação de pytest.
4. `.github/workflows/quality-gate.yml:21-41` — job `static` (game-free) já usa Python 3.12 (`actions/setup-python@v7`), `PYTHONPATH: scripts/quality-gate` e instala `requirements-ci.txt` (`:41`). Job `gate:302` depende de `[static, sca, build-analysis]` e falha fechado se qualquer um não for `success`. `requirements-ci.txt` só tem `semgrep==1.182.1`; a suíte importa `pytest`, `hypothesis`, `yaml` de `requirements-dev.txt`.
5. `scripts/quality-gate/analyzer-probe.sh` e `docs/quality-gate.md` documentam a política de supressão: ID da regra + razão por linha ou linha anterior; `NoWarn`/`nosemgrep`/`.semgrepignore` proibidos; `.editorconfig` raiz sem `severity=none`.
6. `scripts/release-gate.sh` (gate autoritativo do alvo) passou steps 1-4 nesta sessão (format, locked restore, build Release, testes) com `GamePath=/home/luis/stardew-assemblies`; smoke em andamento no momento desta escrita.
7. Constituição v2.0.0 Gate 3: se aplica a qualquer mudança em código executável, "including seemingly unrelated ones" (`.specify/memory/constitution.md`). `scripts/verify-scenario.sh` produz `machine-pass` headless; `verified` (E) é atestação humana (`VERIFICATION_PROTOCOL.md`).

## Triangulation / confidence

- Todas as afirmações são locais e verificadas por comando executado nesta sessão ou leitura de arquivo com linha citada — confiança alta, sem dependência de fonte remota.
- Nenhum status remoto/CI é afirmado aqui: PRs, checks e proteções são **[NV]** até confirmação via `gh` ao vivo.

## Prior art / alternatives considered

- Corrigir o teste acrescentando justificativa ao pragma: rejeitado — preservaria supressão que não suprime nada, contrariando a política "fix the finding, don't ignore it".
- Rodar pytest no `sdd-gate`/`aggregate-gate`: rejeitado — quebra o boundary "no .NET/Python toolchain" e adicionaria install de deps a um gate que é stdlib-only.
- Rodar pytest em job separado: desnecessário — steps em `static` já herdam fail-closed pelo job `gate`.

## Coverage idiomática

Português (repo/docs) e inglês (código/testes/workflows) — ambos lidos diretamente. Nenhuma pesquisa web realizada; nenhuma afirmação carregada depende de fonte externa.
