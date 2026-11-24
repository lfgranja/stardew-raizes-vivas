# Test Roadmap — pós-promução #821 (reconciliação documental)

**Slug**: `pos-promocao-821` · **Date**: 2026-11-24 · **Type**: documentação/governança (subproduto de escalonamento, não incremento de ciclo 80/20).

## Scope and IDs

- `ESC-LR-20261122-008`: registrar fechamento por merge ao vivo de #821.
- `GOV-ADMIN-BYPASS`: registrar nova decisão owner sobre `enforce_admins:false` em `main`.
- Artefatos: `.specify/memory/STATE.md`, `docs/PROGRESSO.md`, `ROADMAP.md`, `OBJETIVO.md`, `specs/024-pilha-escalonamentos-nivel5/registry/escalations.yaml`.

## Preconditions

- Worktree limpa criada de `origin/dev` (`c96eef5`); nenhum código/workflow/script/cenário alterado.
- `gh` autenticado para leitura de PR #821 e proteção de branches.

## Steps

1. **Ação**: `gh pr view 821 --json state,mergedAt,mergeCommit` · **Esperado**: `MERGED`, `2026-11-24T13:31:22Z`, `8a0407e…` · **Superfície**: CLI · **Automatizável**: sim.
2. **Ação**: `gh api repos/lfgranja/stardew-raizes-vivas/branches/main/protection` · **Esperado**: `reviews=1`, `strict=true`, `sdd-gate`, `signatures=true`, `enforce_admins=false` · **Superfície**: CLI/HTTP · **Automatizável**: sim.
3. **Ação**: `python3 -c "import yaml; yaml.safe_load(open('specs/024-.../escalations.yaml'))"` · **Esperado**: YAML válido, item 008 `status=fechado`, item `GOV-ADMIN-BYPASS` presente · **Superfície**: CLI · **Automatizável**: sim.
4. **Ação**: `python3 scripts/ci/sdd-gate.py` · **Esperado**: `PASS (0 errors, … warnings)` · **Superfície**: CLI · **Automatizável**: sim.
5. **Ação**: `bash scripts/tests/test-spec-consistency.sh` · **Esperado**: `PASS`, `C-SCN-36: OK` · **Superfície**: CLI · **Automatizável**: sim.
6. **Ação**: `./scripts/check-protected-paths.sh` · **Esperado**: `all checks passed` · **Superfície**: CLI · **Automatizável**: sim.
7. **Ação**: `git diff --check` e Revisão de Código agêntica independente no PR · **Esperado**: sem whitespace errors; blocker corrigido antes do merge · **Superfície**: CLI/PR · **Automatizável**: parcial (revisão por agente).

## Captures

Sem UI de produto: saídas de terminal coladas em `evidence.md`; nenhuma captura de tela é possível ou necessária.

## Rollback

`git revert` do commit documental na worktree/PR; nenhum efeito em código, CI, proteção de branch ou produção.
