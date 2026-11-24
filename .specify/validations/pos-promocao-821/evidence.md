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

## Correção pós-merge (2026-11-24T14:05Z)

| Passo | Comando | Saída observada | Resultado |
|---|---|---|---|
| 8 | `find .specify/validations -mindepth 1 -maxdepth 1 -type d \| wc -l` no head `936ae31` | `33` (o diretório `pos-promocao-821/` entrou no merge de #826) | PASS `[EV]` |
| 9 | recount completo no head `936ae31` | `specs=34`, `assessments=29`, `validations=33`, `releases=32`, `bugs=40`, `docs/research=5` | PASS `[EV]` |
| 10 | `python3 scripts/ci/sdd-gate.py` + `bash scripts/tests/test-spec-consistency.sh` + `./scripts/check-protected-paths.sh` no head mesclado `936ae31` | `PASS (0 errors, 41 warnings)`; `PASS (1683 checks)`, `C-SCN-36: OK`; `all checks passed` | PASS `[EV]` |
| 11 | `python3 -c "import yaml; ..."` + `git diff --check` na correção | `008 status=fechado`, `controle_atual_pos_merge` presente, GOV item presente; diff limpo | PASS `[EV]` |

`PROGRESSO.md` passou a registrar `validations/=33` e o registry recebeu `controle_atual_pos_merge`, preservando o campo histórico `controle_atual` que descrevia #821 como aberto antes da autorização.
