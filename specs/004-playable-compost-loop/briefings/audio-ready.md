# Briefing: som de ready do composting bin (`composting_bin_ready.ogg`)

Status: briefing de pesquisa — **nenhuma decisão de implementação tomada aqui**.
Spec: `specs/004-playable-compost-loop/spec.md` (FR-008a, FR-005b) · Data: 2026-10-05

## 1. Estado atual (verificado no repo)

- `LivingRoots/Assets/Audio/composting_bin_ready.ogg` e `composting_bin_processing.ogg`
  existem mas são **placeholders vazios de 24 bytes** (só o header `OggS`, zero amostras).
  Não há áudio real para mixar, normalizar ou aprovar — o arquivo precisa ser
  produzido do zero (ou descartado em favor de cue vanilla, ver §2).
- **Unwired total**: nenhuma referência a esses arquivos em C#. `ModEntry.OnAssetRequested`
  (`LivingRoots/ModEntry.cs:114`) registra `Data/CraftingRecipes`, `Data/Objects` e
  `Data/Machines` — **não** registra `Data/AudioChanges`. Não há `Helper.ModContent.Load`,
  `Game1.playSound` com cue custom, nem `Content.Load` em lugar algum.
- O `.csproj` (`LivingRoots/LivingRoots.csproj:26`) copia `Assets/**/*` para o output,
  então os `.ogg` viajam no pacote sem função — peso morto, não risco.
- O que toca hoje: `LivingRoots/Services/CompostingBinService.cs:98,140` chama
  `Game1.playSound("Ship")` na coleta (`CollectCompost`, :98) **e** na transição
  Processing → Ready dentro de `ProcessDayStart` (:140, via
  `ModController.OnDayStarted` em `LivingRoots/Controllers/ModController.cs:838`).
  `Ship` é o som de itens caindo na caixa de expedição (shipping bin — wiki:
  *"sound of items dropping into a wooden box"*) — semântica errada
  para "máquina pronta". Deve ser **removido dos dois pontos** nesse trabalho
  (ver §7: nenhum `Ship` pode restar vindo do bin).
- Origem do requisito: `specs/002-soil-decay-compost/spec.md` **FR-020** — som
  ambiente sutil intermitente (a cada 2–3h in-game) durante processing **+**
  *"distinct 'ready' chime when compost is finished"*, com *"intermittent
  playback prevents noise pollution with multiple bins"*. Este briefing cobre só
  a segunda metade (ready chime); o processing é o briefing-irmão
  `briefings/audio-processing.md` (gate de arquivo em §7.1, snippet canônico de
  registro `Data/AudioChanges` em §4.2 / linhas 145–167 — reutilizar o mesmo
  shape, só troca ID/arquivo/categoria-tradeoff).
- Rejeição de item inválido **já está fora do escopo de áudio custom**: FR-008a exige
  comportamento nativo de máquina vanilla (cue `cancel` + shake do sprite, sem texto).
  O ready-cue não deve reutilizar `cancel` por nenhum motivo.
- Nenhuma task de áudio existe em `specs/004-playable-compost-loop/tasks.md` — o wiring
  do som ready é trabalho não escopado. Este briefing alimenta uma task futura (US2).

## 2. Cues vanilla de machine-ready: o que reutilizar vs. customizar

Fontes primárias: `Modding:Audio` (track list exportada do soundbank) e
`Modding:Machines` (seção Audio & visuals), ambas na Stardew Valley Wiki.

### 2.1 Fato central: vanilla não tem "fanfarra de pronto"

Em Stardew 1.6, máquinas **não tocam som proativo ao completar**. O sinal de ready é
visual: troca de sprite (`ShowNextIndexWhenReady`), partículas/vapor e o ícone de
output coletável. Sons entram em dois momentos:

- **Load/Working** — via `LoadEffects` / `WorkingEffects` em `Data/Machines`
  (ex.: `furnace` = fogo rugindo enquanto processa; `bubbles` = borbulho de keg/cask/oil maker).
- **Interação de coleta** — som no momento em que o jogador retira o output.

Conclusão: não existe cue vanilla com a semântica exata "seu compost ficou pronto,
venha buscar". Qualquer reutilização é aproximação — aceitável e recomendada como
**fallback**, com cue custom como camada opcional por cima.

### 2.2 Candidatos vanilla (todos `Game1.playSound("<id>")`, categoria Sound)

