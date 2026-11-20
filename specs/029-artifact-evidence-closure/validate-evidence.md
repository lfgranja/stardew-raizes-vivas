# Evidência de validação — ciclo 029

- **Data**: 2026-11-20
- **Âncora**: `029-artifact-evidence-closure` @ `specs/029-artifact-evidence-closure`

## SC-001 — candidatos archify (baseline 3/5 → 5/5)

| Candidato | Tipo | Resultado final |
|---|---|---|
| `architecture-living-roots-20261103-075250` | architecture | `ok:true` |
| `lifecycle-composting-bin-20261103-094351` | lifecycle | `ok:true` |
| `workflow-harvest-quality-sync-20261106-015200` | workflow | `ok:true` |
| `workflow-heirloom-pipeline-20261109-191500` | workflow | `ok:true` |
| `workflow-verification-pipeline-20261105-015049` | workflow | `ok:true` |

Comando: `node ~/.agents/skills/archify/bin/archify.mjs validate <tipo> .archify/<dir>/candidate.json --repo-root . --json`.

Correções aplicadas em `workflow-heirloom-pipeline`: fases `harvest` 4..4 e `sync` 5..5 (disjuntas e dentro do limite de coluna 5); aresta `e-postfix-refusal` com `route:auto`; nó `refusal` movido para col 4; aresta `e-host-seed` adicionada para cobrir o `mainPath`.

Correções aplicadas em `workflow-verification-pipeline`: conversão de `layout/components/connections` + `diagram_type:architecture` para `lanes/nodes/edges` + `diagram_type:workflow`, preservando os 12 nós e as 11 conexões originais; `mainPath` reordenado para avanço monotônico de coluna; arestas `inject→verdict` e `verdict→quit` adicionadas.

**Desvio registrado**: o guia `quickstart.md` previa até 3 iterações de reparo; foram necessárias 5, cada uma corrigindo um defeito distinto e determinístico revelado pelo validador (fases sobrepostas, colisão de nós, corredor ambíguo, geometry explícita inválida, `mainPath` sem aresta). Nenhum erro foi suprimido; o resultado final é `ok:true` nos cinco candidatos.

## SC-002 — âncoras documentais

`docs/PROGRESSO.md` e `docs/research/index.md` criados com nota datada de 2026-11-20, contagens por script e `[NV]` em afirmações remotas. `OBJETIVO.md` §10 e `ROADMAP.md` "Verified state note" acrescentados sem apagar histórico.

## SC-003/SC-004 — escopo e honestidade

`git diff --name-only origin/dev | grep '^LivingRoots/'` → **ZERO PRODUCTION DIFF**. Nenhum `verified` humano, D1 ou SC-004 promovido; itens human-gated permanecem pendentes e `[NV]`.

## SC-005 — gate autoritativo

Registrado em `.specify/memory/STATE.md` e na seção final deste ciclo.
