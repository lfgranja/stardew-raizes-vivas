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
| 8 | `gh pr checks 832 --watch --interval 30` no head `3765dc01a3cfb5b01fb023f0928e87d4c9961f84` | `RC=0`, 10 `pass` / 0 `fail` / 2 `skipping`; `sdd-gate`, `Build and Test` e `Quality Gate / static` = `SUCCESS` | PASS | [runs do PR 832](https://github.com/lfgranja/stardew-raizes-vivas/pull/832/checks) |
| 9 | `gh pr view 830 --json statusCheckRollup` às 2026-11-27T23:40:06Z e `gh pr view 830/829/828/831` às 23:44:46Z | #830 `MERGED` às 23:39:53Z (merge commit `65f4f9b5…`, igual ao head); #829 `CLOSED` às 23:42:22Z; #828 (`5f64baa`) e #831 (`91b6cf6`) `OPEN`. O rollup de #830 mantém os 5 `FAILURE` de 02:56Z | PASS (estado corrigido pela ACR) | [PR 830](https://github.com/lfgranja/stardew-raizes-vivas/pull/830) |
| 10 | `gh pr comment 830 --body-file …` às 2026-11-27T23:40:58Z | `ok commented #830`; escalonamento publicado | PASS | [issuecomment-7230327583](https://github.com/lfgranja/stardew-raizes-vivas/pull/830#issuecomment-7230327583) |

## Distinção entre verde local e green remoto

Os itens 2–6 são `[EV]` locais na worktree `dependabot-830-static-pin`. Eles **não** concedem check de PR: o green remoto só existe quando `gh pr checks` reporta `pass` no SHA publicado — item 8 prova isso para #832. O rollup de #830 (item 9) permanece de 02:56Z mesmo após o PR ter sido marcado como `MERGED` às 23:39:53Z; `[INF]`: o GitHub reconheceu a mesclagem quando o commit `65f4f9b` entrou em `dev` via #832, sem ação direta de merge por este agente. O resultado de uma reexecução posterior é `[NV]`.

## Achados fora do escopo (advisory)

`LivingRoots.Verification` também fixa `SonarAnalyzer.CSharp`, mas `test_analyzer_config.py:16-17,80` não o inclui na parametrização — um bump parcial nesse projeto passaria despercebido. Não foi alterado neste ciclo: a correção mínima preserva a decisão documentada de pins literais e não muda a semântica do gate. Registrado como advisory para o SWE.
