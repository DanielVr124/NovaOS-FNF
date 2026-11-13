using System;
using UnityEngine;

namespace NovaOS.FNF
{
    [Serializable]
    public class NovaFNFCharacterAnimation
    {
        public string name;
        public string prefix;
        public string assetPath;
        public string animType;
        public float frameRate = 24f;
        public bool looped;
        public int[] frameIndices;
        public Vector2 offsets;
        public bool hasCustomOffsets;
    }

    /// <summary>
    /// Imported metadata for one official FNF character. The original JSON and
    /// atlas files remain referenced so the renderer can be upgraded without
    /// re-importing the source repository.
    /// </summary>
    [CreateAssetMenu(menuName = "NovaOS/FNF/Character Definition")]
    public class NovaFNFCharacterDefinition : ScriptableObject
    {
        public string characterId;
        public string displayName;
        public string renderType;
        public string assetPath;
        public string startingAnimation;
        public bool flipX;
        public float singTime = 8f;
        public Vector2 offsets;
        public Vector2 cameraOffsets;
        public NovaFNFCharacterAnimation[] animations;
        public TextAsset sourceJson;
        public TextAsset animateAtlasJson;
        public TextAsset spriteMapJson;
        public Texture2D spriteMapTexture;
    }
}
