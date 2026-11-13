# NovaOS-FNF

A Unity 2022.3 / VRChat UdonSharp rhythm-game project targeting the newer official Friday Night Funkin' structure and feel.

This repository is the source workspace. It is not a finished Unity package yet, and scripts still need to be imported, compiled and tested in the actual VRChat project.

## Audio library

Create:

`Assets/NovaOS/AudioLibrary/`

Place audio files anywhere below this folder. The Editor scanner recursively finds imported Unity AudioClips.

Supported formats: WAV, OGG, MP3, AIFF.

Recognized track names:
- `SongName-Inst.ogg`, `SongName-Instrumental.ogg` → instrumental
- `SongName-Voices.ogg`, `SongName-Vocals.ogg` → vocals
- `Inst.ogg` or `Voices.ogg` inside a folder named after the song → uses that parent song folder
- Files in directories named `Menu` or `Music` → menu music
- Files in directories named `SoundEffects` or `SFX` → sound effects

Example supported layouts:

```
Assets/NovaOS/AudioLibrary/
  Tutorial/
    Tutorial-Inst.ogg
    Tutorial-Voices.ogg
  Week1/
    Bopeebo-Inst.ogg
    Bopeebo-Voices.ogg
  Songs/
    Fresh/
      Inst.ogg
      Voices.ogg
  Menu/
    titleTheme.ogg
  SoundEffects/
    confirm.ogg
```

### Build the catalog and populate the VRChat bank

1. Put the imported audio files under `Assets/NovaOS/AudioLibrary/`.
2. In Unity, run **NovaOS → FNF → Audio Library → Scan and Build Catalog**.
3. Create a scene GameObject named `NovaFNF_AudioBank` and add the `NovaFNFUdonAudioBank` UdonSharp component.
4. Select that GameObject.
5. Run **NovaOS → FNF → Audio Library → Populate Selected VRChat Audio Bank**.
6. Add `NovaFNFUdonAudioPlayer` to a separate scene GameObject. Assign the audio bank and four AudioSources (instrumental, vocals, menu music, sound effects).

The catalog scanner creates `Assets/NovaOS/Generated/NovaAudioCatalog.asset`. The populate tool copies the discovered AudioClip references into arrays on the selected UdonSharp component, which is the format used by the VRChat runtime scripts.

This is an Editor import workflow, not arbitrary runtime disk access. VRChat worlds cannot freely browse a player's local folders after the world has been built.

## Chart library

Create:

`Assets/NovaOS/ChartLibrary/`

Place the official split song metadata and chart JSON files in their song folders. The importer searches recursively for `*-metadata*.json` files and pairs them with the matching `*-chart*.json` file in the same directory. It also supports a simple `metadata.json` + `chart.json` pair within a song folder.

Example:

```
Assets/NovaOS/ChartLibrary/
  songs/
    bopeebo/
      bopeebo-metadata.json
      bopeebo-chart.json
    dadbattle/
      dadbattle-metadata.json
      dadbattle-chart.json
      dadbattle-metadata-erect.json
      dadbattle-chart-erect.json
```

### Import chart data

1. Copy chart folders under `Assets/NovaOS/ChartLibrary/`.
2. Create a scene GameObject named `NovaFNF_ChartBank` and add the `NovaFNFChartBank` UdonSharp component.
3. Select that GameObject.
4. Run **NovaOS → FNF → Charts → Import into Selected VRChat Chart Bank**.

The importer reads each chart JSON's difficulty note arrays, note timestamps/directions/sustain lengths, scroll speeds, song metadata and BPM changes. It flattens all imported difficulties into arrays for UdonSharp. The importer currently focuses on note data and BPM metadata; scripted events, special note behavior and alternate character/stage logic remain to be implemented.

## Menu runtime foundation

Scripts:
- `NovaFNFMenuController` — title/main/story/freeplay/options/controls/audio/accessibility/gameplay/results screen switching, screen history, keyboard navigation and gameplay handoff.
- `NovaFNFMenuAction` — reusable menu item action. Wire each Unity UI Button's OnClick to `OnButtonPressed`.
- `NovaFNFGameplayController` — receives song selection from the menu controller.

In the scene, create one GameObject for each screen and assign them in order in the controller's `screens` array. Keep the configured screen index fields consistent with that array. Add `NovaFNFMenuAction` to each selectable item, assign its controller and action ID, then populate the corresponding action array on the menu controller. Action IDs are documented in the component Tooltip.

Keyboard menu navigation: Up/Down (or W/S), Enter/Space to confirm, Escape to go back. While playing, Escape pauses.

The code provides navigation and handoff only; accurate FNF artwork, character presentation, menu-specific animation, song preview cards, and final screen layouts still need to be authored in the Unity scene.

## Gameplay runtime foundation

Scripts:
- `NovaFNFChartBank` — imported flattened chart data.
- `NovaFNFNoteView` — pooled note visual with lane/timing/sustain state.
- `NovaFNFGameplayController` — 4-lane keyboard input, note spawning, opponent auto-notes, basic sustain tracking, judgement, score/combo/misses/accuracy/health, pause/resume/restart/exit and results handoff.
- `NovaFNFUdonAudioPlayer` — synchronized-start instrumental/vocal playback, pause/resume, menu music and SFX references.

The gameplay controller needs eight receptor transforms (four player lanes and four opponent lanes) and a pre-created pool of note visuals. This avoids runtime allocations for every note and is a better fit for VRChat performance.

Current desktop test keys: Left/Down/Up/Right arrows or A/S/W/D. Actual Quest controller note input, haptics, chart events, character animation, camera effects, input calibration and multiplayer-specific features are still outstanding.

## Development status

| Milestone | Current status |
|---|---|
| 1. Audio and asset foundation | Source committed: recursive folder scan, catalog generation, Udon audio bank/player. Needs Unity compile and import test. |
| 2. Menu experience | Navigation controller and reusable menu action source committed. Scene visuals/animations and Unity wiring remain. |
| 3. Song selection and progression | Chart JSON importer and flattened chart bank source committed. Dynamic Freeplay list, difficulty UI, story-week progression and save data remain. |
| 4. Rhythm gameplay | First Udon gameplay loop committed. Sustain accuracy, event handling, characters, effects, and deeper timing validation remain. |
| 5. Results and progression | Basic results handoff exists; full grading, records and persistence remain. |
| 6. VRChat integration | UdonSharp-oriented runtime sources exist. Quest input mapping, world networking policy, and Quest performance tests remain. |
| 7. Polish and testing | Not started. Requires integration and actual Unity/VRChat playtesting. |

## Compatibility and asset notes

- Unity target: 2022.3 LTS.
- Runtime design target: VRChat UdonSharp. Editor scanners are ordinary Unity Editor C# and are not included in the world runtime.
- UdonSharp runtime scripts deliberately use arrays rather than `List<T>` or dictionaries.
- The repository contains source code, not the official FNF music/art package.
- Only add assets you have permission to use and redistribute in the world. Official game code and official game assets have separate licenses.
- No script in this repository has yet been compiled inside the user's Unity project or tested in a live VRChat world. Treat the current code as an engineering work-in-progress until those checks are performed.