| Cue | Descrição oficial (wiki) | Adequação p/ ready | Risco |
|-----|--------------------------|--------------------|----|
| `questcomplete` | ascending "success" jingle; completa quests | Alta — semântica de conclusão | Médio: jogador associa a quests |
| `reward` | sparkling "success" jingle; wallet items | Alta — conclusão genérica | Baixo-médio |
| `achievement` | bright success synth chime c/ reverb | Média — forte demais, vira alarme | **Alarm fatigue**; evitar como padrão |
| `secret1` | mystical chime; descoberta de segredos | Média — tom mágico, não terroso | Dissonante do tema solo/compost |
| `newRecord` / `newArtifact` | sparkling jingle; recorde/level-up | Média | Associa a level-up |
| `jingle1` | high-pitched "ding"; fish pond quests | Média — curto e neutro | Agudo demais p/ uso repetido |
| `crystal` | high-pitched 'ding!' c/ ressonância; elevador | Média | Reverbera segundos — longo |
| `machine_bell` | bell dinging once; prize machine | Média-alta — semântica de máquina | Pouco conhecido, neutro |
| `harvest` / `pickUpItem` / `coin` | "pop"; colheita/coleta | Baixa p/ ready (ok p/ **coleta**) | Confunde pronto × coletado |
| `Ship` (atual) | itens caindo na shipping bin | **Inadequada — remover** | Semântica errada |
| `cancel` | short shaker; cancelamento | **Só p/ rejeição (FR-008a)** | Nunca usar como ready |
| `furnace` / `bubbles` | loops de working | Só p/ estado processing, não ready | — |

### 2.3 Recomendação

1. **Baseline (sem asset novo, YAGNI)**: no wiring, tocar cue vanilla de conclusão curta
   no momento certo (§4) — `reward` ou `questcomplete` como padrão, `machine_bell` como
   alternativa neutra. Custo zero, zero risco de formato, respeita slider de som.
2. **Evolução (pós-004, se playtest pedir identidade sonora)**: registrar cue custom
   `LivingRoots_BinReady` via `Data/AudioChanges` (§3) com **fallback automático** para
   o vanilla caso o registro falhe (cue ausente não pode quebrar nada — FR-014).
   Nunca substituir cue vanilla por override (override de `Data/AudioChanges` é
   permanente na sessão e afeta o jogo base).

## 3. Spec técnica do OGG custom (se a opção 2 for adotada)

Fonte: `Modding:Audio` — seções Data format, Category list, Audio file format,
Manage audio in C#. O shape canônico do snippet C# (`AssetRequested` →
`AsDictionary<string, AudioCueData>`, paths absolutos via `Helper.DirectoryPath`)
está em `briefings/audio-processing.md:145-167` — reutilizar o mesmo shape aqui,
trocando só ID (`LivingRoots_BinReady`), arquivo e trade-offs de categoria.

- **Formatos aceitos**: somente `.ogg` (Vorbis) ou `.wav`. Para SFX curto, `.ogg`
  Vorbis é o esperado; `.wav` só se justifica para sons minúsculos.
- **Estéreo obrigatório**: áudio mono soa mais baixo/abafado que os demais SFX
  (nota explícita da wiki). Produzir/exportar em **estéreo**.
- **Taxa/amostras**: sem exigência documentada além de Vorbis válido; praticar
  44.1 kHz estéreo, pico normalizado próximo aos SFX vanilla (comparar de ouvido
  contra `reward`/`questcomplete` no jogo; evitar master alto — fadiga).
- **Tamanho**: SFX deve ser pequeno (a wiki cita 2–4 MB como teto para *músicas*;
  um ding de <1,5 s deve ficar em **dezenas de KB**).
