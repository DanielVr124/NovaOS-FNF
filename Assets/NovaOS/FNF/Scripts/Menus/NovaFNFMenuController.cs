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
        public int difficultySelectScreenIndex = 10;

        [Header("Screen transitions")]
        public Animator[] screenAnimators;
        public bool animateScreenTransitions = true;
        public float screenTransitionSeconds = 0.22f;
        public string screenEnterTrigger = "Enter";
        public string screenExitTrigger = "Exit";

        [Header("Keyboard-selectable menu items")]
        public NovaFNFMenuAction[] mainMenuActions;
        public NovaFNFMenuAction[] storyModeActions;
        public NovaFNFMenuAction[] freeplayActions;
        public NovaFNFMenuAction[] difficultyActions;
        public NovaFNFMenuAction[] optionsActions;
        public NovaFNFMenuAction[] controlsActions;
        public NovaFNFMenuAction[] audioActions;
        public NovaFNFMenuAction[] accessibilityActions;

        [Header("Audio")]
        public AudioSource uiAudioSource;
        public NovaFNFUdonAudioPlayer menuAudioPlayer;
        public NovaFNFChartBank previewChartBank;
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
        public NovaFNFInputBindings inputBindings;
        public NovaFNFDifficultyListController difficultyList;
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
        private int pendingHideScreenIndex = -1;
        private float pendingHideAtTime;

        private void Start()
        {
            ShowScreen(titleScreenIndex, false);
            PlayMenuTrack(titleMusicId);
        }

        private void Update()
        {
            bool backPressed = inputBindings != null
                ? inputBindings.BackPressed()
                : Input.GetKeyDown(KeyCode.Escape);
            if (backPressed)
            {
                if (currentScreenIndex == gameplayScreenIndex && gameplayController != null)
                    gameplayController.PauseGame();
                else
                    GoBack();
                return;
            }

            bool upPressed = inputBindings != null
                ? inputBindings.MenuUpPressed()
                : Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
            bool downPressed = inputBindings != null
                ? inputBindings.MenuDownPressed()
                : Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
            bool acceptPressed = inputBindings != null
                ? inputBindings.AcceptPressed()
                : Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);

            if (upPressed) MoveSelection(-1);
            if (downPressed) MoveSelection(1);

            if (acceptPressed)
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
            PlayCurrentFreeplayPreview();
            PlayUiSound(confirmSound);
        }

        public void OpenDifficultySelect()
        {
            ShowScreen(difficultySelectScreenIndex, true);
            if (menuAudioPlayer != null) menuAudioPlayer.StopSongPreview();
            PlayUiSound(confirmSound);
            if (difficultyList != null) difficultyList.BuildForSelectedSong();
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

        public void OpenGameplay()
        {
            ShowScreen(gameplayScreenIndex, true);
            if (menuAudioPlayer != null) menuAudioPlayer.StopMenuMusic();
        }

        public void OpenResults()
        {
            ShowScreen(resultsScreenIndex, true);
            PlayMenuTrack(resultsMusicId);
        }

        public void SetDifficultyActions(NovaFNFMenuAction[] actions)
        {
            difficultyActions = actions;
            if (currentScreenIndex == difficultySelectScreenIndex)
            {
                currentActions = difficultyActions;
                selectedActionIndex = 0;
                RefreshHighlights();
            }
        }

        public void SetStoryActions(NovaFNFMenuAction[] actions)
        {
            storyModeActions = actions;
            if (currentScreenIndex == storyModeScreenIndex)
            {
                currentActions = storyModeActions;
                selectedActionIndex = 0;
                RefreshHighlights();
            }
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
            PlayCurrentFreeplayPreview();
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
                    PlayCurrentFreeplayPreview();
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
            else if (actionId == 12) OpenDifficultySelect();
        }

        private void ShowScreen(int screenIndex, bool pushHistory)
        {
            if (screenIndex != freeplayScreenIndex && menuAudioPlayer != null)
                menuAudioPlayer.StopSongPreview();

            if (screens == null || screenIndex < 0 || screenIndex >= screens.Length) return;
            if (pushHistory && currentScreenIndex >= 0 && currentScreenIndex != screenIndex && historyCount < screenHistory.Length)
            {
                screenHistory[historyCount] = currentScreenIndex;
                historyCount++;
            }

            int previousScreen = currentScreenIndex;
            bool keepPreviousForExit = animateScreenTransitions &&
                screenTransitionSeconds > 0f &&
                previousScreen >= 0 && previousScreen < screens.Length &&
                previousScreen != screenIndex && screens[previousScreen] != null;

            if (pendingHideScreenIndex >= 0 && pendingHideScreenIndex != previousScreen &&
                pendingHideScreenIndex != screenIndex && pendingHideScreenIndex < screens.Length &&
                screens[pendingHideScreenIndex] != null)
                screens[pendingHideScreenIndex].SetActive(false);

            for (int i = 0; i < screens.Length; i++)
            {
                if (screens[i] == null) continue;
                bool shouldKeep = i == screenIndex || (keepPreviousForExit && i == previousScreen);
                if (shouldKeep) screens[i].SetActive(true);
                else screens[i].SetActive(false);
            }

            if (keepPreviousForExit)
            {
                TriggerScreenAnimation(previousScreen, screenExitTrigger);
                pendingHideScreenIndex = previousScreen;
                pendingHideAtTime = Time.time + screenTransitionSeconds;
                SendCustomEventDelayedSeconds("HideOutgoingScreen", screenTransitionSeconds);
            }
            else
            {
                pendingHideScreenIndex = -1;
            }

            currentScreenIndex = screenIndex;
            if (animateScreenTransitions) TriggerScreenAnimation(screenIndex, screenEnterTrigger);
            selectedActionIndex = 0;
            currentActions = ActionsForScreen(screenIndex);
            RefreshHighlights();
        }

        public void HideOutgoingScreen()
        {
            if (Time.time < pendingHideAtTime) return;
            int hideIndex = pendingHideScreenIndex;
            pendingHideScreenIndex = -1;
            if (hideIndex < 0 || hideIndex >= screens.Length || hideIndex == currentScreenIndex) return;
            if (screens[hideIndex] != null) screens[hideIndex].SetActive(false);
        }

        private void TriggerScreenAnimation(int screenIndex, string triggerName)
        {
            if (screenAnimators == null || screenIndex < 0 || screenIndex >= screenAnimators.Length) return;
            if (screenAnimators[screenIndex] == null || string.IsNullOrEmpty(triggerName)) return;
            screenAnimators[screenIndex].SetTrigger(triggerName);
        }

        private NovaFNFMenuAction[] ActionsForScreen(int screenIndex)
        {
            if (screenIndex == mainMenuScreenIndex) return mainMenuActions;
            if (screenIndex == difficultySelectScreenIndex) return difficultyActions;
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

        private void PlayCurrentFreeplayPreview()
        {
            if (currentScreenIndex != freeplayScreenIndex ||
                currentActions == null || selectedActionIndex < 0 ||
                selectedActionIndex >= currentActions.Length)
            {
                if (menuAudioPlayer != null) menuAudioPlayer.StopSongPreview();
                return;
            }

            NovaFNFMenuAction action = currentActions[selectedActionIndex];
            if (action == null || action.actionId != 12 ||
                string.IsNullOrEmpty(action.songId))
            {
                if (menuAudioPlayer != null) menuAudioPlayer.StopSongPreview();
                return;
            }

            if (previewChartBank == null || previewChartBank.songIds == null)
            {
                if (menuAudioPlayer != null) menuAudioPlayer.PlaySongPreview(action.songId, 0f, 0f);
                return;
            }

            for (int i = 0; i < previewChartBank.songIds.Length; i++)
            {
                if (previewChartBank.songIds[i] != action.songId) continue;
                string variation = previewChartBank.variationIds != null && i < previewChartBank.variationIds.Length
                    ? previewChartBank.variationIds[i] : "default";
                if (variation != action.variationId) continue;

                float start = previewChartBank.previewStartSeconds != null && i < previewChartBank.previewStartSeconds.Length
                    ? previewChartBank.previewStartSeconds[i] : 0f;
                float end = previewChartBank.previewEndSeconds != null && i < previewChartBank.previewEndSeconds.Length
                    ? previewChartBank.previewEndSeconds[i] : 0f;
                if (menuAudioPlayer != null)
                    menuAudioPlayer.PlaySongPreview(action.songId, start, end);
                return;
            }

            if (menuAudioPlayer != null) menuAudioPlayer.PlaySongPreview(action.songId, 0f, 0f);
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
