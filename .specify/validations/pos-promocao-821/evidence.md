# Evidence — pós-promução #821 (2026-11-24)

Worktree: `/home/luis/development/stardew-raizes-vivas/.specify/queue/worktrees/pos-promocao-821`, base `origin/dev` @ `c96eef5`. Nenhum código/workflow/script/cenário alterado.

| Passo | Comando / ação | Saída observada | Resultado |
|---|---|---|---|
| 1 | `gh pr view 821 --json state,mergedAt,mergeCommit` | `state=MERGED`, `mergedAt=2026-11-24T13:31:22Z`, `mergeCommit=8a0407efdb4ae75b1abd88557020365690a28c53` | PASS `[EV]` |
| 2 | `gh api repos/lfgranja/stardew-raizes-vivas/branches/main/protection` | `reviews=1`, `strict=true`, `contexts=[sdd-gate]`, `signatures=true`, `enforce_admins=false` | PASS `[EV]` |
| 3 | `python3 -c "import yaml; …"` sobre `escalations.yaml` | `008 status=fechado fechamento=True`; `GOV status=escalado-swe refs=3` | PASS `[EV]` |
| 4 | `python3 scripts/ci/sdd-gate.py` | `sdd-gate: PASS (0 errors, 40 warnings)` | PASS `[EV]` |
| 5 | `bash scripts/tests/test-spec-consistency.sh` | `spec-consistency: PASS (1683 checks)`; `C-SCN-36: OK` | PASS `[EV]` |
| 6 | `./scripts/check-protected-paths.sh` | `protected-path preflight: all checks passed.` | PASS `[EV]` |
| 7 | `git diff --check` | sem saída (exit 0) | PASS `[EV]` |
| ACR | Revisão independente em contexto limpo no PR | ver comentário do revisor no PR | registado no PR |

Sem capturas de tela: o ALVO é headless (CLI + rollup `gh`), sem UI de produto — dispensa justificada conforme 0.3.

`[NV]`: efeito de produção/deploy, tag `v0.2.0`, D1, atestação E, SC-004, `RUNNER_AUDIT_STORE` e decisão sobre `enforce_admins`. Nenhuma credencial foi copiada.

Rollback: `git revert` do commit documental; nenhum efeito em código, CI ou proteção de branch.
