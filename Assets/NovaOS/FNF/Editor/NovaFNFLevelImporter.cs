#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NovaOS.FNF;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UdonSharpEditor;

namespace NovaOS.FNF.Editor
{
    /// <summary>
    /// Imports FNF data/levels JSON files into the Udon Story Mode progression bank.
    /// Visual props and level backgrounds are retained as metadata for future scene work;
    /// week song ordering is immediately usable by the StoryProgression runtime.
    /// </summary>
    public static class NovaFNFLevelImporter
    {
        private const string LevelRoot = "Assets/NovaOS/LevelLibrary";

        [MenuItem("NovaOS/FNF/Story Mode/Import Levels into Selected Progression")]
        public static void ImportLevels()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("NovaOS Story Importer", "Select a GameObject with NovaFNFStoryProgression first.", "OK");
                return;
            }

            NovaFNFStoryProgression progression = selected.GetUdonSharpComponent<NovaFNFStoryProgression>();
            if (progression == null)
            {
                EditorUtility.DisplayDialog("NovaOS Story Importer", "The selected GameObject needs a NovaFNFStoryProgression UdonSharp component.", "OK");
                return;
            }

            NovaFNFChartBank chartBank = selected.GetUdonSharpComponent<NovaFNFChartBank>();
            if (chartBank == null)
                chartBank = FindObjectOfType<NovaFNFChartBank>();

            if (!AssetDatabase.IsValidFolder(LevelRoot))
            {
                EditorUtility.DisplayDialog(
                    "NovaOS Story Importer",
                    "Create this folder first:\n\n" + LevelRoot + "\n\nCopy official data/levels JSON files into it.",
                    "OK");
                return;
            }

