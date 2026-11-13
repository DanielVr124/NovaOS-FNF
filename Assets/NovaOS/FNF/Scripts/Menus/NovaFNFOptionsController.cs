using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace NovaOS.FNF
{
    /// <summary>
    /// Session-level gameplay and accessibility settings.
    /// UI Toggle/Slider callbacks can be connected directly to the public methods.
    /// Persistence will be layered on top after PlayerData integration is tested.
    /// </summary>
    public class NovaFNFOptionsController : UdonSharpBehaviour
    {
        public NovaFNFGameplayController gameplay;
        public NovaFNFUdonAudioPlayer audioPlayer;

        [Header("Gameplay")]
        public bool downscroll;
        [Range(0.5f, 2f)] public float scrollSpeedMultiplier = 1f;
        [Range(-250f, 250f)] public float inputOffsetMs;
        public bool ghostTapping = true;

        [Header("Presentation and accessibility")]
        public bool cameraZooms = true;
        public bool flashingLights = true;
        public bool subtitles = true;
        public bool reducedMotion;
        public bool pauseOnUnfocus;

        [Header("Audio")]
        [Range(0f, 1f)] public float instrumentalVolume = 1f;
        [Range(0f, 1f)] public float vocalsVolume = 1f;
        [Range(0f, 1f)] public float menuMusicVolume = 1f;
        [Range(0f, 1f)] public float soundEffectVolume = 1f;

        [Header("Optional value labels")]
        public Text offsetValueText;
        public Text scrollSpeedValueText;

        private void Start()
        {
            ApplyAll();
        }

        public void SetDownscroll(bool value)
        {
            downscroll = value;
            ApplyGameplaySettings();
        }

        public void SetScrollSpeed(float value)
        {
            scrollSpeedMultiplier = Mathf.Clamp(value, 0.5f, 2f);
            ApplyGameplaySettings();
            if (scrollSpeedValueText != null)
                scrollSpeedValueText.text = scrollSpeedMultiplier.ToString("F2") + "x";
        }

        public void SetInputOffset(float value)
        {
            inputOffsetMs = Mathf.Clamp(value, -250f, 250f);
            ApplyGameplaySettings();
            if (offsetValueText != null)
                offsetValueText.text = inputOffsetMs.ToString("F0") + " ms";
        }

        public void SetGhostTapping(bool value)
        {
            ghostTapping = value;
            ApplyGameplaySettings();
        }

        public void SetCameraZooms(bool value) { cameraZooms = value; }
        public void SetFlashingLights(bool value) { flashingLights = value; }
        public void SetSubtitles(bool value) { subtitles = value; }
        public void SetReducedMotion(bool value) { reducedMotion = value; }

        public void SetInstrumentalVolume(float value)
        {
            instrumentalVolume = Mathf.Clamp01(value);
            ApplyAudioSettings();
        }

        public void SetVocalsVolume(float value)
        {
            vocalsVolume = Mathf.Clamp01(value);
            ApplyAudioSettings();
        }

        public void SetMenuMusicVolume(float value)
        {
            menuMusicVolume = Mathf.Clamp01(value);
            ApplyAudioSettings();
        }

        public void SetSoundEffectVolume(float value)
        {
            soundEffectVolume = Mathf.Clamp01(value);
            ApplyAudioSettings();
        }

        public void ResetGameplayDefaults()
        {
            downscroll = false;
            scrollSpeedMultiplier = 1f;
            inputOffsetMs = 0f;
            ghostTapping = true;
            ApplyAll();
        }

        public void ResetAudioDefaults()
        {
            instrumentalVolume = 1f;
            vocalsVolume = 1f;
            menuMusicVolume = 1f;
            soundEffectVolume = 1f;
            ApplyAll();
        }

        private void ApplyAll()
        {
            ApplyGameplaySettings();
            ApplyAudioSettings();
            if (offsetValueText != null) offsetValueText.text = inputOffsetMs.ToString("F0") + " ms";
            if (scrollSpeedValueText != null) scrollSpeedValueText.text = scrollSpeedMultiplier.ToString("F2") + "x";
        }

        private void ApplyGameplaySettings()
        {
            if (gameplay == null) return;
            gameplay.downscroll = downscroll;
            gameplay.scrollSpeedMultiplier = scrollSpeedMultiplier;
            gameplay.inputOffsetMs = inputOffsetMs;
            gameplay.ghostTapping = ghostTapping;
        }

        private void ApplyAudioSettings()
        {
            if (audioPlayer == null) return;
            audioPlayer.instrumentalVolume = instrumentalVolume;
            audioPlayer.vocalsVolume = vocalsVolume;
            audioPlayer.menuMusicVolume = menuMusicVolume;
            audioPlayer.soundEffectVolume = soundEffectVolume;
        }
    }
}
