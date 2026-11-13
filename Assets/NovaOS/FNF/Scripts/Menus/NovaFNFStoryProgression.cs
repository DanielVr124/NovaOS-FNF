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
        public NovaFNFSaveManager saveManager;
        public string[] weekIds;
        public string[] weekTitles;
        public bool[] weekVisible;
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

        public void RestoreUnlockCount(int count)
        {
            int maximum = CountVisibleWeeks();
            unlockedWeekCount = Mathf.Clamp(count, 1, Mathf.Max(1, maximum));
        }

        public void SelectWeek(int weekIndex)
        {
            selectedWeekIndex = weekIndex;
        }

        public bool IsWeekUnlocked(int actualWeekIndex)
        {
            if (weekIds == null || actualWeekIndex < 0 || actualWeekIndex >= weekIds.Length)
                return false;
            if (weekVisible != null && actualWeekIndex < weekVisible.Length &&
                !weekVisible[actualWeekIndex])
                return false;

            int visibleOrdinal = GetVisibleWeekOrdinal(actualWeekIndex);
            return visibleOrdinal > 0 && visibleOrdinal <= unlockedWeekCount;
        }

        public int GetVisibleWeekOrdinal(int actualWeekIndex)
        {
            if (weekIds == null || actualWeekIndex < 0 || actualWeekIndex >= weekIds.Length)
                return 0;

            int ordinal = 0;
            int end = Mathf.Min(actualWeekIndex, weekIds.Length - 1);
            for (int i = 0; i <= end; i++)
            {
                if (weekVisible == null || i >= weekVisible.Length || weekVisible[i])
                    ordinal++;
            }
            return ordinal;
        }

        private int CountVisibleWeeks()
        {
            if (weekIds == null) return 0;
            int count = 0;
            for (int i = 0; i < weekIds.Length; i++)
                if (weekVisible == null || i >= weekVisible.Length || weekVisible[i])
                    count++;
            return count;
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

            if (!IsWeekUnlocked(weekIndex))
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
            int completedVisibleOrdinal = GetVisibleWeekOrdinal(currentWeekIndex);
            if (completedVisibleOrdinal == unlockedWeekCount &&
                unlockedWeekCount < CountVisibleWeeks())
                unlockedWeekCount++;

            if (saveManager != null) saveManager.SaveStoryUnlocks(unlockedWeekCount);
            if (menuController != null) menuController.OpenStoryMode();
        }
    }
}
