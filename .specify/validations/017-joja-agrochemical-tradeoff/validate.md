# Validação de Entrega: 017 Joja / Agrochemical Tradeoff (MVP)

- **Slug**: 017-joja-agrochemical-tradeoff
- **Validado em**: 2026-11-07
- **Direção da funcionalidade**: specs/017-joja-agrochemical-tradeoff/
- **Resultado**: aprovado (com ressalvas de escopo registradas abaixo — nenhuma bloqueia o release)

## Pré-requisitos

- `tasks.md`: **104/104 tarefas marcadas `[X]`** (Phases 1–10). Converge executado duas vezes (`## Phase 9: Convergence`, `## Phase 10: Convergence`), com todos os gaps fechados pela implementação subsequente.
- Ressalva de processo: após o fechamento da Phase 10 (T090–T104), nenhum `/speckit.converge` re-executado ponta a ponta foi rodado; não há gaps conhecidos abertos (Phase 10 os fechou todos, T090–T104 `[x]`), mas a evidência auditável de "converge limpo pós-Phase-10" não existe. Registrada como nota, não como bloqueio técnico.
- `spec.md`, `plan.md`, `tasks.md`, `quickstart.md` presentes. Constituições lidas (`.specify/memory/constitution.md` v2.0.0).

## Cenários de Smoke de Ponta a Ponta

| Cenário (de spec.md/quickstart) | Comando executado | Resultado | Saída relevante |
|----------------------|-------------------|-----------|-----------------|
| S1 — baseline desconhecida aplica penalidade medida explícita (C2) | `dotnet test … --filter "FullyQualifiedName~Agrochemical"` (`AgrochemicalApplicationServiceTests.UnknownBaseline_*`) | pass | 90 medido, `UpdateHealth` nunca usado; persistence seam test (mocked) verde |
| S2 — penalidade medida é bounded e atribuível (C1/C3, 50→40) | idem + Gate 3 headless | pass | `clamp(current-10, 0, 100)`; Gate 3: `evidence-bundle-run-20261107T040235Z-589a` (Farm 60,15: before 50 → after 40, delta −10) |
| S3 — caminho farmhand não suportado recusado com segurança (C4) | `dotnet test` (`ModControllerAgrochemicalTests.*UnsupportedAuthority*`) | pass | recusa antes de mutação/consumo/feedback/broadcast; no unsafe local mutation |
| S4 — host authority, lock, FR-009 sync (MP contract) | `dotnet test` (`OnHostSuccess_*`, `OnLockContention_*`, `WithoutLockService_*`) | pass | ordem acquire→commit→release→consume→feedback→broadcast; conflito = recusa explícita |
| S5 — recusas e alvos inelegíveis | `dotnet test` (`InvalidTarget_*`, `MissingInput_*`, overlong-name guard T099, `OnServiceConflictRefusal_*`) | pass | recusa localizada com causa + não-aplicação + próximo passo (pt/en/default) |
| S6 — paridade de localização | `dotnet test` (`AgrochemicalContentParityTests`, 45 cases) | pass | chaves 017 em default/en/pt; tone/boundary proxies |
| **Gate 3 (host-only, headless)** | `./scripts/verify-scenario.sh --scenario specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-tradeoff.scenario.json --no-acquire --game-path "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build` | **pass** | `verdict: pass`, `signalsComplete: true`, `signalsAgree: true`, `thresholdsOk: true`, `farmhandBreach: false`, offline `--verify-bundle` exit 0; `test.md` promovido pelo runner para `machine-pass` (honesty `R`), bundle `run-20261107T040235Z-589a`, commit `c9ff596` |

Observação de escopo: os cenários S1–S6 são atestados por testes automatizados + Gate 3 host-only machine-evidence — não houve playtest manual em sessão SMAPI interativa. O caminho humano/manual (S1–S5 manuais com grant de item em jogo) permanece bloqueado por inexistência de caminho de aquisição in-game (decisão de descope 2026-11-06, `verification/provisioning-validation.md`).

## Suite de Regressão

- Comando: `GamePath=/home/luis/stardew-assemblies dotnet test Stardew-LivingRoots.sln --configuration Release`
- Resultado: **1871 passed / 0 failed / 4 skipped** (spec-009 pré-existentes), 1875 total — reproduzido hoje (2026-11-07) em `/tmp/opencode/regression-017-validate.log`
- Targeted 017: **119/119** (`/tmp/opencode/targeted-017-validate.log`)
- `dotnet format --verify-no-changes`: exit 0
- Shell smoke suite: `SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh` em execução no fechamento desta validação; `test-017-agrochemical.sh` **23/23**; validator `scripts/tests/test-017-claims.sh` **PASS** (12 rows, digests dos 2 bundles ok)

## Conformidade com a Constituição

- Governo: `.specify/memory/constitution.md` v2.0.0.
- Princípios MUST verificados: I (DDD — Domain sem usings SMAPI; fronteiras Domain/Services/Controllers preservadas), II (segurança — bounds 0–100, `SanitizePathSegments`, sem raw exception messages em logs), III (async-only — sem `.Result`/`.Wait()`), IV (TDD — suites RED→GREEN nas Phases 2/9/10), VI (MP first-class — host-authoritative, lock per-tile first-wins, farmhand recusado pre-mutation; nenhum caminho farmhand mutável).
- Violações MUST encontradas: **nenhuma**.
- Gate 3 (runner) satisfeito para o caminho medido 50→40 (machine-pass, honesty R). Gate 3 manual interativo permanece pendente (human attestation, fora de escopo MVP).

## Resumo

