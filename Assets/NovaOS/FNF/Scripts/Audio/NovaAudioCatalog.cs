using System;
using System.Collections.Generic;
using UnityEngine;

namespace NovaOS.FNF
{
    [CreateAssetMenu(fileName = "NovaAudioCatalog", menuName = "NovaOS/FNF/Audio Catalog")]
    public sealed class NovaAudioCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class SongAudio
        {
            public string songId;
            public string variationId = "default";
            public AudioClip instrumental;
            public AudioClip vocals;
        }

        [Serializable]
        public sealed class VoiceAudio
        {
            public string songId;
            public string variationId = "default";
            public string characterId;
            public AudioClip clip;
        }

        [Serializable]
        public sealed class NamedAudio
        {
            public string id;
            public AudioClip clip;
        }

        [SerializeField] private List<SongAudio> songs = new List<SongAudio>();
        [SerializeField] private List<VoiceAudio> voiceTracks = new List<VoiceAudio>();
        [SerializeField] private List<NamedAudio> menuMusic = new List<NamedAudio>();
        [SerializeField] private List<NamedAudio> soundEffects = new List<NamedAudio>();

        public IReadOnlyList<SongAudio> Songs => songs;
        public IReadOnlyList<VoiceAudio> VoiceTracks => voiceTracks;
        public IReadOnlyList<NamedAudio> MenuMusic => menuMusic;
        public IReadOnlyList<NamedAudio> SoundEffects => soundEffects;

        public bool TryGetSong(string songId, out SongAudio result)
        {
            return TryGetSong(songId, "default", out result);
        }

        public bool TryGetSong(string songId, string variationId, out SongAudio result)
        {
            string wantedSong = NormalizeId(songId);
            string wantedVariation = NormalizeId(variationId);
            for (int i = 0; i < songs.Count; i++)
            {
                if (NormalizeId(songs[i].songId) == wantedSong &&
                    NormalizeId(songs[i].variationId) == wantedVariation)
                {
                    result = songs[i];
                    return true;
                }
            }

            if (wantedVariation != "default")
            {
                for (int i = 0; i < songs.Count; i++)
                {
                    if (NormalizeId(songs[i].songId) == wantedSong &&
                        NormalizeId(songs[i].variationId) == "default")
                    {
                        result = songs[i];
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }

        public bool TryGetMenuMusic(string id, out AudioClip clip)
        {
            return TryGetNamed(menuMusic, id, out clip);
        }

        public bool TryGetSoundEffect(string id, out AudioClip clip)
        {
            return TryGetNamed(soundEffects, id, out clip);
        }

        private static bool TryGetNamed(List<NamedAudio> list, string id, out AudioClip clip)
        {
            string wanted = NormalizeId(id);
            for (int i = 0; i < list.Count; i++)
            {
                if (NormalizeId(list[i].id) == wanted)
                {
                    clip = list[i].clip;
                    return clip != null;
                }
            }

            clip = null;
            return false;
        }

        public static string NormalizeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return value.Trim().ToLowerInvariant().Replace(" ", "-").Replace("_", "-");
        }

#if UNITY_EDITOR
        public void EditorReplaceData(List<SongAudio> newSongs, List<VoiceAudio> newVoiceTracks, List<NamedAudio> newMenuMusic, List<NamedAudio> newSoundEffects)
        {
            songs = newSongs ?? new List<SongAudio>();
            voiceTracks = newVoiceTracks ?? new List<VoiceAudio>();
            menuMusic = newMenuMusic ?? new List<NamedAudio>();
            soundEffects = newSoundEffects ?? new List<NamedAudio>();
        }
#endif
    }
}
