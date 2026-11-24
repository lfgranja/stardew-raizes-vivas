# Quickstart: validação do ciclo 029

Pré-requisitos: estar na raiz do alvo `/home/luis/development/stardew-raizes-vivas`, no branch `029-artifact-evidence-closure`, com `~/.agents/skills/archify/bin/archify.mjs` disponível.

## 1. Verificar os cinco candidatos (SC-001)

```bash
node ~/.agents/skills/archify/bin/archify.mjs validate architecture .archify/architecture-living-roots-20261103-075250/candidate.json --repo-root . --json
node ~/.agents/skills/archify/bin/archify.mjs validate lifecycle .archify/lifecycle-composting-bin-20261103-094351/candidate.json --repo-root . --json
node ~/.agents/skills/archify/bin/archify.mjs validate workflow .archify/workflow-harvest-quality-sync-20261106-015200/candidate.json --repo-root . --json
node ~/.agents/skills/archify/bin/archify.mjs validate workflow .archify/workflow-heirloom-pipeline-20261109-191500/candidate.json --repo-root . --json
node ~/.agents/skills/archify/bin/archify.mjs validate workflow .archify/workflow-verification-pipeline-20261105-015049/candidate.json --repo-root . --json
```

Esperado: `"ok": true` nas cinco saídas; baseline era 3/5.

## 2. Verificar âncoras documentais (SC-002)

```bash
ls docs/PROGRESSO.md docs/research/index.md
grep -n "2026-11-20" OBJETIVO.md ROADMAP.md docs/PROGRESSO.md docs/research/index.md
grep -c '\[NV\]' docs/PROGRESSO.md docs/research/index.md
```

Esperado: dois arquivos presentes; notas datadas; pelo menos uma marcação `[NV]` por âncora.

## 3. Verificar escopo e gate (SC-003 a SC-005)

```bash
git diff --name-only origin/dev | grep '^LivingRoots/' || echo "ZERO PRODUCTION DIFF"
bash scripts/ci/aggregate-gate.sh
git status --short
```

Esperado: `ZERO PRODUCTION DIFF`; `ALL STEPS PASSED`; nenhuma atestação `verified` adicionada.

## Nota

A verificação dos candidatos pode exigir ajuste iterativo de fases/rotas/schema; o limite é 3 iterações de reparo. Se ainda falhar, registrar a dívida e parar — não mascarar o erro.

