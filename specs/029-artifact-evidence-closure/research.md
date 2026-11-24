# Research: Fechar dívida de artefatos e evidência

- **Data**: 2026-11-20
- **Escopo**: resolver as duas decisões técnico-documentais levantadas no planejamento.

## Decisão 1 — Candidato `workflow-verification-pipeline`

- **Decision**: converter a descrição existente para a estrutura aceita pela verificação atual (`lanes`, `nodes`, `edges`, `diagram_type: workflow`), preservando os nós e conexões originais.
- **Rationale**: é o menor diff que atinge `ok:true` sem descartar conteúdo já produzido; a verificação reportou explicitamente `layout/components/connections` como propriedades adicionais proibidas e `diagram_type` divergente.
- **Alternatives considered**: substituir o `candidate.json` por um novo arquivo — rejeitado por risco de perda de informação; apagar o candidato — rejeitado por contrariar FR-001.

## Decisão 2 — Candidato `workflow-heirloom-pipeline`

- **Decision**: ajustar os limites das fases para que `growth`, `harvest` e `sync` não se sobreponham, redirecionar as arestas `e-postfix-request`/`e-postfix-refusal` para corredores distintos e tornar o `mainPath` coerente com as arestas existentes.
- **Rationale**: os erros são determinísticos e localizados; a correção preserve o conteúdo do fluxo plantio → request → harvest → seed-save.
- **Alternatives considered**: desabilitar a checagem de corredor — rejeitado (mascara erro); reduzir o diagrama — rejeitado (perda de conteúdo).

## Decisão 3 — Resíduos de checkboxes

- **Decision**: revisar apenas itens cuja evidência esteja em `STATE.md`, `validate.md` ou `release.md`, rotulando a marcação como revisão de qualidade de requisitos.
- **Rationale**: espelha o padrão já usado no ciclo 026 e evita afirmar implementação nova.
- **Alternatives considered**: marcar todos os checkboxes abertos — rejeitado por falta de evidência e por risco de falsa conclusão.

## Decisão 4 — Gate autoritativo

- **Decision**: manter `scripts/ci/aggregate-gate.sh` e `scripts/release-gate.sh` como autoridade; os seis scripts materializados da factory são diagnóstico não-autoritativo.
- **Rationale**: Bloco 0.a; falham no alvo por baselines específicas da factory (`production-gate-hub.policy.json` ausente, specs sem `spec.md`).
- **Alternatives considered**: importar `check_production_gate.py` como gate — rejeitado por divergência de provedor/CLI e governance do alvo.

