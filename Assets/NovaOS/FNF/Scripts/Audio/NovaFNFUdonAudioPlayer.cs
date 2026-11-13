using UdonSharp;
using UnityEngine;

namespace NovaOS.FNF
{
    /// <summary>
    /// UdonSharp runtime audio playback. Assign the generated bank and sources in the Inspector.
    /// Instrumental and vocal playback are started in the same Udon frame.
    /// </summary>
    public class NovaFNFUdonAudioPlayer : UdonSharpBehaviour
    {
        [Header("References")]
        public NovaFNFUdonAudioBank audioBank;
        public AudioSource instrumentalSource;
        public AudioSource vocalsSource;
        public AudioSource menuMusicSource;
        public AudioSource menuMusicSecondarySource;
        public AudioSource soundEffectSource;

        [Header("Playback")]
        [Range(0f, 1f)] public float instrumentalVolume = 1f;
        [Range(0f, 1f)] public float vocalsVolume = 1f;
        [Range(0f, 1f)] public float menuMusicVolume = 1f;
        [Range(0f, 1f)] public float soundEffectVolume = 1f;
        [Range(0f, 3f)] public float menuMusicFadeSeconds = 0.45f;
        [Range(0f, 0.5f)] public float vocalResyncThreshold = 0.12f;

        public string currentSongId = "";
        public bool songLoaded;
        public bool songPaused;

        private AudioSource activeMenuSource;
        private AudioSource incomingMenuSource;
        private float menuFadeProgress;
        private bool menuFadeActive;

        private void Start()
        {
            activeMenuSource = menuMusicSource;
        }

        private void Update()
        {
            UpdateMenuMusicFade();
            if (!songLoaded || songPaused) return;
            if (instrumentalSource == null || vocalsSource == null) return;
            if (!instrumentalSource.isPlaying || !vocalsSource.isPlaying) return;
            if (vocalsSource.clip == null) return;

            float drift = instrumentalSource.time - vocalsSource.time;
            if (Mathf.Abs(drift) > vocalResyncThreshold)
            {
                float safeTime = Mathf.Clamp(vocalsSource.time + drift, 0f, vocalsSource.clip.length);
                vocalsSource.time = safeTime;
            }
        }

        public bool PlaySong(string songId)
        {
            return PlaySongFromTime(songId, 0f);
        }

        public bool PlaySongFromTime(string songId, float startSeconds)
        {
            if (audioBank == null || instrumentalSource == null || vocalsSource == null)
            {
                Debug.LogError("NovaFNFUdonAudioPlayer is missing its audio bank or song AudioSources.");
                return false;
            }

            int index = audioBank.FindSongIndex(songId);
            if (index < 0 || audioBank.instrumentals == null || index >= audioBank.instrumentals.Length)
            {
                Debug.LogWarning("No instrumental audio was indexed for song: " + songId);
                return false;
            }

            AudioClip instrumental = audioBank.instrumentals[index];
            if (instrumental == null)
            {
                Debug.LogWarning("The instrumental reference is empty for song: " + songId);
                return false;
            }

            StopSong();
            instrumentalSource.clip = instrumental;
            instrumentalSource.volume = instrumentalVolume;
            instrumentalSource.loop = false;

            AudioClip vocal = null;
            if (audioBank.vocals != null && index < audioBank.vocals.Length)
                vocal = audioBank.vocals[index];

            vocalsSource.clip = vocal;
            vocalsSource.volume = vocalsVolume;
            vocalsSource.loop = false;

            float start = Mathf.Clamp(startSeconds, 0f, Mathf.Max(0f, instrumental.length - 0.01f));
            instrumentalSource.time = start;
            if (vocal != null && start < vocal.length)
                vocalsSource.time = start;

            // Both Play calls run in the same Udon update, avoiding a deliberate one-source delay.
            instrumentalSource.Play();
            if (vocal != null && start < vocal.length)
                vocalsSource.Play();

            currentSongId = songId;
            songLoaded = true;
            songPaused = false;
            return true;
        }

        public void PauseSong()
        {
            if (!songLoaded || songPaused) return;
            if (instrumentalSource != null) instrumentalSource.Pause();
            if (vocalsSource != null) vocalsSource.Pause();
            songPaused = true;
        }

