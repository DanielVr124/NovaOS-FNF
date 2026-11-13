# NovaOS-FNF

A Unity 2022.3 LTS / VRChat UdonSharp rhythm-game project. The target is a playable recreation inspired by the **newer official Friday Night Funkin'**, not the older 2020-era menu layout.

This repository is the source workspace. It is **not a finished Unity package yet**. The scripts have been committed, but they still need to be imported, compiled and tested in the actual Unity/VRChat project. The repository does not contain official FNF music, art, character models, stages, or animation assets.

## 1. Audio library: drop in folders and scan

Create:

`Assets/NovaOS/AudioLibrary/`

Copy your audio folders anywhere below this directory. Unity must import the files as AudioClips; the scanner finds them recursively.

Supported Unity audio formats include WAV, OGG, MP3 and AIFF.

### Official FNF-style layout

The scanner understands song folders containing the official role-based filenames:

```
Assets/NovaOS/AudioLibrary/
  songs/
    tutorial/
      Inst.ogg
      Voices-bf.ogg
    bopeebo/
      Inst.ogg
      Voices-bf.ogg
      Voices-dad.ogg
    dadbattle/
      Inst.ogg
      Inst-erect.ogg
      Voices-bf.ogg
      Voices-bf-erect.ogg
      Voices-dad.ogg
      Voices-dad-erect.ogg
  menu/
    titleTheme.ogg
    freeplayTheme.ogg
  soundEffects/
    confirm.ogg
    cancel.ogg
```

It also understands flat names such as `Bopeebo-Inst.ogg`, `Bopeebo-Voices-bf.ogg`, and `Bopeebo-Voices-bf-erect.ogg`.

- `Inst` / `Instrumental` identifies the instrumental.
- `Voices-bf`, `Voices-dad`, and other `Voices-<character>` files are stored as character-specific vocal stems.
- A final suffix such as `-erect` identifies a variation.
- Files under `Menu` or `Music` are indexed as menu tracks.
- Files under `SoundEffects` or `SFX` are indexed as sound effects.
- Missing or duplicate tracks appear in the scan report.

### Generate the audio catalog and VRChat bank

1. Copy audio folders into `Assets/NovaOS/AudioLibrary/`.
2. In Unity, run **NovaOS → FNF → Audio Library → Scan and Build Catalog**.
3. Create a GameObject named `NovaFNF_AudioBank` and add `NovaFNFUdonAudioBank`.
4. Select that GameObject.
5. Run **NovaOS → FNF → Audio Library → Populate Selected VRChat Audio Bank**.
6. Add `NovaFNFUdonAudioPlayer` to a runtime GameObject and assign the populated bank.

The scanner creates or updates `Assets/NovaOS/Generated/NovaAudioCatalog.asset`. The populate tool copies the references into serialized arrays on the UdonSharp bank. The runtime audio player has separate AudioSource fields for instrumental, player vocals, opponent vocals, menu music, secondary menu music (for crossfades), Freeplay preview and sound effects. Player/opponent vocal stems are selected using the character IDs imported from chart metadata; if a separate opponent stem is unavailable, the player does not play the same combined vocal clip twice.

This is an Editor import workflow, not runtime access to a player's PC filesystem. Files must be copied into the Unity project and included in the world build. Anyone receiving the world receives its included audio.

## 2. Chart library: import split chart JSON

Create:

`Assets/NovaOS/ChartLibrary/`

Place split song metadata and chart JSON files under this folder. The importer searches recursively for `*-metadata*.json` files and pairs them with the matching `*-chart*.json` in the same folder. It also accepts a simple `metadata.json` + `chart.json` pair.

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

### Import charts

1. Copy chart folders into `Assets/NovaOS/ChartLibrary/`.
2. Create or select a GameObject with `NovaFNFChartBank`.
3. Select that GameObject.
4. Run **NovaOS → FNF → Charts → Import into Selected VRChat Chart Bank**.

The Editor importer extracts difficulty note arrays, timestamps, directions, sustain lengths, note kinds, scroll speeds, BPM changes, preview intervals, difficulty ratings, character IDs and chart events. It flattens them into arrays for UdonSharp runtime use. Preview values are converted from the chart metadata's millisecond values into Unity seconds.

