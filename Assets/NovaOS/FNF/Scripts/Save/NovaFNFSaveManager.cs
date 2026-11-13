using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Persistence;

namespace NovaOS.FNF
{
    /// <summary>
    /// Per-player persistent records and preferences using VRChat PlayerData.
    /// Reads are delayed until OnPlayerRestored so restored values are not overwritten.
    /// </summary>
    public class NovaFNFSaveManager : UdonSharpBehaviour
    {
        private const string Prefix = "NovaFNF.";
        private const string StoryUnlockKey = "NovaFNF.StoryUnlockedWeeks";
        private const string SongsPlayedKey = "NovaFNF.SongsPlayed";

        public bool dataRestored;
        public int unlockedStoryWeeks = 1;
        public int songsPlayed;
        public NovaFNFStoryProgression storyProgression;
        public NovaFNFOptionsController optionsController;

        public override void OnPlayerRestored(VRCPlayerApi player)
        {
            if (player == null || !player.isLocal) return;
            dataRestored = true;
            unlockedStoryWeeks = PlayerData.GetInt(player, StoryUnlockKey);
            if (unlockedStoryWeeks < 1) unlockedStoryWeeks = 1;
            songsPlayed = PlayerData.GetInt(player, SongsPlayedKey);
            if (storyProgression != null) storyProgression.RestoreUnlockCount(unlockedStoryWeeks);
            if (optionsController != null) LoadGameplaySettings(optionsController);
        }

        public bool SaveSongResult(string songId, string difficultyId, int score, int misses, float accuracy, int maxCombo)
        {
            VRCPlayerApi local = Networking.LocalPlayer;
            if (!dataRestored || local == null) return false;

            string stem = BuildRecordStem(songId, difficultyId);
            string scoreKey = stem + ".BestScore";
            string accuracyKey = stem + ".BestAccuracy";
            string missesKey = stem + ".BestMisses";
            string comboKey = stem + ".BestCombo";

            int previousScore = PlayerData.GetInt(local, scoreKey);
            if (score > previousScore) PlayerData.SetInt(scoreKey, score);

            float previousAccuracy = PlayerData.GetFloat(local, accuracyKey);
            if (accuracy > previousAccuracy) PlayerData.SetFloat(accuracyKey, accuracy);

            int previousMisses = PlayerData.GetInt(local, missesKey);
            if (!PlayerData.HasKey(local, missesKey) || misses < previousMisses)
                PlayerData.SetInt(missesKey, misses);

            int previousCombo = PlayerData.GetInt(local, comboKey);
            if (maxCombo > previousCombo) PlayerData.SetInt(comboKey, maxCombo);

            songsPlayed++;
            PlayerData.SetInt(SongsPlayedKey, songsPlayed);
            return true;
        }

        public int GetBestScore(string songId, string difficultyId)
        {
            VRCPlayerApi local = Networking.LocalPlayer;
            if (!dataRestored || local == null) return 0;
            return PlayerData.GetInt(local, BuildRecordStem(songId, difficultyId) + ".BestScore");
        }

        public float GetBestAccuracy(string songId, string difficultyId)
        {
            VRCPlayerApi local = Networking.LocalPlayer;
            if (!dataRestored || local == null) return 0f;
            return PlayerData.GetFloat(local, BuildRecordStem(songId, difficultyId) + ".BestAccuracy");
        }

        public int GetBestMisses(string songId, string difficultyId)
        {
            VRCPlayerApi local = Networking.LocalPlayer;
            if (!dataRestored || local == null) return 0;
            string key = BuildRecordStem(songId, difficultyId) + ".BestMisses";
            if (!PlayerData.HasKey(local, key)) return -1;
            return PlayerData.GetInt(local, key);
        }

        public int GetBestCombo(string songId, string difficultyId)
        {
            VRCPlayerApi local = Networking.LocalPlayer;
            if (!dataRestored || local == null) return 0;
            return PlayerData.GetInt(local, BuildRecordStem(songId, difficultyId) + ".BestCombo");
        }

        public bool IsFavorite(string songId)
        {
            VRCPlayerApi local = Networking.LocalPlayer;
            if (!dataRestored || local == null) return false;
            return PlayerData.GetBool(local, Prefix + "Favorite." + Normalize(songId));
        }

