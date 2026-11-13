using UdonSharp;
using UnityEngine;

namespace NovaOS.FNF
{
    /// <summary>
    /// Dispatches imported chart events against preconfigured scene bindings.
    /// It is driven by the same instrumental clock as note gameplay.
    /// </summary>
    public class NovaFNFChartEventRouter : UdonSharpBehaviour
    {
        public NovaFNFChartBank chartBank;
        public NovaFNFUdonAudioPlayer audioPlayer;
        public NovaFNFChartEventBinding[] bindings;

        private int chartIndex = -1;
        private int nextEventIndex;
        private int endEventIndex;
        private bool running;

        public void StartEvents(int selectedChartIndex)
        {
            chartIndex = selectedChartIndex;
            nextEventIndex = 0;
            endEventIndex = 0;
            running = false;

            if (chartBank == null || chartBank.eventStarts == null ||
                chartBank.eventCounts == null || chartIndex < 0 ||
                chartIndex >= chartBank.eventStarts.Length ||
                chartIndex >= chartBank.eventCounts.Length)
                return;

            nextEventIndex = chartBank.eventStarts[chartIndex];
            endEventIndex = nextEventIndex + chartBank.eventCounts[chartIndex];

            if (chartBank.eventTimesMs == null || chartBank.eventKinds == null ||
                chartBank.eventValuesJson == null)
                return;

            if (nextEventIndex < 0 || endEventIndex > chartBank.eventTimesMs.Length ||
                endEventIndex > chartBank.eventKinds.Length ||
                endEventIndex > chartBank.eventValuesJson.Length)
                return;

            running = true;
        }

        public void StopEvents()
        {
            running = false;
            chartIndex = -1;
        }

        private void Update()
        {
            if (!running || audioPlayer == null) return;
            if (!audioPlayer.songLoaded || audioPlayer.songPaused) return;

            float songTimeMs = audioPlayer.GetSongPositionSeconds() * 1000f;
            while (nextEventIndex < endEventIndex &&
                   chartBank.eventTimesMs[nextEventIndex] <= songTimeMs)
            {
                DispatchEvent(chartBank.eventKinds[nextEventIndex],
                    chartBank.eventValuesJson[nextEventIndex]);
                nextEventIndex++;
            }

            if (nextEventIndex >= endEventIndex)
                running = false;
        }

        private void DispatchEvent(string eventKind, string valueJson)
        {
            if (bindings == null) return;
            for (int i = 0; i < bindings.Length; i++)
            {
                NovaFNFChartEventBinding binding = bindings[i];
                if (binding == null || binding.eventKind != eventKind) continue;
                binding.Dispatch(valueJson);
            }
        }
    }
}
