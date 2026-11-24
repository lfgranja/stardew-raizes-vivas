# Asset Briefing: `composting_bin_processing.ogg` (custom composting audio)

**Feature**: `specs/004-playable-compost-loop/` · **Date**: 2026-10-05 · **Status**: not started
**Priority**: P2 — enhancement · **Type**: audio cue registration (`Data/AudioChanges` + real `.ogg` data)
**Blocks**: nothing — spec 004 explicitly ships with *"default/native placeholders"* for sound
(spec.md §Assumptions, *"Artwork and sound remain default/native placeholders"*).
This briefing exists so the placeholder can be replaced without re-researching the pipeline.

 Companion file `LivingRoots/Assets/Audio/composting_bin_ready.ogg` (completion ping) is covered
 in §2 but briefed separately if/when it is produced — the wiring is identical, only the
 cue ID, category trade-off, and sound-design notes differ.

 Sources (verified 2026-10-05): [Modding:Audio](https://stardewvalleywiki.com/Modding:Audio)
 (`oldid=193491`, §§Data format, Category list, Audio file format, Playing sounds, Add/Edit Audio),
 [Modding:Machines](https://stardewvalleywiki.com/Modding:Machines) (`oldid=191352`,
 `Audio & visuals`: `LoadEffects`/`WorkingEffects`/`WorkingEffectChance`),
 [Content API](https://stardewvalleywiki.com/Modding:Modder_Guide/APIs/Content) (edit-a-game-asset).

---

## 1. What exists today

### 1.1 The `.ogg` files are 24-byte stubs, not audio

```
$ od -c LivingRoots/Assets/Audio/composting_bin_processing.ogg
O g g S \0 002 \0 \0 \0 \0 \0 \0 \0 \0 \0 \0 \0 \0 \0 \0 \0 \0 \0 \0
```

Both `composting_bin_processing.ogg` and `composting_bin_ready.ogg` are **24 bytes**:
a bare `OggS` capture pattern with zero-length segments — a valid-looking header wrapping
**no Vorbis stream, no samples, no duration**. Any player decodes them as silence (or rejects
them outright). They were committed as placeholders (`ea29c7f feat(assets): add placeholder
textures and audio cues for compost and bin`).

### 1.2 They are copied to output but never loaded

`LivingRoots/LivingRoots.csproj:26-28` copies the whole tree:

```xml
<Content Include="Assets\**\*.*">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

But **no C# references them**: there is no `ModContent.Load`, no `TitleContainer.OpenStream`,
no `Data/AudioChanges` edit anywhere in `LivingRoots/`. `grep` for
`playSound|AudioChanges|composting_bin|processing\.ogg` hits only the vanilla cues below.
The files are dead weight in the shipped zip today.

### 1.3 Current soundscape: 100 % vanilla cues

| Site | Code | Cue | Vanilla meaning (wiki track list) |
|------|------|-----|-----------------------------------|
| `Constants.cs:72` (`CompostCraftingSound`) | recipe craft SFX | `axe` (#1, higher-pitched `axchop`; plays when wooden items such as tappers are placed) | wood knock on placement |
| `CompostingBinService.cs:98` (`CollectCompost`) | collect feedback | `Ship` (#96, items dropping into a wooden box — shipping-bin deposit) | deposit thunk on harvest |
| `CompostingBinService.cs:140` (`ProcessDayStart` maturation) | ready notice | `Ship` | same cue, fired at day start (no player context) |
| `CompostApplicationService.cs:25,33,41,52` | all four refusal paths | `cancel` (#353, short shaker — standard "action refused") | refusal on invalid target |

> Line-note (2026-10-05 re-verification): earlier drafts of this briefing cited
> `CompostingBinService.cs:93/:128`; the call sites have since shifted to `:98` and `:140`.
> Earlier drafts also described `Ship` as a "whoop" — the whoop is `backpackIN` (#133);
> `Ship` is the wooden-box drop. Dosage: the fix is semantic, not structural — the ready
> signal still reads as "shipped/sold something", which is why the custom ready ping (§2, #2)
> should replace it at collect time.

`ModEntry.OnAssetRequested` (`ModEntry.cs:114-177`, subscribed at `ModEntry.cs:31`) already edits `Data/CraftingRecipes`,
`Data/Objects`, and `Data/Machines` — the audio edit slots into this same handler (see §4).
The `Data/Machines` branch (`:152-176`) currently registers only `OutputRules` for
`(BC)LivingRoots.CompostingBin`; no `LoadEffects`/`WorkingEffects` exist yet, so §4.3 is
greenfield inside an established branch, not a refactor.

### 1.4 Prior-spec history that constrains the design (do not re-litigate)

- `specs/002/.../spec.md:152` (**FR-020**, the origin of this asset): intermittent subtle ambient
  sound (low bubbling/composting hum, every 2–3 in-game hours while processing) + one distinct
  ready chime. Intermittent-by-requirement — a looping drone would violate FR-020, which is why
  §4.3 keeps `Looped: false` and lets `WorkingEffectChance` throttle.
- `specs/002/.../research.md:213-224` (§10 Audio Cue Registration) pre-selected the candidates:
  processing → `Game1.playSound("crafting")` *or custom bubbling cue*; ready →
  `Game1.playSound("Ship")` *or custom ding*; invalid → `Game1.playSound("cancel")`.
  Note the wiki itself uses `Game1.playSound("crafting")` as its canonical UI-sound example —
  but our processing cue is world-anchored, not UI, so it must graduate to the machine-effect
  path (§4.3) rather than stay a `Game1.playSound` call.
- `specs/002/.../contracts/machine-interaction.md:91`: "intermittent ambient sound every 2–3
  in-game hours (subtle bubbling)" + ready visual switch. Audio and visual are one contract.
- `specs/002/.../tasks.md:131` (**T034**, marked `[x]`): promised placeholder `.ogg` files
  **and** `AssetRequested` registration per FR-020. Status discrepancy (verified 2026-10-05):
  the files landed (`ea29c7f`) but **no `Data/AudioChanges` registration exists anywhere in
  `LivingRoots/`** — the second half of T034 is unimplemented. This briefing's §4.2 is that
  missing half. Do not mark audio "done" again on files alone; the §7.2 in-game gate
  (cue name in `logSounds`) is the definition of done.

### 1.5 Spec constraints that bound this briefing (do not redesign)

- **FR-008a**: invalid item drop-in on a bin stays **vanilla machine rejection** (audio `cancel`
  cue + machine shake, no text overlay). A custom cue MUST NOT be wired to the rejection path.
- **FR-001a**: the bin is a **native game machine** — its processing SFX should preferably come
  from `Data/Machines` `LoadEffects` / `WorkingEffects`, not from bespoke `Game1.playSound`
  calls in service code.
- Spec assumption: *"Artwork and sound remain default/native placeholders"* — the vanilla cues
  in §1.3 are the compliant baseline. Custom audio is polish, never a gate for US1–US3.

---

## 2. What must be delivered

Two cue registrations in `Data/AudioChanges`, backed by two real `.ogg` files:

| # | Cue ID (proposed) | File | Category | Loop | Purpose |
|---|-------------------|------|----------|------|---------|
| 1 | `LivingRoots_CompostingBin_Processing` | `Assets/Audio/composting_bin_processing.ogg` | `Sound` | `false` (one-shot, chance-gated by machine) | earthy bubbling while a batch works — this briefing's subject |
| 2 | `LivingRoots_CompostingBin_Ready` | `Assets/Audio/composting_bin_ready.ogg` | `Sound` | `false` | short completion ping replacing `Ship` at collect time — same wiring, separate asset task |

ID convention: flat `ModId_Topic_Variant` with underscores. `Data/AudioChanges` keys are plain
unique string IDs (dots are legal but underscores avoid any interaction with token parsing in
`Target`/`Entries` strings and match the existing `LivingRoots.InputId`-style `modData` keys).
The dictionary key **must equal** the entry's `ID` field, and playback code **must** reference
a `ModConstants` string, never a literal (cf. `CompostCraftingSound` precedent).

`ModConstants` additions (names only — values are the cue IDs above):

```csharp
public const string CompostingBinProcessingSound = "LivingRoots_CompostingBin_Processing";
public const string CompostingBinReadySound = "LivingRoots_CompostingBin_Ready";
```

---

## 3. Technical spec — file format

Per [Modding:Audio §Audio file format](https://stardewvalleywiki.com/Modding:Audio): the game
accepts **only `.ogg` (Vorbis) and `.wav`**. `.ogg` is required here (compressed, small SFX;
`.wav` is for tiny UI blips and would bloat the mod zip).

| Property | Requirement | Rationale |
|----------|-------------|-----------|
| Codec / container | **Ogg Vorbis**, `.ogg` extension | only compressed format the pipeline supports |
| Channels | **Stereo** | wiki: mono renders quieter/muffled next to game audio |
| Sample rate | **44100 Hz** | matches game bank; avoids resample artifacts |
| Bitrate | ~96–128 kbps VBR (quality q4–q5) | transparent for lo-fi ambience; keeps each SFX **< 200 KB** |
| Duration (processing) | **2–4 s one-shot**, decay tail included | machine re-triggers via `WorkingEffectChance`; a long file fights the gate |
| Duration (ready) | **0.5–1.0 s ping** | must read instantly as "done", not as a jingle |
| Peak level | −12 to −6 dBFS, no clipping | game mixes SFX hot; leave headroom for stacked bins |
| Loop metadata | **none** (`Looped: false`) | looping is a cue property, not an encoder flag; do not bake loop points |
| File size budget | each file **< 500 KB**, stretch goal < 200 KB | wiki music guidance is 2–4 MB *per track*; SFX must be an order of magnitude smaller since they ship in every download |

Production notes:

- Author at 48 kHz / 24-bit, downsample + dither to 44.1 kHz / 16-bit at export (Audacity:
  `Tracks > Resample`; `File > Export Audio > Ogg Vorbis`, quality slider 4–5).
- Verify with `file` (must report `Ogg data, Vorbis audio`) and `ffprobe` (stream duration
  > 0, channels == 2). A 24-byte file like today's stubs fails both checks — add that exact
  assertion to the verification gate (§7.1).
- Never commit `.mp3` intermediates; convert upstream sources to `.wav`/`.ogg` before editing.

---

## 4. Technical spec — wiring

### 4.1 Route decision: C# `AssetRequested` (preferred), Content Patcher (fallback)

Spec 004's spike settles item/machine registration toward **programmatic `AssetRequested` in
C# with no new content-patcher dependency**. Audio follows the same route: it is one more
`else if` branch in the existing `ModEntry.OnAssetRequested`. Content Patcher `EditData` on
`Data/AudioChanges` (wiki example uses `"FilePaths": ["{{AbsoluteFilePath: assets/...}}"]`)
is documented here only as the fallback if the spike ever reverses the no-CP decision —
it would require adding a CP dependency to `manifest.json` for audio alone, which is
disproportionate.

### 4.2 Registering the cues (C# route)

`Data/AudioChanges` is a `string → AudioCueData` dictionary. Per the wiki's
[Add/Edit Audio](https://stardewvalleywiki.com/Modding:Audio#Manage_audio_in_C.23) section,
the expected edit type is `IDictionary<string, AudioCueData>` and file paths are **absolute
disk paths** (not asset names), built with `Path.Combine`:

```csharp
// ModEntry.cs — new branch in OnAssetRequested, beside Data/Machines
else if (e.NameWithoutLocale.IsEquivalentTo("Data/AudioChanges"))
{
    e.Edit(asset =>
    {
        var data = asset.AsDictionary<string, StardewValley.GameData.AudioCueData>().Data;
        string dir = this.Helper.DirectoryPath; // absolute mod folder at runtime
        data["LivingRoots_CompostingBin_Processing"] = new StardewValley.GameData.AudioCueData
        {
            ID = "LivingRoots_CompostingBin_Processing",
            Category = "Sound",          // follows the in-game sound slider, NOT music
            FilePaths = new List<string>
            {
                Path.Combine(dir, "Assets", "Audio", "composting_bin_processing.ogg"),
            },
            StreamedVorbis = false,      // short SFX: cache in memory, cheap replay
            Looped = false,
            UseReverb = false,
        };
        // ... same shape for LivingRoots_CompostingBin_Ready
    });
}
```

Field rationale (all per wiki §Data format):

- `Category: "Sound"` — the cue must obey the player's **sound** volume slider. `Ambient`
  is tempting (the wiki lists *"machine whirring"* as an Ambient example) but the bin cue is
  an event SFX tied to player actions, not a location bed; `Sound` also keeps it audible for
  players who mute ambience. `Default` is explicitly "unused, should generally be avoided".
- `StreamedVorbis: false` — wiki: `false` = fully cached, replayable any number of times
  with no extra cost; `true` multiplies memory/perf per concurrent play and is for long/rare
  cues. A 2–4 s SFX triggered per bin per day is the textbook `false` case.
- Single-entry `FilePaths` — the pipeline picks a random entry per play; one file = deterministic.
  Add variants later (e.g. `processing_a/b/c.ogg`) purely by appending paths, no code change.
- Session permanence caveat: overrides apply **permanently for the session even if the asset
  is later edited to remove them**. During development, changing or deleting a cue requires a
  full game restart — `InvalidateCache("Data/AudioChanges")` re-runs edits but does not
  unload the old cue. Note this in the test plan, not in code.

No `content.json` is needed for this route. `manifest.json` is untouched (no new dependency,
no `Content Patches` entry). The `.csproj` `Assets\**\*.*` copy rule already ships the files.

### 4.3 Playing the cue from the native machine (preferred over `Game1.playSound`)

Because the bin is a native machine (FR-001a), its sounds should ride the machine's own
effect system so timing, position, and multiplayer fan-out are handled by the game:

`Data/Machines` → `LoadEffects` (fired when input is loaded) and `WorkingEffects` (fired
probabilistically while working), each entry supporting `Sounds: [{ Id, Delay }]`, `ShakeDuration`,
`Frames`, `TemporarySprites` ([Modding:Machines §Audio & visuals](https://stardewvalleywiki.com/Modding:Machines)).
`WorkingEffectChance` (default **0.33**) gates `WorkingEffects` per day-start/clock-tick —
this is the built-in anti-annoyance throttle for exactly our use case:

```csharp
// inside the Data/Machines edit for (BC)LivingRoots.CompostingBin
data[binId].LoadEffects = new List<MachineEffects>
{
    new() { Sounds = new List<MachineSound> { new() { Id = ModConstants.CompostingBinProcessingSound } } },
};
data[binId].WorkingEffects = new List<MachineEffects>
{
    new() { Sounds = new List<MachineSound> { new() { Id = ModConstants.CompostingBinProcessingSound } } },
};
// WorkingEffectChance left at default 0.33 — do NOT set 1.0 (see §6).
```

Consequences for service code:

- `CompostingBinService.ProcessDayStart` (`CompostingBinService.cs:128`) currently calls
  `Game1.playSound("Ship")` on maturation. Once the machine owns its effects, **delete that
  call** — a service-layer sound fired at day start has no player/location context and will
  double up with the machine effect. Same review for `CollectCompost` (`:93`): keep exactly
  one ready signal — either the machine harvest path or one `GameLocation.playSound` call,
  never both.
- `Game1.playSound` (current player only, no position, no multiplayer sync — *"mainly used
  for UI and menu sounds"*) is the **wrong** API for world-anchored bin audio. If any manual
  playback survives (e.g. the ready ping at harvest), use `GameLocation.playSound(cue, tile,
  pitch)` (all players in location, distance-attenuated) or `.localSound(cue, tile)` so
  FR-018/FR-019 (no single-player-only design; first-action-wins with audible feedback for
  both players) hold. Pitch range is 0–2400 in steps of 100 per half-step, 1200 = default —
  a ±100–200 shift per bin is the cheap anti-phasing trick when several bins sit adjacent.
  `DelayedAction.playSoundAfterDelay` is available for sequenced cues (e.g. tok → blip for the
  ready ping, one call per layer with different delays).
- Probe rule: the game's `OutputMethod` delegate receives a `probe` flag meaning *"no
  animations/sounds should play"* — any custom machine C# added later MUST respect it.

### 4.4 What stays vanilla (explicit non-changes)

| Path | Sound | Why untouched |
|------|-------|---------------|
| Invalid item drop-in | `cancel` + shake | FR-008a mandates vanilla rejection; custom audio here would violate the spec |
| Crafting the bin recipe | `axe` (`CompostCraftingSound`) | crafting SFX, unrelated to processing |
| Refusal HUD messages | `cancel` in `CompostApplicationService` | FR-008a text-message paths, out of audio scope |

---

## 5. Licensing — what may ship in these files

The mod is a **public** repository; anything under `Assets/Audio/` is published to every
downloader. Rules, in priority order:

1. **Original recording first.** A self-recorded compost/soil/bubble foley (hands in soil,
   water poured through compost, jar-bubble) is zero-risk and on-brief (§6). A phone mic at
   close range + high-pass + pitch-down is sufficient source material.
2. **CC0 only** if not original. Accept: Freesound CC0, Pixabay Content License audio,
   self-made synth/SFX. Record the source URL + license + author in
   `LivingRoots/Assets/Audio/ATTRIBUTION.md` (new file, one entry per cue) at the same time
   the `.ogg` lands — not as a follow-up.
3. **Never**: ripped game audio (Stardew's XWB banks, other games), `CC-BY-NC` / `CC-BY-ND`
   / "royalty-free but not CC0" packs (NC breaks downstream packaging; ND breaks editing),
   YouTube rips, or "no license stated" downloads. When in doubt, re-record.
4. The current 24-byte stubs carry no authorship issue (no audible content) — replacing them
   with real audio is the moment these rules bite.
5. `LivingRoots/Assets/Audio/ATTRIBUTION.md` does **not** exist yet (verified 2026-10-05:
   the directory holds only the two stub `.ogg` files; no `ATTRIBUTION*` anywhere in the
   repo outside `node_modules`). Creating it is part of the asset task, not a follow-up.

---

## 6. Sound-design direction — composting ambience that does not irritate

Reference point: vanilla **`bubbles`** (played by Kegs, Casks, Oil Makers, Slime Egg-Press).
Players already read "wet organic process working" from that texture. Our processing cue
should sit in the same family — low, wet, earthy — but drier and more granular than a keg,
so a farm with bins and kegs does not produce two identical voices.

Design targets for `composting_bin_processing.ogg`:

- **Character**: soft granular bubbling + faint soil-shift grit, low-mid centered
  (≈200–800 Hz energy), gentle attack, natural decay tail. No hiss, no high whistle, no
  metallic resonance, no vocal-like formants.
- **Anti-annoyance budget** (the cue fires per bin, per day, potentially several bins):
  keep it **quiet by design** (peak −12 dBFS), short (2–4 s), and let `WorkingEffectChance`
  (0.33) do the throttling — do NOT compensate with `Looped: true` drones or by raising
  `WorkingEffectChance` to 1.0. A compost bin is background farm life, not an alarm.
- **Ready ping** (`composting_bin_ready.ogg`): single soft wooden "tok" + faint rising blip,
  clearly distinct from `Ship` (which today means "sold something") so collecting compost
  stops feeling like a shipment. Must read at low volume over rain/night ambience.
- **Do not design**: music beds, melodies, animal sounds, voice, or anything > 4 s. Those
  belong to other briefings (none exist yet — do not freelance them here).

---

## 7. Verification

### 7.1 File gate (CI-able, no game needed)

```bash
cd /home/luis/development/stardew-raizes-vivas
f=file LivingRoots/Assets/Audio/composting_bin_processing.ogg
[ "$(stat -c%s LivingRoots/Assets/Audio/composting_bin_processing.ogg)" -gt 10000 ] \
  && file LivingRoots/Assets/Audio/composting_bin_processing.ogg | grep -q "Ogg data" \
  && ffprobe -v error -show_entries stream=codec_name,channels,sample_rate,duration \
    -of default=noprint_wrappers=1 LivingRoots/Assets/Audio/composting_bin_processing.ogg
```

Expected: size > 10 KB (today's 24-byte stub fails here by three orders of magnitude),
`Ogg data, Vorbis audio`, `channels=2`, `sample_rate=44100`, `duration` in [2, 4].

### 7.2 In-game gate (authoritative — manual playtest, maintainer machine)

Authoring shortcut: the [Soundboard](https://www.nexusmods.com/stardewvalley/mods/34968)
mod (Nexus 34968, menu on F3) lists every cue by category with name, duration, loop flag,
and whether a mod altered it, and plays any cue on click — use it to A/B the custom cue
against vanilla `bubbles`/`Ship` for loudness and character without sleeping days in-game.
It does not replace the gate below (registration + machine timing must still be proven live).

1. Launch mod; SMAPI log shows **no red** `Data/AudioChanges` edit error and no missing-file
   warning for `Assets/Audio/*.ogg`.
2. SMAPI console `debug logSounds`, then load a bin and sleep 2 days: the custom processing
   cue name appears in the log at load time and probabilistically on day ticks; `Ship` no
   longer appears from `ProcessDayStart`.
3. Standing next to the bin vs. across the map: volume attenuates with distance (proves the
   machine-effect/positional path, not a global `Game1.playSound`).
4. Game options: sound slider to 0 silences the cue; music slider does not (proves
   `Category: Sound`).
5. Full restart, then remove the `.ogg` file and relaunch: game still loads, bin still works,
   sanitized warning only (proves §8 fallback). Restore the file afterwards.

### 7.3 Automated-test note

Audio registration is **not unit-testable** in `LivingRoots.Tests`: cue loading needs a booted
content pipeline and real files, and the 003 briefings already established that content
assets are verified in-game, not in the suite. The only test-adjacent assertion worth adding
is a constants check that playback sites reference `ModConstants.CompostingBinProcessingSound`
/ `CompostingBinReadySound` rather than string literals — a `grep` in review, not a test.

---

## 8. Fallback — behaviour when the cue is absent or broken

Layers, outermost first:

1. **Missing/corrupt `.ogg` at load**: the `Data/AudioChanges` edit MUST guard with
   `File.Exists` on the combined path and skip registration with a sanitized log
   (`LogLevel.Warn`, no paths/exception text — FR-015) naming only the cue ID. The mod then
   behaves exactly as today (vanilla cues), per FR-014 (no feature may block loading).
2. **Cue registered but file undecodable**: same posture — engine logs its own error; our
   edit-time `File.Exists` cannot catch this, so the §7.1 `ffprobe` gate is the real defence.
   Never add a redundant `try/catch` around `Game1.playSound` to paper over a bad file.
3. **Playback constants**: keep the vanilla cue IDs (`Ship`, `cancel`, `axe`) reachable —
   either as retained constants or as the explicit fallback values in the machine-data edit
   when the custom file is absent. Deleting the old sounds from the codebase in the same PR
   that adds untested custom audio removes the only proven fallback.
4. **Multiplayer**: no extra fallback needed — machine effects and `GameLocation.playSound`
   are location-scoped by the engine; farmhands hear bins in their own location only, which
   is correct behaviour, not a bug.
5. **Session permanence** (§4.2): a broken cue registered once stays registered until game
   restart. If a playtest poisons the session, restart — do not chase it with cache
   invalidation.

---

## 9. Out of scope

- `composting_bin_ready.ogg` sound design beyond §2/§6 rows (separate asset task, same wiring).
- Any change to rejection/crafting/refusal sounds (§4.4 table) — FR-008a forbids it.
- Music, ambient location beds, footsteps, voice, UI theme — no cue IDs outside the two in §2.
- Replacing or editing vanilla cues (`bubbles`, `Ship`, `cancel`, `axe`) — additive cues only.
- A Content Patcher `content.json` route unless the 004 spike reverses the no-CP decision.
- `i18n` for audio (cues have no text), economy/balance, sprites — covered by sibling briefings.
- Unit tests for audio loading (§7.3) — in-game gate is authoritative.
