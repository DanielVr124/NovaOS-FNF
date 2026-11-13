using UdonSharp;
using UnityEngine;

namespace NovaOS.FNF
{
    /// <summary>
    /// One pooled note visual. Its Transform should be in the same world-space
    /// gameplay canvas/scene as its receptor targets.
    /// </summary>
    public class NovaFNFNoteView : UdonSharpBehaviour
    {
        public GameObject noteVisual;
        public Transform sustainBody;

        private Transform receptor;
        private int lane;
        private float timeMs;
        private float sustainLengthMs;
        private bool opponentNote;
        private bool activeNote;
        private bool holdingSustain;

        public void Initialize(int laneIndex, float noteTimeMs, float sustainMs, bool isOpponent, Transform[] receptors)
        {
            lane = laneIndex;
            timeMs = noteTimeMs;
            sustainLengthMs = Mathf.Max(0f, sustainMs);
            opponentNote = isOpponent;
            holdingSustain = false;
            activeNote = true;

            if (receptors != null && laneIndex >= 0 && laneIndex < receptors.Length)
                receptor = receptors[laneIndex];
            else
                receptor = null;

            if (noteVisual != null) noteVisual.SetActive(true);
            if (sustainBody != null) sustainBody.gameObject.SetActive(sustainLengthMs > 0f);
        }

        public void UpdatePosition(float songTimeMs, float unitsPerSecond, float scrollSpeed, bool downscroll)
        {
            if (!activeNote || receptor == null) return;
            float secondsUntilHit = (timeMs - songTimeMs) / 1000f;
            float verticalDistance = secondsUntilHit * unitsPerSecond * scrollSpeed;
            if (downscroll) verticalDistance = -verticalDistance;

            transform.position = receptor.position + Vector3.up * verticalDistance;
            transform.rotation = receptor.rotation;

            if (sustainBody != null && sustainLengthMs > 0f)
            {
                float bodyLength = sustainLengthMs / 1000f * unitsPerSecond * scrollSpeed;
                Vector3 scale = sustainBody.localScale;
                scale.y = Mathf.Max(0.02f, bodyLength);
                sustainBody.localScale = scale;
            }
        }

        public int GetLane() { return lane; }
        public float GetTimeMs() { return timeMs; }
        public float GetSustainLengthMs() { return sustainLengthMs; }
        public float GetSustainEndMs() { return timeMs + sustainLengthMs; }
        public bool IsOpponentNote() { return opponentNote; }
        public bool IsActiveNote() { return activeNote; }
        public bool IsHoldingSustain() { return holdingSustain; }
        public void SetHoldingSustain(bool value) { holdingSustain = value; }

        public void Deactivate()
        {
            activeNote = false;
            holdingSustain = false;
            if (noteVisual != null) noteVisual.SetActive(false);
            if (sustainBody != null) sustainBody.gameObject.SetActive(false);
        }
    }
}
