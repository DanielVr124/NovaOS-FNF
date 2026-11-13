using UdonSharp;
using UnityEngine;

namespace NovaOS.FNF
{
    /// <summary>
    /// Attach to a menu item. Hook OnButtonPressed to the Unity UI Button OnClick event.
    /// The same action can be selected and activated by the menu controller's keyboard navigation.
    /// </summary>
    public class NovaFNFMenuAction : UdonSharpBehaviour
    {
        public NovaFNFMenuController menuController;
        [Tooltip("0 Story, 1 Freeplay, 2 Options, 3 Back, 4 Controls, 5 Audio, 6 Accessibility, 7 Start selected song, 8 Main Menu, 9 Results, 10 Title")]
        public int actionId;
        public int weekIndex;
        public string songId;
        public string variationId = "default";
        public string difficultyId = "normal";
        public GameObject highlightVisual;
        public Animator itemAnimator;

        public void OnButtonPressed()
        {
            if (menuController == null) return;
            if (actionId == 11)
                menuController.SetSelectedWeek(weekIndex);
            if (!string.IsNullOrEmpty(songId))
                menuController.SetSelectedSong(songId);
            if (!string.IsNullOrEmpty(variationId)) menuController.SetSelectedVariation(variationId);
            if (!string.IsNullOrEmpty(difficultyId)) menuController.SetSelectedDifficulty(difficultyId);
            menuController.RunAction(actionId);
        }

        public void OnPointerEnter()
        {
            if (menuController != null) menuController.SelectAction(this);
        }

        public void SetHighlighted(bool highlighted)
        {
            if (highlightVisual != null)
                highlightVisual.SetActive(highlighted);
            if (itemAnimator != null)
                itemAnimator.SetBool("Selected", highlighted);
        }
    }
}
