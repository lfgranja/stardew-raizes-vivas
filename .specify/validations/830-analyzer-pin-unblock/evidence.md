# Evidência — desbloqueio do bump SonarAnalyzer (#830)

- **Data/hora**: 2026-11-27T23:36Z (verificações locais), reconfirmação remota `gh pr view 830` às 2026-11-27T23:36:54Z.
- **Âncora `[EV]`**: worktree `.specify/queue/worktrees/dependabot-830-static-pin/`, branch `fix/830-analyzer-pin-consistency`, topo herdado `65f4f9b` (commit do Dependabot), `git diff --name-only origin/dev...HEAD` = seis arquivos (três `.csproj`, três `packages.lock.json`).
- **Roteiro versionado**: `.specify/validations/830-analyzer-pin-unblock/test-roadmap.md`. Superfície CLI/headless; não há UI de produto observável, portanto não há captura de tela — a captura é a saída de terminal transcrita abaixo.
- **`[NV]`**: status remoto de checks do PR próprio antes do push; efeito de produção; segredos do Dependabot. Nada foi promovido a `[EV]` sem comando executado.

| Passo | Comando/ação | Saída observada `[EV]` | Resultado | Referência de captura |
|---|---|---|---|---|
| 1 | `git log --oneline -2` na worktree | `65f4f9b Bump SonarAnalyzer.CSharp from 10.36.0.15337 to 10.37.0.15768`; `d777cbb Merge pull request #827` | PASS | Esta tabela |
| 2 | `PYTHONPATH=scripts/quality-gate python -m pytest scripts/quality-gate/tests/test_analyzer_config.py -q -k test_both_projects_reference_the_pinned_analyzers` **antes** da correção | `0 passed, 2 failed`; `AssertionError: assert '10.37.0.15768' == '10.36.0.15337'` em `test_analyzer_config.py:87`, para `[path0]` (LivingRoots) e `[path1]` (LivingRoots.Tests) | FAIL, reproduzido | Esta tabela |
| 3 | Editar somente `scripts/quality-gate/tests/test_analyzer_config.py:20` → `SONAR_VERSION = "10.37.0.15768"` | `git diff --stat`: `.specify/memory/STATE.md` 8 linhas, `test_analyzer_config.py` 1 linha; nenhum `.csproj`, lock file, workflow ou código C# alterado por esta sessão | PASS | `git diff --stat` |
| 4 | Repetir o comando do passo 2 | `2 passed in 0.16s`, `RC=0` | PASS | Esta tabela |
| 5 | `PYTHONPATH=scripts/quality-gate python -m pytest scripts/quality-gate/tests -q` | `180 passed in 14.44s`, `RC=0` (CI tinha `178 passed` antes da correção) | PASS | Esta tabela |
| 6 | `bash scripts/ci/aggregate-gate.sh` | `protected-path preflight: all checks passed`; `[aggregate-gate] ALL STEPS PASSED`; `RC=0` | PASS | Esta tabela |
| 6b | `bash scripts/check-protected-paths.sh` | `protected-path preflight: all checks passed`; `RC=0` | PASS | Esta tabela |
| 7 | `gh pr view 830 --json number,state,headRefOid,mergeStateStatus` | `830 OPEN 65f4f9b5061d434155c322113b6d6758f98a929b BLOCKED` | PASS (PR intacto) | Esta tabela |
| 8 | Após publicação: `gh pr checks <PR>` no head exato | **A preencher após o push** — não é `[EV]` até ser executado | pending | — |
| 9 | Após merge em `dev`: `gh pr checks 830` | **A preencher após o merge** — esperado: `Quality Gate / static` fora das falhas; `BUILDDEPS_TOKEN` permanece nas demais | pending | — |

## Distinção entre verde local e green remoto

Os itens 2–6 são `[EV]` locais nesta worktree. Eles **não** concedem check de PR: o green remoto só existe quando `gh pr checks` reporta `pass` no SHA publicado. O mesmo vale para #830: o rollup remoto observado às 2026-11-27T02:56Z é `[EV]` daquele momento, e qualquer reavaliação posterior exige nova leitura ao vivo.

## Achados fora do escopo (advisory)

`LivingRoots.Verification` também fixa `SonarAnalyzer.CSharp`, mas `test_analyzer_config.py:16-17,80` não o inclui na parametrização — um bump parcial nesse projeto passaria despercebido. Não foi alterado neste ciclo: a correção mínima preserva a decisão documentada de pins literais e não muda a semântica do gate. Registrado como advisory para o SWE.
