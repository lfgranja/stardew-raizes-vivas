# Quickstart — validação do ciclo 031

## Pré-requisitos

- Repositório no worktree do ciclo; `gh` autenticado; `GamePath` apontando para assemblies do jogo.

## Comandos

```bash
# 1. Âncora
pwd; git rev-parse --short HEAD; cat .specify/feature.json

# 2. Evidência remota (somente leitura)
gh run view 44641194770 --log-failed | grep '##\[error\]'
gh run view 44664287245 --log-failed | grep '##\[error\]'
gh run view 44664266743 --log-failed | grep '##\[error\]'

# 3. Gates do alvo
bash scripts/ci/aggregate-gate.sh
GamePath="$HOME/.local/share/Steam/steamapps/common/Stardew Valley" scripts/release-gate.sh
PYTHONPATH=scripts/quality-gate python3 -m pytest scripts/quality-gate/tests -q
python3 scripts/ci/i18n-parity-check.py

# 4. Escopo zero-code
git diff --name-only dev...HEAD
```

## Resultados esperados

- Três trechos `##[error]BUILDDEPS_TOKEN is not set...` com os run IDs correspondentes.
- `aggregate-gate.sh` e `release-gate.sh`: `ALL STEPS PASSED`.
- pytest: sem novas falhas (baseline registrado no STATE.md).
- Diff contém apenas documentação/SDD; nenhum caminho `LivingRoots/`, `.github/workflows/` ou `scripts/ci/`.

## Limite de honestidade

`machine-pass`/gate verde não produz atestação humana E de Gate 3, sessão D1, SC-004, tag/release nem aprovação de revisor distinto.
