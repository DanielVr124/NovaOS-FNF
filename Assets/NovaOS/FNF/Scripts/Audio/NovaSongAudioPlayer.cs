using UnityEngine;

namespace NovaOS.FNF
{
    [DisallowMultipleComponent]
    public sealed class NovaSongAudioPlayer : MonoBehaviour
    {
        [SerializeField] private NovaAudioCatalog catalog;
        [SerializeField] private AudioSource instrumentalSource;
        [SerializeField] private AudioSource vocalsSource;

        public bool IsPlaying => instrumentalSource != null && instrumentalSource.isPlaying;
        public double SongStartDspTime { get; private set; }

        public void Configure(NovaAudioCatalog audioCatalog, AudioSource instrumental, AudioSource vocals)
        {
            catalog = audioCatalog;
            instrumentalSource = instrumental;
            vocalsSource = vocals;
        }

        public bool PlaySong(string songId, float startOffsetSeconds = 0f)
        {
            if (catalog == null || instrumentalSource == null || vocalsSource == null)
            {
                Debug.LogError("NovaSongAudioPlayer is missing its catalog or AudioSources.", this);
                return false;
            }

            if (!catalog.TryGetSong(songId, out NovaAudioCatalog.SongAudio song) || song.instrumental == null)
            {
                Debug.LogWarning("No instrumental audio was found for song '" + songId + "'.", this);
                return false;
            }

            Stop();
            instrumentalSource.clip = song.instrumental;
            vocalsSource.clip = song.vocals;
            instrumentalSource.loop = false;
            vocalsSource.loop = false;

            float safeOffset = Mathf.Max(0f, startOffsetSeconds);
            if (safeOffset >= instrumentalSource.clip.length) return false;

            double startAt = AudioSettings.dspTime + 0.15d;
            SongStartDspTime = startAt - safeOffset;
            instrumentalSource.time = safeOffset;
            instrumentalSource.PlayScheduled(startAt);

            if (vocalsSource.clip != null && safeOffset < vocalsSource.clip.length)
            {
                vocalsSource.time = safeOffset;
                vocalsSource.PlayScheduled(startAt);
            }

            return true;
        }

        public void Stop()
        {
            if (instrumentalSource != null) instrumentalSource.Stop();
            if (vocalsSource != null) vocalsSource.Stop();
        }

        public void Pause()
        {
            if (instrumentalSource != null) instrumentalSource.Pause();
            if (vocalsSource != null) vocalsSource.Pause();
        }

        public void Resume()
        {
            if (instrumentalSource != null) instrumentalSource.UnPause();
            if (vocalsSource != null) vocalsSource.UnPause();
        }

        public double GetSongPositionSeconds()
        {
            if (instrumentalSource == null || instrumentalSource.clip == null) return 0d;
            if (instrumentalSource.isPlaying) return instrumentalSource.time;
            return 0d;
        }
    }
}
