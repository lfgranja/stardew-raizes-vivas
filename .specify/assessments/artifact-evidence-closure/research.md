# Idea Research: Fechar dívida de artefatos e evidência

- **Slug**: artifact-evidence-closure
- **Created**: 2026-11-20
- **Evidence confidence (overall)**: high (evidência executada localmente; nenhuma fonte web consultada)

## Users & Demand

- Mantenedor/leitor do repositório: dois de cinco candidatos archify falham `validate`, e as âncoras `docs/PROGRESSO.md`/`docs/research/index.md` não existem — a dívida é registrada explicitamente em `.specify/memory/STATE.md` desde o ciclo 026. [source: `.specify/memory/STATE.md`, `archify validate` executado em 2026-11-20] (confidence: high)
- Nenhuma demanda de terceiros encontrada; o valor é decisório/documental, não de jogador. [ASSUMPTION] (confidence: medium)

## Prior Art

- Ciclo 026 já fechou 11 checkboxes de `specs/026-stabilize-overlay-cache/tasks.md` como **revisão de qualidade de requisitos**, sem afirmar implementação nova — padrão reutilizável para resíduos de 004/015/018/019. [source: `.specify/memory/STATE.md`, seção "Revisão 026"] (confidence: high)
- `docs/PROGRESSO.md` e `docs/research/index.md` foram criados nesta sessão com fatos locais verificados; não são cópia da factory. [source: arquivos criados em 2026-11-20] (confidence: high)
- A factory (`b1deab1`) possui os seis scripts de gate, mas suas baselines são específicas daquele projeto: executados no alvo, falham com `FileNotFoundError` de `production-gate-hub.policy.json` e 3 specs sem `spec.md` — N/A como autoridade. [source: execução dos seis scripts em 2026-11-20] (confidence: high)

## Market & Context

- Custo de não corrigir: `docs/architecture/` ausente e candidatos inválidos impedem regeneração confiável de diagramas; dívida cosmética contínua em `STATE.md`. [source: `STATE.md`, `archify validate`] (confidence: high)
- Nenhum impacto de mercado externo; assuntos remotos seguem `[NV]`. [ASSUMPTION] (confidence: low)

## Data & Constraints

- `archify validate` em 5 candidatos: 3 `ok:true`; `workflow-heirloom-pipeline` falha por sobreposição de fases (`growth` 3..3 vs `harvest` 3..5), corridor ambíguo e `mainPath` sem edge; `workflow-verification-pipeline` usa schema antigo (`layout/components/connections`) em vez de `lanes/nodes/edges`. [source: saída do comando em 2026-11-20] (confidence: high)
- `.archify/` é gitignored; correções não entram no diff versionado — o PR precisa conter artefatos SDD e notas documentais como entregável. [source: `STATE.md` ciclo 026 + `git status`] (confidence: high)
- Gate autoritativo do alvo permanece verde: `scripts/ci/aggregate-gate.sh` → ALL STEPS PASSED. [source: execução em 2026-11-20] (confidence: high)

## Evidence Against the Idea

- Impacto baixo (I=2): não muda comportamento do mod nem desbloqueia PRs; pode ser vista como limpeza cosmética. [source: própria auditoria] (confidence: high)
- Corrigir o candidato `workflow-verification-pipeline` pode exigir conversão de schema não trivial, com risco de perder detalhes do layout atual. [ASSUMPTION] (confidence: medium)
- Alternativas de maior impacto (C2 PRs, C3 SC-004) estão bloqueadas por owner/humano, o que torna C4 o melhor candidato executável, não o mais valioso absoluto. [source: ranking Bloco 5] (confidence: high)

## Gaps & Open Questions

- [NEEDS CLARIFICATION: converter o candidato de schema antigo ou substituí-lo por um novo candidate.json validado?]
- [NEEDS CLARIFICATION: quais resíduos de checkboxes têm evidência auditável suficiente nesta sessão?]

## Sources

- Repo local `/home/luis/development/stardew-raizes-vivas` — `.specify/memory/STATE.md`, `.archify/*/candidate.json`, `specs/*/tasks.md`, `OBJETIVO.md`, `ROADMAP.md` (política: codebase pointer, leitura direta).
- Factory `/home/luis/development/MDMV/projetos/mdmv-dark-factory` @ `b1deab1` — somente leitura, baseline limpa via `git show` (política: fonte de aparato).
- Nenhuma URL fetchada; nenhuma afirmação remota revalidada nesta pesquisa.

