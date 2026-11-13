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
        private static readonly string[] AudioExtensions = { ".wav", ".ogg", ".mp3", ".aiff", ".aif" };

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
                    effects.Add(new NovaAudioCatalog.NamedAudio { id = NovaAudioCatalog.NormalizeId(fileName), clip = clip });
                    continue;
                }

                if (normalizedPath.Contains("/menu/") || normalizedPath.Contains("/music/"))
                {
                    menu.Add(new NovaAudioCatalog.NamedAudio { id = NovaAudioCatalog.NormalizeId(fileName), clip = clip });
                    continue;
                }

                string songId = StripRoleSuffix(fileName, out AudioRole role);
                if (string.IsNullOrEmpty(songId))
                {
                    warnings.Add("Could not identify song name: " + path);
                    continue;
                }

                if (!songMap.TryGetValue(songId, out MutableSong song))
                {
                    song = new MutableSong();
                    song.id = songId;
                    songMap.Add(songId, song);
                }

                if (role == AudioRole.Instrumental) song.instrumental = clip;
                else if (role == AudioRole.Vocals) song.vocals = clip;
                else
                {
                    // A plain audio filename is treated as the instrumental by default.
                    if (song.instrumental == null) song.instrumental = clip;
                    else warnings.Add("Extra unclassified audio for '" + songId + "': " + path);
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
                    songId = NovaAudioCatalog.NormalizeId(s.id),
                    instrumental = s.instrumental,
                    vocals = s.vocals
                }).ToList();

            foreach (MutableSong song in songMap.Values)
            {
                if (song.instrumental == null) warnings.Add("Missing instrumental: " + song.id);
            }

            catalog.EditorReplaceData(songs, menu, effects);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string report = "Songs indexed: " + songs.Count +
                "\nMenu tracks: " + menu.Count +
                "\nSound effects: " + effects.Count +
                "\nWarnings: " + warnings.Count;

            if (warnings.Count > 0)
                Debug.LogWarning("NovaOS audio scan completed.\n" + report + "\n\n" + string.Join("\n", warnings));
            else
                Debug.Log("NovaOS audio scan completed.\n" + report);

            EditorUtility.DisplayDialog("NovaOS Audio Library", report + "\n\nCatalog: " + CatalogPath, "Done");
        }

        private static string StripRoleSuffix(string fileName, out AudioRole role)
        {
            string normalized = fileName.Trim().Replace('_', '-').Replace(' ', '-');
            string lower = normalized.ToLowerInvariant();
            foreach (string token in InstrumentalTokens)
            {
                string suffix = "-" + token;
                if (lower.EndsWith(suffix, StringComparison.Ordinal))
                {
                    role = AudioRole.Instrumental;
                    return normalized.Substring(0, normalized.Length - suffix.Length);
                }
            }

            foreach (string token in VocalTokens)
            {
                string suffix = "-" + token;
                if (lower.EndsWith(suffix, StringComparison.Ordinal))
                {
                    role = AudioRole.Vocals;
                    return normalized.Substring(0, normalized.Length - suffix.Length);
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
            public AudioClip vocals;
        }
    }
}
#endif
