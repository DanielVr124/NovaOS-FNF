using UdonSharp;
using UnityEngine;

namespace NovaOS.FNF
{
    /// <summary>
    /// Data-driven Story Mode week playlist. Populate the flattened song arrays
    /// in the Inspector; the Continue button on the results screen calls ContinueAfterResults.
    /// </summary>
    public class NovaFNFStoryProgression : UdonSharpBehaviour
    {
        public NovaFNFMenuController menuController;
        public string[] weekIds;
        public string[] weekTitles;
        public int[] weekSongStarts;
        public int[] weekSongCounts;

        [Header("Flattened song entries, in week order")]
        public string[] weekSongIds;
        public string[] weekSongVariations;
        public string[] weekSongDifficulties;

        [Header("Progress")]
        public int unlockedWeekCount = 1;
        public int selectedWeekIndex;
        public int currentWeekIndex = -1;
        public int currentSongInWeek;
        public bool storyRunActive;
        public bool waitingForResultsContinue;

        public void SelectWeek(int weekIndex)
        {
            selectedWeekIndex = weekIndex;
        }

        public void BeginSelectedWeek()
        {
            BeginWeek(selectedWeekIndex);
        }

        public void BeginWeek(int weekIndex)
        {
            if (weekIds == null || weekSongStarts == null || weekSongCounts == null ||
                weekSongIds == null || weekIndex < 0 || weekIndex >= weekIds.Length ||
                weekIndex >= weekSongStarts.Length || weekIndex >= weekSongCounts.Length)
                return;

            if (weekIndex >= unlockedWeekCount)
            {
                Debug.Log("That Story Mode week is still locked.");
                return;
            }

            currentWeekIndex = weekIndex;
            currentSongInWeek = 0;
            storyRunActive = true;
            waitingForResultsContinue = false;
            PlayCurrentSong();
        }

        public void ContinueAfterResults()
        {
            if (!storyRunActive || !waitingForResultsContinue) return;
            waitingForResultsContinue = false;
            int songCount = weekSongCounts[currentWeekIndex];

            if (currentSongInWeek + 1 < songCount)
            {
                currentSongInWeek++;
                PlayCurrentSong();
            }
            else
            {
                CompleteCurrentWeek();
            }
        }

        public void MarkCurrentSongFinished()
        {
            if (storyRunActive)
                waitingForResultsContinue = true;
        }

        public void CancelStoryRun()
        {
            storyRunActive = false;
            waitingForResultsContinue = false;
            currentWeekIndex = -1;
            if (menuController != null) menuController.OpenStoryMode();
        }

        private void PlayCurrentSong()
        {
            if (menuController == null || currentWeekIndex < 0) return;
            int start = weekSongStarts[currentWeekIndex];
            int songCount = weekSongCounts[currentWeekIndex];
            int flattenedIndex = start + currentSongInWeek;

            if (currentSongInWeek < 0 || currentSongInWeek >= songCount ||
                flattenedIndex < 0 || flattenedIndex >= weekSongIds.Length)
                return;

            menuController.SetSelectedSong(weekSongIds[flattenedIndex]);
            if (weekSongVariations != null && flattenedIndex < weekSongVariations.Length)
                menuController.SetSelectedVariation(weekSongVariations[flattenedIndex]);
            if (weekSongDifficulties != null && flattenedIndex < weekSongDifficulties.Length)
                menuController.SetSelectedDifficulty(weekSongDifficulties[flattenedIndex]);

            menuController.StartSelectedSong();
        }

        private void CompleteCurrentWeek()
        {
            storyRunActive = false;
            waitingForResultsContinue = false;
            if (currentWeekIndex + 1 == unlockedWeekCount && unlockedWeekCount < weekIds.Length)
                unlockedWeekCount++;

            if (menuController != null) menuController.OpenStoryMode();
        }
    }
}
