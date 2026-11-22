# PROGRESSO.md — Living Roots / Raízes Vivas

Âncora de progresso verificável. Atualizações são append-only: fatos locais executados são registrados com evidência; afirmações remotas são marcadas `[NV]` até observação ao vivo via `gh`.

## Estado verificado — 2026-11-20

- **Âncora**: alvo `/home/luis/development/stardew-raizes-vivas` @ HEAD `7df732b`. `.specify/feature.json` → `specs/028-quality-gate-pytest-wiring`. Fonte de aparato: factory `b1deab1` (somente leitura).
- **Ciclos entregues**: 026 (overlay cache), 027 (flakiness de relógio de parede), 028 (quality-gate pytest wiring). PR #810 (ciclo 028) `MERGED` em 2026-11-20T02:13:21Z.
- **Gates locais**: `bash scripts/ci/aggregate-gate.sh` → **ALL STEPS PASSED** nesta sessão. `scripts/release-gate.sh` registrado **ALL STEPS PASSED** em `047228e` (7 etapas, `GamePath=/home/luis/stardew-assemblies`; smoke launch-free 51 arquivos / 50 passed / 0 failed / 1 skipped).
- **Suítes registradas**: pytest quality-gate 180/0 após o ciclo 028; xUnit 1999 passed / 0 failed / 4 skipped (2003) no ciclo 027; Gate 3 host-only `machine-pass` com 3 sinais concordantes (bundle `evidence-bundle-run-20261120T012343Z-6b29`), sem `verified` humano.
- **Contagens por script**: 25 diretórios em `specs/` (21 com `spec.md`), 20 assessments, 23 validations, 23 releases, 40 bugs, 24 arquivos de research. Nenhum `*.generated.md` existe.
- **PRs abertos [NV para merge]**: #811 e #812, ambos `MERGEABLE`; `sdd-gate` e `verify` pass, mas `Build and Test`, `Check Formatting` e Quality Gate build-analysis/gate falham por `BUILDDEPS_TOKEN is not set` no contexto de segredo do Dependabot. Não é regressão de código; merge bloqueado por provisão owner-gated.
- **Dívidas autônomas**: 2 de 5 candidatos `.archify/` inválidos (sobreposição de fases/corridor; schema antigo); `docs/architecture/` ausente; seis scripts de gate da factory materializados apenas como diagnóstico (falham por baselines da factory, N/A como autoridade conforme Bloco 0.a).
- **[NV / humano]**: D1 (sessão host+farmhand ao vivo), atestação humana E de Gate 3, execução do protocolo SC-004, tag/release `0.2.0` e canais públicos, `RUNNER_AUDIT_STORE` (#523), segredos do Dependabot e aprovação de revisor distinto.

## Estado verificado — 2026-11-21 (ciclo 030)

- **Âncora**: alvo `/home/luis/development/stardew-raizes-vivas`, branch `feature/030-roteiro-verificacao-funcional` @ `64781ee`; `.specify/feature.json` → `specs/030-roteiro-verificacao-funcional`; factory `b1deab1` (somente leitura).
- **Roteiro executado**: matriz persistida em `specs/030-roteiro-verificacao-funcional/verification-matrix.md` com Camadas A (gates), B (11 grupos xUnit), C (Gate 3) e D (humano `[NV]`). B1–B11: **1378 execuções, 0 falhas**; i18n PASS; pytest 180/0; `aggregate-gate.sh` ALL STEPS PASSED.
- **Gate 3**: `compost-growth-hardening` reexecutado após alinhamento do threshold a `[0, 31)`: `run-20261121T232308Z-64cf`, `verdict=pass`, honestidade R, 3 sinais concordantes, `--verify-bundle` exit 0. `machine-pass` host-only; nenhum `verified` humano.
- **Contagens por script (23:27Z)**: specs=27 (23 com `spec.md`), assessments=22, validations=24, releases=24, bugs=40, pesquisa=26; `*.generated.md`=0; 0 PRs abertos ao final.
- **PRs**: #811 (pin coverlet 10.1.1) e #812 (JsonSchema.Net 9.7.0 + lock regenerado) conduzidos para `dev` com checks ao vivo verdes e mesclados (`048342b`, `18640ce`). Nenhum force-push/auto-merge/delete de branch.
- **[NV / humano]**: D1, atestação E de Gate 3, SC-004, tag/release `0.2.0`, `RUNNER_AUDIT_STORE` (#523), aprovador distinto. Dívida de limites legados em cenários 004/005/010 registrada, não alterada.
- **Fechamento do gate (2026-11-22T00:59:31Z)**: `release-gate.sh` **ALL STEPS PASSED** no HEAD `f4eba87` (steps 1–7); `aggregate-gate.sh` ALL STEPS PASSED e pytest 180/0 no mesmo HEAD. PR #814 mesclado em `1fed5f4`; 0 PRs abertos.
- **Reprodução do roteiro (2026-11-22)**: A ALL STEPS PASSED (release gate em `469b4c1`, 03:00:28Z); B 1378/0; C `run-20261122T030046Z-79af` pass/R/3 sinais; D `[NV]`. Evidência visual via browser (dashboard + Actions + PRs mesclados).


## Estado verificado — 2026-11-22 (ciclo 032)

- **Âncora**: worktree `/home/luis/development/lr-cycle032` (base `dev` @ `f2bf6c7`), branch `feature/032-alinhar-bandas-cenario`; `.specify/feature.json` → `specs/032-alinhar-bandas-cenario`; NNN=032 forçado via `--number` porque o ciclo 031 está em voo em outro worktree. Factory `b1deab1` (somente leitura).
- **Mudança**: 4 linhas em 2 cenários ativos (`compost-growth-healthy`, `soil-health-growth`): `min 67 → 71` e descrição Healthy `[71, 100)`, coerente com `ModConstants.ModerateHealthyBandEdge = 71f`.
- **Gate 3**: `run-20261122T040544Z-738e` (004) e `run-20261122T041058Z-73a0` (005), ambos `verdict=pass`, honestidade R, 3 sinais concordantes, `--verify-bundle` exit 0; blocos promovidos pelo mecanismo determinístico do runner. Duas tentativas `errored`/W por `evaluator-unavailable` em worktree novo foram registradas e não promovidas.
- **Limite**: `machine-pass` host-only; D1, atestação E de Gate 3, SC-004, tag/release `0.2.0`, `RUNNER_AUDIT_STORE` (#523) e aprovador distinto permanecem `[NV]`/human-gated.