The event importer preserves each event's kind and serialized value payload. Runtime event behavior is configured by adding `NovaFNFChartEventBinding` components and matching event kinds; this is a generic event-routing layer, not a complete implementation of every official FNF event or scripted cutscene.

## 3. Story Mode level data

Create:

`Assets/NovaOS/LevelLibrary/`

Copy the relevant official `data/levels` JSON files into it. Select the GameObject containing `NovaFNFStoryProgression`, then run:

**NovaOS → FNF → Story Mode → Import Levels into Selected Progression**

The importer reads level names, ordered song lists, visibility, title asset IDs and background IDs. Invisible levels are excluded from the Story week list but remain in the imported data; visual assets and bespoke week presentation still need to be connected in the Unity scene.

## 4. Menu scene wiring

Runtime menu scripts:

- `NovaFNFMenuController` — title, main, Story, Freeplay, difficulty, Options, Controls, Audio, Accessibility, gameplay and results screen navigation; history/back handling; menu music requests and animated screen transitions.
- `NovaFNFMenuAction` — reusable button/action behavior, keyboard highlight and pointer hover hooks.
- `NovaFNFFreeplayListController` — chart-driven song/variation rows, paging, favorites, best-score display and high-score sorting.
- `NovaFNFDifficultyListController` — difficulty choices and imported ratings for the selected song/variation.
- `NovaFNFWeekListController` — visible Story week paging and locked-week display.
- `NovaFNFStoryProgression` — ordered Story week playback and results-to-next-song progression.

Create the screen objects in the scene and assign them to the controller's `screens` array. The default indices are Title 0, Main 1, Story 2, Freeplay 3, Options 4, Controls 5, Audio 6, Accessibility 7, Gameplay 8, Results 9 and Difficulty 10. Keep the index fields in the Inspector aligned with the array.

Add `NovaFNFMenuAction` to each visible selectable row/button, assign its controller and action ID, and populate the menu controller's action arrays (or let the Freeplay/Week/Difficulty list controllers populate their visible slots). Connect each Unity UI Button's OnClick event to `NovaFNFMenuAction.OnButtonPressed`. Pointer hover can call `OnPointerEnter`. Menu item action IDs are documented in the component tooltip.

For menu transitions, assign one Animator per screen in `screenAnimators` and create the configured `Enter` and `Exit` triggers (or change their names in the Inspector). Assign different menu track IDs for each screen category if separate music is available.

The navigation controller provides flow and behavior, not the visual recreation. Menu art, typography, character poses, menu-specific animations, song cards, backgrounds and layout still need to be authored and wired in Unity.

## 5. Gameplay scene wiring

Runtime scripts:

- `NovaFNFChartBank` — flattened chart data.
- `NovaFNFNoteView` — pooled note visuals, lane arrows and sustain-body presentation.
- `NovaFNFGameplayController` — note spawning, four player lanes, opponent auto-notes, Sick/Good/Bad/Shit timing windows, sustain tracking, score, combo, misses, accuracy, health, countdown, pause/retry/exit and results handoff.
- `NovaFNFJudgementPopup` — animated judgement text with optional separate hit-grade sound effects.
- `NovaFNFBeatClock` — BPM-change-aware beat/step callbacks for character/stage animators and camera pulses.
- `NovaFNFChartEventRouter` / `NovaFNFChartEventBinding` — time-based dispatch of imported chart event data to configured scene reactions.
- `NovaFNFUdonAudioPlayer` — song stems, menu crossfades, previews, pause/resume and sound effects.

Create a prewarmed pool of note GameObjects, each with `NovaFNFNoteView`; assign its visual, optional arrow Text/Image and sustain body. Assign eight receptor transforms to represent four player lanes and four opponent lanes. The gameplay controller can be configured with at least four player receptors, but opponent notes need the opponent receptor transforms too.

Assign player/opponent strumline Animators and character Animators. The defaults expect strumline triggers `Confirm` / `Press`, character sing triggers `singLEFT`, `singDOWN`, `singUP`, `singRIGHT`, and beat clock triggers `Dance`, `Beat`, and `Step`. Match those names to the Animator Controllers you create.

