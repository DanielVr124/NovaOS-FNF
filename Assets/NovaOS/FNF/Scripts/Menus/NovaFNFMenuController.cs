using UdonSharp;
using UnityEngine;

namespace NovaOS.FNF
{
    /// <summary>
    /// Screen stack and keyboard/UI navigation for the NovaOS FNF menu flow.
    /// Assign screens and action arrays in the Inspector. UI Buttons call
    /// NovaFNFMenuAction.OnButtonPressed; keyboard players use Up/Down, Enter and Escape.
    /// </summary>
    public class NovaFNFMenuController : UdonSharpBehaviour
    {
        [Header("Screens (keep indices consistent with the fields below)")]
        public GameObject[] screens;
        public int titleScreenIndex;
        public int mainMenuScreenIndex = 1;
        public int storyModeScreenIndex = 2;
        public int freeplayScreenIndex = 3;
        public int optionsScreenIndex = 4;
        public int controlsScreenIndex = 5;
        public int audioOptionsScreenIndex = 6;
        public int accessibilityScreenIndex = 7;
        public int gameplayScreenIndex = 8;
        public int resultsScreenIndex = 9;

        [Header("Keyboard-selectable menu items")]
        public NovaFNFMenuAction[] mainMenuActions;
        public NovaFNFMenuAction[] storyModeActions;
        public NovaFNFMenuAction[] freeplayActions;
        public NovaFNFMenuAction[] optionsActions;
        public NovaFNFMenuAction[] controlsActions;
        public NovaFNFMenuAction[] audioActions;
        public NovaFNFMenuAction[] accessibilityActions;

        [Header("Audio")]
        public AudioSource uiAudioSource;
        public NovaFNFUdonAudioPlayer menuAudioPlayer;
        public string titleMusicId;
        public string mainMenuMusicId;
        public string storyModeMusicId;
        public string freeplayMusicId;
        public string optionsMusicId;
        public string resultsMusicId;
        public AudioClip moveSound;
        public AudioClip confirmSound;
        public AudioClip cancelSound;

        [Header("Gameplay handoff")]
        public NovaFNFGameplayController gameplayController;
        public NovaFNFStoryProgression storyProgression;
        public int selectedWeekIndex;
        public string selectedSongId = "";
        public string selectedVariationId = "default";
        public string selectedDifficultyId = "normal";

        private int currentScreenIndex = -1;
        private int[] screenHistory = new int[16];
        private int historyCount;
        private NovaFNFMenuAction[] currentActions;
        private int selectedActionIndex;

