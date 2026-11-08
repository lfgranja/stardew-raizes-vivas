# 018 — Inventário final honesto (T038, ciclo 80/20)

- **Data:** 2026-11-07
- **Escopo:** features com código executável/runnable paths cobertos pelo plano 018 (004, 005, 013, 014, 017) + limites explícitos.
- **Regra:** `machine-pass` = runner/harness com sinais declarados concordantes, honesty R, promoção conforme 010/016. **machine-pass ≠ human-verified.** D1 transport = bloqueado/pendente. Farmhand/split-screen guard = **parcial/bloqueado-factível, nunca machine-pass** (T002 / GOVERNANCE-ANNEX). SC-004, legibilidade/render, balance (`P=10` provisional) = humano/qualitativo/playtest. **Sem promoção acima da evidência. Sem runs fabricados.** Bundles gitignored; run IDs são referências.

## Evidência verificada neste ciclo (2026-11-07, pass 2)

- `dotnet build LivingRoots/LivingRoots.csproj -c Release` (GamePath assemblies): **0 erros, 1 aviso** pré-existente.
- `dotnet format Stardew-LivingRoots.sln --verify-no-changes` (GamePath assemblies): **exit 0**.
- `dotnet test LivingRoots.Tests.csproj -c Release --filter "AgrochemicalApplicationServiceTests|MulchApplicationServiceTests|MulchVerificationCommandsTests|SoilHealthServiceTests|CompostApplicationServiceTests"`: **115 passed, 0 failed, exit 0** (avisos MSB3277 Mono.Cecil pré-existentes no ambiente de teste).
- `bash scripts/tests/test-017-agrochemical.sh`: **23/23 PASS**.
- `bash scripts/tests/test-017-claims.sh`: **PASS** (017 claim-records 12 linhas, 19 paths checados, 2 bundles digest-checked).

## Inventário feature × path (conservador)

