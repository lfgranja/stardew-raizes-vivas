# ADR 002 — Adoção do processo mdmv-dark-factory (parcial, nível 4)

- **Status:** Aceito (decisão registrada)
- **Data:** 2026-11-10
- **Contexto:** `mdmv-dark-factory` (hub docs-only de Spec-Driven Development) — SHA avaliado `044f153ef7eee8b6b3299b37fbfdb08075a2c480`
- **Relacionado:** `docs/governance/satellite-manifest.json`, `docs/governance/satellite-adoption-checklist.md`, `AGENTS.md`, `.specify/memory/constitution.md` v2.0.0

## Decisão

Adotar **parcialmente** o processo do mdmv-dark-factory, no nível 4 ("light factory", luzes acesas), como padrão operacional. Adoção integral (nível 5 / dark) fica restrita a jobs estreitos de baixo blast radius com verificação machine-checkable (Fase 4).

Justificativa (conforme research 12 do hub): não há evidência de adoção integral em produção em larga escala; a adoção parcial captura quase todo o ganho com risco contido; sistemas críticos, segurança/autenticação e domínios regulados demandam julgamento humano substancial.

## O que adotamos agora (Fase 0)

- Manifesto de satélite (`docs/governance/satellite-manifest.json`, schema 026.1.0) declarando `target`, `host`, `hub_sha`, `adopted_policies`, `status: parcial`.
- Checklist de adoção local (`docs/governance/satellite-adoption-checklist.md`), preenchido honestamente (itens feitos vs. em fases).
- `adopted_policies`: `022` (hub-first, como override `local-only` porque o host é GitHub, não Forgejo) e `023` (autonomia/escalonamento, via governança da constitution).

## O que fica de fora (e por quê)

- **Runtime sandbox / execução isolada de agentes (G1):** o hub ainda está no MVP (Fase 3); este repo já tem CI GitHub maduro e verification runner com Gate 3. Overkill para mod single-maintainer.
- **Forgejo / runner self-hosted / branch protections do hub (G4/G8):** este repo vive no GitHub; adotar o runner Forgejo duplicaria infraestrutura sem ganho.
- **Ingestão de telemetria do hub (G5):** o hub ainda não tem ingestão real em produção (NV). Adotaremos o padrão JSONL (035/039) localmente (Fase 2) e só publicaremos quando o hub tiver ingestão.
- **Orquestrador/harness de agentes (G2):** nível 5 não é o alvo agora.

## Fases subsequentes (roadmap)

- **Fase 1 — Fechar a cadeia SDD por feature (R4/R7):** `validate.md` aprovado antes de `release.md` para cada spec; gate determinístico `scripts/ci/sdd-gate.py` no CI. Hoje só `017` e `020` têm validation e só `017` tem release (de 17 specs). Decidir retroativo de uma vez vs. por toque.
- **Fase 2 — Execution record + telemetria (035/039):** schema local baseado em 035.1.0, JSONL por PR/sessão, validação com `validate_telemetry.py` do hub (adaptado), integrado ao `verify-scenario.sh`.
- **Fase 3 — Governança de bloqueios e drift (024/026):** escalation registry local; job periódico de drift contra `hub-policy-manifest.json`; `CODEOWNERS` para `specs/`, `.specify/`, `scripts/lib/`.
- **Fase 4 — Narrow lights-out (opcional):** um job repetitivo de baixo blast radius (bump de dependências patch, lint, parity de i18n) com verificação machine-checkable e PR com execution record; humano aprova merge; sem auto-merge cego (022/025/042).

## Zoneamento brownfield (research 11 do hub)

Antes de agentes autônomos tocarem o código:
- **Green:** `Domain/` (lógica pura) + testes — loop apertado.
- **Yellow:** `Services/` (persistência) — testes de caracterização primeiro.
- **Red:** `Controllers/ModController.cs` (estado host-authoritative), segurança/persistência, `Patches/` (Harmony) — humano passo a passo.

## Spec Kit

- Local: 1.2.5 (integração `opencode`, scripts `sh`, numeração sequencial).
- Hub: 1.3.2 (integração `omp`).
- Upgrade para 1.3.2 é **avaliado**, não obrigatório: os artefatos (templates, scripts bash, extensions `assess`/`bug`/`github`) são compatíveis; a mudança principal é a integração (opencode → omp) e o versionamento. Decisão adiada para um PR dedicado de upgrade, para não misturar com a adoção de governança.

## Consequências

- Positivas: governança de satélite rastreável, evidência por feature (R4/R7) como próximo passo claro, alinhamento com o hub sem overengineering.
- Negativas/custos: manutenção manual do manifesto e do checklist; drift check periódico; mais artefatos por feature (validate/release).
- Riscos: se a Fase 1 não for executada, o gap de evidência por feature permanece (hoje 15/17 specs sem validation).

## Alternativas consideradas

- Adoção integral (nível 5) imediata: rejeitada — sem evidência de sucesso em larga escala; risco alto em mod com estado multiplayer host-authoritative.
- Não adotar nada: rejeitada — já rodamos SDD; faltam apenas governança e evidência por feature, de baixo custo.

## Referências

- Hub: `AGENTS.md`, `docs/research/11-greenfield-brownfield-migracao.md`, `docs/research/12-adocao-integral-parcial.md`, `docs/research/07-roadmap-adocao.md`, `docs/governance/SATELLITE-ADOPTION-CHECKLIST.md`, `docs/governance/satellite-manifest.template.json`, `docs/governance/hub-policy-manifest.json`.
- Local: `.specify/memory/constitution.md` v2.0.0, `AGENTS.md`, `VERIFICATION_PROTOCOL.md`.
