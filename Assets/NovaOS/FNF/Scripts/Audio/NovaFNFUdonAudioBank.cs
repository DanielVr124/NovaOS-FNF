using UdonSharp;
using UnityEngine;

namespace NovaOS.FNF
{
    /// <summary>
    /// Runtime-safe audio references for a VRChat world.
    /// The Editor scanner fills these arrays; Udon reads them at runtime.
    /// IDs must be normalized to lowercase kebab-case.
    /// </summary>
    public class NovaFNFUdonAudioBank : UdonSharpBehaviour
    {
        [Header("Songs")]
        public string[] songIds;
        public AudioClip[] instrumentals;
        public AudioClip[] vocals;

        [Header("Menu Music")]
        public string[] menuMusicIds;
        public AudioClip[] menuMusicClips;

        [Header("Sound Effects")]
        public string[] soundEffectIds;
        public AudioClip[] soundEffectClips;

        public int FindSongIndex(string id)
        {
            string wanted = NormalizeId(id);
            if (songIds == null) return -1;

            for (int i = 0; i < songIds.Length; i++)
            {
                if (NormalizeId(songIds[i]) == wanted) return i;
            }
            return -1;
        }

        public int FindMenuMusicIndex(string id)
        {
            string wanted = NormalizeId(id);
            if (menuMusicIds == null) return -1;

            for (int i = 0; i < menuMusicIds.Length; i++)
            {
                if (NormalizeId(menuMusicIds[i]) == wanted) return i;
            }
            return -1;
        }

        public int FindSoundEffectIndex(string id)
        {
            string wanted = NormalizeId(id);
            if (soundEffectIds == null) return -1;

            for (int i = 0; i < soundEffectIds.Length; i++)
            {
                if (NormalizeId(soundEffectIds[i]) == wanted) return i;
            }
            return -1;
        }

        private string NormalizeId(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Trim().ToLowerInvariant().Replace("_", "-").Replace(" ", "-");
        }
    }
}
