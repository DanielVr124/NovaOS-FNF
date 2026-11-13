using UdonSharp;
using UnityEngine;

namespace NovaOS.FNF
{
    /// <summary>
    /// Inspector-configurable receiver for one kind of chart event.
    /// Multiple bindings can listen to the same event kind and optionally filter
    /// its serialized value payload. This keeps event behavior extensible without
    /// requiring arbitrary JSON parsing in Udon at runtime.
    /// </summary>
    public class NovaFNFChartEventBinding : UdonSharpBehaviour
    {
        public string eventKind;
        [Tooltip("Optional case-sensitive fragment required in the event value JSON. Leave empty to accept every value.")]
        public string requiredValueFragment;

        [Header("Animation")]
        public Animator[] animators;
        public string[] animatorTriggers;

        [Header("Scene reactions")]
        public GameObject[] activateObjects;
        public GameObject[] deactivateObjects;
        public Transform transformTarget;
        public Vector3 localPositionOnEvent;
        public bool setLocalPosition;
        public Vector3 localScaleOnEvent = Vector3.one;
        public bool setLocalScale;

        [Header("Audio reaction")]
        public AudioSource audioSource;
        public AudioClip audioClip;

        [Header("Optional UI / stage labels")]
        public UnityEngine.UI.Text debugLabel;
        public string debugText;

        public void Dispatch(string valueJson)
        {
            if (!string.IsNullOrEmpty(requiredValueFragment))
            {
                if (string.IsNullOrEmpty(valueJson) ||
                    valueJson.IndexOf(requiredValueFragment) < 0)
                    return;
            }

            if (animators != null)
            {
                for (int i = 0; i < animators.Length; i++)
                {
                    if (animators[i] == null) continue;
                    string trigger = (animatorTriggers != null && i < animatorTriggers.Length)
                        ? animatorTriggers[i] : "";
                    if (!string.IsNullOrEmpty(trigger))
                        animators[i].SetTrigger(trigger);
                }
            }

            if (activateObjects != null)
                for (int i = 0; i < activateObjects.Length; i++)
                    if (activateObjects[i] != null) activateObjects[i].SetActive(true);

            if (deactivateObjects != null)
                for (int i = 0; i < deactivateObjects.Length; i++)
                    if (deactivateObjects[i] != null) deactivateObjects[i].SetActive(false);

            if (transformTarget != null)
            {
                if (setLocalPosition) transformTarget.localPosition = localPositionOnEvent;
                if (setLocalScale) transformTarget.localScale = localScaleOnEvent;
            }

            if (audioSource != null && audioClip != null)
                audioSource.PlayOneShot(audioClip);

            if (debugLabel != null)
                debugLabel.text = debugText;
        }
    }
}
