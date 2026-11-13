#if UNITY_EDITOR
using System.Collections.Generic;
using NovaOS.FNF;
using UnityEditor;
using UnityEngine;

namespace NovaOS.FNF.Editor
{
    /// <summary>
    /// Static scene-wiring validator. This does not replace a UdonSharp compile
    /// or an actual VRChat upload/play test, but it catches missing references first.
    /// </summary>
    public static class NovaFNFProjectValidator
    {
        [MenuItem("NovaOS/FNF/Validation/Validate Current Scene")]
        public static void ValidateCurrentScene()
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();
            List<string> passed = new List<string>();

            NovaFNFUdonAudioBank audioBank = Object.FindObjectOfType<NovaFNFUdonAudioBank>();
            NovaFNFChartBank chartBank = Object.FindObjectOfType<NovaFNFChartBank>();
            NovaFNFUdonAudioPlayer audioPlayer = Object.FindObjectOfType<NovaFNFUdonAudioPlayer>();
            NovaFNFMenuController menu = Object.FindObjectOfType<NovaFNFMenuController>();
            NovaFNFGameplayController gameplay = Object.FindObjectOfType<NovaFNFGameplayController>();
            NovaFNFBeatClock beatClock = Object.FindObjectOfType<NovaFNFBeatClock>();
            NovaFNFChartEventRouter eventRouter = Object.FindObjectOfType<NovaFNFChartEventRouter>();
            NovaFNFStoryProgression story = Object.FindObjectOfType<NovaFNFStoryProgression>();
            NovaFNFWeekListController weekList = Object.FindObjectOfType<NovaFNFWeekListController>();
            NovaFNFFreeplayListController freeplay = Object.FindObjectOfType<NovaFNFFreeplayListController>();
            NovaFNFDifficultyListController difficulties = Object.FindObjectOfType<NovaFNFDifficultyListController>();
            NovaFNFResultsController results = Object.FindObjectOfType<NovaFNFResultsController>();
            NovaFNFSaveManager saves = Object.FindObjectOfType<NovaFNFSaveManager>();
            NovaFNFInputBindings controls = Object.FindObjectOfType<NovaFNFInputBindings>();
            NovaFNFControlsController controlsScreen = Object.FindObjectOfType<NovaFNFControlsController>();

            Check(audioBank != null, "A NovaFNFUdonAudioBank component exists.", errors);
            Check(chartBank != null, "A NovaFNFChartBank component exists.", errors);
            Check(audioPlayer != null, "A NovaFNFUdonAudioPlayer component exists.", errors);
            Check(menu != null, "A NovaFNFMenuController component exists.", errors);
            Check(gameplay != null, "A NovaFNFGameplayController component exists.", errors);

            if (audioBank != null)
            {
                if (audioBank.songIds == null || audioBank.instrumentals == null)
                    errors.Add("Audio bank song ID or instrumental arrays are null.");
                else
                {
                    Check(audioBank.songIds.Length == audioBank.instrumentals.Length,
                        "Audio bank song ID and instrumental arrays have matching lengths.", errors);
                    Check(audioBank.songVariationIds == null ||
                        audioBank.songVariationIds.Length == audioBank.songIds.Length,
                        "Audio bank variation IDs align with song IDs.", errors);
                }

                if (audioBank.voiceSongIds != null)
                {
                    Check(audioBank.voiceSongIds.Length == audioBank.voiceCharacterIds.Length,
                        "Character voice song and character arrays have matching lengths.", errors);
                    Check(audioBank.voiceVariationIds == null ||
                        audioBank.voiceVariationIds.Length == audioBank.voiceSongIds.Length,
                        "Character voice variation IDs align with voice entries.", errors);
                    Check(audioBank.voiceClips == null ||
                        audioBank.voiceClips.Length == audioBank.voiceSongIds.Length,
                        "Character voice clips align with voice entries.", errors);
                }
            }

            if (audioPlayer != null)
            {
                Check(audioPlayer.audioBank != null, "Audio player references the VRChat audio bank.", errors);
                Check(audioPlayer.instrumentalSource != null, "Audio player has an instrumental AudioSource.", errors);
                Check(audioPlayer.menuMusicSource != null, "Audio player has a menu music AudioSource.", warnings);
                Check(audioPlayer.previewSource != null, "Audio player has a Freeplay preview AudioSource.", warnings);
                Check(audioPlayer.soundEffectSource != null, "Audio player has a sound-effect AudioSource.", warnings);
                if (audioPlayer.audioBank != audioBank && audioBank != null)
                    warnings.Add("Audio player points to a different audio bank than the scene's discovered bank.");
            }

            if (chartBank != null)
            {
                bool noteArraysValid = chartBank.noteTimesMs != null &&
                    chartBank.noteDirections != null &&
                    chartBank.sustainLengthsMs != null &&
                    chartBank.noteTimesMs.Length == chartBank.noteDirections.Length &&
                    chartBank.noteTimesMs.Length == chartBank.sustainLengthsMs.Length;
                Check(noteArraysValid, "Chart note timestamps, directions and sustain lengths are aligned.", errors);

                bool chartRangesValid = chartBank.noteStarts != null && chartBank.noteCounts != null &&
                    chartBank.noteStarts.Length == chartBank.noteCounts.Length;
                Check(chartRangesValid, "Chart start/count arrays are aligned.", errors);

                if (chartBank.eventStarts != null || chartBank.eventCounts != null)
                    Check(chartBank.eventStarts != null && chartBank.eventCounts != null &&
                        chartBank.eventStarts.Length == chartBank.eventCounts.Length,
                        "Chart event start/count arrays are aligned.", errors);

                if (chartBank.songIds == null || chartBank.songIds.Length == 0)
                    warnings.Add("Chart bank contains no imported charts yet.");
            }

