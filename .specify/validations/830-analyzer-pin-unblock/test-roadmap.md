# Roteiro de testes — desbloqueio do bump SonarAnalyzer (#830)

- **ALVO**: `/home/luis/development/stardew-raizes-vivas`; worktree própria `.specify/queue/worktrees/dependabot-830-static-pin/`, branch `fix/830-analyzer-pin-consistency`, criada a partir do head exato de #830 (`65f4f9b5061d434155c322113b6d6758f98a929b`).
- **Sessão**: `session_title` = `stardew-raizes-vivas - desbloqueio CI do bump SonarAnalyzer (#830)`; `session_id` = `20261127T233246Z-lrv-dependabot-830-unblock`.
- **Escopo**: FR-001/T001 de `specs/011-local-oss-quality-gate` (analyzers com versão determinística) e decisão do ciclo 028 que espelhou os pins em `scripts/quality-gate/tests/test_analyzer_config.py`. PR de origem: [#830](https://github.com/lfgranja/stardew-raizes-vivas/pull/830). Nenhuma linha de código do jogo é alterada.
- **Superfície**: CLI/headless. Não existe UI de produto observável nesta superfície; a evidência é saída de terminal e rollup do `gh`, sem captura de tela fingida.
- **Pré-condições**: Python 3.12 com pytest 9.2.0; `GamePath=/home/luis/stardew-assemblies` disponível para builds locais; `gh` autenticado.

## Passos

1. **Fixar a âncora do PR alheio** — `git log --oneline -2` e `git diff --name-only origin/dev...HEAD` na worktree. Resultado esperado: topo `65f4f9b  Bump SonarAnalyzer.CSharp...`, seis arquivos alterados (três `.csproj` e três `packages.lock.json`). Automatizável: sim.
2. **Reproduzir o vermelho determinístico antes da correção** — `PYTHONPATH=scripts/quality-gate python -m pytest scripts/quality-gate/tests/test_analyzer_config.py -q -k test_both_projects_reference_the_pinned_analyzers`. Resultado esperado: `0 passed, 2 failed`, com `assert '10.37.0.15768' == '10.36.0.15337'` em `test_analyzer_config.py:87`. Automatizável: sim.
3. **Aplicar a correção mínima** — atualizar `SONAR_VERSION` em `scripts/quality-gate/tests/test_analyzer_config.py` para `10.37.0.15768`, sem tocar nos `.csproj`, lock files, workflows ou código C#. Resultado esperado: diff de uma linha sobre o commit herdado. Automatizável: sim.
4. **Verificar o verde do teste focal** — repetir o comando do passo 2. Resultado esperado: `2 passed`. Automatizável: sim.
5. **Verificar a suíte determinística do gate** — `PYTHONPATH=scripts/quality-gate python -m pytest scripts/quality-gate/tests -q`. Resultado esperado: `180 passed` (o CI observou `178 passed` no estado quebrado). Automatizável: sim.
6. **Verificar o gate agregado do ALVO** — `bash scripts/ci/aggregate-gate.sh` e `./scripts/check-protected-paths.sh` na worktree. Resultado esperado: exit 0, nenhum caminho protegido alterado. Automatizável: sim.
7. **Publicar e reconfirmar no remoto** — commit com paths explícitos, push sem force, abrir PR para `dev`; conferir `gh pr checks <n>` no head exato: `Build and Test`, `Check Formatting`, `Quality Gate / static`, `Quality Gate / build-analysis`, `Quality Gate / gate`, `sdd-gate`, `verify` = sucesso (o PR próprio não é Dependabot, portanto tem acesso aos segredos). Resultado esperado: checks obrigatórios verdes no SHA publicado. Automatizável: sim.
8. **Revalidar #830 sem tocá-lo** — após o merge do PR próprio em `dev`, `gh pr checks 830` no head `65f4f9b5`. Resultado esperado: `Quality Gate / static` deixa de aparecer entre as falhas; `Build and Test`, `Check Formatting` e `build-analysis` permanecem vermelhos por `BUILDDEPS_TOKEN is not set`. Automatizável: sim.
9. **Escalar a lacuna owner-only** — comentário em #830 com `[EV]`/`[NV]` e URLs canônicas; nenhum merge, deploy, tag ou alteração de proteção. Automatizável: não (decisão humana).

## Rollback

A correção é um literal em teste. Reverter o commit do PR próprio em `dev` restaura o pin antigo; nenhum estado de jogo, save ou produção é afetado. O bump de #830 herdado pode ser revertido com `git revert` do merge se o SWE decidir, sem migração de dados.
