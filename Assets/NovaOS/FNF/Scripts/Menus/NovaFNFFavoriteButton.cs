using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace NovaOS.FNF
{
    /// <summary>
    /// Attach to a favorite button within a Freeplay row. The list controller
    /// assigns the song ID for the currently displayed row.
    /// </summary>
    public class NovaFNFFavoriteButton : UdonSharpBehaviour
    {
        public NovaFNFFreeplayListController freeplayList;
        public NovaFNFSaveManager saveManager;
        public string songId;
        public Text iconText;
        public Image iconImage;
        public Color favoriteColor = Color.yellow;
        public Color normalColor = Color.white;

        public void OnPressed()
        {
            if (freeplayList == null || saveManager == null || string.IsNullOrEmpty(songId)) return;
            freeplayList.ToggleFavorite(songId);
        }

        public void Refresh()
        {
            bool favorite = saveManager != null && !string.IsNullOrEmpty(songId) &&
                saveManager.IsFavorite(songId);

            if (iconText != null) iconText.text = favorite ? "★" : "☆";
            if (iconImage != null) iconImage.color = favorite ? favoriteColor : normalColor;
        }
    }
}
