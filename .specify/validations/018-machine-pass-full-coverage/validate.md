# Validação (R4 baseline): 018 Machine-Pass Full Coverage — Audit / Attestation Cycle

- **Slug**: 018-machine-pass-full-coverage
- **Tipo**: audit/attestation coverage effort (não é incremento de feature com `spec.md` próprio)
- **Validado em (baseline)**: 2026-11-14
- **Direção da auditoria**: `specs/018-machine-pass-full-coverage/`
- **Resultado**: **APROVADO / BASELINED (audit closure, machine-promotion-only)** — com limitações registradas. Este registro fecha a lacuna da cadeia R4/R7 para 018; **não** promove evidência acima do que foi observado.

## Pré-requisitos e artefatos de referência

- **Sem `spec.md`**: 018 é um ciclo de auditoria/attestation de cobertura machine-pass (host-only), não uma feature entregável com especificação própria.
- Artefatos presentes e consultados:
  - `specs/018-machine-pass-full-coverage/plan.md`
  - `specs/018-machine-pass-full-coverage/research.md`
  - `specs/018-machine-pass-full-coverage/quickstart.md`
  - `specs/018-machine-pass-full-coverage/tasks.md`
  - `specs/018-machine-pass-full-coverage/converge.md`
  - `specs/018-machine-pass-full-coverage/verification/` (incl. `summary.md`, `final-inventory.md`, `determinism-checks.md`, `mutation-checks.md`, `validate-2026-11-09-final.md`, cycle records e `t002-farmhand-host-guard-attempt.md`)
- Evidência de validação anterior (mesmo escopo, auditada no repositório): `specs/018-machine-pass-full-coverage/verification/validate-2026-11-09-final.md` (2026-11-09, pass 23) — **aprovado, machine-promotion-only**; mantém-se como registro histórico detalhado. Este arquivo `.specify/validations/.../validate.md` é o **baseline R4** para encadear o fluxo R4/R7.

## Escopo auditado (honesto)

018 cobriu auditoria de cobertura de machine-pass em paths host aplicáveis das features com código executável/runnable paths no plano (004, 005, 013, 014, 015, 017), com promoção/evidência append-only conforme runner/contratos 010/016. O trabalho **não** incluiu mudança de comportamento de gameplay como objetivo, **não** incluiu publicação/release e **não** incluiu attestation humana.

**Resumo de resultado (conforme `verification/summary.md`, `converge.md`, `final-inventory.md`, `validate-2026-11-09-final.md`):**
- **Determinismo (2×, mesma seed/save-mode/binaries/ambiente, semântico):** 19/19 cenários cobertos no ledger atual (com `017-refused-at-minimum` restaurado a verde por reescrita honesta v4 not-applicable no pass 22; histórico de flake/assertion stale registrado).
- **Mutation checks (controles negativos, decision-seam kills):** 15/19 mortos; 4 cenários/predicados explicitamente documentados como sem seam de decisão viável para kill no escopo atual (`013-decay-exemption`, `013-refused-on-ineligible`, `017-refused-contention`, e limites/predicados remanescentes de `017-refused-at-minimum` pós-rewrite), conforme `verification/mutation-checks.md`.
- **População de claims:** 6 specs com bloco GATE3 `machine-pass` (004, 005, 013, 014, 015, 017), cada uma com `verification/claim-records.md` e `verification/host-only-gate3.md`. 018 é agregação e não carrega claim machine-pass próprio além dos registros por feature.
- **Integridade de evidência (auditoria registrada):** bundles citados re-verificados nos ciclos (incl. `--verify-bundle` exit 0 nas reauditorias registradas). Falhas legadas conhecidas/referenciadas historicamente permanecem retidas como histórico, não promovidas e não usadas por GATE3/claim-records atuais.
- **Gates determinísticos/smoke (conforme ledgers):** format, docs markers/doc links, suites/checks e gates locais registrados verdes nos ciclos citados; smoke launch-free teve certificações/revalidações com ressalvas de ambiente documentadas. Nenhum gate é re-executado por este baseline.

