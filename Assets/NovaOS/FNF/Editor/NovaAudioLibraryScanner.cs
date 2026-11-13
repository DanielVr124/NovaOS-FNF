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
    public static class NovaAudioLibraryScanner
    {
        private const string AudioRoot = "Assets/NovaOS/AudioLibrary";
        private const string GeneratedFolder = "Assets/NovaOS/Generated";
        private const string CatalogPath = GeneratedFolder + "/NovaAudioCatalog.asset";

        private static readonly string[] InstrumentalTokens = { "inst", "instrumental", "instrumentals" };
        private static readonly string[] VocalTokens = { "voices", "voice", "vocals", "vocal" };

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

                string characterId;
                string songId = StripRoleSuffix(fileName, out AudioRole role, out characterId);
                if (string.IsNullOrEmpty(songId) && role != AudioRole.Unknown)
                    songId = GetSongFolderName(path);

                if (string.IsNullOrEmpty(songId))
                {
                    warnings.Add("Could not identify song name: " + path);
                    continue;
                }

                string normalizedId = NovaAudioCatalog.NormalizeId(songId);
                if (!songMap.TryGetValue(normalizedId, out MutableSong song))
                {
                    song = new MutableSong();
                    song.id = normalizedId;
                    songMap.Add(normalizedId, song);
                }

                if (role == AudioRole.Instrumental)
                {
                    if (song.instrumental == null) song.instrumental = clip;
                    else warnings.Add("Duplicate instrumental for '" + song.id + "': " + path);
                }
                else if (role == AudioRole.Vocals)
                {
                    AddVoiceTrack(song, characterId, clip, path, warnings);
                }
                else
                {
                    // A plain filename is treated as the instrumental by default.
                    if (song.instrumental == null) song.instrumental = clip;
                    else warnings.Add("Extra unclassified audio for '" + song.id + "': " + path);
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
                .Select(s => new NovaAudioCatalog.SongAudio
                {
                    songId = s.id,
                    instrumental = s.instrumental,
                    vocals = s.defaultVocals != null ? s.defaultVocals : s.firstVoice
                }).ToList();

            var voices = new List<NovaAudioCatalog.VoiceAudio>();
            foreach (MutableSong song in songMap.Values)
            {
                if (song.instrumental == null) warnings.Add("Missing instrumental: " + song.id);
                if (song.voices.Count == 0) warnings.Add("Missing vocals: " + song.id);

                foreach (VoiceEntry voice in song.voices)
                {
                    voices.Add(new NovaAudioCatalog.VoiceAudio
                    {
                        songId = song.id,
                        characterId = voice.characterId,
                        clip = voice.clip
                    });
                }
            }

            catalog.EditorReplaceData(songs, voices, menu, effects);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string report = "Songs indexed: " + songs.Count +
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

        private static void AddVoiceTrack(MutableSong song, string characterId, AudioClip clip, string path, List<string> warnings)
        {
            string normalizedCharacter = NovaAudioCatalog.NormalizeId(characterId);
            if (string.IsNullOrEmpty(normalizedCharacter)) normalizedCharacter = "default";

            for (int i = 0; i < song.voices.Count; i++)
            {
                if (song.voices[i].characterId == normalizedCharacter)
                {
                    warnings.Add("Duplicate vocal track for '" + song.id + "' character '" + normalizedCharacter + "': " + path);
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

        private static string StripRoleSuffix(string fileName, out AudioRole role, out string characterId)
        {
            string normalized = fileName.Trim().Replace('_', '-').Replace(' ', '-');
            string lower = normalized.ToLowerInvariant();
            characterId = "default";

            for (int i = 0; i < InstrumentalTokens.Length; i++)
            {
                string token = InstrumentalTokens[i];
                string suffix = "-" + token;
                if (lower == token)
                {
                    role = AudioRole.Instrumental;
                    return "";
                }
                if (lower.EndsWith(suffix, StringComparison.Ordinal))
                {
                    role = AudioRole.Instrumental;
                    return normalized.Substring(0, normalized.Length - suffix.Length);
                }
            }

            // Official FNF song folders can contain Voices-bf.ogg, Voices-pico.ogg, etc.
            // Also support SongName-Voices-bf.ogg.
            for (int i = 0; i < VocalTokens.Length; i++)
            {
                string token = VocalTokens[i];
                string prefix = token + "-";
                string suffix = "-" + token + "-";

                if (lower.StartsWith(prefix, StringComparison.Ordinal))
                {
                    role = AudioRole.Vocals;
                    characterId = normalized.Substring(prefix.Length);
                    return "";
                }

                int suffixIndex = lower.LastIndexOf(suffix, StringComparison.Ordinal);
                if (suffixIndex >= 0 && suffixIndex + suffix.Length < normalized.Length)
                {
                    role = AudioRole.Vocals;
                    characterId = normalized.Substring(suffixIndex + suffix.Length);
                    return normalized.Substring(0, suffixIndex);
                }

                string plainSuffix = "-" + token;
                if (lower == token)
                {
                    role = AudioRole.Vocals;
                    return "";
                }
                if (lower.EndsWith(plainSuffix, StringComparison.Ordinal))
                {
                    role = AudioRole.Vocals;
                    return normalized.Substring(0, normalized.Length - plainSuffix.Length);
                }
            }

            role = AudioRole.Unknown;
            return normalized;
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
