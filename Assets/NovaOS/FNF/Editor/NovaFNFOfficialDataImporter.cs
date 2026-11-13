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
    /// Converts the official character and stage JSON into Unity-native
    /// ScriptableObject definitions and links their original atlas files.
    /// This preserves the new game's data model instead of flattening it to
    /// the legacy Sparrow-only character format.
    /// </summary>
    [InitializeOnLoad]
    public static class NovaFNFOfficialDataImporter
    {
        private const string AutoImportSessionKey = "NovaOS.FNF.OfficialDataAutoImported";

        static NovaFNFOfficialDataImporter()
        {
            EditorApplication.delayCall += AutoImportAfterPackageImport;
        }

        private static void AutoImportAfterPackageImport()
        {
            if (SessionState.GetBool(AutoImportSessionKey, false)) return;
            if (!AssetDatabase.IsValidFolder(CharacterJsonRoot) ||
                !AssetDatabase.IsValidFolder(StageJsonRoot)) return;

            bool hasCharacters = AssetDatabase.IsValidFolder(CharacterOutput) &&
                AssetDatabase.FindAssets("t:NovaFNFCharacterDefinition", new[] { CharacterOutput }).Length > 0;
            bool hasStages = AssetDatabase.IsValidFolder(StageOutput) &&
                AssetDatabase.FindAssets("t:NovaFNFStageDefinition", new[] { StageOutput }).Length > 0;
            if (hasCharacters && hasStages)
            {
                SessionState.SetBool(AutoImportSessionKey, true);
                return;
            }

            try
            {
                ImportAllInternal(false);
                hasCharacters = AssetDatabase.IsValidFolder(CharacterOutput) &&
                    AssetDatabase.FindAssets("t:NovaFNFCharacterDefinition", new[] { CharacterOutput }).Length > 0;
                hasStages = AssetDatabase.IsValidFolder(StageOutput) &&
                    AssetDatabase.FindAssets("t:NovaFNFStageDefinition", new[] { StageOutput }).Length > 0;
                if (hasCharacters && hasStages)
                    SessionState.SetBool(AutoImportSessionKey, true);
            }
            catch (Exception exception)
            {
                // Do not suppress a later retry if imported data was not ready yet.
                Debug.LogError("NovaOS automatic official-data import failed: " + exception);
            }
        }

        private const string OfficialRoot = "Assets/NovaOS/FNF/OfficialAssets";
        private const string CharacterJsonRoot = OfficialRoot + "/preload/data/characters";
        private const string StageJsonRoot = OfficialRoot + "/preload/data/stages";
        private const string CharacterOutput = "Assets/NovaOS/Generated/Characters";
        private const string StageOutput = "Assets/NovaOS/Generated/Stages";

        [MenuItem("NovaOS/FNF/Official Data/Import Characters and Stages")]
        public static void ImportAll()
        {
            EnsureFolder("Assets/NovaOS/Generated");
            EnsureFolder(CharacterOutput);
            EnsureFolder(StageOutput);

            int characters = ImportCharacters();
            int stages = ImportStages();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string report = "Character definitions: " + characters +
                "\nStage definitions: " + stages +
                "\n\nGenerated assets are in Assets/NovaOS/Generated.";
            Debug.Log("NovaOS official character/stage import completed.\n" + report);
            EditorUtility.DisplayDialog("NovaOS Official Data", report, "Done");
        }

        private static int ImportCharacters()
        {
            if (!AssetDatabase.IsValidFolder(CharacterJsonRoot)) return 0;
            string[] paths = Directory.GetFiles(CharacterJsonRoot, "*.json", SearchOption.AllDirectories);
            int count = 0;
            foreach (string absolute in paths)
            {
                string path = ToAssetPath(absolute);
                TextAsset jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (jsonAsset == null) continue;
                try
                {
                    CharacterJson data = JsonUtility.FromJson<CharacterJson>(jsonAsset.text);
                    if (data == null || data.animations == null) continue;
                    string id = Path.GetFileNameWithoutExtension(path);
                    NovaFNFCharacterDefinition definition = LoadOrCreate<NovaFNFCharacterDefinition>(
                        CharacterOutput + "/" + id + ".asset");
                    definition.characterId = id;
                    definition.displayName = string.IsNullOrEmpty(data.name) ? id : data.name;
                    definition.renderType = data.renderType ?? "";
                    definition.assetPath = data.assetPath ?? "";
                    definition.startingAnimation = data.startingAnimation ?? "idle";
                    definition.flipX = data.flipX;
                    definition.singTime = data.singTime > 0f ? data.singTime : 8f;
                    definition.offsets = Pair(data.offsets);
                    definition.cameraOffsets = Pair(data.cameraOffsets);
                    definition.sourceJson = jsonAsset;
                    definition.animations = ConvertAnimations(data.animations);
                    ResolveCharacterAtlas(definition);
                    EditorUtility.SetDirty(definition);
                    count++;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Could not import character data " + path + ": " + e.Message);
                }
            }
            return count;
        }

        private static int ImportStages()
        {
            if (!AssetDatabase.IsValidFolder(StageJsonRoot)) return 0;
            string[] paths = Directory.GetFiles(StageJsonRoot, "*.json", SearchOption.AllDirectories);
            int count = 0;
            foreach (string absolute in paths)
            {
                string path = ToAssetPath(absolute);
                TextAsset jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (jsonAsset == null) continue;
                try
                {
                    StageJson data = JsonUtility.FromJson<StageJson>(jsonAsset.text);
                    if (data == null) continue;
                    string id = Path.GetFileNameWithoutExtension(path);
                    NovaFNFStageDefinition definition = LoadOrCreate<NovaFNFStageDefinition>(
                        StageOutput + "/" + id + ".asset");
                    definition.stageId = id;
                    definition.displayName = data.name ?? id;
                    definition.directory = data.directory ?? "";
                    definition.cameraZoom = data.cameraZoom > 0f ? data.cameraZoom : 1f;
                    definition.sourceJson = jsonAsset;
                    definition.props = ConvertProps(data.props);
                    definition.characters = ConvertCharacters(data.characters);
                    ResolveStageAtlases(definition);
                    EditorUtility.SetDirty(definition);
                    count++;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Could not import stage data " + path + ": " + e.Message);
                }
            }
            return count;
        }

        private static NovaFNFCharacterAnimation[] ConvertAnimations(CharacterAnimationJson[] source)
        {
            if (source == null) return new NovaFNFCharacterAnimation[0];
            var result = new NovaFNFCharacterAnimation[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                CharacterAnimationJson item = source[i];
                result[i] = new NovaFNFCharacterAnimation
                {
                    name = item.name ?? "",
                    prefix = item.prefix ?? "",
                    assetPath = item.assetPath ?? "",
                    animType = item.animType ?? "",
                    frameRate = item.frameRate > 0f ? item.frameRate : 24f,
                    looped = item.looped,
                    frameIndices = item.frameIndices ?? new int[0],
                    offsets = Pair(item.offsets),
                    hasCustomOffsets = item.offsets != null && item.offsets.Length >= 2
                };
            }
            return result;
        }

        private static NovaFNFStagePropDefinition[] ConvertProps(StagePropJson[] source)
        {
            if (source == null) return new NovaFNFStagePropDefinition[0];
            var result = new NovaFNFStagePropDefinition[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                StagePropJson item = source[i];
                var animations = item.animations ?? new StageAnimationJson[0];
                var names = new string[animations.Length];
                var prefixes = new string[animations.Length];
                var rates = new float[animations.Length];
                var loops = new bool[animations.Length];
                for (int a = 0; a < animations.Length; a++)
                {
                    names[a] = animations[a].name ?? "";
                    prefixes[a] = animations[a].prefix ?? "";
                    rates[a] = animations[a].frameRate > 0f ? animations[a].frameRate : 24f;
                    loops[a] = animations[a].looped;
                }
                result[i] = new NovaFNFStagePropDefinition
                {
                    propName = item.name ?? "Stage Prop",
                    assetPath = item.assetPath ?? "",
                    animationType = item.animType ?? "",
                    animationNames = names,
                    animationPrefixes = prefixes,
                    animationFrameRates = rates,
                    animationLooped = loops,
                    position = Pair(item.position),
                    scale = Pair(item.scale, Vector2.one),
                    scrollFactor = Pair(item.scroll, Vector2.one),
                    zIndex = item.zIndex,
                    danceEvery = item.danceEvery,
                    isPixel = item.isPixel
                };
            }
            return result;
        }

        private static NovaFNFStageCharacterPosition[] ConvertCharacters(StageCharactersJson source)
        {
            if (source == null) return new NovaFNFStageCharacterPosition[0];
            var result = new List<NovaFNFStageCharacterPosition>();
            AddStageCharacter(result, "bf", source.bf);
            AddStageCharacter(result, "dad", source.dad);
            AddStageCharacter(result, "gf", source.gf);
            return result.ToArray();
        }

        private static void AddStageCharacter(List<NovaFNFStageCharacterPosition> list, string id, StageCharacterJson data)
        {
            if (data == null) return;
            list.Add(new NovaFNFStageCharacterPosition
            {
                characterId = id,
                position = Pair(data.position),
                cameraOffsets = Pair(data.cameraOffsets),
                zIndex = data.zIndex
            });
        }

        private static void ResolveCharacterAtlas(NovaFNFCharacterDefinition definition)
        {
            string root = ResolveAssetPath(definition.assetPath);
            if (string.IsNullOrEmpty(root)) return;
            definition.animateAtlasJson = AssetDatabase.LoadAssetAtPath<TextAsset>(root + "/Animation.json");
            definition.spriteMapJson = AssetDatabase.LoadAssetAtPath<TextAsset>(root + "/spritemap1.json");
            definition.spriteMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "/spritemap1.png");
            definition.sparrowAtlasXml = AssetDatabase.LoadAssetAtPath<TextAsset>(root + ".xml");
            definition.sparrowTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(root + ".png");
        }

        private static void ResolveStageAtlases(NovaFNFStageDefinition definition)
        {
            if (definition.props == null) return;
            for (int i = 0; i < definition.props.Length; i++)
            {
                string root = ResolveAssetPath(definition.props[i].assetPath);
                if (string.IsNullOrEmpty(root)) continue;
                definition.props[i].sourceAnimationData = AssetDatabase.LoadAssetAtPath<TextAsset>(root + "/Animation.json");
                definition.props[i].spriteAtlasData = AssetDatabase.LoadAssetAtPath<TextAsset>(root + "/spritemap1.json");
                definition.props[i].spriteAtlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(root + "/spritemap1.png");
                definition.props[i].sparrowAtlasXml = AssetDatabase.LoadAssetAtPath<TextAsset>(root + ".xml");
                definition.props[i].sparrowTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(root + ".png");
            }
        }

        private static string ResolveAssetPath(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            string path = value.Replace("\\", "/");
            if (path.StartsWith("shared:", StringComparison.OrdinalIgnoreCase))
                return OfficialRoot + "/shared/images/" + path.Substring(7).TrimStart('/');
            if (path.StartsWith("preload:", StringComparison.OrdinalIgnoreCase))
                return OfficialRoot + "/preload/images/" + path.Substring(8).TrimStart('/');
            if (path.StartsWith("week", StringComparison.OrdinalIgnoreCase))
                return OfficialRoot + "/preload/images/" + path;
            return OfficialRoot + "/preload/images/" + path;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Vector2 Pair(float[] values)
        {
            return Pair(values, Vector2.zero);
        }

        private static Vector2 Pair(float[] values, Vector2 fallback)
        {
            if (values == null || values.Length < 2) return fallback;
            return new Vector2(values[0], values[1]);
        }

        private static string ToAssetPath(string absolute)
        {
            string root = Directory.GetCurrentDirectory().Replace('\\', '/') + "/";
            return absolute.Replace('\\', '/').Replace(root, "");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        [Serializable] private class CharacterJson
        {
            public string name, renderType, assetPath, startingAnimation;
            public bool flipX;
            public float singTime;
            public float[] offsets, cameraOffsets;
            public CharacterAnimationJson[] animations;
        }
        [Serializable] private class CharacterAnimationJson
        {
            public string name, prefix, assetPath, animType;
            public float frameRate;
            public bool looped;
            public int[] frameIndices;
            public float[] offsets;
        }
        [Serializable] private class StageJson
        {
            public string name, directory;
            public float cameraZoom;
            public StagePropJson[] props;
            public StageCharactersJson characters;
        }
        [Serializable] private class StagePropJson
        {
            public string name, assetPath, animType;
            public float[] position, scale, scroll;
            public int zIndex, danceEvery;
            public bool isPixel;
            public StageAnimationJson[] animations;
        }
        [Serializable] private class StageAnimationJson
        {
            public string name, prefix;
            public float frameRate;
            public bool looped;
        }
        [Serializable] private class StageCharactersJson
        {
            public StageCharacterJson bf, dad, gf;
        }
        [Serializable] private class StageCharacterJson
        {
            public float[] position, cameraOffsets;
            public int zIndex;
        }
    }
}
#endif
