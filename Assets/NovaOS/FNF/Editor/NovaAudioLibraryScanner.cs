#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NovaOS.FNF;
using UnityEditor;
using UnityEngine;

namespace NovaOS.FNF.Editor
{
    /// <summary>
    /// Recursively scans imported audio clips, including the official FNF
    /// songs/<songId>/Inst[-variation].ogg and Voices-<character>[-variation].ogg layout.
    /// </summary>
    [InitializeOnLoad]
    public static class NovaAudioLibraryScanner
    {
        private const string AutoScanSessionKey = "NovaOS.FNF.AudioAutoScanned";

        static NovaAudioLibraryScanner()
        {
            EditorApplication.delayCall += AutoScanAfterPackageImport;
        }

        private static void AutoScanAfterPackageImport()
        {
            if (SessionState.GetBool(AutoScanSessionKey, false)) return;
            SessionState.SetBool(AutoScanSessionKey, true);
            if (!AssetDatabase.IsValidFolder(AudioRoot)) return;
            if (AssetDatabase.LoadAssetAtPath<NovaAudioCatalog>(CatalogPath) != null) return;
            ScanAndBuild(false);
        }

        private const string AudioRoot = "Assets/NovaOS/AudioLibrary";
        private const string GeneratedFolder = "Assets/NovaOS/Generated";
        private const string CatalogPath = GeneratedFolder + "/NovaAudioCatalog.asset";

        [MenuItem("NovaOS/FNF/Audio Library/Scan and Build Catalog")]
        public static void ScanAndBuild()
        {
            if (!AssetDatabase.IsValidFolder(AudioRoot))
            {
                EditorUtility.DisplayDialog(
                    "NovaOS Audio Library",
                    "Create this folder first:\n\n" + AudioRoot + "\n\nThen place your song audio folders inside it.",
                    "OK");
                return;
            }

            var songMap = new Dictionary<string, MutableSong>(StringComparer.OrdinalIgnoreCase);
            var menu = new List<NovaAudioCatalog.NamedAudio>();
            var effects = new List<NovaAudioCatalog.NamedAudio>();
            var warnings = new List<string>();

            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) continue;

                string fileName = Path.GetFileNameWithoutExtension(path);
                string normalizedPath = path.Replace('\\', '/').ToLowerInvariant();

                if (normalizedPath.Contains("/soundeffects/") || normalizedPath.Contains("/sfx/"))
                {
                    AddNamedAudio(effects, fileName, clip, warnings, "sound effect");
                    continue;
                }

                if (normalizedPath.Contains("/menu/") || normalizedPath.Contains("/music/"))
                {
                    AddNamedAudio(menu, fileName, clip, warnings, "menu music");
                    continue;
                }

                string songId;
                string variationId;
                string characterId;
                AudioRole role;
                ParseTrackName(fileName, path, out songId, out variationId, out characterId, out role);

                if (string.IsNullOrEmpty(songId))
                {
                    warnings.Add("Could not identify song name: " + path);
                    continue;
                }

                songId = NovaAudioCatalog.NormalizeId(songId);
                variationId = NovaAudioCatalog.NormalizeId(variationId);
                if (string.IsNullOrEmpty(variationId)) variationId = "default";

                string mapKey = songId + "|" + variationId;
                if (!songMap.TryGetValue(mapKey, out MutableSong song))
                {
                    song = new MutableSong();
                    song.id = songId;
                    song.variationId = variationId;
                    songMap.Add(mapKey, song);
                }

                if (role == AudioRole.Instrumental)
                {
                    if (song.instrumental == null) song.instrumental = clip;
                    else warnings.Add("Duplicate instrumental for '" + song.id + "' variation '" + song.variationId + "': " + path);
                }
                else if (role == AudioRole.Vocals)
                {
                    AddVoiceTrack(song, characterId, clip, path, warnings);
                }
            }

