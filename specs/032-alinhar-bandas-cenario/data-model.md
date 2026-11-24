# Data Model: 032-alinhar-bandas-cenario

## ScenarioBandPredicate

| Field | Rule |
|-------|------|
| `min` | `71` nos dois cenários ativos; inteiro |
| `max` | `100` (inalterado) |
| `exclusiveMin` | `false` (inalterado) |
| `exclusiveMax` | `true` (inalterado) |
| `tolerance` | `0.0001` (inalterado) |
| `description` | `stored health stays in the Healthy interval [71, 100)` |

**Validation**: diff total = 4 linhas; nenhum outro campo muda.

## Gate3Evidence

| Field | Rule |
|-------|------|
| `runId` | gerado pelo runner; obrigatório |
| `verdict` | `pass` para promoção; `partial`/W registrado sem promoção |
| `honestyLevel` | `R` |
| `signals` | 3 (`soilHealthStateDelta`, `cropPhaseDelta`, `smapiLog`) concordantes |
| `bundle` | verificado com `--verify-bundle` exit 0 |
| `block` | promovido pelo runner em `test.md`, nunca editado à mão |

**Transitions**: `pending → pass(R,3 sinais) → machine-pass`; `pending → partial/W → registrado, sem promoção`.
