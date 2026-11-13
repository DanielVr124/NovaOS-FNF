using System;
using UnityEngine;

namespace NovaOS.FNF
{
    [Serializable]
    public class NovaFNFStagePropDefinition
    {
        public string propName;
        public string assetPath;
        public string animationType;
        public string[] animationNames;
        public string[] animationPrefixes;
        public float[] animationFrameRates;
        public bool[] animationLooped;
        public Vector2 position;
        public Vector2 scale = Vector2.one;
        public Vector2 scrollFactor = Vector2.one;
        public int zIndex;
        public int danceEvery;
        public bool isPixel;
        public TextAsset sourceAnimationData;
        public TextAsset spriteAtlasData;
        public Texture2D spriteAtlasTexture;
        public TextAsset sparrowAtlasXml;
        public Texture2D sparrowTexture;
    }

    [Serializable]
    public class NovaFNFStageCharacterPosition
    {
        public string characterId;
        public Vector2 position;
        public Vector2 cameraOffsets;
        public int zIndex;
    }

    /// <summary>Imported stage layout and prop metadata from official stage JSON.</summary>
    [CreateAssetMenu(menuName = "NovaOS/FNF/Stage Definition")]
    public class NovaFNFStageDefinition : ScriptableObject
    {
        public string stageId;
        public string displayName;
        public string directory;
        public float cameraZoom = 1f;
        public NovaFNFStagePropDefinition[] props;
        public NovaFNFStageCharacterPosition[] characters;
        public TextAsset sourceJson;
    }
}
