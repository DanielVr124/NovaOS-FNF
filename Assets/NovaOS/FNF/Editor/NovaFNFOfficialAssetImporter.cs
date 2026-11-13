#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace NovaOS.FNF.Editor
{
    /// <summary>
    /// Fetches the official FNF asset submodule into Unity's Library cache, then
    /// imports its resources into the local Unity project. Imported proprietary
    /// content is deliberately excluded from Git by the project .gitignore.
    /// </summary>
    public static class NovaFNFOfficialAssetImporter
    {
        private const string RepositoryUrl = "https://github.com/FunkinCrew/funkin.assets.git";
        private const string CacheRoot = "Library/NovaOS/FNF/funkin.assets";
        private const string AudioRoot = "Assets/NovaOS/AudioLibrary";
        private const string ChartRoot = "Assets/NovaOS/ChartLibrary";
        private const string LevelRoot = "Assets/NovaOS/LevelLibrary";
        private const string ImportedRoot = "Assets/NovaOS/FNF/ImportedOfficialAssets";

        [MenuItem("NovaOS/FNF/Official Assets/Fetch and Import Official Asset Repository")]
        public static void FetchAndImport()
        {
            if (!EditorUtility.DisplayDialog(
                "Import official Friday Night Funkin' assets",
                "This downloads the official FunkinCrew/funkin.assets repository into your local Unity Library cache, then imports audio, charts, character/stage data, images and fonts into this Unity project.\n\nThe imported proprietary content is excluded from Git by the NovaOS-FNF .gitignore rules. It will still be included in any world build that uses it. Only use and share the resulting world where you have the necessary permissions.\n\nContinue?",
                "Fetch and Import", "Cancel"))
                return;

            try
            {
                EnsureDirectory(Path.GetDirectoryName(CacheRoot));
                if (!Directory.Exists(Path.Combine(CacheRoot, ".git")))
                {
                    if (Directory.Exists(CacheRoot)) Directory.Delete(CacheRoot, true);
                    RunGit("clone --depth 1 " + Quote(RepositoryUrl) + " " + Quote(CacheRoot));
                }
                else
                {
                    RunGit("-C " + Quote(CacheRoot) + " pull --ff-only");
                }

                string source = Path.GetFullPath(CacheRoot);
                int copied = 0;
                int skipped = 0;
                var warnings = new List<string>();

                // Song stems are imported separately so the existing audio scanner can
                // build its song/variation/character-vocal catalog without modification.
                copied += CopyTree(Path.Combine(source, "songs"), AudioRoot + "/songs", true, ref skipped, warnings);
                copied += CopyTree(Path.Combine(source, "preload/music"), AudioRoot + "/music", true, ref skipped, warnings);
                copied += CopyTree(Path.Combine(source, "preload/sounds"), AudioRoot + "/SoundEffects", true, ref skipped, warnings);

                // Official chart and Story Mode data feed the existing importers.
                copied += CopyTree(Path.Combine(source, "preload/data/songs"), ChartRoot + "/songs", false, ref skipped, warnings);
                copied += CopyTree(Path.Combine(source, "preload/data/levels"), LevelRoot, false, ref skipped, warnings);

                // Preserve source-relative structures for the visual/character pipeline.
                copied += CopyTree(Path.Combine(source, "preload/images"), ImportedRoot + "/images", false, ref skipped, warnings);
                copied += CopyTree(Path.Combine(source, "preload/data"), ImportedRoot + "/data", false, ref skipped, warnings);
                copied += CopyTree(Path.Combine(source, "fonts"), ImportedRoot + "/fonts", false, ref skipped, warnings);

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                string report = "Official repository: fetched/updated\nFiles copied: " + copied +
                    "\nDuplicate MP3 files skipped when OGG exists: " + skipped +
                    "\nWarnings: " + warnings.Count +
                    "\n\nAudio: " + AudioRoot +
                    "\nCharts: " + ChartRoot +
                    "\nLevels: " + LevelRoot +
                    "\nVisual/data assets: " + ImportedRoot;

                if (warnings.Count > 0)
                    Debug.LogWarning("NovaOS official asset import completed.\n" + report + "\n\n" + string.Join("\n", warnings));
                else
                    Debug.Log("NovaOS official asset import completed.\n" + report);

                EditorUtility.DisplayDialog("Official FNF asset import", report, "Done");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Official FNF asset import failed", exception.Message +
                    "\n\nCheck that Git is installed and this computer can access GitHub. No imported files are uploaded by this tool.", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static int CopyTree(string source, string destinationAssetPath, bool preferOgg, ref int skipped, List<string> warnings)
        {
            if (!Directory.Exists(source))
            {
                warnings.Add("Source folder not found: " + source);
                return 0;
            }

            string destination = Path.GetFullPath(destinationAssetPath);
            EnsureDirectory(destination);
            string[] files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
            var oggBases = new HashSet<string>(
                files.Where(f => string.Equals(Path.GetExtension(f), ".ogg", StringComparison.OrdinalIgnoreCase))
                     .Select(f => Path.ChangeExtension(f, null)),
                StringComparer.OrdinalIgnoreCase);

            int copied = 0;
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                string extension = Path.GetExtension(file);
                if (preferOgg && string.Equals(extension, ".mp3", StringComparison.OrdinalIgnoreCase) &&
                    oggBases.Contains(Path.ChangeExtension(file, null)))
                {
                    skipped++;
                    continue;
                }

                string relative = file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string target = Path.Combine(destination, relative);
                EnsureDirectory(Path.GetDirectoryName(target));
                try
                {
                    if (!File.Exists(target) || new FileInfo(target).Length != new FileInfo(file).Length ||
                        File.GetLastWriteTimeUtc(target) != File.GetLastWriteTimeUtc(file))
                        File.Copy(file, target, true);
                    copied++;
                }
                catch (Exception exception)
                {
                    warnings.Add("Could not copy " + relative + ": " + exception.Message);
                }
            }

            return copied;
        }

        private static void RunGit(string arguments)
        {
            var start = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = Directory.GetCurrentDirectory(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(start))
            {
                if (process == null) throw new InvalidOperationException("Could not start Git.");
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new InvalidOperationException("Git exited with code " + process.ExitCode + ".\n" + stdout + "\n" + stderr);
                if (!string.IsNullOrWhiteSpace(stdout)) Debug.Log(stdout);
                if (!string.IsNullOrWhiteSpace(stderr)) Debug.Log(stderr);
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        private static void EnsureDirectory(string path)
        {
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
#endif
