# NovaOS-FNF

A Unity 2022.3 / VRChat-compatible Friday Night Funkin'-inspired game project.

## Current engineering milestone: audio library foundation

The first source milestone adds an Editor-side audio scanner and a runtime audio catalog/player.

### Audio folder setup

Create `Assets/NovaOS/AudioLibrary/` and place audio files anywhere beneath it. The scanner searches recursively.

Supported extensions: WAV, OGG, MP3, AIFF.

Recognized filename suffixes:
- `-Inst`, `-Instrumental` → instrumental track
- `-Voices`, `-Vocals` → vocal track

A plain filename is treated as an instrumental if no explicit instrumental file has been found.

Files in folders named `Menu` or `Music` are indexed as menu music. Files in folders named `SoundEffects` or `SFX` are indexed as sound effects.

Example:

```
Assets/NovaOS/AudioLibrary/
  Tutorial/
    Tutorial-Inst.ogg
    Tutorial-Voices.ogg
  Week1/
    Bopeebo-Inst.ogg
    Bopeebo-Voices.ogg
    Fresh-Inst.ogg
    Fresh-Voices.ogg
  Menu/
    titleTheme.ogg
  SoundEffects/
    confirm.ogg
```

In Unity, run **NovaOS → FNF → Audio Library → Scan and Build Catalog**. The scanner creates or updates `Assets/NovaOS/Generated/NovaAudioCatalog.asset`.

The generated catalog stores Unity AudioClip references. It is not a runtime filesystem browser: VRChat worlds cannot freely scan arbitrary folders on a player's PC. Audio must be imported into the Unity project and included in the world build.

### Important licensing note

Only include audio you have the right to use in the world. Official FNF game assets are separately licensed from the open-source game code and are not automatically cleared for redistribution.

## Development status

This is an incremental game implementation. The audio library foundation is not the complete game. Menu systems, chart/gameplay engine, story/freeplay content, settings, input, results, and VRChat interaction still need to be implemented and tested in Unity.
