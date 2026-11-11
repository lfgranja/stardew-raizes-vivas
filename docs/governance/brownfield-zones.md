# Brownfield Zone Map — Living Roots (research 11 / Addy Osmani)

Status: **definido** (2026-11-10)
Referência: `mdmv-dark-factory` docs/research/11-greenfield-brownfield-migracao.md (Addy Osmani, "Brownfield Agentic Engineering").

## Princípio

Antes de qualquer agente autônomo tocar o código, uma pessoa desenha o mapa de zonas. Zonas só mudam quando ganhas. A zona define os verbos permitidos. Nunca deixar shims; migrar em unidades completas. "Write down what the code can't say" (memo de compreensão duradouro). Testes de caracterização travam o comportamento atual antes de refatorar (nunca a mesma sessão escreve os testes e os faz passar).

## Zonas

### 🟢 Green — loop apertado do agente

Lógica pura, isolada, bem testada. Agente pode editar com loop apertado (build + test).

- `LivingRoots/Domain/` — interfaces, modelos, lógica pura (sem usings SMAPI).
  - `Domain/Interfaces/`
  - `Domain/Models/`
  - `Domain/Services/` (HealthCategory, HeirloomIdentity, HeirloomSoilMath)
  - `Domain/Visualization/`
- `LivingRoots.Tests/` — testes xUnit (um arquivo por subject).

**Regras:** agente pode editar; rodar `dotnet test` após cada mudança. Sem shims.

### 🟡 Yellow — testes de caracterização primeiro

Qualidade mista. Antes de mudar, pinar o comportamento atual com testes de caracterização. Agente pode editar após testes verdes.

- `LivingRoots/Services/` — implementações e persistência.
  - `Services/Visualization/`
  - `Services/HeirloomSeedSaveService.cs`
- `LivingRoots/i18n/` — strings de localização (parity test enforça igualdade de chaves).
- `LivingRoots/Assets/` — assets de jogo (sprites, audio).
- `LivingRoots/Constants.cs` — ModConstants (fonte única de magic numbers).
- `scripts/`, `scripts/ci/`, `scripts/lib/`, `scripts/telemetry/`, `scripts/tests/`.

**Regras:** escrever testes de caracterização antes de mudar; rodar `dotnet test` + `dotnet format`. Agente assistido, revisão humana recomendada.

### 🔴 Red — humano no passo a passo (ou não fazer)

Estado compartilhado, segurança, persistência, Harmony patches, orquestração de eventos. Agente não toca sem humano a cada passo.

- `LivingRoots/Controllers/ModController.cs` — orquestrador SMAPI; estado host-authoritative (Principle VI); atomic locks; FR-009/A8 soil-health MP sync.
- `LivingRoots/Patches/` — Harmony patches (HarvestPatches, HeirloomPatches); try/catch fallback.
- `LivingRoots/Services/ModDataService.cs` — persistência via save system; sanitização de paths.
- Segurança: validação de solo health (0-100), path traversal, homoglyph detection, overflow checking.
- Multiplayer host-authoritative state (soil health, compost, machine progress).

**Regras:** humano no passo a passo. Pair programming a cada mudança. Nunca deixar shims. Migrar em unidades completas. Agente pode sugerir, mas não aplicar autonomamente.

## Zonas em YAML (para consumo por agentes)

```yaml
zones:
  green:
    paths:
      - LivingRoots/Domain/
      - LivingRoots.Tests/
    rule: "tight loop: agent edits, dotnet test after each change, no shims"
  yellow:
    paths:
      - LivingRoots/Services/
      - LivingRoots/i18n/
      - LivingRoots/Assets/
      - LivingRoots/Constants.cs
      - scripts/
    rule: "characterization tests first, then edit; dotnet test + dotnet format; human review recommended"
  red:
    paths:
      - LivingRoots/Controllers/ModController.cs
      - LivingRoots/Patches/
      - LivingRoots/Services/ModDataService.cs
    rule: "human step-by-step or don't do; pair programming; never shims; migrate in complete units"
```

## Regras transversais (research 11)

1. A pessoa desenha o mapa (não o agente).
2. Zonas só mudam quando ganhas (evidência de testes/estabilidade).
3. A zona define os verbos (green: edit livre; yellow: test first; red: humano no passo).
4. "Write down what the code can't say" — memos de compreensão duradouros em `docs/`.
5. Testes de caracterização travam comportamento atual antes de refatorar (nunca mesma sessão escreve e faz passar).
6. Migrar em unidades completas (caminho velho deletado, não shim).
7. Paralelizar por último.

## Referências

- `mdmv-dark-factory` docs/research/11-greenfield-brownfield-migracao.md (Addy Osmani, "Brownfield Agentic Engineering", 14/09/2026).
- ADR 002 (zoneamento brownfield, seção "Zoneamento brownfield").
- Constitution Principle VI (multiplayer first-class).
- `docs/mp-live-verification.md`.
- ESC-LR-20261110-005 (MP live verification owed).
