#if UNITY_EDITOR
using System.Collections.Generic;
using NovaOS.FNF;
using UnityEditor;
using UnityEngine;
using UdonSharpEditor;

namespace NovaOS.FNF.Editor
{
    public static class NovaFNFUdonAudioBankPopulator
    {
        private const string CatalogPath = "Assets/NovaOS/Generated/NovaAudioCatalog.asset";

        [MenuItem("NovaOS/FNF/Audio Library/Populate Selected VRChat Audio Bank")]
        public static void PopulateSelected()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("NovaOS Audio Library", "Select the GameObject containing NovaFNFUdonAudioBank first.", "OK");
                return;
            }

            NovaFNFUdonAudioBank bank = selected.GetUdonSharpComponent<NovaFNFUdonAudioBank>();
            if (bank == null)
            {
                EditorUtility.DisplayDialog(
                    "NovaOS Audio Library",
                    "The selected GameObject needs the NovaFNFUdonAudioBank UdonSharp component.",
                    "OK");
                return;
            }

            NovaAudioCatalog catalog = AssetDatabase.LoadAssetAtPath<NovaAudioCatalog>(CatalogPath);
            if (catalog == null)
            {
                EditorUtility.DisplayDialog("NovaOS Audio Library", "Build the audio catalog first using Scan and Build Catalog.", "OK");
                return;
            }

            var songIds = new List<string>();
            var songVariationIds = new List<string>();
            var instrumentals = new List<AudioClip>();
            var vocals = new List<AudioClip>();
            for (int i = 0; i < catalog.Songs.Count; i++)
            {
                NovaAudioCatalog.SongAudio item = catalog.Songs[i];
                songIds.Add(NovaAudioCatalog.NormalizeId(item.songId));
                songVariationIds.Add(NovaAudioCatalog.NormalizeId(item.variationId));
                instrumentals.Add(item.instrumental);
                vocals.Add(item.vocals);
            }

            var voiceSongIds = new List<string>();
            var voiceCharacterIds = new List<string>();
            var voiceVariationIds = new List<string>();
            var voiceClips = new List<AudioClip>();
            for (int i = 0; i < catalog.VoiceTracks.Count; i++)
            {
                voiceSongIds.Add(NovaAudioCatalog.NormalizeId(catalog.VoiceTracks[i].songId));
                voiceCharacterIds.Add(NovaAudioCatalog.NormalizeId(catalog.VoiceTracks[i].characterId));
                voiceVariationIds.Add(NovaAudioCatalog.NormalizeId(catalog.VoiceTracks[i].variationId));
                voiceClips.Add(catalog.VoiceTracks[i].clip);
            }

            var menuIds = new List<string>();
            var menuClips = new List<AudioClip>();
            for (int i = 0; i < catalog.MenuMusic.Count; i++)
            {
                menuIds.Add(NovaAudioCatalog.NormalizeId(catalog.MenuMusic[i].id));
                menuClips.Add(catalog.MenuMusic[i].clip);
            }

            var sfxIds = new List<string>();
            var sfxClips = new List<AudioClip>();
            for (int i = 0; i < catalog.SoundEffects.Count; i++)
            {
                sfxIds.Add(NovaAudioCatalog.NormalizeId(catalog.SoundEffects[i].id));
                sfxClips.Add(catalog.SoundEffects[i].clip);
            }

            SerializedObject serialized = new SerializedObject(bank);
            SetStringArray(serialized, "songIds", songIds);
            SetStringArray(serialized, "songVariationIds", songVariationIds);
            SetObjectArray(serialized, "instrumentals", instrumentals);
            SetObjectArray(serialized, "vocals", vocals);
            SetStringArray(serialized, "voiceSongIds", voiceSongIds);
            SetStringArray(serialized, "voiceCharacterIds", voiceCharacterIds);
            SetStringArray(serialized, "voiceVariationIds", voiceVariationIds);
            SetObjectArray(serialized, "voiceClips", voiceClips);
            SetStringArray(serialized, "menuMusicIds", menuIds);
            SetObjectArray(serialized, "menuMusicClips", menuClips);
            SetStringArray(serialized, "soundEffectIds", sfxIds);
            SetObjectArray(serialized, "soundEffectClips", sfxClips);
            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(bank);
            PrefabUtility.RecordPrefabInstancePropertyModifications(bank);
            Debug.Log("Populated VRChat audio bank: " + songIds.Count + " song variations, " + voiceClips.Count + " character vocal tracks, " + menuIds.Count + " menu tracks, " + sfxIds.Count + " sound effects.", bank);
            EditorUtility.DisplayDialog(
                "NovaOS Audio Library",
                "VRChat audio bank populated.\n\nSongs: " + songIds.Count +
                "\nCharacter vocal tracks: " + voiceClips.Count +
                "\nMenu tracks: " + menuIds.Count +
                "\nSound effects: " + sfxIds.Count,
                "Done");
        }

        private static void SetStringArray(SerializedObject serialized, string propertyName, List<string> values)
        {
            SerializedProperty array = serialized.FindProperty(propertyName);
            if (array == null) return;
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                array.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        private static void SetObjectArray(SerializedObject serialized, string propertyName, List<AudioClip> values)
        {
            SerializedProperty array = serialized.FindProperty(propertyName);
            if (array == null) return;
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
#endif