            if (menu != null)
            {
                Check(menu.screens != null && menu.screens.Length > 0,
                    "Menu controller has a screen array.", errors);
                if (menu.screens != null)
                {
                    for (int i = 0; i < menu.screens.Length; i++)
                        if (menu.screens[i] == null)
                            warnings.Add("Menu screen slot " + i + " is empty.");
                }

                Check(menu.gameplayController != null,
                    "Menu controller references the gameplay controller.", errors);
                Check(menu.inputBindings != null,
                    "Menu controller references shared input bindings.", warnings);
                Check(menu.menuAudioPlayer != null,
                    "Menu controller references the audio player for screen music.", warnings);
            }

            if (gameplay != null)
            {
                Check(gameplay.chartBank != null, "Gameplay references the chart bank.", errors);
                Check(gameplay.audioPlayer != null, "Gameplay references the audio player.", errors);
                Check(gameplay.notePool != null && gameplay.notePool.Length > 0,
                    "Gameplay has a non-empty prewarmed note pool.", errors);
                Check(gameplay.laneReceptors != null && gameplay.laneReceptors.Length >= 4,
                    "Gameplay has at least four player lane receptors.", errors);
                if (gameplay.laneReceptors != null && gameplay.laneReceptors.Length < 8)
                    warnings.Add("Fewer than eight receptors are assigned. Opponent notes will be hidden.");
                Check(gameplay.inputBindings != null,
                    "Gameplay references the shared input bindings.", warnings);
                Check(gameplay.beatClock != null,
                    "Gameplay references the BPM beat clock.", warnings);
                Check(gameplay.chartEventRouter != null,
                    "Gameplay references the chart event router.", warnings);
                Check(gameplay.resultsController != null,
                    "Gameplay references the results controller.", errors);
                Check(gameplay.saveManager != null,
                    "Gameplay references the persistent save manager.", warnings);
            }

            if (eventRouter != null)
            {
                Check(eventRouter.chartBank != null, "Chart event router references a chart bank.", errors);
                Check(eventRouter.audioPlayer != null, "Chart event router references the audio player.", errors);
                if (eventRouter.bindings == null || eventRouter.bindings.Length == 0)
                    warnings.Add("Chart event router has no scene event bindings yet.");
            }

            if (story != null)
            {
                Check(story.weekIds != null && story.weekIds.Length > 0,
                    "Story Mode has imported week definitions.", warnings);
                Check(story.menuController != null,
                    "Story progression references the menu controller.", errors);
            }

            if (weekList != null)
            {
                Check(weekList.storyProgression != null,
                    "Week list references Story progression.", errors);
                Check(weekList.weekSlots != null && weekList.weekSlots.Length > 0,
                    "Week list has at least one prebuilt row slot.", warnings);
            }

            if (freeplay != null)
            {
                Check(freeplay.chartBank != null,
                    "Freeplay list references the chart bank.", errors);
                Check(freeplay.menuController != null,
                    "Freeplay list references the menu controller.", errors);
                Check(freeplay.songSlots != null && freeplay.songSlots.Length > 0,
                    "Freeplay list has at least one prebuilt row slot.", warnings);
            }

            if (difficulties != null)
            {
                Check(difficulties.chartBank != null,
                    "Difficulty list references the chart bank.", errors);
                Check(difficulties.menuController != null,
                    "Difficulty list references the menu controller.", errors);
                Check(difficulties.difficultySlots != null && difficulties.difficultySlots.Length > 0,
                    "Difficulty list has at least one prebuilt row slot.", warnings);
            }

            if (results != null)
                Check(results.menuController != null,
                    "Results screen references the menu controller.", errors);

            if (saves != null)
            {
                Check(saves.inputBindings != null,
                    "Save manager references input bindings.", warnings);
                Check(saves.optionsController != null,
                    "Save manager references options.", warnings);
                Check(saves.storyProgression != null,
                    "Save manager references Story progression.", warnings);
            }

            if (controlsScreen != null)
            {
                Check(controlsScreen.inputBindings != null,
                    "Controls screen references the input bindings.", errors);
                Check(controlsScreen.assignableKeys != null && controlsScreen.assignableKeys.Length > 0,
                    "Controls screen has a key capture list.", warnings);
            }

            string report = "NovaOS FNF scene validation\n\n" +
                "Passed checks: " + passed.Count + "\n" +
                "Errors: " + errors.Count + "\n" +
                "Warnings: " + warnings.Count;

            if (passed.Count > 0)
                report += "\n\nPASSED\n- " + string.Join("\n- ", passed.ToArray());
            if (errors.Count > 0)
                report += "\n\nERRORS\n- " + string.Join("\n- ", errors.ToArray());
            if (warnings.Count > 0)
                report += "\n\nWARNINGS\n- " + string.Join("\n- ", warnings.ToArray());

            if (errors.Count > 0)
                Debug.LogError(report);
            else if (warnings.Count > 0)
                Debug.LogWarning(report);
            else
                Debug.Log(report);

            EditorUtility.DisplayDialog("NovaOS FNF Validation", report, "Close");
        }

        private static void Check(bool condition, string message, List<string> errors)
        {
            if (condition) return;
            errors.Add(message);
        }

        private static void Check(bool condition, string message, List<string> warnings)
        {
            if (condition) return;
            warnings.Add(message);
        }
    }
}
#endif
