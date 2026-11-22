# Quickstart — validação do ciclo 032

## Pré-requisitos

Runner headless com Xvfb; `GamePath` apontando para os assemblies do jogo; `gh` para PR.

## Comandos

```bash
# 1. Diferença cirúrgica
git diff -- specs/004-playable-compost-loop/scenarios/compost-growth-healthy.scenario.json \
  specs/005-soil-health-to-crop-growth/scenarios/soil-health-growth.scenario.json

# 2. Gate 3 (dois cenários)
./scripts/verify-scenario.sh --scenario specs/004-playable-compost-loop/scenarios/compost-growth-healthy.scenario.json \
  --no-acquire --game-path "$HOME/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build
./scripts/verify-scenario.sh --scenario specs/005-soil-health-to-crop-growth/scenarios/soil-health-growth.scenario.json \
  --no-acquire --game-path "$HOME/.local/share/Steam/steamapps/common/Stardew Valley" --skip-build

# 3. Gates determinísticos
bash scripts/ci/aggregate-gate.sh
GamePath="$HOME/.local/share/Steam/steamapps/common/Stardew Valley" scripts/release-gate.sh
```

## Resultados esperados

- Diff: exatamente 4 linhas (`- "min": 67` / `+ "min": 71` e descrição, em cada arquivo).
- 2 runs `pass`, honestidade R, 3 sinais, `--verify-bundle` exit 0; blocos promovidos pelo runner.
- `ALL STEPS PASSED` nos dois gates.

## Limite

`machine-pass` host-only ≠ `verified`; D1/E/SC-004/tag/aprovador permanecem `[NV]`.