        public void ResumeSong()
        {
            if (!songLoaded || !songPaused) return;
            if (instrumentalSource != null) instrumentalSource.UnPause();
            if (vocalsSource != null) vocalsSource.UnPause();
            songPaused = false;
        }

        public void StopSong()
        {
            if (instrumentalSource != null) instrumentalSource.Stop();
            if (vocalsSource != null) vocalsSource.Stop();
            songLoaded = false;
            songPaused = false;
            currentSongId = "";
        }

        public bool IsSongSourcePlaying()
        {
            bool instrumentalPlaying = instrumentalSource != null && instrumentalSource.isPlaying;
            bool vocalsPlaying = vocalsSource != null && vocalsSource.isPlaying;
            return instrumentalPlaying || vocalsPlaying;
        }

        public float GetSongLengthSeconds()
        {
            if (instrumentalSource == null || instrumentalSource.clip == null) return 0f;
            return instrumentalSource.clip.length;
        }

        public float GetSongPositionSeconds()
        {
            if (!songLoaded || instrumentalSource == null) return 0f;
            return instrumentalSource.time;
        }

        public void PlayMenuMusic(string id)
        {
            if (audioBank == null || menuMusicSource == null) return;
            int index = audioBank.FindMenuMusicIndex(id);
            if (index < 0 || audioBank.menuMusicClips == null || index >= audioBank.menuMusicClips.Length) return;
            AudioClip clip = audioBank.menuMusicClips[index];
            if (clip == null) return;

            if (activeMenuSource == null) activeMenuSource = menuMusicSource;
            if (activeMenuSource.clip == clip && activeMenuSource.isPlaying) return;

            if (menuMusicSecondarySource == null || menuMusicFadeSeconds <= 0f)
            {
                menuMusicSource.Stop();
                menuMusicSource.clip = clip;
                menuMusicSource.volume = menuMusicVolume;
                menuMusicSource.loop = true;
                menuMusicSource.Play();
                activeMenuSource = menuMusicSource;
                return;
            }

            if (menuFadeActive && incomingMenuSource != null)
            {
                activeMenuSource.Stop();
                activeMenuSource.volume = 0f;
                activeMenuSource = incomingMenuSource;
                incomingMenuSource = null;
                menuFadeActive = false;
            }

            incomingMenuSource = activeMenuSource == menuMusicSource
                ? menuMusicSecondarySource : menuMusicSource;
            incomingMenuSource.Stop();
            incomingMenuSource.clip = clip;
            incomingMenuSource.loop = true;
            incomingMenuSource.volume = 0f;
            incomingMenuSource.Play();
            menuFadeProgress = 0f;
            menuFadeActive = true;
        }

        public void StopMenuMusic()
        {
            menuFadeActive = false;
            if (menuMusicSource != null) menuMusicSource.Stop();
            if (menuMusicSecondarySource != null) menuMusicSecondarySource.Stop();
            activeMenuSource = menuMusicSource;
            incomingMenuSource = null;
        }

        private void UpdateMenuMusicFade()
        {
            if (!menuFadeActive || activeMenuSource == null || incomingMenuSource == null) return;
            menuFadeProgress += Time.deltaTime / Mathf.Max(0.01f, menuMusicFadeSeconds);
            float blend = Mathf.Clamp01(menuFadeProgress);
            activeMenuSource.volume = menuMusicVolume * (1f - blend);
            incomingMenuSource.volume = menuMusicVolume * blend;

            if (blend >= 1f)
            {
                activeMenuSource.Stop();
                activeMenuSource.volume = 0f;
                activeMenuSource = incomingMenuSource;
                incomingMenuSource = null;
                menuFadeActive = false;
            }
        }

        public void PlaySoundEffect(string id)
        {
            if (audioBank == null || soundEffectSource == null) return;
            int index = audioBank.FindSoundEffectIndex(id);
            if (index < 0 || audioBank.soundEffectClips == null || index >= audioBank.soundEffectClips.Length) return;
            AudioClip clip = audioBank.soundEffectClips[index];
            if (clip == null) return;
            soundEffectSource.PlayOneShot(clip, soundEffectVolume);
        }
    }
}
