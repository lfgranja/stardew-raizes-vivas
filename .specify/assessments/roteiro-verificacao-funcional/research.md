# Idea Research: Roteiro de verificação funcional

- **Slug**: roteiro-verificacao-funcional
- **Created**: 2026-11-21
- **Evidence confidence (overall)**: high

## Users & Demand

- Mantenedor/revisor precisa distinguir "funcionalidade implementada" de "funcionalidade verificada" — o próprio `OBJETIVO.md` e `ROADMAP.md` insistem que `machine-pass` ≠ `verified`. [source: `OBJETIVO.md`, `ROADMAP.md`, `VERIFICATION_PROTOCOL.md`] (confidence: high)
- Jogador/co-op é afetado indiretamente: a ausência de D1 e de atestação E é dívida conhecida, não uma falha descoberta aqui. [source: `.specify/memory/STATE.md`] (confidence: high)

## Prior Art

- Já existem 7 specs com `test.md` e bundles `machine-pass` host-only (004, 005, 013, 014, 015, 017, 020); o roteiro deve reutilizá-los, não recriá-los. [source: `specs/*/test.md`] (confidence: high)
- A matriz de cobertura por feature já foi mapeada nesta sessão por codegraph + leitura de `LivingRoots.Tests/` (11 grupos B1–B11, 7 specs com cenário). [source: exploração local, 2026-11-21] (confidence: high)
- Ciclos 026–029 estabeleceram o padrão de artefatos (intake→decision, spec/checklists/tasks, analyze/converge, validate R4, release R7). [source: `.specify/assessments/`, `.specify/validations/`, `.specify/releases/`] (confidence: high)

## Market & Context

- Não aplicável: trata-se de ferramental interno de verificação, não de produto de mercado. Nenhuma afirmação externa é usada como fundamento. (confidence: high)

## Data & Constraints

- `bash scripts/ci/aggregate-gate.sh` → `ALL STEPS PASSED` em 2026-11-21T23:11Z (0 errors, 29 warnings). [source: saída colada nesta sessão] (confidence: high)
- `GamePath`/`STARDEW_GAME_PATH` não exportados; assemblies em `/home/luis/stardew-assemblies/`; jogo real em `$HOME/.local/share/Steam/steamapps/common/Stardew Valley`; `xvfb-run` disponível. [source: comandos locais] (confidence: high)
- Smoke launch-free leva ~42 min neste host (registro 2026-11-20); `release-gate.sh` pode exceder o teto do harness. [source: `STATE.md`, `docs/PROGRESSO.md`] (confidence: high)
- Gate 3 exige 3 sinais (`soilHealthStateDelta`, `cropPhaseDelta`, `smapiLog`); `partial` → exit 19, nunca promover. [source: `scripts/lib/signals.sh`, `AGENTS.md`] (confidence: high)
- PRs #811/#812: `sdd-gate`/`verify` passam; `Build and Test`, `Check Formatting`, `Quality Gate / build-analysis|gate` falham por `BUILDDEPS_TOKEN is not set` (logs ao vivo 2026-11-20T02:56Z). `gh secret list` mostra o segredo no repositório, `dependabot/secrets` tem `total_count: 0`. #811 tem **segunda** falha real: `test_reportgenerator_and_coverlet_are_pinned` espera `10.1.0` enquanto o PR aplica `10.1.1`. [source: `gh run view --log-failed`, `gh api`, leitura do teste] (confidence: high)
- Deriva de banda: `compost-growth-hardening.scenario.json:80` usa `"max": 34` / "Poor interval (0, 34)"; `Constants.cs:50` define `31f`, e `AGENTS.md` fixa Poor `[0,31)`. [source: leitura literal] (confidence: high)
- Contagens por script em 2026-11-21T23:11Z: specs=26 (22 com `spec.md`), assessments=21 (20 com `decision.md`), validations=24, releases=24, bugs=40, pesquisa=26, tarefas 1256 done / 284 open (1540), `*.generated.md`=0. [source: comandos `find`/`grep`] (confidence: high)

## Evidence Against the Idea

- Reexecutar 20 cenários Gate 3 custa tempo de host e não produz nova funcionalidade; o valor é de auditoria, não de produto. [source: inventário de cenários] (confidence: high)
- A matriz pode ficar stale rapidamente; sem dono designado, vira dívida documental como as anteriores. [source: histórico em `STATE.md`] (confidence: medium)
- Corrigir o threshold do cenário pode exigir revalidação completa do bundle; se o runner rejeitar a edição, o trabalho é desperdiçado. [source: comportamento conhecido do runner] (confidence: medium)

## Gaps & Open Questions

- [NEEDS CLARIFICATION: a matriz deve ser persistida como artefato versionado ou apenas relatada na sessão?]
- [NEEDS CLARIFICATION: quantos cenários Gate 3 devem ser reexecutados — apenas o afetado ou os sete specs com cenário?]
- [NEEDS CLARIFICATION: qual é a precedência entre autorização explícita de merge e checks vermelhos?]

## Sources

- Fontes locais apenas (caminhos acima). Nenhuma URL externa foi buscada; nenhuma afirmação remota não verificada é usada. Status remoto #811/#812 foi observado ao vivo via `gh` em 2026-11-21 e é evidência local de API, mas o desfecho de merge permanece sujeito a confirmação ao vivo.
