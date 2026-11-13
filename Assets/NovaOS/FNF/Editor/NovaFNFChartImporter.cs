#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Imports official FNF split metadata/chart JSON files into the selected
    /// Udon chart-bank component. The importer is Editor-only; the resulting
    /// flattened arrays are ordinary serialized Udon fields at runtime.
    /// </summary>
    public static class NovaFNFChartImporter
    {
        private const string ChartRoot = "Assets/NovaOS/ChartLibrary";

        [MenuItem("NovaOS/FNF/Charts/Import into Selected VRChat Chart Bank")]
        public static void ImportIntoSelectedBank()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog("NovaOS Chart Importer", "Select the GameObject containing NovaFNFChartBank first.", "OK");
                return;
            }

            NovaFNFChartBank bank = selected.GetUdonSharpComponent<NovaFNFChartBank>();
            if (bank == null)
            {
                EditorUtility.DisplayDialog("NovaOS Chart Importer", "The selected GameObject needs the NovaFNFChartBank UdonSharp component.", "OK");
                return;
            }

            string absoluteRoot = Path.Combine(Directory.GetCurrentDirectory(), ChartRoot);
            if (!Directory.Exists(absoluteRoot))
            {
                EditorUtility.DisplayDialog(
                    "NovaOS Chart Importer",
                    "Create this folder and place official FNF song chart folders inside it:\n\n" + ChartRoot,
                    "OK");
                return;
            }

            List<ChartRecord> charts = new List<ChartRecord>();
            List<string> warnings = new List<string>();
            string[] metadataFiles = Directory.GetFiles(absoluteRoot, "*metadata*.json", SearchOption.AllDirectories);

            for (int i = 0; i < metadataFiles.Length; i++)
            {
                string metadataPath = metadataFiles[i];
                try
                {
                    string chartPath = GetChartPath(metadataPath);
                    if (!File.Exists(chartPath))
                    {
                        warnings.Add("Missing matching chart JSON: " + ToAssetPath(chartPath));
                        continue;
                    }

                    Dictionary<string, object> metadata = AsObject(JsonParser.Parse(File.ReadAllText(metadataPath)));
                    Dictionary<string, object> chart = AsObject(JsonParser.Parse(File.ReadAllText(chartPath)));
                    if (metadata == null || chart == null)
                    {
                        warnings.Add("Invalid metadata/chart JSON: " + ToAssetPath(metadataPath));
                        continue;
                    }

                    string songId;
                    string variationId;
                    GetSongAndVariation(metadataPath, out songId, out variationId);
                    string songName = GetString(metadata, "songName", songId);
                    string artist = GetString(metadata, "artist", "");
                    Dictionary<string, object> playData = AsObject(GetValue(metadata, "playData"));
                    Dictionary<string, object> characters = AsObject(GetValue(playData, "characters"));
                    Dictionary<string, object> ratings = AsObject(GetValue(playData, "ratings"));
                    string playerCharacter = Normalize(GetString(characters, "player", "bf"));
                    string opponentCharacter = Normalize(GetString(characters, "opponent", "dad"));
                    string girlfriendCharacter = Normalize(GetString(characters, "girlfriend", "gf"));
                    float[] bpmTimes;
                    float[] bpmValues;
                    ReadBpmChanges(metadata, out bpmTimes, out bpmValues);
                    float baseBpm = bpmValues.Length > 0 ? bpmValues[0] : 100f;

                    Dictionary<string, object> notesByDifficulty = AsObject(GetValue(chart, "notes"));
                    if (notesByDifficulty == null)
                    {
                        warnings.Add("No difficulty notes object in: " + ToAssetPath(chartPath));
                        continue;
                    }

                    Dictionary<string, object> scrollSpeeds = AsObject(GetValue(chart, "scrollSpeed"));
                    foreach (KeyValuePair<string, object> difficultyPair in notesByDifficulty)
                    {
                        List<object> rawNotes = AsArray(difficultyPair.Value);
                        if (rawNotes == null) continue;

                        ChartRecord record = new ChartRecord();
                        record.songId = Normalize(songId);
                        record.variationId = Normalize(variationId);
                        record.difficultyId = Normalize(difficultyPair.Key);
                        record.difficultyRating = (int)GetNumber(ratings, difficultyPair.Key, 0f);
                        record.key = record.songId + "|" + record.variationId + "|" + record.difficultyId;
                        record.songName = songName;
                        record.artist = artist;
                        record.playerCharacterId = playerCharacter;
                        record.opponentCharacterId = opponentCharacter;
                        record.girlfriendCharacterId = girlfriendCharacter;
                        record.baseBpm = baseBpm;
                        record.scrollSpeed = GetNumber(scrollSpeeds, difficultyPair.Key,
                            GetNumber(scrollSpeeds, "default", 1f));
                        record.previewStartSeconds = GetNumber(playData, "previewStart", 0f) / 1000f;
                        record.previewEndSeconds = GetNumber(playData, "previewEnd", 15000f) / 1000f;
                        record.bpmTimes.AddRange(bpmTimes);
                        record.bpmValues.AddRange(bpmValues);

                        List<object> rawEvents = AsArray(GetValue(chart, "events"));
                        if (rawEvents != null)
                        {
                            for (int eventIndex = 0; eventIndex < rawEvents.Count; eventIndex++)
                            {
                                Dictionary<string, object> rawEvent = AsObject(rawEvents[eventIndex]);
                                if (rawEvent == null) continue;
                                float eventTime = GetNumber(rawEvent, "t", -1f);
                                string eventKind = GetString(rawEvent, "e", "");
                                if (eventTime < 0f || string.IsNullOrEmpty(eventKind)) continue;

                                record.eventTimes.Add(eventTime);
                                record.eventKinds.Add(eventKind);
                                record.eventValuesJson.Add(SerializeJson(GetValue(rawEvent, "v")));
                            }
                        }
                        record.SortEvents();

                        for (int noteIndex = 0; noteIndex < rawNotes.Count; noteIndex++)
                        {
                            Dictionary<string, object> rawNote = AsObject(rawNotes[noteIndex]);
                            if (rawNote == null) continue;
                            float time = GetNumber(rawNote, "t", -1f);
                            int direction = (int)GetNumber(rawNote, "d", -1f);
                            float sustain = GetNumber(rawNote, "l", 0f);
                            string kind = GetString(rawNote, "k", "");
                            if (time < 0f || direction < 0 || direction > 7) continue;
                            record.noteTimes.Add(time);
                            record.noteDirections.Add(direction);
                            record.sustainLengths.Add(Mathf.Max(0f, sustain));
                            record.noteKinds.Add(kind);
                        }

                        record.SortNotes();
                        charts.Add(record);
                    }
                }
                catch (Exception exception)
                {
                    warnings.Add("Could not import " + ToAssetPath(metadataPath) + ": " + exception.Message);
                }
            }

            charts = charts.OrderBy(c => c.songId).ThenBy(c => c.variationId).ThenBy(c => c.difficultyId).ToList();
            if (charts.Count == 0)
            {
                EditorUtility.DisplayDialog("NovaOS Chart Importer", "No usable chart difficulties were found.\n\nWarnings: " + warnings.Count, "OK");
                if (warnings.Count > 0) Debug.LogWarning(string.Join("\n", warnings));
                return;
            }

            PopulateBank(bank, charts);
            EditorUtility.SetDirty(bank);
            PrefabUtility.RecordPrefabInstancePropertyModifications(bank);
            if (bank.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(bank.gameObject.scene);

            int totalNotes = 0;
            int erectCharts = 0;
            int nightmareCharts = 0;
            int erectVariationCharts = 0;
            for (int i = 0; i < charts.Count; i++)
            {
                totalNotes += charts[i].noteTimes.Count;
                if (charts[i].variationId == "erect") erectVariationCharts++;
                if (charts[i].difficultyId == "erect") erectCharts++;
                if (charts[i].difficultyId == "nightmare") nightmareCharts++;
            }
            string report = "Chart difficulties imported: " + charts.Count +
                "\nNotes indexed: " + totalNotes +
                "\nErect variation charts: " + erectVariationCharts +
                "\nErect difficulty charts: " + erectCharts +
                "\nNightmare difficulty charts: " + nightmareCharts +
                "\nWarnings: " + warnings.Count +
                "\n\nChart Library: " + ChartRoot;

            Debug.Log("NovaOS chart import completed.\n" + report +
                (warnings.Count > 0 ? "\n\n" + string.Join("\n", warnings) : ""), bank);
            EditorUtility.DisplayDialog("NovaOS Chart Importer", report, "Done");
        }

        private static void PopulateBank(NovaFNFChartBank bank, List<ChartRecord> charts)
        {
            List<string> chartKeys = new List<string>();
            List<string> songIds = new List<string>();
            List<string> variationIds = new List<string>();
            List<string> difficultyIds = new List<string>();
            List<int> difficultyRatings = new List<int>();
            List<string> songNames = new List<string>();
            List<string> artists = new List<string>();
            List<string> playerCharacterIds = new List<string>();
            List<string> opponentCharacterIds = new List<string>();
            List<string> girlfriendCharacterIds = new List<string>();
            List<float> baseBpms = new List<float>();
            List<float> scrollSpeeds = new List<float>();
            List<float> previewStarts = new List<float>();
            List<float> previewEnds = new List<float>();
            List<int> noteStarts = new List<int>();
            List<int> noteCounts = new List<int>();
            List<int> bpmStarts = new List<int>();
            List<int> bpmCounts = new List<int>();
            List<float> allNoteTimes = new List<float>();
            List<int> allDirections = new List<int>();
            List<float> allSustains = new List<float>();
            List<string> allKinds = new List<string>();
            List<float> allBpmTimes = new List<float>();
            List<float> allBpmValues = new List<float>();
            List<int> eventStarts = new List<int>();
            List<int> eventCounts = new List<int>();
            List<float> allEventTimes = new List<float>();
            List<string> allEventKinds = new List<string>();
            List<string> allEventValues = new List<string>();

            for (int i = 0; i < charts.Count; i++)
            {
                ChartRecord chart = charts[i];
                chartKeys.Add(chart.key);
                songIds.Add(chart.songId);
                variationIds.Add(chart.variationId);
                difficultyIds.Add(chart.difficultyId);
                difficultyRatings.Add(chart.difficultyRating);
                songNames.Add(chart.songName);
                artists.Add(chart.artist);
                playerCharacterIds.Add(chart.playerCharacterId);
                opponentCharacterIds.Add(chart.opponentCharacterId);
                girlfriendCharacterIds.Add(chart.girlfriendCharacterId);
                baseBpms.Add(chart.baseBpm);
                scrollSpeeds.Add(chart.scrollSpeed);
                previewStarts.Add(chart.previewStartSeconds);
                previewEnds.Add(chart.previewEndSeconds);

                noteStarts.Add(allNoteTimes.Count);
                noteCounts.Add(chart.noteTimes.Count);
                allNoteTimes.AddRange(chart.noteTimes);
                allDirections.AddRange(chart.noteDirections);
                allSustains.AddRange(chart.sustainLengths);
                allKinds.AddRange(chart.noteKinds);

                bpmStarts.Add(allBpmTimes.Count);
                bpmCounts.Add(chart.bpmTimes.Count);
                allBpmTimes.AddRange(chart.bpmTimes);
                allBpmValues.AddRange(chart.bpmValues);

                eventStarts.Add(allEventTimes.Count);
                eventCounts.Add(chart.eventTimes.Count);
                allEventTimes.AddRange(chart.eventTimes);
                allEventKinds.AddRange(chart.eventKinds);
                allEventValues.AddRange(chart.eventValuesJson);
            }

            SerializedObject serialized = new SerializedObject(bank);
            SetStrings(serialized, "chartKeys", chartKeys);
            SetStrings(serialized, "songIds", songIds);
            SetStrings(serialized, "variationIds", variationIds);
            SetStrings(serialized, "difficultyIds", difficultyIds);
            SetInts(serialized, "difficultyRatings", difficultyRatings);
            SetStrings(serialized, "songNames", songNames);
            SetStrings(serialized, "artists", artists);
            SetStrings(serialized, "playerCharacterIds", playerCharacterIds);
            SetStrings(serialized, "opponentCharacterIds", opponentCharacterIds);
            SetStrings(serialized, "girlfriendCharacterIds", girlfriendCharacterIds);
            SetFloats(serialized, "baseBpms", baseBpms);
            SetFloats(serialized, "scrollSpeeds", scrollSpeeds);
            SetFloats(serialized, "previewStartSeconds", previewStarts);
            SetFloats(serialized, "previewEndSeconds", previewEnds);
            SetInts(serialized, "noteStarts", noteStarts);
            SetInts(serialized, "noteCounts", noteCounts);
            SetInts(serialized, "bpmStarts", bpmStarts);
            SetInts(serialized, "bpmCounts", bpmCounts);
            SetFloats(serialized, "noteTimesMs", allNoteTimes);
            SetInts(serialized, "noteDirections", allDirections);
            SetFloats(serialized, "sustainLengthsMs", allSustains);
            SetStrings(serialized, "noteKinds", allKinds);
            SetFloats(serialized, "bpmChangeTimesMs", allBpmTimes);
            SetFloats(serialized, "bpmValues", allBpmValues);
            SetInts(serialized, "eventStarts", eventStarts);
            SetInts(serialized, "eventCounts", eventCounts);
            SetFloats(serialized, "eventTimesMs", allEventTimes);
            SetStrings(serialized, "eventKinds", allEventKinds);
            SetStrings(serialized, "eventValuesJson", allEventValues);
            serialized.ApplyModifiedProperties();
        }

        private static void SetStrings(SerializedObject serialized, string name, List<string> values)
        {
            SerializedProperty array = serialized.FindProperty(name);
            if (array == null) { Debug.LogError("Chart bank is missing field: " + name); return; }
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) array.GetArrayElementAtIndex(i).stringValue = values[i];
        }

        private static void SetFloats(SerializedObject serialized, string name, List<float> values)
        {
            SerializedProperty array = serialized.FindProperty(name);
            if (array == null) { Debug.LogError("Chart bank is missing field: " + name); return; }
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) array.GetArrayElementAtIndex(i).floatValue = values[i];
        }

        private static void SetInts(SerializedObject serialized, string name, List<int> values)
        {
            SerializedProperty array = serialized.FindProperty(name);
            if (array == null) { Debug.LogError("Chart bank is missing field: " + name); return; }
            array.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) array.GetArrayElementAtIndex(i).intValue = values[i];
        }

        private static void ReadBpmChanges(Dictionary<string, object> metadata, out float[] times, out float[] bpms)
        {
            List<float> resultTimes = new List<float>();
            List<float> resultBpms = new List<float>();
            List<object> changes = AsArray(GetValue(metadata, "timeChanges"));
            if (changes != null)
            {
                for (int i = 0; i < changes.Count; i++)
                {
                    Dictionary<string, object> change = AsObject(changes[i]);
                    if (change == null) continue;
                    resultTimes.Add(GetNumber(change, "t", 0f));
                    resultBpms.Add(GetNumber(change, "bpm", 100f));
                }
            }

            if (resultTimes.Count == 0)
            {
                resultTimes.Add(0f);
                resultBpms.Add(100f);
            }

            times = resultTimes.ToArray();
            bpms = resultBpms.ToArray();
        }

        private static string GetChartPath(string metadataPath)
        {
            string file = Path.GetFileName(metadataPath);
            int marker = file.IndexOf("-metadata", StringComparison.OrdinalIgnoreCase);
            string chartFile;
            if (marker >= 0)
                chartFile = file.Substring(0, marker) + "-chart" + file.Substring(marker + "-metadata".Length);
            else
                chartFile = "chart.json";
            return Path.Combine(Path.GetDirectoryName(metadataPath), chartFile);
        }

        private static void GetSongAndVariation(string metadataPath, out string songId, out string variationId)
        {
            string file = Path.GetFileNameWithoutExtension(metadataPath);
            int marker = file.IndexOf("-metadata", StringComparison.OrdinalIgnoreCase);
            if (marker >= 0)
            {
                songId = file.Substring(0, marker);
                variationId = file.Substring(marker + "-metadata".Length).Trim('-');
            }
            else
            {
                songId = Path.GetFileName(Path.GetDirectoryName(metadataPath));
                variationId = "default";
            }

            if (string.IsNullOrEmpty(songId))
                songId = Path.GetFileName(Path.GetDirectoryName(metadataPath));
            if (string.IsNullOrEmpty(variationId))
                variationId = "default";
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

        private static object GetValue(Dictionary<string, object> dict, string key)
        {
            if (dict != null && dict.TryGetValue(key, out object value)) return value;
            return null;
        }

        private static string GetString(Dictionary<string, object> dict, string key, string fallback)
        {
            object value = GetValue(dict, key);
            return value == null ? fallback : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static float GetNumber(Dictionary<string, object> dict, string key, float fallback)
        {
            object value = GetValue(dict, key);
            if (value == null) return fallback;
            try { return Convert.ToSingle(value, CultureInfo.InvariantCulture); }
            catch { return fallback; }
        }

        private static string SerializeJson(object value)
        {
            if (value == null) return "null";
            if (value is string) return "\"" + EscapeJson((string)value) + "\"";
            if (value is bool) return (bool)value ? "true" : "false";
            if (value is double || value is float || value is int || value is long || value is decimal)
                return Convert.ToString(value, CultureInfo.InvariantCulture);

            Dictionary<string, object> dictionary = AsObject(value);
            if (dictionary != null)
            {
                List<string> parts = new List<string>();
                foreach (KeyValuePair<string, object> pair in dictionary)
                    parts.Add("\"" + EscapeJson(pair.Key) + "\":" + SerializeJson(pair.Value));
                return "{" + string.Join(",", parts.ToArray()) + "}";
            }

            List<object> array = AsArray(value);
            if (array != null)
            {
                List<string> parts = new List<string>();
                for (int i = 0; i < array.Count; i++)
                    parts.Add(SerializeJson(array[i]));
                return "[" + string.Join(",", parts.ToArray()) + "]";
            }

            return "null";
        }

        private static string EscapeJson(string value)
        {
            if (value == null) return "";
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("\r", "\\r").Replace("\n", "\\n")
                .Replace("\t", "\\t").Replace("\b", "\\b").Replace("\f", "\\f");
        }

        private static Dictionary<string, object> AsObject(object value)
        {
            return value as Dictionary<string, object>;
        }

        private static List<object> AsArray(object value)
        {
            return value as List<object>;
        }

        private sealed class ChartRecord
        {
            public string key;
            public string songId;
            public string variationId;
            public string difficultyId;
            public int difficultyRating;
            public string songName;
            public string artist;
            public string playerCharacterId;
            public string opponentCharacterId;
            public string girlfriendCharacterId;
            public float baseBpm;
            public float scrollSpeed;
            public float previewStartSeconds;
            public float previewEndSeconds;
            public List<float> noteTimes = new List<float>();
            public List<int> noteDirections = new List<int>();
            public List<float> sustainLengths = new List<float>();
            public List<string> noteKinds = new List<string>();
            public List<float> bpmTimes = new List<float>();
            public List<float> bpmValues = new List<float>();
            public List<float> eventTimes = new List<float>();
            public List<string> eventKinds = new List<string>();
            public List<string> eventValuesJson = new List<string>();

            public void SortEvents()
            {
                List<int> order = Enumerable.Range(0, eventTimes.Count)
                    .OrderBy(index => eventTimes[index]).ToList();
                List<float> sortedTimes = new List<float>();
                List<string> sortedKinds = new List<string>();
                List<string> sortedValues = new List<string>();
                for (int i = 0; i < order.Count; i++)
                {
                    int index = order[i];
                    sortedTimes.Add(eventTimes[index]);
                    sortedKinds.Add(eventKinds[index]);
                    sortedValues.Add(eventValuesJson[index]);
                }
                eventTimes = sortedTimes;
                eventKinds = sortedKinds;
                eventValuesJson = sortedValues;
            }

            public void SortNotes()
            {
                List<int> order = Enumerable.Range(0, noteTimes.Count)
                    .OrderBy(index => noteTimes[index]).ToList();
                List<float> sortedTimes = new List<float>();
                List<int> sortedDirections = new List<int>();
                List<float> sortedSustains = new List<float>();
                List<string> sortedKinds = new List<string>();
                for (int i = 0; i < order.Count; i++)
                {
                    int index = order[i];
                    sortedTimes.Add(noteTimes[index]);
                    sortedDirections.Add(noteDirections[index]);
                    sortedSustains.Add(sustainLengths[index]);
                    sortedKinds.Add(noteKinds[index]);
                }
                noteTimes = sortedTimes;
                noteDirections = sortedDirections;
                sustainLengths = sortedSustains;
                noteKinds = sortedKinds;
            }
        }

        /// <summary>Small JSON reader kept Editor-only to avoid runtime package dependencies.</summary>
        private sealed class JsonParser
        {
            private readonly string source;
            private int position;

            private JsonParser(string json) { source = json ?? ""; }

            public static object Parse(string json)
            {
                JsonParser parser = new JsonParser(json);
                object value = parser.ReadValue();
                parser.SkipWhitespace();
                if (parser.position != parser.source.Length)
                    throw new FormatException("Unexpected trailing JSON content.");
                return value;
            }

            private object ReadValue()
            {
                SkipWhitespace();
                if (position >= source.Length) throw new FormatException("Unexpected end of JSON.");
                char c = source[position];
                if (c == '{') return ReadObject();
                if (c == '[') return ReadArray();
                if (c == '"') return ReadString();
                if (c == 't') { ReadLiteral("true"); return true; }
                if (c == 'f') { ReadLiteral("false"); return false; }
                if (c == 'n') { ReadLiteral("null"); return null; }
                return ReadNumber();
            }

            private Dictionary<string, object> ReadObject()
            {
                Dictionary<string, object> result = new Dictionary<string, object>();
                position++;
                SkipWhitespace();
                if (Take('}')) return result;
                while (true)
                {
                    SkipWhitespace();
                    if (position >= source.Length || source[position] != '"')
                        throw new FormatException("Expected a JSON object key.");
                    string key = ReadString();
                    SkipWhitespace();
                    Expect(':');
                    result[key] = ReadValue();
                    SkipWhitespace();
                    if (Take('}')) return result;
                    Expect(',');
                }
            }

            private List<object> ReadArray()
            {
                List<object> result = new List<object>();
                position++;
                SkipWhitespace();
                if (Take(']')) return result;
                while (true)
                {
                    result.Add(ReadValue());
                    SkipWhitespace();
                    if (Take(']')) return result;
                    Expect(',');
                }
            }

            private string ReadString()
            {
                Expect('"');
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                while (position < source.Length)
                {
                    char c = source[position++];
                    if (c == '"') return builder.ToString();
                    if (c != '\\') { builder.Append(c); continue; }
                    if (position >= source.Length) throw new FormatException("Invalid string escape.");
                    char escaped = source[position++];
                    if (escaped == '"' || escaped == '\\' || escaped == '/') builder.Append(escaped);
                    else if (escaped == 'b') builder.Append('\b');
                    else if (escaped == 'f') builder.Append('\f');
                    else if (escaped == 'n') builder.Append('\n');
                    else if (escaped == 'r') builder.Append('\r');
                    else if (escaped == 't') builder.Append('\t');
                    else if (escaped == 'u')
                    {
                        if (position + 4 > source.Length) throw new FormatException("Invalid unicode escape.");
                        string hex = source.Substring(position, 4);
                        builder.Append((char)int.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        position += 4;
                    }
                    else throw new FormatException("Unknown string escape.");
                }
                throw new FormatException("Unterminated JSON string.");
            }

            private double ReadNumber()
            {
                int start = position;
                if (position < source.Length && source[position] == '-') position++;
                while (position < source.Length && char.IsDigit(source[position])) position++;
                if (position < source.Length && source[position] == '.')
                {
                    position++;
                    while (position < source.Length && char.IsDigit(source[position])) position++;
                }
                if (position < source.Length && (source[position] == 'e' || source[position] == 'E'))
                {
                    position++;
                    if (position < source.Length && (source[position] == '+' || source[position] == '-')) position++;
                    while (position < source.Length && char.IsDigit(source[position])) position++;
                }
                string token = source.Substring(start, position - start);
                return double.Parse(token, NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            private void ReadLiteral(string literal)
            {
                if (position + literal.Length > source.Length ||
                    string.Compare(source, position, literal, 0, literal.Length, StringComparison.Ordinal) != 0)
                    throw new FormatException("Invalid JSON literal.");
                position += literal.Length;
            }

            private void SkipWhitespace()
            {
                while (position < source.Length && char.IsWhiteSpace(source[position])) position++;
            }

            private bool Take(char expected)
            {
                if (position < source.Length && source[position] == expected)
                {
                    position++;
                    return true;
                }
                return false;
            }

            private void Expect(char expected)
            {
                if (!Take(expected)) throw new FormatException("Expected '" + expected + "' in JSON.");
            }
        }
    }
}
#endif