- **Registro** — editar `Data/AudioChanges` via `AssetRequested` em C# (tipo esperado:
  `IDictionary<string, AudioCueData>`). Campos:

  ```csharp
  // em ModEntry.OnAssetRequested, novo branch:
  // e.NameWithoutLocale.IsEquivalentTo("Data/AudioChanges")
  e.Edit(asset =>
  {
      var data = asset.AsDictionary<string, StardewValley.GameData.AudioCueData>().Data;
      data["LivingRoots_BinReady"] = new StardewValley.GameData.AudioCueData
      {
          ID = "LivingRoots_BinReady",
          Category = "Sound",   // respeita o slider de som (nunca Music/Ambient)
          FilePaths = new List<string>
          {
              Path.Combine(this.Helper.DirectoryPath, "Assets", "Audio", "composting_bin_ready.ogg")
          },
          StreamedVorbis = false, // SFX curto: cache em memória, custo zero por play
          Looped = false,
          UseReverb = false,      // reverb = rastro longo = fadiga; ver §5
      };
  });
  ```

  Notas de contrato (documentadas na wiki, fáceis de errar):
  - `FilePaths` são **paths absolutos de disco**, não asset names — usar
    `Path.Combine(this.Helper.DirectoryPath, ...)` (cf. `Modding:Modder Guide/APIs/Utilities`).
  - O override é **permanente na sessão** mesmo se o asset for editado depois; e
    sobrescrever um ID **reseta todos os campos** — por isso usar ID próprio com
    prefixo do mod (`LivingRoots_`), nunca reusar ID vanilla.
  - `StreamedVorbis = false` para SFX curto e ocasional (recomendação literal da wiki);
    `true` seria para música/ambiente longo.
  - `Category = "Sound"`: o ready-cue deve obedecer ao slider de efeitos. `Ambient`
    seria para loops de fundo; `Music` quebraria a expectativa do jogador.
- **Playback**: `Game1.playSound("LivingRoots_BinReady")` (só jogador local) ou
  posicional via `GameLocation` (ver §4). Chamada deve ser tolerante a cue ausente
  (try/catch + fallback vanilla; log sanitizado FR-015).

## 4. Quando tocar (anti alarm-fatigue)

Restrições de contexto:

- O bin leva **2 dias** (MaturationDays = 2); a transição Processing → Ready acontece
  em `OnDayStarted`/day-start, **enquanto o jogador dorme**. Tocar som global às 6h
  para bins fora da tela = notificação sem referente = fadiga + confusão.
- Baseline saúde 0 é **neutra por decisão** (spec 004, FR-009/FR-010): o jogo já
  minimiza ruído visual; o áudio deve seguir a mesma filosofia.

Direção recomendada:

1. **Nunca tocar no day-start global** por bin (pior caso: N bins = N dings às 6h).
   A transição de estado continua silenciosa no modelo de dados.
2. **Tocar uma vez por entrada em location**: quando o jogador local entra numa
   location que contém ≥1 bin Ready, tocar **um único** ding posicional
   (`location.localSoundAt(cue, tileDoBinMaisPróximo)` ou `playSoundAt`), não um por bin.
3. **Debounce**: cooldown por location (ex.: não repetir em reentradas dentro de
   alguns minutos de jogo) + throttle se múltiplos bins ficarem ready juntos.
4. **Na coleta**: som curto de coleta (pop/`harvest`-like vanilla) é opcional e
   separado do ready-cue — pronto × coletado devem soar diferente.
5. **Posicional, não global**: preferir `GameLocation.localSound*` / `playSoundAt`
   (com atenuação por distância) a `Game1.playSound` quando houver tile de origem.
   Semântica de multiplayer (§6) decide qual.

## 5. Direção de áudio (ding/chime terroso curto)

- **Timbre**: ding/chime curto de caráter terroso/orgânico (madeira, kalimba mutada,
  cerâmica grave) — identidade "solo vivo", não sino de igreja nem fanfarra synth.
  Evitar `achievement` (reverb longo) como referência de intensidade.
- **Envelope**: ataque suave, **decaimento rápido, sem cauda** — duração total
  **< 1 s (alvo 0,5–1,0 s)**. Cauda longa em som repetível diariamente = fadiga.
  Assinatura: **wooden tok + blip ascendente faint** (um "tok" de madeira mutada,
  cf. `Cowboy_gunshot` *"short, hollow 'tock'"* como referência de ataque, +
  blip curto que sinaliza "done"). Uma nota ou duas, sem arpejo.
- **Altura/dinâmica**: registro médio, volume percebido **≤ cues de conclusão vanilla**;
  nada que compita com `questcomplete` em presença. Uma nota ou duas, sem arpejo.
