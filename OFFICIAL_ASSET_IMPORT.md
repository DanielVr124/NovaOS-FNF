# Importing the official FNF asset repository

NovaOS-FNF now includes an Editor tool that fetches the official asset repository directly from GitHub. It does not require a Steam installation or manual uploads of individual tracks.

## Before running it

- Open the NovaOS-FNF Unity project on Windows/macOS/Linux.
- Install Git and make sure the `git` command is available in your system PATH.
- Make sure Unity can access GitHub.
- Use the import only for a project/world and sharing arrangement for which you have the required permissions. The official asset repository states that its creative content is proprietary and may not be publicly distributed.

## Run the importer

In Unity, choose:

**NovaOS → FNF → Official Assets → Fetch and Import Official Asset Repository**

Confirm the prompt. The importer clones or updates:

`FunkinCrew/funkin.assets`

inside Unity's `Library/NovaOS/FNF/funkin.assets` cache, then copies supported source folders into the project.

## What it imports

| Official source | Local Unity destination | Purpose |
|---|---|---|
| `songs/` | `Assets/NovaOS/AudioLibrary/songs/` | Instrumentals and character vocal stems |
| `preload/music/` | `Assets/NovaOS/AudioLibrary/music/` | Menu and other music |
| `preload/sounds/` | `Assets/NovaOS/AudioLibrary/SoundEffects/` | Menu/game sound effects |
| `preload/data/songs/` | `Assets/NovaOS/ChartLibrary/songs/` | Official song metadata and chart JSON |
| `preload/data/levels/` | `Assets/NovaOS/LevelLibrary/` | Story level definitions |
| `preload/images/` | `Assets/NovaOS/FNF/ImportedOfficialAssets/images/` | Original image atlases and UI artwork |
| `preload/data/` | `Assets/NovaOS/FNF/ImportedOfficialAssets/data/` | Character, stage, dialogue and UI data |
| `fonts/` | `Assets/NovaOS/FNF/ImportedOfficialAssets/fonts/` | Font files |

For audio, the importer prefers OGG when an OGG and MP3 with the same relative name both exist, avoiding duplicate catalog entries. MP3 is retained when no OGG counterpart exists. Other source files are copied without conversion so their metadata and atlas sidecars remain together.

## After import

1. Run **NovaOS → FNF → Audio Library → Scan and Build Catalog**.
2. Select the GameObject containing `NovaFNFChartBank`, then run **NovaOS → FNF → Charts → Import into Selected VRChat Chart Bank**.
3. Select the GameObject containing `NovaFNFStoryProgression`, then run **NovaOS → FNF → Story Mode → Import Levels into Selected Progression**.
4. Populate the scene's audio bank using **NovaOS → FNF → Audio Library → Populate Selected VRChat Audio Bank**.

The importer does not automatically build Unity scenes, convert every FNF animation/atlas format into Unity Animator Controllers, or assign visual references to menu and gameplay objects. Those are separate integration steps. The imported files are source resources; the existing UdonSharp systems still need scene wiring and testing.

## Git and world-build behavior

The project's `.gitignore` excludes the imported audio, chart, level and visual asset directories. This prevents the proprietary content from being accidentally committed to the NovaOS-FNF source repository. The imported files remain in the local Unity project and can be included in a local world build. Ignoring them in Git does **not** remove them from a Unity build or make sharing the world automatically authorized.

The importer downloads files to the developer's machine. It does not upload the proprietary assets to NovaOS-FNF or to GitHub.