            string absoluteRoot = Path.Combine(Directory.GetCurrentDirectory(), LevelRoot);
            string[] files = Directory.GetFiles(absoluteRoot, "*.json", SearchOption.AllDirectories);
            List<LevelRecord> levels = new List<LevelRecord>();
            List<string> warnings = new List<string>();

            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                try
                {
                    string json = File.ReadAllText(file);
                    LevelDefinition data = JsonUtility.FromJson<LevelDefinition>(json);
                    if (data == null || data.songs == null || data.songs.Length == 0)
                    {
                        warnings.Add("Skipped JSON without a songs array: " + ToAssetPath(file));
                        continue;
                    }

                    bool visible = true;
                    if (json.Contains("\"visible\""))
                        visible = data.visible;

                    LevelRecord level = new LevelRecord();
                    level.id = Normalize(Path.GetFileNameWithoutExtension(file));
                    level.title = string.IsNullOrEmpty(data.name) ? level.id : data.name;
                    level.visible = visible;
                    level.background = data.background ?? "";
                    level.titleAsset = data.titleAsset ?? "";
                    level.songs = data.songs;
                    levels.Add(level);
                }
                catch (Exception exception)
                {
                    warnings.Add("Could not read " + ToAssetPath(file) + ": " + exception.Message);
                }
            }

            levels.Sort(CompareLevelRecords);
            if (levels.Count == 0)
            {
                EditorUtility.DisplayDialog("NovaOS Story Importer", "No valid story levels were found.\nWarnings: " + warnings.Count, "OK");
                if (warnings.Count > 0) Debug.LogWarning(string.Join("\n", warnings));
                return;
            }

            List<string> weekIds = new List<string>();
            List<string> weekTitles = new List<string>();
            List<bool> weekVisible = new List<bool>();
            List<int> starts = new List<int>();
            List<int> counts = new List<int>();
            List<string> songIds = new List<string>();
            List<string> variations = new List<string>();
            List<string> difficulties = new List<string>();

            for (int i = 0; i < levels.Count; i++)
            {
                LevelRecord level = levels[i];
                weekIds.Add(level.id);
                weekTitles.Add(level.title);
                weekVisible.Add(level.visible);
                starts.Add(songIds.Count);
                counts.Add(level.songs.Length);

                for (int j = 0; j < level.songs.Length; j++)
                {
                    string songId = Normalize(level.songs[j]);
                    songIds.Add(songId);

                    string variation;
                    string difficulty;
                    FindDefaultChart(chartBank, songId, out variation, out difficulty);
                    variations.Add(variation);
                    difficulties.Add(difficulty);
                }
            }

            SerializedObject serialized = new SerializedObject(progression);
            SetStrings(serialized, "weekIds", weekIds);
            SetStrings(serialized, "weekTitles", weekTitles);
            SetBools(serialized, "weekVisible", weekVisible);
            SetInts(serialized, "weekSongStarts", starts);
            SetInts(serialized, "weekSongCounts", counts);
            SetStrings(serialized, "weekSongIds", songIds);
            SetStrings(serialized, "weekSongVariations", variations);
            SetStrings(serialized, "weekSongDifficulties", difficulties);
            serialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(progression);
            PrefabUtility.RecordPrefabInstancePropertyModifications(progression);
            if (progression.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(progression.gameObject.scene);

            int hiddenCount = 0;
            for (int i = 0; i < weekVisible.Count; i++)
                if (!weekVisible[i]) hiddenCount++;

            string report = "Levels imported: " + levels.Count +
                "\nStory-visible levels: " + (levels.Count - hiddenCount) +
                "\nFreeplay-only levels: " + hiddenCount +
                "\nSongs indexed: " + songIds.Count +
                "\nWarnings: " + warnings.Count;

            Debug.Log("NovaOS Story Mode import completed.\n" + report +
                (warnings.Count > 0 ? "\n\n" + string.Join("\n", warnings) : ""), progression);
            EditorUtility.DisplayDialog("NovaOS Story Importer", report, "Done");
        }

        private static int CompareLevelRecords(LevelRecord left, LevelRecord right)
        {
            return CompareNatural(left.id, right.id);
        }

        private static int CompareNatural(string left, string right)
        {
            int i = 0;
            int j = 0;
            while (i < left.Length && j < right.Length)
            {
                bool leftDigit = char.IsDigit(left[i]);
                bool rightDigit = char.IsDigit(right[j]);
                if (leftDigit && rightDigit)
                {
                    int leftStart = i;
                    int rightStart = j;
                    while (i < left.Length && char.IsDigit(left[i])) i++;
                    while (j < right.Length && char.IsDigit(right[j])) j++;

                    int leftSignificant = leftStart;
                    int rightSignificant = rightStart;
                    while (leftSignificant < i - 1 && left[leftSignificant] == '0') leftSignificant++;
                    while (rightSignificant < j - 1 && right[rightSignificant] == '0') rightSignificant++;

                    int leftDigits = i - leftSignificant;
                    int rightDigits = j - rightSignificant;
                    if (leftDigits != rightDigits) return leftDigits.CompareTo(rightDigits);

                    for (int k = 0; k < leftDigits; k++)
                    {
                        int digitCompare = left[leftSignificant + k].CompareTo(right[rightSignificant + k]);
                        if (digitCompare != 0) return digitCompare;
                    }
                }
                else
                {
                    int charCompare = char.ToLowerInvariant(left[i]).CompareTo(char.ToLowerInvariant(right[j]));
                    if (charCompare != 0) return charCompare;
                    i++;
                    j++;
                }
            }

            return (left.Length - i).CompareTo(right.Length - j);
        }

        private static void FindDefaultChart(NovaFNFChartBank bank, string songId, out string variation, out string difficulty)
        {
            variation = "default";
            difficulty = "normal";
            if (bank == null || bank.songIds == null) return;

            int firstMatch = -1;
            int normalMatch = -1;
            int defaultMatch = -1;

            for (int i = 0; i < bank.songIds.Length; i++)
            {
                if (Normalize(bank.songIds[i]) != songId) continue;
                if (firstMatch < 0) firstMatch = i;

                string currentVariation = bank.variationIds != null && i < bank.variationIds.Length
                    ? Normalize(bank.variationIds[i]) : "default";
                string currentDifficulty = bank.difficultyIds != null && i < bank.difficultyIds.Length
                    ? Normalize(bank.difficultyIds[i]) : "normal";

                if (currentVariation == "default" && defaultMatch < 0) defaultMatch = i;
                if (currentVariation == "default" && currentDifficulty == "normal")
                {
                    normalMatch = i;
                    break;
                }
            }

            int chosen = normalMatch >= 0 ? normalMatch : (defaultMatch >= 0 ? defaultMatch : firstMatch);
            if (chosen < 0) return;

            if (bank.variationIds != null && chosen < bank.variationIds.Length)
                variation = Normalize(bank.variationIds[chosen]);
            if (bank.difficultyIds != null && chosen < bank.difficultyIds.Length)
                difficulty = Normalize(bank.difficultyIds[chosen]);
        }

        private static void SetStrings(SerializedObject serialized, string name, List<string> values)
        {
            SerializedProperty array = serialized.FindProperty(name);
            if (array == null) { Debug.LogError("Story progression is missing field: " + name); return; }
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) array.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        private static void SetInts(SerializedObject serialized, string name, List<int> values)
        {
            SerializedProperty array = serialized.FindProperty(name);
            if (array == null) { Debug.LogError("Story progression is missing field: " + name); return; }
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) array.GetArrayElementAtIndex(i).intValue = values[i];
        }

        private static void SetBools(SerializedObject serialized, string name, List<bool> values)
        {
            SerializedProperty array = serialized.FindProperty(name);
            if (array == null) { Debug.LogError("Story progression is missing field: " + name); return; }
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) array.GetArrayElementAtIndex(i).boolValue = values[i];
        }

        private static string ToAssetPath(string absolutePath)
        {
            string root = Directory.GetCurrentDirectory().Replace('\\', '/') + "/";
            return absolutePath.Replace('\\', '/').Replace(root, "");
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "" :
                value.Trim().ToLowerInvariant().Replace("_", "-").Replace(" ", "-");
        }

        [Serializable]
        private sealed class LevelDefinition
        {
            public string version;
            public string name;
            public string titleAsset;
            public string background;
            public string[] songs;
            public bool visible;
        }

        private sealed class LevelRecord
        {
            public string id;
            public string title;
            public bool visible;
            public string titleAsset;
            public string background;
            public string[] songs;
        }
    }
}
#endif