## Critérios (SC-MP) — status de auditoria

| Critério | Status | Evidência / nota |
|---|---|---|
| SC-MP-1 host paths com machine-pass ou bloqueio/infeasibilidade explícita | **PASS (auditado)** | `converge.md`, `summary.md`, `final-inventory.md`, registros por feature |
| SC-MP-2 host exercisable com 3 sinais concordantes + bundle verificável | **PASS (auditado)** | runs/bundles e `--verify-bundle` nos ledgers/ciclos |
| SC-MP-3 farmhand paths como split-screen guard, `partial`, nunca `machine-pass` | **PARTIAL por design** | T002/T013/T019/T022/T028; `verification/t002-farmhand-host-guard-attempt.md` |
| SC-MP-4 013 mulch host machine-pass ou infeasibilidade registrada | **PASS (auditado)** | `specs/013-mulch-soil-cover/test.md` e registros de cenário/run |
| SC-MP-5 sem promoção acima da evidência; D1/human/SC-004 pendentes/bloqueados; append-only | **PASS (auditado)** | `claim-records.md`, promoção/GATE3 conforme runner; limites explícitos |
| SC-MP-6 mutation-check por asserção nova (com exceções documentadas) | **PASS com exceções documentadas** | `verification/mutation-checks.md` (15/19 kills; 4 não-killáveis documentados) |
| SC-MP-7 determinismo 2× por cenário novo antes de promoção | **PASS (auditado)** | `verification/determinism-checks.md` (19/19 semântico) |
| SC-MP-8 sem fabricação | **PASS (auditado)** | bundles/ledgers append-only; falhas legadas retidas e rotuladas; sem GATE3 manual |

## Conformidade com a Constituição (nível de auditoria)

- **I. DDD / II. Segurança / III. Async-only:** sem violação registrada nos artefatos auditados.
- **IV. TDD:** evidência de testes/checks registrada nos ledgers/ciclos (não re-executada aqui).
- **VI. Multiplayer first-class / honestidade:** estado/autoridade host preservados; farmhand transport/guard headless **não** promovido a `machine-pass` (parcial/bloqueado por desenho e ambiente).

## Limitações e ressalvas (obrigatórias)

1. **Não é release de feature nem incremento shippable:** 018 não possui `spec.md` e não introduz entrega publicável própria.
2. **Machine-promotion-only:** `machine-pass` ≠ `human-verified`. Attestation humana, playtest, legibilidade/render, balance e SC-004 permanecem pendentes/fora do escopo machine.
3. **Farmhand/D1:** split-screen/headless `isMainPlayer` flip não demonstrado (T002); cenários farmhand permanecem `blocked-feasible/partial`. Transporte D1 pendente/bloqueado.
4. **Bundles legados:** falhas históricas conhecidas retidas por política append-only/retain-only; não representam claims atuais.
5. **Sem push/merge/tag/publicação:** nenhuma ação de distribuição/publicação externa/tag/proteção de branch realizada por este ciclo.
6. **Reprodutibilidade de ambiente:** revalidações com ressalvas de ambiente documentadas nos ciclos; este baseline não re-executa comandos.

## Resumo

018 está **aprovado e baselined como auditoria de cobertura machine-pass (host-only)** com evidência append-only suficiente para fechar a lacuna R4, dentro dos limites explícitos acima. Nada é promovido a humano/verificado/D1/SC-004/release.

## Próximo passo

- Baseline R7 em `.specify/releases/018-machine-pass-full-coverage/release.md` (registro de auditoria, **não** release de feature, sem bump/publicação).
- Itens remanescentes (autoridade/ambiente): push/checks/branch protection, attestation humana (D1/SC-004/legibilidade/balance) e viabilização de farmhand headless (T002) — fora deste baseline.
