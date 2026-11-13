using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace NovaOS.FNF
{
    /// <summary>
    /// Populates the Freeplay difficulty screen from the imported chart bank.
    /// Variations remain separate selections; each button represents one chart difficulty.
    /// </summary>
    public class NovaFNFDifficultyListController : UdonSharpBehaviour
    {
        public NovaFNFChartBank chartBank;
        public NovaFNFMenuController menuController;
        public NovaFNFMenuAction[] difficultySlots;
        public Text[] difficultyLabels;
        public Text[] ratingLabels;
        public Text pageLabel;

        private string[] availableDifficulties;
        private string[] availableVariations;
        private int difficultyCount;

        public void BuildForSelectedSong()
        {
            if (chartBank == null || menuController == null ||
                chartBank.songIds == null || chartBank.difficultyIds == null)
            {
                difficultyCount = 0;
                RefreshSlots();
                return;
            }

            availableDifficulties = new string[chartBank.difficultyIds.Length];
            availableVariations = new string[chartBank.difficultyIds.Length];
            difficultyCount = 0;

            // Official FNF stores Erect and Nightmare in the separate "erect"
            // variation. Collect every variation for this song, then bind each
            // difficulty button to the variation that actually owns its chart.
            for (int i = 0; i < chartBank.difficultyIds.Length; i++)
            {
                if (i >= chartBank.songIds.Length) break;
                if (chartBank.songIds[i] != menuController.selectedSongId) continue;

                string variation = chartBank.variationIds != null && i < chartBank.variationIds.Length
                    ? chartBank.variationIds[i] : "default";
                string difficulty = chartBank.difficultyIds[i];
                if (string.IsNullOrEmpty(difficulty)) continue;

                int existing = FindDifficulty(difficulty);
                if (existing >= 0)
                {
                    // If a difficulty exists in multiple variations, prefer the
                    // default chart unless the existing entry is already default.
                    if (availableVariations[existing] != "default" && variation == "default")
                        availableVariations[existing] = variation;
                    continue;
                }

                availableDifficulties[difficultyCount] = difficulty;
                availableVariations[difficultyCount] = variation;
                difficultyCount++;
            }

            SortDifficulties();
            RefreshSlots();
        }

        private void RefreshSlots()
        {
            if (difficultySlots == null) return;
            int[] visibleIndices = new int[difficultyCount];
            for (int i = 0; i < difficultyCount; i++) visibleIndices[i] = i;

            int displayCount = Mathf.Min(difficultySlots.Length, difficultyCount);
            NovaFNFMenuAction[] actions = new NovaFNFMenuAction[displayCount];

            for (int slotIndex = 0; slotIndex < difficultySlots.Length; slotIndex++)
            {
                bool valid = slotIndex < displayCount;
                NovaFNFMenuAction slot = difficultySlots[slotIndex];

                if (slot != null)
                {
                    slot.gameObject.SetActive(valid);
                    if (valid)
                    {
                        slot.menuController = menuController;
                        slot.actionId = 7;
                        slot.songId = menuController.selectedSongId;
                        slot.variationId = availableVariations[visibleIndices[slotIndex]];
                        slot.difficultyId = availableDifficulties[visibleIndices[slotIndex]];
                        slot.SetLocked(false);
                        actions[slotIndex] = slot;
                    }
                }

                if (difficultyLabels != null && slotIndex < difficultyLabels.Length &&
                    difficultyLabels[slotIndex] != null)
                {
                    difficultyLabels[slotIndex].text = valid
                        ? FormatDifficulty(availableDifficulties[visibleIndices[slotIndex]])
                        : "";
                }

                if (ratingLabels != null && slotIndex < ratingLabels.Length &&
                    ratingLabels[slotIndex] != null)
                {
                    int rating = valid ? FindRating(
                        availableDifficulties[visibleIndices[slotIndex]],
                        availableVariations[visibleIndices[slotIndex]]) : 0;
                    ratingLabels[slotIndex].text = valid && rating > 0 ? rating + " / 10" : "";
                }
            }

            if (menuController != null) menuController.SetDifficultyActions(actions);
            if (pageLabel != null)
                pageLabel.text = difficultyCount == 0 ? "NO DIFFICULTIES" :
                    Mathf.Min(difficultyCount, difficultySlots.Length) + " AVAILABLE";
        }

        private int FindRating(string difficulty, string variationId)
        {
            if (chartBank == null || chartBank.difficultyRatings == null || chartBank.songIds == null) return 0;
            for (int i = 0; i < chartBank.songIds.Length; i++)
            {
                if (chartBank.songIds[i] != menuController.selectedSongId) continue;
                string variation = chartBank.variationIds != null && i < chartBank.variationIds.Length
                    ? chartBank.variationIds[i] : "default";
                if (variation != variationId) continue;
                if (i < chartBank.difficultyIds.Length && chartBank.difficultyIds[i] == difficulty &&
                    i < chartBank.difficultyRatings.Length)
                    return chartBank.difficultyRatings[i];
            }
            return 0;
        }

        private int FindDifficulty(string difficulty)
        {
            for (int i = 0; i < difficultyCount; i++)
                if (availableDifficulties[i] == difficulty) return i;
            return -1;
        }

        private void SortDifficulties()
        {
            if (availableDifficulties == null) return;
            for (int i = 0; i < difficultyCount - 1; i++)
            {
                for (int j = 0; j < difficultyCount - i - 1; j++)
                {
                    if (DifficultyRank(availableDifficulties[j]) > DifficultyRank(availableDifficulties[j + 1]))
                    {
                        string temp = availableDifficulties[j];
                        availableDifficulties[j] = availableDifficulties[j + 1];
                        availableDifficulties[j + 1] = temp;
                        string tempVariation = availableVariations[j];
                        availableVariations[j] = availableVariations[j + 1];
                        availableVariations[j + 1] = tempVariation;
                    }
                }
            }
        }

        private int DifficultyRank(string difficulty)
        {
            string value = difficulty.ToLowerInvariant();
            if (value == "easy") return 0;
            if (value == "normal") return 1;
            if (value == "hard") return 2;
            if (value == "erect") return 3;
            if (value == "nightmare") return 4;
            return 5;
        }

        private string FormatDifficulty(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Length == 1) return value.ToUpperInvariant();
            return value.Substring(0, 1).ToUpperInvariant() + value.Substring(1);
        }
    }
}
