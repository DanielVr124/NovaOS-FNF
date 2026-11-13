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
        public string[] voiceSongIds;
        public string[] voiceCharacterIds;
        public AudioClip[] voiceClips;

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

        public int FindVoiceIndex(string songId, string characterId)
        {
            string wantedSong = NormalizeId(songId);
            string wantedCharacter = NormalizeId(characterId);
            if (voiceSongIds == null || voiceCharacterIds == null) return -1;

            for (int i = 0; i < voiceSongIds.Length; i++)
            {
                if (i >= voiceCharacterIds.Length) break;
                if (NormalizeId(voiceSongIds[i]) == wantedSong &&
                    NormalizeId(voiceCharacterIds[i]) == wantedCharacter)
                    return i;
            }
            return -1;
        }

        public AudioClip GetVoiceClip(string songId, string characterId)
        {
            int index = FindVoiceIndex(songId, characterId);
            if (index >= 0 && voiceClips != null && index < voiceClips.Length)
                return voiceClips[index];

            int defaultIndex = FindVoiceIndex(songId, "default");
            if (defaultIndex >= 0 && voiceClips != null && defaultIndex < voiceClips.Length)
                return voiceClips[defaultIndex];

            return null;
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
