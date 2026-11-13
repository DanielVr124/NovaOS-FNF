using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace NovaOS.FNF
{
    /// <summary>
    /// Builds a paged Freeplay list from imported chart metadata. It supports
    /// favorites, best-score display, and score sorting using the PlayerData save manager.
    /// All row objects are pre-created in the scene for VRChat-safe runtime use.
    /// </summary>
    public class NovaFNFFreeplayListController : UdonSharpBehaviour
    {
        public NovaFNFChartBank chartBank;
        public NovaFNFMenuController menuController;
        public NovaFNFSaveManager saveManager;
        public NovaFNFMenuAction[] songSlots;
        public Text[] songNameLabels;
        public Text[] artistLabels;
        public Text[] bestScoreLabels;
        public NovaFNFFavoriteButton[] favoriteButtons;
        public Text pageLabel;

        [Header("Freeplay filters")]
        public bool favoritesOnly;
        public bool sortByHighScore;

        private string[] uniqueSongIds;
        private string[] uniqueSongNames;
        private string[] uniqueArtists;
        private string[] uniqueVariations;
        private string[] defaultDifficulties;
        private int[] visibleSongIndices;
        private int songCount;
        private int visibleSongCount;
        private int currentPage;
        private int pageCount;

        private void Start()
        {
            BuildSongList();
        }

        public void BuildSongList()
        {
            if (chartBank == null || chartBank.songIds == null)
            {
                songCount = 0;
                visibleSongCount = 0;
                RefreshPage();
                return;
            }

            int capacity = chartBank.songIds.Length;
            uniqueSongIds = new string[capacity];
            uniqueSongNames = new string[capacity];
            uniqueArtists = new string[capacity];
            uniqueVariations = new string[capacity];
            defaultDifficulties = new string[capacity];
            songCount = 0;

            for (int i = 0; i < chartBank.songIds.Length; i++)
            {
                string songId = chartBank.songIds[i];
                string variationId = (chartBank.variationIds != null && i < chartBank.variationIds.Length)
                    ? chartBank.variationIds[i] : "default";
                if (string.IsNullOrEmpty(songId)) continue;

                // A song appears once in Freeplay. Its alternate variations
                // (Erect/Pico/etc.) are exposed by the difficulty selector.
                int existingSong = FindSong(songId);
                string songName = (chartBank.songNames != null && i < chartBank.songNames.Length)
                    ? chartBank.songNames[i] : songId;
                string artist = (chartBank.artists != null && i < chartBank.artists.Length)
                    ? chartBank.artists[i] : "";

                if (existingSong >= 0)
                {
                    // Prefer the default variation for the row's display metadata.
                    if (uniqueVariations[existingSong] != "default" && variationId == "default")
                    {
                        uniqueSongNames[existingSong] = songName;
                        uniqueArtists[existingSong] = artist;
                        uniqueVariations[existingSong] = variationId;
                    }
                    continue;
                }

                uniqueSongIds[songCount] = songId;
                uniqueSongNames[songCount] = songName;
                uniqueArtists[songCount] = artist;
                uniqueVariations[songCount] = variationId;
                defaultDifficulties[songCount] = FindPreferredDifficulty(songId, variationId);
                songCount++;
            }

            RebuildVisibleList();
        }

        public void ShowAllSongs()
        {
            favoritesOnly = false;
            RebuildVisibleList();
        }

        public void ShowFavorites()
        {
            favoritesOnly = true;
            RebuildVisibleList();
        }

        public void ToggleFavoritesOnly()
        {
            favoritesOnly = !favoritesOnly;
            RebuildVisibleList();
        }

        public void ToggleHighScoreSort()
        {
            sortByHighScore = !sortByHighScore;
            RebuildVisibleList();
        }

        public void RefreshFavorites()
        {
            RebuildVisibleList();
        }

        public void NextPage()
        {
            if (pageCount <= 1) return;
            currentPage++;
            if (currentPage >= pageCount) currentPage = 0;
            RefreshPage();
        }

        public void PreviousPage()
        {
            if (pageCount <= 1) return;
            currentPage--;
            if (currentPage < 0) currentPage = pageCount - 1;
            RefreshPage();
        }

        public void RefreshPage()
        {
            if (songSlots == null) return;
            int start = currentPage * songSlots.Length;
            int pageVisibleCount = Mathf.Min(songSlots.Length, Mathf.Max(0, visibleSongCount - start));
            NovaFNFMenuAction[] pageActions = new NovaFNFMenuAction[pageVisibleCount];

            for (int slotIndex = 0; slotIndex < songSlots.Length; slotIndex++)
            {
                int visibleIndex = start + slotIndex;
                bool valid = visibleIndex < visibleSongCount;
                int songIndex = valid ? visibleSongIndices[visibleIndex] : -1;
                NovaFNFMenuAction slot = songSlots[slotIndex];

                if (slot != null)
                {
                    slot.gameObject.SetActive(valid);
                    if (valid)
                    {
                        slot.menuController = menuController;
                        slot.actionId = 12;
                        slot.songId = uniqueSongIds[songIndex];
                        slot.variationId = uniqueVariations[songIndex];
                        slot.difficultyId = defaultDifficulties[songIndex];
                        pageActions[slotIndex < pageVisibleCount ? slotIndex : 0] = slot;
                    }
                }

                if (songNameLabels != null && slotIndex < songNameLabels.Length && songNameLabels[slotIndex] != null)
                    songNameLabels[slotIndex].text = valid ? uniqueSongNames[songIndex] : "";

                if (artistLabels != null && slotIndex < artistLabels.Length && artistLabels[slotIndex] != null)
                    artistLabels[slotIndex].text = valid ? uniqueArtists[songIndex] : "";

                if (bestScoreLabels != null && slotIndex < bestScoreLabels.Length && bestScoreLabels[slotIndex] != null)
                {
                    int bestScore = valid && saveManager != null
                        ? saveManager.GetBestScore(uniqueSongIds[songIndex] + "-" + uniqueVariations[songIndex], defaultDifficulties[songIndex])
                        : 0;
                    bestScoreLabels[slotIndex].text = valid ? "BEST " + bestScore : "";
                }

                if (favoriteButtons != null && slotIndex < favoriteButtons.Length && favoriteButtons[slotIndex] != null)
                {
                    favoriteButtons[slotIndex].freeplayList = this;
                    favoriteButtons[slotIndex].saveManager = saveManager;
                    favoriteButtons[slotIndex].songId = valid ? uniqueSongIds[songIndex] : "";
                    favoriteButtons[slotIndex].variationId = valid ? uniqueVariations[songIndex] : "";
                    favoriteButtons[slotIndex].Refresh();
                }
            }

            if (menuController != null)
                menuController.SetFreeplayActions(pageActions);

            if (pageLabel != null)
                pageLabel.text = pageCount == 0 ? "0 / 0" : (currentPage + 1) + " / " + pageCount;
        }

        public void ToggleFavorite(string songId, string variationId)
        {
            if (saveManager == null || string.IsNullOrEmpty(songId)) return;
            string favoriteKey = FavoriteKey(songId, variationId);
            saveManager.SetFavorite(favoriteKey, !saveManager.IsFavorite(favoriteKey));
            RebuildVisibleList();
        }

        private void RebuildVisibleList()
        {
            if (visibleSongIndices == null || visibleSongIndices.Length != songCount)
                visibleSongIndices = new int[songCount];

            visibleSongCount = 0;
            for (int i = 0; i < songCount; i++)
            {
                bool include = !favoritesOnly || (saveManager != null &&
                    saveManager.IsFavorite(FavoriteKey(uniqueSongIds[i], uniqueVariations[i])));
                if (include)
                    visibleSongIndices[visibleSongCount++] = i;
            }

            if (sortByHighScore && saveManager != null)
            {
                for (int i = 0; i < visibleSongCount - 1; i++)
                {
                    for (int j = 0; j < visibleSongCount - i - 1; j++)
                    {
                        int left = visibleSongIndices[j];
                        int right = visibleSongIndices[j + 1];
                        int leftScore = saveManager.GetBestScore(uniqueSongIds[left] + "-" + uniqueVariations[left], defaultDifficulties[left]);
                        int rightScore = saveManager.GetBestScore(uniqueSongIds[right] + "-" + uniqueVariations[right], defaultDifficulties[right]);
                        if (rightScore > leftScore)
                        {
                            visibleSongIndices[j] = right;
                            visibleSongIndices[j + 1] = left;
                        }
                    }
                }
            }

            int slotsPerPage = songSlots == null ? 0 : songSlots.Length;
            pageCount = slotsPerPage > 0 ? Mathf.CeilToInt((float)visibleSongCount / slotsPerPage) : 0;
            currentPage = 0;
            RefreshPage();
        }

        private int FindSong(string songId)
        {
            for (int i = 0; i < songCount; i++)
                if (uniqueSongIds[i] == songId) return i;
            return -1;
        }

        private string FavoriteKey(string songId, string variationId)
        {
            return songId + "-" + variationId;
        }

        private string FindPreferredDifficulty(string songId, string variationId)
        {
            if (chartBank == null || chartBank.songIds == null) return "normal";
            string first = "normal";
            bool found = false;

            for (int i = 0; i < chartBank.songIds.Length; i++)
            {
                if (chartBank.songIds[i] != songId) continue;
                string variation = (chartBank.variationIds != null && i < chartBank.variationIds.Length)
                    ? chartBank.variationIds[i] : "default";
                if (variation != variationId) continue;

                string difficulty = (chartBank.difficultyIds != null && i < chartBank.difficultyIds.Length)
                    ? chartBank.difficultyIds[i] : "normal";
                if (!found)
                {
                    first = difficulty;
                    found = true;
                }
                if (difficulty == "normal") return "normal";
            }

            return first;
        }
    }
}
