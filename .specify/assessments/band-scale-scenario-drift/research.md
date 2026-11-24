# Idea Research: Alinhar bandas 31/71 nos cenários ativos 004 e 005

- **Slug**: band-scale-scenario-drift
- **Created**: 2026-11-22T03:49:24Z
- **Evidence confidence (overall)**: high

## Users & Demand

- Revisores e agentes de verificação leem os cenários como contrato executável da banda Healthy; hoje o contrato escrito (`[67,100)`) contradiz o código (`[71,100]`). — [source: `LivingRoots/Constants.cs:50-51`, `LivingRoots.Verification/Evidence/HealthTierClassifier.cs:20-21`] (confidence: high)

## Prior Art

- Ciclo 030 corrigiu apenas `compost-growth-hardening` (`max 34 → 31`) e registrou os demais como dívida. — [source: `specs/030-roteiro-verificacao-funcional/research.md:22`, `STATE.md` dívida registrada] (confidence: high)
- Guarda `scripts/tests/test-band-scale-31-71.sh:13-24` exclui explicitamente 004/005/010 — dívida conhecida e tolerada, não detecção automática. — [source: leitura local] (confidence: high)

## Market & Context

- Sem mercado externo. Custo de inação: dois cenários ativos continuam documentando regra obsoleta em evidência de Gate 3 já promovida a `machine-pass`. — [ASSUMPTION] (confidence: medium)

## Data & Constraints

- `specs/004-playable-compost-loop/scenarios/compost-growth-healthy.scenario.json:79-85` → `min: 67`, `description: "stored health stays in the Healthy interval [67, 100)"`.
- `specs/005-soil-health-to-crop-growth/scenarios/soil-health-growth.scenario.json:66-72` → mesmos valores.
- Valor real observado nos bundles: `soilHealth 80 healthy, phaseDays 3` em `specs/004/test.md:24` e `specs/005/test.md:24`. Com `min: 71` o predicado continua aprovando 80. — [source: bundles registrados; reexecução pendente no implement] (confidence: high)
- `scripts/verify-scenario.sh` avalia `between` com tolerância 0.0001; `max: 100` e `exclusiveMax: true` permanecem inalterados (nenhuma leitura exata de 100 foi evidenciada). — [source: `PredicateEvaluator.cs:283-307`] (confidence: high)
- 010 schemas usam 34/67 apenas em `"examples"`; `jsonschema.validate` não aplica exemplos como restrição (`scripts/tests/test-schema-contract.sh:24-32`). Fixtures são glob-validados e não carregados por xUnit. — [source: investigação read-only 2026-11-22] (confidence: high)

## Evidence Against the Idea

- Reexecução de Gate 3 consome sessão headless real e pode produzir `partial`/W; nesse caso o resultado é registrado, nunca promovido.
- Alterar cenário com `machine-pass` já promovido exige novo run e atualização do bloco pelo runner (nunca edição manual).
- Os fixtures 010 permanecem com 34/67: a dívida não é "extinta" em absoluto, apenas reduzida ao escopo contratual histórico, explicitamente excluído pela guarda.

## Gaps & Open Questions

- Nenhuma bloqueante. Valor exato 100 como leitura real não foi observado; fora de escopo.

## Sources

- Arquivos locais do alvo (todos os citations acima); nenhuma fonte web utilizada.