| Feature | Path / cenário | Classe | Evidência (artefato) | Notas / blind spots |
|---|---|---|---|---|
| 004 playable-compost-loop | Host compost growth — poor 10 / 1.15× (hardening) | host-only machine-pass | `specs/004-playable-compost-loop/test.md` GATE3 `run-20261105T053313Z-535d`; claim cita poor primary | Runner-promoted; bundle gitignored. |
| 004 | Host compost growth — moderate 50 / 1.0× parity | host-only machine-pass (citado) | `specs/004-playable-compost-loop/test.md` claim cita `run-20261107T120435Z-1ea9`; cenário `scenarios/compost-growth-moderate.scenario.json` | Bundle/`--verify-bundle` não rechecado neste ciclo. |
| 004 | Host compost growth — healthy 80 / 0.85× | host-only machine-pass (citado) | `specs/004-playable-compost-loop/test.md` claim cita `run-20261107T120602Z-69ea`; cenário `scenarios/compost-growth-healthy.scenario.json` | Idem. |
| 004 | Farmhand guard (split-screen) | **blocked-feasible/partial, nunca machine-pass** | T002 evidence (`specs/018-.../verification/t002-farmhand-host-guard-attempt.md`) | `isMainPlayer` flip não demonstrado headless; D1 transport pendente. |
| 005 soil-health-to-crop-growth | Host healthy 80 / 0.85× | host-only machine-pass | `specs/005-soil-health-to-crop-growth/test.md` `run-20261107T112602Z-5b08` | Runner-promoted; 3 sinais citados. |
| 005 | Host poor 10 / 1.15× | host-only machine-pass (citado) | `specs/005-soil-health-to-crop-growth/test.md` claim cita `run-20261107T114448Z-4b61`; `scenarios/soil-health-growth-poor.scenario.json` | Bundle não rechecado neste ciclo. |
| 005 | Host moderate 50 / 1.0× parity | host-only machine-pass (citado) | `specs/005-soil-health-to-crop-growth/test.md` claim cita `run-20261107T114623Z-3365`; `scenarios/soil-health-growth-moderate.scenario.json` | Idem. |
| 005 | Farmhand guard | **blocked-feasible/partial** | T002 | D1 pendente. |
| 013 mulch-soil-cover | Host mulch decay/exemption (aplicável ao cenário promovido) | host-only machine-pass | `specs/013-mulch-soil-cover/test.md` GATE3 `run-20261107T205338Z-08f8` (`commit 7d53a39`, honesty R) | Claim genérica “automated headless run + multi-signal evidence”; cenários adicionais existem (`mulch-decay-exemption`, `mulch-7day-expiry`, `mulch-bare-tile-decay-exemption`, `mulch-refused-on-ineligible`) — cobertura por cenário requer auditoria de runs/bundles. |
| 013 | Host mulch refused-on-ineligible | cenário existe; classe **não auditada** neste ciclo | `specs/013-mulch-soil-cover/scenarios/mulch-refused-on-ineligible.scenario.json` | Não promover sem evidência de run. |
| 013 | Host mulch 7-day expiry / bare-tile SC-004 (se aplicável) | cenários existem; classe **parcial/não auditada** | cenários em `specs/013-mulch-soil-cover/scenarios/` | Verificar claims/test.md/records. |
| 013 | Farmhand guard | **blocked-feasible/partial** | T002 | Nunca machine-pass. |
| 014 soil-health-crop-quality | Host harvest matrix poor/moderate/healthy | host-only machine-pass | `specs/014-soil-health-crop-quality/test.md` `run-20261106T014750Z-14b6`; regressão `122915Z-2564` citada | Runner-promoted. |
| 014 | Host unknown/NaN baseline → vanilla/no forced quality | host-only machine-pass (citado) | `specs/014-soil-health-crop-quality/test.md` claim cita `run-20261107T123141Z-3531` (rerun clean); `scenarios/soil-health-harvest-unknown.scenario.json` | Primeira tentativa `122437Z-2f19` parcial/no-dump — honesto no claim. |
| 014 | Host measured-zero → poor/force quality 0 | host-only machine-pass (citado) | `specs/014-soil-health-crop-quality/test.md` claim cita `run-20261107T123257Z-08c9`; `scenarios/soil-health-harvest-measured-zero.scenario.json` | Bundle não rechecado neste ciclo. |
| 014 | Farmhand guard | **blocked-feasible/partial** | T002 | D1 multiplayer pendente onde aplicável. |
| 017 joja-agrochemical-tradeoff | Host measured tradeoff (principal) | host-only machine-pass | `specs/017-joja-agrochemical-tradeoff/test.md` `run-20261107T193756Z-6d97` (commit `090364c`) | Runner-promoted. |
| 017 | Host refused-contention (seam autorizado) | host-only machine-pass (append-only note) | `specs/017-joja-agrochemical-tradeoff/test.md` append-only note; cenário `agrochemical-refused-contention.scenario.json` | Seam determinístico dev/CI, não contenção produção natural. |
| 017 | Host unknown/clamped/refused-at-minimum | cenários existem; runs individuais **não auditados** integralmente neste ciclo | `scenarios/agrochemical-unknown-baseline/clamped/refused-at-minimum.scenario.json`; 017 claim-records válidos (12 linhas) | Verificar claim rows/run records por path. |
| 017 | Farmhand guard | **blocked-feasible/partial** | T002 | Nunca machine-pass. |
| 015 harmony-harvest-seam | Host 3-signal seam | host-only machine-pass (citado em coverage anterior) | `specs/018-.../verification/coverage-status-2026-11-07.md` cita `210830Z-67f1` | **Não revalidado** neste ciclo; D1 SC-004 pendente. |
| Todas | D1 farmhand transport (dois clientes reais) | **blocked/pending** | N/A | Requer sessão real; fora do runner. |
| Todas | SC-004 compreensão (≥90%) | **pending/qualitative** | N/A | Protocolo humano. |
| Todas | Legibilidade render/a11y/font/ícone | **manual/felt** | N/A | Apenas humano. |
| Todas | Balance `P=10` / tentação | **assumed/playtest** | N/A | Sem instrumento. |
| Todas | Cross-machine determinism | **assumed/same-machine only** | N/A | Não testado cross-machine. |

## Avaliação honesta (SC-MP / T038)

- **Alcançado (forte):** base host machine-pass para 004/005/013/014/017 com cenários e comandos presentes; 015 citado como machine-pass host no status anterior; checks determinísticos 017 passam; build/format/115 testes unitários direcionados passam neste ciclo.
- **Não totalmente fechado:** Wave 5 completo (T034 mutation checks por nova asserção, T035 determinismo 2x/seed por cenário, T036 smoke suite completo, T037 `--verify-bundle` por bundle, T030 claim rows completos por feature além de 017, T032 suite 010 completa). Cobertura 004 moderate/healthy e alguns paths 013/017 requer **auditoria de bundles/claim rows** (não fabricada).
- **Bloqueado por design/ambiente:** farmhand/split-screen `machine-pass` impossível pelo contrato; D1 transport pendente; humano pendente para SC-004, render, balance.
- **Conclusão:** **“100% applicable paths com evidência completa Wave 5” não atestado.** O estado é **substancialmente avançado e honesto**, com gaps residuais de auditoria/attestation e bloqueios conhecidos. Próxima ação 80/20: auditoria focada + 1 item Wave 5 de maior valor (smoke suite com timeout adequado ou `--verify-bundle` em bundles locais disponíveis) sem promover claims.
