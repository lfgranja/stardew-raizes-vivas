# Living Roots — Beta Feedback & Bug Reports (draft)

> Draft guide for 0.2.0-beta. Share with testers/players. Keep aligned with actual verification status.

## Verification status (be explicit)

- **Current evidence (snapshot 2026-11-10, `c07fbab`):** deterministic `release-gate.sh` ALL STEPS PASSED; 1997 passed / 0 failed / 4 skipped; 7 headless host-only E2E scenarios `machine-pass` (compost, growth, mulch, harvest quality, Harmony seam, Joja tradeoff, heirloom planting-only).
- **Not yet:** human-verified playthroughs, **D1 live farmhand/multiplayer attestation**, **SC-004** (017), 020 beyond planting. **`machine-pass` ≠ `verified`.**

**Recommended label:** `Status: machine-pass (host-only). Not human-verified. Not D1/farmhand-verified.`

## How to send a useful report (EN)

1. Reproduce the issue once if possible.
2. Upload your SMAPI log: go to https://smapi.io/log, upload `SMAPI-latest.txt` (or `SMAPI-crash.txt` if crashed), click **Save & parse log**, and copy the page link.
3. Tell us: what you did, what you expected, what happened, single-player or multiplayer (**host/farmhand**), and if it happens every time.

**Template**
```text
- Mod + version: Living Roots [version]
- Game / SMAPI / platform (Steam-GOG / OS): 
- SMAPI log link (https://smapi.io/log/...): 
- Single-player or Multiplayer (host or farmhand? split-screen/LAN/online?): 
- New save or existing save (day/season/location if relevant): 
- Steps to reproduce (1-2-3): 
  1.
  2.
  3.
- Expected result: 
- Actual result: 
- Frequency (always/sometimes/once): 
- First seen / after which update: 
- Screenshot/video (if helpful): 
```

## Como enviar um relato útil (PT-BR)

1. Reproduza o problema uma vez, se possível.
2. Envie o log do SMAPI: acesse https://smapi.io/log, envie `SMAPI-latest.txt` (ou `SMAPI-crash.txt` se travou), clique em **Save & parse log** e copie o link da página.
3. Informe: o que você fez, o que esperava, o que aconteceu, single-player ou multiplayer (**host/farmhand**) e se acontece sempre.

**Modelo**
```text
- Mod + versão: Living Roots [versão]
- Jogo / SMAPI / plataforma (Steam-GOG / SO): 
- Link do log SMAPI (https://smapi.io/log/...): 
- Single-player ou Multiplayer (host ou farmhand? tela dividida/LAN/online?): 
- Save novo ou existente (dia/estação/local, se relevante): 
- Passos para reproduzir (1-2-3): 
  1.
  2.
  3.
- Resultado esperado: 
- Resultado obtido: 
- Frequência (sempre/às vezes/uma vez): 
- Quando começou / após qual atualização: 
- Captura/vídeo (se ajudar): 
```

## Maintainer triage checklist

- [ ] Valid parsed `smapi.io/log` link; note SMAPI/game/OS, mod list, errors/warnings/skipped mods/patches.
- [ ] Correct mod + reported version/build/commit; beta context noted.
- [ ] Clear repro (steps, expected vs actual, frequency) and scope (SP/MP, host/farmhand, split-screen, save age, location/time).
- [ ] Check conflicts/deps/duplicates/outdated mods; attempt minimal-mod isolation if needed.
- [ ] Classify (crash, logic, visual/UI, save/data, MP sync, performance) and severity.
- [ ] Record verification status honestly (machine-pass host-only / human-verified / D1 farmhand verified / not reproduced).
- [ ] Repro on matching environment; for MP issues test host and farmhand perspectives where possible.
- [ ] Next action: request missing info, link duplicate/known issue, confirm fix in target build.

## Sources (research, 2026-11-12)
- https://smapi.io/log (SMAPI log parser/upload instructions) — High
- https://stardewmodding.wiki.gg/wiki/How_to_read_your_SMAPI_log — High
- `stardewvalleywiki.com/Modding:Player_Guide/Getting_Started` — Medium (guidance context)
