using UdonSharp;
using UnityEngine;

namespace NovaOS.FNF
{
    /// <summary>
    /// Song-position-driven beat/step clock. BPM changes are read from the imported
    /// chart bank, so animation pulses remain aligned when a chart changes tempo.
    /// </summary>
    public class NovaFNFBeatClock : UdonSharpBehaviour
    {
        public NovaFNFChartBank chartBank;
        public NovaFNFUdonAudioPlayer audioPlayer;

        [Header("Character animation")]
        public Animator playerAnimator;
        public Animator opponentAnimator;
        public Animator girlfriendAnimator;
        public Animator[] stageBeatAnimators;
        public string danceTrigger = "Dance";
        public string beatTrigger = "Beat";
        public string stepTrigger = "Step";

        [Header("Camera pulse")]
        public Transform cameraPivot;
        public float cameraBaseScale = 1f;
        public float beatZoomMultiplier = 1.025f;
        public float zoomReturnSpeed = 5f;
        public bool cameraZooms = true;
        public bool reducedMotion;

        public int currentBeat;
        public int currentStep;
        public float currentBpm;
        public bool running;

        private int chartIndex = -1;
        private int lastDispatchedStep = -1;
        private float zoomScale = 1f;

        public void StartClock(int selectedChartIndex)
        {
            if (chartBank == null || audioPlayer == null) return;
            chartIndex = selectedChartIndex;
            lastDispatchedStep = -1;
            currentBeat = 0;
            currentStep = 0;
            currentBpm = GetBaseBpm();
            running = true;
            zoomScale = cameraBaseScale;
            if (cameraPivot != null) cameraPivot.localScale = Vector3.one * cameraBaseScale;
        }

        public void StopClock()
        {
            running = false;
            if (cameraPivot != null) cameraPivot.localScale = Vector3.one * cameraBaseScale;
        }

        private void Update()
        {
            if (cameraPivot != null)
            {
                float target = cameraBaseScale;
                cameraPivot.localScale = Vector3.Lerp(
                    cameraPivot.localScale,
                    Vector3.one * target,
                    Time.deltaTime * zoomReturnSpeed);
            }

            if (!running || audioPlayer == null || chartBank == null) return;

            float songTimeMs = audioPlayer.GetSongPositionSeconds() * 1000f;
            float beatPosition = GetBeatPosition(songTimeMs);
            currentStep = Mathf.FloorToInt(beatPosition * 4f);
            currentBeat = Mathf.FloorToInt(beatPosition);

            while (lastDispatchedStep < currentStep)
            {
                lastDispatchedStep++;
                DispatchStep(lastDispatchedStep);
                if (lastDispatchedStep % 4 == 0)
                    DispatchBeat(lastDispatchedStep / 4);
            }
        }

        private float GetBeatPosition(float songTimeMs)
        {
            float bpm = GetBaseBpm();
            float segmentStartMs = 0f;
            float elapsedBeats = 0f;

            if (chartIndex >= 0 && chartBank.bpmStarts != null && chartBank.bpmCounts != null &&
                chartIndex < chartBank.bpmStarts.Length && chartIndex < chartBank.bpmCounts.Length &&
                chartBank.bpmChangeTimesMs != null && chartBank.bpmValues != null)
            {
                int start = chartBank.bpmStarts[chartIndex];
                int count = chartBank.bpmCounts[chartIndex];
                for (int i = 0; i < count; i++)
                {
                    int at = start + i;
                    if (at < 0 || at >= chartBank.bpmChangeTimesMs.Length || at >= chartBank.bpmValues.Length) break;
                    float changeTime = chartBank.bpmChangeTimesMs[at];
                    float changeBpm = chartBank.bpmValues[at];
                    if (changeTime <= segmentStartMs)
                    {
                        bpm = changeBpm;
                        continue;
                    }
                    if (changeTime > songTimeMs) break;

                    elapsedBeats += (changeTime - segmentStartMs) * bpm / 60000f;
                    segmentStartMs = changeTime;
                    bpm = changeBpm;
                }
            }

            currentBpm = bpm;
            return elapsedBeats + (songTimeMs - segmentStartMs) * bpm / 60000f;
        }

        private float GetBaseBpm()
        {
            if (chartBank != null && chartBank.baseBpms != null &&
                chartIndex >= 0 && chartIndex < chartBank.baseBpms.Length &&
                chartBank.baseBpms[chartIndex] > 0f)
                return chartBank.baseBpms[chartIndex];
            return 100f;
        }

        private void DispatchStep(int step)
        {
            if (stageBeatAnimators == null) return;
            for (int i = 0; i < stageBeatAnimators.Length; i++)
            {
                Animator animator = stageBeatAnimators[i];
                if (animator != null && !string.IsNullOrEmpty(stepTrigger))
                    animator.SetTrigger(stepTrigger);
            }
        }

        private void DispatchBeat(int beat)
        {
            if (beat % 2 == 0)
            {
                TriggerDance(playerAnimator);
                TriggerDance(opponentAnimator);
                TriggerDance(girlfriendAnimator);
            }

            if (cameraZooms && !reducedMotion && cameraPivot != null)
            {
                cameraPivot.localScale = Vector3.one * cameraBaseScale * beatZoomMultiplier;
            }

            if (stageBeatAnimators != null)
            {
                for (int i = 0; i < stageBeatAnimators.Length; i++)
                {
                    Animator animator = stageBeatAnimators[i];
                    if (animator != null && !string.IsNullOrEmpty(beatTrigger))
                        animator.SetTrigger(beatTrigger);
                }
            }
        }

        private void TriggerDance(Animator animator)
        {
            if (animator != null && !string.IsNullOrEmpty(danceTrigger))
                animator.SetTrigger(danceTrigger);
        }
    }
}
