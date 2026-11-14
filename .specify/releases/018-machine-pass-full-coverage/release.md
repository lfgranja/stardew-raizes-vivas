# Release (R7 baseline): 018 Machine-Pass Full Coverage — Audit / Attestation Cycle

- **Slug**: 018-machine-pass-full-coverage
- **Tipo**: baseline de auditoria (encadeamento R4/R7), **não** release de feature
- **Versão do mod**: 0.1.0 (**sem bump de versão**; nenhuma alteração de versionamento/publicação associada a 018)
- **Lançado em**: 2026-11-14
- **Status**: **BASELINED / NOT RELEASED** — registro interno de fechamento de cadeia de auditoria

## Contexto

018 é um esforço de **auditoria e attestation de cobertura machine-pass (host-only)**, não um incremento shippable com `spec.md` próprio. O objetivo foi inventariar, executar e registrar honestamente a cobertura aplicável, promovendo claims apenas dentro das classes de evidência suportadas.

**Validação R4 (baseline):** `.specify/validations/018-machine-pass-full-coverage/validate.md` — aprovado/baselined (audit closure, machine-promotion-only) com limitações registradas.

## Resultado da auditoria (outcome)

- **Cobertura host machine-pass (auditada):** 6 features com GATE3 `machine-pass` registradas (004, 005, 013, 014, 015, 017), cada uma com `verification/claim-records.md` e `verification/host-only-gate3.md`; 018 atua como agregação sem claim próprio.
- **Determinismo:** 19/19 cenários com evidência semântica 2× registrada (variância de logs não-byte-idêntica documentada); inclui resolução honesta de `017-refused-at-minimum` via rewrite v4 not-applicable (pass 22), com histórico preservado.
- **Mutation checks:** 15/19 decision-seam kills registrados; 4 casos/predicados sem kill viável documentados explicitamente em `verification/mutation-checks.md`.
- **Integridade de evidência:** verificações `--verify-bundle` e auditorias de claim-records registradas nos ciclos; bundles legados históricos retidos sem promoção.
- **Limites preservados (não promovidos):** farmhand guard `partial/bloqueado` (T002), transporte D1 pendente, SC-004/humano (`verified` E)/legibilidade/balance pendentes ou fora do escopo machine.

**Fontes de evidência:** `specs/018-machine-pass-full-coverage/{converge.md,plan.md,research.md,tasks.md,verification/*}` (incl. `summary.md`, `final-inventory.md`, `determinism-checks.md`, `mutation-checks.md`, `validate-2026-11-09-final.md`, cycle records).

## Escopo e exclusões explícitas

- **Sem publicação:** nenhum artefato publicado em Nexus/SMAPI/canais externos.
- **Sem git release actions:** sem tag, sem push, sem merge/release branch action, sem alteração de branch protection/required checks por este baseline.
- **Sem bump de versão:** `LivingRoots/manifest.json` e versionamento do mod permanecem inalterados por 018.
- **Sem promoção de evidência:** nada aqui constitui `human-verified`, D1, SC-004, aprovação de playtest/legibilidade/balance, ou release público.

## Rollback

- **N/A (não aplicável).** Registro documental interno de auditoria, sem release/publicação e sem mudança de versão/artefato distribuído associada.
- **Reversão documental (se necessário):** remover/reverter apenas os artefatos de baseline R4/R7 de 018 (`.specify/validations/018-machine-pass-full-coverage/validate.md`, `.specify/releases/018-machine-pass-full-coverage/release.md`) por commit normal, sem impacto em código de runtime ou evidências append-only existentes em `specs/018-machine-pass-full-coverage/verification/`.

## Comunicação

- **Interna (repositório/equipe):** 018 encerrado como baseline de auditoria com resultado machine-promotion-only e limitações registradas; cadeia R4/R7 documentada.
- **Usuários finais / canais externos / suporte público:** **nenhuma comunicação** (não houve release).
- **Stakeholders:** qualquer comunicação futura de release depende de autorização explícita, gates aplicáveis e attestation humana pendente — fora deste baseline.

## Validação pré-release (estado)

- **R4 (audit):** aprovado/baselined — `.specify/validations/018-machine-pass-full-coverage/validate.md`.
- **Converge/auditoria:** documentados em `specs/018-machine-pass-full-coverage/converge.md` e `verification/*` (estado honesto, sem fabricação).
- **Gates release/publicação:** **não executados/não aplicáveis** a este baseline.

## Próximo passo

- Nenhuma ação de release/publicação derivada automaticamente deste baseline.
- Se/quando houver autorização e ambiente: executar fluxo de release aplicável com push/checks/branch protection sob controle do mantenedor; completar attestation humana e itens bloqueados (T002/D1/SC-004/legibilidade/balance) conforme aplicável; só então considerar versão/publicação real. Até lá, 018 permanece **baselined, audit-only, not released**.