- **Acessibilidade — redundância obrigatória**: áudio nunca é o único canal.
  O ready já tem: (a) sprite de bin cheio (`composting_bin_ready.png` via
  `ShowNextIndexWhenReady`), (b) HUD/toast nativo na entrada da location ou na
  coleta (mesmo padrão FR-008a: `Game1.showRedMessage` para recusas;
  `Game1.addHUDMessage` toast para info — cf. pesquisa 001 em
  `specs/001-soil-health-visualization/research-notification-mechanisms.md`).
  Jogador com som zerado/mudo deve descobrir o ready só pelo visual.
  Texto "+15" flutuante pertence ao **momento da aplicação** do compost, não ao ready —
  não reusar o mesmo floating text para dois eventos.

## 6. Multiplayer: semântica de playback e como testar áudio

Fonte: `Modding:Audio` § Manage audio in C# (quatro métodos, semânticas distintas):

| Método | Quem ouve | Uso p/ ready |
|--------|-----------|--------------|
| `Game1.playSound(cue)` | só jogador local, sem sync, sem posição | UI/menus; ok p/ evento local confirmado |
| `GameLocation.localSound(cue[, tile])` | jogador atual **se** estiver na location (c/ atenuação opcional) | **padrão p/ ready na entrada de location** |
| `GameLocation.playSound(cue[, tile][, pitch])` | **todos** os jogadores na location | evitar p/ ready (um jogador entrando não deve dingar nos outros) |
| `DelayedAction.playSoundAfterDelay(...)` | configurável (local ou todos) | sequências com delay, se um dia precisar |

Regras para o wiring futuro:

- Day-start roda no host; som de ready **não** deve ser broadcast a todos —
  cada cliente toca localmente ao **entrar** na location (reativo, não push).
- `Game1.playSound` dentro de `CompostingBinService.ProcessDayStart` é o padrão atual
  e está errado em dois eixos: semântica (`Ship`) e alcance (global sem posição).
- Pitch: `playSoundPitched` (0–2400, 1200 = default) permite variação sutil entre
  plays se um dia houver múltiplos bins — usar com moderação ou não usar.

Como testar (sem infra nova — tudo com jogo + SMAPI):

1. **Unit (CI)**: teste que o ID do cue é resolvido via constante, que falha de
   registro cai para o fallback vanilla, e que o debounce suprime o 2º play
   (stubs de `IGameAudio`-like ou seam equivalente — sem tocar engine real).
2. **SMAPI console**: comando `debug logSounds` loga cada cue tocado (com dados
   posicionais) — verifica ID correto, momento (entrada de location, não 6h) e
   ausência de duplicatas por bin.
