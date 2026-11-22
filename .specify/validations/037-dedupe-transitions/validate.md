# Validação R4 — Spec 037 (Dedupe de transições)

- **Slug**: 037-dedupe-transitions
- **Date**: 2026-11-22
- **Verdict**: **PENDENTE — smoke completo em execução**; aprovar somente com saída real

## Gate results

| Gate | Evidência | Resultado |
|---|---|---|
| R1 | assessment `promotion-dedupe` — go | PASS |
| Analyze | 5 FR, 0 ambiguidades, 0 achados | PASS |
| SC-001/002 | 275 transições, 0 duplicatas; `test-spec-consistency.sh` → `PASS (1683 checks)`, C-SCN-36 OK | PASS |
| SC-003 | aggregate/pytest/i18n — ver registro final | PENDENTE |
| SC-004 | diff: 1 linha em `promotion-triggers.json` + artefatos SDD; zero código/workflows/scripts/cenários | PASS |
| Constituição | v2.0.0; 0 violações | PASS |

## Honestidade

- O `release-gate.sh` do ciclo 033 terminou com **`[release-gate FAIL] step 5`** (RED real, `exit 15`), não interrupção: 51 arquivos, 49 passed, 1 failed (`test-spec-consistency.sh`). Causa: 3 transições duplicadas criadas por promoção manual redundante nesta sessão; corrigidas preservando o primeiro conjunto.
- Nenhum bloco `GATE3:BEGIN/END` editado à mão; nenhum código/workflow/contrato alterado.
- D1, atestação E, SC-004, tag/release e aprovador distinto permanecem `[NV]`.

## Rollback

Reverter o commit restaura as três duplicatas (e o RED).
