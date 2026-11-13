# Official FNF Layout Reference

This document records the upstream source points used to guide the NovaOS-FNF starter scene. The intent is to keep layout decisions traceable to the current official game instead of inventing generic rhythm-game proportions.

## Upstream source references

Repository: https://github.com/FunkinCrew/Funkin

- `source/funkin/ui/mainmenu/MainMenuState.hx`
  - Main menu items are positioned at `FlxG.width / 2`.
  - Vertical item spacing is `160` source pixels.
  - The first item is placed using `(FlxG.height - spacing * (itemCount - 1)) / 2`.
- `source/funkin/play/PlayState.hx`
  - The health bar background is centered horizontally.
  - In upscroll mode its y coordinate is `FlxG.height * 0.9`; downscroll uses `FlxG.height * 0.1`.
  - The fill is inset 4 px from the background on each side.
  - Score text is positioned relative to the health bar background at x + width - 190 and y + 30.
  - The player strumline is on the right half; the opponent strumline is on the left half. Their x/y coordinates use `STRUMLINE_X_OFFSET`, `STRUMLINE_Y_OFFSET`, and `FullScreenScaleMode.gameCutoutSize`, not arbitrary fixed screen percentages.
- `source/funkin/ui/freeplay/FreeplayState.hx`
  - DJ positioning multiplier: `0.44`.
  - Song-list positioning multiplier: `0.75`.
  - Default difficulty-dot group position: `[260, 170]`.
- `source/funkin/ui/story/StoryMenuState.hx`
  - High-score text uses VCR OSD Mono at 32 px near the top-left safe area.
  - Level title is right-aligned near the top-right safe area.
  - Tracklist heading is centered.

## Starter scene implementation

The generated scene uses a 1280x720 reference canvas and converts source-game y coordinates to Unity's bottom-up RectTransform coordinates.

- Main menu: centered horizontally, 160 px vertical spacing, with positions converted from Flixel's top-down coordinate system.
- Gameplay: opponent lanes on the left, player lanes on the right; upscroll health bar centered at 90% of screen height; score and judgement UI placed relative to that HUD.
- Story, Freeplay, Options, Difficulty, and Results screens are separate screen roots so the menu controller can switch between them.

## Accuracy limits and next refinement

The starter scene is an integration scaffold, not a claim of pixel-perfect reproduction. Exact strumline offsets depend on the upstream runtime's safe-area/cutout calculations, note-style dimensions, and selected platform. Character/stage placement depends on each official stage JSON and its prop offsets. Freeplay layout depends on the current song list, selected capsule, display size, and DJ artwork.

Those values should be read from the matching upstream source/data and applied per asset/platform. Do not replace them with one hard-coded layout and call it exact. The next accuracy pass should use the upstream constants and per-stage/per-character offsets directly, then compare Unity captures against screenshots from the same official build and aspect ratio.
