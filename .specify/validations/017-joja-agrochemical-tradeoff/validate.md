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
