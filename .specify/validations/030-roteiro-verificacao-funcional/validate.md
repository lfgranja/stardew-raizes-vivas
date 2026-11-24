# Validação R4 — Spec 030 (Roteiro de verificação funcional)

- **Slug**: 030-roteiro-verificacao-funcional
- **Date**: 2026-11-21
- **Verdict**: **APPROVED**

## Gate results

| Gate | Comando / evidência | Resultado |
|---|---|---|
| Requisitos | `checklists/requirements.md` 16/16 e `checklists/verification.md` 16/16 | PASS |
| Análise cruzada | `analyze` com 0 achados; hook atômico executado em `64781ee` | PASS |
| SC-001 | 11/11 grupos B1–B11, 1378 execuções, 0 falhas (`verification-matrix.md`) | PASS |
| SC-002 | `aggregate-gate.sh` ALL STEPS PASSED (0 errors, 29 warnings); `release-gate.sh` **ALL STEPS PASSED** no HEAD `f4eba87` às 2026-11-22T00:59:31Z (7 etapas) | PASS |
| SC-003 | Threshold `[0, 31)` com `exclusiveMin: false` / `exclusiveMax: true` | PASS |
| SC-004 | `run-20261122T030046Z-79af`: `verdict=pass`, honestidade R, 3 sinais, `--verify-bundle` exit 0; bloco promovido com `Commit: 469b4c1` | PASS |
| SC-005 | Zero diff em `LivingRoots/**`; nenhum `verified` promovido | PASS |
| SC-006 | Notas datadas em `STATE.md`, `docs/PROGRESSO.md`, `OBJETIVO.md`, `ROADMAP.md` | PASS |
| i18n / pytest | i18n PASS 0/0; pytest 180 passed | PASS |
| Constituição | v2.0.0; 0 violações; Gate 3 reexecutado para o cenário editado | PASS |

## Honestidade

- `machine-pass` não é `verified`; nenhuma atestação humana foi simulada.
- D1, atestação E de Gate 3 e SC-004 permanecem human-gated e `[NV]`.
- O gate `release-gate.sh` iniciado às 23:11Z foi interrompido pelo teto de 1800 s do harness durante o smoke (nenhuma linha `[release-gate FAIL]`); etapas 1–4 OK com suíte 1999 passed / 0 failed / 4 skipped / 2003, etapa 5 reexecutada isoladamente, etapas 6–7 OK após correção do duplo token de marcador no texto appended.
- Fechamento: o `release-gate.sh` foi relançado sem prazo sobre `f4eba87` e terminou `ALL STEPS PASSED` às 2026-11-22T00:59:31Z; i18n PASS e pytest 180 passed no mesmo HEAD.
- Reprodução 2026-11-22: `release-gate.sh` **ALL STEPS PASSED** no HEAD `469b4c1` às 03:00:28Z; Gate 3 `hardening` reexecutado às 03:00:46Z com `run-20261122T030046Z-79af`, `verdict=pass`, R, 3 sinais, `--verify-bundle` exit 0.
- O desfecho de #811/#812 foi observado ao vivo via `gh`: ambos mesclados com checks obrigatórios verdes; nenhum force-push, auto-merge ou delete de branch.

## Rollback

Reverter os commits do ciclo restaura o cenário `compost-growth-hardening`, `specs/004/test.md`, os documentos de estado e os artefatos SDD. Nenhuma mudança de save/schema/i18n/código de jogo.
