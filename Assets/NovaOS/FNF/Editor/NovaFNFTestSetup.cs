#if UNITY_EDITOR
using System.IO;
using NovaOS.FNF;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UdonSharpEditor;

namespace NovaOS.FNF.Editor
{
    /// <summary>
    /// One-command setup for the generated test scene. Imports chart data into
    /// the scene's chart bank, rebuilds the audio catalog and fills the audio
    /// bank, and imports Story Mode level data where available.
    /// </summary>
    public static class NovaFNFTestSetup
    {
        private const string ScenePath = "Assets/NovaOS/Scenes/NovaFNF_TestScene.unity";
        private const string AudioCatalogPath = "Assets/NovaOS/Generated/NovaAudioCatalog.asset";
        private const string ChartRoot = "Assets/NovaOS/ChartLibrary";
        private const string LevelRoot = "Assets/NovaOS/LevelLibrary";

        [MenuItem("NovaOS/FNF/Setup/Prepare Test Scene and Import All Data")]
        public static void PrepareTestSceneAndImportAllData()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                if (!File.Exists(ScenePath))
                    NovaFNFSceneBuilder.Build();

                if (!File.Exists(ScenePath))
                {
                    EditorUtility.DisplayDialog("NovaOS FNF Setup",
                        "The test scene could not be created. Check the Console for the scene-builder error.",
                        "OK");
                    return;
                }

                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                AssetDatabase.SaveAssets();

                NovaFNFChartBank chartBank = Object.FindObjectOfType<NovaFNFChartBank>();
                NovaFNFUdonAudioBank audioBank = Object.FindObjectOfType<NovaFNFUdonAudioBank>();
                NovaFNFStoryProgression story = Object.FindObjectOfType<NovaFNFStoryProgression>();

                if (chartBank == null || audioBank == null || story == null)
                {
                    EditorUtility.DisplayDialog("NovaOS FNF Setup",
                        "The generated scene is missing one or more required data-bank components. Rebuild the test scene and try again.",
                        "OK");
                    return;
                }

                bool audioReady = AssetDatabase.IsValidFolder("Assets/NovaOS/AudioLibrary");
                if (audioReady)
                {
                    NovaAudioLibraryScanner.ScanAndBuild();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                    if (AssetDatabase.LoadAssetAtPath<NovaAudioCatalog>(AudioCatalogPath) != null)
                    {
                        Selection.activeGameObject = audioBank.gameObject;
                        NovaFNFUdonAudioBankPopulator.PopulateSelected();
                    }
                }

                int chartMetadataCount = CountJsonFiles(ChartRoot, "*metadata*.json");
                if (chartMetadataCount > 0)
                {
                    Selection.activeGameObject = chartBank.gameObject;
                    NovaFNFChartImporter.ImportIntoSelectedBank();
                }

                int levelCount = CountJsonFiles(LevelRoot, "*.json");
                if (levelCount > 0)
                {
                    Selection.activeGameObject = story.gameObject;
                    NovaFNFLevelImporter.ImportLevels();
                }

                EditorUtility.SetDirty(chartBank);
                EditorUtility.SetDirty(audioBank);
                EditorUtility.SetDirty(story);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                Selection.activeGameObject = null;

                string report =
                    "Test scene prepared.\n\n" +
                    "Audio library: " + (audioReady ? "scanned" : "folder not found") + "\n" +
                    "Chart metadata files found: " + chartMetadataCount + "\n" +
                    "Story level files found: " + levelCount + "\n\n" +
                    "The Console and importer reports show any skipped files or warnings. " +
                    "This setup step does not compile UdonSharp or prove PC/Quest runtime compatibility.";
                Debug.Log("NovaOS FNF test setup completed.\n" + report);
                EditorUtility.DisplayDialog("NovaOS FNF Setup", report, "OK");
            }
            catch (System.Exception exception)
            {
                Debug.LogError("NovaOS FNF test setup failed: " + exception);
                EditorUtility.DisplayDialog("NovaOS FNF Setup",
                    "Setup stopped because an exception occurred. Check the Console for details.\n\n" +
                    exception.Message, "OK");
            }
        }

        private static int CountJsonFiles(string root, string pattern)
        {
            if (!Directory.Exists(root)) return 0;
            return Directory.GetFiles(root, pattern, SearchOption.AllDirectories).Length;
        }
    }
}
#endif