The current keyboard test bindings default to Left/Down/Up/Right arrows or A/S/W/D for the four lanes, Up/Down or W/S for menus, Enter/Space to confirm, Escape to go back/pause, and P to pause. `NovaFNFInputBindings` centralizes configurable PC bindings. `NovaFNFControlsController` provides a key-capture UI; connect each remap button to `BeginCapture` with the corresponding binding index (0–16).

For VR pointer/touch lane input, add `NovaFNFLaneInputButton` to each of four lane buttons. Connect pointer-down to `PressLane` and pointer-up to `ReleaseLane`. This is a basic world-space button path; Quest controller mapping and actual Quest playtests are still outstanding.

## 6. Results, options and persistence

- `NovaFNFResultsController` displays the current run, grade, personal bests and new-high-score feedback, and exposes Continue, Retry and Main Menu actions.
- `NovaFNFOptionsController` applies downscroll, scroll speed, input offset, ghost tapping, camera zoom, flashing lights, reduced motion, subtitles and audio volume values.
- `NovaFNFSaveManager` uses VRChat PlayerData after `OnPlayerRestored` to save best scores, accuracy, misses, max combo, favorites, Story unlock count, settings and custom key bindings.

Assign the Save Manager references to the relevant gameplay, results, Story, Freeplay, options and controls components. The persistence path must be validated in the actual world and against VRChat's current PlayerData limits.

## 7. Validate the scene

After the components and references are wired, run:

**NovaOS → FNF → Validation → Validate Current Scene**

The Editor validator reports missing components, unset references and array mismatches. This is a static wiring check only: it does not compile UdonSharp, verify animation assets, prove timing accuracy, or replace an actual PC/Quest playtest.

## Development status

| Milestone | Source status | Still required |
|---|---|---|
| 1. Audio and asset foundation | Scanner, catalog, variation/character-voice bank and runtime player committed. | Unity import test, full folder-layout verification, real asset assignment and playback tests. |
| 2. Menu experience | Screen navigation, animated transition hooks, per-screen music and reusable menu actions committed. | Actual menu scene/art, accurate animation/audio timing, Unity wiring and visual comparison. |
| 3. Song selection and progression | Chart/level importers, paged Freeplay, difficulty list, favorites and Story progression committed. | Import against the exact chosen game data, verify every song/variation/level, finish UI and progression edge cases. |
| 4. Rhythm gameplay | First playable Udon loop, countdown, stems, notes, basic sustains, beat clock, chart-event router and results handoff committed. | Timing/judgement validation, complete note/event behavior, character/stage animations, effects, full song playthroughs and balance. |
| 5. Results and progression | Results UI logic, local records, favorites and persistent week unlocks committed. | Match official grading/scoring, test all save/restore paths and finish records UI. |
| 6. VRChat integration | UdonSharp-oriented runtime, PlayerData persistence and pointer lane hooks committed. | UdonSharp compile, Quest controller design, VR pointer setup, multiplayer isolation checks, performance and PC/Quest world tests. |
| 7. Polish and testing | Not started. | Full integration, bug fixing, complete playthrough testing and release-ready validation. |

**No milestone is being represented as finished merely because its source files exist.** No code in this repository has yet been compiled inside the user's Unity project or tested in a live VRChat world.

## Compatibility and asset notes

- Target: Unity 2022.3 LTS with VRChat SDK Worlds and UdonSharp installed.
- Editor importers and validators live under `Editor/`; they do not run in the VRChat world.
- UdonSharp runtime components use serialized arrays rather than runtime generic collections.
- The repository contains source code, not official FNF music/art. Only import and redistribute assets you have permission to use. Official game code and official game assets have separate licenses.
- VRChat cannot freely scan arbitrary folders on a player's PC at runtime. Imported audio and art must be part of the Unity project and world build.
- This project aims to recreate the play experience, but it is not an official FNF product and has no official affiliation.


## Official asset repository import

The project now includes an Editor importer for the official `FunkinCrew/funkin.assets` repository. It fetches the source into Unity's local `Library` cache and imports audio, charts, level data, images, character/stage data and fonts into local project folders. See [OFFICIAL_ASSET_IMPORT.md](OFFICIAL_ASSET_IMPORT.md) for prerequisites, exact destinations and the post-import catalog steps. Imported proprietary content is excluded from Git; it is not uploaded by the importer. This workflow does not yet automatically convert every official atlas/animation into scene-ready Unity objects.
