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
- [~] `adopted_policies` inclui mínimas exigidas pelo hub manifest — parcial: `022`, `023` adotadas; `024`, `025`, `026`, `031` em fases (ADR 002).
- [x] Overrides marcados `local-only`/`proposta-pendente` com racional (022): host é GitHub, não Forgejo (manifest `notes`).

## Gates determinísticos (G2/G4)
- [x] Workflow CI com gates determinísticos do stack (`quality-gate.yml`, `protected-paths.yml`, `code-style.yml`, `verify-scenario.yml`, `build_and_test.yml`).
- [ ] Required status checks + branch protection configurados no GitHub (NV — verificar empiricamente; hoje não há evidência de required checks em `dev`/`main`).
- [ ] Runner self-hosted seguro — N/A: usamos runners GitHub-hosted; reavaliar se migrarmos para self-hosted.

## Telemetria (G3/G5/035/039)
- [ ] Emite JSONL 035.1.0 (`emit_telemetry.py`/`wrap_telemetry.py`) — Fase 2 (ADR 002).
- [ ] Valida com `validate_telemetry.py` (sem segredos; custos rotulados NV billing) — Fase 2.
- [ ] Publica retorno para ingestão do hub (path/release artifact acordado) — Fase 2.

## Segurança (036/018)
- [x] Sem segredos em prompts/logs/PR/artifacts (redação em `verify-scenario.sh`, quality gate).
- [x] Least privilege, allowlists, pins de supply chain (`check-protected-paths.sh`, semgrep, jscpd, lefthook).
- [x] Sem auto-merge cego sem CI+review+protections verificados (022/025) — nenhum auto-merge configurado.

## Drift (G4/026)
- [ ] Avaliação periódica contra `hub-policy-manifest.json` — Fase 3 (ADR 002).
- [ ] Desalinhamento → SE decision request (conformidade ou ADR local) — Fase 3.

## Evidência por feature (R4/R7) — gap principal
- [~] `.specify/validations/<slug>/validate.md` aprovado por feature — hoje só `017` e `020` (de 17 specs).
- [~] `.specify/releases/<slug>/release.md` por feature — hoje só `017`.
- [ ] Gate determinístico da cadeia SDD no CI (`scripts/ci/sdd-gate.py`) — Fase 1.

## Notas
- `~` = parcialmente atendido.
- Escalation registry (024) e execution record (035) são Fase 2/3 (ADR 002).
- Spec Kit local é 1.2.5 (opencode); hub é 1.3.2 (OMP). Upgrade avaliado como parte da Fase 0 (ver ADR 002).
