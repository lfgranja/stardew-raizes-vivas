# Quickstart: Roteiro de verificação funcional

## Pré-condições

```bash
cd /home/luis/development/stardew-raizes-vivas
export GamePath=/home/luis/stardew-assemblies
test -d "/home/luis/.local/share/Steam/steamapps/common/Stardew Valley"
command -v xvfb-run
```

## Camada A — gates determinísticos

```bash
bash scripts/ci/aggregate-gate.sh
GamePath=/home/luis/stardew-assemblies bash scripts/release-gate.sh
python3 scripts/ci/i18n-parity-check.py
PYTHONPATH=scripts/quality-gate python3 -m pytest scripts/quality-gate/tests -q
```

Esperado: `ALL STEPS PASSED` nos dois gates; i18n `0 errors`; pytest sem regressões.

## Camada B — matriz xUnit

```bash
dotnet test Stardew-LivingRoots.sln -c Release --no-build --verbosity minimal \
  --filter "FullyQualifiedName~SoilHealthCategoryInvariantsTests"
```

Repetir para os 11 filtros listados em `verification-matrix.md`. Esperado: `Passed!` e 0 falhas em cada grupo.

## Camada C — Gate 3 do cenário alterado

```bash
./scripts/verify-scenario.sh \
  --scenario specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json \
  --no-acquire \
  --game-path "$HOME/.local/share/Steam/steamapps/common/Stardew Valley" \
  --skip-build
./scripts/verify-scenario.sh --verify-bundle <dir-do-bundle>
```

Esperado: `verdict=pass`, honestidade `R`, 3 sinais presentes e concordantes, exit 0.

## Deriva de banda

```bash
grep -n 'max\|Poor interval' \
  specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json
```

Esperado: `"max": 31` e "Poor interval [0, 31)".

## Honestidade

Nenhum comando acima produz atestação humana. D1, atestação E de Gate 3 e SC-004 permanecem `[NV]`.
