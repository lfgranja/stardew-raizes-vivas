# Release: 017 Joja / Agrochemical Tradeoff (MVP)

- **Slug**: 017-joja-agrochemical-tradeoff
- **Versão**: 0.1.0 (SemVer MINOR — nova funcionalidade compatível; `LivingRoots/manifest.json` 0.0.1 → 0.1.0)
- **Lançado em**: 2026-11-07
- **Status**: released (escopo: release de repositório — branch `dev`; **sem publicação em canal externo** (Nexus/etc.), sem tag git nesta entrega; attestation humana ausente por desenho)

## Mudanças

Requisitos (todos em `specs/017-joja-agrochemical-tradeoff/spec.md`):

- FR-001/FR-016 — única regra controlada; sem monocultura, sem fertilizer-hook, sem economia Joja. O agroquímico aplica penalidade de solo bounded e recuperável.
- FR-002/FR-012 — telegrafia pré-uso (nome/descrição "An industrial Joja shortcut … costs 10 soil health … bounded and recoverable through your usual soil care"); paridade default/en/pt; tom cozy, factual.
- FR-003 — interação mod-controlled (right-click intercept); sem slot de fertilizante vanilla; sem Harmony.
- FR-004 — confirmação curta ao aplicar; nenhum buff mecânico 014/015/vanilla-tier (C1:A; no double-counting).
- FR-005/FR-006 — penalidade bounded `P=10` (provisional, tunável; `AgrochemicalSoilPenalty`); baseline desconhecida `NaN` → 100, escrita absoluta medida (C2:B); `RefusedSoilAtMinimum` em baseline 0 (adjudicado 2026-11-06); resultado expõe `baseline_used`/`post_penalty`/requested/applied/clamped.
- FR-007/FR-008 — feedback pós-uso atribuído (keys `applied`/`applied-clamped`/`applied-unknown-baseline`); recusas com causa + não-aplicação + próximo passo (5 classes × 3 locales); fallback não-enganoso; `RefusedCommitFailed` para commit-drop com causa dedicada (Phase 10).
- FR-009/FR-010 — host-authoritative; lock first-wins por tile; recusa explícita em conflito; broadcast FR-009 apenas após commit; nenhum segundo source of truth.
- FR-011/FR-014 — persistência via seam existente (`ISoilHealthService.SaveData/LoadData`; zero medido não sobrevive save/load — limitação pré-existente registrada `measured-zero-persistence.md`); valores tunáveis em `ModConstants`.
- FR-013/FR-015 — sem assets obrigatórios (ícone: fallback springobjects aceito explicitamente; legibilidade in-game não verificada); verificação honesta: claims em `claim-records.md` (12 rows; host-only machine-pass ×2; D1/human/SC-004 pending).
- SC-001/SC-002/SC-003/SC-005/SC-006 — cobertos por 119 targeted + 1871 full (0 failed; 4 skips spec-009) + Gate 3 host-only pass (commit c9ff596; bundle `evidence-bundle-run-20261107T040235Z-589a`). SC-004 e SC-001 do caminho humano/manual permanecem não-atestados.

Tarefas (T001–T104): Phases 1–7 (feature), Phase 8 (Gate 3 headless, T056–T063), Phase 9 (convergence, T064–T089), Phase 10 (convergence pós-Phase-9, T090–T104). Validação: `.specify/validations/017-joja-agrochemical-tradeoff/validate.md` (**aprovado**, 2026-11-07).

## Plano de Rollback

- **Como reverter**: (1) substituição da pasta `Mods/LivingRoots` da instalação do jogo por build anterior (o backup da versão 0.0.1 existe como `LivingRoots/bin/LivingRoots 0.0.1.zip` e via git revert do commit `d3ada42` + rebuild); (2) rollback de código: `git revert` da faixa T056→T104 (ou do release commit) e rebuild — o spec é host-only por tile, revert total é seguro e não corrompe saves (única mutação = `SetSoilHealth` bounded 0–100 em solo arado do tile aplicado; nenhum formato de save alterado; item `LivingRoots.JojaAgrochemical` some da existência sem migração de dados).
- **Critérios de acionamento**: qualquer crash/regressão reportada em solo/compost/harvest ligada ao agroquímico; contradição de claims (ex.: item consumindo sem aplicar, solo fora de 0–100); feedback falso de sucesso confirmado em jogo; qualquer violação de bound em save de jogador.
- **Prazo para decisão**: 7 dias corridos após o release; decisão por `git revert` do release ou hotfix PATCH (0.1.1).

## Comunicação

- **Usuários**: nova entrada "Joja Agrochemical" utilizável apenas via debug/grant de verificação — **não há caminho de aquisição em jogo neste release** (decisión de descope 2026-11-06); nenhum jogador comum verá o item. Nenhuma ação necessária.
- **Suporte/QA**: limitações conhecidas — Gate 3 host-only (não multiplayer real); D1/human-verified/SC-004 pendentes; zero medido não persiste em save/load; ícone = placeholder vanilla (ilegibilidade in-game não verificada); `P=10` provisional; sem aquisição in-game.
- **Vendas/Partes interessadas**: 017 entrega a primeira decisão destrutiva do loop de solo (trade-off cozy de agroquímico) apenas no nível machine-pass host-only; comunicação pública deve dizer "MVP verificado em máquina, playtest humano pendente". Não comunicar como feature completa para jogadores até release com aquisição in-game e attestation humana.

