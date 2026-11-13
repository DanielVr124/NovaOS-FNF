using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace NovaOS.FNF
{
    /// <summary>
    /// Presents the end-of-song score summary. Grade thresholds are NovaOS defaults
    /// and can be tuned to match the intended scoring rules during polish.
    /// </summary>
    public class NovaFNFResultsController : UdonSharpBehaviour
    {
        public Text songTitleText;
        public Text scoreText;
        public Text missesText;
        public Text accuracyText;
        public Text maxComboText;
        public Text gradeText;
        public Text bestScoreText;
        public Text bestAccuracyText;
        public Text newRecordText;
        public NovaFNFSaveManager saveManager;
        public NovaFNFMenuController menuController;
        public NovaFNFStoryProgression storyProgression;
        public NovaFNFGameplayController gameplayController;

        public void ShowResults(string songTitle, int score, int misses, float accuracy, int maxCombo, bool failed)
        {
            ShowResultsWithRecords(songTitle, "", "", score, misses, accuracy, maxCombo, failed);
        }

        public void ShowResultsWithRecords(string songTitle, string songId, string difficultyId,
            int score, int misses, float accuracy, int maxCombo, bool failed)
        {
            ShowResultsWithRecords(songTitle, songId, difficultyId, score, misses, accuracy, maxCombo, failed, false);
        }

        public void ShowResultsWithRecords(string songTitle, string songId, string difficultyId,
            int score, int misses, float accuracy, int maxCombo, bool failed, bool isNewHighScore)
        {
            if (songTitleText != null) songTitleText.text = songTitle;
            if (scoreText != null) scoreText.text = score.ToString();
            if (missesText != null) missesText.text = misses.ToString();
            if (accuracyText != null) accuracyText.text = accuracy.ToString("F2") + "%";
            if (maxComboText != null) maxComboText.text = maxCombo.ToString();
            if (gradeText != null) gradeText.text = GetGrade(accuracy, misses, failed);

            int bestScore = saveManager != null ? saveManager.GetBestScore(songId, difficultyId) : score;
            float bestAccuracy = saveManager != null ? saveManager.GetBestAccuracy(songId, difficultyId) : accuracy;
            if (bestScoreText != null) bestScoreText.text = "BEST SCORE: " + bestScore;
            if (bestAccuracyText != null) bestAccuracyText.text = "BEST ACCURACY: " + bestAccuracy.ToString("F2") + "%";
            if (newRecordText != null)
                newRecordText.text = isNewHighScore ? "NEW HIGH SCORE!" : "";
        }

        public void ContinuePressed()
        {
            if (storyProgression != null && storyProgression.storyRunActive &&
                storyProgression.waitingForResultsContinue)
            {
                storyProgression.ContinueAfterResults();
                return;
            }

            if (menuController != null)
                menuController.OpenFreeplay();
        }

        public void RetryPressed()
        {
            if (gameplayController != null)
            {
                if (menuController != null && menuController.gameplayScreenIndex >= 0)
                    menuController.OpenGameplay();
                gameplayController.RestartSong();
            }
        }

        public void ReturnToMainMenuPressed()
        {
            if (gameplayController != null && gameplayController.audioPlayer != null)
                gameplayController.audioPlayer.StopSong();
            if (storyProgression != null)
            {
                storyProgression.storyRunActive = false;
                storyProgression.waitingForResultsContinue = false;
                storyProgression.currentWeekIndex = -1;
            }
            if (menuController != null)
                menuController.OpenMainMenu();
        }

        private string GetGrade(float accuracy, int misses, bool failed)
        {
            if (failed) return "FAILED";
            if (misses == 0 && accuracy >= 99f) return "S+";
            if (accuracy >= 95f) return "S";
            if (accuracy >= 90f) return "A";
            if (accuracy >= 80f) return "B";
            if (accuracy >= 70f) return "C";
            if (accuracy >= 60f) return "D";
            return "F";
        }
    }
}
