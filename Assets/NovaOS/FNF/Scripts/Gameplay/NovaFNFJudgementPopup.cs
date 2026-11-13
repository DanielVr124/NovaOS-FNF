using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace NovaOS.FNF
{
    /// <summary>
    /// Reusable hit/miss judgement popup. Assign an Animator with a Show trigger
    /// and tune the visible duration to match the scene's animation.
    /// </summary>
    public class NovaFNFJudgementPopup : UdonSharpBehaviour
    {
        public GameObject root;
        public Text judgementText;
        public Animator animator;
        public string showTrigger = "Show";
        public float visibleSeconds = 0.55f;

        private float hideAtTime;

        private void Start()
        {
            if (root != null) root.SetActive(false);
        }

        public void ShowSick() { Show("Sick!"); }
        public void ShowGood() { Show("Good"); }
        public void ShowBad() { Show("Bad"); }
        public void ShowShit() { Show("Shit"); }
        public void ShowMiss() { Show("Miss"); }

        public void Show(string label)
        {
            if (root != null) root.SetActive(true);
            if (judgementText != null) judgementText.text = label;
            if (animator != null && !string.IsNullOrEmpty(showTrigger))
                animator.SetTrigger(showTrigger);
            hideAtTime = Time.time + Mathf.Max(0.05f, visibleSeconds);
        }

        private void Update()
        {
            if (hideAtTime > 0f && Time.time >= hideAtTime)
            {
                hideAtTime = 0f;
                if (root != null) root.SetActive(false);
            }
        }
    }
}
