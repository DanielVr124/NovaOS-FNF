using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace NovaOS.FNF
{
    /// <summary>
    /// Builds a paged Freeplay list from imported chart metadata. Pre-create the
    /// visible row/button slots in the scene; this component fills them at runtime.
    /// </summary>
    public class NovaFNFFreeplayListController : UdonSharpBehaviour
    {
        public NovaFNFChartBank chartBank;
        public NovaFNFMenuController menuController;
        public NovaFNFMenuAction[] songSlots;
        public Text[] songNameLabels;
        public Text[] artistLabels;
        public Text pageLabel;

        private string[] uniqueSongIds;
        private string[] uniqueSongNames;
        private string[] uniqueArtists;
        private string[] uniqueVariations;
        private string[] defaultDifficulties;
        private int songCount;
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
                if (string.IsNullOrEmpty(songId) || HasSong(songId)) continue;

                uniqueSongIds[songCount] = songId;
                uniqueSongNames[songCount] = (chartBank.songNames != null && i < chartBank.songNames.Length)
                    ? chartBank.songNames[i] : songId;
                uniqueArtists[songCount] = (chartBank.artists != null && i < chartBank.artists.Length)
                    ? chartBank.artists[i] : "";
                uniqueVariations[songCount] = (chartBank.variationIds != null && i < chartBank.variationIds.Length)
                    ? chartBank.variationIds[i] : "default";
                defaultDifficulties[songCount] = FindPreferredDifficulty(songId, uniqueVariations[songCount]);
                songCount++;
            }

            int slotsPerPage = songSlots == null ? 0 : songSlots.Length;
            pageCount = slotsPerPage > 0 ? Mathf.CeilToInt((float)songCount / slotsPerPage) : 0;
            currentPage = 0;
            if (menuController != null && songSlots != null)
                menuController.SetFreeplayActions(songSlots);
            RefreshPage();
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

            for (int slotIndex = 0; slotIndex < songSlots.Length; slotIndex++)
            {
                int songIndex = start + slotIndex;
                bool valid = songIndex < songCount;
                NovaFNFMenuAction slot = songSlots[slotIndex];

                if (slot != null)
                {
                    slot.gameObject.SetActive(valid);
                    if (valid)
                    {
                        slot.menuController = menuController;
                        slot.actionId = 7;
                        slot.songId = uniqueSongIds[songIndex];
                        slot.variationId = uniqueVariations[songIndex];
                        slot.difficultyId = defaultDifficulties[songIndex];
                    }
                }

                if (songNameLabels != null && slotIndex < songNameLabels.Length && songNameLabels[slotIndex] != null)
                    songNameLabels[slotIndex].text = valid ? uniqueSongNames[songIndex] : "";

                if (artistLabels != null && slotIndex < artistLabels.Length && artistLabels[slotIndex] != null)
                    artistLabels[slotIndex].text = valid ? uniqueArtists[songIndex] : "";
            }

            if (pageLabel != null)
                pageLabel.text = pageCount == 0 ? "0 / 0" : (currentPage + 1) + " / " + pageCount;
        }

        private bool HasSong(string songId)
        {
            for (int i = 0; i < songCount; i++)
                if (uniqueSongIds[i] == songId) return true;
            return false;
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
