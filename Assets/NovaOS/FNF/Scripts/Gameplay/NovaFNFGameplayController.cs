using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace NovaOS.FNF
{
    /// <summary>
    /// First playable Udon gameplay loop: chart spawning, 4-lane keyboard input,
    /// basic sustains, opponent auto-notes, judgement, health, score and results.
    /// Assign a prewarmed pool of NovaFNFNoteView objects in the Inspector.
    /// </summary>
    public class NovaFNFGameplayController : UdonSharpBehaviour
    {
        [Header("Systems")]
        public NovaFNFChartBank chartBank;
        public NovaFNFUdonAudioPlayer audioPlayer;
        public NovaFNFMenuController menuController;
        public NovaFNFResultsController resultsController;
        public NovaFNFStoryProgression storyProgression;
        public NovaFNFBeatClock beatClock;
        public NovaFNFChartEventRouter chartEventRouter;
        public NovaFNFInputBindings inputBindings;
        public NovaFNFSaveManager saveManager;

        [Header("Chart selection")]
        public string selectedSongId = "tutorial";
        public string selectedVariationId = "default";
        public string selectedDifficultyId = "normal";
        public bool downscroll;
        public float spawnLeadMs = 2200f;
        public float noteUnitsPerSecond = 2.5f;
        public float scrollSpeedMultiplier = 1f;
        public float inputOffsetMs;
        public bool ghostTapping = true;

        [Header("Note field")]
        public Transform[] laneReceptors;
        public Animator[] playerStrumlineAnimators;
        public Animator[] opponentStrumlineAnimators;
        public Animator playerCharacterAnimator;
        public Animator opponentCharacterAnimator;
        public string[] playerSingTriggers = { "singLEFT", "singDOWN", "singUP", "singRIGHT" };
        public string[] opponentSingTriggers = { "singLEFT", "singDOWN", "singUP", "singRIGHT" };
        public string playerMissTrigger = "miss";
        public string strumConfirmTrigger = "Confirm";
        public string strumPressTrigger = "Press";
        public string strumIdleTrigger = "Static";
        public NovaFNFNoteView[] notePool;

        [Header("Judgement windows (milliseconds)")]
        public float sickWindowMs = 45f;
        public float goodWindowMs = 90f;
        public float badWindowMs = 135f;
        public float shitWindowMs = 160f;
        public float missWindowMs = 160f;
        public NovaFNFJudgementPopup judgementPopup;

        [Header("Scoring")]
        public int sickScore = 350;
        public int goodScore = 200;
        public int badScore = 100;
        public int shitScore = 50;
        public float hitHealthGain = 0.025f;
        public float missHealthLoss = 0.10f;

        [Header("Countdown")]
        public bool enableCountdown = true;
        [Range(1, 8)] public int countdownBeats = 4;
        public Text countdownText;
        public Animator countdownAnimator;
        public string countdownBeatTrigger = "Beat";
        public AudioSource countdownAudioSource;
        public AudioClip[] countdownClips;
        public string[] countdownLabels;

        [Header("UI")]
        public Text scoreText;
        public Text comboText;
        public Text missesText;
        public Text accuracyText;
        public Image healthFill;
        public GameObject pausePanel;

        [Header("Runtime state (read only)")]
        public int score;
        public int combo;
        public int misses;
        public int highestCombo;
        public float health = 1f;
        public float accuracy;
        public bool playing;
        public bool paused;
        public bool countdownActive;

        private int chartIndex = -1;
        private int chartNoteStart;
        private int chartNoteEnd;
        private int nextChartNote;
        private int judgedNotes;
        private int weightedAccuracy;
        private NovaFNFNoteView[] heldSustainNotes = new NovaFNFNoteView[4];
        private bool[] externalLaneHeld = new bool[4];
        private float lastNoteEndMs;
        private bool failed;
        private float countdownStartTime;
        private float countdownBeatSeconds;
        private int countdownStage = -1;

        public void SetSongSelection(string songId, string variationId, string difficultyId)
        {
            selectedSongId = songId;
            selectedVariationId = variationId;
            selectedDifficultyId = difficultyId;
        }

        public void StartSong()
        {
            if (chartBank == null || audioPlayer == null)
            {
                Debug.LogError("Gameplay Controller needs a Chart Bank and Audio Player.");
                return;
            }

            if (chartBank.noteStarts == null || chartBank.noteCounts == null ||
                chartBank.noteTimesMs == null || chartBank.noteDirections == null ||
                chartBank.sustainLengthsMs == null || notePool == null || notePool.Length == 0 ||
                laneReceptors == null || laneReceptors.Length < 4)
            {
                Debug.LogError("The selected chart bank has not been populated or is missing note arrays.");
                return;
            }

            chartIndex = chartBank.FindChartIndex(selectedSongId, selectedVariationId, selectedDifficultyId);
            if (chartIndex < 0)
                chartIndex = chartBank.FindFirstDifficulty(selectedSongId, selectedVariationId);

            if (chartIndex < 0 || chartIndex >= chartBank.noteStarts.Length ||
                chartIndex >= chartBank.noteCounts.Length)
            {
                Debug.LogWarning("No chart found for " + selectedSongId + " / " + selectedVariationId + " / " + selectedDifficultyId + ".");
                return;
            }

            ClearNotes();
            score = 0;
            combo = 0;
            misses = 0;
            highestCombo = 0;
            failed = false;
            judgedNotes = 0;
            weightedAccuracy = 0;
            health = 1f;
            accuracy = 100f;
            lastNoteEndMs = 0f;

            chartNoteStart = chartBank.noteStarts[chartIndex];
            chartNoteEnd = chartNoteStart + chartBank.noteCounts[chartIndex];
            if (chartNoteStart < 0 || chartNoteEnd > chartBank.noteTimesMs.Length ||
                chartNoteEnd > chartBank.noteDirections.Length ||
                chartNoteEnd > chartBank.sustainLengthsMs.Length)
            {
                Debug.LogError("The chart bank contains invalid note range indices for " + selectedSongId + ".");
                return;
            }
            nextChartNote = chartNoteStart;

            for (int i = chartNoteStart; i < chartNoteEnd; i++)
            {
                float end = chartBank.noteTimesMs[i] + chartBank.sustainLengthsMs[i];
                if (end > lastNoteEndMs) lastNoteEndMs = end;
            }

            string playerCharacter = "bf";
            string opponentCharacter = "dad";
            if (chartBank.playerCharacterIds != null && chartIndex < chartBank.playerCharacterIds.Length &&
                !string.IsNullOrEmpty(chartBank.playerCharacterIds[chartIndex]))
                playerCharacter = chartBank.playerCharacterIds[chartIndex];
            if (chartBank.opponentCharacterIds != null && chartIndex < chartBank.opponentCharacterIds.Length &&
                !string.IsNullOrEmpty(chartBank.opponentCharacterIds[chartIndex]))
                opponentCharacter = chartBank.opponentCharacterIds[chartIndex];
            audioPlayer.SetVocalCharacters(playerCharacter, opponentCharacter);

            audioPlayer.StopSong();
            if (beatClock != null) beatClock.StopClock();
            if (chartEventRouter != null) chartEventRouter.StopEvents();
            playing = false;
            paused = false;
            if (pausePanel != null) pausePanel.SetActive(false);
            RefreshUI();

            if (enableCountdown)
            {
                float bpm = 100f;
                if (chartBank.baseBpms != null && chartIndex < chartBank.baseBpms.Length && chartBank.baseBpms[chartIndex] > 0f)
                    bpm = chartBank.baseBpms[chartIndex];

                countdownBeatSeconds = 60f / bpm;
                countdownStartTime = Time.time;
                countdownStage = -1;
                countdownActive = true;
                if (countdownText != null) countdownText.text = "";
            }
            else
            {
                BeginSongNow();
            }
        }

        private void Update()
        {
            if (countdownActive)
            {
                UpdateCountdown();
                if (Input.GetKeyDown(KeyCode.Escape))
                    CancelCountdown();
                return;
            }

            if (!playing || paused) return;

            if (inputBindings != null)
            {
                for (int lane = 0; lane < 4; lane++)
                    if (inputBindings.LanePressed(lane)) HitLane(lane);
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) HitLane(0);
                if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) HitLane(1);
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) HitLane(2);
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) HitLane(3);
            }

            UpdateSustains();

            float songTimeMs = audioPlayer.GetSongPositionSeconds() * 1000f;
            SpawnUpcomingNotes(songTimeMs);
            UpdateActiveNotes(songTimeMs);

            bool backPressed = inputBindings != null
                ? inputBindings.BackPressed()
                : Input.GetKeyDown(KeyCode.Escape);
            bool pausePressed = inputBindings != null
                ? inputBindings.PausePressed()
                : Input.GetKeyDown(KeyCode.P);
            if (backPressed || pausePressed)
                PauseGame();

            if (nextChartNote >= chartNoteEnd && !audioPlayer.IsSongSourcePlaying())
                FinishSong();
        }

        private void UpdateCountdown()
        {
            float elapsed = Time.time - countdownStartTime;
            int stage = Mathf.FloorToInt(elapsed / Mathf.Max(0.01f, countdownBeatSeconds));
            if (stage != countdownStage && stage < countdownBeats)
            {
                countdownStage = stage;
                if (countdownLabels != null && stage >= 0 && stage < countdownLabels.Length && countdownText != null)
                    countdownText.text = countdownLabels[stage];
                else if (countdownText != null)
                    countdownText.text = "";

                if (countdownAnimator != null && !string.IsNullOrEmpty(countdownBeatTrigger))
                    countdownAnimator.SetTrigger(countdownBeatTrigger);

                if (countdownAudioSource != null && countdownClips != null &&
                    stage >= 0 && stage < countdownClips.Length && countdownClips[stage] != null)
                    countdownAudioSource.PlayOneShot(countdownClips[stage]);
            }

            if (elapsed >= countdownBeatSeconds * countdownBeats)
                BeginSongNow();
        }

        private void BeginSongNow()
        {
            countdownActive = false;
            if (countdownText != null) countdownText.text = "";

            playing = audioPlayer != null && audioPlayer.PlaySong(selectedSongId, selectedVariationId);
            paused = false;
            if (!playing)
            {
                Debug.LogWarning("Unable to start song audio for " + selectedSongId + ".");
                if (storyProgression != null && storyProgression.storyRunActive)
                    storyProgression.CancelStoryRun();
                else if (menuController != null)
                    menuController.OpenFreeplay();
                return;
            }

            if (beatClock != null) beatClock.StartClock(chartIndex);
            if (chartEventRouter != null) chartEventRouter.StartEvents(chartIndex);
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        public void CancelCountdown()
        {
            if (!countdownActive) return;
            countdownActive = false;
            if (countdownText != null) countdownText.text = "";
            if (countdownAudioSource != null) countdownAudioSource.Stop();
            if (storyProgression != null && storyProgression.storyRunActive)
                storyProgression.CancelStoryRun();
            else if (menuController != null)
                menuController.OpenFreeplay();
        }

        public void SetExternalLaneHeld(int lane, bool held)
        {
            if (lane < 0 || lane > 3) return;
            externalLaneHeld[lane] = held;
        }

        public void HitLane(int lane)
        {
            if (!playing || paused || countdownActive || lane < 0 || lane > 3) return;

            float songTimeMs = audioPlayer.GetSongPositionSeconds() * 1000f + inputOffsetMs;
            NovaFNFNoteView bestNote = null;
            float bestDifference = 999999f;

            for (int i = 0; i < notePool.Length; i++)
            {
                NovaFNFNoteView note = notePool[i];
                if (note == null || !note.IsActiveNote() || note.IsOpponentNote()) continue;
                if (note.GetLane() != lane || note.IsHoldingSustain()) continue;

                float difference = songTimeMs - note.GetTimeMs();
                float absolute = Mathf.Abs(difference);
                float maximumHitWindow = Mathf.Min(shitWindowMs, missWindowMs);
                if (absolute <= maximumHitWindow && absolute < bestDifference)
                {
                    bestDifference = absolute;
                    bestNote = note;
                }
            }

            if (bestNote == null)
            {
                if (!ghostTapping) RegisterMiss(null);
                return;
            }
            RegisterHit(bestNote, bestDifference);
        }

        public void PauseGame()
        {
            if (!playing || paused || countdownActive) return;
            paused = true;
            audioPlayer.PauseSong();
            if (pausePanel != null) pausePanel.SetActive(true);
        }

        public void ResumeGame()
        {
            if (!playing || !paused) return;
            paused = false;
            audioPlayer.ResumeSong();
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        public void RestartSong()
        {
            countdownActive = false;
            if (audioPlayer != null) audioPlayer.StopSong();
            if (beatClock != null) beatClock.StopClock();
            if (chartEventRouter != null) chartEventRouter.StopEvents();
            StartSong();
        }

        public void ExitToMenu()
        {
            countdownActive = false;
            if (audioPlayer != null) audioPlayer.StopSong();
            if (beatClock != null) beatClock.StopClock();
            playing = false;
            paused = false;
            ClearNotes();
            if (pausePanel != null) pausePanel.SetActive(false);
            if (storyProgression != null && storyProgression.storyRunActive)
                storyProgression.AbandonStoryRun();
            if (menuController != null) menuController.OpenFreeplay();
        }

        private void SpawnUpcomingNotes(float songTimeMs)
        {
            while (nextChartNote < chartNoteEnd &&
                   chartBank.noteTimesMs[nextChartNote] <= songTimeMs + spawnLeadMs)
            {
                int direction = chartBank.noteDirections[nextChartNote];
                if (direction >= 0 && direction < 8)
                {
                    NovaFNFNoteView view = GetFreeNote();
                    if (view != null)
                    {
                        view.Initialize(
                            direction,
                            chartBank.noteTimesMs[nextChartNote],
                            chartBank.sustainLengthsMs[nextChartNote],
                            direction >= 4,
                            laneReceptors);
                    }
                    else
                    {
                        Debug.LogWarning("Note pool exhausted. Increase the prewarmed notePool size.");
                    }
                }
                nextChartNote++;
            }
        }

        private void UpdateActiveNotes(float songTimeMs)
        {
            for (int i = 0; i < notePool.Length; i++)
            {
                NovaFNFNoteView note = notePool[i];
                if (note == null || !note.IsActiveNote()) continue;

                note.UpdatePosition(songTimeMs, noteUnitsPerSecond, GetScrollSpeed(), downscroll);

                if (note.IsOpponentNote() && songTimeMs >= note.GetTimeMs())
                {
                    int opponentLane = note.GetLane() % 4;
                    TriggerStrumline(opponentStrumlineAnimators, opponentLane, strumConfirmTrigger);
                    TriggerCharacterSing(opponentCharacterAnimator, opponentSingTriggers, opponentLane);
                    note.Deactivate();
                    continue;
                }

                if (!note.IsOpponentNote() && !note.IsHoldingSustain() &&
                    songTimeMs + inputOffsetMs > note.GetTimeMs() + missWindowMs)
                    RegisterMiss(note);
            }
        }

        private void UpdateSustains()
        {
            float songTimeMs = audioPlayer.GetSongPositionSeconds() * 1000f + inputOffsetMs;
            for (int lane = 0; lane < 4; lane++)
            {
                NovaFNFNoteView note = heldSustainNotes[lane];
                if (note == null) continue;

                bool held = IsLaneHeld(lane);
                if (!held && songTimeMs < note.GetSustainEndMs())
                {
                    heldSustainNotes[lane] = null;
                    RegisterMiss(note);
                }
                else if (songTimeMs >= note.GetSustainEndMs())
                {
                    heldSustainNotes[lane] = null;
                    note.Deactivate();
                    RegisterSustainComplete();
                }
            }
        }

        private void RegisterHit(NovaFNFNoteView note, float absoluteDifference)
        {
            TriggerStrumline(playerStrumlineAnimators, note.GetLane(), strumConfirmTrigger);
            TriggerCharacterSing(playerCharacterAnimator, playerSingTriggers, note.GetLane());
            float length = note.GetSustainLengthMs();
            if (length > 0f && IsLaneHeld(note.GetLane()))
            {
                note.SetHoldingSustain(true);
                heldSustainNotes[note.GetLane()] = note;
            }
            else
            {
                note.Deactivate();
            }

            judgedNotes++;
            if (absoluteDifference <= sickWindowMs)
            {
                score += sickScore;
                weightedAccuracy += 100;
                combo++;
                if (combo > highestCombo) highestCombo = combo;
                health += hitHealthGain;
                if (judgementPopup != null) judgementPopup.ShowSick();
            }
            else if (absoluteDifference <= goodWindowMs)
            {
                score += goodScore;
                weightedAccuracy += 75;
                combo++;
                if (combo > highestCombo) highestCombo = combo;
                health += hitHealthGain * 0.75f;
                if (judgementPopup != null) judgementPopup.ShowGood();
            }
            else if (absoluteDifference <= badWindowMs)
            {
                score += badScore;
                weightedAccuracy += 50;
                combo = 0;
                health += hitHealthGain * 0.35f;
                if (judgementPopup != null) judgementPopup.ShowBad();
            }
            else
            {
                score += shitScore;
                weightedAccuracy += 25;
                combo = 0;
                health += hitHealthGain * 0.1f;
                if (judgementPopup != null) judgementPopup.ShowShit();
            }

            ClampHealth();
            RefreshUI();
        }

        private void RegisterSustainComplete()
        {
            score += 100;
            health += hitHealthGain * 0.5f;
            ClampHealth();
            RefreshUI();
        }

        private void RegisterMiss(NovaFNFNoteView note)
        {
            if (note != null)
            {
                TriggerStrumline(playerStrumlineAnimators, note.GetLane(), strumPressTrigger);
                if (playerCharacterAnimator != null && !string.IsNullOrEmpty(playerMissTrigger))
                    playerCharacterAnimator.SetTrigger(playerMissTrigger);
                note.Deactivate();
            }
            misses++;
            judgedNotes++;
            combo = 0;
            if (judgementPopup != null) judgementPopup.ShowMiss();
            health -= missHealthLoss;
            ClampHealth();
            RefreshUI();

            if (health <= 0f)
            {
                failed = true;
                FinishSong();
            }
        }

        private void TriggerCharacterSing(Animator animator, string[] singTriggers, int lane)
        {
            if (animator == null || singTriggers == null || lane < 0 || lane >= singTriggers.Length) return;
            if (!string.IsNullOrEmpty(singTriggers[lane]))
                animator.SetTrigger(singTriggers[lane]);
        }

        private void TriggerStrumline(Animator[] strumline, int lane, string trigger)
        {
            if (strumline == null || lane < 0 || lane >= strumline.Length) return;
            Animator animator = strumline[lane];
            if (animator != null && !string.IsNullOrEmpty(trigger))
                animator.SetTrigger(trigger);
        }

        private NovaFNFNoteView GetFreeNote()
        {
            for (int i = 0; i < notePool.Length; i++)
            {
                if (notePool[i] != null && !notePool[i].IsActiveNote())
                    return notePool[i];
            }
            return null;
        }

        private void ClearNotes()
        {
            if (notePool != null)
                for (int i = 0; i < notePool.Length; i++)
                    if (notePool[i] != null) notePool[i].Deactivate();

            for (int lane = 0; lane < 4; lane++)
                heldSustainNotes[lane] = null;
        }

        private bool IsLaneHeld(int lane)
        {
            if (inputBindings != null)
                return externalLaneHeld[lane] || inputBindings.LaneHeld(lane);
            if (lane == 0) return externalLaneHeld[0] || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
            if (lane == 1) return externalLaneHeld[1] || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);
            if (lane == 2) return externalLaneHeld[2] || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
            if (lane == 3) return externalLaneHeld[3] || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
            return false;
        }

        private float GetScrollSpeed()
        {
            if (chartIndex < 0 || chartBank.scrollSpeeds == null || chartIndex >= chartBank.scrollSpeeds.Length)
                return 1f;
            return Mathf.Max(0.1f, chartBank.scrollSpeeds[chartIndex] * scrollSpeedMultiplier);
        }

        private void ClampHealth()
        {
            health = Mathf.Clamp(health, 0f, 2f);
            accuracy = judgedNotes > 0 ? (float)weightedAccuracy / judgedNotes : 100f;
        }

        private void RefreshUI()
        {
            if (scoreText != null) scoreText.text = "Score: " + score;
            if (comboText != null) comboText.text = combo + "x";
            if (missesText != null) missesText.text = "Misses: " + misses;
            if (accuracyText != null) accuracyText.text = "Accuracy: " + accuracy.ToString("F2") + "%";
            if (healthFill != null) healthFill.fillAmount = health * 0.5f;
        }

        private void FinishSong()
        {
            if (!playing) return;
            playing = false;
            paused = false;
            if (audioPlayer != null) audioPlayer.StopSong();
            if (beatClock != null) beatClock.StopClock();
            if (chartEventRouter != null) chartEventRouter.StopEvents();
            ClearNotes();
            string recordSongId = selectedSongId + "-" + selectedVariationId;
            int previousBestScore = saveManager != null
                ? saveManager.GetBestScore(recordSongId, selectedDifficultyId) : 0;
            bool isNewHighScore = score > previousBestScore && score > 0;
            if (saveManager != null)
                saveManager.SaveSongResult(recordSongId, selectedDifficultyId,
                    score, misses, accuracy, highestCombo);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (storyProgression != null && storyProgression.storyRunActive)
            {
                if (failed) storyProgression.CancelStoryRun();
                else storyProgression.MarkCurrentSongFinished();
            }

            if (resultsController != null)
            {
                string title = selectedSongId;
                if (chartBank != null && chartBank.songNames != null &&
                    chartIndex >= 0 && chartIndex < chartBank.songNames.Length)
                    title = chartBank.songNames[chartIndex];
                resultsController.ShowResultsWithRecords(title, recordSongId, selectedDifficultyId,
                    score, misses, accuracy, highestCombo, failed, isNewHighScore);
            }
            if (menuController != null) menuController.OpenResults();
        }
    }
}
