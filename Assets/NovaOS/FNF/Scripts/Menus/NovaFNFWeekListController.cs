using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace NovaOS.FNF
{
    /// <summary>
    /// Creates a paged Story Mode week list from the imported level definitions.
    /// Invisible levels remain in the data for Freeplay but are omitted here.
    /// </summary>
    public class NovaFNFWeekListController : UdonSharpBehaviour
    {
        public NovaFNFStoryProgression storyProgression;
        public NovaFNFMenuController menuController;
        public NovaFNFMenuAction[] weekSlots;
        public Text[] weekTitleLabels;
        public Text pageLabel;

        private int[] visibleWeekIndices;
        private int visibleWeekCount;
        private int currentPage;
        private int pageCount;

        private void Start()
        {
            BuildWeekList();
        }

        public void BuildWeekList()
        {
            if (storyProgression == null || storyProgression.weekIds == null)
            {
                visibleWeekIndices = new int[0];
                visibleWeekCount = 0;
                RefreshPage();
                return;
            }

            visibleWeekIndices = new int[storyProgression.weekIds.Length];
            visibleWeekCount = 0;

            for (int i = 0; i < storyProgression.weekIds.Length; i++)
            {
                bool visible = storyProgression.weekVisible == null ||
                    i >= storyProgression.weekVisible.Length ||
                    storyProgression.weekVisible[i];

                if (visible)
                {
                    visibleWeekIndices[visibleWeekCount] = i;
                    visibleWeekCount++;
                }
            }

            int slotCount = weekSlots == null ? 0 : weekSlots.Length;
            pageCount = slotCount > 0 ? Mathf.CeilToInt((float)visibleWeekCount / slotCount) : 0;
            currentPage = 0;

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
            if (weekSlots == null) return;
            int start = currentPage * weekSlots.Length;
            int pageVisibleCount = Mathf.Min(weekSlots.Length, Mathf.Max(0, visibleWeekCount - start));
            NovaFNFMenuAction[] pageActions = new NovaFNFMenuAction[pageVisibleCount];

            for (int slotIndex = 0; slotIndex < weekSlots.Length; slotIndex++)
            {
                int visibleIndex = start + slotIndex;
                bool valid = visibleIndex < visibleWeekCount;
                NovaFNFMenuAction slot = weekSlots[slotIndex];

                if (slot != null)
                {
                    slot.gameObject.SetActive(valid);
                    if (valid)
                    {
                        int weekIndex = visibleWeekIndices[visibleIndex];
                        slot.menuController = menuController;
                        slot.actionId = 11;
                        slot.weekIndex = weekIndex;
                        if (slotIndex < pageVisibleCount) pageActions[slotIndex] = slot;
                        bool locked = weekIndex >= storyProgression.unlockedWeekCount;
                        slot.SetLocked(locked);
                    }
                }

                if (weekTitleLabels != null && slotIndex < weekTitleLabels.Length &&
                    weekTitleLabels[slotIndex] != null)
                {
                    string title = "";
                    if (valid)
                    {
                        int weekIndex = visibleWeekIndices[visibleIndex];
                        if (storyProgression.weekTitles != null && weekIndex < storyProgression.weekTitles.Length)
                            title = storyProgression.weekTitles[weekIndex];
                        else
                            title = storyProgression.weekIds[weekIndex];
                    }
                    weekTitleLabels[slotIndex].text = title;
                }
            }

            if (menuController != null)
                menuController.SetStoryActions(pageActions);

            if (pageLabel != null)
                pageLabel.text = pageCount == 0 ? "0 / 0" : (currentPage + 1) + " / " + pageCount;
        }

        public void RefreshLockedStates()
        {
            if (visibleWeekIndices == null)
                BuildWeekList();
            else
                RefreshPage();
        }
    }
}