3. **Soundboard mod** (Nexus #34968, menu F3): ouve cues vanilla isolados para
   escolher o fallback e calibrar volume do custom contra eles; flag "vanilla only"
   garante audição sem interferência de outros mods.
4. **Manual single-player**: place bin → load waste → dormir 2 dias → entrar na farm:
   espera-se **1 ding + sprite ready**. Reentrar na location dentro do cooldown:
   silêncio. Coleta: som de coleta distinto. `cancel`/shake só em drop inválido.
5. **Manual multiplayer (roteiro, ligado ao Roadmap D1)**: host + 1 farmhand.
   Matriz: (a) farmhand entra na farm com bin ready → só ele ouve;
   (b) host coleta → farmhand não ouve ding fantasma;
   (c) bins em locations diferentes → atenuação/distância respeitada;
   (d) slider de som zerado num cliente → outro cliente ainda ouve (categoria Sound).
   Registro em vídeo curto ou checklist assinado basta — sem automação de rede (fora
   de escopo da 004, como FR-019 já delimita para estado).

## 7. Verificação (critérios de aceite p/ a task futura)

### 7.1 File gate do ready (CI-able, sem jogo — espelha `audio-processing.md` §7.1)

```bash
cd /home/luis/development/stardew-raizes-vivas
[ "$(stat -c%s LivingRoots/Assets/Audio/composting_bin_ready.ogg)" -gt 10000 ] \
  && file LivingRoots/Assets/Audio/composting_bin_ready.ogg | grep -q "Ogg data" \
  && ffprobe -v error -show_entries stream=codec_name,channels,sample_rate,duration \
    -of default=noprint_wrappers=1 LivingRoots/Assets/Audio/composting_bin_ready.ogg
```

Esperado: size **> 10 KB** (stub atual de 24 bytes falha aqui por ~3 ordens de
magnitude), `Ogg data, Vorbis audio`, `channels=2` (estéreo — mono soa
baixo/abafado, nota explícita da wiki), `sample_rate=44100`,
`duration` em **[0.5, 1.0]**. `ffprobe` existe na máquina do maintainer
(`/usr/bin/ffprobe`, v8.1.3 — verificado 2026-10-05).

### 7.2 Critérios de aceite

- [ ] `composting_bin_ready.ogg` real (estéreo, **< 1 s**, dezenas de KB, passa
      §7.1) ou decisão documentada de baseline 100% vanilla.
- [ ] Cue registrado via `Data/AudioChanges` com ID `LivingRoots_*`, categoria Sound,
      `StreamedVorbis = false`, paths absolutos via `Helper.DirectoryPath`.
- [ ] `Game1.playSound("Ship")` removido dos dois pontos
      (`CompostingBinService.cs:98` coleta e `:140` transição) — nenhum `Ship`
      restante vindo do bin; `grep playSound LivingRoots/` prova.
- [ ] Som toca **na entrada de location com bin Ready** (1×, posicional, debounced) —
      nunca N× no day-start global.
- [ ] Fallback vanilla funciona com registro ausente/corrompido (sem exceção, FR-014;
      log sanitizado FR-015).
- [ ] Sinal visual redundante presente (sprite ready + toast/HUD) — jogo mutado é
      totalmente jogável.
- [ ] `debug logSounds` confirma ID, timing e contagem em sessão manual.
- [ ] Rejeição inválida continua vanilla (`cancel` + shake, sem texto) — intocada.

## 8. Out-of-scope (explicitamente não fazer)

- Loops posicionais contínuos por bin (framework Positional Audio, `bubbles`-like
  permanente) — custo sem benefício para evento 1×/2 dias.
- Música/jukebox, voz, ou qualquer cue `Music`/`Ambient` para este evento.
- Override/substituição de cues vanilla em `Data/AudioChanges`.
- Textos falados ou localizáveis em áudio (áudio é não-verbal; i18n continua em
  `i18n/default.json` via `ITranslationHelper` + `showRedMessage`).
- Automação de teste de rede multiplayer (pertence ao Roadmap D1; 004 cobre stubs).
- Reabrir FR-008a (recusa = `cancel`+shake) ou a decisão de baseline 0 neutro.

## 9. Licenciamento (o que pode virar o `.ogg`)

O repo é público — tudo sob `Assets/Audio/` é publicado a cada download:

1. **Gravação original primeiro** (madeira/kalimba/cerâmica tocada pelo autor =
   risco zero e já no brief §5).
2. **Só CC0** se não for original (Freesound CC0, Pixabay Content License,
   synth/SFX próprio). Registrar fonte URL + licença + autor em
   `LivingRoots/Assets/Audio/ATTRIBUTION.md` (arquivo **ainda não existe** —
   verificado 2026-10-05; criar junto com o `.ogg`, não como follow-up).
3. **Nunca**: áudio ripado do jogo (XWB banks), `CC-BY-NC` / `CC-BY-ND` /
   packs "royalty-free mas não CC0", rips de YouTube, downloads "sem licença".
4. Os stubs atuais de 24 bytes não têm conteúdo audível — nenhuma questão de
   autoria; as regras mordem no momento da substituição.

## Fontes

- Stardew Valley Wiki — `Modding:Audio` (Data/AudioChanges, categorias, formato,
  `Game1.playSound` vs `GameLocation.localSound/playSound`, `debug logSounds`).
- Stardew Valley Wiki — `Modding:Machines` (LoadEffects/WorkingEffects/Sounds,
  `ShowNextIndexWhenReady`, `InvalidItemMessage`).
- Stardew Valley Wiki — `Modding:Modder Guide/APIs/Content` e `APIs/Utilities`
  (`AssetRequested`, `Helper.DirectoryPath` + `Path.Combine`).
- Repo: `LivingRoots/Services/CompostingBinService.cs:98,140` (`Ship`);
  `LivingRoots/ModEntry.cs:114-177` (sem `Data/AudioChanges`);
  `LivingRoots/LivingRoots.csproj:26` (Assets copiados, não carregados);
  `specs/001-soil-health-visualization/research-notification-mechanisms.md`
  (HUDMessage/toast como padrão de notificação).
- Soundboard mod (Nexus #34968) e Sound Tweaker (Nexus #14767) como ferramentas de
  audição/debug — não dependências.