## Validação Pré-Release

- Converge: **limpo para o escopo MVP** — Phase 10 fechou todos os gaps conhecidos (T090–T104 `[x]`); ressalva de processo: nenhum `/speckit.converge` re-executado ponta a ponta pós-Phase-10 (próxima rodada esperada reportar limpo; Phase 9→10 demonstra a tendência).
- Smoke de ponta a ponta: **aprovado** (validate.md — S1–S6 via suíte automatizada + Gate 3 host-only pass, commit c9ff596).
- Testes de regressão: **aprovado** — 1871 passed / 0 failed / 4 skipped (2026-11-07); format gate exit 0; shell suite 49/0 (1 skip de ambiente) na Phase 9, `test-017-agrochemical.sh` 23/23 e claims validator PASS hoje.
- Não liberado em canal público; `git tag` não criado (SemVer registrado apenas em manifest/release.md; tag é ação de publicação e fica a critério do mantenedor).

## Histórico / Auditoria

Release anterior de feature: nenhuma marcada como "released" (histórico anterior = machine-pass por spec, ver `ROADMAP.md` / `COMPLETION_SUMMARY.md`, mantidos append-only pelo runner). Provenance: manifest 0.0.1→0.1.0 (`d3ada42`); validação (`.specify/validations/017-joja-agrochemical-tradeoff/validate.md`); evidência Gate 3 (`specs/017-joja-agrochemical-tradeoff/verification/host-only-gate3-phase10-run.md`, `test.md` honesty R).

## Próximo Passo

1. (Opcional, recomendado) executar `/speckit.converge` uma vez sobre o estado pós-release para fechar a ressalva de processo.
2. Playtest humano (S1–S5 manual após caminho de aquisição escopado), execução SC-004 `sc004-v1`, sessão D farmhand (roadmap D1) e attestation humana `human-verified` — pendentes, fora deste release.
3. Publicação externa (Nexus) somente após (2) e decisão do mantenedor.

## Addendum de escopo — machine-pass vs. build released 0.1.0 (2026-11-07, Phase 11 / T112)

**Append-only; o texto acima não é reescrito.** Declaração de escopo que o corpo deste release não deixa explícita:

- O **machine-pass** que sustenta as claims de Gate 3 acima atesta o commit **`c9ff596`**, no qual o build registrado em `manifest.json` era **0.0.1**.
- O build **released** é o commit **`d3ada42`** (manifest **0.1.0**), que em relação a `c9ff596` difere, para o mod, **apenas** pela string de `Version` em `manifest.json`. Verificação por diff em 2026-11-07: entre `c9ff596` e `d3ada42` o único delta em `LivingRoots/` é `manifest.json` (`0.0.1` → `0.1.0`); **nenhum** `.cs`, `.csproj` ou `.sln` difere no intervalo; os demais deltas são artefatos de evidência/docs produzidos pelos commits do runner e de tarefas (bundle `run-20261107T040235Z-589a/`, `test.md`, `tasks.md`, `claim-records.md`, `summary.md`, `ROADMAP.md`, `COMPLETION_SUMMARY.md`). i18n, testes, cenário e código são idênticos para fins de comportamento do mod.
- **Decisão registrada (2026-11-07): DISCLOSURE, não re-run.** O Gate 3 não foi re-executado sobre `d3ada42`; fica registrado aqui que o caminho de re-run é conhecido (`./scripts/verify-scenario.sh --scenario specs/017-joja-agrochemical-tradeoff/scenarios/agrochemical-tradeoff.scenario.json --no-acquire --game-path "<game>" --skip-build`) mas **não** foi executado por este addendum — a decisão de re-rodar ou aceitar a disclosure é do mantenedor/parent session (um later task pode agendá-la). Enquanto isso valer, vale a disclosure abaixo e nenhuma claim é promovida.
- **O build 0.1.0 não foi Gate-3-run isoladamente.** A evidência de Gate 3 disponível (bundles `run-20261107T015936Z-7f35` e `run-20261107T040235Z-589a`) atesta o path medido 50→40 nas árvores c9ff596 (e e5882c1, Phase 8), não uma execução de Gate 3 sobre o artefato 0.1.0 como tal.
- O campo de mod-version registrado nos bundles pelo runner (**`0.4.0` nas duas execuções históricas**) é **declared-not-observed** — cópia verbatim do `environment.modVersion` declarado no cenário (antes da correção T110), nunca validado contra o build efetivamente carregado. O cenário foi corrigido para `0.1.0` em 2026-11-07; bundles futuros (Phase 12+) registrarão `0.1.0` sob a mesma declaração, sujeitos à mesma limitação. Nenhuma conclusão sobre "qual versão rodou" pode ser extraída do campo mod-version dos bundles históricos.
- Nenhuma desta declaração é uma promoção de evidência; `human-verified`, `d1-farmhand-convergence` e SC-004 permanecem `pending`. A diferença c9ff596→d3ada42 é um bump de string; o risco de divergência funcional é restrito a metadados de carregamento (SMAPI `manifest.json` `Version`), que nenhum dos dois Gate 3 runs mediu.
