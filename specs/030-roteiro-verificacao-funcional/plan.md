# Implementation Plan: Roteiro de verificação funcional

**Branch**: `feature/030-roteiro-verificacao-funcional` | **Date**: 2026-11-21 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/030-roteiro-verificacao-funcional/spec.md`

## Summary

Persistir uma matriz auditável de 11 grupos de verificação (B1–B11), executar os gates autoritativos do alvo, corrigir a deriva de banda no cenário Gate 3 de compostagem em solo pobre e revalidar o bundle correspondente. Nenhuma mudança em `LivingRoots/**`; nenhuma atestação humana promovida.

## Technical Context

**Language/Version**: C#/.NET 6 (mod), Bash (gates), Python 3.12 (quality-gate), JSON (cenários)

**Primary Dependencies**: xUnit 2.9.2 + Moq/FsCheck (`LivingRoots.Tests`), `scripts/release-gate.sh`, `scripts/ci/aggregate-gate.sh`, `scripts/verify-scenario.sh`, `dotnet reportgenerator`

**Storage**: Artefatos versionados em `specs/030-roteiro-verificacao-funcional/`; bundles Gate 3 em diretórios `evidence-bundle-run-*` (gitignored)

**Testing**: `dotnet test Stardew-LivingRoots.sln -c Release --no-build` com filtros `FullyQualifiedName~`; smoke launch-free; Gate 3 headless sob Xvfb

**Target Platform**: Linux (host de desenvolvimento), SMAPI 4.5.x / Stardew Valley 1.6.14+; `GamePath=/home/luis/stardew-assemblies`

**Project Type**: Ferramental interno de verificação (documentação + artefatos de teste); sem código de aplicação

**Performance Goals**: Smoke launch-free ≤ 2700 s por arquivo (`RUN_ALL_FILE_TIMEOUT_SECS`); Gate 3 com 3 sinais concordantes

**Constraints**: Gate 3 `partial` → exit 19, nunca promover; `machine-pass` ≠ `verified`; zero diff em `LivingRoots/**`; sem importação de gates da factory

**Scale/Scope**: 11 grupos xUnit, 7 specs com `test.md`, 1 cenário editado, 4 documentos de estado

## Constitution Check

*GATE: avaliado antes da Phase 0 e reavaliado após a Phase 1.*

| Princípio | Situação | Justificativa |
|---|---|---|
| I — DDD e camadas | PASS | Nenhuma mudança em `Domain/`, `Services/` ou `Controllers/` |
| II — Segurança de dados | PASS | Nenhum input de jogador alterado; segredos não copiados |
| III — Async-only | PASS | Nenhum código C# alterado |
| IV — TDD | PASS | Não há produção nova; a própria matriz é a verificação |
| V — Simplicidade/YAGNI | PASS | Reutiliza gates e cenários existentes; um único artefato novo |
| VI — Multiplayer first-class | PASS | Grupo B9 verifica locks/sync; D1 permanece `[NV]` |
| Gate 3 para código rodável | N/A | Zero diff em `LivingRoots/**`; o cenário alterado é reexecutado |

## Project Structure

### Documentation (this feature)

```text
specs/030-roteiro-verificacao-funcional/
├── plan.md                    # este arquivo
├── research.md                # decisões e fontes locais
├── data-model.md              # entidades da matriz e do bundle
├── quickstart.md              # comandos de validação executados
├── verification-matrix.md     # FR-001/FR-002 — matriz B1..B11 + camadas A/C/D
├── checklists/requirements.md # revisão de qualidade de requisitos
├── checklists/verification.md # revisão de requisitos da matriz
└── tasks.md                   # gerado por /speckit.tasks
```

### Source Code (repository root)

```text
LivingRoots/                    # NÃO alterado
LivingRoots.Tests/              # NÃO alterado
specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json  # único JSON alterado
specs/004-playable-compost-loop/test.md                                          # novo runId
.specify/memory/STATE.md        # nota append-only
docs/PROGRESSO.md               # nota append-only
OBJETIVO.md, ROADMAP.md         # notas append-only
```

**Structure Decision**: Nenhuma nova convenção de diretório em `docs/`; a matriz vive no diretório da spec, seguindo o padrão `specs/*/test.md` para evidência.

## Complexity Tracking

Sem violações constitucionais; tabela não aplicável.
