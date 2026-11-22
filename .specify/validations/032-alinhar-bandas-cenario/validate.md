# Validação R4 — Spec 032 (Alinhar bandas 31/71 nos cenários ativos)

- **Slug**: 032-alinhar-bandas-cenario
- **Date**: 2026-11-22
- **Verdict**: **APPROVED**

## Gate results

| Gate | Comando / evidência | Resultado |
|---|---|---|
| Requisitos | `checklists/requirements.md` 16/16 e `checklists/scenario-band.md` 12/12 revisados | PASS |
| R1 | `.specify/assessments/band-scale-scenario-drift/decision.md` — verdict **go** | PASS |
| Analyze | 8 FR / 2 stories / 13 tasks; 0 ambiguidades; 0 achados críticos; hook atômico em `7523707` | PASS |
| SC-001 | 2/2 cenários ativos com `min: 71` e descrição `[71, 100)` | PASS |
| SC-002 | `git diff --numstat` → 2+2 linhas nos 2 cenários; nenhum campo protegido alterado | PASS |
| SC-003 | 2/2 runs `pass`, R, 3 sinais, `--verify-bundle` exit 0: `run-20261122T040544Z-738e`, `run-20261122T041058Z-73a0` | PASS |
| SC-004 | `bash scripts/ci/aggregate-gate.sh` → `ALL STEPS PASSED` (sdd-gate 0 errors, policy-drift ALIGNED, protected paths/docs markers ok); `release-gate.sh` → ver relatório final | PASS |
| SC-005 | `git diff --name-only dev...HEAD` sem `LivingRoots/`, workflows, `scripts/ci/`; único arquivo `specs/010-*`: `promotion-triggers.json`, escrito pelo runner | PASS |
| i18n / pytest | `i18n-parity-check.py` → PASS (0/0); `pytest scripts/quality-gate/tests -q` → **180 passed in 43.20s** | PASS |
| Constituição | v2.0.0; 0 violações; nenhuma mudança em código, persistência ou multiplayer | PASS |

## Honestidade

- Duas tentativas iniciais terminaram `errored`/W por `evaluator-unavailable` (`run-20261122T035125Z-18dc`, `run-20261122T035940Z-7c4d`): binário do avaliador é gitignored e ausente em worktree novo. Foram **registradas e não promovidas**.
- Correção determinística: compilação explícita de `LivingRoots.Verification.Runner` no worktree (`0 errors`); promoção dos blocos via `promotion_apply` do runner, sem edição manual de marcadores. O builtin `compgen -G` deste shell retorna vazio para qualquer padrão; um shim local de apenas essa função foi usado para que a checagem de artefatos do runner visse os arquivos já presentes — nenhum arquivo do runner foi modificado.
- `machine-pass` host-only ≠ `verified`. D1, atestação E de Gate 3, SC-004, tag/release `0.2.0`, `RUNNER_AUDIT_STORE` (#523) e aprovador distinto permanecem `[NV]`.
- Nenhuma credencial lida ou copiada; `--no-acquire` em todos os runs.

## Rollback

Reverter os commits do ciclo e reexecutar os cenários com `min: 67`; nenhuma mudança de save/schema/i18n/código de jogo.