        public void SetFavorite(string songId, bool favorite)
        {
            VRCPlayerApi local = Networking.LocalPlayer;
            if (!dataRestored || local == null) return;
            PlayerData.SetBool(Prefix + "Favorite." + Normalize(songId), favorite);
        }

        public void SaveStoryUnlocks(int weekCount)
        {
            VRCPlayerApi local = Networking.LocalPlayer;
            if (!dataRestored || local == null) return;
            unlockedStoryWeeks = Mathf.Max(unlockedStoryWeeks, Mathf.Max(1, weekCount));
            PlayerData.SetInt(StoryUnlockKey, unlockedStoryWeeks);
        }

        public int LoadStoryUnlocks()
        {
            return dataRestored ? Mathf.Max(1, unlockedStoryWeeks) : 1;
        }

        public void SaveGameplaySettings(bool downscroll, bool ghostTapping, float scrollSpeed,
            float inputOffsetMs, bool cameraZooms, bool flashingLights,
            float instrumentalVolume, float vocalsVolume, float menuVolume, float sfxVolume)
        {
            VRCPlayerApi local = Networking.LocalPlayer;
            if (!dataRestored || local == null) return;

            PlayerData.SetBool(Prefix + "Settings.Downscroll", downscroll);
            PlayerData.SetBool(Prefix + "Settings.GhostTapping", ghostTapping);
            PlayerData.SetFloat(Prefix + "Settings.ScrollSpeed", scrollSpeed);
            PlayerData.SetFloat(Prefix + "Settings.InputOffsetMs", inputOffsetMs);
            PlayerData.SetBool(Prefix + "Settings.CameraZooms", cameraZooms);
            PlayerData.SetBool(Prefix + "Settings.FlashingLights", flashingLights);
            PlayerData.SetFloat(Prefix + "Settings.InstrumentalVolume", instrumentalVolume);
            PlayerData.SetFloat(Prefix + "Settings.VocalsVolume", vocalsVolume);
            PlayerData.SetFloat(Prefix + "Settings.MenuVolume", menuVolume);
            PlayerData.SetFloat(Prefix + "Settings.SFXVolume", sfxVolume);
        }

        public void LoadGameplaySettings(NovaFNFOptionsController target)
        {
            VRCPlayerApi local = Networking.LocalPlayer;
            if (!dataRestored || local == null || target == null) return;

            string speedKey = Prefix + "Settings.ScrollSpeed";
            if (!PlayerData.HasKey(local, speedKey))
            {
                target.ApplySavedSettings();
                return;
            }

            target.downscroll = PlayerData.GetBool(local, Prefix + "Settings.Downscroll");
            target.ghostTapping = PlayerData.GetBool(local, Prefix + "Settings.GhostTapping");
            target.scrollSpeedMultiplier = Mathf.Clamp(PlayerData.GetFloat(local, speedKey), 0.5f, 2f);
            target.inputOffsetMs = Mathf.Clamp(PlayerData.GetFloat(local, Prefix + "Settings.InputOffsetMs"), -250f, 250f);
            target.cameraZooms = PlayerData.GetBool(local, Prefix + "Settings.CameraZooms");
            target.flashingLights = PlayerData.GetBool(local, Prefix + "Settings.FlashingLights");
            target.instrumentalVolume = PlayerData.GetFloat(local, Prefix + "Settings.InstrumentalVolume");
            target.vocalsVolume = PlayerData.GetFloat(local, Prefix + "Settings.VocalsVolume");
            target.menuMusicVolume = PlayerData.GetFloat(local, Prefix + "Settings.MenuVolume");
            target.soundEffectVolume = PlayerData.GetFloat(local, Prefix + "Settings.SFXVolume");
            target.ApplySavedSettings();
        }

        private string BuildRecordStem(string songId, string difficultyId)
        {
            return Prefix + "Record." + Normalize(songId) + "." + Normalize(difficultyId);
        }

        private string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "unknown";
            return value.Trim().ToLowerInvariant().Replace(" ", "-").Replace("_", "-");
        }
    }
}
