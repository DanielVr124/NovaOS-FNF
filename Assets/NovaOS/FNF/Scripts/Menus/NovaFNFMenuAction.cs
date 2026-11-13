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
        public string songId;
        public GameObject highlightVisual;

        public void OnButtonPressed()
        {
            if (menuController == null) return;
            if (!string.IsNullOrEmpty(songId))
                menuController.SetSelectedSong(songId);
            menuController.RunAction(actionId);
        }

        public void SetHighlighted(bool highlighted)
        {
            if (highlightVisual != null)
                highlightVisual.SetActive(highlighted);
        }
    }
}