A funcionalidade **atende à especificação no nível de evidência disponível**: o caminho de aplicação host é verificado ponta a ponta por máquina (Gate 3, três sinais concordantes), a suíte automatizada está verde (1871/0/4, targeted 119/119) e a constituição não foi violada. Regressões: nenhuma. A validação libera para `/speckit.release`.

## Ressalvas e bloqueios para Release

- **Nenhuma violação bloqueante.** Ressalvas registradas (nenhuma promove classes de evidência):
  1. `d1-farmhand-convergence` (farmhand real): **pending**.
  2. `human-verified` (attestation humana): **pending**.
  3. SC-004 (comprehension ≥90%, protocolo `sc004-v1`): **pending** — nunca executado.
  4. Arredondamento de zero medido não sobrevive a save/load (sparse-cache pré-existente; row `blocked`).
  5. Aquisição em jogo (path manual S1–S5): **bloqueada/descopada** por decisão 2026-11-06.
  6. Gate 3 cobre apenas o caminho medido 50→40; C2 unknown-baseline é coberto por testes automatizados.
  7. Deriva documental menor: `quickstart.md` §"017 status" cita o bundle Phase-8 (`…015936Z-7f35`); o run atual é `…040235Z-589a` (commit c9ff596). Não altera resultados.
  8. Converge re-executado pós-Phase-10: não rodado (ver Pré-requisitos).

## Próximo Passo

- Aprovado para `/speckit.release`. Antes de marcar o pacote de release como `released`, considerar rodar `/speckit.converge` uma vez para fechar a ressalva de processo — ou aceitar explicitamente o estado (Phase 10 fechou todos os gaps conhecidos; não há artefato pendente de converge).

## Addendum corretivo (2026-11-07, Phase 11 / T111) — sem alteração das linhas anteriores

**Append-only; o conteúdo acima é preservado.** Correções e registros contra os números e referências desta validação:

1. **S6 — enumeração estática corrigida.** A linha S6 acima cita "`AgrochemicalContentParityTests`, 45 cases" — o número de 45 era a contagem pré-Phase-10 (T081-era). Recontagem estática de `LivingRoots.Tests/AgrochemicalContentParityTests.cs` em 2026-11-07: **16 Theories carregando 60 `[InlineData]` + 5 `[Fact]` = 65 casos enumerados estaticamente** (não 64 = 4 Facts + 60 InlineData, como chegou a ser fornecido para esta nota — a contagem de Facts não pôde ser reproduzida a partir do fonte e fica registrada como discrepância, não adotada). O total targeted de **119** (exit 0, `verification/convergence-phase10-targeted.log`) não é afetado por esta correção; a decomposição por classe do 119 não foi re-enumerada (mesmo critério do `convergence-rerun.md` e do `quickstart.md` refresh de T105). A figura "45" nas demais partes do documento permanece como registro histórico pré-Phase-10.
2. **Ressalva 7 estendida** (registrada aqui, sem reescrever a entrada original): além da citação do bundle Phase-8 no §"017 status" do quickstart, a deriva documental coberta por esta validação inclui também (a) a **omissão da tabela de artifacts** no quickstart (ausentes `verification/host-only-gate3-phase10-run.md`, `evidence-bundle-run-20261107T040235Z-589a/`, `convergence-phase10-*.log` — corrigido em T105), (b) o **heading 108** do quickstart ("optional, if runner/scenario added later" — cenário existe e rodou nas Phases 8 e 10; corrigido em T105), (c) o **drift 88-vs-119** da enumeração targeted (corrigido em T105; logs commitados `verification/convergence-phase10-*.log` são a referência), e (d) a **preferência de citação `/tmp/opencode`**: citações de `/tmp/opencode/*.log` devem dar lugar aos logs commitados (`verification/convergence-phase10-targeted.log`, `-full.log`, `-red.log`); o `run-all.sh` executado no fechamento desta validação deixou `/tmp` apenas como contexto de sessão, não como provenance. Ressalva 7 e suas extensões são derivadas documentais; nenhuma altera resultados.
3. **Shell smoke suite — resultado final registrado.** O estado "em execução no fechamento desta validação" (seção Suite de Regressão, acima) fica encerrado assim: `SMOKE_SKIP_INTEGRATION=1 scripts/tests/run-all.sh` → **exit 0 — 50 arquivos, 49 passed, 0 failed, 1 skipped** (um skip por ambiente). Consistente com o registrado em `convergence-rerun.md` e na Phase 9.
4. **modVersion drift (T110).** O cenário `scenarios/agrochemical-tradeoff.scenario.json` declarava `environment.modVersion: "0.4.0"` até 2026-11-07; corrigido para **`0.1.0`** (SemVer válido, casa com o manifest release em `d3ada42`). **Ambos os bundles rastreados registram `0.4.0`** (`evidence-bundle-run-20261107T015936Z-7f35/` Phase 8; `evidence-bundle-run-20261107T040235Z-589a/` Phase 10) porque o runner copia o campo declarado verbatim para o `modVersion` do bundle — é **declared-not-observed**, nunca validado contra o build efetivamente implantado (o campo foi corrigido apenas agora). `time` no cenário não é o wall-clock do run (timestamps reais: bundles/run records, ex. Phase 10 = 2026-11-07T04:03:17Z); `rngSeed` é contexto declarado legal por schema. Bundles futuros (Phase 12+) registrarão `0.1.0`. Nenhuma linha desta validação é alterada por isso.
5. **`release.md` paralelo.** Observação análoga (modVersion declared-not-observed, bundles 0.4.0) é registrada também no `release.md` via addenda de escopo/wording da Phase 11 (T112/T113).

Nenhuma ressalva promove classes de evidência; `d1-farmhand-convergence`, `human-verified` e SC-004 permanecem `pending`; as duas linhas `blocked` permanecem `blocked`.