            EnsureFolder(GeneratedFolder);
            NovaAudioCatalog catalog = AssetDatabase.LoadAssetAtPath<NovaAudioCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<NovaAudioCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var songs = songMap.Values
                .OrderBy(s => s.id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(s => s.variationId, StringComparer.OrdinalIgnoreCase)
                .Select(s => new NovaAudioCatalog.SongAudio
                {
                    songId = s.id,
                    variationId = s.variationId,
                    instrumental = s.instrumental,
                    vocals = s.defaultVocals != null ? s.defaultVocals : s.firstVoice
                }).ToList();

            var voices = new List<NovaAudioCatalog.VoiceAudio>();
            foreach (MutableSong song in songMap.Values)
            {
                if (song.instrumental == null)
                    warnings.Add("Missing instrumental: " + song.id + " (" + song.variationId + ")");
                if (song.voices.Count == 0)
                    warnings.Add("Missing vocals: " + song.id + " (" + song.variationId + ")");

                foreach (VoiceEntry voice in song.voices)
                {
                    voices.Add(new NovaAudioCatalog.VoiceAudio
                    {
                        songId = song.id,
                        variationId = song.variationId,
                        characterId = voice.characterId,
                        clip = voice.clip
                    });
                }
            }

            catalog.EditorReplaceData(songs, voices, menu, effects);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int distinctSongs = songMap.Values.Select(s => s.id).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            int erectInstrumentals = songs.Count(s => s.variationId == "erect" && s.instrumental != null);
            int erectSongs = songs.Count(s => s.variationId == "erect");
            string report = "Songs indexed: " + distinctSongs +
                "\nSong variations indexed: " + songs.Count +
                "\nErect remix instrumentals: " + erectInstrumentals + " / " + erectSongs +
                "\nCharacter vocal tracks: " + voices.Count +
                "\nMenu tracks: " + menu.Count +
                "\nSound effects: " + effects.Count +
                "\nWarnings: " + warnings.Count;

            if (warnings.Count > 0)
                Debug.LogWarning("NovaOS audio scan completed.\n" + report + "\n\n" + string.Join("\n", warnings));
            else
                Debug.Log("NovaOS audio scan completed.\n" + report);

            EditorUtility.DisplayDialog("NovaOS Audio Library", report + "\n\nCatalog: " + CatalogPath, "Done");
        }

        private static void ParseTrackName(
            string fileName,
            string assetPath,
            out string songId,
            out string variationId,
            out string characterId,
            out AudioRole role)
        {
            string normalized = fileName.Trim().Replace('_', '-').Replace(' ', '-');
            string lower = normalized.ToLowerInvariant();
            songId = "";
            variationId = "default";
            characterId = "default";
            role = AudioRole.Unknown;

            // Official folder layout: Inst.ogg or Inst-erect.ogg inside <songId>.
            if (lower == "inst" || lower == "instrumental" || lower == "instrumentals")
            {
                role = AudioRole.Instrumental;
                songId = GetSongFolderName(assetPath);
                return;
            }

            if (lower.StartsWith("inst-", StringComparison.Ordinal) ||
                lower.StartsWith("instrumental-", StringComparison.Ordinal))
            {
                role = AudioRole.Instrumental;
                int dash = normalized.IndexOf('-');
                variationId = normalized.Substring(dash + 1);
                if (lower.StartsWith("instrumental-", StringComparison.Ordinal))
                    variationId = normalized.Substring("instrumental-".Length);
                songId = GetSongFolderName(assetPath);
                return;
            }

            // Official folder layout: Voices-bf.ogg or Voices-bf-erect.ogg.
            if (lower == "voices" || lower == "voice" || lower == "vocals" || lower == "vocal")
            {
                role = AudioRole.Vocals;
                songId = GetSongFolderName(assetPath);
                return;
            }

            if (lower.StartsWith("voices-", StringComparison.Ordinal) ||
                lower.StartsWith("voice-", StringComparison.Ordinal) ||
                lower.StartsWith("vocals-", StringComparison.Ordinal) ||
                lower.StartsWith("vocal-", StringComparison.Ordinal))
            {
                role = AudioRole.Vocals;
                int dash = normalized.IndexOf('-');
                string remainder = normalized.Substring(dash + 1);
                SplitCharacterVariation(remainder, out characterId, out variationId);
                songId = GetSongFolderName(assetPath);
                return;
            }

            // Flat or named-file layout: songId-Inst[-variation].ogg.
            string[] instrumentalMarkers = { "-instrumental-", "-inst-" };
            for (int i = 0; i < instrumentalMarkers.Length; i++)
            {
                int marker = lower.LastIndexOf(instrumentalMarkers[i], StringComparison.Ordinal);
                if (marker >= 0 && marker + instrumentalMarkers[i].Length < normalized.Length)
                {
                    role = AudioRole.Instrumental;
                    songId = normalized.Substring(0, marker);
                    variationId = normalized.Substring(marker + instrumentalMarkers[i].Length);
                    return;
                }
            }

            if (lower.EndsWith("-instrumental", StringComparison.Ordinal) ||
                lower.EndsWith("-instrumentals", StringComparison.Ordinal) ||
                lower.EndsWith("-inst", StringComparison.Ordinal))
            {
                role = AudioRole.Instrumental;
                int marker = lower.LastIndexOf("-inst", StringComparison.Ordinal);
                if (marker < 0) marker = lower.LastIndexOf("-instrumental", StringComparison.Ordinal);
                songId = normalized.Substring(0, marker);
                return;
            }

            // Flat layout: songId-Voices[-character][-variation].ogg.
            string[] vocalMarkers = { "-voices-", "-voice-", "-vocals-", "-vocal-" };
            for (int i = 0; i < vocalMarkers.Length; i++)
            {
                int marker = lower.LastIndexOf(vocalMarkers[i], StringComparison.Ordinal);
                if (marker >= 0)
                {
                    role = AudioRole.Vocals;
                    songId = normalized.Substring(0, marker);
                    string remainder = normalized.Substring(marker + vocalMarkers[i].Length);
                    if (!string.IsNullOrEmpty(remainder))
                        SplitCharacterVariation(remainder, out characterId, out variationId);
                    return;
                }
            }

            if (lower.EndsWith("-voices", StringComparison.Ordinal) ||
                lower.EndsWith("-voice", StringComparison.Ordinal) ||
                lower.EndsWith("-vocals", StringComparison.Ordinal) ||
                lower.EndsWith("-vocal", StringComparison.Ordinal))
            {
                role = AudioRole.Vocals;
                int marker = lower.LastIndexOf("-voices", StringComparison.Ordinal);
                if (marker < 0) marker = lower.LastIndexOf("-voice", StringComparison.Ordinal);
                if (marker < 0) marker = lower.LastIndexOf("-vocals", StringComparison.Ordinal);
                if (marker < 0) marker = lower.LastIndexOf("-vocal", StringComparison.Ordinal);
                songId = normalized.Substring(0, marker);
                return;
            }

            // Plain files still work as instrumentals. Their filename supplies the song ID.
            role = AudioRole.Instrumental;
            songId = normalized;
        }

