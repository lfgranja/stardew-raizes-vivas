# Human Verification Playbook — Living Roots

- **Prepared**: 2026-11-18 (agent-prepared, **not executed**)
- **Execution status**: `pending` — no human attestation has been recorded; every Gate 3 claim remains `machine-pass host-only` until a real session is logged
- **Owner**: maintainer (you). The agent can prepare files, commands and templates; only a human with the game can produce the observation signal and the E attestation.

This is a time-boxed front-end to `VERIFICATION_PROTOCOL.md` §3 and `specs/009-end-to-end-game-verification/checklists/gate3-verification-contract.md`. It does not replace them; it makes them easy to start when you are tired.

## Escolha um caminho (tempo real)

| Caminho | Tempo | O que produz | O que **não** produz |
|---------|-------|--------------|----------------------|
| **A — mínimo** | ~15–20 min | 1 host Gate 3 run (compost loop) com 3 sinais | D1, SC-004, promoção a `verified` em specs com SC pendente |
| **B — host completo** | ~40 min | A + persistência (save/reload) + segundo loop (heirloom plant) | D1 e SC-004 |
| **C — completo** | ~90 min | B + sessão D1 (2 players) + SC-004 | Nada; é o teto de evidência humana hoje |

Se você só tem 15 minutos: faça **A** e pare. Um A bem registrado vale mais que um C pela metade.

## Antes de abrir o jogo (5 min, posso fazer por você)

- [ ] Backup da pasta de saves com data (`~/.../StardewValley/Saves/<YYYYMMDD>-lr-baseline/`).
- [ ] Perfil de teste dedicado; mod list/versões anotadas (`manifest.json` + SMAPI header).
- [ ] Build Release atual (`GamePath=/home/luis/stardew-assemblies dotnet build ... -c Release -m:1`) e mods deployados.
- [ ] SMAPI-latest.txt rotacionado/renomeado com data para virar evidência de log.
- [ ] Save inicial copiado como `pre-state.json`/`pre-state.sav` para diff posterior.

## A — Sessão Gate 3 host (mínimo viável)

**Cenário**: `specs/004-playable-compost-loop/scenarios/compost-growth-hardening.scenario.json` (já existe, machine-pass `7374`).

1. Abrir via **SMAPI** (não vanilla). Confirmar header SMAPI no console e nenhum `Skipping mod` para `lfgranja.LivingRoots`.
2. Anotar estado **antes**: dia/estação/clima, tile-alvo, valor de soil health observado, estado do bin.
3. Executar na ordem: lavrar tile → inserir resíduo orgânico no bin → avançar dia (dormir) → aplicar composto → observar tile → salvar e sair para título → reentrar no mesmo save.
4. Capturar **os três sinais obrigatórios** (falta um → não é `verified`):
   - **Observação**: notas datadas com o que apareceu na tela em cada transição (ex.: "bin UI mostrava `Ready` e timer incrementou", não "pareceu ok"). Foto opcional, mas útil.
   - **Artefato**: diff do save (campos `CompostingBinData` / `SoilHealthState`), conferindo valor não-default persistido e ausência de entradas `0` onde o estado mudou (convenção sparse cache).
   - **Log**: trecho de `SMAPI-latest.txt` sem WARN/ERROR/exception de draw/update, com timestamp da sessão.
5. Comparar **quantitativamente** pré/pós: valor de soil health antes/depois, fase do bin, presença do composto após reload. Nada de "pareceu funcionar".
6. Salvar evidência em `specs/004-playable-compost-loop/verification/human-gate3-<YYYYMMDD>-<HHMM>.md` com: data, commit do mod, locale, quem executou, 3 caminhos de arquivo, veredito `pass|fail`, e a frase exata: `atested-by: <nome> (human)`.
7. Se os 3 sinais concordarem: rodar `./scripts/verify-scenario.sh --verify-bundle <bundle>` e promover **somente** via `scripts/lib/promotion.sh`. `machine-pass` ≠ `verified`.

## B — Extensão host (opcional, +25 min)

- Repetir o bloco A para o loop de plantio herança (spec 020, bundle `6021`) — atenção: o bundle atual cobre **só plantio**; colheita, grant de semente e aquisição no carro da Traveler continuam sem Gate 3.
- Testar save existente (mid-playthrough), não apenas save limpo; anotar qual dos dois casos foi usado.

## C — D1 + SC-004 (precisa de você + segunda pessoa)

**D1 — multiplayer real (constituição Princípio VI)**
- Duas instâncias reais (host + farmhand), mesmo mod/build nos dois clientes.
- No farmhand: lavrar/aplicar composto/colher; no host: observar o mesmo tile e confirmar valor idêntico após convergência.
- Conflito de mesma tile: duas ações concorrentes; a segunda deve ser **recusada com mensagem explícita** e inventário intacto (first-wins), nunca last-write-wins.
- Evidência: notas de ambos os clientes + logs + diff do save do host. Sem isso, 004 T034, 014 T109, 015 T049/T057 e 018 farmhand-guards permanecem abertos.

**SC-004 — compreensão (spec 017)**
- Seguir `specs/017-joja-agrochemical-tradeoff/verification/sc004-comprehension-protocol.md` à risca: participante adulto, sem conhecimento prévio do spec/protocolo; prompt `sc004-v1`; registrar timing, locale, respostas e desvios.
- Meta: ≥90% de acerto nos dois elementos (atalho de curto prazo **e** custo de solo). Se não rodar, SC-004 fica `pending` — não marcá-lo como completo.

## Regras de honestidade (não negociáveis)

- Nunca promover `machine-pass` → `verified` sem os 3 sinais de uma sessão humana real.
- Nunca escrever "verificado por build / N testes passaram" como prova de Gate 3.
- Marcar `[NV]` toda afirmação sobre status remoto/CI que não tenha sido checada ao vivo na hora.
- Não apagar nem renomear `evidence-bundle-run-*`; trilha é append-only.
- Se o teste falhar, registrar o falso/erro com o mesmo rigor do sucesso — um `fail` bem documentado é evidência útil.

## O que eu posso fazer agora sem você

- Preencher os templates de evidência com data/commit/caminhos já conhecidos.
- Gerar o comando exato de build/deploy e o `diff` do save assim que você me disser o caminho do save de teste.
- Consolidar seu relato em `test.md` / `validate.md` e rodar os gates determinísticos depois da sessão.
- **Não posso**: gerar sinal de observação, atestar `verified`, nem simular segundo jogador.

## Status desta sessão

Nenhuma sessão humana executada. Campo reservado para preenchimento pós-execução:

```text
executed-at: _
executed-by: _
commit: _
path: A | B | C
signals: observation _ | artifact _ | log _
verdict: _
promotion: _
```