        private void Start()
        {
            ShowScreen(titleScreenIndex, false);
            PlayMenuTrack(titleMusicId);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentScreenIndex == gameplayScreenIndex && gameplayController != null)
                    gameplayController.PauseGame();
                else
                    GoBack();
                return;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                MoveSelection(-1);

            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                MoveSelection(1);

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                if (currentScreenIndex == titleScreenIndex)
                    OpenMainMenu();
                else if (currentScreenIndex != gameplayScreenIndex)
                    ConfirmSelection();
            }
        }

        public void OpenTitle()
        {
            ShowScreen(titleScreenIndex, true);
            PlayMenuTrack(titleMusicId);
        }

        public void OpenMainMenu()
        {
            ShowScreen(mainMenuScreenIndex, true);
            PlayMenuTrack(mainMenuMusicId);
            PlayUiSound(confirmSound);
        }

        public void OpenStoryMode()
        {
            ShowScreen(storyModeScreenIndex, true);
            PlayMenuTrack(storyModeMusicId);
            PlayUiSound(confirmSound);
        }

        public void OpenFreeplay()
        {
            ShowScreen(freeplayScreenIndex, true);
            PlayMenuTrack(freeplayMusicId);
            PlayUiSound(confirmSound);
        }

        public void OpenOptions()
        {
            ShowScreen(optionsScreenIndex, true);
            PlayMenuTrack(optionsMusicId);
            PlayUiSound(confirmSound);
        }

        public void OpenControls()
        {
            ShowScreen(controlsScreenIndex, true);
            PlayMenuTrack(optionsMusicId);
            PlayUiSound(confirmSound);
        }

        public void OpenAudioOptions()
        {
            ShowScreen(audioOptionsScreenIndex, true);
            PlayMenuTrack(optionsMusicId);
            PlayUiSound(confirmSound);
        }

        public void OpenAccessibilityOptions()
        {
            ShowScreen(accessibilityScreenIndex, true);
            PlayMenuTrack(optionsMusicId);
            PlayUiSound(confirmSound);
        }

        public void StartSelectedSong()
        {
            if (gameplayController == null || string.IsNullOrEmpty(selectedSongId))
            {
                Debug.LogWarning("Choose a song and assign NovaFNFGameplayController before starting gameplay.");
                return;
            }

            gameplayController.SetSongSelection(selectedSongId, selectedVariationId, selectedDifficultyId);
            ShowScreen(gameplayScreenIndex, true);
            if (menuAudioPlayer != null) menuAudioPlayer.StopMenuMusic();
            gameplayController.StartSong();
            PlayUiSound(confirmSound);
        }

        public void OpenResults()
        {
            ShowScreen(resultsScreenIndex, true);
            PlayMenuTrack(resultsMusicId);
        }

        public void SetFreeplayActions(NovaFNFMenuAction[] actions)
        {
            freeplayActions = actions;
            if (currentScreenIndex == freeplayScreenIndex)
            {
                currentActions = freeplayActions;
                selectedActionIndex = 0;
                RefreshHighlights();
            }
        }

        public void SetSelectedWeek(int weekIndex)
        {
            selectedWeekIndex = weekIndex;
            if (storyProgression != null) storyProgression.SelectWeek(weekIndex);
        }

        public void SetSelectedSong(string songId)
        {
            selectedSongId = songId;
        }

        public void SetSelectedVariation(string variationId)
        {
            selectedVariationId = variationId;
        }

        public void SetSelectedDifficulty(string difficultyId)
        {
            selectedDifficultyId = difficultyId;
        }

        public void GoBack()
        {
            if (historyCount > 0)
            {
                historyCount--;
                int previous = screenHistory[historyCount];
                ShowScreen(previous, false);
                PlayScreenMusic(previous);
                PlayUiSound(cancelSound);
            }
            else if (currentScreenIndex != mainMenuScreenIndex)
            {
                ShowScreen(mainMenuScreenIndex, false);
                PlayMenuTrack(mainMenuMusicId);
                PlayUiSound(cancelSound);
            }
        }

        public void MoveSelection(int direction)
        {
            if (currentActions == null || currentActions.Length == 0) return;
            selectedActionIndex += direction;
            if (selectedActionIndex < 0) selectedActionIndex = currentActions.Length - 1;
            if (selectedActionIndex >= currentActions.Length) selectedActionIndex = 0;
            RefreshHighlights();
            PlayUiSound(moveSound);
        }

        public void SelectAction(NovaFNFMenuAction action)
        {
            if (currentActions == null || action == null) return;
            for (int i = 0; i < currentActions.Length; i++)
            {
                if (currentActions[i] == action)
                {
                    selectedActionIndex = i;
                    RefreshHighlights();
                    return;
                }
            }
        }

        public void ConfirmSelection()
        {
            if (currentActions == null || currentActions.Length == 0) return;
            NovaFNFMenuAction action = currentActions[selectedActionIndex];
            if (action != null) action.OnButtonPressed();
        }

        public void RunAction(int actionId)
        {
            if (actionId == 0) OpenStoryMode();
            else if (actionId == 1) OpenFreeplay();
            else if (actionId == 2) OpenOptions();
            else if (actionId == 3) GoBack();
            else if (actionId == 4) OpenControls();
            else if (actionId == 5) OpenAudioOptions();
            else if (actionId == 6) OpenAccessibilityOptions();
            else if (actionId == 7) StartSelectedSong();
            else if (actionId == 8) OpenMainMenu();
            else if (actionId == 9) OpenResults();
            else if (actionId == 10) OpenTitle();
            else if (actionId == 11 && storyProgression != null) storyProgression.BeginSelectedWeek();
        }

        private void ShowScreen(int screenIndex, bool pushHistory)
        {
            if (screens == null || screenIndex < 0 || screenIndex >= screens.Length) return;
            if (pushHistory && currentScreenIndex >= 0 && currentScreenIndex != screenIndex && historyCount < screenHistory.Length)
            {
                screenHistory[historyCount] = currentScreenIndex;
                historyCount++;
            }

            for (int i = 0; i < screens.Length; i++)
                if (screens[i] != null) screens[i].SetActive(i == screenIndex);

            currentScreenIndex = screenIndex;
            selectedActionIndex = 0;
            currentActions = ActionsForScreen(screenIndex);
            RefreshHighlights();
        }

        private NovaFNFMenuAction[] ActionsForScreen(int screenIndex)
        {
            if (screenIndex == mainMenuScreenIndex) return mainMenuActions;
            if (screenIndex == storyModeScreenIndex) return storyModeActions;
            if (screenIndex == freeplayScreenIndex) return freeplayActions;
            if (screenIndex == optionsScreenIndex) return optionsActions;
            if (screenIndex == controlsScreenIndex) return controlsActions;
            if (screenIndex == audioOptionsScreenIndex) return audioActions;
            if (screenIndex == accessibilityScreenIndex) return accessibilityActions;
            return null;
        }

        private void RefreshHighlights()
        {
            if (currentActions == null) return;
            for (int i = 0; i < currentActions.Length; i++)
                if (currentActions[i] != null)
                    currentActions[i].SetHighlighted(i == selectedActionIndex);
        }

        private void PlayScreenMusic(int screenIndex)
        {
            if (screenIndex == titleScreenIndex) PlayMenuTrack(titleMusicId);
            else if (screenIndex == mainMenuScreenIndex) PlayMenuTrack(mainMenuMusicId);
            else if (screenIndex == storyModeScreenIndex) PlayMenuTrack(storyModeMusicId);
            else if (screenIndex == freeplayScreenIndex) PlayMenuTrack(freeplayMusicId);
            else if (screenIndex == optionsScreenIndex || screenIndex == controlsScreenIndex ||
                     screenIndex == audioOptionsScreenIndex || screenIndex == accessibilityScreenIndex)
                PlayMenuTrack(optionsMusicId);
            else if (screenIndex == resultsScreenIndex) PlayMenuTrack(resultsMusicId);
        }

        private void PlayMenuTrack(string trackId)
        {
            if (menuAudioPlayer != null && !string.IsNullOrEmpty(trackId))
                menuAudioPlayer.PlayMenuMusic(trackId);
        }

        private void PlayUiSound(AudioClip clip)
        {
            if (uiAudioSource != null && clip != null)
                uiAudioSource.PlayOneShot(clip);
        }
    }
}
