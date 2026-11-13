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
        public string[] songVariationIds;
        public AudioClip[] instrumentals;
        public AudioClip[] vocals;
        public string[] voiceSongIds;
        public string[] voiceCharacterIds;
        public string[] voiceVariationIds;
        public AudioClip[] voiceClips;

        [Header("Menu Music")]
        public string[] menuMusicIds;
        public AudioClip[] menuMusicClips;

        [Header("Sound Effects")]
        public string[] soundEffectIds;
        public AudioClip[] soundEffectClips;

        public int FindSongIndex(string id)
        {
            return FindSongIndex(id, "default");
        }

        // Exact lookup is used by validation so a missing remix instrumental
        // cannot be hidden by the runtime's intentional default-audio fallback.
        public int FindExactSongIndex(string id, string variationId)
        {
            string wantedSong = NormalizeId(id);
            string wantedVariation = NormalizeId(variationId);
            if (songIds == null) return -1;

            for (int i = 0; i < songIds.Length; i++)
            {
                string variation = songVariationIds != null && i < songVariationIds.Length
                    ? NormalizeId(songVariationIds[i]) : "default";
                if (NormalizeId(songIds[i]) == wantedSong && variation == wantedVariation) return i;
            }
            return -1;
        }

        public int FindSongIndex(string id, string variationId)
        {
            string wantedSong = NormalizeId(id);
            string wantedVariation = NormalizeId(variationId);
            if (songIds == null) return -1;

            for (int i = 0; i < songIds.Length; i++)
            {
                string variation = songVariationIds != null && i < songVariationIds.Length
                    ? NormalizeId(songVariationIds[i]) : "default";
                if (NormalizeId(songIds[i]) == wantedSong && variation == wantedVariation) return i;
            }

            if (wantedVariation != "default")
            {
                for (int i = 0; i < songIds.Length; i++)
                {
                    string variation = songVariationIds != null && i < songVariationIds.Length
                        ? NormalizeId(songVariationIds[i]) : "default";
                    if (NormalizeId(songIds[i]) == wantedSong && variation == "default") return i;
                }
            }
            return -1;
        }

        public int FindVoiceIndex(string songId, string characterId)
        {
            return FindVoiceIndex(songId, characterId, "default");
        }

        public int FindVoiceIndex(string songId, string characterId, string variationId)
        {
            string wantedSong = NormalizeId(songId);
            string wantedCharacter = NormalizeId(characterId);
            string wantedVariation = NormalizeId(variationId);
            if (voiceSongIds == null || voiceCharacterIds == null) return -1;

            // Prefer the exact character/variation pair.
            int exact = FindVoicePair(wantedSong, wantedCharacter, wantedVariation);
            if (exact >= 0) return exact;

            // If the variation has no character-specific stem, use default vocals for that variation.
            exact = FindVoicePair(wantedSong, "default", wantedVariation);
            if (exact >= 0) return exact;

            // Otherwise, reuse this character's default-variation vocal stem.
            if (wantedVariation != "default")
            {
                exact = FindVoicePair(wantedSong, wantedCharacter, "default");
                if (exact >= 0) return exact;
                exact = FindVoicePair(wantedSong, "default", "default");
                if (exact >= 0) return exact;
            }
            return -1;
        }

        private int FindVoicePair(string wantedSong, string wantedCharacter, string wantedVariation)
        {
            if (voiceSongIds == null || voiceCharacterIds == null) return -1;
            for (int i = 0; i < voiceSongIds.Length; i++)
            {
                if (i >= voiceCharacterIds.Length) break;
                string variation = voiceVariationIds != null && i < voiceVariationIds.Length
                    ? NormalizeId(voiceVariationIds[i]) : "default";
                if (NormalizeId(voiceSongIds[i]) == wantedSong &&
                    NormalizeId(voiceCharacterIds[i]) == wantedCharacter &&
                    variation == wantedVariation)
                    return i;
            }
            return -1;
        }

        public AudioClip GetVoiceClip(string songId, string characterId)
        {
            return GetVoiceClip(songId, characterId, "default");
        }

        public AudioClip GetVoiceClip(string songId, string characterId, string variationId)
        {
            int index = FindVoiceIndex(songId, characterId, variationId);
            if (index >= 0 && voiceClips != null && index < voiceClips.Length)
                return voiceClips[index];
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