        private static void SplitCharacterVariation(string value, out string characterId, out string variationId)
        {
            characterId = value;
            variationId = "default";
            int lastDash = value.LastIndexOf('-');
            if (lastDash > 0 && lastDash < value.Length - 1)
            {
                characterId = value.Substring(0, lastDash);
                variationId = value.Substring(lastDash + 1);
            }
        }

        private static void AddVoiceTrack(MutableSong song, string characterId, AudioClip clip, string path, List<string> warnings)
        {
            string normalizedCharacter = NovaAudioCatalog.NormalizeId(characterId);
            if (string.IsNullOrEmpty(normalizedCharacter)) normalizedCharacter = "default";

            for (int i = 0; i < song.voices.Count; i++)
            {
                if (song.voices[i].characterId == normalizedCharacter)
                {
                    warnings.Add("Duplicate vocal track for '" + song.id + "' variation '" +
                        song.variationId + "' character '" + normalizedCharacter + "': " + path);
                    return;
                }
            }

            song.voices.Add(new VoiceEntry { characterId = normalizedCharacter, clip = clip });
            if (normalizedCharacter == "default") song.defaultVocals = clip;
            if (song.firstVoice == null) song.firstVoice = clip;
        }

        private static void AddNamedAudio(List<NovaAudioCatalog.NamedAudio> target, string fileName, AudioClip clip, List<string> warnings, string category)
        {
            string id = NovaAudioCatalog.NormalizeId(fileName);
            for (int i = 0; i < target.Count; i++)
            {
                if (NovaAudioCatalog.NormalizeId(target[i].id) == id)
                {
                    warnings.Add("Duplicate " + category + " ID '" + id + "': " + clip.name);
                    return;
                }
            }
            target.Add(new NovaAudioCatalog.NamedAudio { id = id, clip = clip });
        }

        private static string GetSongFolderName(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath);
            while (!string.IsNullOrEmpty(directory))
            {
                string folder = Path.GetFileName(directory);
                string normalized = folder.ToLowerInvariant();
                if (normalized != "audio" && normalized != "song" &&
                    normalized != "songs" && normalized != "assets" &&
                    normalized != "music" && normalized != "audio-library")
                    return folder;

                directory = Path.GetDirectoryName(directory);
            }
            return "";
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private enum AudioRole { Unknown, Instrumental, Vocals }

        private sealed class MutableSong
        {
            public string id;
            public string variationId;
            public AudioClip instrumental;
            public AudioClip defaultVocals;
            public AudioClip firstVoice;
            public List<VoiceEntry> voices = new List<VoiceEntry>();
        }

        private sealed class VoiceEntry
        {
            public string characterId;
            public AudioClip clip;
        }
    }
}
#endif
