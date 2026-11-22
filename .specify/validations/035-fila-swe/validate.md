# Validação R4 — Spec 035 (Promoção dev→main na fila SWE)

- **Slug**: 035-fila-swe
- **Date**: 2026-11-22
- **Verdict**: **PENDENTE — `release-gate.sh` em execução**; aprovar somente com saída real

## Gate results

| Gate | Evidência | Resultado |
|---|---|---|
| R1 | assessment `swe-escalation-queue` — go | PASS |
| Analyze | 5 FR, 0 ambiguidades, 0 achados críticos | PASS |
| SC-001 | `escalations.yaml` → 8 itens; último `ESC-LR-20261122-008` / `escalado-swe` (YAML parse OK) | PASS |
| SC-002 | 0 merges em `main`, 0 tags, 0 aprovações simuladas | PASS |
| SC-003 | aggregate/pytest/i18n — ver registro final | PENDENTE |
| SC-004 | diff sem código/workflows/scripts/cenários/contratos | PASS |
| Constituição | v2.0.0; 0 violações | PASS |

## Honestidade

- CI do PR #821 é verde por observação ao vivo (`gh pr checks --watch` RC=0); `REVIEW_REQUIRED` permanece.
- Merge em `main`, tag `v*.*.*` e release pública são owner/SWE-gated e não executados.
- D1, atestação E de Gate 3, SC-004, `RUNNER_AUDIT_STORE` permanecem `[NV]`.

## Rollback

Reverter os commits do PR restaura a fila e as âncoras.
