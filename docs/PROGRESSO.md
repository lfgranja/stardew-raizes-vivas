# PROGRESSO.md — Living Roots / Raízes Vivas

Âncora de progresso verificável. Atualizações são append-only: fatos locais executados são registrados com evidência; afirmações remotas são marcadas `[NV]` até observação ao vivo via `gh`.

## Estado verificado — 2026-11-24 (pós-promução #821)

- **Âncora**: ALVO `/home/luis/development/stardew-raizes-vivas`, `dev=c96eef5`, `origin/main=8a0407e`; FONTE_FACTORY `b1deab1` (somente leitura, árvore suja não transplantada).
- **Promoção**: PR [#821](https://github.com/lfgranja/stardew-raizes-vivas/pull/821) `MERGED` às 2026-11-24T13:31:22Z, merge commit `8a0407e`, por autorização explícita do SWE. Review requirement e `required_signatures` foram removidos apenas na janela do merge e restaurados; `main` permanece com 1 aprovador, `strict:true`, `sdd-gate`, assinaturas e `enforce_admins:false` (admin bypass). 0 tags, 0 releases; nenhum deploy disparado (`release.yml` só em tags `v*.*.*`).
- **Gates locais**: `sdd-gate` PASS (0 erros, 41 warnings); `test-spec-consistency.sh` PASS (1683 checks, C-SCN-36 OK); `check-protected-paths.sh` all checks passed. Nenhuma suíte xUnit/Gate 3 reexecutada — nenhuma mudança de código nesta sessão.
- **Contagens por script**: `specs/`=34 diretórios (33 numerados, 30 com `spec.md`), `.specify/assessments/`=29, `validations/`=33 (inclui `pos-promocao-821/`, criado neste ciclo), `releases/`=32, `bugs/`=40, `docs/research/`=5 arquivos. 7 `validate.md` (031–037) permanecem `PENDENTE`.
- **Decisão 80/20**: da fila, nenhum candidato `IMPLEMENTAÇÃO`/`CORREÇÃO` elegível — `ZERO-PERSIST` tem adjudicação T100, bandas 010 são grandfathered por 032/014, rotação depende de E/D1, T189–T191 falham o filtro de consumidor. Anti-loop 0.9.3: **PARE**; nenhuma feature nova.
- **[NV]**: deploy/produção, tag `v0.2.0`, D1, atestação E, SC-004, `RUNNER_AUDIT_STORE` (#523), segredos Dependabot e decisão sobre `enforce_admins`. Blocos abaixo descrevem #821 como aberto/`[NV]` e são histórico supersetado por esta nota.

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


## Estado verificado — 2026-11-22 (ciclo 031)

- **Âncora**: alvo `/home/luis/development/lr-cycle031` (worktree de `dev` @ `f2bf6c7`), branch `feature/031-nv-remote-log-closure`; `.specify/feature.json` → `specs/031-nv-remote-log-closure`; factory `b1deab1` (somente leitura).
- **[NV] resolvido**: causa step-level dos PRs #790–#796 verificada ao vivo via `gh run view --log-failed` (runs 44641194770, 44664287245, 44664266743) e `gh api .../dependabot/secrets` (vazio). Evidência em `research/2026-11-22-remote-nv-closure.md`.
- **Limite de honestidade**: D1, atestação E de Gate 3, SC-004, tag/release `0.2.0`, `RUNNER_AUDIT_STORE` (#523) e aprovador distinto permanecem **[NV]**/human-gated. Nenhuma promoção adicional.
- **Escopo**: documentação/SDD apenas; zero diff em `LivingRoots/`, `.github/workflows/` e `scripts/ci/`. Gate 3 real-game N/A.

## Estado verificado — 2026-11-22 (ciclo 032)

- **Âncora**: worktree `/home/luis/development/lr-cycle032` (base `dev` @ `f2bf6c7`), branch `feature/032-alinhar-bandas-cenario`; `.specify/feature.json` → `specs/032-alinhar-bandas-cenario`; NNN=032 forçado via `--number` porque o ciclo 031 está em voo em outro worktree. Factory `b1deab1` (somente leitura).
- **Mudança**: 4 linhas em 2 cenários ativos (`compost-growth-healthy`, `soil-health-growth`): `min 67 → 71` e descrição Healthy `[71, 100)`, coerente com `ModConstants.ModerateHealthyBandEdge = 71f`.
- **Gate 3**: `run-20261122T040544Z-738e` (004) e `run-20261122T041058Z-73a0` (005), ambos `verdict=pass`, honestidade R, 3 sinais concordantes, `--verify-bundle` exit 0; blocos promovidos pelo mecanismo determinístico do runner. Duas tentativas `errored`/W por `evaluator-unavailable` em worktree novo foram registradas e não promovidas.
- **Limite**: `machine-pass` host-only; D1, atestação E de Gate 3, SC-004, tag/release `0.2.0`, `RUNNER_AUDIT_STORE` (#523) e aprovador distinto permanecem `[NV]`/human-gated.

## Estado verificado — 2026-11-22 (fechamento autônomo)

- **Merges**: #817 → `c3c904e` (04:50:07Z) e #818 → `686fea7` (04:56:20Z), ambos com `mergeStateStatus=CLEAN` e checks obrigatórios verdes observados ao vivo via `gh`; **0 PRs abertos**.
- **Ciclo 031**: `[NV]` step-level de #790–#796 resolvido com logs de `gh run view --log-failed` (runs 44641194770/44664287245/44664266743) e prova de que `BUILDDEPS_TOKEN` existe como segredo de repositório mas não no escopo Dependabot.
- **Ciclo 032**: banda Healthy alinhada a 31/71 em 2 cenários ativos (4 linhas); Gate 3 `run-20261122T040544Z-738e` e `run-20261122T041058Z-73a0`, ambos `pass`/R/3 sinais; blocos promovidos pelo runner.
- **Gates locais**: `aggregate-gate.sh` ALL STEPS PASSED nos dois ciclos; i18n PASS 0/0; pytest 180/0; step 4 isolado do 031 = 1999/0/4. `release-gate.sh` interrompido por teto do harness durante o smoke, sem FAIL — não promovido a PASS.
- **[NV / humano]**: D1, atestação E de Gate 3, SC-004, tag/release `0.2.0`, `RUNNER_AUDIT_STORE` (#523), aprovador distinto. Fila autônoma extinguida.

## Estado verificado — 2026-11-22 (correção de atribuição, ciclo 034)

- **SemVer 0.1.7 restaurado** no release 032 (PATCH, zero mudança jogável).
- **NV — atribuição**: mecanismo `BUILDDEPS_TOKEN` ausente no escopo Dependabot verificado nos runs de **19–20/11** (`44641194770`, `44664287245`, `44664266743`); dois deles correspondem a #811/#812. Não houve leitura direta dos runs de 18/11; estender a causa a #790–#796 é inferência do mesmo mecanismo, explicitamente marcada.


## Estado verificado — 2026-11-22 (promoção dev→main, ciclo 035)

- **PR #821** `dev`→`main` aberto; CI **integralmente verde** (`gh pr checks --watch` RC=0 às 05:31:56Z). Único bloqueio: `REVIEW_REQUIRED` (1 aprovador distinto, proteção strict/enforce_admins lida ao vivo).
- **Fila SWE**: item `ESC-LR-20261122-008` adicionado ao registro oficial de escalonamentos, status `escalado-swe`. Merge em `main`, tag `v*.*.*` e release pública permanecem **[NV]/owner-gated**. Nenhuma operação owner foi executada.
- **Escopo do ciclo**: apenas documentação/SDD; zero código, workflow, cenário ou contrato alterado.

## Estado verificado — 2026-11-22 (refresh do CI green, ciclo 036)

- **PR #821**: CI reexecutado no HEAD `b3eaa94`, `RC=0` às 05:40:50Z; único bloqueio `REVIEW_REQUIRED` (aprovador distinto, SWE-gated). Fila SWE atualizada com `atualizacao_ci` sem apagar o registro anterior.
- **Smoke 031**: **51 arquivos / 50 passed / 0 failed / 1 skipped**, `RC=0` às 05:42:22Z; evidência local do ciclo 031 completa por etapas isoladas (gate interrompido por teto do harness, sem FAIL).
- **NV owner**: merge em `main`, tag `v*.*.*`, release pública, D1, atestação E, SC-004 e `RUNNER_AUDIT_STORE` seguem não executados.
