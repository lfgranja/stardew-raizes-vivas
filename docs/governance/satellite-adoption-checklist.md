# Checklist de Adoção — Satélite MDMV (hub-only)

Fonte: `/home/luis/development/MDMV/projetos/mdmv-dark-factory/docs/governance/SATELLITE-ADOPTION-CHECKLIST.md`
Hub avaliado: `044f153ef7eee8b6b3299b37fbfdb08075a2c480` (2026-11-10)
Satélite: `stardew-raizes-vivas` (github.com/lfgranja, branch default `dev`)
Status geral: **parcial** — ver `docs/adr/002-adocao-mdmv-dark-factory.md`.

## Identidade
- [x] `target_repo` (`stardew-raizes-vivas`), host (`github`), owner (`lfgranja`), branch default (`dev`).
- [x] `hub_sha` do mdmv-dark-factory avaliado (ADR 002 + `satellite-manifest.json`).

## Políticas (022/026)
- [x] Manifesto `satellite-manifest.json` (`docs/governance/satellite-manifest.json`, schema 026.1.0).
- [x] `adopted_policies` inclui mínimas exigidas pelo hub manifest — 022, 023, 024 adotadas; 025 pendente (Fase 4); 026 concluída (Fase 3); 031 concluído (Fase 1).
- [x] Overrides marcados `local-only`/`proposta-pendente` com racional (022): host é GitHub, não Forgejo (manifest `notes`).

## Gates determinísticos (G2/G4)
- [x] Workflow CI com gates determinísticos do stack (`quality-gate.yml`, `protected-paths.yml`, `code-style.yml`, `verify-scenario.yml`, `build_and_test.yml`).
- [x] Required status checks + branch protection configurados no GitHub — `sdd-gate` (contexto `sdd-gate`) agora é required em `dev`/`main` (strict=true), configurado via `gh` CLI em 2026-11-10 (PR #780). Branch protection pré-existente (PR obrigatório, enforce_admins).
- [ ] Runner self-hosted seguro — N/A: usamos runners GitHub-hosted; reavaliar se migrarmos para self-hosted.

## Fase 4 — Narrow lights-out (opcional)
- [x] Classe de job inicial definida: paridade i18n (default/en/pt) — `scripts/ci/i18n-parity-check.py` + workflow `i18n-parity.yml`.
- [x] Gates machine-checkable: i18n-parity, build, test, format, sdd-gate (documentados em `docs/governance/narrow-lights-out.md`).
- [x] Exemplo executado: 4 chaves `mulch.*` adicionadas ao `pt.json` (paridade restaurada, 27/27/27).
- [x] Sem auto-merge cego: PR obrigatório + humano aprova merge (políticas 022/025/042).
- [ ] Classes futuras (bump deps patch, lint fix, novas chaves i18n) — não habilitadas.

## Telemetria (G3/G5/035/039)
- [x] Emite JSONL 035.1.0 (`scripts/telemetry/emit_telemetry.py`) — Fase 2 concluída (`docs/telemetry/execution-record.jsonl`, 9 eventos seed).
- [x] Valida com `validate_telemetry.py` (sem segredos; custos rotulados NV billing) — Fase 2 concluída; validação PASS (0 errors, 0 warnings, strict ok).
- [x] Schema local (`docs/governance/execution-record.schema.json`, baseado em 035) publicado.
- [ ] Publica retorno para ingestão do hub — pendente (hub ainda sem ingestão real, NV); padrão compatível (schema_version 035.1.0).

## Segurança (036/018)
- [x] Sem segredos em prompts/logs/PR/artifacts (redação em `verify-scenario.sh`, quality gate).
- [x] Least privilege, allowlists, pins de supply chain (`check-protected-paths.sh`, semgrep, jscpd, lefthook).
- [x] Sem auto-merge cego sem CI+review+protections verificados (022/025) — nenhum auto-merge configurado.

## Drift (G4/026)
- [x] Avaliação periódica contra `hub-policy-manifest.json` — Fase 3 concluída: `scripts/ci/check_policy_drift.py` + `.github/workflows/policy-drift.yml` (cron semanal + dispatch); drift check PASS (ALIGNED).
- [x] Desalinhamento → SE decision request (conformidade ou ADR local) — Fase 3: registro de escalonamentos em `specs/024-pilha-escalonamentos-nivel5/registry/escalations.yaml` (7 itens).

## Evidência por feature (R4/R7) — gap principal
- [x] `.specify/validations/<slug>/validate.md` aprovado por feature — retroativo concluído para 17 specs (R4) (Fase 1).
- [x] `.specify/releases/<slug>/release.md` por feature — retroativo concluído para 17 specs (R7) (Fase 1).
- [x] Gate determinístico da cadeia SDD no CI (`scripts/ci/sdd-gate.py` + `.github/workflows/sdd-gate.yml`) — Fase 1. Required check configurado (PR #780).

## Notas
- `~` = parcialmente atendido.
- Escalation registry (024) e execution record (035) — Fase 2 concluída para telemetria; escalation registry é Fase 3.
- Spec Kit local é 1.3.2 (opencode); hub é 1.3.2 (OMP). Upgrade de 1.2.5 -> 1.3.2 executado (ESC-006 resolvido). Integração mantida em opencode (migração para OMP é decisão separada).
- Telemetria: `docs/telemetry/execution-record.jsonl` (9 eventos seed); validador adaptado de `validate_telemetry.py` do hub (stdlib, no network).
- Débito técnico: jscpd baseline re-baselined (10 clones, 148 linhas, 0.83%) — refactor owed (ESC-004). MP live verification documentada (docs/mp-live-verification.md) — owed (ESC-005).
