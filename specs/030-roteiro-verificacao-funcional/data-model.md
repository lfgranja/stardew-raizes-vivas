# Data Model: Roteiro de verificação funcional

## VerificationGroup (B1–B11)

| Campo | Tipo | Regra |
|---|---|---|
| `id` | string | `B1`..`B11`, único |
| `feature` | string | Nome da funcionalidade coberta |
| `filter` | string | Expressão `FullyQualifiedName~` exata, agrupada com `|` |
| `expected` | string | Resultado esperado (0 falhas + comportamento observado) |
| `observed` | string | Contagem real colada na execução |
| `evidenceLimit` | string | `machine-pass`, `host-only` ou `[NV]` humano |

Relacionamento: cada grupo aponta para zero ou mais `Scenario` e para nenhum `HumanOnlyItem`.

## Scenario

| Campo | Tipo | Regra |
|---|---|---|
| `path` | string | Caminho versionado em `specs/*/scenarios/` |
| `thresholds` | array | Cada item tem `signal`, `predicate`, `description` |
| `expectedSignals` | array | `soilHealthStateDelta`, `cropPhaseDelta`, `smapiLog` |
| `bundleRunId` | string | Preenchido após reexecução; ausente antes |
| `verdict` | enum | `pass` \| `partial` \| `fail`; `partial` nunca promovido |

Transição de estado: `editado` → `executado` → `{pass, partial, fail}` → `bundle verificado`.

## StateNote

| Campo | Tipo | Regra |
|---|---|---|
| `date` | ISO-8601 | Data da nota datada |
| `head` | string | `git rev-parse --short HEAD` no momento |
| `anchor` | string | Conteúdo de `.specify/feature.json` |
| `counts` | map | Contagens obtidas por `find`/`grep`, nunca por prosa |
| `nvItems` | array | Itens `[NV]` com causa |

Invariante: notas são append-only; nenhuma nota reescreve histórico anterior.
